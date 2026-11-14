import { useId, useState } from "react";
import styles from "./App.module.css";
import { AbaPedidos } from "./features/pedidos/AbaPedidos";
import { AbaTopClientes } from "./features/relatorios/AbaTopClientes";
import { IndicadorDeSaude } from "./features/saude/IndicadorDeSaude";

const ABAS = [
  { chave: "pedidos", titulo: "Pedidos" },
  { chave: "top-clientes", titulo: "Top Clientes" },
] as const;

type Aba = (typeof ABAS)[number]["chave"];

export function App() {
  const id = useId();
  const [abaAtiva, setAbaAtiva] = useState<Aba>("pedidos");

  return (
    <main className={styles.pagina}>
      <header className={styles.cabecalho}>
        <h1>Gestão de Pedidos</h1>
        <IndicadorDeSaude />
      </header>

      <div role="tablist" aria-label="Seções" className={styles.abas}>
        {ABAS.map((aba) => (
          <button
            key={aba.chave}
            type="button"
            role="tab"
            id={`${id}-aba-${aba.chave}`}
            aria-selected={abaAtiva === aba.chave}
            aria-controls={`${id}-painel-${aba.chave}`}
            className={styles.aba}
            onClick={() => setAbaAtiva(aba.chave)}
          >
            {aba.titulo}
          </button>
        ))}
      </div>

      {/* Só a aba ativa fica montada: a outra não consulta a API enquanto está escondida. */}
      <div
        role="tabpanel"
        id={`${id}-painel-${abaAtiva}`}
        aria-labelledby={`${id}-aba-${abaAtiva}`}
      >
        {abaAtiva === "pedidos" ? <AbaPedidos /> : <AbaTopClientes />}
      </div>
    </main>
  );
}
