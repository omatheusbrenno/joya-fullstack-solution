import { useEffect, useId, useRef } from "react";
import { useClientes } from "../../clientes/api/clientes";
import { type PedidoCriado, useCriarPedido } from "../api/criarPedido";
import { FormularioDoPedido } from "./FormularioDoPedido";
import styles from "./NovoPedido.module.css";

type Props = {
  aberto: boolean;
  aoFechar: () => void;
  aoCriar: (pedido: PedidoCriado) => void;
};

/** Modal de criação de Pedido sobre o <dialog> nativo: foco preso, Esc fecha e o fundo fica inerte. */
export function ModalNovoPedido({ aberto, aoFechar, aoCriar }: Props) {
  const idDoTitulo = useId();
  const dialogo = useRef<HTMLDialogElement>(null);
  const clientes = useClientes();
  const criacao = useCriarPedido();

  useEffect(() => {
    const elemento = dialogo.current;
    if (!elemento) return;
    if (aberto && !elemento.open) elemento.showModal();
    if (!aberto && elemento.open) elemento.close();
  }, [aberto]);

  function fechar() {
    criacao.reset();
    aoFechar();
  }

  return (
    <dialog
      ref={dialogo}
      aria-labelledby={idDoTitulo}
      className={styles.modal}
      onClose={fechar}
      onCancel={(evento) => {
        // Durante o POST o modal não fecha: o resultado ainda vai chegar.
        if (criacao.isPending) evento.preventDefault();
      }}
    >
      <h2 id={idDoTitulo}>Novo Pedido</h2>
      {/* Remonta a cada abertura: o formulário sempre começa limpo. */}
      {aberto && (
        <FormularioDoPedido
          clientes={clientes.data ?? []}
          criacao={criacao}
          aoCancelar={fechar}
          aoCriar={(pedido) => {
            criacao.reset();
            aoCriar(pedido);
          }}
        />
      )}
    </dialog>
  );
}
