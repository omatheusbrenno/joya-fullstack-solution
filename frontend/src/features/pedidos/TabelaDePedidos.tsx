import { formatarDataHoraUtc, formatarMoeda } from "../../compartilhado/formatacao";
import styles from "./AbaPedidos.module.css";
import type { PedidoListado } from "./api/pedidos";

export function TabelaDePedidos({ pedidos }: { pedidos: PedidoListado[] }) {
  if (pedidos.length === 0) {
    return <p className={styles.vazio}>Nenhum Pedido encontrado.</p>;
  }

  return (
    <table className={styles.tabela}>
      <thead>
        <tr>
          <th scope="col">Pedido</th>
          <th scope="col">Cliente</th>
          <th scope="col">Data do Pedido (UTC)</th>
          <th scope="col" className={styles.numero}>
            Itens
          </th>
          <th scope="col" className={styles.numero}>
            Valor Total
          </th>
        </tr>
      </thead>
      <tbody>
        {pedidos.map((pedido) => (
          <tr key={pedido.id}>
            <td>#{pedido.id}</td>
            <td>
              {pedido.nomeCliente} <span className={styles.secundario}>(#{pedido.clienteId})</span>
            </td>
            <td>
              <time dateTime={pedido.dataPedido}>{formatarDataHoraUtc(pedido.dataPedido)}</time>
            </td>
            <td className={styles.numero}>{pedido.quantidadeItens}</td>
            <td className={styles.numero}>{formatarMoeda(pedido.valorTotal)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
