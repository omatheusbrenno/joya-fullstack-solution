import { useMutation, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { enviarJsonParaApi } from "../../../compartilhado/buscarDaApi";
import { CHAVE_DOS_PRODUTOS } from "../../produtos/api/produtos";
import { CHAVE_DOS_PEDIDOS } from "./pedidos";

/** Prefixo das consultas do Top Clientes. A aba do ticket #5 deve usar esta chave para ser atualizada aqui. */
export const CHAVE_DO_TOP_CLIENTES = ["top-clientes"] as const;

/** Corpo de POST /pedidos. Não há campo de preço: o Preço Unitário vem do Produto (RN07). */
export type NovoPedido = {
  clienteId: number;
  itens: { produtoId: number; quantidade: number }[];
};

export const pedidoCriadoSchema = z.object({
  id: z.number().int().positive(),
  clienteId: z.number().int().positive(),
  nomeCliente: z.string(),
  dataPedido: z.iso.datetime(),
  itens: z.array(
    z.object({
      produtoId: z.number().int().positive(),
      nomeProduto: z.string(),
      quantidade: z.number().int().positive(),
      precoUnitario: z.number().nonnegative(),
    }),
  ),
  valorTotal: z.number().nonnegative(),
});
export type PedidoCriado = z.infer<typeof pedidoCriadoSchema>;

/** Cria o Pedido e atualiza o que ele muda: a listagem, o Top Clientes e o Estoque dos Produtos (CA22). */
export function useCriarPedido() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (pedido: NovoPedido) => enviarJsonParaApi("/pedidos", pedido, pedidoCriadoSchema),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: CHAVE_DOS_PEDIDOS }),
        queryClient.invalidateQueries({ queryKey: CHAVE_DO_TOP_CLIENTES }),
        queryClient.invalidateQueries({ queryKey: CHAVE_DOS_PRODUTOS }),
      ]),
  });
}
