import { useState } from "react";
import styles from "./AbaTopClientes.module.css";
import { periodoDoAnoCorrente, periodoValido, useTopClientes } from "./api/topClientes";
import { FiltroDoPeriodo } from "./FiltroDoPeriodo";
import { TabelaDeTopClientes } from "./TabelaDeTopClientes";

/** Os até cinco Clientes com maior Valor Total no Período, que abre no ano corrente (CA23). */
export function AbaTopClientes() {
  const [periodo, setPeriodo] = useState(() => periodoDoAnoCorrente());
  const top = useTopClientes(periodo);

  return (
    <div className={styles.aba}>
      <h2>Top Clientes</h2>
      <FiltroDoPeriodo periodo={periodo} aoMudar={setPeriodo} />

      {/* Com o Período inválido não há consulta: o aviso fica no filtro e o resultado anterior some. */}
      {!periodoValido(periodo) ? null : top.isError ? (
        <p role="alert" className={styles.erro}>
          Não foi possível carregar o Top Clientes. Tente novamente.
        </p>
      ) : top.data ? (
        <div aria-busy={top.isFetching} className={top.isPlaceholderData ? styles.carregando : ""}>
          <TabelaDeTopClientes clientes={top.data} />
        </div>
      ) : (
        top.isFetching && <p>Carregando Top Clientes…</p>
      )}
    </div>
  );
}
