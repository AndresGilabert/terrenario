---
id: "MVP-715"
tipo: feature
titulo: "Correos del producto: inventario y maquetacion unificada"
estado: borrador
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
actualizado_en: "2026-08-07"
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

- [ ] **CA-1**: Existe el inventario de correos salientes en la KB, con disparador y destinatario.
- [ ] **CA-2**: Todos los correos del inventario usan la plantilla comun.
- [ ] **CA-3**: La plantilla incluye la identificacion del responsable y el motivo del envio.
- [ ] **CA-4**: Cada correo tiene version en texto plano.
- [ ] **CA-5**: Verificado enviando cada tipo de correo de verdad y revisando el resultado en un cliente
  real, no solo la plantilla renderizada.
- [ ] **CA-6**: Ninguna imagen ni recurso remoto de terceros en los correos.

## Maquetas y referencias visuales

No aplica: el prototipo no cubre correo.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| — | RN-017, RN-035 | falta | Composicion ad-hoc por tipo de correo |

## Notas y decisiones

- El correo es la unica via del producto hacia alguien que **todavia no tiene cuenta**: la invitacion.
  Que se vea legitimo no es estetica, es tasa de aceptacion.
