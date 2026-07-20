---
bloque: 01-producto
documento: roadmap
actualizado_en: "2026-07-20"
---

# Roadmap de Alto Nivel

> Este es el roadmap de visión macro. El detalle de cada épica está en `../09-desarrollos/epicas/`.
> Los sprints y fechas exactas se gestionan en el sistema de tickets (Jira / Linear / etc.).

---

## Estado actual del producto

**Versión en producción**: no definida
**Estado de definición**: cierre funcional del MVP completado en KB
**Próximo hito**: Hito A — Identidad y contexto seguro de Workspace

---

## Propuesta de roadmap MVP

> Propuesta de roadmap basada en la KB actualizada tras el cierre funcional.
> No fija fechas cerradas; define orden, dependencias y criterio de alcance para crear después las épicas e historias.

### Corto plazo (0-3 meses)

| Bloque | Alcance | Resultado esperado | Dependencia principal | Hito |
|-------|---------|--------------------|-----------------------|------|
| Identidad y contexto seguro | Google OIDC, creación/unión a Workspaces, invitaciones por email/enlace, selector de Workspace y permisos planos MVP | Usuario autenticado puede entrar y operar dentro de un Workspace activo sin fricción | Ninguna | Hito A — Base segura y multiusuario |
| Maestros operativos y onboarding | Terrenos, Temporadas, Trabajadores, Tareas y creación automática de primera temporada | El sistema queda listo para registrar operativa real sin depender de configuración manual compleja | Hito A | Hito B — Base operativa preparada |
| Diario y operativa diaria | Diario cronológico unificado, Actividades, Compras, Imputaciones y validaciones de coste/tarea/temporada | Se puede registrar el día a día completo con trazabilidad mínima y sin bloquear por compras ausentes | Hito B | Hito C — Registro operativo end-to-end |
| Producción y dashboard MVP | Cosechas, catálogo global de producto, regla XOR rendimiento/litros, widgets KPI y comparativa histórica básica | El usuario obtiene visibilidad accionable de la temporada a partir de datos operativos reales | Hito C | Hito D — Visibilidad operativa MVP |

### Medio plazo (3-6 meses)

| Bloque | Alcance | Resultado esperado | Dependencia principal | Hito |
|-------|---------|--------------------|-----------------------|------|
| Endurecimiento y salida a MVP | Hardening de seguridad, tests obligatorios, validación RGPD/LOPDGDD, smoke E2E y criterios de salida | El MVP puede desplegarse con un nivel de riesgo controlado y evidencia mínima de calidad | Hito D | Hito E — Salida controlada a MVP |
| Observabilidad inicial | Embudo de login, métricas de uso del dashboard, alertas básicas y operación medible | El equipo puede detectar barreras de acceso y degradaciones operativas en los primeros usuarios reales | Hito E | Hito F — Operación medible |

### Largo plazo (6-12 meses)

| Bloque | Alcance | Resultado esperado | Dependencia principal | Hito |
|-------|---------|--------------------|-----------------------|------|
| Offline/sync diferido | Outbox, idempotencia, reintentos y resolución de conflictos post-MVP | Captura resiliente fuera de cobertura, sin ampliar el alcance del MVP inicial | Hito F | Hito G — Resiliencia offline |
| Evoluciones de producto | Permisos granulares, importación desde hoja, balance/molturación/precio, analítica avanzada y meteo+IA | Escalado funcional después de validar adopción y operativa base | Hito F | Hito H — Escalado funcional |

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
5. Evitar abrir frentes paralelos que no pueda absorber un equipo pequeño.

## Justificación de los bloques

### 1. Identidad y contexto seguro primero

Se ejecuta primero porque toda la KB ya define un MVP multiusuario y Workspace-first. Sin autenticación, invitaciones y contexto activo de Workspace, el resto de entidades nacería sin perímetro funcional ni de seguridad.

### 2. Maestros antes que captura masiva

Terrenos, temporadas, trabajadores y tareas deben quedar disponibles antes de la operativa para evitar que actividades, compras y cosechas empiecen a generarse con semántica inconsistente. Además, la autoselección de temporada activa y el diario dependen de esta base.

### 3. Diario y operativa diaria antes que dashboard

El MVP promete simplicidad de uso diario, no solo visibilidad analítica. Por eso el diario cronológico unificado y la captura de actividades/compras se priorizan antes del dashboard: sin operativa sólida, el dashboard sería una capa visual con poco valor real.

### 4. Producción y dashboard sobre datos ya estabilizados

Producción se coloca después de la base operativa porque comparte dependencias de terreno, temporada y contexto Workspace. El dashboard entra al final del núcleo funcional para calcular KPIs sobre datos ya consistentes y no sobre modelos todavía moviéndose.

### 5. Endurecimiento como bloque explícito

No se deja como trabajo difuso de cierre. La KB ya define gates de testing, cumplimiento y validación para salir a producción; por eso el roadmap reserva un bloque específico de estabilización antes de considerar el MVP listo.

## Qué queda fuera del MVP

Estas líneas no bloquean el roadmap ni deben infiltrarse en las primeras épicas:

1. Offline/sync diferido.
2. Importación o migración desde la hoja actual.
3. Permisos granulares por usuario, rol o terreno.
4. Balance, molturación y precio en producción.
5. Analítica avanzada, datos colaborativos y meteo+IA.

## Dependencias de implementación

| Bloque | Depende de | Motivo |
|-------|------------|--------|
| Hito A — Base segura y multiusuario | — | Punto de entrada del sistema |
| Hito B — Base operativa preparada | Hito A | Los maestros viven dentro de un Workspace activo |
| Hito C — Registro operativo end-to-end | Hito B | Actividades, tareas, compras y diario dependen de maestros y temporada activa |
| Hito D — Visibilidad operativa MVP | Hito C | Cosechas y dashboard requieren operativa y reglas ya estables |
| Hito E — Salida controlada a MVP | Hito D | No tiene sentido endurecer antes de cerrar el núcleo funcional |
| Hito F — Operación medible | Hito E | La observabilidad inicial debe medir un MVP ya desplegable |

## Trazabilidad con arquitectura

Este roadmap se alinea con el plan por incrementos de `../02-arquitectura/vision-general.md` y con el cierre funcional registrado en `definicion-requisitos-usuario.md` y `reglas-de-negocio.md`.
