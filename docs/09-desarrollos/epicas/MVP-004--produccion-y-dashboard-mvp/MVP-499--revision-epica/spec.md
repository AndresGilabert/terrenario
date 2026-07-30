---
id: "MVP-499"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-401", "MVP-402", "MVP-403", "MVP-404", "MVP-405", "MVP-406", "MVP-407"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "scope-control"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "qa", "stabilization"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-07-30"
---

# MVP-499 — Revision epica

## Contexto

Durante la ejecucion de una epica aparecen ajustes, puntos ciegos y necesidades no previstas en las historias originales. Si no se centralizan antes del cierre, se dispersan y se pierde trazabilidad para decidir el trabajo posterior.

## Objetivo

Ejecutar una revision final de la epica para validar el funcionamiento global, consolidar los pendientes detectados y convertirlos en nuevas historias planificables.

## Requisitos de usuario

### HU-1 — Consolidar pendientes de la epica

**Como** Product Owner,
**quiero** reunir en un solo punto los ajustes y requisitos detectados durante la epica,
**para** evitar omisiones y cerrar el alcance con trazabilidad.

### HU-2 — Verificar calidad funcional final

**Como** equipo de producto y desarrollo,
**quiero** revisar el estado final de la epica sobre el flujo integrado,
**para** abrir nuevas historias concretas con evidencias de error o falta.

## Alcance (in-scope)

- Revision integral del comportamiento entregado por la epica.
- Consolidacion de puntos ciegos y requisitos pendientes detectados durante las historias previas.
- Creacion de nuevas historias para cubrir errores, faltas o ajustes detectados.
- Priorizacion inicial de los nuevos items segun impacto funcional y de negocio.

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los nuevos cambios detectados.
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [x] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado. _(Ver «Revisión funcional» y el veredicto por CA de épica más abajo, con evidencia sobre la API y la UI conducidas.)_
- [x] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados. _(Hallazgos `R-01`..`R-08` abajo.)_
- [x] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique. _(Correcciones menores hechas en esta rama; decisiones de producto P-040/P-036/P-041 resueltas o diferidas con destino explícito.)_

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Revisión funcional (CA-1)

Pasada de verificación real sobre el flujo integrado (backend + UI conducidos con JWT de dev, Workspace
«Rafa»), no solo relectura de la KB. Veredicto por criterio de la épica `MVP-004`:

| CA de la épica | Veredicto | Evidencia |
|---|---|---|
| **CA-1** (todas las historias `completado`) | ✅ | `_indice.md`: `MVP-401`..`MVP-407` entregadas; `MVP-499` cierra la épica |
| **CA-2** (registrar cosechas sin ambigüedad rendimiento/litros + 4 widgets sin error bloqueante) | ✅ | XOR (RN-004) verificado vía API: ambos → `400 VALIDATION_HARVEST_XOR_YIELD_LITERS`, ninguno → `201` (correcto: RN-004 = «como mucho uno»). Los cuatro widgets renderizan sin error de consola; conversión `kg_100kg`→L/100kg y catálogos verificados |
| **CA-3** (dashboard respeta filtros, taxonomías y dato incompleto) | ✅ | Filtro de temporada cambia los totales (2026: 4460,5; 2025: 1000; 2027: 0) y cuadra con `kg-by-season`; taxonomía de destino incluye `desconocido`; `kg/árbol` excluye terrenos sin `tree_count` y avisa; **totales cuadran** entre `summary`, `kg-por-destino`, `kg-por-terreno` y el diario (todos 4460,5): sin doble contabilización |

Cobertura de contrato: auditoría campo a campo de los endpoints de dashboard y de cosechas contra
`contratos-api.md`; ambos **fieles al contrato** en toda regla de cálculo y filtro. Sin defectos de
comportamiento; los hallazgos son de documentación, un copy obsoleto y un nit de comparador.

## Hallazgos (CA-2) y su resolución (CA-3)

| ID | Tipo | Descripción | Resolución |
|---|---|---|---|
| `R-01` | verificación | Consistencia de totales entre los cuatro agregados y el diario | ✅ Pasa (sin acción) |
| `R-02` | verificación | XOR de cosecha (RN-004): ambos→400, ninguno→201, conversión de unidad | ✅ Pasa (sin acción) |
| `R-03` | defecto/ux | El copy de bienvenida del **Home** aún decía que cosechas y Visión General «quedan por llegar, marcadas «Pronto»» (obsoleto: encendidas; MVP-406 retiró las etiquetas «Pronto») | **Corregido en esta rama** (subsumido en P-040) |
| `R-04` | contrato/doc | La sección de cosechas de `contratos-api.md` no documentaba la divergencia de códigos POST/PATCH (`VALIDATION_REQUIRED` alcanzable en PATCH; `date` del alta → `VALIDATION_HARVEST_REQUIRED_FIELDS`) | **Corregido en esta rama** (nota añadida) |
| `R-05` | contrato/doc | El código del filtro de fechas de `GET /harvests` (`VALIDATION_REQUIRED`) no estaba documentado | **Corregido en esta rama** |
| `R-06` | nit/consistencia | El desempate alfabético usaba `Ordinal` en destinos y `OrdinalIgnoreCase` en terrenos, pese a que el contrato dice «mismo criterio» (sin efecto hoy: claves canónicas en minúscula) | **Corregido en esta rama** (alineado a `OrdinalIgnoreCase`) |
| `R-07` | matiz/rounding | Doble redondeo al derivar litros → deriva de ~0,01 L/100kg en el rendimiento medio de partidas que declararon litros | **Anotado, aceptable** (sin acción): coherente con la precisión declarada; el resumen es autoconsistente |
| `R-08` | doc | El alcance de la épica decía «uno entre rendimiento o litros», impreciso frente a RN-004 («como mucho uno») | **Corregido en esta rama** |

## Decisiones de producto (puntos asignados a esta revisión)

- **`P-040` — Encaje del Home con la Visión General. Resuelto (decisión del PO, 2026-07-30):** el Home
  tiene **dos caras** según la preparación del Workspace. Mientras quede algún maestro por poblar,
  muestra la bienvenida y el checklist «Prepara tu explotación»; **cuando la explotación está
  preparada, el Home pasa a ser la Visión General** (se reutiliza la misma vista, no se duplica). Así no
  hay dos pantallas de inicio compitiendo. Verificado en UI: con `tasks=0` se ve el checklist (3/4);
  con todos los maestros poblados, `/app` renderiza el dashboard.
- **`P-036` (borrado físico de un maestro sin uso) + `P-041` (fusión de maestros duplicados). Diferidos
  a post-MVP (decisión del PO):** no son defectos sino **funcionalidad nueva** (backend + UI) que no
  bloquea la salida a MVP. Se anota que su premisa —poder comprobar el «sin uso histórico»— **ya se
  cumple** (existen las entidades operativas), así que quedan listos para priorizarse. Siguen en
  `MVP-999`.

## Notas y decisiones

- Esta historia se ejecuta como cierre de la épica; la épica `MVP-004` se marca `completado` al cerrarla.
- **Ampliación de alcance (patrón `MVP-x99`):** «implementar los cambios detectados» está fuera de
  alcance, pero —como en `MVP-299`/`MVP-399`— se admiten **correcciones de cierre**: los defectos
  pequeños y acotados (`R-03`..`R-06`, `R-08`) se arreglan en esta misma rama; solo las decisiones de
  producto o la funcionalidad nueva (P-040 resuelto; P-036/P-041 diferidos) se gestionan aparte.
