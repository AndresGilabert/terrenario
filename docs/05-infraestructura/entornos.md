---
bloque: 05-infraestructura
documento: entornos
actualizado_en: "2026-08-08"
---

# Entornos

---

## Modelo de entornos por fase

| Fase | Entornos activos | Objetivo |
|------|------------------|----------|
| C (MVP inicial) | `prod` | Coste minimo y operacion simple |
| A | `dev` + `prod` | Acelerar iteracion sin perder control de produccion |
| B | `dev` + `staging` + `prod` | Endurecimiento de calidad y prevalidacion |

Estado actual: fase C.

Reglas de activacion:

1. Paso C -> A: regla combinada durante 2 meses consecutivos (>= 8 despliegues/mes o >= 25 PRs/mes).
2. Paso A -> B: MAU cualitativo medio-alto durante 2 meses y cumplimiento de la regla de cambios sostenidos en el mismo periodo.

---

## Entorno `dev` (fase A en adelante)

**Requisitos base**:

1. Docker
2. Runtime .NET 10
3. Node.js LTS para frontend y tooling

**Arrancar el entorno**:

```bash
docker compose up --build
```

**Secretos en `dev`**: se gestionan en Secret Manager del proveedor (igual que `prod`).

---

## Entorno `staging` (solo fase B)

**Propósito**: validación final previa a producción para reducir riesgo de release.

**Acceso**: restringido a responsable técnico.

**Deploy**: automático desde rama de integración según política de CI/CD vigente.

**Base de datos**: datos de prueba sin PII en claro.

---

## Entorno `prod`

**Acceso**: restringido.

**Deploy**: gate manual obligatorio en pipeline.

**Backup fase C (actual)**: snapshot puntual/manual.
**Backup fase A**: snapshot semanal con retencion 2 semanas.
**Backup fase B**: snapshot diario 7 dias + semanal 8 semanas.

---

## Variables de entorno por entorno

| Variable | dev | staging | prod | Descripción |
|----------|-----|---------|------|-------------|
| `APP_ENV` | `dev` | `staging` | `prod` | Entorno activo |
| `LOG_LEVEL` | `debug` | `info` | `warning` | Nivel de logs |
| `DATABASE_URL` | secreto | secreto | secreto | Cadena de conexión DB |
| `OIDC_CLIENT_ID` | secreto | secreto | secreto | Cliente OIDC |
| `SENTRY_DSN` | secreto | secreto | secreto | Error tracking |
| `Invitations__AcceptBaseUrl` | URL del front dev | URL del front staging | `https://app.terrenario.com/invitations` | Base pública del enlace de invitación |
| `WorkspaceLifecycle__ReactivationBaseUrl` | URL del front dev | URL del front staging | `https://app.terrenario.com/reactivations` | Base pública del enlace de reactivación de Workspace (MVP-206) |
| `WorkspaceLifecycle__ReactivationLifetimeDays` | `7` | `7` | `7` | Vigencia del enlace de reactivación, de un solo uso |
| `Email__Host` | secreto | secreto | secreto | Servidor SMTP. Vacío = no se envían invitaciones |
| `Email__Port` | `587` | `587` | `587` | `465` si se usa TLS implícito |
| `Email__SecurityMode` | `starttls` | `starttls` | `starttls` | `ssl`, `none` o `auto` según servidor |
| `Email__Username` | secreto | secreto | secreto | Usuario de autenticación |
| `Email__Password` | secreto | secreto | secreto | Contraseña o contraseña de aplicación |
| `Email__FromAddress` | secreto | secreto | secreto | Remitente. Vacío = no se envían invitaciones |
| `Email__FromName` | `Terrenario` | `Terrenario` | `Terrenario` | Nombre visible del remitente |
| `Legal__PrivacyPolicyUrl` | URL del front dev | URL del front staging | `https://app.terrenario.com/legal/privacidad` | Política de Privacidad enlazada en el pie de todos los correos (MVP-715) |
| `Legal__LegalName` y demás campos de identidad | versionado | versionado | versionado | Solo para sobreescribir la identidad del responsable en un despliegue concreto. Vacío = el valor de `legal-entity.json` |
| `Ops__ApiKey` | secreto | secreto | secreto | Llave de servicio para `GET /api/v1/ops/signals` (MVP-603). **Vacía = el endpoint no existe (404)** |
| `Ops__AlertEmail` | secreto | secreto | secreto | Destinatario de los avisos de alerta. Vacío = las alertas solo quedan en la traza |
| `Ops__AlertsEnabled` | `true` | `true` | `true` | Apaga la vigilancia. Se pone a `false` en el arnés de tests |
| `Telemetry__FlushIntervalSeconds` | `60` | `60` | `60` | Cadencia del volcado de contadores (MVP-601) |
| `Telemetry__RetentionDays` | `400` | `400` | `400` | Histórico de contadores agregados. No es un plazo de `RN-041`: no hay datos personales |

> **Por qué toda la cuenta de envío va como secreto**: el repositorio es público. `Host`, `Username`
> y `FromAddress` no son credenciales por sí solos, pero identifican una cuenta concreta de un
> servicio de terceros y, commiteados, quedan en el historial de git de forma permanente. En
> `appsettings.json` se mantienen vacíos: definen la forma de la sección, no sus valores. En local
> se gestionan con User Secrets y por entorno con el Secret Manager del proveedor cloud.
>
> **Cuenta de envío de invitaciones**: decisión y alternativas en
> [ADR-0010](../02-arquitectura/decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md).
> El remitente definitivo está **pendiente de decisión de negocio**. Para producción hace falta un
> dominio propio con **SPF, DKIM y DMARC** publicados; sin eso las invitaciones acaban en spam. Una
> cuenta de Google Workspace con contraseña de aplicación sirve para `dev` y arranque, pero tiene
> límite de envío diario.
>
> Mientras `Email__Host` o `Email__FromAddress` estén vacíos, el entorno arranca con un warning, las
> invitaciones se emiten igual y la API responde `email_sent: false`: se comparten por enlace.
>
> **Identidad del responsable en el pie de los correos** (MVP-715): no es configuración de entorno
> por defecto. Sale de `src/frontend/terrenario-web/src/config/legal-entity.json`, incrustado en el
> ensamblado al compilar, que es el mismo fichero del que se alimentan la Política de Privacidad y
> los Términos publicados. Las variables `Legal__*` solo existen para sobreescribirlo en un
> despliegue concreto; un valor vacío cae al versionado. Inventario y criterios en
> [correos-del-producto.md](../06-integraciones/correos-del-producto.md).

---

## Gestión de secretos

Los secretos de `prod` y `dev` se gestionan en Secret Manager del proveedor cloud.
**Nunca** incluir secretos en el código, en variables de CI/CD visibles ni en este documento.
Ver `../07-seguridad/modelo-seguridad.md`.

## Trazabilidad KB

1. Reglas de arquitectura y fases: `../02-arquitectura/vision-general.md`
2. Pipeline y promoción: `ci-cd.md`
3. Recuperación y backup: `disaster-recovery.md`
4. Gestión de secretos: `../07-seguridad/modelo-seguridad.md`
