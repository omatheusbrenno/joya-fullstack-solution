import { ErroDeValidacaoDaApi } from "../../../compartilhado/buscarDaApi";
import { rotuloDoCampo } from "./formularioDoPedido";
import styles from "./NovoPedido.module.css";

/**
 * Violações 400/422 devolvidas pela API, todas de uma vez (RN11). As mensagens vêm do contrato de erros
 * (já validadas por schema) e são texto puro: o React escapa. Outros erros ficam com uma mensagem genérica.
 */
export function ViolacoesDaApi({ erro }: { erro: Error | null }) {
  if (!erro) return null;

  if (!(erro instanceof ErroDeValidacaoDaApi)) {
    return (
      <p role="alert" className={styles.erro}>
        Não foi possível criar o Pedido. Tente novamente.
      </p>
    );
  }

  return (
    <div role="alert" className={styles.violacoes}>
      <p>O Pedido não foi criado:</p>
      <ul>
        {Object.entries(erro.violacoes).flatMap(([campo, mensagens]) =>
          mensagens.map((mensagem) => (
            <li key={`${campo}-${mensagem}`}>
              {rotuloDoCampo(campo)}: {mensagem}
            </li>
          )),
        )}
      </ul>
    </div>
  );
}
