import styles from "./AbaPedidos.module.css";

type Props = {
  pagina: number;
  tamanhoPagina: number;
  totalItens: number;
  aoMudar: (pagina: number) => void;
};

export function Paginacao({ pagina, tamanhoPagina, totalItens, aoMudar }: Props) {
  const totalDePaginas = Math.max(1, Math.ceil(totalItens / tamanhoPagina));

  return (
    <nav className={styles.paginacao} aria-label="Paginação dos Pedidos">
      <button type="button" disabled={pagina <= 1} onClick={() => aoMudar(pagina - 1)}>
        Anterior
      </button>
      <span>
        Página {pagina} de {totalDePaginas} · {totalItens} {totalItens === 1 ? "Pedido" : "Pedidos"}
      </span>
      <button type="button" disabled={pagina >= totalDePaginas} onClick={() => aoMudar(pagina + 1)}>
        Próxima
      </button>
    </nav>
  );
}
