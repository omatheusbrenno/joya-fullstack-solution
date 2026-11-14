-- Parte 4 do enunciado: a query original, sem nenhuma alteração.
-- Deveria devolver os Clientes que mais compraram em 2026, mas é lenta e devolve resultado errado.
-- Os erros e a evidência (plano de execução, STATISTICS IO/TIME) estão no README, seção "Parte 4".
-- Não é aplicada pelo db-init: fica fora do padrão NN-*.sql de propósito.
USE JoyaPedidos;
GO

SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
