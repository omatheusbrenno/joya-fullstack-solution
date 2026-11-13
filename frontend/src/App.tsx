import styles from "./App.module.css";
import { AbaPedidos } from "./features/pedidos/AbaPedidos";
import { IndicadorDeSaude } from "./features/saude/IndicadorDeSaude";

export function App() {
  return (
    <main className={styles.pagina}>
      <header className={styles.cabecalho}>
        <h1>Gestão de Pedidos</h1>
        <IndicadorDeSaude />
      </header>
      <AbaPedidos />
    </main>
  );
}
