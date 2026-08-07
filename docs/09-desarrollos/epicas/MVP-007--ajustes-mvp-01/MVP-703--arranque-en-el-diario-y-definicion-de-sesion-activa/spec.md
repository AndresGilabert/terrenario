---
id: "MVP-703"
tipo: feature
titulo: "Arranque en el diario y definicion de sesion activa"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-701"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "producto", "observabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["home", "diario", "telemetria-uso"]
  etiquetas: ["mvp", "ajustes", "ux", "kpi"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-703 — Arranque en el diario y definicion de sesion activa

> **Origen**: `P-087` y `P-078` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-087`** — Desde `MVP-499` (`P-040`), con los maestros poblados el Home **es** la Vision General.
Como la cosecha se concentra al final de campana, durante la mayor parte del ano lo primero que se ve
al entrar es «Sin cosechas en {temporada}». Ademas contradice a `RN-033`, que declara el diario
cronologico unificado **vista principal del MVP**: el producto dice que su vista principal es una y
arranca en otra.

**`P-078`** — La senal `app_session_started` se emite desde `AppLayout`, que esta anidado dentro de
`RequireWorkspace`, y esa guarda redirige a `/onboarding` cuando no hay Workspace activo. Resultado: el
endpoint de telemetria permite la senal sin Workspace —y su comentario dice expresamente que lo hace a
proposito— pero el cliente **no la manda nunca** en onboarding. El KPI «uso del dashboard en sesiones
activas» mide en realidad «de las sesiones que llegaron al area operativa, cuantas abrieron el panel»:
una pregunta mas estrecha y mas favorable que la declarada.

Van juntos porque **cambiar el arranque cambia el KPI**: si el Home deja de ser el dashboard, «cuantas
sesiones lo abren» pasa a medir otra cosa. Resolver uno sin el otro dejaria una metrica que nadie sabe
leer.

## Objetivo

Que el producto arranque donde dice que esta su vista principal, y que la definicion de «sesion
activa» que sostiene el KPI de uso coincida con lo que el sistema mide de verdad.

## Requisitos de usuario

### HU-1 — Empezar la jornada donde se trabaja

**Como** titular de la explotacion,
**quiero** entrar directamente al diario de campo,
**para** registrar o consultar el dia a dia sin pasar por un panel que la mayor parte del ano esta vacio.

### HU-2 — Medir lo que se dice medir

**Como** responsable del producto,
**quiero** que el KPI de uso del dashboard declare exactamente el universo que cuenta,
**para** poder interpretarlo sin que sea sistematicamente favorable.

## Alcance (in-scope)

- El destino de arranque del area operativa pasa a ser el **Diario de campo**.
- El checklist de preparacion sigue siendo la primera cara mientras falten maestros por poblar: eso no
  cambia.
- La Vision General conserva su entrada propia en la navegacion lateral.
- Redefinicion documentada de «sesion activa» como «la que llega al area operativa», con correccion
  del comentario de `TelemetryController` y de `docs/05-infraestructura/observabilidad.md`, que hoy
  afirman otra cosa.
- Reformulacion del KPI de uso del dashboard para que siga siendo legible cuando el panel deja de ser
  la pantalla de arranque.

## Fuera de alcance (out-of-scope)

- Destino de arranque configurable por usuario: descartado por el PO en la clasificacion (una
  preferencia mas en un producto con un solo perfil de usuario).
- Cambiar el contenido del diario o del dashboard.
- Emitir la senal de sesion tambien en onboarding: descartado, meteria en el divisor sesiones en las
  que el panel todavia no existe.

## Criterios de aceptación

- [ ] **CA-1**: Con los maestros poblados, entrar en el area operativa lleva al Diario de campo.
- [ ] **CA-2**: Con maestros pendientes, sigue apareciendo el checklist de preparacion.
- [ ] **CA-3**: La Vision General sigue siendo alcanzable en un clic desde la navegacion.
- [ ] **CA-4**: El comentario del codigo y la KB describen «sesion activa» de la misma forma, y esa
  forma coincide con lo que se emite realmente.
- [ ] **CA-5**: El KPI de uso del dashboard queda redefinido y documentado para el nuevo arranque, con
  nota explicita de que su serie historica no es comparable con la anterior.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| DiarioView | RN-033 (vista principal) | parcial | Existe, pero no es el arranque |

## Notas y decisiones

- Esta historia **revierte parcialmente `P-040`**, que en `MVP-499` decidio que el Home fuese la Vision
  General. La decision no era mala: lo que faltaba era el dato de que la cosecha se concentra al final
  de campana y el panel esta vacio casi todo el ano.
- Queda explicito que la serie del KPI se rompe aqui. Preferimos una ruptura declarada a una metrica
  que cambia de significado en silencio.
