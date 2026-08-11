---
id: "MVP-806"
tipo: feature
titulo: "Depuracion de maestros: borrado y fusion"
estado: completado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: []
bloquea: ["MVP-807"]
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["producto", "backend", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["maestros", "terrenos", "trabajadores", "tareas", "temporadas"]
  etiquetas: ["mvp", "ajustes", "maestros", "higiene-de-datos"]
  nivel_riesgo: medio
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-806 — Depuracion de maestros: borrado y fusion

> **Origen**: `P-036` y `P-041` del registro de `MVP-999`, diferidos en `MVP-499` a la espera de que su
> premisa se cumpliera y retriados a esta epica en la segunda revision del MVP (2026-08-10).

## Contexto

**`P-036`** — Un registro de maestro creado por error no se puede borrar, solo inactivar. La politica
de inactivacion con historico es correcta para lo que ya se uso, pero no contempla el caso trivial de
una fila recien creada y nunca referenciada: se queda para siempre en la lista de inactivos. `RN-037`
cubre el borrado con confirmacion de los **registros operativos**, no de los maestros.

**`P-041`** — La migracion que creo los indices unicos de nombre (`MVP-207`) resolvio los duplicados
preexistentes renombrandolos con sufijo « (2)», « (3)»…, sin perder datos. El usuario se queda con
filas que probablemente queria unificar y solo puede renombrarlas o inactivarlas. `MVP-208` amplio el
escenario al materializar a los miembros como responsables: una fila de cuadrilla que ocupaba el nombre
de un miembro quedo renombrada « (2)» y **probablemente es la misma persona**. Ese es el caso de fusion
mas probable y el mas incomodo de convivir con el.

Los dos se difirieron en `MVP-499` por una razon concreta: comprobar el «sin uso historico» exigia que
existieran las entidades operativas. **Esa premisa ya se cumple** —hay actividades, cosechas, compras y
consumos—, y por eso el punto vuelve.

## Objetivo

Que un maestro se pueda depurar: retirar de verdad lo que nunca se uso y unificar lo que quedo
partido, sin poner en riesgo ningun registro operativo existente.

## Requisitos de usuario

### HU-1 — Borrar lo que cree por error

**Como** titular de la explotacion,
**quiero** eliminar un terreno, un trabajador, una tarea o una temporada que cree por equivocacion y no
he usado nunca,
**para** no arrastrarlo en la lista de inactivos para siempre.

### HU-2 — Unificar lo que es lo mismo

**Como** titular de la explotacion,
**quiero** fusionar dos fichas que son la misma persona o el mismo terreno,
**para** que el historico deje de estar partido en dos.

## Alcance (in-scope)

- **Borrado fisico** de un registro de maestro **sin uso historico**, con confirmacion explicita que
  nombre lo que se elimina, en los cuatro maestros de la epica `MVP-002`: terrenos, temporadas,
  trabajadores y tareas.
- Comprobacion del «sin uso» **en servidor** contra todas las entidades que pueden referenciarlo, no
  solo contra la que se este mirando. Si hay uso, la accion no se ofrece y el intento responde con un
  error que explica por que.
- **Fusion** de dos registros del mismo maestro: se elige cual sobrevive, las referencias del otro se
  reapuntan y el absorbido desaparece. Confirmacion explicita que diga cuantos registros se van a
  reapuntar.
- Caso particular de la fusion entre una fila de cuadrilla y un miembro del Workspace: sobrevive la del
  miembro, porque su nombre lo fija la cuenta (`RN-036`) y no es renombrable.
- Extension de `RN-037` para recoger el criterio en los maestros.

## Fuera de alcance (out-of-scope)

- **Borrar un maestro con historico**: sigue rigiendo la inactivacion. Ni siquiera con confirmacion:
  eso dejaria registros operativos sin la ficha que los explica.
- Deshacer una fusion. Es una operacion con confirmacion explicita y sin vuelta atras, igual que el
  borrado de `RN-037`.
- Fusion entre maestros distintos, o fusion masiva por coincidencia de nombre.
- Deteccion automatica de candidatos a fusionar: la propone el usuario, no el sistema.
- El sentido inverso de `workers.user_account_id` (`P-022`, vincular una fila de cuadrilla a una
  cuenta): sigue en backlog, y esta historia le quita casi todo su motivo.

## Criterios de aceptación

- [x] **CA-1**: Un registro de maestro nunca referenciado se puede eliminar desde la interfaz, con
  confirmacion explicita, y desaparece de las listas de activos e inactivos.
  **Evidencia**: `DELETE /api/v1/{plots|seasons|workers|tasks}/{id}` responde `204` y la ficha deja de
  salir en el listado sin filtro, que trae activos e inactivos.
  `MasterDepurationIntegrationTests.Borrar_Deberia_QuitarLaFichaDeLosListados_EnLosCuatroMaestros`
  inactiva la ficha antes de borrarla —es justo la que hoy se queda para siempre en «inactivos»— y lo
  comprueba en terrenos, tareas y responsables; `BorrarUnaTemporada_Deberia_QuitarlaDelListado` cubre
  el cuarto. La confirmacion explicita es el `ConfirmDialog` de `RN-037`, con el foco inicial en
  «Cancelar»: `TareasView.test.tsx` verifica que **no** hay ninguna llamada `DELETE` hasta que se
  confirma.

- [x] **CA-2**: Un registro con **cualquier** uso historico no ofrece la accion de borrar, y el intento
  directo contra la API responde con un error que dice cuantos registros lo referencian. Verificado con
  un caso de cada tipo de referencia, no solo con uno.
  **Evidencia**: nueve tipos de referencia declarados en `MasterReferenceMap` y **nueve pruebas, una
  por tipo**, en dos niveles. Contra PostgreSQL real (`MasterRepositoryPostgresTests`): terreno desde
  actividades, cosechas y consumos; temporada desde actividades, cosechas, compras y consumos;
  trabajador desde actividades; tarea desde las actividades que la eligieron del catalogo. Contra la
  API (`MasterDepurationIntegrationTests`), los mismos nueve casos devolviendo
  `422 BUSINESS_RULE_MASTER_IN_USE`; por ejemplo, un terreno con dos actividades responde «No se puede
  eliminar el terreno «La Hoya»: 2 actividades lo referencian». El recuento cuenta tambien los
  registros eliminados logicamente
  (`ElUso_Deberia_ContarTambienLosRegistrosEliminadosLogicamente`), porque su clave ajena sigue
  apuntando a la ficha. En la interfaz, el boton solo aparece con `usage_count === 0`: `TareasView`
  no lo ofrece con `3` ni con `null` («no consultado»).
  **Comprobado en rojo**: retirando `PurchaseConsumption.PlotId` del mapa fallan
  `MasterReferenceCoverageTests(kind: Plot)`, `ElUsoDeUnTerreno_Deberia_ContarLosConsumos` y
  `BorrarUnTerrenoConConsumos_Deberia_Responder422`.

- [x] **CA-3**: Fusionar dos fichas reapunta todos los registros del absorbido al superviviente, sin
  perder ninguno: comprobado contando los registros de los dos antes y la suma en el superviviente
  despues.
  **Evidencia**: `FusionarTerrenos_Deberia_ReapuntarLosTresTiposDeReferencia_Y_BorrarElAbsorbido`
  cuenta **5** registros entre los dos terrenos antes (1 del superviviente + 2 actividades, 1 cosecha
  y 1 consumo del absorbido), fusiona, y encuentra **5** en el superviviente despues, con
  `reassigned_count = 4`. `FusionarTemporadas_...` hace lo mismo con los cuatro tipos de referencia de
  temporada. La respuesta `200` trae la cifra, y la confirmacion de la interfaz la anuncia antes:
  «Se reapuntaran 4 registros a la ficha que se conserva».

- [x] **CA-4**: Al fusionar una fila de cuadrilla con un miembro del Workspace, sobrevive la del
  miembro y el indice unico parcial de `MVP-208` sigue cumpliendose.
  **Evidencia**:
  `FusionarCuadrillaEnMiembro_Deberia_ConservarLaFichaDelMiembro_Y_SuIndiceUnico` reproduce el
  escenario real —el miembro «Andrés Gilabert» materializado por `MVP-208` y la cuadrilla homonima
  «Andrés Gilabert (2)»—, fusiona, y comprueba en base de datos que sigue habiendo **exactamente una**
  fila con `user_account_id` en el Workspace (`ux_workers_workspace_user_account`). El sentido
  contrario responde `422 BUSINESS_RULE_MASTER_MERGE_MEMBER_SURVIVES`
  (`FusionarMiembroEnCuadrilla_Deberia_Responder422`), y borrar la ficha de un miembro,
  `422 BUSINESS_RULE_WORKER_MEMBERSHIP_MANAGED`. En la interfaz el usuario no llega a pedirlo: el
  dialogo fija el sentido y lo explica (`MergeMasterDialog.test.tsx`, `TrabajadoresView.test.tsx`).

- [x] **CA-5**: Ni el borrado ni la fusion dejan huerfano ningun registro operativo: las claves ajenas
  siguen resolviendo despues de la operacion.
  **Evidencia**: tres capas, todas comprobadas. (1) Despues de fusionar terrenos, no queda **ninguna**
  fila en `activities`, `harvests` ni `purchase_consumptions` apuntando al absorbido. (2) El diario
  sigue resolviendo el nombre: `Fusionar_Deberia_ReapuntarLosRegistros_Y_DejarLasClavesAjenasResolviendo`
  lee `GET /activities` y comprueba que **todas** las filas devuelven `plot_name: "Bancal de arriba"`.
  (3) La fusion vuelve a contar el uso del absorbido dentro de la transaccion antes de borrarlo, y por
  debajo estan las FK `RESTRICT`: `Borrar_Deberia_TraducirLaClaveAjenaA422_Cuando_AlguienRegistraEntreMedias`
  fuerza esa carrera y obtiene el `422`, no un `500`. Una fusion que pise una edicion ajena se deshace
  entera (`Fusionar_Deberia_FallarEntera_Cuando_OtraPersonaEditaUnRegistroQueSeReapunta`: el absorbido
  sigue existiendo despues del `409`).

- [x] **CA-6**: `RN-037` describe el criterio que aplica a los maestros.
  **Evidencia**: `docs/01-producto/reglas-de-negocio.md`, `RN-037`, nuevo apartado «Los maestros
  (extension de `MVP-806`)»: tabla que contrasta el criterio con el de los registros operativos
  —logico y siempre frente a fisico y solo sin uso—, las tres condiciones del borrado, la fusion como
  salida para lo que si tiene historico y la supervivencia de la ficha del miembro. El contrato de los
  endpoints esta en `docs/02-arquitectura/contratos-api.md` §0.f.

## Notas y decisiones

- **La comprobacion del «sin uso» es la parte delicada, no el borrado.** Un maestro puede estar
  referenciado desde cuatro tablas operativas y, en el caso de los terrenos, tambien desde los
  consumos. Comprobarlo contra una sola es exactamente el fallo que dejaria un registro huerfano; por
  eso `CA-2` exige un caso por tipo de referencia.
- **La fusion no es un borrado con pasos previos.** Reapuntar claves ajenas dentro de una transaccion,
  con el control de concurrencia que ya usan las entidades operativas, es lo que separa esta historia
  de un `DELETE`.
- Va **antes de `MVP-807`**: deja el maestro de personas limpio antes de tocar el ciclo de vida de la
  membresia.
