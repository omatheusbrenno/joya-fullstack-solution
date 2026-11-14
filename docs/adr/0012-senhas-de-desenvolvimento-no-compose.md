# Senhas de desenvolvimento como padrão no compose, sobrescritas por `.env`

A regra de segurança A02 proíbe segredo no compose versionado, mas o enunciado exige que a aplicação suba com um único `docker compose up`, sem ajuste manual. Exigir um `.env` seria um passo manual. Por isso, o `docker-compose.yml` traz senhas **só de desenvolvimento** como valor padrão (`${MSSQL_SA_PASSWORD:-...}`), que um `.env` local sobrescreve (ver `.env.example`).

O `.mcp.json` versionado segue a mesma regra pelo mesmo motivo: a senha do login somente leitura do MCP (ADR 0010) vem de `${SENHA_MCP:-...}`, com o mesmo padrão de desenvolvimento do compose.

Essas senhas não são segredos: protegem só um SQL Server descartável, publicado apenas em `127.0.0.1`. Nenhum ambiente fora da máquina local pode usá-las. O deploy (ADR 0007) lê as credenciais das variáveis de ambiente do provedor e valida o certificado do banco; o `TrustServerCertificate=True` também vale só para o compose local.

Não copie esses padrões para nenhuma configuração de deploy.
