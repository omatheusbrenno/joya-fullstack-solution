import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { buscarJsonDaApi } from "../../../compartilhado/buscarDaApi";

export const produtoSchema = z.object({
  id: z.number().int().positive(),
  nome: z.string(),
  preco: z.number().nonnegative(),
  estoque: z.number().int().nonnegative(),
});
export type Produto = z.infer<typeof produtoSchema>;

const produtosSchema = z.array(produtoSchema);

/** Chave invalidada depois de criar um Pedido: o Estoque mudou. */
export const CHAVE_DOS_PRODUTOS = ["produtos"] as const;

export function useProdutos() {
  return useQuery({
    queryKey: CHAVE_DOS_PRODUTOS,
    queryFn: ({ signal }) => buscarJsonDaApi("/produtos", produtosSchema, undefined, signal),
  });
}
