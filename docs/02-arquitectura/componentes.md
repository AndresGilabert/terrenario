---
bloque: 02-arquitectura
documento: componentes
actualizado_en: "2026-07-24"
---

# Componentes del Sistema

> Diagrama C4 Nivel 3. Describe los componentes principales dentro de cada contenedor del MVP.
> Para el contexto del sistema completo, ver `vision-general.md`.

---

## Componente: API Core (MVP)

**Contenedor padre**: API Backend (`.NET 10 + ASP.NET Core Controllers`)
**Responsabilidad**: Exponer endpoints REST `/api/v1`, aplicar validación de entrada y control de acceso por Workspace.
**Owner**: equipo técnico del producto

### Interfaces expuestas

| Interfaz | Tipo | Descripción |
|----------|------|-------------|
| `/api/v1/workspaces` | REST | Alta del Workspace y consulta del Workspace activo |
| `/api/v1/plots` | REST | Alta, edición y consulta de terrenos |
| `/api/v1/seasons` | REST | Alta, edición y consulta de temporadas |
| `/api/v1/workers` | REST | Gestión de maestro de trabajadores |
| `/api/v1/activities` | REST | Registro y consulta de actividad operativa |
| `/api/v1/harvests` | REST | Registro y consulta de cosechas |
| `/api/v1/purchases` | REST | Registro de compras e imputaciones |
| `/api/v1/dashboard/*` | REST | Agregaciones KPI por Workspace y temporada |
| `/api/v1/workspaces/invitations` | REST | Emisión y listado de invitaciones del Workspace activo |
| `/api/v1/invitations/{token}` | REST | Consulta y aceptación de una invitación recibida |

> Rutas en inglés según [ADR-0009](./decisiones/ADR-0009--idioma-de-identificadores-en-codigo.md).

### Dependencias

| Componente / Servicio | Tipo de dependencia | Descripción |
|----------------------|---------------------|-------------|
| Auth Gateway (Google OIDC) | sincrónica | Validación de identidad y emisión/validación de sesión |
| PostgreSQL | sincrónica | Persistencia transaccional y consultas de lectura |
| Servicio de email | sincrónica | Invitaciones a miembros de Workspace por SMTP genérico ([ADR-0010](./decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md)). La cuenta de envío se configura por entorno; sin ella, o si el envío falla, la invitación sigue siendo válida y se comparte por enlace |

---

## Diagrama de componentes

```mermaid
C4Component
    title Componentes — API Backend Terrenario MVP

    Container_Boundary(c1, "API Backend (.NET 10)") {
        Component(ctrl, "Controllers", "ASP.NET Core", "Endpoints REST v1, validación de request y códigos de error")
        Component(app, "Application Services", "C#", "Casos de uso y orquestación por entidad")
        Component(dom, "Domain Services", "C#", "Reglas de negocio: workspace-scope, XOR cosecha, validaciones")
        Component(repo, "Repositories", "EF Core", "Acceso a datos y control de concurrencia optimista")
        Component(qry, "Dashboard Queries", "SQL/EF Core", "Agregaciones KPI por filtros de temporada y terreno")
        Component(obs, "Observability Adapter", "OpenTelemetry/Sentry", "Trazabilidad y métricas operativas")
    }

    Rel(ctrl, app, "Invoca")
    Rel(app, dom, "Aplica reglas")
    Rel(app, repo, "Persistencia")
    Rel(app, qry, "Consulta KPI")
    Rel(app, obs, "Emite eventos técnicos")
```

---

## Catálogo de servicios

> Ver detalle de infraestructura en `../05-infraestructura/entornos.md`.

| Servicio | Tipo | URL (prod) | Owner | SLA |
|----------|------|-----------|-------|-----|
| `terrenario-api` | API | pendiente de definir por entorno | equipo técnico | 99.9% |
| `google-oidc` | Integración externa | proveedor externo | seguridad | según proveedor |
| `email-service` | Integración externa | SMTP del proveedor (cuenta pendiente de provisionar) | producto/infra | según proveedor |
