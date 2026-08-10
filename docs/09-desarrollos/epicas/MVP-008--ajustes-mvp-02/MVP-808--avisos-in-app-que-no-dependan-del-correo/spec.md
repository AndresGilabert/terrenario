---
id: "MVP-808"
tipo: feature
titulo: "Avisos in-app que no dependan del correo"
estado: aprobado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "producto"]
  modulo_path: "03-modulos/"
  componentes: ["notificaciones", "workspaces", "invitaciones"]
  etiquetas: ["mvp", "ajustes", "notificaciones", "RU-31"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-808 — Avisos in-app que no dependan del correo

> **Origen**: `P-011` y `P-029` del registro de `MVP-999`, retriados a esta epica en la segunda
> revision del MVP (2026-08-10) con alcance minimo.

## Contexto

**`P-011`** — La bandeja de invitaciones (la campanita de `MVP-107`) se refresca **solo al montar la
sesion**: una invitacion recibida mientras el usuario ya esta dentro no aparece hasta recargar o volver
a entrar. El tiempo real y los canales externos quedaron fuera del alcance de `MVP-107`.

**`P-029`** — La solicitud de reactivacion de un Workspace (`RN-040`) **solo se avisa por correo**.
Quien dio de baja el Workspace se entera por email de que tiene una decision pendiente, con enlace a
`/reactivations`; si ese correo no llega o se pierde, la solicitud se queda esperando **sin ninguna
senal dentro de la aplicacion**. Y al solicitante tampoco se le notifica el resultado: lo descubre
porque el Workspace reaparece —o no— en su selector.

Lo que hace grave a `P-029` no es la comodidad, es que **una decision irreversible depende de que un
correo llegue**. El producto ya tiene la superficie donde ponerlo: la campanita solo cubre un tipo de
aviso porque nunca hizo falta mas.

El Product Owner acota esta historia al **minimo que quita esa dependencia**. La generalizacion del
centro de notificaciones (`RU-31`, con canales configurables y tipos de tarea) sigue siendo fase
posterior.

## Objetivo

Que ninguna decision pendiente dentro del producto dependa exclusivamente de un correo, y que la
bandeja se entere de lo que llega mientras la sesion esta abierta.

## Requisitos de usuario

### HU-1 — Ver la invitacion que acaba de llegar

**Como** usuario con la sesion abierta,
**quiero** que una invitacion recibida ahora mismo aparezca sin tener que recargar,
**para** no perderla porque estaba trabajando.

### HU-2 — No depender de un correo para una decision irreversible

**Como** persona que dio de baja un Workspace,
**quiero** ver dentro de la aplicacion que alguien ha pedido reactivarlo,
**para** poder decidir aunque el correo no me haya llegado.

## Alcance (in-scope)

- **Refresco de la bandeja al recuperar el foco de la ventana**, con la salvaguarda de no lanzar una
  peticion por cada cambio de pestana: un intervalo minimo entre refrescos.
- Nuevo tipo de aviso en la bandeja: **solicitud de reactivacion pendiente de decidir**, con enlace a
  la pantalla que ya existe.
- El aviso desaparece de la bandeja cuando la solicitud se resuelve, por cualquiera de las dos vias.
- Reutilizacion del `NotificationsContext` de `MVP-107` y de su tracking de «vistas», sin duplicar la
  mecanica.

## Fuera de alcance (out-of-scope)

- **Tiempo real** (websockets, server-sent events) y **polling continuo**: el refresco es al recuperar
  el foco, no en segundo plano. Es el mismo criterio con el que `RN-006` descarto el refresco
  automatico del dashboard.
- **Notificar al solicitante el resultado** de su solicitud: sigue descubriendolo por el selector.
  **Decision del PO (2026-08-10)**: se queda fuera, tambien la denegacion. El hueco grave era el otro
  —que una decision irreversible dependiera de que llegara un correo—, y meterlo abriria avisos con
  ciclo de vida propio (leido, caducado, archivado) que la campanita hoy no tiene.
- Canales externos (push, WhatsApp) y configuracion de canales por usuario: eso es `RU-31`, fase
  posterior.
- Avisar de que una invitacion ha sido anulada: `P-039`, **descartado** por el PO en la revision
  anterior.
- Avisar al resto de miembros de que alguien ha abandonado el Workspace (`MVP-807`).

## Criterios de aceptación

- [ ] **CA-1**: Una invitacion emitida mientras la sesion esta abierta aparece en la bandeja al volver
  a la ventana, sin recargar.
- [ ] **CA-2**: Cambiar de pestana repetidamente **no** genera una peticion por cada cambio: se
  comprueba contando las peticiones en un intervalo corto.
- [ ] **CA-3**: Con una solicitud de reactivacion pendiente, la bandeja la muestra con enlace a la
  pantalla de decision, sin necesidad de haber recibido el correo.
- [ ] **CA-4**: Resuelta la solicitud —autorizada o denegada—, el aviso desaparece de la bandeja.
- [ ] **CA-5**: El correo de `RN-040` **se sigue enviando**: el aviso in-app se suma, no sustituye.

## Notas y decisiones

- **El alcance minimo es la decision, no una limitacion aceptada a regañadientes.** Generalizar el
  centro de notificaciones sin mas tipos que estos dos seria construir una abstraccion sin consumidor,
  que es justo lo que `P-007` enseño a no hacer.
- **El refresco al recuperar el foco se descarto para el dashboard y se acepta aqui**, y no es
  contradictorio: `RN-006` habla de recalcular cifras, donde el usuario decide cuando mirar; aqui se
  trata de enterarse de algo que alguien te ha mandado, que por definicion no depende de cuando mires.
