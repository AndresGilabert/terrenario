---
id: "MVP-711"
tipo: feature
titulo: "Canal de feedback del usuario"
estado: borrador
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
actualizado_en: "2026-08-07"
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

- [ ] **CA-1**: Existe una entrada visible al canal desde el area operativa.
- [ ] **CA-2**: Enviar un reporte produce un correo al destinatario de operacion con el texto y el
  contexto tecnico.
- [ ] **CA-3**: El usuario recibe confirmacion en pantalla de que se ha enviado, y un mensaje util si
  falla.
- [ ] **CA-4**: No se carga ningun recurso ni script de terceros: `RN-042` sigue sin activarse y la CSP
  no se toca.
- [ ] **CA-5**: El tratamiento del dato queda descrito en `privacidad-datos.md` con su plazo, coherente
  con `RN-041`.
- [ ] **CA-6**: El envio esta protegido frente a abuso basico (limite por sesion o por usuario).

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| AjustesView | RN-017, RN-041, RN-042 | falta | No existe canal de producto |

## Notas y decisiones

- El correo del destinatario **no va al repositorio**: mismo criterio que `Ops__AlertEmail`, que vive en
  user-secrets y en la configuracion del App Service porque el repositorio es publico.
- El contexto tecnico adjunto no puede incluir datos operativos del Workspace: basta con donde estaba y
  que peticion fallo.
