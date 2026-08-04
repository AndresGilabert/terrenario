#!/usr/bin/env bash
#
# Enlaza los dominios personalizados y emite sus certificados gestionados.
#
# Va aparte de `crear-infraestructura.sh` porque **depende del DNS**, que no está en Azure: si se
# ejecuta antes de que los registros hayan propagado, Azure rechaza la validación y hay que empezar
# de nuevo. Separarlo evita repetir la creación de recursos solo por haber llegado pronto.

set -euo pipefail

DOMINIO="${DOMINIO:-terrenario.com}"
GRUPO="${GRUPO:-rg-terrenario-prod}"
API="${API:-app-terrenario-api}"
WEB="${WEB:-swa-terrenario-web}"

paso() { printf "\n\033[1;36m▸ %s\033[0m\n" "$*"; }
ok()   { printf "  \033[0;32m✓\033[0m %s\n" "$*"; }

# ── 0. Comprobar el DNS antes de tocar nada ───────────────────────────────────
paso "Comprobando DNS"
for host in "api.$DOMINIO" "app.$DOMINIO"; do
  if ! host "$host" >/dev/null 2>&1 && ! nslookup "$host" >/dev/null 2>&1; then
    echo "  $host todavía no resuelve. Espera a que propague y vuelve a ejecutar." >&2
    exit 1
  fi
  ok "$host resuelve"
done

# ── 1. API ────────────────────────────────────────────────────────────────────
paso "Dominio de la API: api.$DOMINIO"
if az webapp config hostname list --resource-group "$GRUPO" --webapp-name "$API" \
     --query "[?name=='api.$DOMINIO']" -o tsv | grep -q .; then
  ok "Ya enlazado"
else
  az webapp config hostname add \
    --resource-group "$GRUPO" --webapp-name "$API" --hostname "api.$DOMINIO" -o none
  ok "Enlazado"
fi

# El certificado gestionado es gratuito y se renueva solo. Sin él el enlace existe pero sirve por
# HTTP, y sobre HTTP la cookie de refresco no puede ser `Secure`: la sesión no funcionaría.
if az webapp config ssl list --resource-group "$GRUPO" \
     --query "[?subjectName=='api.$DOMINIO']" -o tsv | grep -q .; then
  ok "Certificado ya emitido"
else
  paso "Emitiendo certificado gestionado (tarda un par de minutos)"
  HUELLA=$(az webapp config ssl create \
    --resource-group "$GRUPO" --name "$API" --hostname "api.$DOMINIO" \
    --query thumbprint -o tsv)
  az webapp config ssl bind \
    --resource-group "$GRUPO" --name "$API" \
    --certificate-thumbprint "$HUELLA" --ssl-type SNI -o none
  ok "Certificado emitido y enlazado"
fi

# ── 2. Cliente ────────────────────────────────────────────────────────────────
paso "Dominio del cliente: app.$DOMINIO"
if az staticwebapp hostname list --resource-group "$GRUPO" --name "$WEB" \
     --query "[?name=='app.$DOMINIO']" -o tsv | grep -q .; then
  ok "Ya enlazado"
else
  az staticwebapp hostname set \
    --resource-group "$GRUPO" --name "$WEB" --hostname "app.$DOMINIO" -o none
  ok "Enlazado (Azure emite y renueva el certificado solo)"
fi

paso "Dominios listos"
echo "  https://api.$DOMINIO"
echo "  https://app.$DOMINIO"
echo
echo "  Siguiente: ./configurar-api.sh"
