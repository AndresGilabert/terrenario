---
id: "MVP-809"
tipo: tarea
titulo: "Trazabilidad de los requisitos de usuario"
estado: aprobado
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

- [ ] **CA-1**: `definicion-requisitos-usuario.md` contiene la matriz con los **47** requisitos y el
  destino real de cada uno.
- [ ] **CA-2**: El pipeline de KB **falla** cuando un requisito marcado MVP no tiene destino.
  Verificado **provocando el fallo** con un requisito de prueba, no leyendo la regla.
- [ ] **CA-3**: El pipeline pasa en verde sobre el estado real de la KB una vez poblada la matriz.
- [ ] **CA-4**: `RU-32`, `RU-33`, `RU-34` y `RU-36` reflejan su estado real, con la decision y su
  motivo escritos.
- [ ] **CA-5**: Todo hallazgo del repaso que no cierre esta historia queda registrado como punto nuevo
  en `MVP-999`, con su destino propuesto.
- [ ] **CA-6**: `desarrollo-local.md` recoge la nota de entorno de `P-069`.

## Notas y decisiones

- **`CA-2` es el criterio que distingue esta historia de una tabla mas.** Una matriz que nadie
  comprueba envejece igual que el registro de puntos: la leccion de `P-096` es literalmente esa, y por
  eso el criterio exige provocar el fallo.
- **El repaso probablemente encuentre mas huecos.** `CA-5` existe para que se registren en vez de
  ampliar el alcance de esta historia sobre la marcha, que es como una tarea de gobernanza se convierte
  en un frente abierto.
- **Se corrigen los requisitos, no se borran.** Un `RU` que pasa a backlog sigue existiendo con su
  numero: la convencion de la KB es no reutilizar identificadores aunque la regla se retire.
