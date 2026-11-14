-- Login do MCP do SQL Server (ADR 0010): o Claude lê dados e planos de execução, e não escreve nada.
-- A garantia está na permissão do login, não na flag "read-only" do servidor MCP (OWASP A01).
-- Só db_datareader + SHOWPLAN: nada de db_datawriter, DDL ou EXECUTE.
-- A senha chega pela variável do sqlcmd SENHA_MCP: do .env ou, sem ele, do padrão de desenvolvimento do compose (ADR 0012).
-- Idempotente: se o login já existe, só realinha a senha.
-- Depende de 00-criar-banco.sql (DEFAULT_DATABASE e USE JoyaPedidos).
USE master;
GO

IF SUSER_ID(N'joya_mcp') IS NULL
    CREATE LOGIN joya_mcp WITH PASSWORD = N'$(SENHA_MCP)', CHECK_POLICY = ON, DEFAULT_DATABASE = JoyaPedidos;
ELSE
    ALTER LOGIN joya_mcp WITH PASSWORD = N'$(SENHA_MCP)';
GO

USE JoyaPedidos;
GO

IF USER_ID(N'joya_mcp') IS NULL
    CREATE USER joya_mcp FOR LOGIN joya_mcp;
GO

ALTER ROLE db_datareader ADD MEMBER joya_mcp;
-- SHOWPLAN deixa ler o plano estimado (SET SHOWPLAN_XML) e o real (SET STATISTICS XML) da investigação da Parte 4.
GRANT SHOWPLAN TO joya_mcp;
GO
