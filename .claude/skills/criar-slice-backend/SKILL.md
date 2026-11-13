---
name: criar-slice-backend
description: Padrão de um caso de uso da API (Vertical Slice com Minimal API, Dapper e regra pura). Use ao criar ou mudar um endpoint em backend/Joya.Pedidos.Api/Features/, uma consulta ou um comando, ou os testes de um slice.
---

# Criar um slice do backend

Um slice é um caso de uso inteiro em `Features/<Feature>/<CasoDeUso>/`: endpoint → handler concreto → (regra pura) → SQL. Os exemplos vivos são a fonte de verdade. Leia o mais parecido antes de escrever:

- **Consulta**: `Features/Pedidos/ListarPedidos/` (query string validada, filtros opcionais, paginação). Uma consulta mínima é `Features/Produtos/ListarProdutos/`.
- **Comando com regra de negócio**: `Features/Pedidos/CriarPedido/` (função pura, transação, 400/422).

Os nomes vêm do `GLOSSARY.md`: domínio em PT-BR, sufixo técnico em inglês (`CriarPedidoHandler`, `ListarPedidosEndpoint`).

## Passos

Faça na ordem do `/tdd`, um teste por ciclo. Cada passo só termina com o teste daquele ciclo verde.

1. **Seams.** Combine com o humano quais seams o slice testa:
   - a regra pura, com teste unitário, quando o slice tem regra de negócio;
   - o HTTP, com teste de integração, sempre.
2. **Regra pura** (se houver). Escreva primeiro o teste em `Joya.Pedidos.Tests.Unit/Features/<Feature>/<CasoDeUso>/` e depois a regra.
3. **Teste de integração do caminho feliz**, em `Joya.Pedidos.Tests.Integration/Features/<Feature>/`. Ele fica red antes de a rota existir.
4. **Slice**:
   - os arquivos `<CasoDeUso>Request.cs`, `<CasoDeUso>Response.cs`, `<CasoDeUso>Handler.cs` e `<CasoDeUso>Endpoint.cs`;
   - o par `Add<CasoDeUso>()`/`Map<CasoDeUso>()` registrado em `Program.cs`.
5. **Casos de erro e de borda** pelo HTTP, um ciclo por critério de aceite.

   Um teste que passa de primeira não provou nada. Quebre de propósito o trecho que ele protege, veja o teste ficar red e desfaça a mudança com `git checkout -- <arquivo>`.
6. **Verificação**: `dotnet format --verify-no-changes`, depois a suíte unitária e a de integração inteiras.

## Endpoint

- O arquivo `<CasoDeUso>Endpoint.cs` é uma classe estática com duas extensões:
  - `Add<CasoDeUso>(IServiceCollection)` registra o handler `AddScoped`;
  - `Map<CasoDeUso>(IEndpointRouteBuilder)` mapeia a rota.
- O endpoint só traduz: chama o handler e converte o resultado em `TypedResults`. A tradução fica num `ParaHttp` com um `Results<...>` explícito (ver `CriarPedidoEndpoint`).
- Query string: um record com `[AsParameters]`, validado na borda pelo `AddValidation` com DataAnnotations e `IValidatableObject`. Data em query string usa `Infraestrutura/DataIso` (formato estrito `yyyy-MM-dd`).
- Erros em ProblemDetails (ADR 0008), com as chaves em camelCase, no caminho do JSON (`itens[1].quantidade`):
  - **400** é erro de forma: `TypedResults.ValidationProblem(erros)`;
  - **422** é regra de negócio: `TypedResults.Problem(new HttpValidationProblemDetails(erros) { Status = 422 })`. O formato `errors` por campo é o mesmo do 400, então o frontend lê os dois igual.
- Sem `Location` quando não existe a rota de detalhe.

## Handler

- É uma classe `sealed` com dependências por construtor primário: `FabricaDeConexao` e, se precisar, `ILogger<T>`. Não tem estado.
- Orquestra só o I/O: carrega, chama a regra pura, persiste. Toda decisão de negócio fica na regra.
- Devolve um tipo do slice, nunca `IResult`. Num comando, o tipo é uma união pequena: `PedidoCriado | PedidoNaoCriado`.
- Os logs usam `[LoggerMessage]` (`partial`), com ids e contagens, nunca dados pessoais (A09).

## SQL (Dapper, ADR 0003)

- O SQL fica em `private const string` dentro do handler. Um trecho repetido pode ser outro `const`, interpolado em tempo de compilação. Nenhum valor de fora entra no texto (A05).
- Os parâmetros passam por objeto anônimo ou por `DynamicParameters` com `DbType` explícito. Cada chamada vai num `CommandDefinition` com o `cancellationToken` (e a `transaction`, se houver).
- Uma lista que vem do request viaja num único parâmetro JSON lido com `OPENJSON(@X) WITH (...)`. O número de parâmetros não cresce com a entrada.
- Filtro opcional segue o padrão `(@X IS NULL OR col = @X)` com `OPTION (RECOMPILE)`.
- Período vira o intervalo semiaberto `[início, fim + 1 dia)`, sem função sobre a coluna.
- A coluna `DATETIME2` volta do banco sem `Kind`. Aplique `DateTime.SpecifyKind(..., Utc)` antes de responder: a data sai com `Z` no JSON.
- Num comando com escrita, abra a transação com `BeginTransactionAsync(ReadCommitted)`. Mudança concorrente de saldo usa `UPDATE` condicional (`WHERE saldo >= @q`) com `OUTPUT`: o que faltar no OUTPUT perdeu a corrida, então faça rollback e devolva 422.
- A API roda com o login `joya_api` (`db_datareader` + `db_datawriter`). Ele não tem DDL.

## Regra pura

- É uma classe `static` (ex.: `RegrasDoPedido`) com uma função `Avaliar(...)`. A função recebe o request e o que o handler já carregou e devolve uma união: o resultado montado ou uma lista de violações. Não lança exceção para fluxo esperado (ADR 0002).
- Cada `Violacao` tem `Tipo` (`Forma` → 400, `Negocio` → 422), `Campo` no caminho do JSON e `Mensagem` em PT-BR.
- Todas as violações saem de uma vez (RN11). Se houver violação de forma, só ela é devolvida: regra de negócio sobre um request malformado não diz nada, e o status é um só.
- Os tipos de entrada e saída (`ItemSolicitado`, `ProdutoEncontrado`, `PedidoMontado`) ficam num arquivo do slice, separado da regra.

## Testes

- **Unitários**: rodam sem I/O e em milissegundos. O valor esperado é um literal independente, nunca recalculado do jeito do código. Cubra várias violações juntas e valores com centavos.
- **Integração**:
  - a classe leva `[Collection(ColecaoDoBanco.Nome)]`, recebe `BancoDeTestes` no construtor e implementa `IAsyncLifetime`;
  - `InitializeAsync` limpa o banco com `CenarioDoBanco.LimparAsync()` e cria o `ApiDeTestes`;
  - o arrange é feito pelo `CenarioDoBanco`, com a conexão admin. Faltando um insert, acrescente um método lá.
  - o assert é feito **pela API** (`GET /produtos` para o Estoque, `GET /pedidos` para o que foi persistido), nunca por consulta direta ao banco;
  - o corpo da resposta é lido em records `private sealed` locais à classe de teste;
  - erro: confira o status, o `application/problem+json` e as chaves de `errors`.
- **Concorrência**: dispare N requisições com `Task.WhenAll` (N > 2 aumenta a chance de a corrida acontecer de fato) e confira o estado final pela API.

## Commits

Um commit por passo verde, em Conventional Commits PT-BR, com o escopo da feature: `feat(pedidos): ...` ou `test(pedidos): ...`.
