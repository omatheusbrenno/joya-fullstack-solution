# Spec — Mini Gestão de Pedidos

> Retrato aprovado do PRD [#1](https://github.com/omatheusbrenno/joya-fullstack-solution/issues/1). Os termos seguem o [`GLOSSARY.md`](../GLOSSARY.md) e o porquê de cada decisão está em [`docs/adr/`](adr/). O PRD continua sendo o rascunho vivo, e este arquivo é a versão que o código precisa cumprir.

## 1. Objetivo

Permitir que o operador **crie Pedidos** de Clientes com seus Itens, validando a existência do Cliente e o Estoque de cada Produto. O operador também **consulta os Pedidos** por Cliente e por Período, com paginação, e vê os **Top Clientes**: os cinco Clientes de maior Valor Total num Período.

A entrega é de ponta a ponta: SQL Server, API .NET 10 em Vertical Slices e SPA em React + TypeScript. Tudo sobe com `docker compose up`, com testes unitários e de integração contra um SQL Server real.

**Fora de escopo:** autenticação (ADR 0004); status, edição e cancelamento de Pedido; detalhe do Pedido (`GET /pedidos/{id}`); cadastro de Clientes e Produtos; descontos, impostos e frete; múltiplas moedas e fusos horários; chave de idempotência.

## 2. Regras de negócio

| # | Regra | Violação |
|---|---|---|
| RN01 | O Cliente do Pedido precisa existir. | `422` |
| RN02 | O Pedido tem de 1 a 50 Itens de Pedido. | `400` |
| RN03 | A quantidade de cada Item é um inteiro entre 1 e 10.000. | `400` |
| RN04 | O mesmo Produto não aparece em dois Itens do mesmo Pedido. | `400` |
| RN05 | Todo Produto dos Itens precisa existir. | `422` |
| RN06 | Cada Produto precisa ter Estoque maior ou igual à quantidade pedida. Faltando Estoque em qualquer Item, o Pedido inteiro é recusado. | `422` |
| RN07 | O Preço Unitário de cada Item é copiado do preço atual do Produto na criação. O request não tem campo de preço. | — |
| RN08 | A Data do Pedido é o instante atual em UTC, definido pelo servidor. | — |
| RN09 | Criar o Pedido faz a Baixa de Estoque na mesma transação, de forma atômica: dois Pedidos simultâneos nunca vendem a mesma unidade. | `422` (RN06) |
| RN10 | Valor Total do Pedido = Σ quantidade × Preço Unitário dos Itens. É sempre calculado, nunca armazenado (ADR 0009). | — |
| RN11 | Todas as violações são devolvidas de uma vez, não só a primeira. | — |
| RN12 | O Período é um intervalo de datas inclusivo, em UTC. Data inicial posterior à final é inválida. | `400` |
| RN13 | Top Clientes: até 5 Clientes, agrupados por Cliente (id, nunca nome), em ordem decrescente de Valor Total, com desempate pelo id do Cliente. Clientes sem Pedidos no Período ficam de fora. O Período é obrigatório. | `400` sem Período |

**Limites de proteção** (OWASP A04/A10):
- 100 requisições por minuto por IP no geral, e 20 por minuto em `POST /pedidos` (`429`).
- Corpo de requisição de até 64 KB.
- `tamanhoPagina` de no máximo 100.

## 3. Endpoints

Todas as rotas são servidas sob `/api` pelo proxy de mesma origem (ADR 0011). Os erros saem em ProblemDetails (ADR 0008):
- `400`: formato ou validação de campo, com erros agrupados por campo.
- `422`: regra de negócio, com todas as violações e o Item afetado.
- `429`: rate limit.
- `500`: erro genérico, sem detalhes internos.

### `POST /pedidos`

Request:

```json
{ "clienteId": 1, "itens": [ { "produtoId": 3, "quantidade": 2 } ] }
```

`201 Created`:

```json
{
  "id": 1001,
  "clienteId": 1,
  "nomeCliente": "Ana Souza",
  "dataPedido": "2026-11-14T13:05:00Z",
  "itens": [ { "produtoId": 3, "nomeProduto": "Teclado", "quantidade": 2, "precoUnitario": 150.00 } ],
  "valorTotal": 300.00
}
```

### `GET /pedidos`

| Parâmetro | Tipo | Regra |
|---|---|---|
| `clienteId` | int | opcional |
| `dataInicio` | data `yyyy-MM-dd` | opcional, inclusiva |
| `dataFim` | data `yyyy-MM-dd` | opcional, inclusiva, ≥ `dataInicio` |
| `pagina` | int | ≥ 1, padrão 1 |
| `tamanhoPagina` | int | 1 a 100, padrão 20 |

`200 OK`, ordenado por Data do Pedido decrescente e, em caso de empate, por id decrescente:

```json
{
  "itens": [ { "id": 1001, "clienteId": 1, "nomeCliente": "Ana Souza", "dataPedido": "2026-11-14T13:05:00Z", "quantidadeItens": 1, "valorTotal": 300.00 } ],
  "pagina": 1,
  "tamanhoPagina": 20,
  "totalItens": 1
}
```

### `GET /relatorios/top-clientes`

Parâmetros `dataInicio` e `dataFim`, ambos obrigatórios e inclusivos. `200 OK`:

```json
[ { "clienteId": 7, "nome": "Carlos Lima", "quantidadePedidos": 42, "valorTotal": 18350.90 } ]
```

### Endpoints de apoio

| Rota | Resposta `200` |
|---|---|
| `GET /clientes` | `[{ "id", "nome" }]`, para os seletores de Cliente |
| `GET /produtos` | `[{ "id", "nome", "preco", "estoque" }]`, para o modal de criação |
| `GET /health` | saúde da API, incluindo a conexão com o banco |

O Período é sempre convertido para o intervalo semiaberto `[dataInicio 00:00, dataFim + 1 dia 00:00)` sobre `DataPedido`. Nunca se aplica função sobre a coluna, para preservar o uso do índice.

## 4. Modelo de dados

```
Clientes   (Id PK, Nome)
Produtos   (Id PK, Nome, Preco decimal(18,2), Estoque int CHECK >= 0)
Pedidos    (Id PK, ClienteId FK → Clientes, DataPedido datetime2 UTC)
ItensPedido(Id PK, PedidoId FK → Pedidos, ProdutoId FK → Produtos,
            Quantidade int CHECK 1..10000, PrecoUnitario decimal(18,2),
            UNIQUE (PedidoId, ProdutoId))
```

| Índice | Para quê |
|---|---|
| `Pedidos(DataPedido)` | filtro por Período e ordenação da listagem |
| `Pedidos(ClienteId, DataPedido)` | filtro por Cliente + Período e Top Clientes |
| `ItensPedido(PedidoId) INCLUDE (Quantidade, PrecoUnitario)` | Valor Total sem lookup |
| `ItensPedido(ProdutoId)` | suporte à FK |

O schema nasce só dos scripts idempotentes de `database/` (ADR 0003). A carga tem:
- Dados legíveis: cerca de 10 Clientes, incluindo dois homônimos e Clientes sem Pedidos em 2026, e cerca de 15 Produtos, um deles com Estoque baixo.
- Um volume gerado de forma **determinística**, com cerca de 50 mil Pedidos entre 2025 e 2026.

Um login somente leitura (`db_datareader` + `SHOWPLAN`) serve ao MCP (ADR 0010).

## 5. Critérios de aceite

### Criar Pedido

- [ ] **CA01** Dado um Cliente existente e Produtos com Estoque, quando crio um Pedido com dois Itens, então recebo `201` com Preço Unitário igual ao preço atual de cada Produto, Valor Total correto e Data do Pedido em UTC, e o Estoque de cada Produto diminui exatamente pela quantidade pedida.
- [ ] **CA02** Dado um `clienteId` inexistente, quando crio o Pedido, então recebo `422` apontando o Cliente, nada é persistido e nenhum Estoque muda.
- [ ] **CA03** Dado um Pedido sem Itens, ou com mais de 50, quando o envio, então recebo `400`.
- [ ] **CA04** Dada uma quantidade 0, negativa ou acima de 10.000, quando envio o Pedido, então recebo `400` indicando o Item.
- [ ] **CA05** Dado o mesmo Produto em dois Itens, quando envio o Pedido, então recebo `400` por Produto duplicado.
- [ ] **CA06** Dado um `produtoId` inexistente, quando envio o Pedido, então recebo `422` indicando o Item.
- [ ] **CA07** Dados dois Itens com Estoque insuficiente, quando envio o Pedido, então recebo `422` listando os dois Itens, e nada é persistido.
- [ ] **CA08** Dado um Produto com 1 unidade em Estoque, quando dois Pedidos simultâneos pedem 1 unidade cada, então exatamente um recebe `201`, o outro recebe `422`, e o Estoque termina em 0.
- [ ] **CA09** Dado um request com um campo de preço, quando crio o Pedido, então o campo é ignorado e vale o preço do Produto.

### Listar Pedidos

- [ ] **CA10** Sem filtros, recebo os Pedidos ordenados do mais recente para o mais antigo, paginados com 20 por padrão, com `totalItens` correto.
- [ ] **CA11** Com `clienteId`, recebo só Pedidos daquele Cliente. Com `dataInicio` e/ou `dataFim`, recebo só Pedidos do Período, com as duas pontas inclusivas (um Pedido às 23:59:59 do último dia aparece). Os filtros se combinam.
- [ ] **CA12** Com `dataInicio` posterior a `dataFim`, ou `tamanhoPagina` acima de 100, recebo `400`.
- [ ] **CA13** Cada linha traz id, Cliente (id e nome), Data do Pedido, quantidade de Itens e Valor Total igual à soma dos Itens.

### Top Clientes

- [ ] **CA14** Dado um Período, recebo no máximo 5 Clientes, em ordem decrescente de Valor Total, com quantidade de Pedidos.
- [ ] **CA15** Dados dois Clientes homônimos com Pedidos no Período, eles aparecem como entradas distintas, com valores próprios.
- [ ] **CA16** Clientes sem Pedidos no Período não aparecem. Sem `dataInicio` ou `dataFim`, recebo `400`.

### Proteção e operação

- [ ] **CA17** Acima do limite de requisições por IP, recebo `429`. Com corpo acima de 64 KB, a requisição é recusada.
- [ ] **CA18** Um erro inesperado devolve `500` em ProblemDetails, sem stack trace nem detalhes internos.
- [ ] **CA19** `GET /health` responde saudável com o banco no ar e não saudável sem ele.

### Frontend

- [ ] **CA20** A aba Pedidos lista os Pedidos com filtros de Cliente e Período, paginação, valores em BRL (pt-BR) e mensagem própria para "nenhum Pedido encontrado".
- [ ] **CA21** O modal cria um Pedido escolhendo Cliente e Itens. Ele mostra preço, Estoque e uma prévia do Valor Total, bloqueia Pedido sem Itens e Produto duplicado, desabilita o envio durante o POST e exibe as violações `422` devolvidas pela API.
- [ ] **CA22** Depois de criar um Pedido, o modal fecha, uma confirmação aparece e a listagem, o Top Clientes e o Estoque dos Produtos se atualizam.
- [ ] **CA23** A aba Top Clientes abre com o Período do ano corrente, permite alterá-lo e diferencia homônimos.

### Ambiente, Parte 4 e testes

- [ ] **CA24** Com Docker instalado, `docker compose up` sobe banco (com scripts executados automaticamente), API e frontend, sem ajustes manuais. Rodar de novo não quebra nada.
- [ ] **CA25** A query da Parte 4, original e corrigida, fica em `database/`. O README explica cada erro: `LEFT JOIN` anulado pelo `WHERE`, `YEAR()` impedindo o seek, agrupamento por nome, ordem crescente e falta do limite de cinco. Também mostra a evidência de antes e depois (plano de execução e `STATISTICS IO/TIME`).
- [ ] **CA26** Os testes unitários cobrem RN01 a RN07, RN10 e RN11 na função pura de regras. Os testes de integração cobrem CA01 a CA19 contra um SQL Server real (Testcontainers). Os dois tipos rodam com um comando cada, documentado no README.
- [ ] **CA27** (diferencial) O Playwright cobre "criar Pedido e vê-lo na listagem" e "Top Clientes de um Período fixo da carga", contra o compose, também na CI.
