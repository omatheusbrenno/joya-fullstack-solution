# Frontend — SPA TypeScript

Interface de Gestão de Pedidos que consome a API de `backend/`. Este arquivo vale para tudo em `frontend/`. As regras do repo inteiro estão no `CLAUDE.md` da raiz.

## Restrições fixas (vêm do enunciado, não se discutem)

- React ou Angular, com TypeScript.
- Tela de listagem de pedidos com filtros, e tela ou modal de criação de pedido.
- Precisa subir pelo `docker compose up` da raiz.
- Diferencial: testes end-to-end com Playwright (criar um pedido e vê-lo na listagem).

## Arquitetura (a decidir, registrar em ADR)

Ainda **não está decidido**: React ou Angular, bundler, biblioteca de dados/estado, validação de schema na borda e geração do cliente da API a partir do OpenAPI do backend. Decida via `/grill-with-docs` e grave o ADR antes de codar.

Princípios que já valem:

- TypeScript em modo `strict`, sem `any`.
- Organização por feature (`features/pedidos/...`), espelhando os slices do backend.
- Os dados que vêm da API são validados por schema na borda. O tipo nunca é presumido.
- Componentes pequenos, com a lógica de apresentação separada das chamadas HTTP.
- Linter e formatador opinativos, que o agente roda antes de dar algo por pronto.
- Termos de domínio exatamente como no `GLOSSARY.md` (Pedido, Cliente, Produto, ItemPedido).

## Comandos

A preencher quando o projeto existir (dev server, build, lint, typecheck, testes, Playwright).
