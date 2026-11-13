---
paths:
  - "backend/**"
  - "database/**"
  - "docker-compose*.yml"
---

# Segurança no backend (OWASP Top 10:2025)

Requisito de qualidade, não extra. Valem para todo código em `backend/`, nos scripts SQL e no compose. Na dúvida entre conveniência e segurança, escolha segurança e registre o trade-off em ADR.

- **A01 Controle de acesso**: negar por padrão. Nunca confie em ID vindo do cliente sem checar se o recurso existe e pode ser acessado. CORS com origem explícita, nunca `*` com credenciais.
- **A02 Configuração**: nenhum segredo no código, no compose versionado ou em `appsettings*.json`. Use variáveis de ambiente e `.env` (gitignored) com um `.env.example`. Swagger e páginas de erro detalhadas só em Development. Envie headers de segurança (HSTS, `X-Content-Type-Options`, CSP na borda).
- **A03 Cadeia de suprimentos**: pacotes NuGet com versão fixa e de fonte oficial. Rode `dotnet list package --vulnerable` antes de concluir uma tarefa que mexe em dependências. Imagens Docker com tag fixa, nunca `latest`.
- **A04 Criptografia**: TLS na conexão com o SQL Server (`Encrypt=True`). Nunca invente criptografia própria.
- **A05 Injeção**: SQL **sempre parametrizado**. Nada de concatenar nem interpolar entrada do usuário em SQL, nem mesmo em `ORDER BY` ou filtro: use uma whitelist. Compor o texto do SQL a partir de constantes de compilação (`const`) é permitido, porque nenhum valor de fora entra no texto. Valide toda entrada na borda (tipo, faixa, tamanho, paginação com teto).
- **A06 Design inseguro**: regras de negócio (estoque, cliente existente, pedido sem itens) validadas no servidor, nunca só no front. Baixa de estoque atômica (transação e controle de concorrência) para que dois pedidos não vendam o mesmo estoque.
- **A07 Autenticação**: se houver auth, use a do framework (nada caseiro) e não revele se o usuário existe.
- **A08 Integridade**: não desserialize tipos polimórficos vindos do cliente. Use DTOs explícitos e evite over-posting (nunca faça bind direto na entidade).
- **A09 Logs e alertas**: logue eventos relevantes com logging estruturado. **Nunca** logue segredo, connection string ou dado pessoal completo.
- **A10 Exceções**: um handler global que devolve `ProblemDetails` genérico. Stack trace e mensagem interna nunca chegam ao cliente. Falhe fechado: no erro, negue e desfaça a transação.

Antes de dar a tarefa por concluída, confira mentalmente esta lista sobre o diff.
