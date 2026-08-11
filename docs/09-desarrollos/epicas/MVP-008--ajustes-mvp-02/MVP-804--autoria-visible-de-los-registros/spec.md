---
id: "MVP-804"
tipo: feature
titulo: "Autoria visible de los registros operativos"
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
  dominios: ["producto", "backend", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "produccion", "compras-consumo"]
  etiquetas: ["mvp", "ajustes", "trazabilidad", "RU-21"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-11"
---

# MVP-804 — Autoria visible de los registros operativos

> **Origen**: `P-113` del registro de `MVP-999`, detectado en la segunda revision completa del MVP
> (2026-08-10).

## Contexto

`RU-21` («Se guardan usuario y fecha de la ultima edicion/creacion de cada registro critico», Estado:
MVP) esta cumplido a medias, y la mitad que falta es la que sirve para algo.

Las cuatro tablas operativas —`activities`, `harvests`, `purchases` y `purchase_consumptions`— guardan
`created_by`, `created_at`, `updated_by` y `updated_at` desde que se crearon. Pero el dato **no sale de
la base de datos**: cero apariciones de esas columnas en la capa de aplicacion, en los controladores y
en todo el cliente. Ni la API los devuelve ni la UI los muestra.

Pesa por `RN-034`: en el MVP los permisos son planos, de modo que cualquier miembro del Workspace puede
editar o borrar el registro de cualquier otro. Hoy, ante una cifra que no cuadra, no hay forma de saber
quien la apunto ni quien la cambio, y la unica via es preguntar uno por uno.

La parte cara —capturar el dato en cada alta y en cada edicion— ya esta hecha. Lo que falta es leerlo y
pintarlo: no hay migracion.

## Objetivo

Que se pueda saber quien registro y quien modifico por ultima vez cada registro operativo, sin
convertir el producto en un sistema de auditoria.

## Requisitos de usuario

### HU-1 — Saber de quien es un apunte

**Como** miembro de un Workspace compartido,
**quiero** ver quien registro una labor, una cosecha o una compra y quien la cambio por ultima vez,
**para** poder aclarar una discrepancia sin preguntar a todo el mundo.

## Alcance (in-scope)

- Exponer en la respuesta de los registros operativos **quien** creo y **quien** hizo la ultima
  edicion, con su nombre resuelto, y **cuando**.
- Mostrarlo en el detalle de cada registro operativo —diario, cosechas, compras y consumos—, en un
  lugar discreto: es informacion de apoyo, no un dato de captura.
- Respetar la baja de cuenta: una cuenta anonimizada aparece como «Cuenta eliminada», que es
  exactamente lo que `MVP-505` dejo previsto al conservar la fila anonimizada porque «el historico
  operativo de terceros guarda quien lo registro».
- Omitir la linea de ultima edicion cuando coincide con la creacion y nadie ha tocado el registro
  despues: repetir el mismo nombre dos veces no informa.

## Fuera de alcance (out-of-scope)

- **Historico completo de cambios**: `RU-21` lo excluye expresamente («No se mantiene historico
  completo de cambios por simplicidad»). Solo la ultima edicion.
- Autoria en los **maestros**: sus tablas no tienen esas columnas y `RU-21` habla de «registros
  criticos».
- Filtrar o buscar por autor, y cualquier informe de actividad por persona.
- Notificar a nadie de que su registro ha sido modificado.

## Criterios de aceptación

- [x] **CA-1**: La respuesta de un registro operativo incluye quien lo creo y quien lo edito por ultima
  vez, con nombre y fecha, en los cuatro tipos.
  **Evidencia** (API y PostgreSQL reales, `RecordAuthorshipTests`): actividad, cosecha, compra y consumo
  devuelven `created_by_name: "Andrés Gilabert"` y `updated_by_name: "Andrés Gilabert"` junto a los
  `created_at`/`updated_at` que ya existian. La compra se relee **del listado**, que es de donde la saca
  su modal: no tiene lectura por id y no ha hecho falta inventarla. Con Lucia corrigiendo la cosecha de
  Andres, la respuesta separa los dos: `created_by_name: "Andrés Gilabert"`,
  `updated_by_name: "Lucía Pérez"`, y `updated_at > created_at`.
- [x] **CA-2**: El detalle de un registro en la interfaz muestra esa informacion, y solo muestra la
  linea de ultima edicion cuando de verdad hubo una posterior a la creacion.
  **Evidencia**: nueve tests de cliente (`RecordAuthorship.test.tsx`, `HarvestFormModal.test.tsx`). El
  modal de correccion rotula «Registrado por **Andrés Gilabert** el 20 oct 2025»; con
  `updated_at: 2025-11-03` aparece ademas «Ultima edicion de **Lucía Pérez** el 3 nov 2025»; con los dos
  instantes iguales la segunda linea **no existe en el DOM**
  (`queryByText(/Última edición/)` devuelve `null`). La omision mira el **instante**, no el nombre: hay
  test de que corregir tu propio registro si se cuenta. En el alta no aparece nada.
  El dato de partida no es teorico: en la base de datos de desarrollo, 3 de las 5 cosechas mas recientes
  tienen `updated_at > created_at` y 2 no, asi que las dos ramas se dan en datos reales.
- [x] **CA-3**: Un registro creado por una cuenta despues dada de baja se muestra como «Cuenta
  eliminada» y **no** filtra el nombre ni el correo que tenia. Verificado contra un registro real de
  una cuenta anonimizada, no solo con el codigo de respuesta.
  **Evidencia**: `Deberia_MostrarCuentaEliminada_YNoFiltrarNombreNiCorreo_Cuando_LaCuentaSeDioDeBaja`.
  El test hace el recorrido entero contra PostgreSQL real: Lucia entra por el flujo de login, acepta la
  invitacion, **corrige una cosecha**, y ejecuta la baja por el endpoint real
  (`POST /account/closure` con la frase `ELIMINAR MI CUENTA`). Al releer la cosecha,
  `updated_by_name == "Cuenta eliminada"` y `created_by_name == "Andrés Gilabert"` —la baja de una
  cuenta no borra la autoria de otra—. Lo que fija el criterio es que la asercion **no mira esos dos
  campos**: comprueba que el cuerpo entero de la respuesta no contiene `Lucía Pérez`, ni
  `lucia@ejemplo.test`, ni la subcadena `lucia`. El mismo recorrido se repite sobre el **listado**
  (`GET /harvests?season_id=all`), que es la lectura que se pide sin querer.
  La proteccion esta en el SQL, no en el rotulo: la proyeccion deja de devolver el nombre en cuanto
  `users.deleted_at IS NOT NULL`, sin mirar que guarda `display_name`.
- [x] **CA-4**: La informacion de autoria no aparece en ningun listado masivo ni en el diario en forma
  de columna: no cambia la densidad de las listas.
  **Evidencia**: `NoDeberia_LlevarLaAutoria_AlMuroDelDiario` comprueba que la respuesta de
  `GET /api/v1/diary` no contiene ni `created_by_name` ni `updated_by_name` —el muro tiene proyeccion
  propia (`DiaryRow`) y no se ha tocado—. En el cliente, `CosechasView` renderiza la lista con las filas
  ya trayendo el dato y no lo pinta: `queryByText(/Registrado por/)` devuelve `null`. La autoria vive en
  un unico componente (`RecordAuthorship`), que solo usan los cuatro modales.

## Notas y decisiones

- **No se anade ningun dato personal nuevo.** El nombre ya esta en el producto y la fila ya guarda la
  referencia; lo unico que cambia es que se lee. Aun asi, `CA-3` existe para comprobar que la baja de
  cuenta sigue siendo efectiva por este camino nuevo, que es justo el tipo de fuga que abre una
  funcionalidad de lectura.
- **El alcance minimo es deliberado.** `RU-21` renuncia al historico completo «por simplicidad», y esta
  historia no lo reabre: exponerlo todo convertiria una ayuda en un registro de vigilancia entre
  companeros, que no es lo que el requisito pide.
- **Se lee en la proyeccion que listado y detalle ya comparten**, no en un endpoint nuevo. Es lo que
  hace que compras y consumos —que no tienen lectura por id— reciban la autoria sin una peticion mas, y
  lo que evita abrir un segundo camino de lectura sobre el mismo registro (leccion de `MVP-708`).
  `CA-4` habla de la **interfaz**: que el dato viaje en la fila no lo pinta en la tabla.
- **Solo viaja el nombre**, ni el correo ni el identificador de la cuenta. Ninguno hace falta para
  responder a «quien apunto esto», y la opcion protectora es no ampliar la superficie de datos
  personales cuando no aporta.
- **Hallazgo lateral, fuera de alcance**: la purga de `RN-041`
  (`RetentionPurgeService.PurgeAccountsAsync`) comprueba `workspaces`, `workspace_invitations` y
  `workspace_reactivation_requests` antes de borrar una cuenta, pero **no** las cuatro tablas
  operativas, porque no tienen `FK` hacia `users` que la retenga. A los 24 meses, `created_by` puede
  quedar apuntando a una fila que ya no existe, y eso contradice el motivo por el que `MVP-505`
  conserva la fila anonimizada. Esta historia lo **tolera** —`LEFT JOIN` y «Cuenta eliminada», con test
  que lo fija— pero no lo arregla: decidir si la purga debe retener esas cuentas es alcance propio.
