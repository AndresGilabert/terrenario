---
id: "MVP-809"
tipo: tarea
titulo: "Trazabilidad de los requisitos de usuario"
estado: completado
prioridad: alta
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
  dominios: ["gobernanza", "documentacion", "calidad"]
  modulo_path: "03-modulos/"
  componentes: ["kb", "pipeline-ci"]
  etiquetas: ["mvp", "ajustes", "trazabilidad", "gate"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-809 — Trazabilidad de los requisitos de usuario

> **Origen**: `P-114` del registro de `MVP-999`, con las correcciones documentales de `P-111` y
> `P-112`. Detectado en la segunda revision completa del MVP (2026-08-10).

## Contexto

De los 47 requisitos de usuario definidos en `definicion-requisitos-usuario.md`, **solo tres se citan
en algun otro documento de la KB**: `RU-18`, `RU-19` y `RU-31`, y los tres por casualidad —los arrastran
`P-017` y `P-029` del registro de puntos—.

Las epicas y las historias trazan contra `RN-xxx`, que es una capa mas abajo. Nadie traza contra
`RU-xxx`. Asi que el primer eslabon de la cadena que el propio roadmap declara como criterio de
priorizacion —«maximizar trazabilidad requisito -> regla -> contrato -> validacion»— **no existe**, y un
requisito puede quedarse marcado «Estado: MVP» durante todo el roadmap sin que nada lo delate.

No es una hipotesis. La misma revision encontro las tres consecuencias:

- `RU-24` (aviso de cosecha duplicada), marcado MVP, nunca construido ni descartado -> `P-110`.
- `RU-32`, `RU-33` y `RU-34` (sugerencias y planificacion de tareas), marcados MVP, sin modulo, sin
  epica y sin decision -> `P-111`.
- `RU-36` (codigo por email en la baja de cuenta), marcado MVP, resuelto de otra forma sin que la
  divergencia conste como decision -> `P-112`.

Es el mismo patron que `P-096` describio en el otro extremo del ciclo: una cadena que solo se sostiene
si alguien se acuerda. Y la respuesta tiene que ser la misma que entonces —una comprobacion en el
gate—, porque la diligencia ya se demostro insuficiente.

## Objetivo

Que un requisito de usuario marcado «Estado: MVP» no pueda quedarse sin destino declarado sin que el
gate de KB lo diga.

## Requisitos de usuario

### HU-1 — Que la KB delate lo que se ha quedado atras

**Como** responsable del producto,
**quiero** que el pipeline falle si un requisito marcado MVP no tiene destino,
**para** no descubrirlo dos revisiones tarde.

## Alcance (in-scope)

- **Matriz de trazabilidad** `RU -> destino` en `definicion-requisitos-usuario.md`: para cada requisito,
  la regla, la historia o la decision que lo recoge, y su estado real (`entregado`, `en <historia>`,
  `backlog`, `descartado`).
- **Comprobacion en `validar_kb.py`** que falle cuando un requisito marcado «Estado: MVP» no tenga
  destino declarado, o cuando su destino nombre una historia `completado` sin que el requisito conste
  como entregado. Imputada al documento correcto, con el mismo criterio que la guarda de `P-096`.
- Repaso completo de los 47 requisitos para poblar la matriz con lo que **de verdad** hay hoy, no con
  lo que se supone. Cualquier hallazgo nuevo de ese repaso se registra como punto en `MVP-999`, no se
  arregla aqui.
- **Correccion de `RU-32`, `RU-33` y `RU-34`** (`P-111`): dejan de figurar como MVP y pasan a backlog
  con destino explicito, porque son alcance de epica propia.
- **Correccion de `RU-36`** (`P-112`): la confirmacion de la baja de cuenta es una frase tecleada
  verificada en servidor, no un codigo por email. Se registra como decision, con su motivo.
- Nota de entorno pendiente de `P-069` en `desarrollo-local.md`: la suite de backend exige Docker y una
  politica de Application Control permisiva, y el CI sobre Linux es el entorno de referencia.

## Fuera de alcance (out-of-scope)

- **Construir** ninguno de los requisitos que el repaso encuentre sin destino: el aviso de duplicados
  es `MVP-805` y las sugerencias de tareas se quedan en backlog. Esta historia declara, no implementa.
- Trazabilidad de `RN-xxx` hacia contratos o tests: la cadena tiene mas eslabones, pero el que falta es
  el primero.
- Renumerar, reescribir o consolidar los requisitos existentes.

## Criterios de aceptación

- [x] **CA-1**: `definicion-requisitos-usuario.md` contiene la matriz con los **47** requisitos y el
  destino real de cada uno.

  **Evidencia.** Seccion «Matriz de trazabilidad RU -> destino» al final del documento, con una fila por
  `RU-01`..`RU-47`. Contado por el propio lector del gate:

  ```text
  declarados 47 filas 47
  sin estado: []
  Counter({'mvp': 36, 'backlog': 7, 'fase-posterior': 4})
  Counter({'entregado': 28, 'backlog': 12, 'entregado con hueco': 5, 'en <historia>': 2})
  ```

  El reparto por estado real: **28 entregados**, **5 entregados con hueco** (`RU-06`, `RU-10`, `RU-29`,
  `RU-39`, `RU-44`), **2 en curso** (`RU-21` en `MVP-804` y `RU-24` en `MVP-805`) y **12 en backlog**. El
  repaso se hizo contra el codigo, no contra lo que se suponia; las divergencias encontradas estan en
  `CA-5`.

- [x] **CA-2**: El pipeline de KB **falla** cuando un requisito marcado MVP no tiene destino.
  Verificado **provocando el fallo** con un requisito de prueba, no leyendo la regla.

  **Evidencia.** Se inyecto temporalmente un `RU-48` con `Estado: MVP` y con la celda de destino vacia
  en la matriz, y se ejecuto el comando bloqueante del CI. Salida literal:

  ```text
  ❌ 1 error(es) encontrado(s):
    ERROR: D:\PROJECTES\terrenario\.claude\worktrees\agent-abd7aa721f7997a58\docs\01-producto\definicion-requisitos-usuario.md: RU-48 esta marcado 'Estado: MVP' y no tiene destino declarado. Escribe en la matriz de trazabilidad la regla (RN-xxx), la historia (MVP-xxx), el punto del registro (P-xxx) o el ADR que lo recoge.

  Corrige los errores antes de continuar.

  [RUN] Validar estructura y frontmatter
        C:\Users\Andres\AppData\Local\Python\pythoncore-3.14-64\python.exe D:\PROJECTES\terrenario\.claude\worktrees\agent-abd7aa721f7997a58\docs\00-meta\scripts\validar_kb.py --validar --solo-cambios --base-ref origin/develop
  [FAIL] Validar estructura y frontmatter (exit=1)
  ```

  Se provoco tambien la **segunda mitad** de la comprobacion: marcar `RU-01` como `en MVP-202` con
  `MVP-202` en estado `completado`. Salida literal en `--solo-cambios`:

  ```text
  ⚠️  1 advertencia(s):
    WARN:  [legacy] D:\PROJECTES\terrenario\.claude\worktrees\agent-abd7aa721f7997a58\docs\09-desarrollos\epicas\MVP-002--maestros-operativos-y-onboarding\MVP-202--maestro-de-terrenos-con-alta-minima\spec.md: MVP-202 esta 'completado' y es el destino de RU-01, pero el requisito sigue como 'en MVP-202' en la matriz de trazabilidad de 01-producto/definicion-requisitos-usuario.md. Marcalo 'entregado', o 'entregado con hueco' con el punto que persigue lo que falta, o cambia su destino.
  ```

  Y en modo estricto:

  ```text
  ❌ 1 error(es) encontrado(s):
    ERROR: D:\PROJECTES\terrenario\.claude\worktrees\agent-abd7aa721f7997a58\docs\09-desarrollos\epicas\MVP-002--maestros-operativos-y-onboarding\MVP-202--maestro-de-terrenos-con-alta-minima\spec.md: MVP-202 esta 'completado' y es el destino de RU-01, pero el requisito sigue como 'en MVP-202' en la matriz de trazabilidad de 01-producto/definicion-requisitos-usuario.md. Marcalo 'entregado', o 'entregado con hueco' con el punto que persigue lo que falta, o cambia su destino.
  ```

  **Esa diferencia es el diseno, no un defecto.** El error se imputa al `spec.md` de la historia, igual
  que en la guarda de `P-096`: en `--solo-cambios` un hallazgo sobre un fichero que el PR no toca se
  degrada a aviso, y este PR no toca `MVP-202`. En el PR que cierra una historia, su spec **si** esta en
  el diff, y ahi bloquea, que es cuando tiene que hacerlo. Atribuyendolo al documento de requisitos, este
  caso no bloquearia nunca.

  Los dos requisitos de prueba se retiraron; el estado final del documento es el de `CA-3`.

  **Y cazo un caso real antes de salir de la rama.** Al rebasar sobre `develop`, `MVP-808` habia
  mergeado y estaba ya en `completado`. La matriz decia `RU-31` -> `en MVP-808`, y la comprobacion
  bloqueo con el error imputado al spec de `MVP-808`. El hallazgo era legitimo: `MVP-808` entrego el
  minimo in-app, no la generalizacion por canal y tipo de tarea que pide `RU-31`. Se corrigio la matriz
  —`RU-31` pasa a estado real `backlog`, con `MVP-808` en el destino como lo que si se construyo— y se
  acoto la segunda mitad de la comprobacion a los requisitos **declarados MVP**: un requisito de fase
  posterior puede recibir una rebanada de una historia sin quedar entregado, y exigirle «entregado»
  ensenaria a escribirlo para callar al gate.

- [x] **CA-3**: El pipeline pasa en verde sobre el estado real de la KB una vez poblada la matriz.

  **Evidencia.** Mismo comando bloqueante, sin el requisito de prueba y con la matriz real poblada:

  ```text
  [RUN] Validar estructura y frontmatter
  [OK]   Validar estructura y frontmatter

  [RUN] Regenerar indices de epicas
  [OK]   Regenerar indices de epicas

  [RUN] Comprobar _indice.md sin cambios pendientes
  [OK]   Todos los _indice.md estan actualizados

  [RUN] Linting markdown
  [OK]   Linting markdown

  [OK] Pipeline de validacion KB completado
  ```

  `validar_kb.py --validar` en modo estricto: **0 advertencias, 0 errores**.

- [x] **CA-4**: `RU-32`, `RU-33`, `RU-34` y `RU-36` reflejan su estado real, con la decision y su
  motivo escritos.

  **Evidencia.**

  - `RU-32`, `RU-33` y `RU-34` pasan de «Estado: MVP» (y «MVP (basico)» en `RU-32`) a «Estado: Backlog
    post-MVP», con una nota de correccion encabezando el bloque que da el motivo: no es un defecto de
    la entrega sino alcance nuevo del tamano de una epica —entidad de plan, motor de recurrencia sobre
    el historico, senal de omision y superficie propia—, y el producto no tiene hoy el concepto de tarea
    planificada. En la matriz, los tres con destino `P-111` y estado real `backlog`.
  - `RU-36` pasa de «Confirmacion por codigo de email para borrado» a «Confirmacion explicita del
    borrado con frase tecleada», describiendo lo que el producto hace de verdad —teclear
    `ELIMINAR MI CUENTA`, comprobado **tambien en servidor**— con la decision del PO y su motivo:
    la frase ya cumple lo que el requisito buscaba (confirmacion explicita, informada y verificada en
    servidor de una operacion irreversible) y el codigo anadiria un sexto correo del producto y un punto
    de fallo de entrega a un flujo que la persona inicia estando ya autenticada. Comprobado en el codigo
    antes de escribirlo: `AccountController.ConfirmationPhrase` y su comparacion en servidor.

- [x] **CA-5**: Todo hallazgo del repaso que no cierre esta historia queda registrado como punto nuevo
  en `MVP-999`, con su destino propuesto.

  **Evidencia.** Cuatro puntos nuevos, todos verificados en el codigo y ninguno arreglado aqui:

  | Punto | Hallazgo | Destino propuesto |
  |---|---|---|
  | `P-119` | `RU-06` promete filtrar por **tarea** y ese filtro no existe: ni `DiaryFilter` ni `ActivityFilter` lo admiten, y la UI solo monta tipo, terreno, temporada, responsable y busqueda | Backlog post-MVP (ampliacion del filtro del diario) |
  | `P-120` | Al editar una actividad antigua, el selector de responsable no reofrece al trabajador si se ha inactivado, contra lo que dice `RU-29`. El mismo modal si lo hace para la tarea | Backlog post-MVP, u oportunista en la proxima historia que toque `ActivityFormModal` |
  | `P-121` | Las compras no tienen unidad de medida: 25 kg de abono y 5 L de herbicida se guardan igual y el precio unitario significa cosas distintas en cada fila (`RU-10`, `RU-39`) | Backlog post-MVP (cluster de compras y consumos) |
  | `P-122` | `RU-41` dice que se admite duracion 0 y `RN-002` exige horas mayores que 0: dos documentos de la KB que se contradicen. En el mismo lote, `RU-44` dice «sin limite maximo» y existe un tope de 999,99 h | Backlog post-MVP (decision de producto: `RU-41` frente a `RN-002`) |

  Ademas se dejaron **notas de repaso** en la matriz para tres cosas que **no** son huecos pero se leian
  mal: la ubicacion del terreno es texto libre y no coordenadas (`RU-01`); `RU-18`, `RU-19` y `RU-38`
  estan escritos alrededor de una dimension —el cultivo— que el modelo no tiene y que hoy los hace
  ciertos por construccion (`P-059`, `P-060`); y `RU-25` sigue vigente pese a `MVP-304`, porque la
  imputacion vincula compras con **terrenos**, no con registros de trabajo.

- [x] **CA-6**: `desarrollo-local.md` recoge la nota de entorno de `P-069`.

  **Evidencia.** Nueva subseccion «Que exige el entorno (riesgo aceptado de `P-069`)» dentro de
  «Ejecucion de tests»: Docker en marcha para los tests de integracion con Testcontainers, politica de
  Application Control permisiva, la firma exacta del fallo (`0x800711C7` sobre
  `Testcontainers.PostgreSql.dll`) para no confundirlo con un fallo de logica, como comprobarlo
  (`VerifiedAndReputablePolicyState`) y la declaracion de que **el CI sobre Linux es el entorno de
  referencia**. Anadida tambien la fila correspondiente en la tabla de trazabilidad del documento.

## Notas y decisiones

- **`CA-2` es el criterio que distingue esta historia de una tabla mas.** Una matriz que nadie
  comprueba envejece igual que el registro de puntos: la leccion de `P-096` es literalmente esa, y por
  eso el criterio exige provocar el fallo.
- **El repaso probablemente encuentre mas huecos.** `CA-5` existe para que se registren en vez de
  ampliar el alcance de esta historia sobre la marcha, que es como una tarea de gobernanza se convierte
  en un frente abierto.
- **Se corrigen los requisitos, no se borran.** Un `RU` que pasa a backlog sigue existiendo con su
  numero: la convencion de la KB es no reutilizar identificadores aunque la regla se retire.
- **`P-111` no se marca `resuelto`, y es a proposito.** Su fila ya estaba en `backlog-post-mvp`, que es
  un estado decidido, y lo que esta historia entrega es la **correccion del estado del requisito**, no
  la planificacion de tareas. Moverla a `resuelto` diria que existe algo que no existe. La nota de lo
  hecho queda al final de su descripcion. `P-069` tampoco cambia de estado —ya estaba `resuelto` desde
  el 2026-08-03— y solo se anota que su unica tarea residual, la nota de entorno, esta cerrada.
- **El repaso encontro cuatro huecos y ninguno se arreglo aqui.** `P-119` a `P-122`. Tres son
  requisitos MVP que el producto no cumple del todo y el cuarto es una contradiccion entre dos
  documentos de la propia KB, que es el hallazgo mas incomodo: `RU-41` y `RN-002` llevaban siete epicas
  diciendo lo contrario el uno del otro sin que nadie los cruzara.
