---
bloque: 05-infraestructura
documento: publicacion-inicial-en-azure
actualizado_en: "2026-08-04"
---

# Runbook — Publicación inicial en Azure

Todo lo que hay que crear y configurar **una sola vez** para que un tag `v*` publique el producto.
A partir de ahí publicar es poner un tag, y este documento solo se vuelve a abrir si algo cambia.

Sustituye `TUDOMINIO` por el dominio elegido en todos los pasos.

---

## 0. La decisión que condiciona el resto

**El cliente y la API tienen que compartir dominio registrable.** No es preferencia: la cookie de
refresco es `SameSite=Strict` (`AuthController`), y `algo.azurestaticapps.net` y
`algo.azurewebsites.net` son **sitios distintos** —los dos sufijos están en la Public Suffix List—,
así que el navegador no enviaría esa cookie al renovar la sesión.

El síntoma sería desagradable de diagnosticar: **todo funciona 15 minutos** —lo que dura el token de
acceso— y después la aplicación echa a la persona fuera sin ningún error visible.

Con `app.TUDOMINIO` y `api.TUDOMINIO` el problema no existe: distinto origen, mismo sitio.

---

## 1. Recursos que hay que crear

Todos en la región **España (Spain Central)**. No es cosmético: la Política de Privacidad publicada
declara que los datos se alojan en España, así que crear el servidor en otra región convertiría ese
documento en falso.

| # | Recurso | Nombre sugerido | Plan |
|---|---------|-----------------|------|
| 1 | Grupo de recursos | `rg-terrenario-prod` | — |
| 2 | Azure Database for PostgreSQL Flexible Server | `psql-terrenario-prod` | Burstable **B1ms**, 32 GB |
| 3 | App Service Plan (Linux) | `plan-terrenario-prod` | **B1** |
| 4 | App Service (.NET 9, Linux) | `app-terrenario-api` | en el plan anterior |
| 5 | Static Web App | `swa-terrenario-web` | **Standard** |

Dos avisos de plan, para que no haya sorpresas a mitad:

- **App Service B1 y no F1**: el plan gratuito **no admite dominio propio con TLS**, que es
  justamente lo que necesitamos por el paso 0.
- **Static Web Apps Standard y no Free**: el plan gratuito no permite enlazar la API ni da SLA. Para
  esta topología el Free serviría, pero deja sin margen el primer cambio.

### 1.1 PostgreSQL

Al crearlo:

- Autenticación: **solo PostgreSQL** (usuario y contraseña).
- Usuario administrador: `terrenario_admin`. **Guarda la contraseña**: se usa en el paso 3.
- Nombre de la base: `terrenario`.
- Versión: **16**.

Después, en **Redes** del servidor:

- Activa **«Permitir el acceso público desde servicios de Azure»**. Es lo que deja entrar al App
  Service sin abrir el servidor a Internet.
- **No** añadas reglas para tu IP salvo que vayas a consultar desde tu equipo; si lo haces,
  quítalas después.

En **Parámetros del servidor**, comprueba que `require_secure_transport` está en `ON` (viene así por
defecto). La cadena de conexión del paso 3 lo asume.

### 1.2 App Service

Al crearlo:

- Publicación: **Código**, pila **.NET 9 (STS)**, sistema **Linux**.
- En **Configuración → Configuración general**: **HTTPS Only = On** y **TLS mínimo 1.2**.
- Deja el resto por defecto.

### 1.3 Static Web App

- Origen del despliegue: **Otro** (no conectes GitHub aquí). El workflow de este repositorio ya se
  encarga; dejar que Azure cree el suyo duplicaría despliegues.
- Copia el **token de despliegue** (Información general → Administrar token de despliegue): va al
  paso 4.

---

## 2. Dominios y DNS

En el proveedor de `TUDOMINIO`:

| Registro | Tipo | Valor |
|----------|------|-------|
| `api` | CNAME | `app-terrenario-api.azurewebsites.net` |
| `asuid.api` | TXT | *(el «ID de verificación de dominio personalizado» del App Service)* |
| `app` | CNAME | *(el nombre que da Static Web Apps al añadir el dominio)* |

Después:

1. **App Service → Dominios personalizados → Agregar**: `api.TUDOMINIO`. Valida y, al terminar,
   **crea el certificado administrado** (gratuito) y enlázalo.
2. **Static Web App → Dominio personalizado → Agregar**: `app.TUDOMINIO`. El certificado lo emite y
   renueva Azure solo.

> La propagación DNS puede tardar. Hasta que `api.TUDOMINIO` responda con su certificado, no sigas:
> los pasos 3 y 4 dependen de esas URL.

---

## 3. Configuración del App Service

**Configuración → Variables de entorno → Configuración de la aplicación.** El doble guion bajo es la
forma de anidar secciones de configuración de .NET en una variable de entorno.

| Nombre | Valor |
|--------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | `Host=psql-terrenario-prod.postgres.database.azure.com;Database=terrenario;Username=terrenario_admin;Password=LA_CONTRASEÑA;SSL Mode=Require;Trust Server Certificate=true` |
| `Auth__Google__ClientId` | El identificador de cliente de Google |
| `Auth__Google__ClientSecret` | El secreto de cliente de Google |
| `Auth__Jwt__PrivateKeyPem` | La clave privada RSA en PEM (ver 3.1) |
| `Auth__Jwt__PublicKeyPem` | La clave pública RSA en PEM |
| `Cors__AllowedOrigins__0` | `https://app.TUDOMINIO` |
| `Invitations__AcceptBaseUrl` | `https://app.TUDOMINIO/invitations` |
| `WorkspaceLifecycle__ReactivationBaseUrl` | `https://app.TUDOMINIO/reactivations` |
| `Email__Host` | El SMTP de Arsys |
| `Email__Port` | `587` |
| `Email__Username` | La cuenta de envío |
| `Email__Password` | Su contraseña |
| `Email__FromAddress` | La dirección remitente |

**`Cors__AllowedOrigins__0` es el fallo más fácil de cometer**: el `__0` es obligatorio porque es una
lista. Sin él, la API rechaza al cliente y no funciona ninguna llamada, con un error de CORS que en el
navegador parece un problema de red.

Las **migraciones se aplican solas al arrancar** (`Database:MigrateOnStartup`, activo por defecto),
así que la base se crea con el primer despliegue. Si una migración falla, la aplicación no arranca:
es deliberado, mejor que servir peticiones contra un esquema equivocado.

### 3.1 Generar el par de claves RSA

En tu equipo:

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out jwt-private.pem && openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem
```

Pega el contenido **íntegro** de cada fichero, cabeceras `-----BEGIN…` incluidas. El portal admite
saltos de línea en el valor.

> Guarda la clave privada donde guardes las contraseñas y **bórrala del disco**. Quien la tenga puede
> emitir tokens válidos para cualquier cuenta.

---

## 4. Configuración de GitHub

**Settings → Environments → New environment → `produccion`.** Aquí, y no en el repositorio, porque
así puedes exigir aprobación manual antes de cada publicación (recomendado: marca *Required
reviewers* contigo mismo).

Secretos del entorno:

| Secreto | De dónde sale |
|---------|---------------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | App Service → Información general → **Descargar perfil de publicación**. Pega el XML entero |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | El token de despliegue del paso 1.3 |

Variables del entorno (no son secretos: acaban en el bundle público):

| Variable | Valor |
|----------|-------|
| `AZURE_WEBAPP_NAME` | `app-terrenario-api` |
| `VITE_API_BASE_URL` | `https://api.TUDOMINIO` |
| `VITE_GOOGLE_CLIENT_ID` | El identificador de cliente de Google |
| `PUBLIC_WEB_URL` | `https://app.TUDOMINIO` |

---

## 5. Google Cloud Console

En **APIs y servicios → Credenciales**, sobre el ID de cliente de OAuth existente:

- **Orígenes autorizados de JavaScript**: añade `https://app.TUDOMINIO`
- **URI de redirección autorizados**: añade `https://app.TUDOMINIO/auth/callback`

No borres las de `localhost`: se seguirán usando en desarrollo.

> Si la pantalla de consentimiento sigue en **modo de prueba**, solo entrarán las cuentas que
> figuren como usuarios de prueba. Para una validación con pocos usuarios eso vale, y evita la
> verificación de Google. Tenlo presente antes de repartir el enlace.

---

## 6. Publicar

Con todo lo anterior hecho:

```bash
git checkout main && git pull && git tag -a v0.5.0-hito-e -m "Release v0.5.0 — Hito E: salida controlada a MVP" && git push origin v0.5.0-hito-e
```

El workflow `deploy.yml` hace, por este orden:

1. **Comprueba que el gate está en verde** para ese commit. Si el CI falló, no publica: publicar un
   commit en rojo sería saltarse el gate de `MVP-504` por la puerta de atrás.
2. Publica la **API** y el **cliente** en paralelo.
3. **Smoke**: comprueba que la API responde con sus cabeceras de seguridad y que el cliente sirve el
   documento y resuelve una ruta del SPA (`/legal/privacidad`).

Si algo falla y lo arreglas, **no hace falta otro tag**: `Actions → Publicar → Run workflow` acepta
el tag como parámetro.

---

## 7. Comprobación manual después del primer despliegue

El smoke automático dice que responde, no que funcione. Esto hay que verlo con los ojos:

- [ ] `https://app.TUDOMINIO` carga y la Política de Privacidad se abre **sin iniciar sesión**
- [ ] El acceso con Google completa y entra en la aplicación
- [ ] **Esperar 16 minutos con la pestaña abierta y seguir dentro** — es la comprobación del paso 0,
      la que demuestra que la cookie de refresco viaja
- [ ] Crear un Workspace, registrar una labor y verla en el diario
- [ ] Enviar una invitación por correo y comprobar que llega
- [ ] En Ajustes → Privacidad, el inventario se ve completo
- [ ] En el registro del App Service aparece `Expurgo completado (RN-041)` a los ~5 minutos del
      arranque

---

## 8. Qué queda fuera de este runbook

- **Copias de seguridad**: PostgreSQL Flexible Server las hace solo (7 días de retención por
  defecto). Restaurarlas es otro procedimiento; ver [disaster-recovery.md](../disaster-recovery.md).
- **Observabilidad**: sin Application Insights, los registros son los del App Service y se consultan
  desde el portal. Suficiente para validar, corto para operar.
- **Entorno de `staging`**: este runbook monta producción directamente, que es la decisión tomada
  para la validación. Duplicar los recursos con sufijo `-stg` es el camino cuando haga falta.

---

## Trazabilidad KB

1. Gate de salida y criterios de promoción: [`../../08-procesos/gate-salida-mvp.md`](../../08-procesos/gate-salida-mvp.md)
2. Proceso de release y rollback: [`../../08-procesos/proceso-release.md`](../../08-procesos/proceso-release.md)
3. Entornos: [`../entornos.md`](../entornos.md)
4. Pipeline: [`../ci-cd.md`](../ci-cd.md)
