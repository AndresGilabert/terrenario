---
id: "MVP-808"
tipo: feature
titulo: "Avisos in-app que no dependan del correo"
estado: completado
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

- [x] **CA-1**: Una invitacion emitida mientras la sesion esta abierta aparece en la bandeja al volver
  a la ventana, sin recargar.
  **Evidencia**: `NotificationsContext.test.tsx` →
  `Deberia_TraerLaInvitacionNueva_Cuando_SeVuelveALaVentanaPasadoElIntervalo`. La bandeja arranca con
  0 avisos; tras avanzar el reloj falso 30 s y emitir `visibilitychange` + `focus`, la invitacion `a`
  aparece sin remontar el arbol. Se escuchan los dos eventos porque `visibilitychange` no salta al
  volver desde otra ventana y `focus` no salta en movil.
- [x] **CA-2**: Cambiar de pestana repetidamente **no** genera una peticion por cada cambio: se
  comprueba contando las peticiones en un intervalo corto.
  **Evidencia**: `Deberia_NoLanzarUnaPeticionPorCadaCambioDePestana_Cuando_SeVuelveVeinteVecesSeguidas`
  simula **20 idas y vueltas = 40 eventos** en 10 s de reloj falso. Cuenta de peticiones: **1 a
  `/invitations/received` y 1 a `/workspaces/reactivations`** —las de la carga inicial—, ninguna mas.
  Pasados los 30 s del intervalo minimo, la siguiente vuelta si refresca: pasa a 2 y 2, de modo que la
  salvaguarda espacia y no anula. **Falla sin el cambio**: con `MIN_REFRESH_INTERVAL_MS = 0` el test
  cae con `expected "vi.fn()" to be called 1 times, but got 41 times`. Un tercer caso fija que una
  sola vuelta —que emite los dos eventos— cuesta **una** peticion, no dos.
- [x] **CA-3**: Con una solicitud de reactivacion pendiente, la bandeja la muestra con enlace a la
  pantalla de decision, sin necesidad de haber recibido el correo.
  **Evidencia**: `NotificationBell.test.tsx` →
  `Deberia_AnunciarLaSolicitudConEnlaceADecidir_Cuando_HayUnaReactivacionPendiente`: la campanita
  pinta el nombre del Workspace, «Marta pide que le traspases esta explotacion y se reactive» y un
  enlace `Ver y decidir` con `href="/reactivations"`. El contador suma los dos tipos
  (`Deberia_ContarLosDosTiposEnLaChapa_Cuando_ConvivenAvisos`: 1 invitacion + 1 solicitud ⇒
  `aria-label` «Notificaciones: 2 aviso(s) pendiente(s)»). En el contexto,
  `Deberia_PublicarlasEnLaBandeja_Cuando_HayAlgunaEsperandoDecision` y
  `Deberia_SumarLosDosTiposEnElContador_Cuando_ConvivenEnLaBandeja` (2 invitaciones + 1 solicitud ⇒
  `pendingCount` 3). Contra PostgreSQL real,
  `ListPendingAuthorizationsAsync_Deberia_VerLaSolicitudDeUnWorkspaceDadoDeBaja` comprueba que la
  consulta ve la solicitud **con el Workspace dado de baja** —el `join` a `Workspaces` no la filtra— y
  que quien no decide no la ve.
  **Limitacion conocida**: la campanita vive en el shell, que exige Workspace activo. Quien dio de
  baja su **unico** Workspace no la tiene delante y sigue dependiendo del correo y del enlace de
  Ajustes. Registrado como candidato a punto nuevo en `MVP-999`, no resuelto aqui.
- [x] **CA-4**: Resuelta la solicitud —autorizada o denegada—, el aviso desaparece de la bandeja.
  **Evidencia**: `ReactivationInboxPage` refresca la bandeja tras resolver
  (`Promise.all([load(), refreshNotifications()])`), y
  `Deberia_DesaparecerDeLaBandeja_Cuando_LaSolicitudSeResuelve` comprueba que ese refresco deja
  `pendingReactivations` vacio y `pendingCount` en 0. En el origen del dato, la `[Theory]`
  `ListPendingAuthorizationsAsync_Deberia_DejarDeVerla_CuandoSeResuelvePorCualquieraDeLasDosVias`
  corre contra PostgreSQL con los dos valores —`autorizada` y `denegada`— y en los dos la consulta
  deja de devolverla. Ademas, el refresco por foco la retira aunque la decision se tome desde otro
  dispositivo.
- [x] **CA-5**: El correo de `RN-040` **se sigue enviando**: el aviso in-app se suma, no sustituye.
  **Evidencia**: `RequestReactivationHandler` no se ha tocado —el diff de esta historia no incluye
  ningun fichero de `src/backend/Terrenario.Api/`— y su test
  `Solicitar_Deberia_ConsumirElEnlaceYAvisarAQuienDioDeBaja` sigue exigiendo
  `_emailSender.Received(1).SendReactivationRequestedAsync(...)`. Se le ha añadido el comentario que
  lo declara guarda de este `CA-5`, para que la proxima pasada no «simplifique» el envio ahora que la
  campanita cubre el mismo hueco. `RN-040` recoge la regla por escrito.

## Verificación

| Gate | Resultado |
|---|---|
| `dotnet build src/backend/Terrenario.sln -warnaserror` | Correcto, **0 advertencias**, 0 errores |
| `dotnet test src/backend/Terrenario.sln` | **961 pruebas**, 0 fallos (3 nuevas contra PostgreSQL) |
| `npx tsc --noEmit` | Sin salida |
| `npm run lint` (oxlint) | 7 avisos, **todos preexistentes** (`only-export-components` de los contextos y un `exhaustive-deps` de `OAuthCallback`); ninguno en el codigo de esta historia |
| `npm run build` | Correcto en 1,25 s |
| `npx vitest run` | **33 ficheros, 282 pruebas**, 0 fallos (16 nuevas: 11 de contexto + 5 de campanita) |

Comprobacion visual pendiente del PO: el aspecto de la tarjeta de reactivacion dentro de la campanita
y la convivencia de los dos tipos de aviso en la misma bandeja.

## Notas y decisiones

- **El alcance minimo es la decision, no una limitacion aceptada a regañadientes.** Generalizar el
  centro de notificaciones sin mas tipos que estos dos seria construir una abstraccion sin consumidor,
  que es justo lo que `P-007` enseño a no hacer.
- **El refresco al recuperar el foco se descarto para el dashboard y se acepta aqui**, y no es
  contradictorio: `RN-006` habla de recalcular cifras, donde el usuario decide cuando mirar; aqui se
  trata de enterarse de algo que alguien te ha mandado, que por definicion no depende de cuando mires.
