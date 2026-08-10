---
id: "MVP-807"
tipo: feature
titulo: "Ciclo de vida de la membresia"
estado: aprobado
prioridad: alta
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: ["MVP-806"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["producto", "backend", "frontend", "autorizacion"]
  modulo_path: "03-modulos/"
  componentes: ["workspaces", "miembros", "identidad"]
  etiquetas: ["mvp", "ajustes", "membresia", "RN-034"]
  nivel_riesgo: medio
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-807 — Ciclo de vida de la membresia

> **Origen**: `P-048` y `P-049` del registro de `MVP-999`, hallazgos `R-26` y `R-27` de la 3a pasada de
> `MVP-299`, retriados a esta epica en la segunda revision del MVP (2026-08-10).

## Contexto

**`P-048`** — Un miembro no propietario **no puede abandonar un Workspace**. No hay via, ni de API ni
de UI: `MVP-204` cubre retirar el acceso **a otra** persona y la pantalla oculta la accion sobre uno
mismo; `MVP-206` cubre la salida **del propietario**, con traspaso o baja logica. Un miembro corriente
que ya no colabora arrastra ese Workspace en su selector indefinidamente y, desde `MVP-208`, sigue
ademas como responsable seleccionable dentro de el. Es el hueco simetrico del ciclo de vida que
absorbio `MVP-002`, y no lo cubre ninguna historia de `MVP-001` a `MVP-007`.

Con `RN-035` (invitaciones por email y por **enlace compartible**), entrar en un Workspace ajeno es
facil; salir no existe.

**`P-049`** — La UI nunca ofrece revocar a un copropietario aunque la API lo permite.
`WorkspaceMembersController` calcula `can_revoke` como `status == activo && role != workspace_owner`,
mientras la guarda real de `RevokeMemberHandler` solo protege al propietario **unico**
(`CountActiveOwnersAsync <= 1`), que es lo que dice literalmente el `CA-8` de `MVP-204`. Mientras el
propietario era siempre uno la diferencia no se notaba; `MVP-206` introdujo Workspaces con varios
propietarios y ahora la regla publicada y la accion disponible no coinciden. **No es un fallo de
seguridad** —la UI es mas restrictiva que la API— sino una incoherencia a decidir.

Los dos comparten superficie («Miembros y accesos») y la misma decision de producto.

## Objetivo

Cerrar el ciclo de vida de la membresia por el lado que falta —la salida voluntaria— y que lo que la
interfaz ofrece coincida con lo que la regla permite.

## Requisitos de usuario

### HU-1 — Salir de un Workspace que ya no es mio

**Como** miembro invitado a un Workspace,
**quiero** poder salir de el por voluntad propia,
**para** dejar de verlo en mi selector cuando ya no colaboro en esa explotacion.

### HU-2 — Que la accion de retirar acceso sea la que dice la regla

**Como** miembro de un Workspace con varios propietarios,
**quiero** que la interfaz me ofrezca exactamente las revocaciones que la regla permite,
**para** no descubrir por casualidad que podia hacer algo que la pantalla escondia.

## Alcance (in-scope)

- Accion de **abandonar el Workspace** para un miembro activo, en «Miembros y accesos» o en Ajustes,
  con confirmacion explicita que diga que sus registros no se borran.
- **Reutilizacion** de la guarda de no-orfandad que ya existe (`RN-038`, `WorkspaceOwnershipGuard`): un
  propietario unico no puede abandonar sin resolver la propiedad, exactamente igual que en la baja de
  cuenta de `MVP-505`. No se reimplementa la regla.
- Guarda de no dejar el Workspace sin ningun miembro activo, la del `CA-8` de `MVP-204`, reutilizada
  igual.
- Efecto sobre la fila de responsable materializada en `MVP-208`: al salir, deja de ofrecerse como
  responsable seleccionable, sin borrar el historico que ya tenga.
- Reingreso **solo por invitacion nueva**, igual que el revocado.
- **Alineacion de `can_revoke` con la guarda real** (decision del PO, 2026-08-10): la interfaz pasa a
  ofrecer la revocacion de un copropietario **mientras quede otro propietario activo**, que es lo que
  la API ya permite y lo que el `CA-8` de `MVP-204` dice literalmente. La guarda que de verdad importa
  —no dejar el Workspace sin propietario— no se toca.
- Actualizacion de `RN-034` para recoger la salida voluntaria y la alineacion anterior.

## Fuera de alcance (out-of-scope)

- **Roles y permisos granulares** (`RU-13`): los permisos siguen planos.
- Avisar al resto de miembros de que alguien se ha ido: es alcance de `MVP-808` si se decide, no de
  esta.
- Cambiar el flujo de baja de Workspace o el de baja de cuenta, que ya existen.
- Readmision automatica o solicitud de reingreso.

## Criterios de aceptación

- [ ] **CA-1**: Un miembro activo no propietario puede abandonar un Workspace desde la interfaz, con
  confirmacion explicita, y ese Workspace desaparece de su selector.
- [ ] **CA-2**: Un propietario **unico** que intenta abandonar recibe la misma obligacion de resolver
  la propiedad que ya impone la baja de cuenta, resuelta por `WorkspaceOwnershipGuard` y **no** por
  codigo nuevo. Comprobado que la llamada pasa por esa guarda.
- [ ] **CA-3**: El ultimo miembro activo de un Workspace no puede abandonarlo dejandolo sin nadie.
- [ ] **CA-4**: Quien abandona deja de aparecer como responsable seleccionable, y las labores que ya
  tenia asignadas siguen mostrando su nombre en el historico.
- [ ] **CA-5**: Volver a entrar exige una invitacion nueva; el enlace anterior no sirve.
- [ ] **CA-6**: En un Workspace con **dos** propietarios activos, la interfaz ofrece retirar el acceso
  a uno de ellos y la operacion se completa; con **uno solo**, ni se ofrece ni la API la permite.
  `can_revoke` y la guarda de `RevokeMemberHandler` describen la misma regla, y `RN-034` la recoge.

## Notas y decisiones

- **La guarda no se reimplementa, se llama.** Es la condicion con la que se registro `P-024` y la que
  `MVP-505` respeto al construir la baja de cuenta. `CA-2` la comprueba en vez de confiarla.
- **`P-049` era una decision de producto y esta tomada.** Las dos salidas eran defendibles: que un
  copropietario pueda retirar el acceso a otro es coherente con `RN-034` (permisos planos), y que no
  pueda protege contra una expulsion entre iguales. **Decision del PO (2026-08-10): manda `RN-034`**,
  asi que se alinea la interfaz con la API. Se descarta endurecer la guarda porque obligaria a que un
  copropietario solo pudiera salir por su propio pie o traspasando, y porque se apartaria de lo que el
  `CA-8` de `MVP-204` dice.
- Va **despues de `MVP-806`**: fusionar primero las fichas duplicadas evita decidir sobre una membresia
  cuya persona esta representada dos veces en el maestro.
