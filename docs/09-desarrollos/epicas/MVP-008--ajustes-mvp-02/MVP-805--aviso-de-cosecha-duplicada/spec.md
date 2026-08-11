---
id: "MVP-805"
tipo: feature
titulo: "Aviso de cosecha duplicada"
estado: completado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "0.5d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: ["MVP-803"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["producto", "frontend", "backend"]
  modulo_path: "03-modulos/"
  componentes: ["produccion"]
  etiquetas: ["mvp", "ajustes", "RU-24", "avisos"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-805 — Aviso de cosecha duplicada

> **Origen**: `P-110` del registro de `MVP-999`, detectado en la segunda revision completa del MVP
> (2026-08-10).

## Contexto

`RU-24` («Avisos de posibles duplicados», Estado: MVP) dice: si se intenta crear una cosecha con mismo
terreno, fecha, producto y unidad que uno existente, se muestra aviso y se permite guardar igual, sin
bloqueo.

Verificado en el navegador: con la partida del 20 de octubre de 2025 en «Matorral» ya registrada, abrir
el modal y poner exactamente ese terreno, esa fecha y ese producto **no produce ningun aviso**. No hay
logica de duplicados en produccion; la unica que existe en el sistema es la unicidad de nombre de los
maestros (`MVP-205`, `MVP-207`), que es otra cosa —una guarda que bloquea, no un aviso—.

Ninguna historia de `MVP-001` a `MVP-007` menciona `RU-24`, asi que no es una decision de alcance
registrada: es un requisito que se perdio. Es una de las tres consecuencias de `P-114`.

El producto ya tiene el patron: `RN-023` avisa cuando la fecha queda fuera del rango de la campana y
`RN-043` cuando un consumo es anterior a su compra. Los dos avisan sin impedir, en el mismo formulario
y con el mismo aspecto. Este es un tercero de la misma familia.

## Objetivo

Que apuntar dos veces la misma partida deje de ser silencioso, sin impedirlo: una cosecha repetida el
mismo dia en el mismo terreno es posible y a veces correcta.

## Requisitos de usuario

### HU-1 — Que me avisen si ya lo apunte

**Como** titular de la explotacion,
**quiero** que el formulario me avise si ya existe una partida igual,
**para** no duplicar los kilos de una campana por apuntar dos veces lo mismo.

## Alcance (in-scope)

- Aviso **no bloqueante** en el formulario de cosecha cuando ya existe una partida viva con el mismo
  terreno, la misma fecha y el mismo producto.
- El aviso aparece **mientras se rellena**, como los de `RN-023` y `RN-043`, no al intentar guardar.
- El aviso nombra la partida existente (kilos y destino) para que se pueda distinguir de un vistazo si
  es la misma o una segunda de verdad.
- Al **editar** una partida, la comparacion excluye la propia: corregir el destino de una cosecha no
  puede avisar de que esa cosecha ya existe.
- Nueva regla de negocio que recoja el aviso, con la misma redaccion no bloqueante que `RN-023` y
  `RN-043`, y actualizacion del estado de `RU-24` en el documento de requisitos.

## Fuera de alcance (out-of-scope)

- **Bloquear el alta**: `RU-24` dice expresamente «Se permite guardar igual (sin bloqueo)», y dos
  partidas del mismo terreno y dia son un caso real.
- Avisos de duplicado en actividades, compras o consumos: `RU-24` habla solo de cosechas.
- Deteccion difusa (kilos parecidos, fechas proximas) o cualquier forma de fusion de partidas.
- Validaciones de rango blando —avisar de una cosecha inusualmente alta—, que el documento de
  requisitos deja expresamente en backlog.

## Criterios de aceptación

- [x] **CA-1**: Al rellenar una cosecha con terreno, fecha y producto iguales a los de una partida viva
  del Workspace, aparece un aviso que nombra la partida existente con sus kilos y su destino.
  **Evidencia** (API real, el escenario con el que se detecto `P-110`): con la partida del 20 de
  octubre de 2025 en «Matorral» registrada,
  `GET /harvests/duplicates?plot_id=<Matorral>&date=2025-10-20&product=aceituna_olivar` devuelve
  `{ kgs: 1000.00, destination: "aceite_para_venta" }`. El test de componente comprueba que el aviso
  rotula «1000 kg, Aceite para venta».
- [x] **CA-2**: El aviso **no impide guardar**: la partida se crea con normalidad si se confirma.
  **Evidencia**: test de componente que comprueba que el boton de guardar sigue habilitado con el aviso
  a la vista. El servidor no cambia: no hay validacion nueva en el alta.
- [x] **CA-3**: Editar una partida existente sin cambiar terreno, fecha ni producto **no** dispara el
  aviso.
  **Evidencia**: con `exclude_id` de la propia partida, la misma consulta devuelve `total: 0` contra la
  API real. Y el formulario **manda** ese identificador al corregir, comprobado con test de componente.
- [x] **CA-4**: Una partida eliminada (borrado logico, `RN-037`) no dispara el aviso.
  **Evidencia**: prueba de integracion que borra la partida y vuelve a preguntar: `total: 0`. Se cumple
  por construccion —el puerto excluye las eliminadas en todas sus lecturas— y aun asi hay test, porque
  «por construccion» es lo que deja de ser cierto cuando alguien anade otra consulta.
- [x] **CA-5**: El aviso convive con los de `RN-023` y `RN-043` sin desplazar el formulario ni ocultar
  ninguno de los dos cuando coinciden.
  **Evidencia**: los dos avisos se apilan en el mismo bloque, **encima** de los campos de captura, asi
  que ninguno desplaza lo que se esta escribiendo. Test de componente con una fecha fuera de rango
  **y** duplicada: los dos aparecen. `RN-043` no aplica a este formulario —es del consumo—, y se hace
  constar en vez de darlo por comprobado.
- [x] **CA-6**: `RU-24` deja de figurar sin destino en el documento de requisitos.
  **Evidencia**: `RU-24` recoge su entrega en `MVP-805`, su formalizacion como `RN-044` y la decision
  sobre «misma unidad» con su motivo.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Notas y decisiones

- **«Misma unidad» no se traduce literalmente** (decision del PO, 2026-08-10). `RU-24` se escribio
  cuando la cosecha aun podia informar el rendimiento de varias formas; hoy `RN-013` fija la unidad
  canonica y el modo de entrada es solo eso, un modo de entrada. La comparacion es **terreno + fecha +
  producto**, que es lo que identifica una partida. Incluir el modo de entrada dejaria sin avisar
  precisamente el duplicado mas probable: quien apunta dos veces lo mismo suele hacerlo de dos
  maneras, una con litros y otra con rendimiento. Anadir los kilos tampoco: se escaparia el caso de
  teclear mal la cantidad al repetir, que es cuando el aviso mas sirve.
- Va **despues de `MVP-803`**, que rehace la superficie de Cosechas.
