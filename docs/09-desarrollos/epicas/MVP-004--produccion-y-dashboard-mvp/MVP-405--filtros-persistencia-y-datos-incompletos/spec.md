---
id: "MVP-405"
tipo: feature
titulo: "Filtros, persistencia y datos incompletos"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-403", "MVP-404"]
bloquea: ["MVP-005", "MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "filtros", "calidad-dato"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "filtros", "kpi-kg-por-arbol"]
  etiquetas: ["mvp", "dashboard", "filters"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-405 — Filtros, persistencia y datos incompletos

## Contexto

El dashboard MVP no solo necesita widgets: también debe respetar contexto de Workspace, temporada y terrenos, conservar filtros tras recarga manual y tratar explícitamente los casos en que faltan datos base como `num_arboles`.

## Objetivo

Cerrar la experiencia operativa del dashboard con filtros coherentes, persistencia mínima y tratamiento explícito del dato incompleto.

## Requisitos de usuario

### HU-1 — Mantener el contexto de lectura del dashboard

**Como** usuario que revisa la temporada,
**quiero** filtrar por temporada y terrenos sin perder ese contexto al recargar,
**para** consultar resultados de forma rápida y consistente.

### HU-2 — Entender cuándo un KPI está incompleto

**Como** usuario del Workspace,
**quiero** que la app me avise cuando falten datos base,
**para** no interpretar como exacto un KPI calculado sobre información parcial.

## Alcance (in-scope)

- Filtro por temporada y terrenos en dashboard.
- Resolución por defecto de temporada activa y todos los terrenos activos.
- Persistencia de filtros tras recarga manual.
- Tratamiento de `kg/árbol` con exclusión de terrenos sin `num_arboles` y aviso de dato incompleto.
- Dashboard en una sola pantalla con scroll vertical y sin refresco continuo.

## Fuera de alcance (out-of-scope)

- Filtros avanzados por propietario o dimensiones adicionales fuera del núcleo MVP.
- Refresco en tiempo real.
- Configuración de paneles o widgets personalizados.

## Criterios de aceptación

- [ ] **CA-1**: El dashboard aplica por defecto temporada activa y todos los terrenos activos cuando no se informan filtros.
- [ ] **CA-2**: La recarga manual conserva los filtros activos del usuario.
- [ ] **CA-3**: El KPI `kg/árbol` excluye terrenos sin `num_arboles` e informa explícitamente que el dato es incompleto.

## Maquetas y referencias visuales

- Referencia funcional: RN-005, RN-006, RN-007, RN-008 y RN-010.

## Notas y decisiones

- Esta historia cierra la experiencia completa del dashboard MVP antes del endurecimiento y la observabilidad.