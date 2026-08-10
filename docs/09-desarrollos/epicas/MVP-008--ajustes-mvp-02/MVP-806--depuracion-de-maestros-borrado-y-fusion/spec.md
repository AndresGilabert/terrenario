---
id: "MVP-806"
tipo: feature
titulo: "Depuracion de maestros: borrado y fusion"
estado: aprobado
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

- [ ] **CA-1**: Un registro de maestro nunca referenciado se puede eliminar desde la interfaz, con
  confirmacion explicita, y desaparece de las listas de activos e inactivos.
- [ ] **CA-2**: Un registro con **cualquier** uso historico no ofrece la accion de borrar, y el intento
  directo contra la API responde con un error que dice cuantos registros lo referencian. Verificado con
  un caso de cada tipo de referencia, no solo con uno.
- [ ] **CA-3**: Fusionar dos fichas reapunta todos los registros del absorbido al superviviente, sin
  perder ninguno: comprobado contando los registros de los dos antes y la suma en el superviviente
  despues.
- [ ] **CA-4**: Al fusionar una fila de cuadrilla con un miembro del Workspace, sobrevive la del
  miembro y el indice unico parcial de `MVP-208` sigue cumpliendose.
- [ ] **CA-5**: Ni el borrado ni la fusion dejan huerfano ningun registro operativo: las claves ajenas
  siguen resolviendo despues de la operacion.
- [ ] **CA-6**: `RN-037` describe el criterio que aplica a los maestros.

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
