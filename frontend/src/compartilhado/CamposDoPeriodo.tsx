import { useId } from "react";
import { type PeriodoParcial, periodoCompleto, periodoInvertido } from "./periodo";

type Props = {
  periodo: PeriodoParcial;
  aoMudar: (periodo: PeriodoParcial) => void;
  /** Período obrigatório (Top Clientes, RN13): faltar uma das datas também é aviso. */
  obrigatorio?: boolean;
  classeDoErro?: string | undefined;
};

/** As datas inicial e final do Período, com o aviso de Período inválido ligado ao campo da data final. */
export function CamposDoPeriodo({ periodo, aoMudar, obrigatorio = false, classeDoErro }: Props) {
  const id = useId();
  const incompleto = obrigatorio && !periodoCompleto(periodo);
  const invertido = periodoInvertido(periodo);
  const aviso = incompleto
    ? "Informe as datas inicial e final do Período."
    : invertido
      ? "A data final deve ser igual ou posterior à data inicial."
      : "";

  return (
    <>
      <label htmlFor={`${id}-inicio`}>Data inicial</label>
      <input
        id={`${id}-inicio`}
        type="date"
        required={obrigatorio}
        value={periodo.dataInicio ?? ""}
        onChange={(e) => aoMudar({ ...periodo, dataInicio: e.target.value || undefined })}
      />

      <label htmlFor={`${id}-fim`}>Data final</label>
      <input
        id={`${id}-fim`}
        type="date"
        required={obrigatorio}
        value={periodo.dataFim ?? ""}
        aria-invalid={Boolean(aviso)}
        aria-describedby={aviso ? `${id}-erro` : undefined}
        onChange={(e) => aoMudar({ ...periodo, dataFim: e.target.value || undefined })}
      />

      {aviso && (
        <p id={`${id}-erro`} role="alert" className={classeDoErro}>
          {aviso}
        </p>
      )}
    </>
  );
}
