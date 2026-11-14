import { expect, test } from "@playwright/test";

// Números fixos do volume determinístico (database/04-carga-volume.sql) somado à carga legível, em 2025.
// O mesmo Top 5 é conferido no teste de integração CargaDeVolumeTests.
const TOP_2025 = [
  { posicao: "1", cliente: "Ana Souza (#1)", pedidos: "6.027", valorTotal: "R$ 22.005.966,90" },
  { posicao: "2", cliente: "Ana Souza (#4)", pedidos: "5.272", valorTotal: "R$ 19.267.052,90" },
  { posicao: "3", cliente: "Carlos Lima (#7)", pedidos: "4.520", valorTotal: "R$ 16.517.209,20" },
  { posicao: "4", cliente: "Bruno Lima (#2)", pedidos: "3.771", valorTotal: "R$ 13.787.821,70" },
  { posicao: "5", cliente: "Carla Mendes (#3)", pedidos: "3.013", valorTotal: "R$ 10.990.530,80" },
];

test("Top Clientes num Período fixo da carga mostra 5 linhas decrescentes com homônimos separados", async ({
  page,
}) => {
  await page.goto("/");
  await page.getByRole("tab", { name: "Top Clientes" }).click();
  const aba = page.getByRole("tabpanel", { name: "Top Clientes" });

  // Abre no ano corrente, em UTC como o Período (RN12).
  const ano = new Date().getUTCFullYear();
  const periodo = aba.getByRole("group", { name: "Período" });
  await expect(periodo.getByLabel("Data inicial")).toHaveValue(`${ano}-01-01`);
  await expect(periodo.getByLabel("Data final")).toHaveValue(`${ano}-12-31`);

  await periodo.getByLabel("Data inicial").fill("2025-01-01");
  await periodo.getByLabel("Data final").fill("2025-12-31");

  const linhas = aba
    .getByRole("table")
    .getByRole("row")
    .filter({ has: page.getByRole("cell") });
  await expect(linhas).toHaveCount(TOP_2025.length);
  for (const [indice, esperado] of TOP_2025.entries()) {
    const celulas = linhas.nth(indice).getByRole("cell");
    await expect(celulas).toHaveText([
      esperado.posicao,
      esperado.cliente,
      esperado.pedidos,
      esperado.valorTotal,
    ]);
  }
});

test("Top Clientes num Período sem Pedidos mostra o estado vazio", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("tab", { name: "Top Clientes" }).click();
  const periodo = page
    .getByRole("tabpanel", { name: "Top Clientes" })
    .getByRole("group", { name: "Período" });

  await periodo.getByLabel("Data inicial").fill("2020-01-01");
  await periodo.getByLabel("Data final").fill("2020-12-31");

  await expect(page.getByText("Nenhum Cliente com Pedidos neste Período.")).toBeVisible();
});
