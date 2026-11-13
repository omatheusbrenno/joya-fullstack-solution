import { useId } from "react";
import { useClientes } from "../clientes/api/clientes";
import styles from "./AbaPedidos.module.css";
import { type FiltrosDePedidos, periodoInvertido } from "./api/pedidos";

type Props = {
  filtros: FiltrosDePedidos;
  aoMudar: (filtros: FiltrosDePedidos) => void;
};

export function FiltrosDaListagem({ filtros, aoMudar }: Props) {
  const id = useId();
  const clientes = useClientes();
  const invertido = periodoInvertido(filtros);

  return (
    <fieldset className={styles.filtros}>
      <legend>Filtros</legend>

      <label htmlFor={`${id}-cliente`}>Cliente</label>
      <select
        id={`${id}-cliente`}
        value={filtros.clienteId ?? ""}
        disabled={clientes.isPending || clientes.isError}
        onChange={(e) =>
          aoMudar({ ...filtros, clienteId: e.target.value ? Number(e.target.value) : undefined })
        }
      >
        <option value="">Todos os Clientes</option>
        {clientes.data?.map((cliente) => (
          // O id aparece junto do nome para diferenciar Clientes homônimos.
          <option key={cliente.id} value={cliente.id}>
            {cliente.nome} (#{cliente.id})
          </option>
        ))}
      </select>

      <label htmlFor={`${id}-inicio`}>Data inicial</label>
      <input
        id={`${id}-inicio`}
        type="date"
        value={filtros.dataInicio ?? ""}
        onChange={(e) => aoMudar({ ...filtros, dataInicio: e.target.value || undefined })}
      />

      <label htmlFor={`${id}-fim`}>Data final</label>
      <input
        id={`${id}-fim`}
        type="date"
        value={filtros.dataFim ?? ""}
        aria-invalid={invertido}
        aria-describedby={invertido ? `${id}-erro-periodo` : undefined}
        onChange={(e) => aoMudar({ ...filtros, dataFim: e.target.value || undefined })}
      />

      <button type="button" onClick={() => aoMudar({})}>
        Limpar filtros
      </button>

      {clientes.isError && <p role="alert">Não foi possível carregar os Clientes.</p>}
      {invertido && (
        <p id={`${id}-erro-periodo`} role="alert" className={styles.erro}>
          A data final deve ser igual ou posterior à data inicial.
        </p>
      )}
    </fieldset>
  );
}
