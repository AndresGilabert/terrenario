---
id: "MVP-699"
tipo: feature
titulo: "Revision epica"
estado: en-progreso
prioridad: media
sprint: ""
hito: "Hito F — Operación medible"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-006--observabilidad-inicial"
depende_de: ["MVP-601", "MVP-602", "MVP-603"]
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
actualizado_en: "2026-08-06"
---

# MVP-699 — Revision epica

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

- [ ] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
- [ ] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
- [ ] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique.

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.

---

## Resultado de la revisión (en curso, 2026-08-06)

### Cómo se hace

Verificación sobre el sistema en marcha, no releyendo la KB: API real, base de datos real y consulta
de los endpoints publicados. Cada hallazgo se numera `R-xx` y dice si se corrige aquí o se deriva.

### Hallazgos

#### `R-01` · La observabilidad no tiene serie temporal: solo ventanas fijas — **corregido aquí**

**Pregunta de partida del PO**: ¿la observabilidad del embudo y del resto de parámetros es visible de
algún modo, o solo consultando la base de datos?

**Qué se comprobó**, contrastando las métricas realmente almacenadas contra la respuesta del endpoint:

- **No está solo en la base de datos**: `GET /api/v1/ops/signals` publica los tres SLO, el embudo de 7
  días, el uso del producto, el monitoreo de negocio mínimo y el estado de las cinco alertas. Hay
  además logs estructurados y aviso por correo.
- **No hay ninguna interfaz**: confirmado con `grep`, ninguna pantalla del cliente consume estas
  señales. Es coherente con el «N/A en fase C» de la tabla de dashboards de la KB.
- **Siete cosas eran solo consultables por SQL**, y una de ellas pesa mucho más que las otras seis: el
  informe solo ofrecía **ventanas fijas** (7 días, 30 días y 30 minutos). Ni un dato por día.

**Por qué importa**: se podía ver que la conversión de la semana es del 82 %, pero no si la semana
anterior fue 90 % o 70 %, ni qué día cayó. Y `kpis.md` declara todos los objetivos «pendientes de
baseline», con las primeras cuatro semanas destinadas a fijarlo — que es exactamente lo que no se
puede hacer sin comparar semanas. Los datos estaban en la tabla; lo que faltaba era exponerlos.

**Corrección aplicada**: `GET /api/v1/ops/signals?days=N` devuelve una **serie por día** (28 días por
defecto, acotada a 1..400) con conversión, uso del dashboard, cobertura de widgets, tasa de error,
P95, altas y minutos observados de cada día. Los días sin datos se emiten con recuentos a `0` y
cocientes a `null`: omitirlos escondería que ese día no se observó nada.

El parámetro **no mueve las ventanas de los SLO**: esas las define la KB y son parte del objetivo, no
una preferencia de consulta. Hay un test que lo fija.

**Evidencia**: sembrando dos días en la base y consultando `?days=10` contra la API real, la serie
muestra la caída de 90 % (día -8) a 60 % (día -1) y los días sin datos como `null`, mientras
`error_rate_7d` sigue ignorando el día -8 por quedar fuera de su ventana.

El resto del desglose que sigue siendo solo-SQL (qué widget falla, qué código de error de Google, qué
recurso se crea, historial de alertas, visitas frente a sesiones, histograma de latencia) se deriva a
`MVP-999` (`P-073`), y la pantalla de operación a `P-074`, por decisión del PO.

### Hallazgos pendientes de la pasada de verificación

> La revisión sigue en curso: quedan por ejercitar el flujo integrado completo y el resto de
> criterios de la épica.
