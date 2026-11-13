# MCP do SQL Server conecta com um login dedicado, somente leitura

O Claude acessa o SQL Server do `docker compose` pelo DBHub (configurado em `.mcp.json`) com um login próprio que tem só `db_datareader` e `SHOWPLAN`, criado pelo script de inicialização. A flag "read-only" de qualquer servidor MCP é só uma camada. A garantia de que o agente não altera nem apaga dados está na permissão do login, não na ferramenta. O `SHOWPLAN` existe para ler planos de execução na investigação da Parte 4. Se o DBHub bloquear `SET SHOWPLAN_XML`, troque o servidor MCP, não as permissões.
