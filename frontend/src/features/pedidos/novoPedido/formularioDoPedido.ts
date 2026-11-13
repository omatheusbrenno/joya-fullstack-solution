import { z } from "zod";
import type { Produto } from "../../produtos/api/produtos";

// Os limites espelham RN02 e RN03. O front só antecipa o erro: quem decide é a API.
export const MAXIMO_DE_ITENS = 50;
const QUANTIDADE_MAXIMA = 10_000;

const idEscolhido = (mensagem: string) => z.string().regex(/^\d+$/, mensagem).transform(Number);

const quantidade = z
  .string()
  .trim()
  .regex(/^\d+$/, "Informe uma quantidade inteira.")
  .transform(Number)
  .pipe(
    z
      .number()
      .min(1, "A quantidade deve ser pelo menos 1.")
      .max(QUANTIDADE_MAXIMA, `A quantidade deve ser no máximo ${QUANTIDADE_MAXIMA}.`),
  );

/** Campos do formulário como strings (valores de <select> e <input>), convertidos para o corpo do POST. */
export const formularioDoPedidoSchema = z.object({
  clienteId: idEscolhido("Escolha o Cliente."),
  itens: z
    .array(z.object({ produtoId: idEscolhido("Escolha o Produto."), quantidade }))
    .min(1, "Adicione pelo menos um Item.")
    .max(MAXIMO_DE_ITENS, `O Pedido pode ter no máximo ${MAXIMO_DE_ITENS} Itens.`)
    .superRefine((itens, contexto) => {
      // RN04: o mesmo Produto não aparece em dois Itens.
      const primeiroItem = new Map<number, number>();
      itens.forEach((item, indice) => {
        const anterior = primeiroItem.get(item.produtoId);
        if (anterior === undefined) {
          primeiroItem.set(item.produtoId, indice);
        } else {
          contexto.addIssue({
            code: "custom",
            path: [indice, "produtoId"],
            message: `Este Produto já está no Item ${anterior + 1}.`,
          });
        }
      });
    }),
});

export type CamposDoPedido = z.input<typeof formularioDoPedidoSchema>;
export type ItemDoFormulario = CamposDoPedido["itens"][number];

export const ITEM_VAZIO: ItemDoFormulario = { produtoId: "", quantidade: "1" };

export function camposIniciais(): CamposDoPedido {
  return { clienteId: "", itens: [{ ...ITEM_VAZIO }] };
}

/** Prévia do Valor Total: só entram Itens com Produto escolhido e quantidade inteira positiva. */
export function previaDoValorTotal(itens: ItemDoFormulario[], produtos: Produto[]): number {
  const precoPorId = new Map(produtos.map((produto) => [produto.id, produto.preco]));
  // Soma em centavos para a prévia não acumular erro de ponto flutuante.
  const centavos = itens.reduce((soma, item) => {
    const preco = precoPorId.get(Number(item.produtoId));
    const qtd = Number(item.quantidade);
    if (preco === undefined || !Number.isInteger(qtd) || qtd < 1) return soma;
    return soma + Math.round(preco * 100) * qtd;
  }, 0);
  return centavos / 100;
}

/** "itens[1].quantidade" vira "Item 2"; "clienteId" vira "Cliente". Para listar as violações da API. */
export function rotuloDoCampo(campo: string): string {
  const item = /^itens\[(\d+)\]/.exec(campo);
  if (item?.[1] !== undefined) return `Item ${Number(item[1]) + 1}`;
  if (campo === "clienteId") return "Cliente";
  if (campo === "itens") return "Itens";
  return "Pedido";
}
