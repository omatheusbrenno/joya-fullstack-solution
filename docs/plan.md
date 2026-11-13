# Plano de implementação

Este arquivo tem duas partes:

1. **Plano gerado em plan mode.** É a saída do Claude Code em plan mode, copiada sem edição. O prompt foi: "planeje a implementação a partir da spec (issue #1, `docs/spec.md`) e dos ADRs".
2. **O que ajustei ou discordei e por quê.** É a minha revisão crítica desse plano, comparada ao fatiamento vertical feito depois com `/to-tickets`.

O plano que vale para a execução são os tickets do GitHub Issues (board #3). Este arquivo registra o raciocínio.

---

## Parte 1 — Plano gerado em plan mode


### Contexto

O repositório já tem a configuração AI First (CLAUDE.md por app, regras OWASP em `.claude/rules/`, hook de bloqueio de reescrita do histórico), o `GLOSSARY.md`, os ADRs 0001–0011, o PRD na issue #1 e o `docs/spec.md` (RN01–RN13, CA01–CA27). Ainda não há código: não existem `backend/` com solution, `frontend/` com projeto, `database/` nem `docker-compose.yml`. O objetivo é entregar até 15/11/2026, 16:10, todo o obrigatório do enunciado (Partes 1 a 6 + README) e, depois, os diferenciais (Playwright, GitHub Actions e deploy).

### Abordagem

Construir de baixo para cima, garantindo a cada fase que o que existe compila, sobe e tem teste. Toda fase termina com um commit pequeno, em Conventional Commits PT-BR.

#### Fase 0 — Fundação do repositório (≈ 45 min)

1. `backend/Joya.Pedidos.slnx` com três projetos:
   - `Joya.Pedidos.Api` (ASP.NET Core 10, Minimal API, `Nullable` e `TreatWarningsAsErrors` ligados).
   - `Joya.Pedidos.Tests.Unit` (xUnit).
   - `Joya.Pedidos.Tests.Integration` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.MsSql`).
2. `Directory.Build.props` com analyzers e `Directory.Packages.props` com versões centralizadas.
3. `frontend/` com Vite + React + TS strict, Biome, TanStack Query, Zod e React Hook Form.
4. `.env.example`, `.gitignore` e `.dockerignore` atualizados.
5. Hook de PostToolUse em `.claude/settings.json` + `.claude/hooks/verificar-edicao.sh`: `dotnet build` para `*.cs`; `biome check` + `tsc --noEmit` para `*.ts(x)`.
6. Workflow `.github/workflows/ci.yml` com build + testes do backend e build + lint do frontend.
7. Configuração no GitHub: desativar squash e rebase merge (`gh api -X PATCH repos/... -F allow_squash_merge=false -F allow_rebase_merge=false`).

#### Fase 1 — Banco de dados (≈ 1 h)

1. `database/01-criar-schema.sql`: tabelas Clientes, Produtos, Pedidos e ItensPedido, com PK, FK, CHECKs, UNIQUE (PedidoId, ProdutoId) e os quatro índices da spec, todos idempotentes (`IF NOT EXISTS`).
2. `database/02-criar-login-leitura.sql`: login `mcp_leitura` com `db_datareader` + `SHOWPLAN`.
3. `database/03-carga-exemplo.sql`:
   - Dados legíveis: cerca de 10 Clientes (dois homônimos) e cerca de 15 Produtos (um com Estoque baixo).
   - Volume determinístico: cerca de 50 mil Pedidos gerados a partir de uma tabela de números (`ROW_NUMBER` sobre `sys.all_objects` cruzado), com Cliente, data e Itens derivados por aritmética modular.
4. `database/debug/top-clientes-original.sql` e `top-clientes-corrigida.sql`.
5. `docker-compose.yml` com:
   - `db` (mssql/server:2022-latest, com healthcheck via sqlcmd).
   - `db-init` (mssql-tools, roda os scripts em ordem, `restart: "no"`).
6. `.mcp.json` com o DBHub apontando para `localhost:1433` com `mcp_leitura`. Validar no Claude: listar tabelas e rodar `SET SHOWPLAN_XML ON` sobre a query original.

#### Fase 2 — Infraestrutura da API (≈ 1 h)

Em `Joya.Pedidos.Api/Infraestrutura/`:
- `FabricaDeConexao` (`SqlConnection` a partir de `ConnectionStrings:Pedidos`, injetada explicitamente).
- `ProblemDetails` + `ManipuladorDeExcecoes` (500 genérico, sem stack trace).
- Rate limiting:
  - Política global de 100/min por IP.
  - Política `criacao-pedido` de 20/min.
- `ForwardedHeaders` com proxies conhecidos.
- Limite de corpo de 64 KB.
- Headers de segurança (`X-Content-Type-Options`, `Content-Security-Policy` restritiva, `Referrer-Policy`).
- `AddValidation()` nativo do .NET 10.
- OpenAPI (`AddOpenApi`).
- `GET /health` com check de banco.

Em `Program.cs`: só composição, em que cada slice expõe `MapXxx(this IEndpointRouteBuilder)`.

Teste de integração base: `ApiFactory` (`WebApplicationFactory<Program>` + `MsSqlContainer`, que aplica `database/01` e `03` uma vez por coleção) + teste do `/health`.

#### Fase 3 — Endpoints de apoio (≈ 30 min)

`Features/Clientes/ListarClientes/` e `Features/Produtos/ListarProdutos/`: endpoint + handler + query Dapper. Testes de integração simples.

#### Fase 4 — Criar Pedido (≈ 2 h)

`Features/Pedidos/CriarPedido/`:
- `CriarPedidoRequest` / `CriarPedidoResponse`: records com DataAnnotations para a forma (RN02–RN03).
- `RegrasDoPedido.Avaliar(request, clienteEncontrado, produtosEncontrados)`: função pura que devolve `ResultadoDaAvaliacao` com violações (RN01, RN04–RN07, RN10, RN11) ou o `PedidoMontado`.
- `CriarPedidoHandler`, em uma transação:
  1. Carrega o Cliente e os Produtos (`WHERE Id IN @ids`).
  2. Chama a regra.
  3. Faz `INSERT` em Pedidos e ItensPedido.
  4. Executa `UPDATE Produtos SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q` por Item; zero linhas afetadas gera rollback e `422`.
- `CriarPedidoEndpoint`: `POST /pedidos` com a política `criacao-pedido`.

Testes:
- Unitários da regra: todos os casos de CA02–CA09 que não precisam de banco.
- Integração: CA01–CA09, incluindo a concorrência com `Task.WhenAll` sobre um Produto de Estoque 1.

Depois do primeiro slice verde, extrair a skill `.claude/skills/criar-slice-backend/SKILL.md` a partir do código real.

#### Fase 5 — Listar Pedidos e Top Clientes (≈ 1 h 30)

- `Features/Pedidos/ListarPedidos/`: filtros opcionais montados com SQL parametrizado e cláusulas condicionais fixas (`(@clienteId IS NULL OR p.ClienteId = @clienteId)`), período semiaberto, `OFFSET/FETCH` e `COUNT(*) OVER()` ou query de contagem separada. Testes de integração CA10–CA13.
- `Features/Relatorios/TopClientes/`: mesma lógica da query corrigida, parametrizada. Testes de integração CA14–CA16.
- Ambos feitos seguindo a skill `criar-slice-backend`.

#### Fase 6 — Debug da query, Parte 4 (≈ 45 min)

Via MCP:
1. Plano de execução e `SET STATISTICS IO, TIME ON` da query original contra a carga.
2. Aplicar a correção e repetir a medição.
3. Registrar no README: os erros encontrados, as leituras lógicas e o tempo antes e depois, e o scan virando seek.

Extrair a skill `criar-script-sql`.

#### Fase 7 — Frontend (≈ 2 h)

`frontend/src/features/`:
- `pedidos/api/` (schemas Zod + funções `fetch` em `/api/...`).
- `pedidos/ListaDePedidos`, `pedidos/FiltrosDePedidos` e `pedidos/ModalCriarPedido`.
- `relatorios/TopClientes`.
- `clientes/api` e `produtos/api`.

`App.tsx` traz as abas e o `QueryClientProvider`.

Após criar um Pedido, invalidar `['pedidos']`, `['top-clientes']` e `['produtos']`. O botão de enviar fica desabilitado com `isPending`. Formatação BRL fica em `compartilhado/formatarMoeda.ts`.

Em produção, `Dockerfile` multi-stage (node → nginx), com `nginx.conf` repassando `/api/` → `http://api:8080/` e servindo o SPA.

#### Fase 8 — Docker completo (≈ 45 min)

- `backend/Dockerfile` multi-stage (sdk:10.0 → aspnet:10.0, usuário não-root).
- Serviços `api` (depende de `db-init: service_completed_successfully`, healthcheck em `/health`) e `web` no compose.
- Verificar `docker compose down -v && docker compose up` do zero.

#### Fase 9 — E2E e CI completa (≈ 1 h)

- `frontend/e2e/` com Playwright: criar Pedido e vê-lo na listagem; Top Clientes num Período fixo da carga.
- Job de CI que roda `docker compose up -d --wait` + Playwright.

#### Fase 10 — README, spec/plan e deploy (≈ 1 h 30)

1. README:
   - Como rodar com Docker e como rodar os testes.
   - Decisões técnicas (links para os ADRs).
   - Como usei o Claude e o que precisei corrigir.
   - A Parte 4 e o MCP.
2. Deploy:
   - Azure SQL Free com os scripts aplicados.
   - Render com a imagem da API e variáveis de ambiente.
   - Vercel com `vercel.json` (rewrite de `/api` para o Render).
   - Link no README.
3. Vídeo.

### Arquivos críticos

- `backend/Joya.Pedidos.Api/Program.cs` e `Infraestrutura/*`
- `backend/Joya.Pedidos.Api/Features/Pedidos/CriarPedido/RegrasDoPedido.cs` (o módulo profundo)
- `backend/Joya.Pedidos.Tests.Integration/ApiFactory.cs`
- `database/01-criar-schema.sql`, `03-carga-exemplo.sql`, `database/debug/*`
- `docker-compose.yml`, `backend/Dockerfile`, `frontend/Dockerfile`, `frontend/nginx.conf`
- `.claude/settings.json` (hook), `.mcp.json`, `.claude/skills/criar-slice-backend/`, `.claude/skills/criar-script-sql/`
- `.github/workflows/ci.yml`

### Verificação

- `dotnet test backend/Joya.Pedidos.Tests.Unit`: verde em segundos, sem Docker.
- `dotnet test backend/Joya.Pedidos.Tests.Integration`: verde com Docker rodando, cobrindo CA01–CA19.
- `docker compose down -v && docker compose up`: `http://localhost:8080` abre a página, cria um Pedido, a listagem e o Top Clientes atualizam, e o Estoque cai.
- `npx playwright test` contra o compose: dois cenários verdes.
- Pelo MCP: o plano de execução da query corrigida mostra `Index Seek` em `Pedidos(DataPedido)` e menos leituras lógicas que a original.
- GitHub Actions verde no push.
- Conferir os CA01–CA27 de `docs/spec.md` um a um antes da entrega.

---

## Parte 2 — O que ajustei ou discordei e por quê

O plan mode acertou o *conteúdo* técnico: respeitou os ADRs (Dapper, Minimal API sem mediador, Testcontainers, proxy de mesma origem), apontou o módulo profundo (`RegrasDoPedido`) e terminou com uma verificação por critério de aceite. As discordâncias são quase todas de **ordem e de forma de trabalho**, que é onde um plano gerado tende a errar.

### 1. O plano é horizontal (por camada); a execução é vertical (por funcionalidade)

O plano constrói "de baixo para cima": fundação → banco → infraestrutura da API → endpoints → frontend (Fase 7) → Docker da API e do frontend (Fase 8) → E2E (Fase 9). Até a Fase 7 nada é utilizável de ponta a ponta. Se o prazo apertar, o que fica de fora é justamente o frontend e o `docker compose up` completo, os dois obrigatórios.

**Ajuste:** os tickets seguem *tracer bullets*. O primeiro atravessa todas as camadas com o mínimo possível (schema → API → frontend → compose → CI) e cada ticket seguinte entrega uma funcionalidade inteira, do banco à tela. Assim, a qualquer momento, o que está pronto sobe e funciona.

### 2. Teste depois do código, não antes

Em cada fase o plano lista "Testes:" *depois* da implementação. Isso contradiz o fluxo do repositório (`/tdd`: red → green → refactor).

**Ajuste:** cada ticket começa pelo teste que falha, no seam combinado na spec: HTTP da API, função pura de regras ou navegador.

### 3. Docker, E2E e CI chegam tarde demais

O compose com API e frontend só fecha na Fase 8, e o Playwright e o job de CI com compose só na Fase 9. Os 5% de "sobe com um comando, sem ajustes manuais" ficam sem verificação contínua até o fim.

**Ajuste:** o compose completo e a CI entram no primeiro tracer bullet, e o E2E nasce com a primeira funcionalidade visível (criar Pedido e vê-lo na listagem).

### 4. Inconsistência com a spec no status do Produto repetido (RN04)

O plano coloca a regra de Produto repetido (RN04) dentro de `RegrasDoPedido`, junto com as violações que geram `422`. A spec e o ADR 0008 tratam RN04 como erro de forma (`400`): ele não depende do banco. O plano também não diz como a função pura separa erros de forma de erros de negócio.

**Ajuste:** a função pura classifica cada violação (forma ou negócio). O endpoint traduz forma para `400` e negócio para `422`, e um teste fixa essa tradução.

### 5. CSP no lugar errado

O plano põe `Content-Security-Policy` na API, que só devolve JSON. A CSP protege o documento HTML, ou seja, quem serve o SPA.

**Ajuste:** a CSP vai para o `nginx.conf` do frontend (e para os headers da Vercel). A API mantém `X-Content-Type-Options`, `Referrer-Policy` e uma CSP mínima `default-src 'none'`, como defesa em profundidade.

### 6. Granularidade de commits

"Toda fase termina com um commit" gera cerca de 11 commits grandes. O histórico é avaliado (10%) e o enunciado pede commits pequenos e frequentes.

**Ajuste:** commits a cada passo verde do TDD (teste, implementação, refatoração), uma branch e um PR por ticket, com merge commit.

### 7. Estimativa acima do tempo sugerido

O plano soma cerca de 12 h 45, contra as 6 a 8 h sugeridas pelo enunciado. Não corto escopo obrigatório, mas a ordem vertical protege o essencial. Deploy e vídeo ficam explicitamente como as últimas fatias, e o que não couber é registrado no README como dívida consciente.

### 8. Pontos que mantive do plan mode

- A extração das skills `criar-slice-backend` e `criar-script-sql` a partir do primeiro exemplo real.
- A verificação final percorrendo os CA01–CA27.
- A desativação de squash e rebase merge logo na fundação.
- `COUNT(*) OVER()` versus contagem separada fica para decidir no ticket de listagem, olhando o plano de execução.

> Comparação com o fatiamento do `/to-tickets`: a ser complementada com os números dos tickets quando o fatiamento for publicado.
