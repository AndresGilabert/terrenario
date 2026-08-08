---
id: "MVP-007"
tipo: epica
titulo: "Ajustes MVP 01"
estado: completado
prioridad: alta
hito: "Hito G — Ajustes de uso real"
tickets: []
historias: ['MVP-701', 'MVP-702', 'MVP-703', 'MVP-704', 'MVP-705', 'MVP-706', 'MVP-707', 'MVP-708', 'MVP-709', 'MVP-710', 'MVP-711', 'MVP-712', 'MVP-713', 'MVP-714', 'MVP-715', 'MVP-716', 'MVP-799']
depende_de: ["MVP-001", "MVP-002", "MVP-003", "MVP-004", "MVP-005", "MVP-006"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "producto", "calidad", "cumplimiento"]
  modulo_path: "03-modulos/"
  componentes: ["shell", "diario", "dashboard", "produccion", "identidad", "soporte"]
  etiquetas: ["mvp", "ajustes", "post-primer-uso"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# EPICA MVP-007 — Ajustes MVP 01

## Contexto

El MVP salio a produccion el 2026-08-05 (`v0.6.0-hito-f`, `app.terrenario.com`) con las seis epicas
del roadmap cerradas y su gate de salida superado. La revision completa del MVP —conducida sobre la
aplicacion en marcha, no sobre la KB— confirmo los once puntos que aporto el Product Owner y anadio
cuatro mas, y obligo a reabrir uno que se habia dado por encaminado y nunca se ejecuto.

Lo que aparece no son funcionalidades que falten: son **incoherencias entre lo que el producto
promete y lo que hace**. Dos pantallas responden con cifras distintas a «cuanto llevo esta campana».
Cambiar de Workspace deja a la vista los datos del anterior. La regla que declara el diario vista
principal convive con un arranque que lleva a otra pantalla. El icono de la pestana es el del andamiaje.

Esa clase de defecto no se detecta con tests verdes ni con un gate de release: se detecta usando el
producto. Por eso esta epica existe y por eso llega **despues** de la primera publicacion, no antes.

## Objetivo

Corregir las incoherencias de uso detectadas en la revision del MVP y cerrar los huecos que impiden
que un usuario real trabaje —y se queje— sin friccion, sin ampliar el alcance funcional del producto
mas alla de lo que el Product Owner aprobo punto por punto.

## Requisitos de usuario de alto nivel

- **Como** Antonio (titular de la explotacion), **quiero** que todas las pantallas hablen de la misma
  campana y del mismo Workspace, **para** poder fiarme de las cifras que veo.
- **Como** Antonio, **quiero** entrar directamente a donde trabajo cada dia, **para** no empezar la
  jornada en un panel vacio.
- **Como** Antonio, **quiero** poder decir que algo no funciona, **para** no tener que abandonar el
  producto en silencio cuando algo falla.
- **Como** responsable tecnico, **quiero** que las alertas solo salten cuando pasa algo de verdad,
  **para** que sigan sirviendo cuando pase.

## Alcance

- Coherencia del contexto activo (Workspace y temporada de trabajo) en todas las vistas operativas.
- Aprovechamiento del espacio en escritorio y densidad de informacion en movil.
- Pantalla de arranque y su efecto sobre la medicion de uso.
- Accesibilidad de los modales de formulario y bloqueo de la interaccion con el fondo.
- Lectura economica minima de la campana: precio de venta e importes.
- Identidad de marca, presencia publica del producto y canal de feedback.
- Saneamiento acotado: errores de OAuth mal clasificados, plazos de retencion pendientes y un dato
  personal versionado.
- Inventario y maquetacion unificada de los correos salientes.
- Consolidacion del catalogo de modulos de la KB.

## Fuera de alcance

- **Offline real y sincronizacion diferida**: `MVP-709` cubre avisar y no perder lo escrito, no
  operar sin cobertura. Eso sigue siendo `Hito H — Resiliencia offline`.
- **Modelo de produccion ampliado**: variedad por terreno, producto por Workspace, maestro de
  almazaras y empresas compradoras y rendimiento configurable (`P-059` a `P-063`) quedan en backlog
  como una sola evolucion. `MVP-707` entrega el minimo que **no** los arrastra.
- **Segundo proveedor de identidad**: `RN-036` sigue vigente. `MVP-712` explica la via que ya existe,
  no anade proveedores.
- **Ciclo de vida de la membresia**: abandonar un Workspace y revocar copropietarios (`P-048`,
  `P-049`) quedan en backlog.
- **Borrado y fusion de registros de maestro** (`P-036`, `P-041`) quedan en backlog.
- **Generalizacion del centro de notificaciones** (`P-011`, `P-029`) queda en backlog.

## Criterios de aceptación de la épica

- [x] **CA-1**: Todas las historias de la epica estan en estado `completado`. Las dieciseis, con sus PR
  mergeados en `develop`.
- [x] **CA-2**: Las cifras de una misma campana coinciden entre el diario, el listado de cosechas, el
  libro de compras y la Vision General, verificado contra la API con datos reales. Coinciden las seis
  superficies —diario, cosechas, compras, consumos, `dashboard/summary` y `dashboard/economics`— y
  tambien el calculo a mano sumando fila a fila. **El contraste hubo que provocarlo**: con los datos que
  habia, ninguna cosecha tenia precio y no existia ninguna imputacion, asi que habria pasado sin tocar
  lo que `MVP-707` y `MVP-708` construyeron. Detalle en el [TDD de MVP-799](./MVP-799--revision-epica/tech-design.md).
- [x] **CA-3**: Cambiar de Workspace o de temporada desde el shell deja todas las vistas mostrando el
  contexto elegido, sin recarga manual. Comprobado sobre la aplicacion: diario, cosechas y panel pasan a
  la campana elegida con `performance.getEntriesByType('navigation').length` todavia en **1**.
- [x] **CA-4**: Un usuario puede reportar una incidencia desde dentro del producto y el equipo la recibe
  con contexto tecnico suficiente. Verificado con **envio real por SMTP**, no solo con el HTML: correo
  entregado de 4.501 bytes, `multipart/alternative`, con version desplegada, ruta, `X-Request-Id` y
  navegador, y **sin nada de la explotacion** —se envio a proposito una ruta con identificadores de
  temporada y terreno en la query y no llega ninguno—.
- [x] **CA-5**: Las reglas de negocio que esta epica modifica estan actualizadas. `RN-006`, `RN-008`,
  `RN-009`, `RN-029` y `RN-041`, comprobadas contra el producto y no solo contra su redaccion. Se anade
  `RN-043`, que `MVP-708` creo.
- [x] **CA-6**: Ningun punto aprobado en la clasificacion queda sin historia que lo construya. **Se
  cumple, con una salvedad que es el hallazgo principal de la revision**: ninguno se quedo sin
  construir, pero **quince filas del registro seguian diciendo que si**, `P-055` entre ellas. Se
  verificaron una a una, las quince estaban hechas, y se han cerrado con su evidencia. Que el punto que
  motivo este criterio volviera a estar mal anotado demuestra que no se cierra con diligencia sino con
  una comprobacion: es `P-096`.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

| Historia | Puntos que cierra |
|---|---|
| `MVP-701` — Coherencia de contexto: Workspace y temporada | `P-081`, `P-082`, `P-083` |
| `MVP-702` — Aprovechamiento del espacio: escritorio y movil | `P-086`, `P-090` |
| `MVP-703` — Arranque en el diario y definicion de sesion activa | `P-087`, `P-078` |
| `MVP-704` — Modales accesibles | `P-055` (reabierto) |
| `MVP-705` — Navegacion del diario en la URL | `P-072` |
| `MVP-706` — Comportamiento de la Vision General | `P-075`, `P-085` |
| `MVP-707` — Valor economico de la campana | `P-084`, `P-092` |
| `MVP-708` — Roces de captura en compras y consumos | `P-057`, `P-058` |
| `MVP-709` — Respuesta a la perdida de conexion | `P-091` |
| `MVP-710` — Identidad de marca y presencia del producto | `P-080` |
| `MVP-711` — Canal de feedback del usuario | `P-088` |
| `MVP-712` — Acceso con cualquier direccion de correo | `P-089` |
| `MVP-713` — Errores de OAuth y ruido en las alertas | `P-079` |
| `MVP-714` — Higiene de datos: retencion y secretos | `P-071`, `P-076` |
| `MVP-715` — Correos del producto: inventario y maquetacion | `P-001`, `P-030` |
| `MVP-716` — Consolidacion del catalogo de modulos | `P-020` |
| `MVP-799` — Revision epica | — |

## Secuenciacion recomendada

1. **`MVP-701`** primero: es el unico que corrige datos visibles equivocados, y `MVP-705` y `MVP-707`
   construyen sobre el contexto que deja fijado.
2. **`MVP-706` antes que `MVP-707`**: las dos tocan `VisionGeneralView`. Hacerlas al reves obliga a
   resolver el mismo conflicto dos veces.
3. **`MVP-702` y el resto de UI** despues de `MVP-701`, para no reordenar componentes cuya carga aun
   va a cambiar.
4. **`MVP-713`, `MVP-714` y `MVP-716`** son independientes de todo lo anterior y pueden ir en
   paralelo.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia, igual que en el resto de epicas:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo (`prototype/terrenario-mvp`) aporta referencia visual y de flujo.
- Si hay contradiccion, prevalece la KB.

Matiz propio de esta epica: **varios de sus puntos nacen de divergencias con el prototipo que se
aceptaron en su momento** (el ancho del contenido, la densidad en movil, los campos no portados). El
prototipo se relee aqui como evidencia de intencion original, no como especificacion.

## Reglas de negocio que esta epica modifica

| Regla | Cambio | Historia |
|---|---|---|
| `RN-006` — Estrategia de refresco del dashboard | El refresco deja de ser «un acto explicito» con boton y pasa a ser recarga de pagina o reentrada en la pantalla. Decision del PO: en explotaciones pequenas no hay varios usuarios introduciendo datos a la vez | `MVP-706` |
| `RN-008` — Filtro por defecto inicial | Deja de aplicar solo al dashboard: la temporada de trabajo pasa a ser el defecto tambien en diario, cosechas y compras | `MVP-701` |
| `RN-009` — Widgets minimos obligatorios | Se amplia con la lectura economica de la campana (gasto e ingreso) | `MVP-707` |
| `RN-029` — Produccion MVP limitada al nucleo operativo | Se matiza: la cosecha admite precio por kg e importe derivado. Sigue sin haber molturacion ni capa comercial | `MVP-707` |
| `RN-041` — Todo lo que se conserva tiene plazo | Se anade la categoria de tokens de refresco revocados o caducados, con plazo corto | `MVP-714` |

## Notas y decisiones

- **Esta epica no amplia el producto, lo cuadra.** Toda ampliacion funcional real —modelo de
  produccion, offline, proveedores de identidad, ciclo de vida de la membresia— se dejo fuera de forma
  explicita en la clasificacion punto por punto del 2026-08-06/07.
- **La leccion de `P-055` esta escrita en `CA-6`.** Un punto con destino asignado pero sin historia
  que lo construya se pierde: `MVP-502` cerro sin recogerlo porque su alcance era seguridad y PII, no
  accesibilidad. Ningun punto de esta epica se apoya en «lo hara la historia de al lado».
- **El hito reordena el roadmap.** `Hito G` pasa a ser esta epica; `Resiliencia offline` se desplaza a
  `Hito H` y `Escalado funcional` a `Hito I`. El orden temporal real lo justifica: los ajustes salen
  del primer uso y son previos a cualquier evolucion.
- **Un punto se descarta**: `P-039` (avisar por correo de que una invitacion se ha anulado). Decision
  del PO: basta con que se avise al intentar unirse, que es lo que ya hace el preview del enlace.
