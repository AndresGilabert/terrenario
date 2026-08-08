---
id: "MVP-715"
tipo: feature
titulo: "Correos del producto: inventario y maquetacion unificada"
estado: completado
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["comunicaciones", "ux", "legal"]
  modulo_path: "03-modulos/"
  componentes: ["email", "plantillas", "smtp"]
  etiquetas: ["mvp", "ajustes", "email"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-715 — Correos del producto: inventario y maquetacion unificada

> **Origen**: `P-001` y `P-030` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

El transporte de correo ya es comun (`SmtpMailer`, extraido en `MVP-206`), pero **la composicion sigue
siendo ad-hoc por tipo de correo**: cada flujo que necesito enviar algo se escribio su mensaje. Hoy
conviven al menos la invitacion por email, el aviso de baja de Workspace con enlace de reactivacion, el
aviso de solicitud a quien dio de baja y las notificaciones de la baja de cuenta.

`P-001` pedia inventario y plantillas unificadas con criterios de contenido legal (RGPD/LOPDGDD y
LSSI/ePrivacy donde aplique); `P-030` anadio dos plantillas nuevas que refuerzan lo mismo.

Ninguno esta roto: lo que falta es coherencia visual y de contenido, y saber cuantos son.

> El recuento de este parrafo es el de partida y **resulto ser incorrecto**. Lo corregido esta en
> «Hallazgo del inventario», mas abajo.

## Objetivo

Que todos los correos que salen del producto se reconozcan como del mismo producto y digan lo que
legalmente tienen que decir, con una sola forma de componerlos.

## Requisitos de usuario

### HU-1 — Reconocer un correo del producto

**Como** persona que recibe un aviso,
**quiero** que se vea que viene de Terrenario y que sepa que hacer con el,
**para** no confundirlo con correo no deseado.

### HU-2 — Cumplir sin revisarlo caso a caso

**Como** responsable de cumplimiento,
**quiero** que el pie legal y la identificacion del responsable esten en la plantilla,
**para** que ningun correo nuevo salga sin ellos.

## Alcance (in-scope)

- **Inventario** completo de los correos salientes del producto, con su disparador, su destinatario y
  su contenido actual.
- Plantilla comun (cabecera, cuerpo, llamada a la accion, pie legal) sobre el `SmtpMailer` existente.
- Criterios de contenido legal aplicados en la plantilla: identificacion del responsable, motivo del
  envio y, donde aplique, forma de dejar de recibirlo.
- Migracion de todos los correos inventariados a la plantilla comun.
- Version en texto plano de cada correo.

## Fuera de alcance (out-of-scope)

- **Aviso de invitacion anulada** (`P-039`): **descartado** por el PO. Basta con que se avise al
  intentar unirse, que es lo que ya hace el preview del enlace.
- Correos nuevos que no existan hoy.
- Plataforma de envio, seguimiento de aperturas o marketing de ningun tipo.
- Notificaciones in-app (`P-011`, `P-029`), que quedan en backlog.

## Criterios de aceptación

- [x] **CA-1**: Existe el inventario de correos salientes en la KB, con disparador y destinatario.
  `docs/06-integraciones/correos-del-producto.md`. **Son cinco, no cuatro**: ver «Hallazgo del
  inventario» mas abajo.
- [x] **CA-2**: Todos los correos del inventario usan la plantilla comun.
  Los cinco se componen con `ProductEmailTemplate`; ningun emisor construye marcado.
  `ProductEmailCatalog` los recorre en la suite.
- [x] **CA-3**: La plantilla incluye la identificacion del responsable y el motivo del envio.
  `ProductEmailInventoryTests.CadaCorreo_Deberia_IdentificarAlResponsableDelTratamiento` y
  `...DecirPorQueSeEnvia`, sobre los cinco y en las dos versiones. La identidad sale del mismo
  `legal-entity.json` que alimenta la Politica de Privacidad publicada, incrustado en la API.
- [x] **CA-4**: Cada correo tiene version en texto plano.
  `ProductEmailInventoryTests.CadaCorreo_Deberia_TenerVersionEnTextoPlano`. Las dos versiones salen
  del mismo contenido, asi que no pueden decir cosas distintas.
- [x] **CA-5**: Verificado **enviando cada tipo de correo de verdad** —los cinco, por SMTP, con el
  `SmtpMailer` de produccion— contra un receptor local que captura lo que MailKit pone en el cable
  (`scripts/smtp-sink.py`). No es un doble del emisor: habla SMTP por el socket.

  El HTML renderizado **no bastaba**, y esa fue la correccion del PO. El cuerpo es justo lo unico que
  no puede romperse en el envio; lo que solo aparece al poner el mensaje en el cable es el sobre. Medido
  en los cinco: `multipart/alternative` con las dos partes en `utf-8` y `quoted-printable` —acentos
  incluidos—, pie legal presente en **ambas** partes, `From`/`Subject` como los ve la bandeja, y
  **ningun host remoto** en ninguno.

  | Correo | Tamano | Partes | Pie legal | Remotos |
  |---|---|---|---|---|
  | Invitacion | 4.875 B | texto + HTML utf-8 | si | ninguno |
  | Baja de Workspace | 4.454 B | texto + HTML utf-8 | si | ninguno |
  | Solicitud de reactivacion | 4.045 B | texto + HTML utf-8 | si | ninguno |
  | Alerta disparada | 3.398 B | texto + HTML utf-8 | si | ninguno |
  | Alerta resuelta | 3.238 B | texto + HTML utf-8 | si | ninguno |

  La **invitacion se probo ademas de extremo a extremo**: `POST /workspaces/invitations` contra la API
  en marcha, sobre un Workspace desechable, con la API apuntando al receptor. Respondio
  `email_sent: true` y el mensaje llego con la plantilla nueva.

  Reproducible: `ProductEmailDeliveryTests` reenvia los cinco cuando se define
  `TERRENARIO_SMTP_SINK_PORT`. Los `.eml` capturados se abren en Outlook, Thunderbird o Apple Mail como
  un mensaje recibido, que es la parte de «cliente real» que queda en manos de quien revisa.

  **Lo que sigue fuera de esto**: como se ve en Outlook clasico, Gmail y Apple Mail de verdad. El `<hr>`
  y el `border-radius` degradan distinto en Outlook clasico; no es un fallo, pero es lo primero que
  mirara el ojo.
- [x] **CA-6**: Ninguna imagen ni recurso remoto de terceros en los correos.
  `ProductEmailInventoryTests.CadaCorreo_Deberia_ViajarSinRecursosRemotos`: el unico atributo que
  puede llevar una URL es `href`; se descartan ademas `<img>`, `<link>`, `<script>`, `@import`,
  `@font-face`, `background-image` y `url(`.

## Hallazgo del inventario

El spec hablaba de «al menos» cuatro correos y contaba entre ellos unas **notificaciones de la baja de
cuenta que no existen**: `CloseAccountHandler` no envia ningun correo. Lo que sale por ese camino es
el aviso de baja de Workspace, porque cerrar la cuenta obliga antes a resolver los Workspaces de
propiedad unica (RN-038).

En cambio aparecieron **dos que nadie habia contado**: los avisos de alerta de operacion de `MVP-603`
(disparada y resuelta), que eran el unico correo del producto sin maquetacion ninguna. Total: cinco.

## Maquetas y referencias visuales

No aplica: el prototipo no cubre correo.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| — | RN-017, RN-035 | hecho | Los cinco correos por `ProductEmailTemplate`; `ProductEmailInventoryTests` (pie legal, motivo, texto plano y cero recursos remotos) y previews en `artifacts/correos/` |

## Notas y decisiones

- El correo es la unica via del producto hacia alguien que **todavia no tiene cuenta**: la invitacion.
  Que se vea legitimo no es estetica, es tasa de aceptacion.
- **La identidad del responsable no se copia**: sale del mismo `legal-entity.json` del que se
  alimentan la Politica de Privacidad y los Terminos publicados, incrustado en la API al compilar.
  Duplicar un NIF es divergir, y el sitio donde se nota es la bandeja de alguien.
- **Los avisos de alerta de operacion entran en la plantilla** aunque vayan a una direccion interna:
  un correo del producto es un correo del producto, y ahi el motivo del envio y el modo de apagarlos
  son informacion util para quien herede esa bandeja.
- El **«como dejar de recibirlo» no es igual en los cinco**, y se dice lo que se puede ofrecer de
  verdad: en los avisos del ciclo de vida del Workspace, que no hay baja posible, en vez de simular
  una que no existe.
- Detalle tecnico en el [tech-design.md](./tech-design.md).
