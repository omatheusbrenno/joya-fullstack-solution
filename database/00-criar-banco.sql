-- Cria o banco da Gestão de Pedidos. Idempotente: pode rodar várias vezes.
-- No Azure SQL Database (deploy, ADR 0007) o banco JoyaPedidos é criado no portal e o script conecta direto
-- nele, então o DB_ID já o encontra. O CREATE DATABASE fica em EXEC porque o Azure só o aceita sozinho no lote.
IF DB_ID(N'JoyaPedidos') IS NULL
BEGIN
    EXEC (N'CREATE DATABASE JoyaPedidos;');
END
GO
