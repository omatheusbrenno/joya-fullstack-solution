import { expect, test } from "@playwright/test";

// Ids fixos da carga de exemplo (database/03-carga-exemplo.sql).
const BRUNO_LIMA = "2";
const MOUSE_SEM_FIO = "2";

function hojeEmUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

test("criar um Pedido e vê-lo na listagem", async ({ page }) => {
  await page.goto("/");

  // Filtra antes de criar: a linha nova só aparece se a listagem for atualizada depois do POST (CA22).
  const filtros = page.getByRole("group", { name: "Filtros" });
  await filtros.getByLabel("Cliente").selectOption(BRUNO_LIMA);
  await filtros.getByLabel("Data inicial").fill(hojeEmUtc());

  await page.getByRole("button", { name: "Novo Pedido" }).click();
  const modal = page.getByRole("dialog", { name: "Novo Pedido" });
  await modal.getByLabel("Cliente").selectOption(BRUNO_LIMA);
  await modal.getByLabel("Produto do Item 1").selectOption(MOUSE_SEM_FIO);
  await modal.getByLabel("Quantidade do Item 1").fill("2");
  await expect(modal.getByText("R$ 159,80")).toBeVisible();

  await modal.getByRole("button", { name: "Criar Pedido" }).click();

  await expect(modal).toBeHidden();
  const confirmacao = page.getByRole("status").filter({ hasText: /Pedido #\d+ criado/ });
  await expect(confirmacao).toBeVisible();
  const id = (await confirmacao.textContent())?.match(/#(\d+)/)?.[1];
  expect(id).toBeDefined();

  const linha = page.getByRole("row").filter({ hasText: `#${id}` });
  await expect(linha).toContainText("Bruno Lima");
  await expect(linha).toContainText("R$ 159,80");
});

test("o modal bloqueia Pedido sem Itens e Produto repetido e mostra as violações 422 da API", async ({
  page,
}) => {
  const WEBCAM_FULL_HD = "15"; // Estoque baixo na carga de exemplo.
  await page.goto("/");
  await page.getByRole("button", { name: "Novo Pedido" }).click();
  const modal = page.getByRole("dialog", { name: "Novo Pedido" });
  const criar = modal.getByRole("button", { name: "Criar Pedido" });
  await modal.getByLabel("Cliente").selectOption(BRUNO_LIMA);

  await modal.getByRole("button", { name: "Remover Item 1" }).click();
  await criar.click();
  await expect(modal.getByText("Adicione pelo menos um Item.")).toBeVisible();

  await modal.getByRole("button", { name: "Adicionar Item" }).click();
  await modal.getByRole("button", { name: "Adicionar Item" }).click();
  await modal.getByLabel("Produto do Item 1").selectOption(MOUSE_SEM_FIO);
  await modal.getByLabel("Produto do Item 2").selectOption(MOUSE_SEM_FIO);
  await criar.click();
  await expect(modal.getByText("Este Produto já está no Item 1.")).toBeVisible();

  // Mais do que o Estoque: o front deixa passar e quem recusa é a API (422).
  await modal.getByRole("button", { name: "Remover Item 2" }).click();
  await modal.getByLabel("Produto do Item 1").selectOption(WEBCAM_FULL_HD);
  await modal.getByLabel("Quantidade do Item 1").fill("10000");
  let liberarPost = () => {};
  await page.route("**/api/pedidos", async (rota) => {
    await new Promise<void>((resolve) => {
      liberarPost = resolve;
    });
    await rota.continue();
  });
  await criar.click();
  await expect(modal.getByRole("button", { name: "Criando Pedido…" })).toBeDisabled();
  liberarPost();

  const violacoes = modal.getByRole("alert").filter({ hasText: "O Pedido não foi criado" });
  await expect(violacoes).toContainText("Item 1: Estoque insuficiente de Webcam Full HD");
  await expect(modal).toBeVisible();
});
