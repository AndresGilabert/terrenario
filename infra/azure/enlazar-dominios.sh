#!/usr/bin/env bash
#
# Enlaza los dominios personalizados y emite sus certificados gestionados.
#
# Va aparte de `crear-infraestructura.sh` porque **depende del DNS**, que no está en Azure: si se
# ejecuta antes de que los registros hayan propagado, Azure rechaza la validación y hay que empezar
# de nuevo. Separarlo evita repetir la creación de recursos solo por haber llegado pronto.
#
# PLT-101 — Además del dominio de la aplicación (`app.$DOMINIO`), enlaza los dominios comprados solo
# para no perderlos (`$DOMINIO`, `www.$DOMINIO` y lo mismo para cada dominio de
# `DOMINIOS_REDIRECCION`): no tienen contenido propio, la API los redirige con 301 a
# `app.$DOMINIO` (`AlternateDomainRedirectMiddleware`). El enlace y el certificado son el mismo
# mecanismo de Azure para cualquier hostname, apex o subdominio: lo que cambia es el registro DNS que
# hay que crear (`A`/`ALIAS` para el apex, `CNAME` para el resto), y eso lo decide quien gestiona el
# DNS, no este script.

set -euo pipefail

# En Windows el instalador no siempre deja `az` en el PATH de la sesion en curso. Permite
# apuntarlo sin editar el script: AZ="/c/Program Files/.../az.cmd" ./script.sh
AZ="${AZ:-az}"

DOMINIO="${DOMINIO:-terrenario.com}"
GRUPO="${GRUPO:-rg-terrenario-prod}"
API="${API:-app-terrenario-api}"

# PLT-101 — Dominios de solo-redirección, cada uno aporta su apex y su `www`. Se pueden desactivar
# pasando una cadena vacía: DOMINIOS_REDIRECCION="" ./enlazar-dominios.sh
DOMINIOS_REDIRECCION="${DOMINIOS_REDIRECCION:-terrenario.com terrenario.es}"

paso() { printf "\n\033[1;36m▸ %s\033[0m\n" "$*"; }
ok()   { printf "  \033[0;32m✓\033[0m %s\n" "$*"; }

# ── Hosts a enlazar: el de la aplicación más los de redirección ──────────────
HOSTS=("app.$DOMINIO")
for dominio_redir in $DOMINIOS_REDIRECCION; do
  HOSTS+=("$dominio_redir" "www.$dominio_redir")
done

# ── 0. Comprobar el DNS antes de tocar nada ───────────────────────────────────
paso "Comprobando DNS"
for host in "${HOSTS[@]}"; do
  if ! host "$host" >/dev/null 2>&1 && ! nslookup "$host" >/dev/null 2>&1; then
    echo "  $host todavía no resuelve. Espera a que propague y vuelve a ejecutar." >&2
    exit 1
  fi
  ok "$host resuelve"
done

# El certificado gestionado es gratuito y se renueva solo. Sin él el enlace existe pero sirve por
# HTTP, y sobre HTTP la cookie de refresco no puede ser `Secure`: la sesión no funcionaría (en los
# dominios de redirección no hay cookie, pero sin certificado el 301 tampoco llega a servirse: el
# navegador corta en el `TLS handshake` antes de que exista respuesta que redirigir).
#
# Se hace en tres tiempos —pedir, esperar, enlazar— porque `ssl create` **devuelve antes de que el
# certificado exista**: la huella sale vacía y el enlace falla con «Certificate for thumbprint ''
# not found», dejando el dominio enlazado pero sirviendo el comodín de Azure. Es un fallo que se ve
# tarde y mal, porque la página responde.
#
# La existencia se consulta sobre el **recurso**: `webapp config ssl list` devolvió una lista vacía
# con el certificado ya creado, así que no sirve para decidir.
huella_actual() {
  "$AZ" resource show --resource-group "$GRUPO" \
    --resource-type Microsoft.Web/certificates --name "$1" \
    --query "properties.thumbprint" -o tsv 2>/dev/null
}

enlazar_hostname() {
  local host="$1"

  paso "Dominio: $host"
  if "$AZ" webapp config hostname list --resource-group "$GRUPO" --webapp-name "$API" \
       --query "[?name=='$host']" -o tsv | grep -q .; then
    ok "Ya enlazado"
  else
    "$AZ" webapp config hostname add \
      --resource-group "$GRUPO" --webapp-name "$API" --hostname "$host" -o none
    ok "Enlazado"
  fi

  local huella
  huella=$(huella_actual "$host" || true)

  if [ -z "$huella" ]; then
    paso "Emitiendo certificado gestionado para $host (tarda un par de minutos)"
    # Puede escupir un error de deserialización de la propia CLI y aun así crear el certificado.
    "$AZ" webapp config ssl create \
      --resource-group "$GRUPO" --name "$API" --hostname "$host" -o none 2>/dev/null || true

    for intento in $(seq 1 15); do
      huella=$(huella_actual "$host" || true)
      [ -n "$huella" ] && break
      printf "  esperando al certificado (%d/15)\r" "$intento"
      sleep 20
    done
    echo
  fi

  if [ -z "$huella" ]; then
    echo "El certificado de $host no llegó a emitirse. Reintenta el script dentro de unos minutos." >&2
    exit 1
  fi
  ok "Certificado disponible ($huella)"

  local estado
  estado=$("$AZ" webapp config hostname list --resource-group "$GRUPO" --webapp-name "$API" \
    --query "[?name=='$host'].sslState | [0]" -o tsv)

  if [ "$estado" = "SniEnabled" ]; then
    ok "Certificado ya enlazado"
  else
    "$AZ" webapp config ssl bind \
      --resource-group "$GRUPO" --name "$API" \
      --certificate-thumbprint "$huella" --ssl-type SNI -o none
    ok "Certificado enlazado (SNI)"
  fi
}

for host in "${HOSTS[@]}"; do
  enlazar_hostname "$host"
done

paso "Dominios listos"
echo "  https://app.$DOMINIO (aplicación)"
for dominio_redir in $DOMINIOS_REDIRECCION; do
  echo "  https://$dominio_redir y https://www.$dominio_redir → redirigen a https://app.$DOMINIO (301)"
done
echo
echo "  Recuerda: 'Domains:AlternateHosts' en appsettings.json debe listar estos dominios de"
echo "  redirección para que 'AlternateDomainRedirectMiddleware' los redirija (PLT-101)."
echo
echo "  Siguiente: ./configurar-api.sh"
