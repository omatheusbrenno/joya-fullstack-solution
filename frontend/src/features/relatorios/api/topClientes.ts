import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { buscarJsonDaApi } from "../../../compartilhado/buscarDaApi";

export const topClienteSchema = z.object({
  clienteId: z.number().int().positive(),
  nome: z.string(),
  quantidadePedidos: z.number().int().positive(),
  valorTotal: z.number().nonnegative(),
});
export type TopCliente = z.infer<typeof topClienteSchema>;

const topClientesSchema = z.array(topClienteSchema).max(5);

/** Período de GET /relatorios/top-clientes, obrigatório. As datas vêm do <input type="date">, em yyyy-MM-dd. */
export type Periodo = { dataInicio: string; dataFim: string };

/** Prefixo das consultas do Top Clientes, invalidado depois de criar um Pedido. */
export const CHAVE_DO_TOP_CLIENTES = ["top-clientes"] as const;

/** O ano corrente em UTC, o mesmo fuso do Período (RN12). */
export function periodoDoAnoCorrente(agora: Date = new Date()): Periodo {
  const ano = agora.getUTCFullYear();
  return { dataInicio: `${ano}-01-01`, dataFim: `${ano}-12-31` };
}

/** Sem uma das datas ou com a final antes da inicial não há consulta: a API devolveria 400. */
export function periodoValido({ dataInicio, dataFim }: Periodo): boolean {
  return Boolean(dataInicio && dataFim && dataInicio <= dataFim);
}

export function useTopClientes(periodo: Periodo) {
  return useQuery({
    queryKey: [...CHAVE_DO_TOP_CLIENTES, periodo],
    queryFn: ({ signal }) =>
      buscarJsonDaApi("/relatorios/top-clientes", topClientesSchema, periodo, signal),
    enabled: periodoValido(periodo),
    placeholderData: keepPreviousData,
  });
}
