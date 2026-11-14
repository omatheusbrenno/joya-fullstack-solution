-- Login do MCP do SQL Server (ADR 0010): o Claude lê dados e planos de execução, e não escreve nada.
-- A garantia está na permissão do login, não na flag "read-only" do servidor MCP (OWASP A01).
-- Só db_datareader + SHOWPLAN: nada de db_datawriter, DDL ou EXECUTE.
-- A senha chega pela variável do sqlcmd SENHA_MCP: do .env ou, sem ele, do padrão de desenvolvimento do compose (ADR 0012).
-- Idempotente: se o login já existe, só realinha a senha.
-- Depende de 00-criar-banco.sql (DEFAULT_DATABASE e USE JoyaPedidos).
--
-- O MCP só fala com o banco local do compose. No Azure SQL Database (deploy, ADR 0007; EngineEdition = 5) o
-- script não cria nada: um usuário a mais no banco público seria só mais uma senha para vazar.
-- O CREATE LOGIN fica em EXEC porque o Azure o recusa até num ramo que não executa (ver 02-criar-login-api.sql).
IF CAST(SERVERPROPERTY('EngineEdition') AS INT) = 5
    PRINT N'Azure SQL Database: o login do MCP é só do ambiente local (ADR 0010). Nada a fazer.';
ELSE IF SUSER_ID(N'joya_mcp') IS NULL
    EXEC (N'CREATE LOGIN joya_mcp WITH PASSWORD = N''$(SENHA_MCP)'', CHECK_POLICY = ON, DEFAULT_DATABASE = JoyaPedidos;');
ELSE
    EXEC (N'ALTER LOGIN joya_mcp WITH PASSWORD = N''$(SENHA_MCP)'';');
GO

USE JoyaPedidos;
GO

IF CAST(SERVERPROPERTY('EngineEdition') AS INT) <> 5
BEGIN
    IF USER_ID(N'joya_mcp') IS NULL
        EXEC (N'CREATE USER joya_mcp FOR LOGIN joya_mcp;');

    ALTER ROLE db_datareader ADD MEMBER joya_mcp;
    -- SHOWPLAN deixa ler o plano estimado (SET SHOWPLAN_XML) e o real (SET STATISTICS XML) da investigação da Parte 4.
    GRANT SHOWPLAN TO joya_mcp;
END
GO
