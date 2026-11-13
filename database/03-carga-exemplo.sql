-- Carga de exemplo legível: Clientes, Produtos e alguns Pedidos para a tela já mostrar dados ao subir.
-- Idempotente: ids fixos, e cada linha só entra se ainda não existir. Rodar de novo não duplica nada.
-- Destaques: dois Clientes homônimos ("Ana Souza", ids 1 e 4), Clientes sem Pedidos em 2026 (9 e 10)
-- e um Produto com Estoque baixo (15). As Datas do Pedido estão em UTC.
USE JoyaPedidos;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

SET IDENTITY_INSERT dbo.Clientes ON;
INSERT INTO dbo.Clientes (Id, Nome)
SELECT v.Id, v.Nome
FROM (VALUES
    (1, N'Ana Souza'),
    (2, N'Bruno Lima'),
    (3, N'Carla Mendes'),
    (4, N'Ana Souza'),
    (5, N'Diego Ferreira'),
    (6, N'Elisa Rocha'),
    (7, N'Carlos Lima'),
    (8, N'Fernanda Alves'),
    (9, N'Gustavo Pereira'),
    (10, N'Helena Martins')
) AS v (Id, Nome)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Clientes AS c WHERE c.Id = v.Id);
SET IDENTITY_INSERT dbo.Clientes OFF;

SET IDENTITY_INSERT dbo.Produtos ON;
INSERT INTO dbo.Produtos (Id, Nome, Preco, Estoque)
SELECT v.Id, v.Nome, v.Preco, v.Estoque
FROM (VALUES
    (1, N'Teclado mecânico', 150.00, 120),
    (2, N'Mouse sem fio', 79.90, 200),
    (3, N'Monitor 24 polegadas', 899.00, 40),
    (4, N'Headset USB', 199.90, 75),
    (5, N'Cadeira ergonômica', 1290.00, 15),
    (6, N'Mesa regulável', 1850.00, 10),
    (7, N'Notebook 14 polegadas', 4299.00, 25),
    (8, N'Hub USB-C', 129.90, 150),
    (9, N'SSD 1 TB', 449.90, 60),
    (10, N'Memória RAM 16 GB', 319.90, 80),
    (11, N'Mousepad grande', 49.90, 300),
    (12, N'Suporte para notebook', 89.90, 90),
    (13, N'Cabo HDMI 2 m', 34.90, 400),
    (14, N'Luminária de mesa', 119.90, 50),
    (15, N'Webcam Full HD', 279.90, 2)
) AS v (Id, Nome, Preco, Estoque)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Produtos AS p WHERE p.Id = v.Id);
SET IDENTITY_INSERT dbo.Produtos OFF;

SET IDENTITY_INSERT dbo.Pedidos ON;
INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido)
SELECT v.Id, v.ClienteId, v.DataPedido
FROM (VALUES
    (1, 9, CAST('2025-03-14T10:20:00' AS DATETIME2(0))),
    (2, 1, CAST('2025-08-02T15:45:00' AS DATETIME2(0))),
    (3, 4, CAST('2025-11-28T09:05:00' AS DATETIME2(0))),
    (4, 2, CAST('2025-12-31T23:59:59' AS DATETIME2(0))),
    (5, 1, CAST('2026-01-15T13:30:00' AS DATETIME2(0))),
    (6, 3, CAST('2026-02-03T08:10:00' AS DATETIME2(0))),
    (7, 4, CAST('2026-03-21T17:25:00' AS DATETIME2(0))),
    (8, 5, CAST('2026-05-09T11:00:00' AS DATETIME2(0))),
    (9, 7, CAST('2026-06-30T23:59:59' AS DATETIME2(0))),
    (10, 6, CAST('2026-08-18T14:40:00' AS DATETIME2(0))),
    (11, 8, CAST('2026-10-05T10:15:00' AS DATETIME2(0))),
    (12, 2, CAST('2026-11-10T16:50:00' AS DATETIME2(0)))
) AS v (Id, ClienteId, DataPedido)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pedidos AS p WHERE p.Id = v.Id);
SET IDENTITY_INSERT dbo.Pedidos OFF;

-- O Preço Unitário é o preço do Produto na data do Pedido: alguns diferem do preço atual de propósito.
SET IDENTITY_INSERT dbo.ItensPedido ON;
INSERT INTO dbo.ItensPedido (Id, PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT v.Id, v.PedidoId, v.ProdutoId, v.Quantidade, v.PrecoUnitario
FROM (VALUES
    (1, 1, 7, 1, 3999.00),
    (2, 1, 2, 1, 69.90),
    (3, 2, 1, 2, 140.00),
    (4, 3, 3, 2, 849.00),
    (5, 3, 13, 2, 29.90),
    (6, 4, 5, 1, 1190.00),
    (7, 5, 9, 1, 449.90),
    (8, 5, 10, 2, 319.90),
    (9, 5, 8, 1, 129.90),
    (10, 6, 4, 3, 199.90),
    (11, 7, 11, 5, 49.90),
    (12, 8, 6, 1, 1850.00),
    (13, 8, 14, 1, 119.90),
    (14, 9, 7, 2, 4299.00),
    (15, 9, 12, 2, 89.90),
    (16, 10, 15, 1, 279.90),
    (17, 11, 1, 1, 150.00),
    (18, 11, 2, 1, 79.90),
    (19, 11, 11, 1, 49.90),
    (20, 12, 3, 1, 899.00)
) AS v (Id, PedidoId, ProdutoId, Quantidade, PrecoUnitario)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ItensPedido AS i WHERE i.Id = v.Id);
SET IDENTITY_INSERT dbo.ItensPedido OFF;

COMMIT TRANSACTION;
GO
