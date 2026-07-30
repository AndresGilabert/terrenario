---
id: "MVP-004"
tipo: epica
titulo: "Producción y dashboard MVP"
estado: completado
prioridad: critica
hito: "Hito D — Visibilidad operativa MVP"
tickets: []
historias: ["MVP-401", "MVP-402", "MVP-403", "MVP-404", "MVP-405", "MVP-406", "MVP-407", "MVP-499"]
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
actualizado_en: "2026-07-30"
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

- Registro y edición de cosechas con `producto`, `kgs`, `destino`, temporada y, **opcionalmente**, `rendimiento` o `litros` (como mucho uno de los dos, no ambos; RN-004).
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

- [x] **CA-1**: Todas las historias de la épica están en estado `completado`. _(8/8 en `_indice.md`: `MVP-401`..`MVP-407` entregadas y `MVP-499` cerrada.)_
- [x] **CA-2**: El usuario puede registrar cosechas sin ambigüedad entre rendimiento y litros y ver los cuatro widgets mínimos sin error bloqueante. _(`MVP-499`: XOR de RN-004 verificado vía API; los cuatro widgets renderizan sin error de consola.)_
- [x] **CA-3**: El dashboard respeta filtros, taxonomías y reglas de dato incompleto definidas en la KB. _(`MVP-499`: filtros por temporada/terreno, taxonomía de destino con `desconocido`, `kg/árbol` con exclusión de RN-010, y totales que cuadran entre los cuatro agregados y el diario.)_

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-401` — Registro y edición de cosechas.
- `MVP-402` — Reglas de producción, catálogo de producto y destino.
- `MVP-403` — Dashboard MVP: resumen y kg por destino.
- `MVP-404` — Dashboard MVP: kg por terreno y evolución de rendimiento.
- `MVP-405` — Filtros, persistencia de contexto y manejo de datos incompletos.
- `MVP-406` — Navegación del área operativa: agrupación del menú, sección activa y ruta desconocida.
- `MVP-407` — Detalle de terreno con histórico de cosechas y labores (parte de detalle de `P-019`).
- `MVP-499` — Revision epica.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia para todas las historias de esta epica:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo solo aporta referencia visual, estructura de pantallas y flujos UX.
- Si hay contradiccion, prevalece la KB.

Referencia base del prototipo:

- [prototype/terrenario-mvp/src/components/CosechasView.tsx](../../../../prototype/terrenario-mvp/src/components/CosechasView.tsx)
- [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)
- [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)
- [prototype/reports/mvp-prototype-coverage.md](../../../../prototype/reports/mvp-prototype-coverage.md)

Matriz historia -> pantallas/componentes:

| Historia | Referencias de prototipo | Cobertura |
|---|---|---|
| MVP-401 | [prototype/terrenario-mvp/src/components/CosechasView.tsx](../../../../prototype/terrenario-mvp/src/components/CosechasView.tsx), [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx) | Parcial: registro/listado de cosechas disponibles |
| MVP-402 | [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx) | Parcial: destino visible y opciones basicas; regla XOR rendimiento/litros y catalogos cerrados MVP no implementados |
| MVP-403 | [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx) | Parcial: resumen y distribucion por destino disponibles |
| MVP-404 | [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx) | Parcial: produccion por terreno y evolucion disponibles |
| MVP-405 | [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx), [prototype/terrenario-mvp/src/components/TerrenosView.tsx](../../../../prototype/terrenario-mvp/src/components/TerrenosView.tsx) | Parcial: filtro visual disponible; persistencia tras recarga y reglas completas de dato incompleto pendientes |
| MVP-406 | [prototype/terrenario-mvp/src/App.tsx](../../../../prototype/terrenario-mvp/src/App.tsx) | No cubierto: el prototipo no contempla agrupacion del menu, seccion activa ni pantalla de ruta desconocida |
| MVP-407 | [prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx](../../../../prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx) | Parcial: el prototipo aporta la estructura del detalle; los campos son los reales (RN-028), no los inventados (superficie/riego/poda), y el historico sale del diario |

## Notas y decisiones

- Esta épica debe consumir la operativa ya capturada; no debe adelantar trabajo analítico que dependa de modelos fuera del MVP.
- La comparativa histórica es básica y solo aparece cuando haya datos suficientes.
- **Puntos de `MVP-999` asignados a esta épica** (3ª pasada de `MVP-299`, 2026-07-28). Todos estaban
  registrados con la indicación de «revisar al cierre de `MVP-004`»; ahora tienen destino concreto:
  - **`P-021`** — enriquecer la tarjeta de temporada con su producción agregada, que `MVP-203` omitió
    deliberadamente por no inventar métricas sin datos de cosecha. Consumidor: `MVP-403`/`MVP-405`.
  - **`P-045`** — una temporada desbancada se rotula «planificada» aunque sea una campaña pasada.
    `season_status` es un catálogo cerrado de producto y el vocabulario lo fija quien filtre por
    temporada: `MVP-405`. Consolidar con `P-021`.
  - **`P-019`** (parte de detalle) — el modal de detalle de terreno con histórico de cosechas y
    labores, diferido en `MVP-202` porque sus datos dependían de esta épica y de `MVP-003`. **Entregado
    en `MVP-407`** (2026-07-30), leyendo el histórico del diario unificado por terreno. La parte de **ER**
    (coordenadas/`soil_metadata`) sigue en `MVP-999`.
  - **`P-040`** — decidir si el Home pasa a ser la Visión General y qué ocurre con el checklist de
    preparación que entregó `MVP-207`, en vez de acabar con dos pantallas de inicio.
  - **`P-036` + `P-041`** — borrado y **fusión** de registros de maestro creados por error. Su propio
    enunciado los condicionaba a que existieran las entidades operativas necesarias para comprobar el
    «sin uso histórico»: eso ocurre justo aquí. Los decide `MVP-499`.
  - **`P-025` + `P-037` + `P-046`** — deuda de navegación, agrupada en la historia nueva **`MVP-406`**:
    al cerrar esta épica están encendidos los diez módulos y el menú alcanza su tamaño definitivo.
- **`MVP-401` completa `RN-033`.** El diario cronológico que entrega `MVP-305` no puede incluir
  cosechas porque `HARVEST` no existe todavía; encenderlas en el diario es alcance de `MVP-401`
  (hallazgo `G-4` de la revisión previa de `MVP-003`).
- **Cierre de la épica (`MVP-499`, 2026-07-30).** Las ocho historias quedan entregadas y verificadas
  contra la API real y la UI conducida, y los **tres criterios de la épica se cumplen** (ver el veredicto
  por CA en el `spec.md` de `MVP-499`). La revisión no encontró defectos de comportamiento —dashboard y
  cosechas son fieles al contrato— y las correcciones menores detectadas (`R-03`..`R-06`, `R-08`: copy
  obsoleto del Home, huecos de documentación del contrato de cosechas y un nit de comparador) se
  resolvieron en la propia rama de revisión. De los puntos de producto asignados: **`P-040` se resolvió**
  (el Home pasa a ser la Visión General cuando la explotación está preparada) y **`P-036`/`P-041`**
  (borrado y fusión de maestros sin uso) se **difieren a post-MVP** por ser funcionalidad nueva que no
  bloquea la salida. Ninguno rompe un criterio de la épica.
