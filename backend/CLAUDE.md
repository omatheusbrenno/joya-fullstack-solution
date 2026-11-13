# Backend — API .NET

API de Gestão de Pedidos sobre SQL Server. Este arquivo vale para tudo em `backend/`. As regras do repo inteiro estão no `CLAUDE.md` da raiz.

## Restrições fixas (vêm do enunciado, não se discutem)

- .NET 8 ou superior, banco SQL Server.
- Endpoints obrigatórios: `POST /pedidos`, `GET /pedidos` (filtros por cliente e período, com paginação), `GET /relatorios/top-clientes`.
- Testes unitários das regras e validações do pedido, e testes de integração contra um SQL Server real (Testcontainers ou o do Docker Compose).
- Precisa subir pelo `docker compose up` da raiz.

## Arquitetura (decidida, ver `docs/adr/`)

- **.NET 10** (ADR 0001).
- **Vertical Slices com Minimal API**: cada caso de uso em `Features/<Feature>/<CasoDeUso>/`, com endpoint → handler concreto injetado por construtor, sem MediatR (ADR 0002).
- **Layout**: `Joya.Pedidos.Api` (slices em `Features/`, compartilhado mínimo em `Infraestrutura/`), `Joya.Pedidos.Tests.Unit` e `Joya.Pedidos.Tests.Integration` (ADR 0002).
- **Dados**: Dapper com SQL parametrizado no slice; o schema vem dos scripts de `database/`, sem EF Core nem migrations (ADR 0003).
- **Regras de negócio** em função pura por slice, que devolve erros ou o resultado, sem exceção para fluxo esperado. A validação de forma usa o recurso nativo do .NET 10 (`AddValidation` + DataAnnotations), sem FluentValidation.
- **Erros** em ProblemDetails: `400` forma, `422` regra de negócio, `429` rate limit, `500` genérico (ADR 0008).
- **Sem autenticação**, com defesa na borda; CORS fechado porque o frontend chega pela mesma origem (ADRs 0004 e 0011).
- **Testes**: unitários sem I/O; integração com Testcontainers aplicando os scripts de `database/` (ADR 0006).
- **Lint**: `dotnet format` + analyzers com `TreatWarningsAsErrors`.

Princípios que já valem:

- Arquivos pequenos e coesos, com responsabilidade única (alvo abaixo de ~200 linhas).
- Dependências explícitas por construtor. Nada de service locator nem globais.
- Regra de negócio em funções puras e testáveis, separada de I/O (HTTP, banco).
- Contratos explícitos na borda (request/response tipados e validados) e OpenAPI gerado.
- Nomes semânticos, tirados do `GLOSSARY.md`.

Quando a arquitetura estiver decidida, cada artefato recorrente (endpoint, query, command, migration/script) ganha uma skill própria em `.claude/skills/`, para que as convenções não se acumulem neste arquivo.

## Comandos

A preencher quando a solution existir (build, testes unitários, testes de integração, formatação).
