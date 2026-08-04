#!/usr/bin/env bash
#
# Crea la infraestructura de producción de Terrenario en Azure.
#
# Es **idempotente**: cada recurso se comprueba antes de crearse, así que volver a ejecutarlo tras un
# fallo continúa donde se quedó en vez de reventar. Esa es la razón de hacerlo por comandos y no por
# el portal: un montaje a mano no se puede repetir igual, y el primer intento casi nunca sale entero.
#
# Lo que este script NO hace, porque no puede:
#   - Crear los registros DNS: viven en tu proveedor de dominio, no en Azure.
#   - Validar los dominios personalizados: necesitan que el DNS ya haya propagado.
#   - Configurar Google Cloud Console.
# Esos tres pasos están en el runbook, y el script te dice cuándo toca cada uno.
#
# Uso:
#   ./crear-infraestructura.sh
#
# Requisitos: az CLI con sesión iniciada (`az login`) y la suscripción correcta seleccionada
# (`az account set --subscription "..."`).

set -euo pipefail

# ── Parámetros ────────────────────────────────────────────────────────────────
DOMINIO="${DOMINIO:-terrenario.com}"
REGION="${REGION:-spaincentral}"
GRUPO="${GRUPO:-rg-terrenario-prod}"
PLAN="${PLAN:-plan-terrenario-prod}"
API="${API:-app-terrenario-api}"
WEB="${WEB:-swa-terrenario-web}"
PG="${PG:-psql-terrenario-prod}"
PG_USUARIO="${PG_USUARIO:-terrenario_admin}"
PG_BD="${PG_BD:-terrenario}"

# Static Web Apps: `Free` basta para la validación —admite dominio propio y certificado gestionado—.
# `Standard` solo aporta SLA, backend enlazado y red privada, que aquí no se usan.
SKU_WEB="${SKU_WEB:-Free}"
# App Service: `B1` es el escalón **mínimo con dominio propio y TLS**. `F1` y `D1` no valen: sin HTTPS
# la cookie de refresco no puede ser `Secure` y la sesión no funciona.
SKU_API="${SKU_API:-B1}"
# PostgreSQL: el escalón más barato. Si la suscripción es nueva, entra en los 12 meses gratuitos.
SKU_PG="${SKU_PG:-Standard_B1ms}"

paso() { printf "\n\033[1;36m▸ %s\033[0m\n" "$*"; }
ok()   { printf "  \033[0;32m✓\033[0m %s\n" "$*"; }
avis() { printf "  \033[0;33m!\033[0m %s\n" "$*"; }

existe() { az "$@" >/dev/null 2>&1; }

# ── 0. Comprobaciones previas ─────────────────────────────────────────────────
paso "Comprobando sesión y región"
az account show --query "{suscripcion:name, id:id}" -o tsv | sed 's/^/  /'

if ! az account list-locations --query "[?name=='$REGION'] | [0].name" -o tsv | grep -q .; then
  echo "La región '$REGION' no está disponible en esta suscripción." >&2
  exit 1
fi
ok "Región $REGION disponible"

# La región no es cosmética: la Política de Privacidad publicada declara que los datos se alojan en
# España. Crear el servidor en otra región convertiría ese documento en falso.
[ "$REGION" = "spaincentral" ] || avis "Región distinta de España: hay que corregir la Política de Privacidad y privacidad-datos.md"

# ── 1. Grupo de recursos ──────────────────────────────────────────────────────
paso "Grupo de recursos"
if existe group show --name "$GRUPO"; then
  ok "$GRUPO ya existe"
else
  az group create --name "$GRUPO" --location "$REGION" -o none
  ok "$GRUPO creado"
fi

# ── 2. PostgreSQL ─────────────────────────────────────────────────────────────
paso "PostgreSQL Flexible Server"
if existe postgres flexible-server show --resource-group "$GRUPO" --name "$PG"; then
  ok "$PG ya existe"
else
  if [ -z "${PG_PASSWORD:-}" ]; then
    echo "Define PG_PASSWORD con la contraseña del administrador antes de ejecutar." >&2
    echo "Guárdala donde guardes las credenciales: no se puede recuperar después." >&2
    exit 1
  fi
  az postgres flexible-server create \
    --resource-group "$GRUPO" --name "$PG" --location "$REGION" \
    --admin-user "$PG_USUARIO" --admin-password "$PG_PASSWORD" \
    --sku-name "$SKU_PG" --tier Burstable \
    --storage-size 32 --version 16 \
    --database-name "$PG_BD" \
    --public-access None \
    --yes -o none
  ok "$PG creado con la base $PG_BD"
fi

# Deja entrar al App Service sin abrir el servidor a Internet: la regla 0.0.0.0 es la forma que tiene
# Azure de decir «solo servicios de Azure», no «todo el mundo».
if existe postgres flexible-server firewall-rule show \
     --resource-group "$GRUPO" --name "$PG" --rule-name PermitirServiciosAzure; then
  ok "Acceso desde servicios de Azure ya permitido"
else
  az postgres flexible-server firewall-rule create \
    --resource-group "$GRUPO" --name "$PG" --rule-name PermitirServiciosAzure \
    --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none
  ok "Acceso desde servicios de Azure permitido"
fi

# ── 3. App Service ────────────────────────────────────────────────────────────
paso "App Service (API)"
if existe appservice plan show --resource-group "$GRUPO" --name "$PLAN"; then
  ok "$PLAN ya existe"
else
  az appservice plan create \
    --resource-group "$GRUPO" --name "$PLAN" --location "$REGION" \
    --is-linux --sku "$SKU_API" -o none
  ok "$PLAN creado ($SKU_API Linux)"
fi

if existe webapp show --resource-group "$GRUPO" --name "$API"; then
  ok "$API ya existe"
else
  az webapp create \
    --resource-group "$GRUPO" --plan "$PLAN" --name "$API" \
    --runtime "DOTNETCORE:9.0" -o none
  ok "$API creado"
fi

az webapp update --resource-group "$GRUPO" --name "$API" --https-only true -o none
az webapp config set --resource-group "$GRUPO" --name "$API" --min-tls-version 1.2 -o none
ok "HTTPS obligatorio y TLS mínimo 1.2"

# ── 4. Static Web App ─────────────────────────────────────────────────────────
paso "Static Web App (cliente)"
if existe staticwebapp show --resource-group "$GRUPO" --name "$WEB"; then
  ok "$WEB ya existe"
else
  # Sin `--source`: el despliegue lo hace el workflow de este repositorio. Si se conectara aquí a
  # GitHub, Azure crearía su propio workflow y habría dos despliegues compitiendo.
  az staticwebapp create \
    --resource-group "$GRUPO" --name "$WEB" --location "westeurope" --sku "$SKU_WEB" -o none
  ok "$WEB creado ($SKU_WEB)"
  avis "Static Web Apps no está en Spain Central; el contenido es estático y público, sin dato personal"
fi

# ── 5. Lo que hay que hacer a mano ────────────────────────────────────────────
paso "Datos para los siguientes pasos"

ASUID=$(az webapp show --resource-group "$GRUPO" --name "$API" \
  --query customDomainVerificationId -o tsv)
WEB_HOST=$(az staticwebapp show --resource-group "$GRUPO" --name "$WEB" \
  --query defaultHostname -o tsv)

cat <<FIN

  Crea estos registros DNS en el proveedor de $DOMINIO:

    api            CNAME   $API.azurewebsites.net
    asuid.api      TXT     $ASUID
    app            CNAME   $WEB_HOST

  Cuando hayan propagado (compruébalo con: dig +short api.$DOMINIO), ejecuta:

    ./enlazar-dominios.sh

  Y después, para volcar la configuración del App Service:

    ./configurar-api.sh

FIN

paso "Infraestructura creada"
