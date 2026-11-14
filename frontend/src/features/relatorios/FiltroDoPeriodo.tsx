import { CamposDoPeriodo } from "../../compartilhado/CamposDoPeriodo";
import type { PeriodoParcial } from "../../compartilhado/periodo";
import styles from "./AbaTopClientes.module.css";

type Props = {
  periodo: PeriodoParcial;
  aoMudar: (periodo: PeriodoParcial) => void;
};

export function FiltroDoPeriodo({ periodo, aoMudar }: Props) {
  return (
    <fieldset className={styles.periodo}>
      <legend>Período</legend>
      <CamposDoPeriodo periodo={periodo} aoMudar={aoMudar} obrigatorio classeDoErro={styles.erro} />
    </fieldset>
  );
}
