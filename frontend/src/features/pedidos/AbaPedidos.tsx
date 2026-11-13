import { useState } from "react";
import styles from "./AbaPedidos.module.css";
import { type FiltrosDePedidos, periodoInvertido, useListaDePedidos } from "./api/pedidos";
import { FiltrosDaListagem } from "./FiltrosDaListagem";
import { Paginacao } from "./Paginacao";
import { TabelaDePedidos } from "./TabelaDePedidos";

export function AbaPedidos() {
  const [filtros, setFiltros] = useState<FiltrosDePedidos>({});
  const [pagina, setPagina] = useState(1);
  const lista = useListaDePedidos(filtros, pagina);

  function mudarFiltros(novos: FiltrosDePedidos) {
    setFiltros(novos);
    setPagina(1);
  }

  return (
    <section aria-labelledby="titulo-pedidos" className={styles.aba}>
      <h2 id="titulo-pedidos">Pedidos</h2>
      <FiltrosDaListagem filtros={filtros} aoMudar={mudarFiltros} />

      {/* Com o Período inválido não há consulta: o aviso fica nos filtros e o resultado anterior some. */}
      {periodoInvertido(filtros) ? null : lista.isError ? (
        <p role="alert" className={styles.erro}>
          Não foi possível carregar os Pedidos. Tente novamente.
        </p>
      ) : lista.data ? (
        <div
          aria-busy={lista.isFetching}
          className={lista.isPlaceholderData ? styles.carregando : ""}
        >
          <TabelaDePedidos pedidos={lista.data.itens} />
          <Paginacao
            pagina={lista.data.pagina}
            tamanhoPagina={lista.data.tamanhoPagina}
            totalItens={lista.data.totalItens}
            aoMudar={setPagina}
          />
        </div>
      ) : (
        lista.isFetching && <p>Carregando Pedidos…</p>
      )}
    </section>
  );
}
