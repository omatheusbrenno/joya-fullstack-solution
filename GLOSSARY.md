# Gestão de Pedidos

Cadastro e consulta de Pedidos feitos por Clientes sobre um catálogo de Produtos com Estoque. Os termos de domínio são em PT-BR e valem igualmente para backend, frontend, banco, testes e issues.

## Catálogo

**Cliente**:
Pessoa ou empresa que faz Pedidos. É identificado pelo seu id, nunca pelo nome: dois Clientes podem ter o mesmo nome.
_Avoid_: Comprador, Usuário, Conta

**Produto**:
Item do catálogo que pode ser pedido, com um preço atual e um Estoque.
_Avoid_: Mercadoria, Artigo, SKU

**Estoque**:
Quantidade de unidades de um Produto disponíveis para novos Pedidos.
_Avoid_: Saldo, Inventário

## Pedidos

**Pedido**:
Compra registrada por um Cliente, composta de um ou mais Itens de Pedido. Nasce completo e não muda depois: não tem status, edição nem cancelamento.
_Avoid_: Ordem, Venda, Compra, Order

**Item de Pedido**:
Linha de um Pedido que liga um Produto a uma quantidade inteira maior que zero e ao Preço Unitário congelado. Um Pedido não repete o mesmo Produto em dois Itens.
_Avoid_: Linha, Produto do pedido, OrderItem

**Preço Unitário**:
Preço do Produto copiado para o Item de Pedido no momento da criação. Mudanças posteriores no preço do Produto não o alteram.
_Avoid_: Preço de venda, Valor unitário

**Valor Total do Pedido**:
Soma de quantidade × Preço Unitário de todos os Itens de um Pedido.
_Avoid_: Total, Montante, Subtotal

**Data do Pedido**:
Instante, em UTC, em que o sistema registrou o Pedido. Quem cria o Pedido não a escolhe.
_Avoid_: Data de criação, Data da compra

**Baixa de Estoque**:
Redução do Estoque de cada Produto pela quantidade pedida, feita junto com a criação do Pedido. Se algum Produto não tiver Estoque suficiente, o Pedido inteiro é recusado.
_Avoid_: Reserva, Débito de estoque

## Consultas

**Período**:
Intervalo de datas inclusivo (data inicial e data final), interpretado em UTC, usado para filtrar Pedidos pela Data do Pedido.
_Avoid_: Intervalo, Range, Janela

**Top Clientes**:
Os até cinco Clientes com maior soma de Valor Total dos Pedidos num Período, em ordem decrescente. Clientes sem Pedidos no Período não aparecem.
_Avoid_: Ranking, Melhores clientes, Maiores compradores
