import { useEstadoDeSaude } from "./api";

const rotulos = {
  Healthy: "API saudável",
  Degraded: "API degradada",
  Unhealthy: "API indisponível",
} as const;

export function IndicadorDeSaude() {
  const { data, isPending, isError } = useEstadoDeSaude();

  const texto = isPending ? "Verificando a API…" : isError ? "API inacessível" : rotulos[data];

  return (
    <output aria-live="polite" data-testid="indicador-de-saude">
      {texto}
    </output>
  );
}
