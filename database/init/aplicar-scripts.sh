#!/usr/bin/env bash
# Aplica, em ordem, todos os scripts de database/ num SQL Server.
# Os scripts são idempotentes: rodar de novo (docker compose up) não quebra nada.
#
# Sem variáveis extras, vale o SQL Server do compose (servidor db, usuário sa, certificado autoassinado).
# No deploy (ADR 0007), o mesmo script aplica os mesmos arquivos no Azure SQL Database:
#   SERVIDOR_SQL=<servidor>.database.windows.net BANCO_SQL=JoyaPedidos USUARIO_SQL=<admin>
#   SENHA_ADMIN_SQL=<senha do admin> CONFIAR_CERTIFICADO=nao SENHA_API=<senha da API>
# No Azure é preciso conectar direto em JoyaPedidos: lá o USE não troca de banco.
set -euo pipefail

SERVIDOR_SQL="${SERVIDOR_SQL:-db}"
USUARIO_SQL="${USUARIO_SQL:-sa}"
BANCO_SQL="${BANCO_SQL:-master}"
CONFIAR_CERTIFICADO="${CONFIAR_CERTIFICADO:-sim}"
# O sqlcmd lê a senha daqui, e ela não aparece na lista de processos.
export SQLCMDPASSWORD="${SENHA_ADMIN_SQL:-${MSSQL_SA_PASSWORD:-}}"
# O login do MCP não existe no Azure (05-criar-login-mcp.sql), então lá a senha dele não é pedida.
SENHA_MCP="${SENHA_MCP:-nao-usada-fora-do-compose}"

opcoes=(-S "$SERVIDOR_SQL" -d "$BANCO_SQL" -U "$USUARIO_SQL" -b)
# O certificado do compose é autoassinado. O do Azure é validado (OWASP A04).
[[ "$CONFIAR_CERTIFICADO" == "sim" ]] && opcoes+=(-C)

for script in /database/[0-9][0-9]-*.sql; do
  echo "Aplicando $(basename "$script")..."
  /opt/mssql-tools18/bin/sqlcmd "${opcoes[@]}" \
    -v SENHA_API="$SENHA_API" SENHA_MCP="$SENHA_MCP" \
    -i "$script"
done

echo "Banco pronto."
