const moeda = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

// A Data do Pedido é UTC e o Período também (RN12): exibir em UTC mantém a tela coerente com o filtro.
const dataHora = new Intl.DateTimeFormat("pt-BR", {
  dateStyle: "short",
  timeStyle: "short",
  timeZone: "UTC",
});

export function formatarMoeda(valor: number): string {
  return moeda.format(valor);
}

export function formatarDataHoraUtc(iso: string): string {
  return dataHora.format(new Date(iso));
}
