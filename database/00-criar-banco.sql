-- Cria o banco da Gestão de Pedidos. Idempotente: pode rodar várias vezes.
IF DB_ID(N'JoyaPedidos') IS NULL
BEGIN
    CREATE DATABASE JoyaPedidos;
END
GO
