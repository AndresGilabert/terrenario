# Terrenario — Tu tierra, bajo control

Plataforma de gestión agrícola para el agricultor moderno. Gestiona terrenos, cosechas y tareas diarias desde un único workspace.

> Para contexto de producto y arquitectura: [`docs/01-producto/vision-y-objetivos.md`](./docs/01-producto/vision-y-objetivos.md)  
> Para agentes de IA y reglas de la KB: [`AGENTS.md`](./AGENTS.md)

---

## Requisitos previos

| Herramienta | Versión mínima | Notas |
|-------------|---------------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0 | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 20 LTS | `node --version` |
| [PostgreSQL](https://www.postgresql.org/download/) | 15 | Local o Docker |
| [Git](https://git-scm.com/) | 2.x | - |
| Cuenta Google Cloud | — | Para OAuth 2.0 |

---

## 1. Clonar el repositorio

```bash
git clone https://github.com/AndresGilabert/terrenario.git
cd terrenario
git checkout develop
```

---

## 2. Configurar Google OAuth 2.0

El flujo de autenticación requiere un proyecto en Google Cloud con una aplicación OAuth 2.0 configurada.

### 2.1 Crear la aplicación en Google Cloud Console

1. Entra en [Google Cloud Console](https://console.cloud.google.com/).
2. Crea un proyecto o selecciona uno existente.
3. Ve a **APIs y servicios → Credenciales → Crear credenciales → ID de cliente OAuth 2.0**.
4. Tipo de aplicación: **Aplicación web**.
5. Configura los **Orígenes de JavaScript autorizados**:

   ```text
   http://localhost:5173
   ```

6. Configura los **URIs de redireccionamiento autorizados**:

   ```text
   http://localhost:5173/auth/callback
   ```

7. Guarda. Obtendrás un **Client ID** y un **Client Secret** — los necesitarás en los pasos siguientes.

---

## 3. Generar par de claves RSA (para JWT RS256)

El backend firma los tokens JWT con RSA-256. Genera un par de claves en local:

**macOS / Linux:**

```bash
# Clave privada (2048 bits)
openssl genrsa -out jwt_private.pem 2048

# Clave pública
openssl rsa -in jwt_private.pem -pubout -out jwt_public.pem

# Ver el contenido (lo necesitarás en el siguiente paso)
cat jwt_private.pem
cat jwt_public.pem
```

**Windows (PowerShell):**

```powershell
# Requiere OpenSSL instalado (incluido en Git for Windows)
$env:Path += ";C:\Program Files\Git\usr\bin"

openssl genrsa -out jwt_private.pem 2048
openssl rsa -in jwt_private.pem -pubout -out jwt_public.pem

Get-Content jwt_private.pem
Get-Content jwt_public.pem
```

> ⚠️ **No commitas estos archivos.** Son secretos de desarrollo local. El `.gitignore` ya excluye `*.pem`.

---

## 4. Configurar el backend

### 4.1 Base de datos PostgreSQL

Crea la base de datos de desarrollo:

```sql
-- Conéctate a tu instancia PostgreSQL
CREATE DATABASE terrenario_dev;
```

Con Docker (alternativa rápida):

```bash
docker run --name terrenario-pg \
  -e POSTGRES_DB=terrenario_dev \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:15
```

### 4.2 Secretos del backend (User Secrets)

Usa el sistema de secretos de .NET para **no poner credenciales en archivos de configuración**:

```bash
cd src/backend/Terrenario.Api

# Inicializar (si no existe ya un UserSecretsId)
dotnet user-secrets init

# Cadena de conexión PostgreSQL
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=terrenario_dev;Username=postgres;Password=postgres"

# Google OAuth 2.0
dotnet user-secrets set "Auth:Google:ClientId"     "TU_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Auth:Google:ClientSecret" "TU_GOOGLE_CLIENT_SECRET"

# JWT RSA — pega el contenido completo del PEM incluyendo cabecera/pie
dotnet user-secrets set "Auth:Jwt:PrivateKeyPem" "$(cat ../../../jwt_private.pem)"
dotnet user-secrets set "Auth:Jwt:PublicKeyPem"  "$(cat ../../../jwt_public.pem)"
```

**Windows (PowerShell):**

```powershell
cd src\backend\Terrenario.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Host=localhost;Database=terrenario_dev;Username=postgres;Password=postgres"

dotnet user-secrets set "Auth:Google:ClientId"     "TU_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Auth:Google:ClientSecret" "TU_GOOGLE_CLIENT_SECRET"

dotnet user-secrets set "Auth:Jwt:PrivateKeyPem" (Get-Content ..\..\..\jwt_private.pem -Raw)
dotnet user-secrets set "Auth:Jwt:PublicKeyPem"  (Get-Content ..\..\..\jwt_public.pem -Raw)
```

> Los secretos se almacenan en `%APPDATA%\Microsoft\UserSecrets\` (Windows) o `~/.microsoft/usersecrets/` (macOS/Linux) y **nunca se commitean**.

### 4.3 Verificar la configuración

```bash
dotnet user-secrets list
```

Debes ver las 5 claves configuradas.

---

## 5. Configurar el frontend

```bash
cd src/frontend/terrenario-web

# Crear el archivo de entorno local a partir del ejemplo
cp .env.example .env
```

Edita `.env` y rellena los valores:

```dotenv
# URL del backend (puerto del perfil "http" en launchSettings.json)
VITE_API_BASE_URL=http://localhost:5127

# El mismo Client ID de Google que configuraste en el backend
VITE_GOOGLE_CLIENT_ID=TU_GOOGLE_CLIENT_ID.apps.googleusercontent.com
```

---

## 6. Arrancar los servicios

### 6.1 Backend

```bash
cd src/backend/Terrenario.Api
dotnet run
```

Al arrancar en modo Development, el backend:

- Aplica la migración de base de datos automáticamente (crea las tablas `usuarios` y `refresh_tokens`)
- Expone la API en `http://localhost:5127`
- Expone OpenAPI en `http://localhost:5127/openapi/v1.json`

Verificar que funciona:

```bash
curl http://localhost:5127/api/v1/auth/me
# Espera: 401 Unauthorized (correcto — no hay token)
```

### 6.2 Frontend

```bash
cd src/frontend/terrenario-web
npm install
npm run dev
```

Accede a `http://localhost:5173` — verás la landing page de Terrenario.

---

## 7. Validación funcional de MVP-101

Con ambos servicios corriendo, puedes validar el flujo completo de autenticación:

### Flujo normal

1. Ve a `http://localhost:5173`
2. Haz clic en **Empezar gratis con Google** o **Ingresar**
3. En la pantalla de login, haz clic en **Continuar con Google**
4. Autoriza el acceso en la pantalla de Google (usará tu cuenta real)
5. Serás redirigido a `http://localhost:5173/app` con el mensaje de bienvenida

### Flujo de cierre de sesión

1. Haz clic en **Cerrar sesión**
2. Serás redirigido a la landing page
3. Al intentar acceder a `http://localhost:5173/app` directamente, serás redirigido a `/login`

### Verificar el access token (DevTools)

- Abre DevTools → Application → Session Storage → `http://localhost:5173`
- Verás la clave `terrenario_at` con el JWT
- En `https://jwt.io` puedes decodificarlo y verificar `sub`, `iss`, `aud`, `exp`

### Verificar el refresh token (DevTools)

- DevTools → Application → Cookies → `http://localhost:5127`
- Verás la cookie `refresh_token` con las flags `HttpOnly` y `SameSite=Strict`

### Endpoint `/me` con token

```bash
# Copia el access_token de la Session Storage
curl -H "Authorization: Bearer TU_ACCESS_TOKEN" \
  http://localhost:5127/api/v1/auth/me
# Respuesta: {"id":"...","display_name":"Tu Nombre"}
```

---

## 8. Ejecutar los tests

```bash
cd src/backend
dotnet test
# Resultado esperado: 12/12 tests pasando
```

---

## 9. Estructura del proyecto

```text
terrenario/
├── docs/                    Base de conocimiento (KB)
│   ├── 00-meta/             Convenciones, plantillas, scripts
│   ├── 01-producto/         Visión, stakeholders, roadmap
│   ├── 02-arquitectura/     C4, ADRs, contratos API
│   ├── 04-ingenieria/       Estándares, Git flow, testing
│   ├── 05-infraestructura/  Entornos, CI/CD
│   ├── 07-seguridad/        Modelo de seguridad, autenticación
│   └── 09-desarrollos/      Épicas e historias activas
├── src/
│   ├── backend/
│   │   ├── Terrenario.Api/          API .NET 9 (ASP.NET Core)
│   │   └── Terrenario.Api.Tests/    Tests unitarios (xUnit)
│   └── frontend/
│       └── terrenario-web/          React 19 + TypeScript + Vite
└── prototype/               Prototipos de diseño (referencia)
```

---

## 10. Solución de problemas frecuentes

### Error: "REPLACE_IN_SECRETS" al arrancar el backend

Los User Secrets no están configurados. Repasa el **paso 4.2**.

### Error: "redirect_uri_mismatch" en Google

La URL de callback no coincide con lo configurado en Google Cloud Console. Verifica que `http://localhost:5173/auth/callback` está en los **URIs de redireccionamiento autorizados**.

### Error: "Error establishing a database connection"

PostgreSQL no está corriendo o la cadena de conexión es incorrecta. Verifica con `psql -h localhost -U postgres -d terrenario_dev`.

### Error al aplicar la migración: "role does not exist"

El usuario de PostgreSQL configurado no existe. Ajusta el `Username` y `Password` en los User Secrets para usar un usuario que exista en tu instancia.

### El frontend no puede llamar al backend (error CORS / Network)

Verifica que `VITE_API_BASE_URL` en `.env` apunta al puerto correcto (`5127`) y que el backend está corriendo.

### Error TypeScript en el frontend

```bash
cd src/frontend/terrenario-web
npx tsc --noEmit
```

---

## Documentación adicional

| Recurso | Ruta |
|---------|------|
| Agentes de IA / Copilot | [`AGENTS.md`](./AGENTS.md) |
| Flujo de Git | [`docs/04-ingenieria/flujo-git.md`](./docs/04-ingenieria/flujo-git.md) |
| Estándares de código | [`docs/04-ingenieria/estandares-codigo.md`](./docs/04-ingenieria/estandares-codigo.md) |
| Arquitectura del sistema | [`docs/02-arquitectura/vision-general.md`](./docs/02-arquitectura/vision-general.md) |
| Modelo de seguridad | [`docs/07-seguridad/modelo-seguridad.md`](./docs/07-seguridad/modelo-seguridad.md) |
| Autenticación / OIDC | [`docs/07-seguridad/autenticacion-autorizacion.md`](./docs/07-seguridad/autenticacion-autorizacion.md) |
| Setup detallado de entorno | [`docs/05-infraestructura/desarrollo-local.md`](./docs/05-infraestructura/desarrollo-local.md) |
| Historia MVP-101 (spec) | [`docs/09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-101--google-oidc-y-sesion-base/spec.md`](./docs/09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-101--google-oidc-y-sesion-base/spec.md) |
| Tech design MVP-101 | [`docs/09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-101--google-oidc-y-sesion-base/tech-design.md`](./docs/09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-101--google-oidc-y-sesion-base/tech-design.md) |
