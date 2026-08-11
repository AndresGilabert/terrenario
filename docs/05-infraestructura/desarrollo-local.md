---
bloque: 05-infraestructura
documento: desarrollo-local
actualizado_en: "2026-08-10"
---

# Desarrollo Local

Guía detallada para configurar y arrancar el entorno de desarrollo de Terrenario en local.
Para una versión compacta (quick start), consulta el [`README.md`](../../README.md) del repositorio.

---

## Stack tecnológico

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Backend API | .NET / ASP.NET Core Web API | 9.0 (target: net9.0) |
| ORM | Entity Framework Core + Npgsql | 9.x |
| Base de datos | PostgreSQL | 15+ |
| Frontend | React + TypeScript + Vite | React 19, TS 6, Vite 8 |
| CSS | Tailwind CSS | 4.x (plugin Vite) |
| Autenticación | Google OIDC + JWT RS256 | — |
| Email transaccional | SMTP genérico (MailKit) | 4.x |

---

## Variables de configuración

### Backend (`Terrenario.Api`)

Gestionadas con **dotnet User Secrets** en local (nunca en archivos commiteados).

| Clave | Descripción | Ejemplo |
|-------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión PostgreSQL | `Host=localhost;Database=terrenario_dev;Username=postgres;Password=postgres` |
| `Auth:Google:ClientId` | Client ID de Google OAuth 2.0 | `123456789.apps.googleusercontent.com` |
| `Auth:Google:ClientSecret` | Client Secret de Google OAuth 2.0 | `GOCSPX-...` |
| `Auth:Jwt:PrivateKeyPem` | Clave privada RSA en formato PEM | `-----BEGIN RSA PRIVATE KEY-----\n...` |
| `Auth:Jwt:PublicKeyPem` | Clave pública RSA en formato PEM | `-----BEGIN PUBLIC KEY-----\n...` |
| `Email:Host` | Servidor SMTP de la cuenta de envío | `sandbox.smtp.mailtrap.io` |
| `Email:Username` | Usuario de autenticación SMTP | `a1b2c3d4e5f6a7` |
| `Email:Password` | Contraseña de la cuenta SMTP de envío | contraseña de aplicación |
| `Email:FromAddress` | Remitente de las invitaciones | `no-reply@terrenario.com` |

Los valores no secretos están en [`appsettings.json`](../../src/backend/Terrenario.Api/appsettings.json) y los overrides de desarrollo en [`appsettings.Development.json`](../../src/backend/Terrenario.Api/appsettings.Development.json).

> **El repositorio es público.** Las claves de la cuenta de envío (`Email:Host`, `Email:Username`,
> `Email:FromAddress`) van a User Secrets junto con la contraseña, aunque en sí mismas no sean
> secretos: identifican una cuenta concreta de un servicio de terceros y, una vez commiteadas,
> quedan en el historial de git de forma permanente. En `appsettings.json` se quedan vacías a
> propósito: definen la forma de la sección, no sus valores.

#### Valores por defecto (no secretos)

```json
{
  "Auth": {
    "Jwt": {
      "Issuer": "terrenario-api",
      "Audience": "terrenario-web",
      "AccessTokenLifetimeSeconds": 900
    },
    "RefreshToken": {
      "LifetimeSeconds": 2592000
    }
  },
  "Invitations": {
    "LifetimeDays": 7,
    "AcceptBaseUrl": "http://localhost:5173/invitations"
  },
  "Email": {
    "Host": "",
    "Port": 587,
    "SecurityMode": "starttls",
    "Username": "",
    "FromAddress": "",
    "FromName": "Terrenario",
    "TimeoutSeconds": 15
  }
}
```

> `Invitations:AcceptBaseUrl` es la base pública del enlace de invitación (MVP-103); el backend le
> añade `/{token}`. En local apunta al Vite del frontend.

### Frontend (`terrenario-web`)

Gestionadas con archivo `.env` local (excluido por `.gitignore`).

| Variable | Descripción | Valor local |
|----------|-------------|-------------|
| `VITE_API_BASE_URL` | URL base del backend | `http://localhost:5127` |
| `VITE_GOOGLE_CLIENT_ID` | Client ID de Google (igual que el del backend) | `123456789.apps.googleusercontent.com` |

> El puerto `5127` corresponde al perfil `http` de [`launchSettings.json`](../../src/backend/Terrenario.Api/Properties/launchSettings.json).

---

## Puertos y URLs locales

| Servicio | URL | Notas |
|---------|-----|-------|
| Backend API | `http://localhost:5127` | Perfil `http` de launchSettings |
| Backend API (HTTPS) | `https://localhost:7267` | Perfil `https` de launchSettings |
| OpenAPI (Swagger JSON) | `http://localhost:5127/openapi/v1.json` | Solo en Development |
| Frontend Vite | `http://localhost:5173` | Puerto por defecto de Vite |

---

## Configuración de Google OAuth 2.0

### Crear el proyecto y la credencial

1. Accede a [Google Cloud Console](https://console.cloud.google.com/).
2. Crea un nuevo proyecto (p. ej. `terrenario-dev`) o selecciona uno existente.
3. Activa la **API de Google+ / People API** si se solicita.
4. Ve a **APIs y servicios → Pantalla de consentimiento OAuth**:
   - Tipo de usuario: **Externo**
   - Nombre de la aplicación: `Terrenario (dev)`
   - Correo de soporte: el tuyo
   - Agrega tu correo en **Usuarios de prueba** (mientras la app no esté verificada, solo los usuarios de prueba pueden autenticarse)
5. Ve a **APIs y servicios → Credenciales → Crear credenciales → ID de cliente OAuth 2.0**:
   - Tipo: **Aplicación web**
   - Nombre: `terrenario-web-local`
   - Orígenes de JavaScript autorizados: `http://localhost:5173`
   - URIs de redireccionamiento autorizados: `http://localhost:5173/auth/callback`
6. Guarda y copia el **Client ID** y el **Client Secret**.

### Consideraciones para pruebas funcionales

- Durante el desarrollo, la app estará en modo "Pruebas". Solo los emails añadidos como **usuarios de prueba** en la pantalla de consentimiento pueden autenticarse.
- Añade los emails del equipo como usuarios de prueba.
- La pantalla de consentimiento mostrará un aviso de "app no verificada" — es normal en desarrollo. Haz clic en "Continuar" para proceder.

---

## Generación de claves RSA

El backend firma los JWT con RSA-256. Cada entorno tiene su propio par de claves.

```bash
# Generar clave privada (PKCS#1, 2048 bits)
openssl genrsa -out jwt_private.pem 2048

# Extraer la clave pública
openssl rsa -in jwt_private.pem -pubout -out jwt_public.pem
```

> Las claves generadas son solo para el entorno local. Cada entorno (dev, staging, prod) debe tener sus propias claves generadas de forma independiente y almacenadas en el gestor de secretos correspondiente.

---

## Cuenta de envío de emails (invitaciones)

> Decisión completa y alternativas descartadas:
> [ADR-0010](../02-arquitectura/decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md).

Las invitaciones de MVP-103 se envían por **SMTP genérico**, así que la misma configuración sirve
para Google Workspace, Brevo, Amazon SES, SendGrid, Mailgun o un servidor corporativo.

### Comportamiento sin cuenta configurada

Por defecto `Email:Host` y `Email:FromAddress` están vacíos. En ese estado:

1. El backend arranca con un warning que lo advierte.
2. Las invitaciones se emiten con normalidad y son válidas.
3. La API responde `email_sent: false` y la interfaz ofrece el enlace para compartirlo a mano.

No es un fallo: es el modo previsto mientras no haya cuenta contratada. Lo que **no** ocurre es dar
por enviado un correo que nunca salió.

### Opción recomendada en local: bandeja de pruebas (Mailtrap)

Un sandbox SMTP captura los correos en una bandeja web sin entregarlos a nadie. Es lo que conviene
en desarrollo: se ve el correo real —asunto, maquetación, enlace— sin riesgo de escribir a una
persona por error. Las credenciales de la bandeja las da el propio servicio:

```bash
cd src/backend/Terrenario.Api && dotnet user-secrets set "Email:Host" "sandbox.smtp.mailtrap.io" && dotnet user-secrets set "Email:Username" "TU_USUARIO_DE_BANDEJA" && dotnet user-secrets set "Email:Password" "TU_CONTRASENA_DE_BANDEJA" && dotnet user-secrets set "Email:FromAddress" "no-reply@terrenario.com"
```

`Port` (587) y `SecurityMode` (`starttls`) ya vienen bien por defecto desde `appsettings.json`. Como
la bandeja no entrega nada fuera, `FromAddress` puede ser el remitente definitivo sin haber
verificado todavía el dominio.

Alternativa sin cuenta externa: un servidor SMTP de pruebas local (`smtp4dev`, MailHog o Papercut)
en `localhost:1025` con `Email:SecurityMode` a `none`.

> `SecurityMode: none` solo es aceptable contra un servidor de pruebas en `localhost`. En cualquier
> entorno real se usa `starttls` (puerto 587) o `ssl` (puerto 465).

### Enviar a buzones reales desde local

Solo si necesitas comprobar la entrega de verdad. Con Gmail o Google Workspace:

```bash
cd src/backend/Terrenario.Api && dotnet user-secrets set "Email:Host" "smtp.gmail.com" && dotnet user-secrets set "Email:Username" "tu-cuenta@gmail.com" && dotnet user-secrets set "Email:Password" "CONTRASENA_DE_APLICACION" && dotnet user-secrets set "Email:FromAddress" "tu-cuenta@gmail.com"
```

Hace falta **verificación en dos pasos activa** y una **contraseña de aplicación**: la contraseña
normal de la cuenta no funciona por SMTP. La cuenta tiene además un límite de envío diario, así que
sirve para desarrollo pero no para producción.

### Comprobar qué cuenta está activa

`dotnet user-secrets list` vuelca los valores en claro por consola, incluidos la clave privada JWT y
la contraseña de base de datos. Para ver solo qué claves de email hay configuradas:

```bash
cd src/backend/Terrenario.Api && dotnet user-secrets list | grep -o "^Email:[A-Za-z]*"
```

Si el backend arranca con el warning de "sin cuenta de envío", falta `Email:Host` o
`Email:FromAddress`.

### Antes de producción

El remitente definitivo **está pendiente de decisión de negocio** (ADR-0010). Para producción hace
falta un dominio propio con **SPF**, **DKIM** y **DMARC** publicados en su DNS; sin esa alineación
las invitaciones acaban en spam. El proveedor que se contrate es **encargado del tratamiento** a
efectos de RGPD y requiere su DPA firmado.

---

## Ejecución de migraciones de base de datos

En modo Development, el backend aplica las migraciones automáticamente al arrancar (`await db.Database.MigrateAsync()`). Las tablas se crean si no existen.

Para aplicar migraciones manualmente o generar nuevas:

```bash
cd src/backend

# Aplicar migraciones pendientes
dotnet ef database update --project Terrenario.Api

# Crear una nueva migración
dotnet ef migrations add NombreMigracion --project Terrenario.Api

# Ver el SQL que se aplicará (sin ejecutar)
dotnet ef migrations script --project Terrenario.Api
```

Requiere la herramienta `dotnet-ef`:

```bash
dotnet tool install --global dotnet-ef
```

---

## Esquema de base de datos

> Los identificadores están en inglés desde MVP-102, según
> [ADR-0009](../02-arquitectura/decisiones/ADR-0009--idioma-de-identificadores-en-codigo.md).
> La migración `AdoptEnglishIdentifiersAndAddWorkspaces` renombra el esquema de MVP-101 sin
> destruir datos, así que basta con aplicarla sobre una base de datos existente.

### Tabla `users` (MVP-101)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador interno |
| `google_sub` | TEXT UNIQUE NOT NULL | Subject de Google OIDC |
| `display_name` | TEXT NOT NULL | Nombre visible |
| `email` | TEXT NOT NULL | Email de Google |
| `is_active` | BOOLEAN NOT NULL | Usuario habilitado |
| `created_at` | TIMESTAMPTZ | Fecha de creación |
| `updated_at` | TIMESTAMPTZ | Última actualización |

### Tabla `refresh_tokens` (MVP-101)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador |
| `token_hash` | TEXT UNIQUE NOT NULL | SHA-256 del token |
| `user_id` | UUID FK → users | Usuario propietario |
| `expires_at` | TIMESTAMPTZ | Expiración (30 días) |
| `revoked_at` | TIMESTAMPTZ? | Fecha de revocación |
| `created_at` | TIMESTAMPTZ | Fecha de emisión |

### Tabla `workspaces` (MVP-102)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador del Workspace |
| `owner_id` | UUID FK → users | Usuario creador |
| `name` | VARCHAR(120) NOT NULL | Nombre de la explotación |
| `created_at` | TIMESTAMPTZ | Fecha de creación |
| `updated_at` | TIMESTAMPTZ | Última actualización |

### Tabla `workspace_members` (MVP-102)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador de la membresía |
| `workspace_id` | UUID FK → workspaces | Workspace al que pertenece |
| `user_id` | UUID FK → users | Usuario miembro |
| `role` | VARCHAR(50) NOT NULL | `workspace_owner` o `workspace_member` |
| `is_active` | BOOLEAN NOT NULL | Membresía vigente |
| `joined_at` | TIMESTAMPTZ | Fecha de alta en el Workspace |

Índice único `(workspace_id, user_id)`: un usuario no puede tener dos membresías del mismo Workspace.

### Tabla `workspace_invitations` (MVP-103)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador de la invitación |
| `workspace_id` | UUID FK → workspaces | Workspace al que se invita |
| `invited_by_user_id` | UUID FK → users | Miembro que emite la invitación |
| `channel` | VARCHAR(20) NOT NULL | `email` o `enlace` |
| `email` | VARCHAR(320)? | Destinatario, solo en el canal `email` |
| `token_hash` | TEXT UNIQUE NOT NULL | SHA-256 del token; el enlace en claro no se guarda |
| `status` | VARCHAR(20) NOT NULL | `pendiente` o `aceptada` |
| `expires_at` | TIMESTAMPTZ | Caducidad (7 días por defecto) |
| `created_at` | TIMESTAMPTZ | Fecha de emisión |
| `accepted_at` | TIMESTAMPTZ? | Fecha de aceptación |
| `accepted_by_user_id` | UUID? FK → users | Quién aceptó la invitación |

Índice de apoyo `(workspace_id, status)` para el listado de invitaciones pendientes.

---

## Ejecución de tests

### Qué exige el entorno (riesgo aceptado de `P-069`)

La suite de backend **no se puede ejecutar entera en cualquier máquina**. Dos requisitos, y conviene
conocerlos antes de dar por rota una rama:

- **Docker en marcha.** Los tests de integración levantan PostgreSQL con Testcontainers (`MVP-501`,
  decisión que cerró `P-031`). Sin Docker no arrancan; los unitarios sí.
- **Una política de Application Control permisiva.** `Testcontainers.PostgreSql.dll` es un ensamblado
  sin reputación establecida, y **Smart App Control de Windows lo bloquea**. Cuando ocurre, la suite
  pasa de verde a decenas de fallos **sin ningún cambio de código**, todos con la misma firma:

  ```text
  System.IO.FileLoadException ... Una directiva de Control de aplicaciones bloqueó este archivo (0x800711C7)
  ```

  Si todos los fallos son ese, no hay nada roto en el producto: es la política de la máquina. Se
  confirma en el registro de Code Integrity (eventos 3077/3118) y con
  `Get-CimInstance -ClassName Win32_DeviceGuard` (`VerifiedAndReputablePolicyState = 1` significa que
  está activo). Smart App Control **no admite exclusiones por fichero** y solo se puede desactivar de
  forma **irreversible**, así que no es una decisión de desarrollo: es del responsable de la máquina.

> **El entorno de referencia es el CI sobre Linux**, donde esta política no aplica y donde `ci.yml`
> ejecuta la suite completa. Lo que diga el CI manda sobre lo que diga una máquina de desarrollo
> Windows. El riesgo queda **aceptado y documentado** en el gate de `MVP-504`; no se revierte
> Testcontainers, porque hacerlo reabriría `P-031`.

### Comandos

```bash
cd src/backend

# Ejecutar todos los tests con output detallado
dotnet test --logger "console;verbosity=normal"

# Solo los tests de auth
dotnet test --filter "FullyQualifiedName~Auth"

# Solo los tests de workspaces
dotnet test --filter "FullyQualifiedName~Workspaces"

# Solo los tests de invitaciones
dotnet test --filter "FullyQualifiedName~Invitations"
```

Cobertura actual: **59 tests** en 10 suites

| Suite | Tests | Qué cubre |
|-------|-------|-----------|
| `ExchangeGoogleCodeHandlerTests` | 6 | Flujo OAuth: nuevo usuario, usuario existente, error Google, telemetría, sesión con y sin Workspace |
| `RefreshTokenHandlerTests` | 5 | Rotación de refresh token y conservación del Workspace activo |
| `JwtServiceTests` | 4 | JWT RS256: emisión, validación, token inválido, clave diferente |
| `WorkspaceTests` | 8 | Invariantes del agregado Workspace y membresía del creador |
| `CreateWorkspaceHandlerTests` | 4 | Alta de Workspace, membresía vinculada y reemisión de sesión |
| `ActiveWorkspaceResolverTests` | 4 | Resolución del Workspace activo y caídas al valor por defecto |
| `WorkspaceInvitationTests` | 14 | Invariantes de la invitación: canal, destinatario, caducidad y aceptación |
| `CreateInvitationHandlerTests` | 6 | Emisión por email y por enlace, sin cuenta configurada, fallo del proveedor y ya-es-miembro |
| `AcceptInvitationHandlerTests` | 6 | Membresía derivada, reemisión de sesión y rechazos por token, caducidad o Workspace |
| `InvitationEmailComposerTests` | 3 | Composición del correo: remitente, asunto, enlace y escapado de HTML |

---

## Flujo de autenticación PKCE (resumen técnico)

```text
Frontend                    Backend                     Google
   │                           │                           │
   │ 1. generateCodeVerifier() │                           │
   │ 2. generateCodeChallenge()│                           │
   │ 3. Redirect → Google      │                           │
   │ ──────────────────────────┼──────────────────────────►│
   │                           │                           │ 4. User consents
   │ ◄─────────────────────────┼───────────────────────────│
   │ 5. /auth/callback?code=X  │                           │
   │ 6. POST /api/v1/auth/google/callback                  │
   │ ──────────────────────────►│                           │
   │                           │ 7. Exchange code + verifier
   │                           │ ──────────────────────────►│
   │                           │ ◄──────────────────────────│
   │                           │ 8. Validate id_token       │
   │                           │ 9. Upsert usuario en DB    │
   │                           │ 10. Emit JWT + refresh token
   │ ◄─────────────────────────│                           │
   │ 11. access_token (JSON)   │                           │
   │ 12. refresh_token (cookie HttpOnly)                   │
```

---

## Trazabilidad

| Documento | Ruta |
|-----------|------|
| Arquitectura general | [`docs/02-arquitectura/vision-general.md`](../02-arquitectura/vision-general.md) |
| Modelo de seguridad | [`docs/07-seguridad/modelo-seguridad.md`](../07-seguridad/modelo-seguridad.md) |
| Autenticación OIDC | [`docs/07-seguridad/autenticacion-autorizacion.md`](../07-seguridad/autenticacion-autorizacion.md) |
| Tech design MVP-101 | [`docs/09-desarrollos/epicas/.../tech-design.md`](../09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-101--google-oidc-y-sesion-base/tech-design.md) |
| Tech design MVP-102 | [`docs/09-desarrollos/epicas/.../tech-design.md`](../09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-102--creacion-de-workspace-y-primer-acceso/tech-design.md) |
| Tech design MVP-103 | [`docs/09-desarrollos/epicas/.../tech-design.md`](../09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-103--invitaciones-por-email-y-enlace/tech-design.md) |
| Entornos y secretos | [`docs/05-infraestructura/entornos.md`](./entornos.md) |
| Riesgo de entorno de la suite (`P-069`) | [`docs/09-desarrollos/epicas/MVP-999--pendientes-transversales-y-diferidos/spec.md`](../09-desarrollos/epicas/MVP-999--pendientes-transversales-y-diferidos/spec.md) |
