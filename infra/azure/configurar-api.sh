#!/usr/bin/env bash
#
# Vuelca la configuración del App Service.
#
# Por comandos y no por el portal por un motivo concreto: `Cors__AllowedOrigins__0` necesita el `__0`
# porque es una lista, y es el error más fácil de cometer a mano. Sin él la API rechaza al cliente y
# el navegador muestra un fallo de CORS que parece un problema de red.
#
# Los valores sensibles se leen del entorno, nunca de este fichero. Exporta lo que falte antes de
# ejecutar; el script te dice qué echa en falta en vez de escribir configuraciones a medias.

set -euo pipefail

DOMINIO="${DOMINIO:-terrenario.com}"
GRUPO="${GRUPO:-rg-terrenario-prod}"
API="${API:-app-terrenario-api}"
PG="${PG:-psql-terrenario-prod}"
PG_USUARIO="${PG_USUARIO:-terrenario_admin}"
PG_BD="${PG_BD:-terrenario}"

paso() { printf "\n\033[1;36m▸ %s\033[0m\n" "$*"; }
ok()   { printf "  \033[0;32m✓\033[0m %s\n" "$*"; }

# ── Comprobar que están todos los secretos ────────────────────────────────────
paso "Comprobando variables"
FALTAN=()
for v in PG_PASSWORD GOOGLE_CLIENT_ID GOOGLE_CLIENT_SECRET JWT_PRIVATE_PEM JWT_PUBLIC_PEM \
         EMAIL_HOST EMAIL_USERNAME EMAIL_PASSWORD EMAIL_FROM; do
  [ -n "${!v:-}" ] || FALTAN+=("$v")
done

if [ ${#FALTAN[@]} -gt 0 ]; then
  echo "  Faltan: ${FALTAN[*]}" >&2
  cat <<'FIN' >&2

  Genera el par de claves RSA con:
    openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out jwt-private.pem
    openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem

  Y expórtalas conservando los saltos de línea:
    export JWT_PRIVATE_PEM="$(cat jwt-private.pem)"
    export JWT_PUBLIC_PEM="$(cat jwt-public.pem)"

  Guarda la privada donde guardes las contraseñas y BÓRRALA del disco: quien la tenga puede
  emitir tokens válidos para cualquier cuenta.
FIN
  exit 1
fi
ok "Todas presentes"

CADENA="Host=$PG.postgres.database.azure.com;Database=$PG_BD;Username=$PG_USUARIO;Password=$PG_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

paso "Aplicando configuración a $API"
az webapp config appsettings set --resource-group "$GRUPO" --name "$API" -o none --settings \
  "ASPNETCORE_ENVIRONMENT=Production" \
  "ConnectionStrings__DefaultConnection=$CADENA" \
  "Auth__Google__ClientId=$GOOGLE_CLIENT_ID" \
  "Auth__Google__ClientSecret=$GOOGLE_CLIENT_SECRET" \
  "Auth__Jwt__PrivateKeyPem=$JWT_PRIVATE_PEM" \
  "Auth__Jwt__PublicKeyPem=$JWT_PUBLIC_PEM" \
  "Cors__AllowedOrigins__0=https://app.$DOMINIO" \
  "Invitations__AcceptBaseUrl=https://app.$DOMINIO/invitations" \
  "WorkspaceLifecycle__ReactivationBaseUrl=https://app.$DOMINIO/reactivations" \
  "Email__Host=$EMAIL_HOST" \
  "Email__Port=${EMAIL_PORT:-587}" \
  "Email__SecurityMode=starttls" \
  "Email__Username=$EMAIL_USERNAME" \
  "Email__Password=$EMAIL_PASSWORD" \
  "Email__FromAddress=$EMAIL_FROM" \
  "Email__FromName=Terrenario"

ok "Configuración aplicada"

paso "Comprobando lo que suele fallar"
az webapp config appsettings list --resource-group "$GRUPO" --name "$API" \
  --query "[?name=='Cors__AllowedOrigins__0'].value" -o tsv | sed 's/^/  CORS: /'

echo
echo "  El App Service se reinicia solo al cambiar la configuración."
echo "  Las migraciones se aplican en ese arranque (Database:MigrateOnStartup)."
echo "  Compruébalo con:"
echo "    az webapp log tail --resource-group $GRUPO --name $API"
