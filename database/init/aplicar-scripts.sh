#!/usr/bin/env bash
# Aplica, em ordem, todos os scripts de database/ no SQL Server do compose.
# Os scripts são idempotentes: rodar de novo (docker compose up) não quebra nada.
set -euo pipefail

for script in /database/[0-9][0-9]-*.sql; do
  echo "Aplicando $(basename "$script")..."
  /opt/mssql-tools18/bin/sqlcmd -S db -U sa -P "$MSSQL_SA_PASSWORD" -C -b \
    -v SENHA_API="$SENHA_API" SENHA_MCP="$SENHA_MCP" \
    -i "$script"
done

echo "Banco pronto."
