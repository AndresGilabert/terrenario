---
id: "MVP-407"
tipo: feature
titulo: "Detalle de terreno con histórico de cosechas y labores"
estado: completado
prioridad: media
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-202", "MVP-301", "MVP-401", "MVP-305"]
bloquea: []
relacionado_con: ["MVP-202", "MVP-305"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["terrenos", "diario", "produccion"]
  modulo_path: "03-modulos/"
  componentes: ["terrenos", "detalle-terreno", "diario"]
  etiquetas: ["mvp", "terrenos", "historico", "deuda"]
  nivel_riesgo: bajo
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# MVP-407 — Detalle de terreno con histórico de cosechas y labores

## Contexto

El maestro de terrenos (`MVP-202`) entregó listado, alta, edición e inactivación, pero **difirió el
modal de detalle** del prototipo (`TerrenoDetailModal`): su valor es el **histórico por parcela**
—cosechas y labores—, cuyos datos no existían hasta que `MVP-003` (diario/actividades) y `MVP-004`
(cosechas) los encendieron. Registrado como `P-019` (parte de detalle), con destino esta épica.

Con `HARVEST` y el diario unificado (`MVP-305`) ya vivos, se puede cerrar: el detalle es una **lectura**
que compone lo que un terreno tiene detrás.

## Objetivo

Dar a cada terreno una vista de detalle que muestre sus datos y su historia —qué se ha recolectado y qué
labores se han hecho en esa parcela— sin salir del maestro.

## Requisitos de usuario

### HU-1 — Ver la ficha completa de un terreno

**Como** usuario del maestro de terrenos,
**quiero** abrir el detalle de una parcela y ver sus datos,
**para** consultarla y editarla sin recorrer la tarjeta comprimida.

### HU-2 — Ver qué ha dado y qué se ha hecho en una parcela

**Como** usuario que revisa una parcela,
**quiero** ver su histórico de cosechas y de labores,
**para** entender su rendimiento y su actividad a lo largo de las campañas.

## Alcance (in-scope)

- Vista de detalle (modal) abierta desde la tarjeta del maestro, con los **campos reales** del terreno
  (RN-028): nombre, alias, tipo de propiedad, propietario, referencia catastral, ubicación, nº de
  árboles y estado.
- Histórico de **cosechas** de la parcela (fecha, kg, destino, rendimiento), a través de todas las
  temporadas, en orden cronológico inverso, con estado vacío explícito.
- Histórico de **labores** (actividades) de la parcela (fecha, tarea, responsable, horas), con estado
  vacío explícito.
- Reutilización del formulario de edición del maestro desde el propio detalle.
- El histórico se lee del diario unificado (`GET /api/v1/diary?plot_id=…`, MVP-305), sin endpoint nuevo.

## Fuera de alcance (out-of-scope)

- Campos inventados por el prototipo que **no están en el modelo** (superficie, sistema de riego, estado
  de poda, imagen): fuera por RN-028; no se recuperan aquí.
- Reconciliación del ER de `PLOT` (coordenadas/`soil_metadata`): es la **otra parte de `P-019`**, que
  sigue en `MVP-999` sin impacto funcional.
- Compras y consumos de la parcela: el detalle se centra en cosechas y labores (P-019). El diario
  completo ya los muestra en su vista.
- Paginación del histórico: hoy no la hay en el diario (`P-051`); cuando llegue, aplica igual aquí.

## Criterios de aceptación

- [x] **CA-1**: Desde el maestro se abre el detalle de un terreno, que muestra sus datos reales (no los
  inventados por el prototipo) y permite editarlo reutilizando el formulario del maestro.
- [x] **CA-2**: El detalle muestra el histórico de cosechas del terreno —fecha, kg, destino,
  rendimiento— de todas las temporadas, con estado vacío explícito cuando no hay ninguna.
- [x] **CA-3**: El detalle muestra el histórico de labores (actividades) del terreno —fecha, tarea,
  responsable, horas—, con estado vacío explícito cuando no hay ninguna.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx](../../../../../prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx)

> El prototipo se usa solo como referencia visual. La fuente de verdad funcional es la KB: los campos
> son los del modelo real (RN-028), no los inventados por el prototipo.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TerrenoDetailModal | RN-028, RN-033 | cubierto | Detalle con campos reales + histórico de cosechas y labores del diario por terreno |

## Notas y decisiones

- **Origen.** Cierra la **parte de detalle** de `P-019`, diferida en `MVP-202` porque sus datos dependían
  de `MVP-003`/`MVP-004`. La parte de **ER** (coordenadas/`soil_metadata`) sigue en `MVP-999`.
- **Solo frontend.** El histórico se compone con el diario unificado (`MVP-305`), que ya acepta
  `plot_id` y devuelve cosechas y labores con sus campos. Sin endpoint, sin migración.
- **Campos reales, no los del prototipo.** El maestro (`MVP-202`) ya descartó superficie/riego/poda por
  RN-028; el detalle es coherente con esa decisión y no los reintroduce.
