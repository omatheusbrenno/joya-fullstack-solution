import { useId } from "react";
import type { FieldErrors, UseFormRegister } from "react-hook-form";
import { formatarMoeda } from "../../../compartilhado/formatacao";
import type { Produto } from "../../produtos/api/produtos";
import type { CamposDoPedido, ItemDoFormulario } from "./formularioDoPedido";
import styles from "./NovoPedido.module.css";

type Props = {
  indice: number;
  item: ItemDoFormulario;
  produtos: Produto[];
  register: UseFormRegister<CamposDoPedido>;
  erros: FieldErrors<ItemDoFormulario> | undefined;
  aoRemover: () => void;
};

/** Um Item de Pedido: Produto (com preço e Estoque), quantidade e remoção. */
export function CamposDoItem({ indice, item, produtos, register, erros, aoRemover }: Props) {
  const id = useId();
  const numero = indice + 1;
  const produto = produtos.find((candidato) => candidato.id === Number(item.produtoId));
  const idDoErro = `${id}-erro`;
  const mensagens = [erros?.produtoId?.message, erros?.quantidade?.message].filter(Boolean);

  return (
    <div className={styles.item}>
      <label htmlFor={`${id}-produto`}>Produto</label>
      <select
        id={`${id}-produto`}
        aria-label={`Produto do Item ${numero}`}
        aria-invalid={Boolean(erros?.produtoId)}
        aria-describedby={mensagens.length > 0 ? idDoErro : undefined}
        {...register(`itens.${indice}.produtoId`)}
      >
        <option value="">Escolha o Produto</option>
        {produtos.map((opcao) => (
          <option key={opcao.id} value={opcao.id} disabled={opcao.estoque === 0}>
            {opcao.nome} · {formatarMoeda(opcao.preco)} · Estoque: {opcao.estoque}
          </option>
        ))}
      </select>

      <label htmlFor={`${id}-quantidade`}>Quantidade</label>
      <input
        id={`${id}-quantidade`}
        type="number"
        inputMode="numeric"
        min={1}
        max={produto?.estoque}
        step={1}
        aria-label={`Quantidade do Item ${numero}`}
        aria-invalid={Boolean(erros?.quantidade)}
        aria-describedby={mensagens.length > 0 ? idDoErro : undefined}
        className={styles.quantidade}
        {...register(`itens.${indice}.quantidade`)}
      />

      <span className={styles.detalhe}>
        {produto
          ? `${formatarMoeda(produto.preco)} cada · Estoque: ${produto.estoque}`
          : "Escolha um Produto"}
      </span>

      <button type="button" aria-label={`Remover Item ${numero}`} onClick={aoRemover}>
        Remover
      </button>

      {mensagens.length > 0 && (
        <p id={idDoErro} className={styles.erro}>
          {mensagens.join(" ")}
        </p>
      )}
    </div>
  );
}
