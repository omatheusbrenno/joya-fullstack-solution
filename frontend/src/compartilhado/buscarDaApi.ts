import { z } from "zod";

/** Falha de chamada à API. A mensagem é genérica: nada do corpo de erro vai para a tela. */
export class ErroDaApi extends Error {
  readonly status: number;

  constructor(status: number) {
    super(`A API respondeu ${status}.`);
    this.name = "ErroDaApi";
    this.status = status;
  }
}

/**
 * 400 (forma) ou 422 (regra de negócio) com as violações por campo (ADR 0008), já validadas por schema.
 * As chaves são caminhos do JSON enviado, como "clienteId" ou "itens[1].quantidade".
 */
export class ErroDeValidacaoDaApi extends ErroDaApi {
  readonly violacoes: Record<string, string[]>;

  constructor(status: number, violacoes: Record<string, string[]>) {
    super(status);
    this.name = "ErroDeValidacaoDaApi";
    this.violacoes = violacoes;
  }
}

const problemaDeValidacaoSchema = z.object({
  errors: z.record(z.string(), z.array(z.string())),
});

type ParametrosDeConsulta = Record<string, string | number | undefined>;

function montarUrl(caminho: string, parametros: ParametrosDeConsulta = {}): string {
  const busca = new URLSearchParams();
  for (const [nome, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== "") busca.set(nome, String(valor));
  }
  const consulta = busca.toString();
  return `/api${caminho}${consulta ? `?${consulta}` : ""}`;
}

/**
 * Chama a API sempre por caminho relativo (/api/...), na mesma origem (ADR 0011),
 * e valida o corpo com o schema antes de devolvê-lo. O tipo nunca é presumido.
 */
export async function buscarTextoDaApi<T>(caminho: string, schema: z.ZodType<T>): Promise<T> {
  const resposta = await fetch(montarUrl(caminho), { headers: { Accept: "text/plain" } });
  return schema.parse(await resposta.text());
}

export async function buscarJsonDaApi<T>(
  caminho: string,
  schema: z.ZodType<T>,
  parametros?: ParametrosDeConsulta,
  signal?: AbortSignal,
): Promise<T> {
  const resposta = await fetch(montarUrl(caminho, parametros), {
    headers: { Accept: "application/json" },
    ...(signal ? { signal } : {}),
  });
  if (!resposta.ok) throw new ErroDaApi(resposta.status);
  return schema.parse(await resposta.json());
}

/** POST de JSON na API. Violações 400/422 viram {@link ErroDeValidacaoDaApi}; o resto, {@link ErroDaApi}. */
export async function enviarJsonParaApi<T>(
  caminho: string,
  corpo: unknown,
  schema: z.ZodType<T>,
): Promise<T> {
  const resposta = await fetch(montarUrl(caminho), {
    method: "POST",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: JSON.stringify(corpo),
  });
  if (resposta.status === 400 || resposta.status === 422) {
    const problema = problemaDeValidacaoSchema.safeParse(await resposta.json().catch(() => null));
    if (problema.success) throw new ErroDeValidacaoDaApi(resposta.status, problema.data.errors);
  }
  if (!resposta.ok) throw new ErroDaApi(resposta.status);
  return schema.parse(await resposta.json());
}
