import { expect, test } from "@playwright/test";

// O nginx serve o SPA com uma CSP restritiva (só 'self'). Se o build passar a precisar de script ou estilo
// inline, ou de recurso externo, o navegador bloqueia em silêncio: este teste torna isso visível.
test("o SPA carrega sob a CSP do nginx sem nenhuma violação", async ({ page }) => {
  const violacoes: string[] = [];
  page.on("console", (mensagem) => {
    if (/Content Security Policy|Refused to/i.test(mensagem.text())) {
      violacoes.push(mensagem.text());
    }
  });

  const resposta = await page.goto("/");

  expect(resposta?.headers()["content-security-policy"]).toContain("default-src 'self'");
  await expect(page.getByRole("tab", { name: "Pedidos" })).toBeVisible();
  await page.waitForLoadState("networkidle");
  expect(violacoes).toEqual([]);
});
