import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// Em desenvolvimento, o Vite faz o papel do nginx: repassa /api para a API, mantendo a mesma origem (ADR 0011).
const apiEmDesenvolvimento = process.env.API_URL ?? "http://localhost:5080";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api": {
        target: apiEmDesenvolvimento,
        rewrite: (caminho) => caminho.replace(/^\/api/, ""),
      },
    },
  },
});
