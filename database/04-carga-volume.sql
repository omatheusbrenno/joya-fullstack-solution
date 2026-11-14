-- Volume determinístico: 50 mil Pedidos entre 2025-01-01 e 2026-10-31, para dar números reproduzíveis
-- aos testes, ao E2E e à investigação de desempenho do Top Clientes.
-- Determinístico: tudo deriva do número da linha (n) por aritmética modular, sem NEWID() nem RAND().
-- Dois bancos novos com a carga têm exatamente os mesmos dados.
-- Idempotente: ids fixos, e cada linha só entra se ainda não existir.
-- Depende de 03-carga-exemplo.sql (Clientes 1 a 8 e Produtos 1 a 15).
--
-- Faixas de id reservadas: Pedidos 100001 a 150000 e Itens 100001 a 250000. Assim o volume nunca colide
-- com Pedidos criados pela API num banco que já existia antes deste script; os novos seguem depois de 150000.
--
-- Distribuição:
-- - Cliente: n % 37 com pesos fixos. Os homônimos "Ana Souza" (1 e 4) pesam mais e lideram o Top Clientes.
--   Os Clientes 9 e 10 ficam de fora, para continuarem sem Pedidos em 2026.
-- - Data do Pedido (UTC): 2025-01-01 + (n * 7919) % (669 dias em segundos). 7919 é primo e espalha as datas.
-- - Itens: 1 + n % 3 Itens, Produto 1 + (n + 5j) % 15 (distintos no Pedido), quantidade 1 + (n + j) % 4,
--   Preço Unitário igual ao preço do Produto.
USE JoyaPedidos;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- Tabela de números 1..50000 a partir do produto cartesiano de uma visão do sistema (só a contagem importa).
SELECT TOP (50000) CAST(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS INT) AS N
INTO #Numeros
FROM sys.all_objects AS a
CROSS JOIN sys.all_objects AS b;

SET IDENTITY_INSERT dbo.Pedidos ON;
INSERT INTO dbo.Pedidos (Id, ClienteId, DataPedido)
SELECT
    100000 + n.N,
    CASE
        WHEN n.N % 37 < 8 THEN 1
        WHEN n.N % 37 < 15 THEN 4
        WHEN n.N % 37 < 21 THEN 7
        WHEN n.N % 37 < 26 THEN 2
        WHEN n.N % 37 < 30 THEN 3
        WHEN n.N % 37 < 33 THEN 5
        WHEN n.N % 37 < 35 THEN 6
        ELSE 8
    END,
    DATEADD(SECOND, CAST((CAST(n.N AS BIGINT) * 7919) % (669 * 86400) AS INT), CAST('2025-01-01T00:00:00' AS DATETIME2(0)))
FROM #Numeros AS n
WHERE NOT EXISTS (SELECT 1 FROM dbo.Pedidos AS p WHERE p.Id = 100000 + n.N);
SET IDENTITY_INSERT dbo.Pedidos OFF;

SET IDENTITY_INSERT dbo.ItensPedido ON;
INSERT INTO dbo.ItensPedido (Id, PedidoId, ProdutoId, Quantidade, PrecoUnitario)
SELECT
    100000 + (n.N - 1) * 3 + j.J + 1,
    100000 + n.N,
    pr.Id,
    1 + (n.N + j.J) % 4,
    pr.Preco
FROM #Numeros AS n
INNER JOIN (VALUES (0), (1), (2)) AS j (J) ON j.J <= n.N % 3
INNER JOIN dbo.Produtos AS pr ON pr.Id = 1 + (n.N + 5 * j.J) % 15
WHERE NOT EXISTS (SELECT 1 FROM dbo.ItensPedido AS i WHERE i.Id = 100000 + (n.N - 1) * 3 + j.J + 1);
SET IDENTITY_INSERT dbo.ItensPedido OFF;

DROP TABLE #Numeros;

COMMIT TRANSACTION;
GO
