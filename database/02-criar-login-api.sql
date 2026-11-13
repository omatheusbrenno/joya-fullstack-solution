-- Login da API com o mínimo de privilégio: lê e escreve dados, não altera schema (OWASP A01/A02).
-- A senha chega pela variável do sqlcmd SENHA_API (vem do .env, nunca versionada).
-- Idempotente: se o login já existe, só realinha a senha.
USE master;
GO

IF SUSER_ID(N'joya_api') IS NULL
    CREATE LOGIN joya_api WITH PASSWORD = N'$(SENHA_API)', CHECK_POLICY = ON;
ELSE
    ALTER LOGIN joya_api WITH PASSWORD = N'$(SENHA_API)';
GO

USE JoyaPedidos;
GO

IF USER_ID(N'joya_api') IS NULL
    CREATE USER joya_api FOR LOGIN joya_api;
GO

ALTER ROLE db_datareader ADD MEMBER joya_api;
ALTER ROLE db_datawriter ADD MEMBER joya_api;
GO
