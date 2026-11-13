import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { buscarJsonDaApi } from "../../../compartilhado/buscarDaApi";

export const pedidoListadoSchema = z.object({
  id: z.number().int().positive(),
  clienteId: z.number().int().positive(),
  nomeCliente: z.string(),
  dataPedido: z.iso.datetime(),
  quantidadeItens: z.number().int().nonnegative(),
  valorTotal: z.number().nonnegative(),
});
export type PedidoListado = z.infer<typeof pedidoListadoSchema>;

export const paginaDePedidosSchema = z.object({
  itens: z.array(pedidoListadoSchema),
  pagina: z.number().int().positive(),
  tamanhoPagina: z.number().int().positive(),
  totalItens: z.number().int().nonnegative(),
});
export type PaginaDePedidos = z.infer<typeof paginaDePedidosSchema>;

/** Filtros de GET /pedidos. As datas vêm do <input type="date">, já em yyyy-MM-dd. */
export type FiltrosDePedidos = {
  clienteId?: number | undefined;
  dataInicio?: string | undefined;
  dataFim?: string | undefined;
};

export const TAMANHO_DA_PAGINA = 20;

export function periodoInvertido({ dataInicio, dataFim }: FiltrosDePedidos): boolean {
  return Boolean(dataInicio && dataFim && dataInicio > dataFim);
}

export function useListaDePedidos(filtros: FiltrosDePedidos, pagina: number) {
  return useQuery({
    queryKey: ["pedidos", filtros, pagina],
    queryFn: ({ signal }) =>
      buscarJsonDaApi(
        "/pedidos",
        paginaDePedidosSchema,
        { ...filtros, pagina, tamanhoPagina: TAMANHO_DA_PAGINA },
        signal,
      ),
    enabled: !periodoInvertido(filtros),
    placeholderData: keepPreviousData,
  });
}
