---
id: "MVP-101"
tipo: feature
titulo: "TDD: Acceso con Google OIDC y sesión base"
estado: en-progreso
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["autenticacion", "seguridad"]
  modulo_path: "03-modulos/"
  componentes: ["google-oidc", "sesion", "auth-api"]
  etiquetas: ["mvp", "auth", "oidc", "jwt", "pkce"]
  nivel_riesgo: alto
creado_en: "2026-07-21"
actualizado_en: "2026-07-21"
---

# TDD: MVP-101 — Acceso con Google OIDC y sesión base

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Implementación de la autenticación Google OIDC mediante el flujo Authorization Code + PKCE. El backend (.NET 10) valida el código con Google, persiste o vincula el usuario y emite un JWT RS256 de corta vida junto con un refresh token rotante en cookie HttpOnly. El frontend (React + TypeScript + Vite) gestiona el flujo PKCE y almacena el access token en memoria.

## Diagrama de arquitectura / flujo

```mermaid
sequenceDiagram
    participant U as Usuario (Browser)
    participant FE as Frontend (SPA)
    participant BE as Backend API (.NET 10)
    participant G as Google OIDC
    participant DB as PostgreSQL

    Note over FE: Genera code_verifier + code_challenge (PKCE SHA-256)
    FE->>FE: Guarda code_verifier + state en sessionStorage
    FE->>G: Redirige a accounts.google.com/o/oauth2/v2/auth<br/>?response_type=code&code_challenge=...&state=...
    G->>U: Pantalla de consentimiento Google
    U->>G: Acepta (o cancela)
    G->>FE: Redirect a /auth/callback?code=...&state=... (o ?error=access_denied)

    alt Login cancelado o error
        FE->>U: Muestra mensaje de error seguro (sin PII)
    else Login correcto
        FE->>BE: POST /api/v1/auth/google/callback<br/>{ code, redirect_uri, code_verifier }
        BE->>G: Intercambio code por tokens (code + code_verifier)
        G->>BE: { id_token, access_token }
        BE->>BE: Valida id_token (firma, iss, aud, exp)
        BE->>DB: UPSERT usuarios (google_sub → find or create)
        BE->>DB: INSERT refresh_tokens (token_hash, user_id, expires_at)
        BE->>FE: 200 { access_token (JWT RS256 15min), expires_in, user: { id, display_name } }<br/>Set-Cookie: refresh_token=...; HttpOnly; Secure; SameSite=Strict
        FE->>FE: Guarda access_token en AuthContext (memoria)
    end

    Note over FE,BE: Sesión activa — acceso a endpoints protegidos

    FE->>BE: GET /api/v1/auth/me (Authorization: Bearer <access_token>)
    BE->>BE: Valida JWT (firma RS256, exp, iss, aud)
    BE->>FE: 200 { id, display_name }

    Note over FE,BE: Renovación de token

    FE->>BE: POST /api/v1/auth/refresh (cookie: refresh_token)
    BE->>DB: Valida refresh token (hash, no expirado, no revocado)
    BE->>DB: Rota refresh token (invalida el anterior, INSERT nuevo)
    BE->>FE: 200 { access_token, expires_in }<br/>Set-Cookie: refresh_token=... (nuevo)
```

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `src/backend/Terrenario.Api` | nuevo | Proyecto ASP.NET Core Web API con autenticación |
| `src/backend/Terrenario.Api.Tests` | nuevo | Tests unitarios con xUnit |
| `src/frontend/terrenario-web` | nuevo | SPA React + TypeScript + Vite |
| `src/backend/.../Controllers/AuthController.cs` | nuevo | Endpoints de autenticación |
| `src/backend/.../Domain/Users/User.cs` | nuevo | Entidad de dominio Usuario |
| `src/backend/.../Infrastructure/Data/TerrenarioDbContext.cs` | nuevo | DbContext con tablas `usuarios` y `refresh_tokens` |

## Diseño detallado

### Modelo de datos

```sql
-- usuarios: entidad central de identidad
CREATE TABLE usuarios (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    google_sub      TEXT NOT NULL UNIQUE,
    nombre          TEXT NOT NULL,
    email           TEXT NOT NULL,
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now(),
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- refresh_tokens: tokens rotantes por usuario
CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    usuario_id      UUID NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    token_hash      TEXT NOT NULL UNIQUE,   -- SHA-256 del token en claro
    expires_at      TIMESTAMPTZ NOT NULL,
    revocado_en     TIMESTAMPTZ,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_refresh_tokens_usuario_id ON refresh_tokens(usuario_id);
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);
```

### API / Contratos

```yaml
# POST /api/v1/auth/google/callback
# Intercambia un authorization code de Google por una sesión interna
request:
  body:
    code: string          # Authorization code de Google
    redirect_uri: string  # Debe coincidir con el registrado en Google Cloud Console
    code_verifier: string # PKCE verifier (plain text, el backend calcula el challenge)

responses:
  200:
    body:
      access_token: string   # JWT RS256, exp=15min
      expires_in: 900        # segundos
      user:
        id: uuid
        display_name: string
    headers:
      Set-Cookie: refresh_token=<token>; HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth; Max-Age=2592000
  401:
    body: { error: { code: "AUTH_GOOGLE_TOKEN_INVALID", message: "Autenticación no válida" } }
  500:
    body: { error: { code: "AUTH_GOOGLE_EXCHANGE_FAILED", message: "Error al completar el acceso" } }

# POST /api/v1/auth/refresh
# Renueva el access_token usando el refresh_token de la cookie
request:
  cookies:
    refresh_token: string
responses:
  200:
    body: { access_token: string, expires_in: 900 }
    headers:
      Set-Cookie: refresh_token=<nuevo_token>; HttpOnly; Secure; SameSite=Strict; ...
  401:
    body: { error: { code: "AUTH_REFRESH_TOKEN_INVALID", message: "Sesión expirada" } }

# POST /api/v1/auth/logout
# Revoca el refresh_token activo
request:
  cookies:
    refresh_token: string
responses:
  204: {}
  headers:
    Set-Cookie: refresh_token=; HttpOnly; Secure; SameSite=Strict; Max-Age=0

# GET /api/v1/auth/me
# Devuelve información no-sensible del usuario autenticado
request:
  headers:
    Authorization: Bearer <access_token>
responses:
  200:
    body: { id: uuid, display_name: string }
  401:
    body: { error: { code: "AUTH_UNAUTHENTICATED", message: "Token ausente o no válido" } }
```

### Lógica de negocio

**Alta implícita de usuario (upsert):**

- Al recibir un `id_token` válido de Google, se extrae `sub`, `name` y `email`.
- Si existe un usuario con ese `google_sub` → se actualiza nombre/email y se retorna.
- Si no existe → se crea el registro en `usuarios`.
- La decisión de upsert por `google_sub` garantiza idempotencia ante re-logins.

**JWT (access_token):**

- Algoritmo RS256. Clave privada RSA almacenada en Secret Manager (en dev: `appsettings.Development.json` cifrado o variable de entorno).
- Claims: `sub` (usuario.id), `name` (display_name), `iss` (terrenario-api), `aud` (terrenario-web), `exp` (now+15min), `iat`.
- No se incluye email en el JWT para minimizar PII en tokens.

**Refresh token:**

- Token opaco de 32 bytes aleatorios (Base64URL).
- Se persiste el hash SHA-256 en DB para validación sin exponer el valor en claro.
- Rotación: al refrescar, el token anterior se invalida y se emite uno nuevo.
- TTL: 30 días.

**Telemetría de embudo (sin PII):**

- Eventos estructurados vía `ILogger` con `scope` `auth.funnel`.
- Campos: `timestamp`, `session_id` (aleatorio por intento), `flow_id`, `channel=web`, `error_code` (cuando aplique).
- Nunca se loguea email completo; si se necesita correlación se usa hash SHA-256 del email.

### Manejo de errores

| Situación | Código HTTP | Código de error | Nota |
| --------- | ----------- | --------------- | ---- |
| Code de Google inválido o expirado | 401 | `AUTH_GOOGLE_TOKEN_INVALID` | No se expone detalle interno |
| Google devuelve `error=access_denied` | 400 | `AUTH_LOGIN_CANCELLED` | El usuario canceló el flujo |
| State CSRF no coincide (frontend) | (manejado en FE) | — | Se aborta antes de llamar al backend |
| Refresh token expirado | 401 | `AUTH_REFRESH_TOKEN_INVALID` | Redirige a login |
| Refresh token revocado | 401 | `AUTH_REFRESH_TOKEN_INVALID` | Mismo mensaje por seguridad |
| Error interno al consultar Google | 500 | `AUTH_GOOGLE_EXCHANGE_FAILED` | Log interno con request_id |

## Alternativas descartadas

| Alternativa | Por qué se descartó |
| ----------- | ------------------- |
| Almacenar refresh_token en localStorage | Expuesto a XSS. HttpOnly cookie es más seguro |
| Flujo implícito (response_type=token) | Obsoleto por OAuth 2.0 Security BCP; PKCE es el estándar recomendado |
| Validar id_token solo en frontend | Sin control de servidor; no permite emisión de JWT propio |
| Symmetric JWT (HMAC HS256) | RS256 permite validación distribuida sin compartir clave secreta |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
| ------ | ------------ | ---------- |
| Configuración incorrecta de redirect_uri en Google Cloud Console | alta | Documentar URI exactas en `appsettings.Development.json.example` |
| Rotación de clave RSA sin downtime | media | Soportar JWKS endpoint con múltiples claves en validación |
| Leak de refresh_token si cookie no es Secure en dev | baja | Env var `ASPNETCORE_ENVIRONMENT=Development` activa `Secure=false` solo en localhost |

## Plan de testing

> Referencia: `docs/04-ingenieria/estrategia-testing.md`

- [x] Tests unitarios:
  - `ExchangeGoogleCodeHandler`: caso feliz, token Google inválido, usuario nuevo, usuario existente
  - `RefreshTokenHandler`: token válido, token expirado, token revocado
  - `JwtService`: emisión con claims correctos, validación de expiración
- [ ] Tests de integración: endpoint POST /auth/google/callback con mock de Google (pendiente sprint 1)
- [ ] Tests E2E: flujo completo login Google (pendiente sprint final)

## Checklist de implementación

- [ ] Diseño técnico revisado y aprobado
- [x] Migraciones de base de datos preparadas (tablas `usuarios` y `refresh_tokens`)
- [ ] Tests escritos y pasando
- [ ] Documentación de API actualizada
- [ ] Módulo Auth documentado en `docs/03-modulos/`
- [ ] Sin `TODO` sin resolver en este documento
