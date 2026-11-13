#!/usr/bin/env bash
# PreToolUse (Bash): bloqueia comandos git que reescrevem histórico ou destroem trabalho.
# O teste técnico avalia o histórico de commits e proíbe squash/reescrita.
# Baseado em mattpocock/skills (misc/git-guardrails-claude-code), sem bloquear `git push` normal.
# Lê o JSON do hook no stdin; exit 2 = bloqueia e devolve o motivo ao agente.

input="$(cat)"

if command -v jq >/dev/null 2>&1; then
  cmd="$(printf '%s' "$input" | jq -r '.tool_input.command // empty')"
else
  cmd="$input"
fi

[ -z "$cmd" ] && exit 0

patterns=(
  'git[^|;&]*push[^|;&]*(--force|--force-with-lease|[[:space:]]-f([[:space:]]|$)|[[:space:]]\+)'
  'git[^|;&]*rebase([[:space:]]|$)'
  'git[^|;&]*commit[^|;&]*--amend'
  'git[^|;&]*merge[^|;&]*--squash'
  'git[^|;&]*reset[^|;&]*--hard'
  'git[^|;&]*clean[^|;&]*-[a-zA-Z]*f'
  'git[^|;&]*branch[^|;&]*-D'
  'git[^|;&]*(filter-branch|filter-repo|replace)([[:space:]]|$)'
  'git[^|;&]*checkout[^|;&]*[[:space:]]\.([[:space:]]|$)'
  'git[^|;&]*restore[^|;&]*[[:space:]]\.([[:space:]]|$)'
)

for p in "${patterns[@]}"; do
  if printf '%s' "$cmd" | grep -qE -- "$p"; then
    # rebase --abort / --continue apenas encerram um rebase já em curso
    if printf '%s' "$cmd" | grep -qE -- 'rebase[[:space:]]+--(abort|quit)'; then exit 0; fi
    echo "BLOQUEADO pelo hook do projeto: '$cmd' reescreve histórico ou descarta trabalho (padrão: $p). O histórico de commits é avaliado e não pode ser reescrito. Peça ao humano para executar manualmente, se for mesmo necessário." >&2
    exit 2
  fi
done

exit 0
