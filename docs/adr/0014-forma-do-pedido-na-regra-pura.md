# Backend: a forma do Pedido é avaliada na função pura, e a violação de forma prevalece sobre as de negócio

No corpo de `POST /pedidos`, as regras de forma (RN02 de 1 a 50 Itens, RN03 quantidade de 1 a 10.000, RN04 Produto repetido) ficam em `RegrasDoPedido`, ao lado das regras de negócio. Não ficam em DataAnnotations. Cada violação sai classificada como forma (`400`) ou negócio (`422`, ADR 0008), e o endpoint só traduz a classe em status. A borda (`AddValidation`) barra apenas o corpo que nem dá para avaliar: Cliente ausente ou Item nulo. Assim, todas as regras do Pedido são testadas sem I/O num único lugar, inclusive a classificação que o frontend usa para escolher a mensagem.

Se houver qualquer violação de forma, só as de forma são devolvidas, e as de negócio nem são avaliadas. A RN11 ("todas as violações de uma vez") vale dentro de cada classe: a resposta tem um status só, e conferir Estoque contra uma quantidade inválida não informa nada.

## Considered Options

- **Forma em DataAnnotations, negócio na função pura**: segue o padrão das query strings (`ListarPedidosRequest`), mas espalha as regras do Pedido em dois mecanismos. Além disso, RN04 (Produto repetido) não cabe num atributo.
- **Devolver forma e negócio juntos**: cumpre a RN11 ao pé da letra, mas obriga a escolher um único status para violações de classes diferentes e avalia regras de negócio sobre entrada inválida.
