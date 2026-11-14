-- Parte 4 do enunciado: a query corrigida. É a mesma lógica do GET /relatorios/top-clientes
-- (ConsultarTopClientesHandler), com o Período fixo em 2026 em vez de parâmetros.
-- Os erros corrigidos e a evidência de antes e depois estão no README, seção "Parte 4".
-- Não é aplicada pelo db-init: fica fora do padrão NN-*.sql de propósito.
--
-- O que mudou em relação à original:
-- 1. INNER JOIN, explícito: o filtro de 2026 já descartava os Clientes sem Pedido, então o LEFT JOIN era falso.
-- 2. Período semiaberto [2026-01-01, 2027-01-01) sem função sobre DataPedido: vira seek em IX_Pedidos_DataPedido.
-- 3. Agrupa por ClienteId, não por Nome: os homônimos "Ana Souza" (1 e 4) são Clientes diferentes (RN13).
-- 4. ORDER BY decrescente, com desempate pelo id para a ordem ser estável.
-- 5. TOP (5).
-- Os índices que ela usa estão em 01-criar-schema.sql (IX_Pedidos_DataPedido e IX_ItensPedido_PedidoId).
USE JoyaPedidos;
GO

WITH ValorDoPedido AS (
    SELECT p.ClienteId, v.ValorTotal
    FROM dbo.Pedidos AS p
    CROSS APPLY (
        SELECT COALESCE(SUM(ip.Quantidade * ip.PrecoUnitario), 0) AS ValorTotal
        FROM dbo.ItensPedido AS ip
        WHERE ip.PedidoId = p.Id
    ) AS v
    -- Literais já em DATETIME2(0), o mesmo tipo do parâmetro do endpoint: sem conversão implícita no plano.
    WHERE p.DataPedido >= CAST('2026-01-01' AS DATETIME2(0))
      AND p.DataPedido < CAST('2027-01-01' AS DATETIME2(0))
),
PorCliente AS (
    SELECT ClienteId, COUNT(*) AS QuantidadePedidos, SUM(ValorTotal) AS ValorTotal
    FROM ValorDoPedido
    GROUP BY ClienteId
)
SELECT TOP (5) pc.ClienteId, c.Nome, pc.QuantidadePedidos, pc.ValorTotal
FROM PorCliente AS pc
INNER JOIN dbo.Clientes AS c ON c.Id = pc.ClienteId
ORDER BY pc.ValorTotal DESC, pc.ClienteId ASC;
