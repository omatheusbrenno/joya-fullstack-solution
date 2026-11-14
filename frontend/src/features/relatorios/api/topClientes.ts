import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { buscarJsonDaApi } from "../../../compartilhado/buscarDaApi";
import {
  type PeriodoParcial,
  periodoCompleto,
  periodoInvertido,
} from "../../../compartilhado/periodo";

export const topClienteSchema = z.object({
  clienteId: z.number().int().positive(),
  nome: z.string(),
  quantidadePedidos: z.number().int().positive(),
  valorTotal: z.number().nonnegative(),
});
export type TopCliente = z.infer<typeof topClienteSchema>;

/** A API devolve até cinco Clientes (RN13): mais do que isso é resposta fora do contrato. */
const MAXIMO_DE_TOP_CLIENTES = 5;

const topClientesSchema = z.array(topClienteSchema).max(MAXIMO_DE_TOP_CLIENTES);

/** Prefixo das consultas do Top Clientes, invalidado depois de criar um Pedido. */
export const CHAVE_DO_TOP_CLIENTES = ["top-clientes"] as const;

/** O ano corrente em UTC, o mesmo fuso do Período (RN12). */
export function periodoDoAnoCorrente(agora: Date = new Date()): PeriodoParcial {
  const ano = agora.getUTCFullYear();
  return { dataInicio: `${ano}-01-01`, dataFim: `${ano}-12-31` };
}

/** O Período é obrigatório no Top Clientes: sem uma das datas ou invertido, a API devolveria 400. */
export function periodoValido(periodo: PeriodoParcial): boolean {
  return periodoCompleto(periodo) && !periodoInvertido(periodo);
}

export function useTopClientes(periodo: PeriodoParcial) {
  return useQuery({
    queryKey: [...CHAVE_DO_TOP_CLIENTES, periodo],
    queryFn: ({ signal }) =>
      buscarJsonDaApi("/relatorios/top-clientes", topClientesSchema, periodo, signal),
    enabled: periodoValido(periodo),
    placeholderData: keepPreviousData,
  });
}
