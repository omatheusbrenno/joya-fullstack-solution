# Joya — Mini Gestão de Pedidos

Teste técnico Full Stack (.NET + TypeScript, AI First): cadastro e consulta de Pedidos de Clientes, de ponta a ponta, desenvolvido com Claude Code.

- **Banco**: SQL Server, com o schema e a carga em scripts idempotentes (`database/`).
- **API**: .NET 10, Vertical Slices com Minimal API, Dapper e regras de negócio em função pura (`backend/`).
- **Frontend**: React + TypeScript strict, com as abas Pedidos e Top Clientes e o modal de criação (`frontend/`).

O que a entrega precisa cumprir está em [`docs/spec.md`](docs/spec.md) (RN01–RN13, CA01–CA27), e os termos de domínio estão no [`GLOSSARY.md`](GLOSSARY.md).

**Sumário:** [Versão online](#versão-online) · [Como rodar com Docker](#como-rodar-com-docker) · [Como rodar os testes](#como-rodar-os-testes) · [Decisões técnicas](#decisões-técnicas) · [Como usei o Claude](#como-usei-o-claude) · [MCP do SQL Server](#mcp-do-sql-server) · [Parte 4](#parte-4-debug-da-query-de-top-clientes) · [Critérios de aceite](#conferência-dos-critérios-de-aceite) · [Dívidas conscientes](#dívidas-conscientes)

## Versão online

**https://joya-pedidos.vercel.app**: nada para instalar (ADR 0007).

> **A primeira requisição leva cerca de 50 s.** A API roda no plano gratuito do Render, que hiberna depois de 15 min sem uso, e o banco do Azure SQL Database Free também pausa quando fica ocioso. Se a tela demorar ou mostrar erro na primeira carga, espere um minuto e recarregue. Depois disso ela responde normalmente.

- **Banco**: Azure SQL Database (Free Offer), com o schema e a carga aplicados pelos mesmos scripts de `database/`. No Azure, a API entra com um usuário contido de privilégio mínimo, e o login do MCP não é criado.
- **API**: o mesmo `backend/Dockerfile`, no Render (`render.yaml`). A connection string só existe no painel do Render.
- **Frontend**: o build estático na Vercel (`frontend/vercel.json`). O rewrite repassa `/api` para o Render, então o navegador só fala com uma origem e o CORS continua fechado (ADR 0011). A CSP e os headers de segurança são os mesmos do `nginx.conf`.
- **Rate limit**: atrás da Vercel, a API vê o IP de saída da Vercel, e visitantes que saem pelo mesmo IP dividem o limite (ADR 0015).

Para refazer o deploy, rode `scripts/wizard-deploy.sh`. Ele guia os passos nos painéis do Azure, do Render e da Vercel e confere o `/health`, o rewrite, os headers e os E2E no ambiente online.

## Como rodar com Docker

Pré-requisito: Docker com o Compose v2. Na raiz do repositório:

```bash
docker compose up --build
```

Abra **http://localhost:8080**. Um comando sobe quatro serviços, nesta ordem:

| Serviço | O que faz |
| --- | --- |
| `db` | SQL Server 2022, publicado só em `127.0.0.1:1433` |
| `db-init` | aplica `database/00` a `05` em ordem e termina com `Exited (0)`: banco, schema, login da API, carga legível, ~50 mil Pedidos e login do MCP |
| `api` | a API .NET, sem porta publicada, só alcançável pelo `web` |
| `web` | nginx com o SPA e o proxy `/api` → `api` (mesma origem, ADR 0011) |

- **Sem `.env`, funciona**: valem as senhas de desenvolvimento do compose (ADR 0012). Para trocá-las, copie `.env.example` para `.env` e edite.
- **Rodar de novo não quebra nada**: os scripts são idempotentes, e a carga entra por id fixo, sem duplicar.
- **Para scripts e CI**: `docker compose up --build -d --wait` só devolve o terminal quando tudo está saudável.
- **Zerar o banco**: `docker compose down -v` apaga o volume, e o próximo `up` recria tudo.
- O compose fixa a sub-rede `172.30.86.0/24`, em que a API confia como proxy. Se ela colidir com outra rede Docker local, o `up` falha com "Pool overlaps".

## Como rodar os testes

| Suíte | Comando | Precisa de | Hoje |
| --- | --- | --- | --- |
| Unitários (regra pura do Pedido) | `cd backend && dotnet test --project Joya.Pedidos.Tests.Unit` | só o .NET 10 SDK | 11, em < 1 s |
| Integração (API + SQL Server real) | `cd backend && dotnet test --project Joya.Pedidos.Tests.Integration` | Docker (Testcontainers sobe o SQL Server e aplica os scripts de `database/`) | 59, em ~35 s |
| E2E (Playwright no navegador) | veja abaixo | o compose no ar | 5 |
| Frontend (lint, tipos, build) | `cd frontend && npm ci && npm run lint && npm run typecheck && npm run build` | Node | — |

E2E, contra o compose:

```bash
docker compose up --build -d --wait
cd frontend
npm ci
npx playwright install chromium   # só na primeira vez
npx playwright test               # outra URL: BASE_URL=https://joya-pedidos.vercel.app npx playwright test
```

A CI (`.github/workflows/ci.yml`) roda tudo isso em cada push e PR, em três jobs: backend (format, build, pacotes vulneráveis, unitários e integração), frontend (lint, tipos, build e `npm audit`) e compose (`up --wait` do zero, `/health` pelo nginx e Playwright).

## Decisões técnicas

Cada decisão tem um ADR em [`docs/adr/`](docs/adr/), com o contexto e as alternativas descartadas. As que mais moldam o código:

| ADR | Decisão | Por quê, em uma linha |
| --- | --- | --- |
| [0001](docs/adr/0001-backend-dotnet-10.md) | .NET 10 | o suporte do .NET 8 terminou em 10/11/2026, antes da entrega (OWASP A06) |
| [0002](docs/adr/0002-backend-minimal-api-sem-mediador.md) | Vertical Slices, Minimal API, sem MediatR | cada caso de uso numa pasta, com endpoint → handler explícito |
| [0003](docs/adr/0003-backend-dapper-com-scripts-sql.md) | Dapper + scripts SQL como fonte única do schema | a Parte 4 é sobre SQL e plano de execução, então o SQL fica à vista |
| [0004](docs/adr/0004-api-sem-autenticacao.md) | Sem autenticação, com defesa na borda | fora do enunciado; rate limit, limite de corpo e headers no lugar |
| [0005](docs/adr/0005-frontend-react.md) | React + Vite + TS strict | uma página só, sem router: mais leve, e o deploy estático na Vercel é direto |
| [0006](docs/adr/0006-testes-integracao-testcontainers.md) | Testcontainers aplicando `database/` | o teste roda os mesmos scripts de `database/` que o compose e o deploy |
| [0007](docs/adr/0007-deploy-azure-sql-render-vercel.md) | Azure SQL Free, Render e Vercel | três planos gratuitos que rodam SQL Server, contêiner e SPA |
| [0008](docs/adr/0008-api-contrato-de-erros.md) | ProblemDetails: 400 forma, 422 negócio, 413, 429, 500 | o front distingue "corrija o campo" de "o Pedido não pode ser feito" |
| [0009](docs/adr/0009-valor-total-calculado.md) | Valor Total calculado, nunca armazenado | uma fonte de verdade (os Itens), sem dado para dessincronizar |
| [0010](docs/adr/0010-mcp-sql-server-login-somente-leitura.md) | MCP com login `db_datareader` + `SHOWPLAN` | somente leitura garantido pelo SQL Server, não pela ferramenta |
| [0011](docs/adr/0011-frontend-mesma-origem-via-proxy.md) | Frontend pela mesma origem, via proxy | CORS fechado e nenhuma URL de API no bundle |
| [0012](docs/adr/0012-senhas-de-desenvolvimento-no-compose.md) | Senhas de desenvolvimento como padrão no compose | `docker compose up` sem passo manual, `.env` sobrescreve |
| [0013](docs/adr/0013-envelope-de-paginacao-generico.md) | Envelope de paginação genérico | `itens`/`totalItens` são linhas da página, não Itens de Pedido |
| [0014](docs/adr/0014-forma-do-pedido-na-regra-pura.md) | Forma do Pedido na função pura, prevalecendo sobre o negócio | RN02–RN04 e RN01/RN05/RN06 testadas juntas, sem I/O |
| [0015](docs/adr/0015-rate-limit-atras-da-vercel.md) | Rate limit atrás da Vercel conta pelo IP dela | confiar em mais um salto deixaria forjar o IP chamando o Render direto |
| [0016](docs/adr/0016-azure-usuario-contido-e-sem-login-do-mcp.md) | Usuário contido no Azure, sem login do MCP | no Azure o banco da aplicação não cria login no `master`, e o MCP só lê o banco local |

Duas decisões de implementação que valem destaque:

- **Baixa de Estoque atômica (RN09)**: um `UPDATE Produtos SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q` por Item, na mesma transação do `INSERT`. Quem perde a corrida não aparece no `OUTPUT`, e o Pedido inteiro volta com `422`. Sem lock explícito nem `SERIALIZABLE`. O teste de CA08 dispara 10 Pedidos simultâneos pela última unidade.
- **Período sem função sobre a coluna**: todo Período vira `[dataInicio 00:00, dataFim + 1 dia)`, na listagem, no Top Clientes e na query corrigida da Parte 4, para o índice de `DataPedido` continuar utilizável.

## Como usei o Claude

O projeto inteiro foi feito com o Claude Code, num fluxo fixo de skills de [mattpocock/skills](https://github.com/mattpocock/skills). Cada etapa produziu um artefato versionado ou no GitHub, e eu revisei cada um antes da etapa seguinte.

### O fluxo

| Etapa | Skill | O que saiu |
| --- | --- | --- |
| 1. Alinhar a feature | `/grill-with-docs` | o [`GLOSSARY.md`](GLOSSARY.md) e os ADRs 0001–0011, antes de qualquer código |
| 2. Especificar | `/to-spec` | o PRD na issue [#1](https://github.com/omatheusbrenno/joya-fullstack-solution/issues/1) e o [`docs/spec.md`](docs/spec.md), com RN01–RN13 e CA01–CA27 |
| 3. Planejar | plan mode | o plano em fases, copiado sem edição em [`docs/plan.md`](docs/plan.md), com a minha revisão crítica |
| 4. Fatiar | `/to-tickets` | 8 tickets verticais (#2–#9), sub-issues da #1, com bloqueios e o board Kanban [#3](https://github.com/users/omatheusbrenno/projects/3) |
| 5. Implementar | `/implement` + `/tdd` | um ticket por branch, red → green → refactor, commits a cada passo |
| 6. Revisar e entregar | `/code-review` + `/pr` | revisão por padrão do repo e por aderência ao ticket, e o PR com evidência de antes e depois |

Até o deploy, o resultado foi: 7 PRs com merge commit (#10–#16), 85 commits em Conventional Commits PT-BR, 5 ADRs a mais do que o grilling previu (0012–0016) e nenhum histórico reescrito. A comparação do plano com a execução real está em [`docs/plan.md`](docs/plan.md#10-fechamento-o-que-aconteceu-de-fato).

### O contexto que o Claude carrega

- **`CLAUDE.md`** na raiz, mais `backend/CLAUDE.md` e `frontend/CLAUDE.md`. Cada um só tem o que não dá para descobrir olhando o código: restrições do enunciado, arquitetura decidida, comandos e pegadinhas.
- **Rules** em `.claude/rules/`: OWASP Top 10:2025 para backend e frontend, carregadas pelo caminho do arquivo editado.
- **Hooks** em `.claude/hooks/`:
  - `block-history-rewrite.sh` (PreToolUse) bloqueia `rebase`, `--amend`, `push --force`, `reset --hard` e companhia;
  - `verificar-edicao.sh` (PostToolUse) roda `dotnet build` depois de cada edição em C#, e Biome + `tsc` depois de cada edição em TS.
- **Skills customizadas**, extraídas do primeiro exemplo real, e não escritas antes do código:
  - [`criar-slice-backend`](.claude/skills/criar-slice-backend/SKILL.md), depois do primeiro slice de Criar Pedido (#4);
  - [`criar-script-sql`](.claude/skills/criar-script-sql/SKILL.md), depois dos scripts de carga e do login do MCP (#6).
  Os slices e scripts seguintes foram escritos a partir delas.
- **MCP** do SQL Server (`.mcp.json`), com login somente leitura, usado na Parte 4. Detalhes em [MCP do SQL Server](#mcp-do-sql-server).
- **`docs/agents/`** diz às skills onde ficam issues, rótulos de triagem e docs de domínio.

### O que funcionou bem

- **Grilling antes da spec.** As perguntas incômodas vieram antes do código: Produto repetido é `400` ou `422`? Homônimos no Top Clientes? Valor Total armazenado? Cada resposta virou ADR ou termo do glossário, e o código usou os mesmos nomes do início ao fim.
- **Tracer bullets no lugar das fases do plan mode.** O primeiro ticket já subia banco, API, página, CI e hook. Depois dele, o `main` sempre teve algo funcionando de ponta a ponta.
- **TDD com o red como evidência.** Cada PR mostra o teste vermelho antes e verde depois. Os testes que passaram de primeira foram conferidos por mutação: tirar o `WHERE Estoque >= q` derruba o CA08, e agrupar o Top Clientes por nome derruba o CA15.
- **O hook de edição.** Ele pegou erro de compilação e de lint na hora, inclusive dois problemas na própria configuração (Biome e `node_modules`, abaixo).
- **Skills extraídas do código real.** Os slices depois do #4 saíram no mesmo formato, sem convenção acumulando no `CLAUDE.md`.
- **Segurança verificável.** As rules deram o checklist, e cada item virou teste: 429, 413, 500 sem stack trace, headers, proxy confiável, login do MCP sem escrita e CSP no navegador.

### O que precisei corrigir no código gerado

Cada correção é um commit separado, então dá para ver o antes e o depois.

| Commit | Problema no código gerado | Correção |
| --- | --- | --- |
| `dd60e5a` | O Biome não rodava: procurava um `.gitignore` dentro de `frontend/`. O hook pegou. | O Biome passou a usar só `files.includes` |
| `d51f76b` | Sem `node_modules`, o hook de edição passava calado, e um erro de TS passaria como sucesso | O hook avisa que a verificação não rodou |
| `97fb28d` | Os erros de validação saíam com as chaves em PascalCase (`DataFim`), diferente do contrato JSON | Chaves em camelCase, iguais aos campos |
| `7bf14b4` | Com Período invertido, a tela mostrava o aviso e também o resultado da consulta anterior | O resultado antigo some quando o Período é inválido |
| `9ea66cc` (na rule, não no código) | A rule A05 proibia toda composição de texto SQL, inclusive a partir de `const`, e o filtro de `GET /pedidos`, montado com trechos `const`, ficava em conflito com ela | A rule passou a proibir só a interpolação de entrada do usuário |
| `62b3f57` | Em `POST /pedidos`, Item `null` virava `500`, e corpo sem `clienteId` virava `422` "Cliente 0 não encontrado" | Os dois viram `400`, com teste |
| `2fef8e0`, `4e9c666`, `3d19520` | Na revisão: SQL repetido em strings, chaves de query do TanStack duplicadas, nomes fora do glossário, helpers de teste copiados entre classes | SQL em `const`, chaves compartilhadas, slice `ConsultarTopClientes`, helpers em `Infraestrutura/` dos testes |
| `6fd36a4` | O `/health` caía no rate limit global. A checagem de saúde do Render receberia `429` e derrubaria a API | O `/health` ficou fora do limite, com teste |
| `c19a9d7` | O wizard de deploy passava as senhas na linha de comando do `docker run` (visíveis no `ps`), e o `sed` falhava no macOS | Senhas pelo ambiente e `sed` portável |
| `3280595` | A senha gerada para a API começava com `Api_`, e o Azure SQL recusa senha com 3 letras seguidas do nome do login | Senha gerada sem pedaço do nome |

Duas correções vieram do próprio plano, antes do código (detalhes em [`docs/plan.md`](docs/plan.md)):

- o plan mode punha a CSP na API, que só devolve JSON, quando quem precisa dela é o HTML do SPA;
- e tratava Produto repetido como `422`, contra a spec.

Uma terceira veio da medição: o plano esperava `Index Seek` em `Pedidos(DataPedido)` para o ano de 2026 inteiro, e o MCP mostrou que, com 44% da tabela no ano, o scan é a escolha certa ([Parte 4](#evidência-plano-de-execução-e-statistics-iotime)).

## MCP do SQL Server

O Claude Code lê o banco do `docker compose` pelo [DBHub](https://github.com/bytebase/dbhub) (`@bytebase/dbhub@1.5.0`, versão fixa), declarado no `.mcp.json` versionado. Ao abrir o projeto, o Claude Code pede para aprovar o servidor `sqlserver`. Com o compose no ar, ele tem duas ferramentas:

- `search_objects`: lista tabelas, colunas e índices.
- `execute_sql`: roda SQL. `EXPLAIN <query>` devolve o plano estimado (o DBHub liga `SET SHOWPLAN_XML`); `EXPLAIN ANALYZE <query>` executa e devolve o plano real (`SET STATISTICS XML`). As mensagens de `SET STATISTICS IO, TIME ON` voltam junto do resultado.

**Somente leitura pela permissão, não pela ferramenta (ADR 0010).** O DBHub conecta com o login `joya_mcp`, criado por `database/05-criar-login-mcp.sql` em todo `docker compose up`, que tem só `db_datareader` e `SHOWPLAN`. Um `INSERT` ou `DELETE` falha no SQL Server com "The INSERT permission was denied", qualquer que seja o cliente. Os testes de `LoginDoMcpTests` provam isso contra um SQL Server real. O modo `readonly` do DBHub fica desligado: ele bloquearia o `SET STATISTICS` da investigação, e seria só mais uma camada sobre a garantia que já está no login.

A senha vem da variável `SENHA_MCP`. Sem ela, valem o padrão de desenvolvimento do compose e do `.mcp.json` (ADR 0012), que só alcança o SQL Server publicado em `127.0.0.1`.

### Como o MCP foi usado

Na Parte 4, todo número abaixo saiu do MCP, com o mesmo comando do `.mcp.json` e o login `joya_mcp`. O `.mcp.json` nasceu no meio da sessão, e o Claude Code só carrega servidores MCP ao iniciar. Por isso o Claude falou com o DBHub por um cliente MCP mínimo via stdio: as mesmas mensagens `tools/call`, sem reiniciar a sessão.

1. `search_objects` para conferir as tabelas da carga.
2. `execute_sql` com `SET STATISTICS IO, TIME ON` na query original e na corrigida, três vezes cada. A tabela abaixo traz a terceira, com cache quente.
3. `EXPLAIN ANALYZE` das duas, para ler os operadores, as linhas estimadas contra as reais e as leituras por operador.
4. A mesma medição num Período seletivo (outubro de 2026), para isolar o efeito do `YEAR()` sobre o seek.
5. A saída da corrigida comparada com `GET /relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31`: as mesmas cinco linhas, com os mesmos valores.

Um `INSERT` e um `DELETE` tentados pelo MCP voltaram negados pelo SQL Server, o que confirma a garantia do login na prática.

## Parte 4: debug da query de Top Clientes

A query do enunciado deveria devolver os Clientes que mais compraram em 2026. A original e a corrigida estão lado a lado em `database/parte-4/`, fora do padrão `NN-*.sql`, então o `db-init` não as executa:

- [`top-clientes-original.sql`](database/parte-4/top-clientes-original.sql), sem alteração;
- [`top-clientes-corrigida.sql`](database/parte-4/top-clientes-corrigida.sql), a mesma lógica do endpoint `GET /relatorios/top-clientes`.

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
```

Ambiente da medição: SQL Server 2022 (CU28) do compose, com 50.019 Pedidos e 100.028 Itens (a carga de `03` e `04` mais 7 Pedidos criados pelo E2E). 22.141 Pedidos (44%) caem em 2026.

### O que estava errado

1. **O `WHERE` anula o `LEFT JOIN`.** Num Cliente sem Pedido, `p.DataPedido` é `NULL`. `YEAR(NULL) = 2026` não é verdadeiro, então a linha cai. O `LEFT JOIN` promete os Clientes sem compra e o `WHERE` os descarta: o resultado é o de um `INNER JOIN`, só que mais caro. O plano mostra isso. Um **Merge Join (Left Outer Join)** gera 50.020 linhas, incluindo os Clientes sem Pedido com `NULL`, e só depois um **Filter** `datepart(year, DataPedido) = 2026` joga fora 27.879 delas. Para um top de quem mais comprou, Cliente sem compra não entra, então a correção é o `INNER JOIN` explícito. Se o pedido fosse listar todos os Clientes com zero, o filtro de data iria para o `ON`.
2. **O `YEAR()` sobre a coluna impede o seek.** Com uma função em volta, o SQL Server não consegue buscar um trecho do índice `IX_Pedidos_DataPedido`: lê todos os Pedidos e avalia `YEAR()` linha a linha. Ele também perde o histograma: estimou 28.350 linhas para as 22.141 reais. A correção é o intervalo semiaberto `DataPedido >= '2026-01-01' AND DataPedido < '2027-01-01'`, sem função na coluna, que estima 22.138. É o mesmo Período `[início, fim + 1 dia)` da API. Para 2026 inteiro, o otimizador ainda escolhe o scan, porque o ano tem 44% da tabela. O seek perdido aparece num Período seletivo, medido em [O seek num Período seletivo](#o-seek-num-período-seletivo).
3. **O `GROUP BY c.Nome` soma homônimos.** Os Clientes 1 e 4 se chamam "Ana Souza". A original devolve uma "Ana Souza" com R$ 32.821.025,50, que é a soma de duas pessoas (R$ 17.480.723,70 + R$ 15.340.301,80). A correção agrupa por `ClienteId` e só depois busca o nome (RN13).
4. **A ordem é crescente.** `ORDER BY Total` sem `DESC` põe quem menos comprou no topo: a original começa por "Elisa Rocha". A correção usa `ORDER BY ValorTotal DESC`, com desempate por `ClienteId` para a ordem ser estável.
5. **Falta o limite de cinco.** A original devolve um total por nome para todos com compra em 2026: 7 linhas para 8 Clientes, porque os homônimos se fundem. A correção usa `TOP (5)`.

### Resultado: antes e depois

| Original (7 linhas, crescente) | | Corrigida (5 linhas) | | |
| --- | --- | --- | --- | --- |
| **Nome** | **Total** | **Cliente** | **Pedidos** | **Valor Total** |
| Elisa Rocha | 4.324.470,40 | 1 · Ana Souza | 4.790 | 17.480.723,70 |
| Fernanda Alves | 4.392.813,00 | 4 · Ana Souza | 4.193 | 15.340.301,80 |
| Diego Ferreira | 6.574.396,80 | 7 · Carlos Lima | 3.587 | 13.078.698,80 |
| Carla Mendes | 8.760.262,50 | 2 · Bruno Lima | 2.993 | 10.888.696,40 |
| Bruno Lima | 10.888.696,40 | 3 · Carla Mendes | 2.392 | 8.760.262,50 |
| Carlos Lima | 13.078.698,80 | | | |
| Ana Souza (1 + 4 somados) | 32.821.025,50 | | | |

### Evidência: plano de execução e `STATISTICS IO/TIME`

Período de 2026 inteiro, cache quente:

| | Original | Corrigida |
| --- | --- | --- |
| Leituras lógicas: Pedidos | 134 (Index Scan em `IX_Pedidos_ClienteId_DataPedido`) | 145 (Clustered Index Scan em `PK_Pedidos`) |
| Leituras lógicas: ItensPedido | 507 (Index Scan) | 507 (Index Scan) |
| Leituras lógicas: Clientes | 2 | 10 (5 seeks, só nos vencedores) |
| Tempo de CPU / decorrido | ≈ 51 ms / ≈ 50 ms | ≈ 24 ms / ≈ 23 ms |
| Linhas estimadas × reais do filtro de data | 28.350 × 22.141 | 22.138 × 22.141 |

Operadores do plano real:

- **Original**: Clustered Index Scan em Clientes → Index Scan em Pedidos → **Merge Join (Left Outer)** com 50.020 linhas → **Filter** `YEAR()` → **Hash Match (Right Outer Join)** com os 100.028 Itens, gerando 44.274 linhas → **Hash Match (Aggregate)** por Nome → **Sort** crescente.
- **Corrigida**: Index Scan em `IX_ItensPedido_PedidoId` → **Stream Aggregate** do Valor Total por Pedido, que aproveita a ordem do índice → **Merge Join** com os 22.141 Pedidos de 2026 (as duas entradas já vêm ordenadas pelo id do Pedido) → **Hash Match (Aggregate)** por `ClienteId`, com 8 linhas → **Sort** → **Nested Loops** com **Clustered Index Seek** em Clientes, só para os 5 → **Top**.

Para o ano inteiro, as leituras ficam praticamente iguais e o tempo cai pela metade. O ganho vem da forma do plano: a original carrega 50 mil linhas por um join externo e faz um hash join com os 100 mil Itens; a corrigida agrega os Itens num passe ordenado e junta por merge.

O plano da corrigida **não faz seek em Pedidos para o ano inteiro, e está certo**. 44% da tabela cai em 2026: ler as 145 páginas do índice clusterizado em ordem custa menos do que um seek seguido de um hash join. Por isso não entrou índice novo. Os de `01-criar-schema.sql` bastam: `IX_ItensPedido_PedidoId ... INCLUDE (Quantidade, PrecoUnitario)` em todo Período, e `IX_Pedidos_DataPedido` quando o Período é seletivo.

### O seek num Período seletivo

Para isolar o efeito do `YEAR()`, a corrigida rodou em outubro de 2026 (2.031 Pedidos, 4% da tabela) com as duas formas de filtro:

| | `YEAR(DataPedido) = 2026 AND MONTH(DataPedido) = 10` | `DataPedido >= '2026-10-01' AND DataPedido < '2026-11-01'` |
| --- | --- | --- |
| Acesso a Pedidos | Clustered Index Scan em `PK_Pedidos` | **Index Seek** em `IX_Pedidos_DataPedido` |
| Leituras lógicas: Pedidos | 145 | **9** |
| Linhas estimadas × reais | 3.138 × 2.031 | 1.830 × 2.031 |

Com a função, o SQL Server lê a tabela inteira para achar 4% dela. Com o intervalo, vai direto ao trecho do índice: 16 vezes menos páginas. A estimativa também melhora: o erro cai de 54% para cima para 10% para baixo. Nos dois casos os Itens continuam num Index Scan de cerca de 500 páginas: agregá-los num passe ordenado custa menos do que 2 mil seeks. O tempo total fica em ≈ 20 ms nos dois, por causa desse scan. A diferença cresce com o volume: o scan custa a tabela inteira, e o seek só o trecho do Período.

## Conferência dos critérios de aceite

Em 14/11/2026, os 27 critérios de [`docs/spec.md`](docs/spec.md#5-critérios-de-aceite) foram conferidos um a um, por dois caminhos:

- **contra o sistema rodando**: o `docker compose up` local, pelo nginx em `localhost:8080`, com `curl` e o navegador;
- **pelas suítes**: 11 unitários, 59 de integração e 5 E2E, todos verdes no mesmo dia. A CI do `main` também estava verde.

| CA | Ao vivo, no compose | Teste automatizado |
| --- | --- | --- |
| CA01 | `201` com Preço Unitário do Produto, Valor Total 359,80, data em UTC; Estoque do Mouse 182 → 181 | `Pedido_valido_devolve_201_...e_baixa_o_Estoque` |
| CA02 | `422` com `clienteId: Cliente não encontrado.`; total de Pedidos e Estoque iguais antes e depois | `Cliente_inexistente_devolve_422_...` |
| CA03 | `400` para Pedido sem Itens | `Pedido_sem_Itens_ou_com_mais_de_50_devolve_400` |
| CA04 | `400` em `itens[0].quantidade` (0) e `itens[1].quantidade` (10.001) | `Quantidade_fora_de_1_a_10000_...` |
| CA05 | `400`: "O Produto 2 já aparece no Item 1." | `Produto_repetido_devolve_400` |
| CA06 | `422` em `itens[0].produtoId` | `Produto_inexistente_devolve_422_indicando_o_Item` |
| CA07 | `422` listando os dois Itens com o Estoque disponível; total de Pedidos e Estoque iguais antes e depois | `Estoque_insuficiente_em_dois_Itens_...` |
| CA08 | só por teste: exige um Produto com Estoque 1 | `Pedidos_simultaneos_pela_ultima_unidade_...` (10 POSTs em paralelo) |
| CA09 | `precoUnitario: 0.01` no request, 79,90 na resposta | `Pedido_valido_devolve_201_...` |
| CA10 | 20 linhas, `pagina: 1`, `tamanhoPagina: 20`, `totalItens: 50022`, da mais recente para a mais antiga | `Sem_filtros_...`, `Pagina_com_20_por_padrao_...` |
| CA11 | o Período 30/06/2026 traz o Pedido das 23:59:59; Cliente + Período combinados | `Filtra_por_Cliente_e_por_Periodo_...` |
| CA12 | `400` em `dataFim` (invertido) e em `tamanhoPagina=101` | `Parametros_invalidos_devolvem_400_...` |
| CA13 | cada linha com id, Cliente, data, `quantidadeItens` e `valorTotal` | `Sem_filtros_...` |
| CA14 | 5 Clientes de 2026 em ordem decrescente, com `quantidadePedidos` | `Devolve_no_maximo_5_Clientes_...` |
| CA15 | Ana Souza #1 (17.480.723,70) e #4 (15.340.301,80) em linhas separadas | `Clientes_homonimos_aparecem_como_entradas_distintas_...` |
| CA16 | `400` sem `dataFim`; `[]` para 2030 | `Periodo_ausente_ou_invertido_...`, `Periodo_sem_Pedidos_...` |
| CA17 | 105 requisições num minuto: as excedentes recebem `429`; corpo de 93 KB → `413` | `Acima_de_100_...`, `Acima_de_20_POST_...`, `Corpo_acima_de_64_KB_...` |
| CA18 | só por teste: exige forçar uma exceção na API | `Excecao_inesperada_devolve_500_...sem_stack_trace...` |
| CA19 | `db` parado → `503 Unhealthy`; `db` de volta → `200 Healthy` | `Health_com_banco_no_ar_...`, `Health_sem_banco_...` |
| CA20 | 20 linhas em BRL ("R$ 359,80"), paginação, filtro por Cliente (só "Ana Souza (#4)"), "Nenhum Pedido encontrado." num Período de 2030 | E2E `criar um Pedido e vê-lo na listagem` |
| CA21 | o Produto aparece como "Mouse sem fio · R$ 79,90 · Estoque: 181", e a prévia mostra "R$ 79,90" | E2E `o modal bloqueia Pedido sem Itens e Produto repetido e mostra as violações 422` (inclui o botão desabilitado durante o POST) |
| CA22 | depois de criar: confirmação "Pedido #… criado", Estoque do Mouse 181 → 180 no modal, Bruno Lima no Top Clientes de 2.996 para 2.997 Pedidos (+ R$ 79,90) | E2E `criar um Pedido e vê-lo na listagem` (o modal fecha, a confirmação aparece e a listagem filtrada se atualiza) |
| CA23 | a aba abre em 01/01/2026–31/12/2026, com "Ana Souza (#1)" e "Ana Souza (#4)" | E2E `Top Clientes num Período fixo da carga ...` |
| CA24 | rodar de novo: `up --build -d --wait` sobre o volume existente, com `db-init` em `Exited (0)` e tudo saudável | subida do zero: verificada pelo job `compose` da CI em cada push |
| CA25 | — | seção [Parte 4](#parte-4-debug-da-query-de-top-clientes) e `database/parte-4/` |
| CA26 | — | unitários de `RegrasDoPedido`: RN01, RN05 e RN06 juntas (RN11), RN02 (0, 51 e 50 Itens), RN03, RN04, RN07 e RN10 (com centavos). Integração: CA01–CA19 nas linhas acima. Um comando por tipo, em [Como rodar os testes](#como-rodar-os-testes) |
| CA27 | 5/5 contra o compose | job `compose` da CI roda o Playwright |

Os 27 critérios são atendidos. Dois só têm prova por teste (CA08 e CA18), porque reproduzi-los ao vivo exigiria escrever direto no banco ou quebrar a API de propósito. Nesta conferência, o CA24 não foi refeito do zero na máquina local (isso apagaria o volume com `down -v`). A CI faz essa subida do zero em todo push.

## Dívidas conscientes

Nada abaixo viola um critério de aceite. São limites que eu conheço e deixei de fora por prazo ou por escopo:

- **Erro de conversão sem o campo.** `GET /pedidos?clienteId=abc` devolve `400` em ProblemDetails, mas sem a chave `clienteId` em `errors`. O erro de conversão acontece no binding, antes da validação. Pendência anotada desde o PR #11.
- **Interface sem polimento de layout nem versão mobile.** As telas cumprem os CAs no desktop, mas não foram ajustadas para telas pequenas.
- **Contagem sem separador de milhar.** A paginação mostra "50022 Pedidos". Os valores em dinheiro já saem em BRL pt-BR.
- **Sem testes Vitest no frontend.** As regras do front (Período, prévia do Valor Total, bloqueios do modal) são cobertas pelos E2E, e as do Pedido também são validadas no servidor.
- **Rate limit compartilhado na versão online.** Atrás da Vercel, visitantes que saem pelo mesmo IP dela dividem o limite (ADR 0015).
- **Primeira requisição lenta online**, pela hibernação do Render e do Azure SQL Free (ver [Versão online](#versão-online)).
- **Fora de escopo pela spec:** autenticação (ADR 0004), status, edição e cancelamento de Pedido, detalhe do Pedido, cadastro de Clientes e Produtos, descontos, impostos e frete, e chave de idempotência no `POST /pedidos`.
