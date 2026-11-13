import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { buscarTextoDaApi } from "../../compartilhado/buscarDaApi";

export const estadoDeSaudeSchema = z.enum(["Healthy", "Degraded", "Unhealthy"]);
export type EstadoDeSaude = z.infer<typeof estadoDeSaudeSchema>;

export function useEstadoDeSaude() {
  return useQuery({
    queryKey: ["saude"],
    queryFn: () => buscarTextoDaApi("/health", estadoDeSaudeSchema),
    refetchInterval: 30_000,
  });
}
