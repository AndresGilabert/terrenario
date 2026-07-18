---
bloque: 02-arquitectura
documento: tech-stack
actualizado_en: "2026-07-18"
---

# Stack Tecnológico

> Tecnologías objetivo para implementar el MVP con bajo riesgo operativo.
> Base normativa: `../07-seguridad/modelo-seguridad.md` y `../07-seguridad/privacidad-datos.md`.

---

## Frontend

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| React | 19 | UI SPA del MVP | ADR-0007 |
| TypeScript | 5.x | Tipado estático del frontend | ADR-0007 |
| Vite | 6.x | Build/dev server del frontend | ADR-0007 |

## Backend

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| .NET | 10 | Runtime y plataforma backend | ADR-0003 |
| ASP.NET Core Web API (Controllers) | 10 | Exposición de API REST modular | ADR-0003 |
| OpenAPI | 3.1 | Contratos versionados y documentación | ADR-0006 |

## Base de datos

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| PostgreSQL | 15 | Persistencia transaccional principal | ADR-0001 |
| Entity Framework Core | 10 | ORM, migraciones code-first y acceso a datos MVP | ADR-0004 |
| Dapper (post-MVP) | 2.x | Optimizar lecturas analíticas complejas en evolución C | ADR-0004 |

## Infraestructura y DevOps

| Tecnología | Versión | Propósito | ADR |
|-----------|---------|-----------|-----|
| Docker | 27.x | Entornos reproducibles para local/CI | ADR-0008 |
| GitHub Actions | actual | CI para lint, test, validación docs y build | ADR-0008 |
| Terraform | 1.9.x | Infra declarativa para entorno cloud (activación diferida) | ADR-0008 |
| Sentry | 8.x | Trazabilidad de errores sin PII en claro | ADR-0008 |
| OpenTelemetry | 1.x | Trazas distribuidas y métricas operativas (activación diferida) | ADR-0008 |

## Herramientas de desarrollo

| Herramienta | Versión | Propósito |
|------------|---------|-----------|
| Roslyn Analyzers | 10 | Reglas estáticas de calidad .NET |
| dotnet format | 10 | Formateo consistente |
| xUnit | 2.x | Tests unitarios e integración |
| Playwright | 1.5x | Tests E2E web de flujos online |
| Spectral | 6.x | Lint de OpenAPI |
| pre-commit hooks | actual | Controles de calidad antes de commit |

## Criterios de selección tecnológica

1. Madurez y mantenimiento activo.
2. Encaje con reglas de seguridad y privacidad vigentes.
3. Curva de aprendizaje razonable para un equipo pequeño.
4. Coste operativo sostenible para etapa MVP.
5. Compatibilidad con arquitectura Workspace-first y operación online.
6. Facilidad de test automatizado y observabilidad.

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
| Ninguna en MVP inicial | N/A | N/A |

---

## Riesgos técnicos del stack y mitigación

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Deriva de arquitectura por cambios de stack | Medio | ADRs vinculantes y guía de estructura de solución .NET |
| Drift entre DTO y OpenAPI | Medio | Generación automática de esquema + lint Spectral en CI |
| Exposición accidental de PII en logs | Alto legal | Redacción de logs y tests de no-regresión para campos sensibles |

---

## Dependencias de decisión abiertas

1. Definir al entrar en fase A la retención detallada de telemetría y los umbrales finales de alertado (scope diferido en ADR-0008).
