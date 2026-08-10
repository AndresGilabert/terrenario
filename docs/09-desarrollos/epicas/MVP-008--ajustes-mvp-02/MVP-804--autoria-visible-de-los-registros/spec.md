---
id: "MVP-804"
tipo: feature
titulo: "Autoria visible de los registros operativos"
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
  dominios: ["producto", "backend", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "produccion", "compras-consumo"]
  etiquetas: ["mvp", "ajustes", "trazabilidad", "RU-21"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
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

- [ ] **CA-1**: La respuesta de un registro operativo incluye quien lo creo y quien lo edito por ultima
  vez, con nombre y fecha, en los cuatro tipos.
- [ ] **CA-2**: El detalle de un registro en la interfaz muestra esa informacion, y solo muestra la
  linea de ultima edicion cuando de verdad hubo una posterior a la creacion.
- [ ] **CA-3**: Un registro creado por una cuenta despues dada de baja se muestra como «Cuenta
  eliminada» y **no** filtra el nombre ni el correo que tenia. Verificado contra un registro real de
  una cuenta anonimizada, no solo con el codigo de respuesta.
- [ ] **CA-4**: La informacion de autoria no aparece en ningun listado masivo ni en el diario en forma
  de columna: no cambia la densidad de las listas.

## Notas y decisiones

- **No se anade ningun dato personal nuevo.** El nombre ya esta en el producto y la fila ya guarda la
  referencia; lo unico que cambia es que se lee. Aun asi, `CA-3` existe para comprobar que la baja de
  cuenta sigue siendo efectiva por este camino nuevo, que es justo el tipo de fuga que abre una
  funcionalidad de lectura.
- **El alcance minimo es deliberado.** `RU-21` renuncia al historico completo «por simplicidad», y esta
  historia no lo reabre: exponerlo todo convertiria una ayuda en un registro de vigilancia entre
  companeros, que no es lo que el requisito pide.
