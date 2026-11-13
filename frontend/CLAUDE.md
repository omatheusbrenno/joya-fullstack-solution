# Frontend — SPA TypeScript

Interface de Gestão de Pedidos que consome a API de `backend/`. Este arquivo vale para tudo em `frontend/`. As regras do repo inteiro estão no `CLAUDE.md` da raiz.

## Restrições fixas (vêm do enunciado, não se discutem)

- React ou Angular, com TypeScript.
- Tela de listagem de pedidos com filtros, e tela ou modal de criação de pedido.
- Precisa subir pelo `docker compose up` da raiz.
- Diferencial: testes end-to-end com Playwright (criar um pedido e vê-lo na listagem).

## Arquitetura (decidida, ver `docs/adr/`)

- **React + Vite + TypeScript strict** (ADR 0005).
- **Uma página sem router**: abas "Pedidos" e "Top Clientes", mais o modal de criação de Pedido.
- **Dados**: TanStack Query para estado de servidor; schemas Zod escritos à mão em `features/<feature>/api/`, validando toda resposta da API. Formulário com React Hook Form + Zod.
- **API pela mesma origem**: só caminhos relativos `/api/...`, repassados pelo nginx (Docker) ou por um rewrite (Vercel). Nada de URL da API embutida no build (ADR 0011).
- **Estilo**: CSS Modules, sem biblioteca de componentes.
- **Qualidade**: Biome (lint + format) e `tsc --noEmit`; Vitest para funções puras; Playwright para os fluxos de criar Pedido e consultar Top Clientes.

Princípios que já valem:

- TypeScript em modo `strict`, sem `any`.
- Organização por feature (`features/pedidos/...`), espelhando os slices do backend.
- Os dados que vêm da API são validados por schema na borda. O tipo nunca é presumido.
- Componentes pequenos, com a lógica de apresentação separada das chamadas HTTP.
- Linter e formatador opinativos, que o agente roda antes de dar algo por pronto.
- Termos de domínio exatamente como no `GLOSSARY.md` (Pedido, Cliente, Produto, ItemPedido).

## Comandos

Rode a partir de `frontend/`, depois de `npm ci`:

- **Dev server**: `npm run dev`. O Vite repassa `/api` para `http://localhost:5080`; mude com `API_URL`.
- **Lint + formato**: `npm run lint` (corrigir: `npm run format`).
- **Tipos**: `npm run typecheck`
- **Build**: `npm run build`
- **Dependências**: `npm audit --omit=dev`

A imagem Docker serve o build pelo nginx (`nginx.conf`), que repassa `/api/` para o serviço `api`.
