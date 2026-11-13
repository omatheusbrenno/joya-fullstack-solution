import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { buscarJsonDaApi } from "../../../compartilhado/buscarDaApi";

export const clienteSchema = z.object({
  id: z.number().int().positive(),
  nome: z.string(),
});
export type Cliente = z.infer<typeof clienteSchema>;

const clientesSchema = z.array(clienteSchema);

export function useClientes() {
  return useQuery({
    queryKey: ["clientes"],
    queryFn: ({ signal }) => buscarJsonDaApi("/clientes", clientesSchema, undefined, signal),
    staleTime: 5 * 60_000,
  });
}
