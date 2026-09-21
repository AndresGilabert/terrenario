#!/usr/bin/env bash
#
# Enlaza `wwwroot` del backend con `dist/` del frontend mediante un enlace simbólico, no una
# copia: cada `npm run build` deja el backend sirviendo exactamente lo mismo que generó, sin
# ningún paso manual de por medio.
#
# Uso (una sola vez por clon del repositorio):
#   ./infra/local/vincular-wwwroot.sh

set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
WWWROOT="$RAIZ/src/backend/Terrenario.Api/wwwroot"
DIST="$RAIZ/src/frontend/terrenario-web/dist"

if [ -e "$WWWROOT" ]; then
  if [ -L "$WWWROOT" ]; then
    echo "Ya enlazado: $WWWROOT -> $(readlink "$WWWROOT")"
    exit 0
  fi
  echo "$WWWROOT ya existe y no es un enlace simbólico. Bórralo a mano si quieres reemplazarlo (podría contener un wwwroot real de otra prueba)." >&2
  exit 1
fi

if [ ! -d "$DIST" ]; then
  # El enlace no necesita que el destino exista todavía, pero avisamos igual: sin `npm run build`
  # después, el backend arrancará con un `wwwroot` que apunta a nada.
  mkdir -p "$DIST"
  echo "Creado $DIST vacío (todavía no has ejecutado 'npm run build')."
fi

ln -s "$DIST" "$WWWROOT"
echo "Enlazado: $WWWROOT -> $DIST"
echo
echo "Siguiente:"
echo "  cd src/frontend/terrenario-web && npm run build"
echo "  cd ../../backend/Terrenario.Api && dotnet run"
