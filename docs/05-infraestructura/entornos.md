---
bloque: 05-infraestructura
documento: entornos
actualizado_en: "2026-07-24"
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
