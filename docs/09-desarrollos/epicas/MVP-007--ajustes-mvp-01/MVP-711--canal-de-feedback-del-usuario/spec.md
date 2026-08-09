---
id: "MVP-711"
tipo: feature
titulo: "Canal de feedback del usuario"
estado: completado
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["producto", "soporte", "privacidad"]
  modulo_path: "03-modulos/"
  componentes: ["feedback", "email", "shell"]
  etiquetas: ["mvp", "ajustes", "soporte"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-711 — Canal de feedback del usuario

> **Origen**: `P-088` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

No existe ningun canal para que un usuario reporte un fallo o pida algo. Confirmado por busqueda en
todo el cliente: no hay «Ayuda», «Contacto», «Soporte» ni `mailto` fuera de las paginas legales. La
unica direccion publicada es la de **privacidad**, en la Politica, que es un canal de derechos RGPD y
no un canal de producto.

Un usuario real que se topa con un fallo no tiene donde decirlo, y el equipo se entera solo por las
alertas de `MVP-603`, que ven caidas y errores 5xx pero no «esto no se entiende» ni «me falta esto».

**Decision del PO (2026-08-06)**: formulario propio que envia por correo, **sin terceros**. Un widget
de tickets externo cargaria scripts y entraria en `RN-042`, obligando a montar el banner de cookies que
`MVP-505` evito precisamente porque el producto no usa nada no esencial.

## Objetivo

Que el usuario pueda decir que algo no funciona desde dentro del producto, y que lo que llegue baste
para reproducirlo.

## Requisitos de usuario

### HU-1 — Reportar un problema sin salir de la aplicacion

**Como** titular de la explotacion,
**quiero** contar lo que me ha pasado desde donde me ha pasado,
**para** no tener que buscar a quien escribir ni explicar donde estaba.

### HU-2 — Recibir algo accionable

**Como** responsable del producto,
**quiero** que el aviso llegue con contexto tecnico,
**para** poder reproducir el problema sin una conversacion de ida y vuelta.

## Alcance (in-scope)

- Entrada visible en el shell (navegacion lateral o Ajustes) hacia «Enviar sugerencia o incidencia».
- Formulario con tipo (incidencia / sugerencia), texto libre y envio.
- Envio por el `SmtpMailer` ya existente, con **contexto tecnico adjunto**: version desplegada, ruta
  desde la que se envia, `X-Request-Id` de la ultima peticion fallida si lo hay, y navegador.
- Confirmacion al usuario de que se ha enviado.
- Actualizacion de `docs/07-seguridad/privacidad-datos.md`: que dato se recoge, con que base y cuanto
  se conserva.

## Fuera de alcance (out-of-scope)

- Herramienta de tickets externa, con o sin widget: descartada por el PO por su encaje con `RN-042` y
  por anadir un encargado de tratamiento.
- Estados, asignacion o seguimiento del reporte dentro del producto.
- Chat, conversacion o respuesta desde la aplicacion.
- Adjuntar capturas o ficheros.

## Criterios de aceptación

- [x] **CA-1**: Existe una entrada visible al canal desde el area operativa.
  **Evidencia**: «Sugerencias e incidencias» en la seccion **Configuracion** de la navegacion lateral
  (`AppSidebar.tsx`), visible desde cualquier pantalla del shell y en el drawer de movil. Lleva a
  `/app/feedback`, ruta propia dentro del shell y **fuera** de la guarda de oferta de temporada
  (`App.tsx`). Se descarto colgarla como panel al final de `AjustesView`: esa pantalla termina en la
  zona de baja de cuenta, deliberadamente la ultima por ser lo irreversible (`MVP-505`).
- [x] **CA-2**: Enviar un reporte produce un correo al destinatario de operacion con el texto y el
  contexto tecnico.
  **Evidencia**: `POST /api/v1/feedback` → `SubmitFeedbackHandler` → `SmtpFeedbackEmailSender`, que
  compone con `ProductEmailTemplate` (`MVP-715`) y entrega por `SmtpMailer` (ADR-0010) al buzon
  `Feedback:Recipient`. El correo es el **sexto** del inventario: entra en `ProductEmailCatalog` y en
  `docs/06-integraciones/correos-del-producto.md`. Contexto tecnico comprobado por
  `FeedbackEmailComposerTests` (version, ruta, `X-Request-Id` y navegador en HTML y en texto plano) y
  el recorrido completo por `FeedbackControllerTests`. **No verificado**: la recepcion real en una
  bandeja. Se comprobo en `MVP-799` contra la bandeja de pruebas de Mailtrap que el proyecto ya tenia
  configurada (`docs/05-infraestructura/desarrollo-local.md`).
- [x] **CA-3**: El usuario recibe confirmacion en pantalla de que se ha enviado, y un mensaje util si
  falla.
  **Evidencia**: `FeedbackView.tsx` muestra un `role="status"` al recibir el `202` y vacia el
  formulario; ante error muestra un `role="alert"` con el mensaje de la API, que distingue cupo
  agotado (`RATE_LIMIT_FEEDBACK`), canal sin configurar (`FEEDBACK_CHANNEL_UNAVAILABLE`) y fallo de
  entrega (`FEEDBACK_DELIVERY_FAILED`). Un fallo de envio **no confirma nada**:
  `FeedbackControllerTests.Deberia_NoConfirmarNada_Cuando_ElEnvioFalla`.
- [x] **CA-4**: No se carga ningun recurso ni script de terceros: `RN-042` sigue sin activarse y la CSP
  no se toca.
  **Evidencia**: no hay widget, iframe, script ni `fetch` a ningun dominio ajeno; los iconos de la
  pantalla son de la tipografia Material Symbols ya autoalojada desde `MVP-505`. `vite.config.ts` no
  se modifica y la suite `sin-recursos-externos.test.ts` sigue en verde. El correo tampoco los lleva:
  `ProductEmailInventoryTests.CadaCorreo_Deberia_ViajarSinRecursosRemotos` recorre ahora seis correos.
- [x] **CA-5**: El tratamiento del dato queda descrito en `privacidad-datos.md` con su plazo, coherente
  con `RN-041`.
  **Evidencia**: seccion «Canal de sugerencias e incidencias (MVP-711)» en
  `docs/07-seguridad/privacidad-datos.md`, con que se recoge, que **no** se recoge, la base
  (interes legitimo del art. 6.1.f, con su ponderacion) y el plazo: 24 meses en el buzon, el mismo
  criterio de `RN-041`, con la salvedad escrita de que es el unico plazo que **no ejecuta ninguna
  rutina** porque no vive en la base de datos. Fila anadida tambien a la tabla de retencion.
- [x] **CA-6**: El envio esta protegido frente a abuso basico (limite por sesion o por usuario).
  **Evidencia**: `FeedbackRateLimiter`, **tres reportes por hora y cuenta** en ventana deslizante y
  **en servidor**: deshabilitar el boton en el cliente ordena la pantalla pero no es un limite. Supera
  el cupo → `429` con `Retry-After`. El cupo se consume **al entregar**, no al intentar, para no
  castigar a nadie por una caida del proveedor de correo. Cubierto por `FeedbackRateLimiterTests`.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| AjustesView | RN-017, RN-041, RN-042 | hecho | Canal como **pantalla propia** (`/app/feedback`) con entrada en la navegacion, no como panel de Ajustes: esa pantalla acaba en la baja de cuenta y el canal habria quedado por debajo de lo irreversible. `FeedbackView.test.tsx` (6 casos), `report-context.test.ts` (4), `http-client.test.ts` (3 nuevos) y 27 casos de backend en `Tests/Feedback/` |

## Notas y decisiones

- El correo del destinatario **no va al repositorio**: mismo criterio que `Ops__AlertEmail`, que vive en
  user-secrets y en la configuracion del App Service porque el repositorio es publico.
- El contexto tecnico adjunto no puede incluir datos operativos del Workspace: basta con donde estaba y
  que peticion fallo.
