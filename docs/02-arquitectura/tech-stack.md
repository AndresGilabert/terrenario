---
bloque: 02-arquitectura
documento: tech-stack
actualizado_en: "2026-08-12"
---

# Stack Tecnológico

> Tecnologías **efectivamente en uso** en el código entregado, con la versión que declara
> `Terrenario.Api.csproj`, `Terrenario.Api.Tests.csproj` y `package.json`.
> Base normativa: `../07-seguridad/modelo-seguridad.md` y `../07-seguridad/privacidad-datos.md`.
>
> Lo que se decidió en un ADR pero **no llegó a adoptarse** no se lista aquí como si estuviera
> montado: vive en [Tecnologías declaradas y no adoptadas](#tecnologías-declaradas-y-no-adoptadas),
> con el estado real y la razón.

---

## Frontend

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| React | 19.2 | UI SPA del MVP | ADR-0007 |
| TypeScript | 6.0 | Tipado estático del frontend | ADR-0007 |
| Vite | 8.1 | Build/dev server del frontend | ADR-0007 |
| React Router | 8.3 | Enrutado, guardas de sesión y de Workspace | — |
| Tailwind CSS | 4.3 | Estilos, vía plugin oficial de Vite | — |
| Material Symbols · Inter · Plus Jakarta Sans | — | Iconografía y tipografías, servidas como subconjunto propio generado en el `build` (`MVP-810`) | — |

## Backend

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| .NET | 9.0 (`net9.0`) | Runtime y plataforma backend | ADR-0003 |
| ASP.NET Core Web API (Controllers) | 9.0 | Exposición de API REST modular | ADR-0003 |
| OpenAPI (`Microsoft.AspNetCore.OpenApi`) | 3.0 · paquete 9.0.16 | Contrato generado desde el código y servido en `/openapi/v1.json` | ADR-0006 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0 | Validación del access token propio (RS256) | ADR-0003 |
| `Google.Apis.Auth` | 1.68 | Verificación del token de identidad de Google | — |
| MailKit | 4.17 | Envío de correo transaccional por SMTP | ADR-0010 |

> **La versión de .NET es 9.0, no 10.** El proyecto declara `net9.0` desde el primer commit
> (`MVP-101`) y el CI instala `9.0.x`. Ver la nota de actualización en
> [ADR-0003](./decisiones/ADR-0003--backend-dotnet9-aspnet-core-controllers.md).
>
> **La versión de OpenAPI es 3.0, no 3.1.** `Program.cs` llama a `AddOpenApi()` sin opciones, y el
> valor por defecto de `OpenApiOptions.OpenApiVersion` en el paquete 9.0.16 es `OpenApi3_0`. Emitir
> 3.1 exige configurarlo explícitamente y no se ha hecho.

## Base de datos

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| PostgreSQL | 15+ en local · **16** en producción | Persistencia transaccional principal | ADR-0001 |
| Entity Framework Core | 9.0 | ORM, migraciones code-first y acceso a datos MVP | ADR-0004 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0 | Proveedor PostgreSQL para EF Core | ADR-0004 |
| Dapper (post-MVP) | 2.x | Optimizar lecturas analíticas complejas en evolución C | ADR-0004 |

## Infraestructura y DevOps

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| Azure App Service (Linux) | B1 | Alojamiento de la API, que sirve también el cliente | — |
| Azure Database for PostgreSQL Flexible Server | B1ms · PG 16 | Base de datos de producción (España, Spain Central) | ADR-0001 |
| Docker | 27.x | PostgreSQL local y contenedores de la suite de tests | ADR-0008 |
| GitHub Actions | actual | CI (build, lint, tests, `npm audit`), validación de KB y despliegue por tag | ADR-0008 |
| Scripts `az` (`infra/azure/`) | — | Aprovisionamiento del entorno de producción | ADR-0008 |

## Herramientas de desarrollo

| Herramienta | Versión | Propósito |
|------------|---------|-----------|
| xUnit | 2.9 | Tests de backend, unitarios y de integración |
| FluentAssertions | 6.12 | Aserciones legibles en la suite de backend |
| NSubstitute | 5.3 | Dobles de prueba en la suite de backend |
| Testcontainers for PostgreSQL | 4.13 | PostgreSQL real en contenedor: la suite corre contra el mismo motor que producción (`MVP-501`) |
| `Microsoft.AspNetCore.Mvc.Testing` | 9.0 | Arnés de API para el smoke E2E de servidor |
| Vitest | 4.1 | Tests del cliente |
| Testing Library (React · DOM · user-event) | 16.3 | Pruebas de componentes orientadas a comportamiento |
| Oxlint | 1.71 | Lint del cliente |
| Roslyn Analyzers | 9 (incluidos en el SDK) | Reglas estáticas de calidad .NET; el `build` corre con `-warnaserror` |
| `dotnet format` | 9 (incluido en el SDK) | Formateo consistente |
| markdownlint | CLI | Lint de la documentación, bloqueante en CI |
| `validar_pipeline_kb.py` | — | Gate de la KB: estructura, frontmatter, índices y trazabilidad de requisitos |
| pre-commit hooks | actual | Ejecuta el gate de la KB antes de commitear |

## Criterios de selección tecnológica

1. Madurez y mantenimiento activo.
2. Encaje con reglas de seguridad y privacidad vigentes.
3. Curva de aprendizaje razonable para un equipo pequeño.
4. Coste operativo sostenible para etapa MVP.
5. Compatibilidad con arquitectura Workspace-first y operación online.
6. Facilidad de test automatizado y observabilidad.

## Tecnologías declaradas y no adoptadas

> Decisiones tomadas en un ADR que **no se implementaron**. Se listan aquí en vez de en las tablas de
> arriba porque un stack que nombra herramientas inexistentes engaña a quien se incorpora y a quien
> audita. Cada fila dice qué se hace hoy en su lugar.

| Tecnología | Dónde se declaró | Estado real | Qué se hace en su lugar |
|-----------|------------------|-------------|-------------------------|
| Sentry | ADR-0008 §3 | **No implementada.** No hay dependencia ni cuenta | Observabilidad propia de `MVP-601`/`602`/`603`: telemetría en tablas propias, señales en `/api/v1/ops/signals` y alertas por correo |
| Terraform | ADR-0008 §4 (activación diferida) | **No implementada** | Aprovisionamiento con scripts `az` en `infra/azure/` |
| OpenTelemetry | ADR-0008 §5 (activación diferida) | **No implementada** | Logs estructurados con `request-id` y métricas operativas propias |
| Playwright | ADR-0007 §4 y `../04-ingenieria/estrategia-testing.md` | **No montada** (`MVP-999`, `P-064`) | Smoke E2E **de servidor** con `WebApplicationFactory` sobre la API real |
| Spectral | Este documento, hasta 2026-08-12 | **Nunca incorporada al CI** | El contrato se genera desde el código, así que no puede divergir de los DTO |

## Tecnologías en evaluación

| Tecnología | Caso de uso propuesto | Estado | ADR |
|-----------|----------------------|--------|-----|
| Redis Streams | Escalado de colas/eventos en evolución de arquitectura | en-evaluación | no iniciado |
| Keycloak | Identidad multi-proveedor avanzada cuando crezcan roles | en-evaluación | no iniciado |
| Workbox | Habilitar modo offline y cacheo avanzado post-MVP | backlog | no iniciado |
| Dapper | Paso de evolución C para lecturas analíticas post-MVP | backlog | ADR-0004 |

## Tecnologías deprecadas

| Tecnología | Reemplazada por | Fecha de deprecación |
|-----------|----------------|---------------------|
| SQLite en la suite de tests | Testcontainers con PostgreSQL real (`MVP-501`, `P-031`) | 2026-07-31 |

---

## Riesgos técnicos del stack y mitigación

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Deriva de arquitectura por cambios de stack | Medio | ADRs vinculantes y guía de estructura de solución .NET |
| Deriva entre el stack documentado y el implementado | Medio | Este documento se revisa en cada `MVP-x99`; el precedente es `P-094` (componentes) y `P-129` (stack) |
| Drift entre DTO y OpenAPI | Bajo | El contrato se **genera** desde los tipos del código, no se mantiene a mano |
| Exposición accidental de PII en logs | Alto legal | Redacción de logs y tests de no-regresión para campos sensibles |

---

## Dependencias de decisión abiertas

1. Definir al entrar en fase A la retención detallada de telemetría y los umbrales finales de alertado
   (scope diferido en ADR-0008).
2. Decidir si Sentry sigue siendo la herramienta de error tracking objetivo o si la observabilidad
   propia del MVP la sustituye de forma definitiva (ADR-0008 pendiente de actualizar en fase A).
