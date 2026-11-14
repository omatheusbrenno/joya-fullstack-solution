-- Login da API com o mínimo de privilégio: lê e escreve dados, não altera schema (OWASP A01/A02).
-- A senha chega pela variável do sqlcmd SENHA_API (vem do .env, nunca versionada).
-- Idempotente: se o login (ou o usuário contido) já existe, só realinha a senha.
-- Depende de 00-criar-banco.sql.
--
-- Os mesmos scripts servem a dois motores:
-- - SQL Server (compose, testes): login no servidor + usuário no banco. CREATE LOGIN vale em qualquer banco,
--   então não há USE master.
-- - Azure SQL Database (deploy, ADR 0007; EngineEdition = 5): o USE não troca de banco e não há login no master
--   a partir do banco da aplicação. A API entra com um usuário contido, com senha no próprio banco.
-- O DDL específico de cada motor fica em EXEC: o Azure recusa CREATE LOGIN até num ramo que não executa.
IF CAST(SERVERPROPERTY('EngineEdition') AS INT) <> 5
BEGIN
    IF SUSER_ID(N'joya_api') IS NULL
        EXEC (N'CREATE LOGIN joya_api WITH PASSWORD = N''$(SENHA_API)'', CHECK_POLICY = ON;');
    ELSE
        EXEC (N'ALTER LOGIN joya_api WITH PASSWORD = N''$(SENHA_API)'';');
END
GO

USE JoyaPedidos;
GO

IF CAST(SERVERPROPERTY('EngineEdition') AS INT) = 5
BEGIN
    IF USER_ID(N'joya_api') IS NULL
        EXEC (N'CREATE USER joya_api WITH PASSWORD = N''$(SENHA_API)'';');
    ELSE
        EXEC (N'ALTER USER joya_api WITH PASSWORD = N''$(SENHA_API)'';');
END
ELSE IF USER_ID(N'joya_api') IS NULL
    EXEC (N'CREATE USER joya_api FOR LOGIN joya_api;');
GO

ALTER ROLE db_datareader ADD MEMBER joya_api;
ALTER ROLE db_datawriter ADD MEMBER joya_api;
GO
