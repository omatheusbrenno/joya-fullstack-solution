# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

This is a **single-context** repo: one domain (Gestão de Pedidos), one ubiquitous language, shared by `backend/` and `frontend/`. The monorepo split is technical (two apps), not a split of bounded contexts.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root: the ubiquitous language. Backend and frontend use the same terms.
- **`docs/adr/`** at the repo root: read ADRs that touch the area you're about to work in. ADRs for either app also live here; prefix the title with the app when it's app-scoped (e.g. `0004-backend-vertical-slices.md`).

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

```
/
├── GLOSSARY.md
├── docs/
│   ├── adr/
│   │   ├── 0001-....md
│   │   └── 0002-....md
│   └── agents/          ← this config (issue tracker, labels, domain)
├── backend/             ← .NET API (has its own CLAUDE.md)
└── frontend/            ← TypeScript SPA (has its own CLAUDE.md)
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name, a class, a route, a React/Angular component), use the term as defined in `GLOSSARY.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders), but worth reopening because…_
