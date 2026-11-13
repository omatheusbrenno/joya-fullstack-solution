# Joya — Mini Gestão de Pedidos

Teste técnico Full Stack (.NET + TypeScript, AI First): cadastro e consulta de Pedidos de Clientes, de ponta a ponta, desenvolvido com Claude Code.

> Em construção. Este README vai documentar como rodar com Docker, como rodar os testes, as decisões técnicas, o uso do Claude e o debug da query.

## Configuração AI First (já disponível)

- `CLAUDE.md` na raiz, mais `backend/CLAUDE.md` e `frontend/CLAUDE.md`, com o contexto de cada app.
- `.claude/skills/`: skills de engenharia de [mattpocock/skills](https://github.com/mattpocock/skills), instaladas com `npx skills add`. Versões fixadas em `skills-lock.json`; para atualizar, `npx skills update`.
- `.claude/rules/`: regras de segurança (OWASP Top 10:2025), carregadas por caminho.
- `.claude/hooks/`: hook que bloqueia reescrita do histórico git.
- `docs/agents/`: issue tracker (GitHub Issues + Projects), rótulos de triagem e layout dos docs de domínio.
