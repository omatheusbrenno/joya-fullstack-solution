/** Período de consulta: datas do <input type="date">, em yyyy-MM-dd, inclusivas e em UTC (RN12). */
export type PeriodoParcial = {
  dataInicio?: string | undefined;
  dataFim?: string | undefined;
};

/** Data final antes da inicial: a API devolveria 400, então nem se consulta. */
export function periodoInvertido({ dataInicio, dataFim }: PeriodoParcial): boolean {
  return Boolean(dataInicio && dataFim && dataInicio > dataFim);
}

export function periodoCompleto({ dataInicio, dataFim }: PeriodoParcial): boolean {
  return Boolean(dataInicio && dataFim);
}
