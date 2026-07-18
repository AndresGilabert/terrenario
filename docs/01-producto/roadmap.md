---
bloque: 01-producto
documento: roadmap
actualizado_en: "2026-07-18"
---

# Roadmap de Alto Nivel

> Este es el roadmap de visión macro. El detalle de cada épica está en `../09-desarrollos/epicas/`.
> Los sprints y fechas exactas se gestionan en el sistema de tickets (Jira / Linear / etc.).

---

## Estado actual del producto

**Versión en producción**: no definida
**Próximo hito**: Fundaciones técnicas MVP (auth OIDC + Workspaces + entidades base)

---

## Épicas planificadas

> Roadmap técnico mínimo para arrancar planificación del MVP.
> No se fijan fechas de release en esta versión; solo hitos de implementación y trazabilidad.

### Corto plazo (0-3 meses)

| Épica | Descripción | Estado | Hito |
|-------|-------------|--------|------|
| Fundaciones de dominio y seguridad | Auth OIDC, Workspaces, Terrenos, Temporadas, Trabajadores y base de permisos por Workspace | planificada | Hito A — Base operativa segura |
| Operativa diaria y captura de datos | Actividades, Compras e Imputaciones con validaciones de dominio y trazabilidad | planificada | Hito B — Registro operativo end-to-end |
| Producción y dashboard MVP | Cosechas, reglas XOR, widgets KPI y filtros persistentes | planificada | Hito C — Visibilidad de temporada |

### Medio plazo (3-6 meses)

| Épica | Descripción | Estado | Hito |
|-------|-------------|--------|------|
| Endurecimiento técnico y cumplimiento | Hardening de seguridad, cobertura de tests y criterios de salida a producción | planificada | Hito D — Salida controlada a MVP |
| Observabilidad operativa | Cierre de alertas, telemetría de login y umbrales de operación inicial | planificada | Hito E — Operación medible |

### Largo plazo (6-12 meses)

| Épica | Descripción | Estado | Hito |
|-------|-------------|--------|------|
| Offline/sync diferido | Outbox, idempotencia, reintentos y resolución de conflictos post-MVP | exploración | Hito F — Resiliencia offline |
| Evoluciones de producto | Permisos granulares, mejoras de catálogo y analítica avanzada | exploración | Hito G — Escalado funcional |

---

## Épicas completadas

| Épica | Versión | Fecha de release |
|-------|---------|-----------------|
| _(sin registros)_ | | |

---

## Criterios de priorización

> Cómo se decide qué entra en el roadmap y en qué orden.

1. Entregar primero capacidades que habilitan registro operativo diario sin fricción.
2. Priorizar riesgos de seguridad y cumplimiento antes de ampliar alcance funcional.
3. Maximizar trazabilidad requisito -> regla -> contrato -> validación.
4. Diferir capacidades offline y analítica avanzada hasta estabilizar la operación online.

## Trazabilidad con arquitectura

Este roadmap se alinea con el plan por incrementos de `../02-arquitectura/vision-general.md`.
