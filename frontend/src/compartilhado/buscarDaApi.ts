import type { z } from "zod";

/**
 * Chama a API sempre por caminho relativo (/api/...), na mesma origem (ADR 0011),
 * e valida o corpo com o schema antes de devolvê-lo. O tipo nunca é presumido.
 */
export async function buscarTextoDaApi<T>(caminho: string, schema: z.ZodType<T>): Promise<T> {
  const resposta = await fetch(`/api${caminho}`, { headers: { Accept: "text/plain" } });
  return schema.parse(await resposta.text());
}
