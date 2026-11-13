# API: envelope de paginação genérico, em que `itens` são as linhas da página e não Itens de Pedido

Toda resposta paginada usa o mesmo envelope: `{ itens, pagina, tamanhoPagina, totalItens }`. Em `GET /pedidos`, `itens` são os Pedidos da página e `totalItens` é o total de Pedidos que atendem aos filtros. Já `quantidadeItens`, dentro de cada linha, conta os Itens de Pedido daquele Pedido. A colisão com o termo de domínio "Item de Pedido" é conhecida e aceita: o envelope fica igual para qualquer listagem futura e o contrato da spec (§3) já está fixado assim. Não renomeie `itens` para `pedidos` nem `totalItens` para `totalPedidos`: o frontend e os testes dependem desses nomes.

## Considered Options

- **Envelope com nome do domínio** (`{ pedidos, totalPedidos }`): elimina a ambiguidade, mas obriga cada listagem a ter um envelope próprio e contradiz o contrato já publicado na spec.
