---
id: "MVP-706"
tipo: feature
titulo: "Comportamiento de la Vision General"
estado: completado
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "observabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "widgets", "telemetria-uso"]
  etiquetas: ["mvp", "ajustes", "bug", "kpi"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-706 — Comportamiento de la Vision General

> **Origen**: `P-075` y `P-085` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-075`** — Las cuatro peticiones de la Vision General van en un unico `Promise.all`, asi que si una
falla se descarta el resultado de las otras tres y la pantalla muestra solo el mensaje de error.
`MVP-602` lo asume al medir (informa los cuatro widgets como `error`), pero la experiencia es peor de
lo necesario y la medida tambien: no se puede atribuir el fallo al widget que lo causo, que es justo lo
que pide el KPI de cobertura.

**`P-085`** — El boton «Actualizar» existe por `RN-006` (no hay refresco en segundo plano) y es ademas
la **unica fuente** de la senal `dashboard.manual_refresh` que `MVP-602` (CA-2) usa para medir el uso
del panel. **Decision del PO (2026-08-06): se retira.** Motivo: el objetivo son explotaciones pequenas,
donde no va a ser habitual que unos usuarios introduzcan datos mientras otros esperan a que el panel se
actualice. El refresco pasa a ser recargar la pagina o volver a entrar en la pantalla.

Van juntas porque las dos tocan la misma pantalla y la misma instrumentacion.

## Objetivo

Que un fallo parcial no vacie el panel entero, y que el refresco del dashboard sea el que el PO ha
decidido, con la regla y la metrica alineadas con esa decision.

## Requisitos de usuario

### HU-1 — No perder tres widgets por culpa de uno

**Como** titular de la explotacion,
**quiero** ver los datos que si se han podido calcular,
**para** no quedarme sin panel porque una parte falle.

## Alcance (in-scope)

- `Promise.allSettled` en la carga de la Vision General, con estado de error **acotado al widget** que
  falla y el resto pintado.
- Atribucion del fallo al widget concreto en la senal de cobertura de `MVP-602`.
- **Retirada del boton «Actualizar»**.
- Reescritura de `RN-006`: el refresco es recarga de pagina o reentrada en la pantalla, no un acto
  explicito con control propio. Debe quedar escrito el motivo (perfil de uso de explotacion pequena).
- Retirada de la senal `dashboard.manual_refresh` del informe de `GET /api/v1/ops/signals`, o su
  declaracion explicita como discontinuada, y ajuste del `CA-2` de `MVP-602` en su spec.

## Fuera de alcance (out-of-scope)

- Refresco automatico, en segundo plano o al recuperar el foco de la ventana: descartado por el PO.
- Cambiar los widgets o su contenido; el widget economico es `MVP-707`.

## Criterios de aceptación

- [x] **CA-1**: Si una de las peticiones del dashboard falla, las demas se pintan y solo el widget
  afectado muestra su error. Verificado sobre la aplicacion en marcha interceptando **solo**
  `GET /dashboard/kg-by-plot` para que devuelva `500`: los otros tres widgets siguen con sus datos
  reales y el error aparece en el sitio del widget caido, con el mensaje que devolvio la API.
- [x] **CA-2**: La medida de cobertura de widgets identifica **cual** falla, no los cuatro. Senal
  emitida en ese mismo escenario: `kg_by_plot: error` y los otros tres con su estado propio.
- [x] **CA-3**: El boton «Actualizar» ya no existe en la Vision General. El pie de la pantalla explica
  que el refresco es recargar la pagina o volver a entrar.
- [x] **CA-4**: `RN-006` describe la estrategia de refresco vigente y su motivo (perfil de uso de
  explotacion pequena), y `RN-007` se ajusta para no seguir hablando de «recarga manual».
- [x] **CA-5**: `dashboard.manual_refresh` no aparece en el informe operativo como metrica viva —se
  retiran `product_usage_7d.manual_refresh_per_session` y `daily[].manual_refresh`, con test de
  integracion que lo fija— y el `CA-2` de `MVP-602` queda marcado como **superado** en su propio spec.
  El evento se sigue **aceptando** en el endpoint de telemetria para no responder `400` a un cliente
  cacheado tras el despliegue.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| DashboardView | RN-006, RN-009 | hecho | Fallo parcial acotado al widget y refresco reescrito; verificado con un `500` real en una sola de las cuatro peticiones |

## Notas y decisiones

- **Retirar el boton tiene consecuencias en la KB, no solo en la pantalla.** Esta historia no se puede
  cerrar dejando `RN-006` y el `CA-2` de `MVP-602` describiendo un mundo que ya no existe.
- Va **antes** de `MVP-707`, que toca la misma vista.
