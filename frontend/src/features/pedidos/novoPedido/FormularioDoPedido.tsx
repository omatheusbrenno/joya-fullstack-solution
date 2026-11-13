import { zodResolver } from "@hookform/resolvers/zod";
import { useId } from "react";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { formatarMoeda } from "../../../compartilhado/formatacao";
import type { Cliente } from "../../clientes/api/clientes";
import { useProdutos } from "../../produtos/api/produtos";
import type { NovoPedido, PedidoCriado, useCriarPedido } from "../api/criarPedido";
import {
  type CamposDoPedido,
  camposIniciais,
  formularioDoPedidoSchema,
  ITEM_VAZIO,
  MAXIMO_DE_ITENS,
  previaDoValorTotal,
} from "./formularioDoPedido";
import { LinhaDoItem } from "./LinhaDoItem";
import styles from "./NovoPedido.module.css";
import { ViolacoesDaApi } from "./ViolacoesDaApi";

type Props = {
  clientes: Cliente[];
  criacao: ReturnType<typeof useCriarPedido>;
  aoCancelar: () => void;
  aoCriar: (pedido: PedidoCriado) => void;
};

export function FormularioDoPedido({ clientes, criacao, aoCancelar, aoCriar }: Props) {
  const id = useId();
  const produtos = useProdutos();
  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<CamposDoPedido, unknown, NovoPedido>({
    resolver: zodResolver(formularioDoPedidoSchema),
    defaultValues: camposIniciais(),
  });
  const { fields, append, remove } = useFieldArray({ control, name: "itens" });
  const itens = useWatch({ control, name: "itens" });
  const listaDeProdutos = produtos.data ?? [];
  const erroDosItens = errors.itens?.message ?? errors.itens?.root?.message;

  return (
    <form
      noValidate
      className={styles.formulario}
      onSubmit={handleSubmit((pedido) => criacao.mutate(pedido, { onSuccess: aoCriar }))}
    >
      <div className={styles.campo}>
        <label htmlFor={`${id}-cliente`}>Cliente</label>
        <select
          id={`${id}-cliente`}
          aria-invalid={Boolean(errors.clienteId)}
          aria-describedby={errors.clienteId ? `${id}-erro-cliente` : undefined}
          {...register("clienteId")}
        >
          <option value="">Escolha o Cliente</option>
          {clientes.map((cliente) => (
            // O id diferencia Clientes homônimos.
            <option key={cliente.id} value={cliente.id}>
              {cliente.nome} (#{cliente.id})
            </option>
          ))}
        </select>
        {errors.clienteId && (
          <p id={`${id}-erro-cliente`} className={styles.erro}>
            {errors.clienteId.message}
          </p>
        )}
      </div>

      <fieldset className={styles.itens}>
        <legend>Itens</legend>
        {produtos.isError && (
          <p role="alert" className={styles.erro}>
            Não foi possível carregar os Produtos.
          </p>
        )}
        {fields.map((campo, indice) => (
          <LinhaDoItem
            key={campo.id}
            indice={indice}
            item={itens[indice] ?? ITEM_VAZIO}
            produtos={listaDeProdutos}
            register={register}
            erros={errors.itens?.[indice]}
            aoRemover={() => remove(indice)}
          />
        ))}
        {erroDosItens && <p className={styles.erro}>{erroDosItens}</p>}
        <button
          type="button"
          disabled={fields.length >= MAXIMO_DE_ITENS}
          onClick={() => append({ ...ITEM_VAZIO })}
        >
          Adicionar Item
        </button>
      </fieldset>

      <p className={styles.total}>
        Valor Total (prévia):{" "}
        <strong>{formatarMoeda(previaDoValorTotal(itens, listaDeProdutos))}</strong>
      </p>

      <ViolacoesDaApi erro={criacao.error} />

      <div className={styles.acoes}>
        <button type="button" disabled={criacao.isPending} onClick={aoCancelar}>
          Cancelar
        </button>
        <button type="submit" disabled={criacao.isPending}>
          {criacao.isPending ? "Criando Pedido…" : "Criar Pedido"}
        </button>
      </div>
    </form>
  );
}
