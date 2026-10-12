---
id: "PRD-101"
tipo: mejora
titulo: "Evolución diaria del rendimiento con regresiones"
estado: en-progreso
prioridad: alta
sprint: ""
hito: "Post-MVP — Preparación para la promoción"
esfuerzo_estimado: ""
tickets: []
epica: "PRD-100--dashboard-listo-para-la-promocion"
depende_de: ["MVP-404"]
bloquea: []
relacionado_con: ["MKT-108"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "kpis", "historico"]
  modulo_path: "03-modulos/produccion-y-dashboard"
  componentes: ["dashboard", "evolucion-rendimiento"]
  etiquetas: ["dashboard", "regresion", "bkl-002"]
  nivel_riesgo: medio
creado_en: "2026-10-12"
actualizado_en: "2026-10-12"
---

# PRD-101 — Evolución diaria del rendimiento con regresiones

## Contexto

Origen: `BKL-002`, solicitud del PO (2026-10-10). El widget «Evolución del rendimiento» de
[MVP-404](../../MVP-004--produccion-y-dashboard-mvp/MVP-404--dashboard-kg-por-terreno-y-evolucion/spec.md)
agrupa por mes o semana y compara con una media histórica fija. Pero el rendimiento graso sube a lo
largo de la campaña: un valor único compara mal el principio con el final. Lo que el agricultor
necesita es saber si su campaña va por encima o por debajo de lo habitual **y cómo se espera que evolucione**,
para decidir si adelanta o retrasa las próximas cosechas.

Este widget **sustituye** al actual; no es uno nuevo.

## Objetivo

Mostrar el rendimiento de la temporada actual día a día y compararlo con la tendencia de las campañas
anteriores, de modo que se vea de un vistazo si va por encima o por debajo y cuál es la evolución esperada.

## Requisitos de usuario

### HU-1 — Ver mi rendimiento día a día

**Como** usuario del Workspace,
**quiero** ver el rendimiento de cada día cosechado,
**para** seguir la campaña sin que se diluya en promedios mensuales.

### HU-2 — Comparar con la tendencia histórica

**Como** agricultor que decide cuándo cosechar,
**quiero** ver la tendencia esperada según los últimos 5 y 10 años,
**para** saber si la campaña va por encima o por debajo y si conviene adelantar o retrasar cosechas.

## Alcance (in-scope)

- Serie diaria de la temporada actual en L/100kg (RN-013). Si un día tiene varias cosechas, su valor es
  el ponderado por kilos.
- Recta de regresión de la temporada actual, dibujada con dos o más fechas distintas y solo sobre
  las fechas con datos. Con una sola fecha se muestra el punto, sin recta.
- Recta de regresión de todo el histórico disponible (desde un año previo) y rectas de 5 y 10 años cuando
  el histórico llega a esa profundidad (RN-015). Sustituyen a la media histórica fija y a sus chips.
- Las rectas históricas cubren todo el periodo histórico de cosecha, no solo las fechas ya cosechadas
  este año, para mostrar la evolución esperada de las semanas siguientes.
- Mismo ámbito que hoy: terrenos filtrados, fechas equivalentes de años anteriores y filtros del dashboard.
- En cada recta, todos los días pesan igual; los kilos solo ponderan el valor de un día con varias cosechas.
- Se retira la agrupación por mes y semana.

## Fuera de alcance (out-of-scope)

- Mensaje de diferencia con la media: la lectura es solo gráfica y leyenda (decisión del PO).
- Predicciones propias o recta de la temporada actual prolongada hacia el futuro.
- Widgets nuevos, exportación y analítica avanzada.
- Captura promocional del widget, que se gestiona en `MKT-108`.

## Criterios de aceptación

- [x] **CA-1**: Un día con varias cosechas se dibuja como un único punto, con rendimiento ponderado por kilos.
- [x] **CA-2**: Un usuario con una única cosecha ve el punto de ese día, sin recta de temporada, y ve las
  rectas históricas si hay histórico.
- [x] **CA-3**: Con dos o más fechas distintas aparece la recta de la temporada, limitada a las fechas con datos.
- [x] **CA-4**: Las rectas de 5 y 10 años solo aparecen cuando el histórico llega a esos años; la general
  aparece desde un año previo con dato (RN-015).
- [x] **CA-5**: Las rectas históricas se extienden por todo el periodo histórico de cosecha, incluidas las
  semanas sin datos de la temporada actual.
- [x] **CA-6**: Un día sin dato de aceite no dibuja punto. Sin cosechas esta temporada, se muestran solo las rectas históricas.
- [x] **CA-7**: El ámbito de terrenos y las fechas comparables coinciden con los de MVP-404; el filtro de
  terreno también afecta al histórico.
- [x] **CA-8**: No queda selector ni parámetro de agrupación por mes/semana.
- [ ] **CA-9**: Los casos anteriores están cubiertos por tests (hecho) y verificados en el sistema en marcha con
  datos sintéticos, en móvil y escritorio (pendiente).

## Maquetas y referencias visuales

- Referencia aportada por el PO en la solicitud (2026-10-12): serie diaria con trazo continuo y recta de
  tendencia discontinua. No está archivada en el repositorio; adjuntarla al cerrar el refinamiento.

## Notas y decisiones

Decisiones del PO en el refinamiento del 2026-10-12:

- La ponderación por kilos se aplica solo al valor diario. En las regresiones todos los días pesan igual,
  porque el rendimiento graso no depende de los kilos del día.
- El mínimo es una fecha, porque hay usuarios con un único bancal y una única cosecha.
- El detalle de cálculo y de contrato está en el [tech-design](./tech-design.md).
