#!/usr/bin/env bash
# PostToolUse (Edit|Write|MultiEdit): verifica o arquivo recém-editado e devolve o erro ao agente.
#   *.cs, *.csproj, *.props  -> dotnet build da solution do backend (warnings são erros)
#   *.ts, *.tsx (frontend/)  -> biome check no arquivo + tsc --noEmit do projeto
# Testes ficam fora (lentos para cada edição): rodam no fim do ticket e na CI.
# exit 2 = bloqueia e mostra o stderr ao agente, que corrige no mesmo ciclo.

input="$(cat)"
arquivo="$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty' 2>/dev/null)"
[ -z "$arquivo" ] && exit 0

raiz="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"

case "$arquivo" in
  "$raiz"/backend/*.cs | "$raiz"/backend/*.csproj | "$raiz"/backend/*.props)
    if ! saida="$(cd "$raiz/backend" && dotnet build --nologo -v q -clp:ErrorsOnly 2>&1)"; then
      printf 'dotnet build falhou depois de editar %s:\n%s\n' "${arquivo#"$raiz"/}" "$(printf '%s' "$saida" | grep -E 'error' | sort -u | head -20)" >&2
      exit 2
    fi
    ;;
  "$raiz"/frontend/*.ts | "$raiz"/frontend/*.tsx)
    cd "$raiz/frontend" || exit 0
    if [ ! -d node_modules ]; then
      echo "Não deu para verificar ${arquivo#"$raiz"/}: falta frontend/node_modules. Rode 'npm ci' em frontend/ e edite de novo." >&2
      exit 2
    fi
    if ! saida="$(npx --no-install biome check "$arquivo" 2>&1)"; then
      printf 'biome check falhou em %s (rode npm run format para corrigir o formato):\n%s\n' "${arquivo#"$raiz"/}" "$(printf '%s' "$saida" | head -40)" >&2
      exit 2
    fi
    if ! saida="$(npx --no-install tsc -b --noEmit 2>&1)"; then
      printf 'tsc falhou depois de editar %s:\n%s\n' "${arquivo#"$raiz"/}" "$(printf '%s' "$saida" | head -20)" >&2
      exit 2
    fi
    ;;
esac

exit 0
