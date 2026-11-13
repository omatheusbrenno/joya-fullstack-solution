# Backend — API .NET

API de Gestão de Pedidos sobre SQL Server. Este arquivo vale para tudo em `backend/`. As regras do repo inteiro estão no `CLAUDE.md` da raiz.

## Restrições fixas (vêm do enunciado, não se discutem)

- .NET 8 ou superior, banco SQL Server.
- Endpoints obrigatórios: `POST /pedidos`, `GET /pedidos` (filtros por cliente e período, com paginação), `GET /relatorios/top-clientes`.
- Testes unitários das regras e validações do pedido, e testes de integração contra um SQL Server real (Testcontainers ou o do Docker Compose).
- Precisa subir pelo `docker compose up` da raiz.

## Arquitetura (a decidir, registrar em ADR)

Direção desejada: **Vertical Slice Architecture**, organizada por feature (`Features/Pedidos/CriarPedido/...`) e não por camada técnica, sem abrir mão do SOLID. Ainda **não está decidido**: biblioteca de mediação (ou nenhuma), acesso a dados (EF Core ou Dapper), validação, versão do .NET e layout dos projetos de teste. Decida via `/grill-with-docs` e grave o ADR antes de codar.

Princípios que já valem:

- Arquivos pequenos e coesos, com responsabilidade única (alvo abaixo de ~200 linhas).
- Dependências explícitas por construtor. Nada de service locator nem globais.
- Regra de negócio em funções puras e testáveis, separada de I/O (HTTP, banco).
- Contratos explícitos na borda (request/response tipados e validados) e OpenAPI gerado.
- Nomes semânticos, tirados do `GLOSSARY.md`.

Quando a arquitetura estiver decidida, cada artefato recorrente (endpoint, query, command, migration/script) ganha uma skill própria em `.claude/skills/`, para que as convenções não se acumulem neste arquivo.

## Comandos

A preencher quando a solution existir (build, testes unitários, testes de integração, formatação).
