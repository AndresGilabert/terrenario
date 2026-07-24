---
bloque: 05-infraestructura
documento: desarrollo-local
actualizado_en: "2026-07-24"
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

Los valores no secretos están en [`appsettings.json`](../../src/backend/Terrenario.Api/appsettings.json) y los overrides de desarrollo en [`appsettings.Development.json`](../../src/backend/Terrenario.Api/appsettings.Development.json).

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
  }
}
```

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

### Tabla `usuarios` (MVP-101)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador interno |
| `google_sub` | TEXT UNIQUE NOT NULL | Subject de Google OIDC |
| `nombre` | TEXT NOT NULL | Nombre visible |
| `email` | TEXT NOT NULL | Email de Google |
| `activo` | BOOLEAN NOT NULL | Usuario habilitado |
| `creado_en` | TIMESTAMPTZ | Fecha de creación |
| `actualizado_en` | TIMESTAMPTZ | Última actualización |

### Tabla `refresh_tokens` (MVP-101)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador |
| `token_hash` | TEXT UNIQUE NOT NULL | SHA-256 del token |
| `usuario_id` | UUID FK → usuarios | Usuario propietario |
| `expires_at` | TIMESTAMPTZ | Expiración (30 días) |
| `revocado_en` | TIMESTAMPTZ? | Fecha de revocación |
| `creado_en` | TIMESTAMPTZ | Fecha de emisión |

### Tabla `workspaces` (MVP-102)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador del Workspace |
| `owner_id` | UUID FK → usuarios | Usuario creador |
| `nombre` | VARCHAR(120) NOT NULL | Nombre de la explotación |
| `creado_en` | TIMESTAMPTZ | Fecha de creación |
| `actualizado_en` | TIMESTAMPTZ | Última actualización |

### Tabla `usuarios_workspace` (MVP-102)

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `id` | UUID PK | Identificador de la membresía |
| `workspace_id` | UUID FK → workspaces | Workspace al que pertenece |
| `usuario_id` | UUID FK → usuarios | Usuario miembro |
| `rol` | VARCHAR(50) NOT NULL | `workspace_owner` o `workspace_member` |
| `activo` | BOOLEAN NOT NULL | Membresía vigente |
| `unido_en` | TIMESTAMPTZ | Fecha de alta en el Workspace |

Índice único `(workspace_id, usuario_id)`: un usuario no puede tener dos membresías del mismo Workspace.

---

## Ejecución de tests

```bash
cd src/backend

# Ejecutar todos los tests con output detallado
dotnet test --logger "console;verbosity=normal"

# Solo los tests de auth
dotnet test --filter "FullyQualifiedName~Auth"

# Solo los tests de workspaces
dotnet test --filter "FullyQualifiedName~Workspaces"
```

Cobertura actual: **30 tests** en 6 suites

| Suite | Tests | Qué cubre |
|-------|-------|-----------|
| `ExchangeGoogleCodeHandlerTests` | 6 | Flujo OAuth: nuevo usuario, usuario existente, error Google, telemetría, sesión con y sin Workspace |
| `RefreshTokenHandlerTests` | 5 | Rotación de refresh token y conservación del Workspace activo |
| `JwtServiceTests` | 4 | JWT RS256: emisión, validación, token inválido, clave diferente |
| `WorkspaceTests` | 8 | Invariantes del agregado Workspace y membresía del creador |
| `CreateWorkspaceHandlerTests` | 4 | Alta de Workspace, membresía vinculada y reemisión de sesión |
| `ActiveWorkspaceResolverTests` | 4 | Resolución del Workspace activo y caídas al valor por defecto |

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
| Entornos y secretos | [`docs/05-infraestructura/entornos.md`](./entornos.md) |
