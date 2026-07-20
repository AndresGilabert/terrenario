---
bloque: 07-seguridad
documento: autenticacion-autorizacion
actualizado_en: "2026-07-18"
---

# Autenticación y Autorización

---

## Autenticación

## Decisión de producto (MVP y fases)

1. MVP: login principal con Google (OAuth 2.0 + OpenID Connect) como proveedor de identidad de menor friccion.
2. El modelo debe permitir incorporar otros proveedores sociales compatibles con OIDC/OAuth 2.0 si el negocio lo requiere.
3. Fase futura: Passkeys (WebAuthn/FIDO2) como opcion adicional de bajo esfuerzo para el usuario.
4. Durante MVP no se exige password local al usuario final Antonio.

## Modelo de autenticacion

**Metodo externo (entrada)**: Google OIDC.

**Metodo interno (sesion API)**: JWT (JSON Web Tokens) con firma RS256 emitido por el servicio de identidad tras login valido.

| Campo del JWT | Descripción |
|--------------|-------------|
| `sub` | ID del usuario |
| `roles` | Lista de roles asignados |
| `exp` | Expiración (15 min recomendado para `access_token`) |
| `iss` | Issuer (servicio de identidad) |

**Flujo de autenticación**:

```mermaid
sequenceDiagram
    Cliente->>Google: Iniciar sesion OIDC
    Google-->>Cliente: Authorization code
    Cliente->>AuthService: Intercambio code por sesion interna
    AuthService-->>Cliente: access_token + refresh_token
    Cliente->>API: Request + Authorization: Bearer {access_token}
    API->>API: Validar JWT (firma + expiración)
    API-->>Cliente: Response
```

**Renovación**: usar `refresh_token` para obtener nuevo `access_token` sin re-login.

## Trazabilidad obligatoria del embudo de login

Para detectar usuarios que llegan a pantalla de login pero no completan autenticacion, el sistema debe registrar telemetria de embudo con eventos anonimizados/pseudonimizados y sin exponer PII en claro.

Eventos minimos:

1. `login_screen_viewed`
2. `login_google_clicked`
3. `login_google_success`
4. `login_google_error`
5. `login_abandonment` (timeout o salida sin exito)

Campos minimos por evento:

1. `timestamp`
2. `session_id` (aleatorio)
3. `flow_id` (correlacion del intento de login)
4. `channel` (`web`/`mobile`)
5. `error_code` (cuando aplique)

Regla de privacidad:

1. Nunca loguear tokens, emails completos ni identificadores sensibles en texto plano.
2. Si se requiere identificar reincidencia, usar hash irreversible/pseudonimo.

---

## Autorización — Roles y permisos

| Rol | Descripción | Permisos principales |
|-----|-------------|---------------------|
| `workspace_owner` | Usuario creador del Workspace | Lectura/escritura completa en su Workspace |
| `workspace_member` | Miembro invitado y aceptado del Workspace | Lectura/escritura completa en su Workspace |
| `service` | Servicio interno (M2M) | Solo operaciones técnicas explícitamente autorizadas |

Regla MVP:

1. No existen permisos granulares por recurso en esta fase.
2. El control obligatorio es pertenencia al Workspace activo.
3. Cualquier operación fuera del Workspace devuelve `AUTH_WORKSPACE_FORBIDDEN`.

---

## Autenticación M2M (entre servicios)

Los servicios internos se autentican con **API Keys** o **tokens de servicio**:

- Las API Keys de servicio se almacenan en el gestor de secretos
- Se rotan cada 90 días
- Nunca se usan API Keys de usuarios reales para comunicación entre servicios

---

## Headers de seguridad HTTP

Todos los endpoints deben devolver:

```http
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Content-Security-Policy: default-src 'self'
```

---

## Sesiones y cookies

Si se usan cookies (para la web app):

- `HttpOnly: true`
- `Secure: true`
- `SameSite: Strict`
- Duración máxima: 30 dias para refresh token (ajustable por riesgo)

## Evolucion planificada (fuera de MVP)

1. Incorporar Passkeys como segundo metodo de autenticacion de alta usabilidad.
2. Mantener Google Login como camino principal mientras sea el metodo con menor friccion para publico senior.
