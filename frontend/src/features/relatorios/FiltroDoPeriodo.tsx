import { useId } from "react";
import styles from "./AbaTopClientes.module.css";
import { type Periodo, periodoValido } from "./api/topClientes";

type Props = {
  periodo: Periodo;
  aoMudar: (periodo: Periodo) => void;
};

export function FiltroDoPeriodo({ periodo, aoMudar }: Props) {
  const id = useId();
  const incompleto = !periodo.dataInicio || !periodo.dataFim;
  const invalido = !periodoValido(periodo);

  return (
    <fieldset className={styles.periodo}>
      <legend>Período</legend>

      <label htmlFor={`${id}-inicio`}>Data inicial</label>
      <input
        id={`${id}-inicio`}
        type="date"
        required
        value={periodo.dataInicio}
        onChange={(e) => aoMudar({ ...periodo, dataInicio: e.target.value })}
      />

      <label htmlFor={`${id}-fim`}>Data final</label>
      <input
        id={`${id}-fim`}
        type="date"
        required
        value={periodo.dataFim}
        aria-invalid={invalido}
        aria-describedby={invalido ? `${id}-erro` : undefined}
        onChange={(e) => aoMudar({ ...periodo, dataFim: e.target.value })}
      />

      {invalido && (
        <p id={`${id}-erro`} role="alert" className={styles.erro}>
          {incompleto
            ? "Informe as datas inicial e final do Período."
            : "A data final deve ser igual ou posterior à data inicial."}
        </p>
      )}
    </fieldset>
  );
}
