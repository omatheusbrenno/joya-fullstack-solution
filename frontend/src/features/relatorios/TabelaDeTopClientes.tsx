import { formatarMoeda, formatarNumero } from "../../compartilhado/formatacao";
import styles from "./AbaTopClientes.module.css";
import type { TopCliente } from "./api/topClientes";

export function TabelaDeTopClientes({ clientes }: { clientes: TopCliente[] }) {
  if (clientes.length === 0) {
    return <p className={styles.vazio}>Nenhum Cliente com Pedidos neste Período.</p>;
  }

  return (
    <table className={styles.tabela}>
      <thead>
        <tr>
          <th scope="col">Posição</th>
          <th scope="col">Cliente</th>
          <th scope="col" className={styles.numero}>
            Pedidos
          </th>
          <th scope="col" className={styles.numero}>
            Valor Total
          </th>
        </tr>
      </thead>
      <tbody>
        {clientes.map((cliente, indice) => (
          <tr key={cliente.clienteId}>
            <td>{indice + 1}</td>
            <td>
              {/* O id diferencia os Clientes homônimos (CA23). */}
              {cliente.nome} <span className={styles.secundario}>(#{cliente.clienteId})</span>
            </td>
            <td className={styles.numero}>{formatarNumero(cliente.quantidadePedidos)}</td>
            <td className={styles.numero}>{formatarMoeda(cliente.valorTotal)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
