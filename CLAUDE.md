# Joya — Mini Gestão de Pedidos

Monorepo de teste técnico: cadastro e consulta de Pedidos de Clientes, de ponta a ponta (SQL Server + API .NET + SPA TypeScript), desenvolvido com Claude Code.

## Mapa do repositório

| Pasta | O que é | Contexto próprio |
| --- | --- | --- |
| `backend/` | API .NET (regras de negócio, persistência) | `backend/CLAUDE.md` |
| `frontend/` | SPA TypeScript (telas de pedidos) | `frontend/CLAUDE.md` |
| `database/` | scripts SQL idempotentes de criação e carga, aplicados em ordem pelo `db-init` do compose | — |
| `docs/adr/` | decisões de arquitetura (fonte histórica do "porquê") | — |
| `docs/agents/` | configuração das skills (tracker, labels, domínio) | — |
| `.claude/` | skills, rules, hooks e settings versionados | — |

Cada app tem o próprio `CLAUDE.md`, carregado quando você trabalha dentro dela. Este arquivo vale para o repo inteiro: não repita aqui o que é de uma app só.

## Regras do repositório

- **Linguagem ubíqua**: use os termos do `GLOSSARY.md` em código, rotas, testes, commits e issues. Termo novo vai para o glossário via `/domain-modeling`, não é inventado no meio do código.
- **Nomes**: termos de domínio em PT-BR (`Pedido`, `ItemPedido`, rotas `/pedidos`, JSON `clienteId`); sufixos técnicos em inglês (`CriarPedidoHandler`, `ListarPedidosEndpoint`).
- **Decisões viram ADR** em `docs/adr/`. Não contradiga um ADR em silêncio: aponte o conflito.
- **PRDs e tickets ficam no GitHub Issues**, não em arquivos do repo. Exceção: `docs/spec.md` e `docs/plan.md` são entregáveis avaliados e ficam versionados.
- **Segurança (OWASP Top 10)** é requisito, não extra. As regras por app estão em `.claude/rules/` e carregam sozinhas pelo caminho do arquivo editado.
- **Não planeje nem implemente o teste técnico sem pedido explícito.** Cada etapa (grilling, spec, plano, tickets, código) começa quando o humano pede.

## Git e histórico

- O histórico é avaliado: **commits pequenos, frequentes, Conventional Commits em PT-BR** (`feat(pedidos): valida estoque ao criar pedido`).
- **Proibido reescrever histórico**: nada de squash, rebase, `commit --amend`, `push --force`, `reset --hard`. O hook `.claude/hooks/block-history-rewrite.sh` bloqueia esses comandos.
- Faça commit só quando o humano pedir.

## Fluxo de trabalho com as skills (mattpocock/skills)

1. `/grill-with-docs`: alinhar a feature, consolidando o `GLOSSARY.md` e os ADRs.
2. `/to-spec`: PRD publicado como issue no GitHub.
3. `/to-tickets`: fatiar em issues verticais (tracer bullets) com bloqueios.
4. `/tdd` ou `/implement`: red → green → refactor, um ticket por vez.
5. `/code-review` e `/pr`: revisar e abrir o PR.
6. `/diagnosing-bugs` para bugs e `/improve-codebase-architecture` para entropia.

Use `/ask-matt` quando não souber qual skill usar.

## Comandos

- **Subir tudo** (banco + scripts + API + frontend): `docker compose up --build`. A aplicação abre em http://localhost:8080; a API fica atrás do nginx em `/api`. Sem `.env`, valem as senhas de desenvolvimento do `.env.example`.
- **Esperar ficar pronto** (scripts, CI): `docker compose up --build -d --wait`.
- **Testes do backend** (de `backend/`): `dotnet test --project Joya.Pedidos.Tests.Unit` e `dotnet test --project Joya.Pedidos.Tests.Integration` (este precisa de Docker).
- **Verificação do frontend** (de `frontend/`): `npm run lint && npm run typecheck && npm run build`.
- **E2E** (de `frontend/`, com o compose no ar): `npx playwright test`.
- **Zerar o banco**: `docker compose down -v` (apaga o volume; peça ao humano).
- **CI**: `.github/workflows/ci.yml` roda backend, frontend e compose em cada push e PR.

Os comandos de build, teste e lint de cada app ficam no `CLAUDE.md` dela. O hook `.claude/hooks/verificar-edicao.sh` já roda build e lint depois de cada edição; os testes você roda no fim de cada passo do TDD.

## Agent skills

### Issue tracker

Issues e PRDs no GitHub Issues (`omatheusbrenno/joya-fullstack-solution`) via `gh` CLI, com o board Kanban [#3](https://github.com/users/omatheusbrenno/projects/3) no GitHub Projects. Ver `docs/agents/issue-tracker.md`.

### Triage labels

Vocabulário padrão: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. Ver `docs/agents/triage-labels.md`.

### Domain docs

Single-context: um `GLOSSARY.md` e um `docs/adr/` na raiz, compartilhados por backend e frontend. Ver `docs/agents/domain.md`.
