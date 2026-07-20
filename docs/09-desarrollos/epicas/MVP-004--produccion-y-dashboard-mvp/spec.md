---
id: "MVP-004"
tipo: epica
titulo: "Producción y dashboard MVP"
estado: borrador
prioridad: critica
hito: "Hito D — Visibilidad operativa MVP"
tickets: []
historias: ["MVP-401", "MVP-402", "MVP-403", "MVP-404", "MVP-405"]
depende_de: ["MVP-001", "MVP-002", "MVP-003"]
bloquea: ["MVP-005", "MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "dashboard", "kpis"]
  modulo_path: "03-modulos/"
  componentes: ["cosechas", "dashboard", "kpis"]
  etiquetas: ["mvp", "produccion", "analytics-basica"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# EPICA MVP-004 — Producción y dashboard MVP

## Contexto

La visibilidad del producto depende de convertir la captura operativa en métricas comprensibles. La KB ya ha cerrado el alcance de producción del MVP, la regla XOR entre rendimiento y litros, el catálogo global de producto y los cuatro widgets mínimos del dashboard.

Esta épica debe construirse sobre datos ya estabilizados, no como sustituto prematuro de la operativa.

## Objetivo

Permitir registrar cosechas consistentes y mostrar un dashboard operativo útil por Workspace y temporada, con comparativa histórica básica y gestión explícita de datos incompletos.

## Requisitos de usuario de alto nivel

- **Como** usuario de la explotación, **quiero** registrar cosechas con un modelo simple pero consistente, **para** transformar trabajo operativo en visibilidad de temporada.
- **Como** usuario que revisa resultados, **quiero** ver un dashboard claro con KPIs y comparativas básicas, **para** tomar decisiones rápidas sin salir de la aplicación.

## Alcance

- Registro y edición de cosechas con `producto`, `kgs`, `destino`, temporada y uno entre `rendimiento` o `litros`.
- Catálogo global fijo de productos de cosecha.
- Soporte de destino `desconocido` y taxonomía cerrada de destinos.
- Dashboard MVP en una sola pantalla con scroll vertical.
- Widgets: resumen de temporada, kg por destino, kg por terreno y evolución de rendimiento.
- Comparativa histórica básica y tratamiento de dato incompleto en `kg/árbol`.
- Filtros por Workspace, temporada y terrenos con persistencia tras recarga manual.

## Fuera de alcance

- Precio, balance, molturación y capa económico-industrial de producción.
- Analítica avanzada, exploración ad-hoc o benchmarking colaborativo.
- Refresco en tiempo real o actualización continua en segundo plano.
- Offline o captura diferida de cosechas.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: El usuario puede registrar cosechas sin ambigüedad entre rendimiento y litros y ver los cuatro widgets mínimos sin error bloqueante.
- [ ] **CA-3**: El dashboard respeta filtros, taxonomías y reglas de dato incompleto definidas en la KB.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-401` — Registro y edición de cosechas.
- `MVP-402` — Reglas de producción, catálogo de producto y destino.
- `MVP-403` — Dashboard MVP: resumen y kg por destino.
- `MVP-404` — Dashboard MVP: kg por terreno y evolución de rendimiento.
- `MVP-405` — Filtros, persistencia de contexto y manejo de datos incompletos.

## Notas y decisiones

- Esta épica debe consumir la operativa ya capturada; no debe adelantar trabajo analítico que dependa de modelos fuera del MVP.
- La comparativa histórica es básica y solo aparece cuando haya datos suficientes.
