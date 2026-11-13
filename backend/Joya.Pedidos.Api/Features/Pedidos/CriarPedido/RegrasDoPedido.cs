namespace Joya.Pedidos.Api.Features.Pedidos.CriarPedido;

/// <summary>
/// Regras do Pedido (RN01 a RN07, RN10, RN11) numa função pura: recebe o request e o que foi carregado
/// do banco e devolve o Pedido montado ou as violações. Sem I/O e sem exceção para fluxo esperado (ADR 0002).
/// </summary>
public static class RegrasDoPedido
{
    public const int MinimoDeItens = 1;
    public const int MaximoDeItens = 50;
    public const int QuantidadeMinima = 1;
    public const int QuantidadeMaxima = 10_000;

    public static AvaliacaoDoPedido Avaliar(
        IReadOnlyList<ItemSolicitado> itens,
        ClienteEncontrado? cliente,
        IReadOnlyCollection<ProdutoEncontrado> produtos)
    {
        var violacoesDeForma = VerificarForma(itens);
        if (violacoesDeForma.Count > 0)
        {
            return new PedidoRecusado(violacoesDeForma);
        }

        var produtosPorId = produtos.ToDictionary(produto => produto.Id);
        var violacoesDeNegocio = new List<Violacao>();
        if (cliente is null)
        {
            violacoesDeNegocio.Add(Negocio("clienteId", "Cliente não encontrado."));
        }

        var itensMontados = new List<ItemMontado>(itens.Count);
        for (var indice = 0; indice < itens.Count; indice++)
        {
            var item = itens[indice];
            if (!produtosPorId.TryGetValue(item.ProdutoId, out var produto))
            {
                violacoesDeNegocio.Add(Negocio(CampoDoItem(indice, "produtoId"), $"Produto {item.ProdutoId} não encontrado."));
                continue;
            }

            if (produto.Estoque < item.Quantidade)
            {
                violacoesDeNegocio.Add(Negocio(CampoDoItem(indice, "quantidade"), MensagemDeEstoqueInsuficiente(produto.Nome, produto.Estoque, item.Quantidade)));
            }

            // RN07: o Preço Unitário vem do preço atual do Produto, nunca do request.
            itensMontados.Add(new ItemMontado(produto.Id, produto.Nome, item.Quantidade, produto.Preco));
        }

        return cliente is null || violacoesDeNegocio.Count > 0
            ? new PedidoRecusado(violacoesDeNegocio)
            : new PedidoAceito(new PedidoMontado(cliente, itensMontados));
    }

    /// <summary>
    /// RN06 e RN09 no momento da Baixa de Estoque: os Itens cujo Produto não baixou perderam a corrida para
    /// outro Pedido entre a leitura e o UPDATE condicional. O Pedido inteiro é recusado, apontando esses Itens.
    /// </summary>
    public static IReadOnlyList<Violacao> EstoqueConsumidoNaBaixa(PedidoMontado pedido, IReadOnlySet<int> produtosBaixados) =>
        [.. pedido.Itens
            .Select((item, indice) => (item, indice))
            .Where(par => !produtosBaixados.Contains(par.item.ProdutoId))
            .Select(par => Negocio(
                CampoDoItem(par.indice, "quantidade"),
                $"Estoque insuficiente de {par.item.NomeProduto}: outro Pedido acabou de levar as unidades disponíveis."))];

    private static string MensagemDeEstoqueInsuficiente(string nomeDoProduto, int estoque, int quantidade) =>
        $"Estoque insuficiente de {nomeDoProduto}: há {estoque} e o Item pede {quantidade}.";

    /// <summary>
    /// RN02, RN03 e RN04: dependem só do request. Com qualquer uma delas, as regras de negócio não são avaliadas:
    /// conferir Estoque contra uma quantidade inválida não diz nada, e o status é um só (400).
    /// </summary>
    private static List<Violacao> VerificarForma(IReadOnlyList<ItemSolicitado> itens)
    {
        if (itens.Count is < MinimoDeItens or > MaximoDeItens)
        {
            return [Forma("itens", $"O Pedido deve ter de {MinimoDeItens} a {MaximoDeItens} Itens.")];
        }

        var violacoes = new List<Violacao>();
        var primeiroItemDoProduto = new Dictionary<int, int>();
        for (var indice = 0; indice < itens.Count; indice++)
        {
            var item = itens[indice];
            if (item.Quantidade is < QuantidadeMinima or > QuantidadeMaxima)
            {
                violacoes.Add(Forma(CampoDoItem(indice, "quantidade"), $"A quantidade deve estar entre {QuantidadeMinima} e {QuantidadeMaxima}."));
            }

            // RN04: o primeiro Item de cada Produto vale; cada repetição seguinte é uma violação.
            if (!primeiroItemDoProduto.TryAdd(item.ProdutoId, indice))
            {
                violacoes.Add(Forma(CampoDoItem(indice, "produtoId"), $"O Produto {item.ProdutoId} já aparece no {NomeDoItem(primeiroItemDoProduto[item.ProdutoId])}."));
            }
        }

        return violacoes;
    }

    /// <summary>Caminho do campo no JSON, com índice a partir de 0 (ex.: <c>itens[1].quantidade</c>).</summary>
    private static string CampoDoItem(int indice, string propriedade) => $"itens[{indice}].{propriedade}";

    /// <summary>Nome do Item para quem lê a mensagem, contando a partir de 1.</summary>
    private static string NomeDoItem(int indice) => $"Item {indice + 1}";

    private static Violacao Forma(string campo, string mensagem) => new(TipoDeViolacao.Forma, campo, mensagem);

    private static Violacao Negocio(string campo, string mensagem) => new(TipoDeViolacao.Negocio, campo, mensagem);
}
