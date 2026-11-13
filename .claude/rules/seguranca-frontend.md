---
paths:
  - "frontend/**"
---

# Segurança no frontend (OWASP Top 10:2025)

Valem para todo código em `frontend/`. O front **não é fronteira de segurança**: toda regra também é validada no backend.

- **XSS / A05 Injeção**: nunca renderize HTML cru (`dangerouslySetInnerHTML`, `[innerHTML]`, `bypassSecurityTrust*`) com dado da API ou do usuário. Deixe o framework escapar.
- **A02 Configuração**: nenhum segredo no bundle. Tudo que vai para o front é público. Nem a URL da API entra no build: o front só chama caminhos relativos `/api/...` (ADR 0011).
- **A03 Cadeia de suprimentos**: lockfile versionado e instalação com `npm ci`. Rode `npm audit --omit=dev` antes de concluir uma tarefa que mexe em dependências. Não adicione pacote para algo trivial.
- **A08 Integridade**: valide por schema toda resposta da API antes de usá-la. Dado inválido é erro tratado, nunca `as Tipo`.
- **A01 Acesso**: esconder botão não é controle de acesso. O backend decide.
- **A09/A10 Erros**: mostre mensagens genéricas ao usuário. Nada de stack trace, payload bruto ou `console.log` de dado sensível em produção.
- **Dados sensíveis**: não guarde token nem dado pessoal em `localStorage`.
