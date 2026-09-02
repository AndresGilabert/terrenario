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

# En Windows el instalador no siempre deja `az` en el PATH de la sesion en curso. Permite
# apuntarlo sin editar el script: AZ="/c/Program Files/.../az.cmd" ./script.sh
AZ="${AZ:-az}"

# ── Parámetros ────────────────────────────────────────────────────────────────
DOMINIO="${DOMINIO:-terrenario.com}"
REGION="${REGION:-spaincentral}"
GRUPO="${GRUPO:-rg-terrenario-prod}"
PLAN="${PLAN:-plan-terrenario-prod}"
API="${API:-app-terrenario-api}"
PG="${PG:-psql-terrenario-prod}"
PG_USUARIO="${PG_USUARIO:-terrenario}"
PG_BD="${PG_BD:-terrenario}"

# El cliente lo sirve la propia API, así que no hay recurso de hosting estático: Azure Static Web
# Apps **no tiene región europea abierta a altas nuevas**, y servirlo desde EE. UU. haría falsas dos
# frases de la Política de Privacidad. Un solo origen, todo en Spain Central.
# App Service: `B1` es el escalón **mínimo con dominio propio y TLS**. `F1` y `D1` no valen: sin HTTPS
# la cookie de refresco no puede ser `Secure` y la sesión no funciona.
SKU_API="${SKU_API:-B1}"
# PostgreSQL: el escalón más barato. Si la suscripción es nueva, entra en los 12 meses gratuitos.
SKU_PG="${SKU_PG:-Standard_B1ms}"

paso() { printf "\n\033[1;36m▸ %s\033[0m\n" "$*"; }
ok()   { printf "  \033[0;32m✓\033[0m %s\n" "$*"; }
avis() { printf "  \033[0;33m!\033[0m %s\n" "$*"; }

existe() { "$AZ" "$@" >/dev/null 2>&1; }

# ── 0. Comprobaciones previas ─────────────────────────────────────────────────
paso "Comprobando sesión y región"
"$AZ" account show --query "{suscripcion:name, id:id}" -o tsv | sed 's/^/  /'

if ! "$AZ" account list-locations --query "[?name=='$REGION'] | [0].name" -o tsv | grep -q .; then
  echo "La región '$REGION' no está disponible en esta suscripción." >&2
  exit 1
fi
ok "Región $REGION disponible"

# La región no es cosmética: la Política de Privacidad publicada declara que los datos se alojan en
# España. Crear el servidor en otra región convertiría ese documento en falso.
[ "$REGION" = "spaincentral" ] || avis "Región distinta de España: hay que corregir la Política de Privacidad y privacidad-datos.md"

# ── 0 bis. Proveedores de recursos ────────────────────────────────────────────
#
# Una suscripción nueva no tiene registrados los proveedores hasta que se usan. Algunos comandos lo
# hacen solos —`webapp create` avisa y registra `Microsoft.Web`— pero **PostgreSQL no**: falla con
# `MissingSubscriptionRegistration` después de haber empezado a crear el servidor, que es el peor
# momento para enterarse. Registrarlos aquí cuesta segundos y es idempotente.
paso "Proveedores de recursos"
for ns in Microsoft.DBforPostgreSQL Microsoft.Web; do
  estado=$("$AZ" provider show --namespace "$ns" --query registrationState -o tsv 2>/dev/null || echo "NotRegistered")
  if [ "$estado" = "Registered" ]; then
    ok "$ns ya registrado"
  else
    "$AZ" provider register --namespace "$ns" --wait -o none
    ok "$ns registrado"
  fi
done

# ── 1. Grupo de recursos ──────────────────────────────────────────────────────
paso "Grupo de recursos"
if existe group show --name "$GRUPO"; then
  ok "$GRUPO ya existe"
else
  "$AZ" group create --name "$GRUPO" --location "$REGION" -o none
  ok "$GRUPO creado"
fi

# ── 2. PostgreSQL ─────────────────────────────────────────────────────────────
paso "PostgreSQL Flexible Server"
if existe postgres flexible-server show --resource-group "$GRUPO" --name "$PG"; then
  ok "$PG ya existe"
elif [ -n "${OMITIR_PG:-}" ]; then
  # `OMITIR_PG=1` deja fuera el único paso que necesita una contraseña, para que pueda ejecutarlo
  # quien la custodia mientras el resto del montaje lo hace otra persona. El script imprime al final
  # el comando exacto que falta.
  avis "PostgreSQL omitido (OMITIR_PG). El resto del montaje sigue."
  PG_PENDIENTE=1
else
  if [ -z "${PG_PASSWORD:-}" ]; then
    echo "Define PG_PASSWORD con la contraseña del administrador antes de ejecutar." >&2
    echo "Guárdala donde guardes las credenciales: no se puede recuperar después." >&2
    echo "O usa OMITIR_PG=1 para crear todo lo demás y dejar la base para después." >&2
    exit 1
  fi
  # Dos trampas de esta orden, ambas encontradas montándolo de verdad:
  #
  # 1. Sin `--database-name`: desde la CLI 2.89 ese parámetro **solo vale para clusters elásticos**
  #    («can only be used when --node-count is present») y hace fallar la creación entera. La base
  #    se crea aparte, justo debajo.
  # 2. `--public-access 0.0.0.0` y **no `None`**. Pese a lo que sugiere la ayuda, `None` deja el
  #    servidor con `publicNetworkAccess: Disabled`, y entonces ni el App Service puede conectarse
  #    ni se pueden crear reglas de cortafuegos: fallan con «not supported for a server without
  #    public access enabled». `0.0.0.0` es la forma que tiene Azure de decir «solo servicios de
  #    Azure», y de paso crea la regla.
  "$AZ" postgres flexible-server create \
    --resource-group "$GRUPO" --name "$PG" --location "$REGION" \
    --admin-user "$PG_USUARIO" --admin-password "$PG_PASSWORD" \
    --sku-name "$SKU_PG" --tier Burstable \
    --storage-size 32 --version 16 \
    --public-access 0.0.0.0 \
    --yes -o none
  ok "$PG creado"
fi

# Comprobación propia, no dentro del bloque anterior: el servidor puede existir de una pasada previa
# y la base no, que es justo lo que pasa cuando la creación se queda a medias.
if [ -n "${PG_PENDIENTE:-}" ]; then
  avis "Base de datos pendiente: se crea junto con el servidor"
elif existe postgres flexible-server db show \
     --resource-group "$GRUPO" --server-name "$PG" --name "$PG_BD"; then
  ok "Base $PG_BD ya existe"
else
  "$AZ" postgres flexible-server db create \
    --resource-group "$GRUPO" --server-name "$PG" --name "$PG_BD" -o none
  ok "Base $PG_BD creada"
fi

# Deja entrar al App Service sin abrir el servidor a Internet: la regla 0.0.0.0 es la forma que tiene
# Azure de decir «solo servicios de Azure», no «todo el mundo».
if [ -n "${PG_PENDIENTE:-}" ]; then
  avis "Regla de acceso pendiente: se crea al ejecutar el script con la base ya existente"
elif existe postgres flexible-server firewall-rule show \
     --resource-group "$GRUPO" --server-name "$PG" --name PermitirServiciosAzure; then
  ok "Acceso desde servicios de Azure ya permitido"
else
  "$AZ" postgres flexible-server firewall-rule create \
    --resource-group "$GRUPO" --server-name "$PG" --name PermitirServiciosAzure \
    --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none
  ok "Acceso desde servicios de Azure permitido"
fi

# ── 3. App Service ────────────────────────────────────────────────────────────
paso "App Service (API)"
if existe appservice plan show --resource-group "$GRUPO" --name "$PLAN"; then
  ok "$PLAN ya existe"
else
  "$AZ" appservice plan create \
    --resource-group "$GRUPO" --name "$PLAN" --location "$REGION" \
    --is-linux --sku "$SKU_API" -o none
  ok "$PLAN creado ($SKU_API Linux)"
fi

if existe webapp show --resource-group "$GRUPO" --name "$API"; then
  ok "$API ya existe"
else
  "$AZ" webapp create \
    --resource-group "$GRUPO" --plan "$PLAN" --name "$API" \
    --runtime "DOTNETCORE:9.0" -o none
  ok "$API creado"
fi

"$AZ" webapp update --resource-group "$GRUPO" --name "$API" --https-only true -o none
"$AZ" webapp config set --resource-group "$GRUPO" --name "$API" --min-tls-version 1.2 -o none
ok "HTTPS obligatorio y TLS mínimo 1.2"

# ── 4. Lo que hay que hacer a mano ────────────────────────────────────────────
paso "Datos para los siguientes pasos"

ASUID=$("$AZ" webapp show --resource-group "$GRUPO" --name "$API" \
  --query customDomainVerificationId -o tsv)

# PLT-101 — El dominio raíz (`terrenario.com`, `terrenario.es`) no admite CNAME: hace falta el registro
# `A` con la IP de entrada del App Service (o `ALIAS`/`ANAME` si el proveedor DNS lo soporta, que es
# preferible porque sigue la IP si Azure la cambia). `www` sí es CNAME, igual que `app`.
IP_ENTRADA=$("$AZ" webapp show --resource-group "$GRUPO" --name "$API" \
  --query inboundIpAddress -o tsv)

DOMINIOS_REDIRECCION="${DOMINIOS_REDIRECCION:-terrenario.com terrenario.es}"

cat <<FIN

  Crea estos registros DNS en el proveedor de $DOMINIO:

    app            CNAME   $API.azurewebsites.net
    asuid.app      TXT     $ASUID

  Y estos por cada dominio de redirección (PLT-101 — terrenario.com/.es y sus www, sin contenido
  propio, redirigen a app.$DOMINIO):
FIN

for dominio_redir in $DOMINIOS_REDIRECCION; do
  cat <<FIN

  Para $dominio_redir:
    @ (raíz)             A       $IP_ENTRADA
    asuid.$dominio_redir TXT     $ASUID
    www                  CNAME   $API.azurewebsites.net
    asuid.www.$dominio_redir TXT $ASUID
FIN
done

cat <<FIN

  Cuando hayan propagado (compruébalo con: dig +short app.$DOMINIO), ejecuta:

    ./enlazar-dominios.sh

  Y después, para volcar la configuración del App Service:

    ./configurar-api.sh

FIN

if [ -n "${PG_PENDIENTE:-}" ]; then
  cat <<FIN
  Falta la base de datos. Ejecútalo quien custodie la contraseña:

    PG_PASSWORD='...' AZ="\$AZ" OMITIR_PG= ./crear-infraestructura.sh

  Es idempotente: lo ya creado se salta y solo añade PostgreSQL y su regla de acceso.

FIN
fi

paso "Infraestructura creada"
