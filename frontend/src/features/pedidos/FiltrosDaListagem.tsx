import { useId } from "react";
import { CamposDoPeriodo } from "../../compartilhado/CamposDoPeriodo";
import { useClientes } from "../clientes/api/clientes";
import styles from "./AbaPedidos.module.css";
import type { FiltrosDePedidos } from "./api/pedidos";

type Props = {
  filtros: FiltrosDePedidos;
  aoMudar: (filtros: FiltrosDePedidos) => void;
};

export function FiltrosDaListagem({ filtros, aoMudar }: Props) {
  const id = useId();
  const clientes = useClientes();

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

      <CamposDoPeriodo
        periodo={filtros}
        aoMudar={(periodo) => aoMudar({ ...filtros, ...periodo })}
        classeDoErro={styles.erro}
      />

      <button type="button" onClick={() => aoMudar({})}>
        Limpar filtros
      </button>

      {clientes.isError && <p role="alert">Não foi possível carregar os Clientes.</p>}
    </fieldset>
  );
}
