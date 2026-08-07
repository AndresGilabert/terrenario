---
id: "MVP-799"
tipo: feature
titulo: "Revision epica"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-701", "MVP-702", "MVP-703", "MVP-704", "MVP-705", "MVP-706", "MVP-707", "MVP-708", "MVP-709", "MVP-710", "MVP-711", "MVP-712", "MVP-713", "MVP-714", "MVP-715", "MVP-716"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "gobernanza"]
  modulo_path: "03-modulos/"
  componentes: ["revision", "verificacion"]
  etiquetas: ["mvp", "ajustes", "revision"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-799 — Revision epica

> **Origen**: — del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

Revision de cierre de la epica, con la misma mecanica que `MVP-199`, `MVP-299`, `MVP-399`, `MVP-499`,
`MVP-599` y `MVP-699`: pasada de verificacion **real** contra el flujo integrado —no relectura de la
KB— para producir hallazgos con evidencia, veredicto por cada criterio de aceptacion de la epica y
derivacion de lo que no se corrija.

Esta revision tiene ademas un encargo propio: **comprobar que ningun punto aprobado se ha quedado sin
construir**. Es el `CA-6` de la epica y existe porque `P-055` se perdio exactamente asi.

## Objetivo

Dar por cerrada la epica solo si lo que dice estar hecho lo esta, medido contra el sistema en marcha.

## Requisitos de usuario

### HU-1 — Cerrar sobre evidencia

**Como** Product Owner,
**quiero** que el cierre de la epica se sostenga en verificacion real,
**para** no repetir el patron de dar por resuelto lo que solo estaba anotado.

## Alcance (in-scope)

- Verificacion conducida de los seis criterios de aceptacion de la epica.
- Contraste numerico entre pantallas para `CA-2`: cifras de la misma campana en diario, cosechas,
  compras y Vision General, sacadas de la API con datos reales.
- Comprobacion de que los 25 puntos aprobados tienen historia que los recoge y evidencia de cierre.
- Correcciones de cierre acotadas en la propia rama de revision, segun el criterio ya establecido en el
  proyecto.
- Alta en `MVP-999` de los hallazgos que necesiten decision de producto o consumidor posterior.
- Actualizacion de las reglas de negocio modificadas, si alguna historia la dejo a medias.

## Fuera de alcance (out-of-scope)

- Implementar funcionalidad nueva.
- Reabrir decisiones ya tomadas en la clasificacion del 2026-08-06/07.

## Criterios de aceptación

- [ ] **CA-1**: Tabla de veredicto con evidencia por cada criterio de aceptacion de `MVP-007`.
- [ ] **CA-2**: Hallazgos numerados `R-xx` con evidencia reproducible.
- [ ] **CA-3**: Trazabilidad punto a punto: los 25 puntos aprobados aparecen como `resuelto` en el
  registro de `MVP-999`, con la historia que los cerro.
- [ ] **CA-4**: Los derivados quedan dados de alta como `P-xxx` con destino explicito.
- [ ] **CA-5**: `RN-006`, `RN-008`, `RN-009`, `RN-029` y `RN-041` reflejan el estado real del producto.

## Maquetas y referencias visuales

No aplica.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| Todas | Criterios de aceptacion de MVP-007 | pendiente | Pendiente |

## Notas y decisiones

- Los hallazgos con filo salen de **contar y comparar cifras del sistema en marcha**, no de releer. En
  esta epica el contraste obvio es el de `CA-2`, que es el mismo metodo con el que se detecto `P-082`.
