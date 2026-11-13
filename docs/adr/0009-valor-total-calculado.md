# Valor Total do Pedido é calculado a partir dos Itens, sem coluna armazenada

Não existe coluna `ValorTotal` em `Pedidos`. O valor é sempre `SUM(Quantidade * PrecoUnitario)` dos Itens de Pedido. Como o Pedido é imutável, a coluna desnormalizada seria segura, mas criaria uma segunda fonte de verdade sem ganho medido: a listagem paginada calcula o valor só para os Pedidos da página, coberta pelo índice `ItensPedido(PedidoId) INCLUDE (Quantidade, PrecoUnitario)`. Só reabra com um plano de execução mostrando que o cálculo virou gargalo.
