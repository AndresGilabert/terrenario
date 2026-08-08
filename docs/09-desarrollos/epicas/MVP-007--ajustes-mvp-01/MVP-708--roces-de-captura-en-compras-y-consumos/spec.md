---
id: "MVP-708"
tipo: feature
titulo: "Roces de captura en compras y consumos"
estado: completado
prioridad: baja
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["operativa", "ux"]
  modulo_path: "03-modulos/"
  componentes: ["purchases", "consumptions"]
  etiquetas: ["mvp", "ajustes", "ux"]
  nivel_riesgo: bajo
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-708 — Roces de captura en compras y consumos

> **Origen**: `P-057` y `P-058` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-057`** — El alta de compra sugiere el vocabulario del historico (`GET /api/v1/purchases/products`,
`RN-031`), pero el formulario de consumo sin compra previa —el mismo campo de texto libre, en la misma
pantalla— no sugiere nada, porque las sugerencias solo miran `purchases`. Favorece justo la dispersion
de nombres que las sugerencias existen para evitar: «Abono NPK» comprado y «abono npk» consumido
conviven sin que nadie lo note.

**`P-058`** — Se admite imputar una compra con fecha **anterior** a la de la propia compra, sin aviso.
Verificado: imputando el 2020-01-01 una compra del 2026-07-31, responde `201`. No debe **bloquearse**
—la captura retroactiva es real y `RN-032` ya asume que el papeleo va por detras del campo— pero un
consumo anterior a su compra es casi siempre un error de tecleo en la fecha.

## Objetivo

Reducir dos roces del formulario que ensucian los datos sin que nadie se de cuenta.

## Requisitos de usuario

### HU-1 — Escribir el mismo nombre siempre

**Como** persona registrando consumos,
**quiero** que el campo me sugiera los materiales que ya uso,
**para** no acabar con tres nombres distintos del mismo producto.

### HU-2 — Enterarme de una fecha imposible

**Como** persona imputando una compra,
**quiero** que se me avise si la fecha del consumo es anterior a la de la compra,
**para** corregir el tecleo sin que me lo impidan cuando de verdad es asi.

## Alcance (in-scope)

- Las sugerencias de material combinan el historico de **compras y de consumos**, o el endpoint acepta
  un ambito.
- Aviso **no bloqueante** cuando la fecha del consumo es anterior a la de su compra: senal en el
  formulario y etiqueta en la fila, con la misma filosofia que `RN-023` usa para la temporada.
- Regla de negocio nueva documentada para ese aviso.

## Fuera de alcance (out-of-scope)

- Normalizacion avanzada de nombres (acentos, similitud), que sigue fuera de alcance desde `MVP-205`.
- Bloquear ninguna fecha: la captura retroactiva es legitima.

## Criterios de aceptación

- [x] **CA-1**: El campo de material del consumo sin compra previa sugiere terminos del historico de
  compras **y** de consumos.
  **Evidencia**: `GET /api/v1/purchases/products` une en SQL `purchases` y los consumos sin compra
  previa (`MaterialRepository`), y `ConsumptionFormModal` alimenta su `datalist` con esa lista.
  `PurchaseCaptureFrictionTests.ElVocabularioDeMateriales_Deberia_AprenderDeComprasYDeConsumos`:
  «Cobre de la nave», que solo existe como consumo, se devuelve tambien en la busqueda parcial.
  `ComprasView.test.tsx` comprueba que el campo del modal ofrece las dos entradas.
- [x] **CA-2**: Imputar un consumo con fecha anterior a la de su compra sigue permitido y responde
  `201`, con aviso visible en el formulario.
  **Evidencia**: `PurchaseCaptureFrictionTests.ImputarConFechaAnteriorALaCompra_Deberia_Responder201_ConElAviso`
  reproduce el caso del punto —imputar el `2020-01-01` una compra del `2026-07-31`— contra la API
  real: `201`, `is_before_purchase_date: true` y `proportional_cost` intacto. En el formulario,
  `ComprasView.test.tsx` verifica que el aviso aparece al teclear la fecha y que el boton de guardar
  sigue habilitado.
- [x] **CA-3**: La fila resultante lleva una etiqueta que lo senala, como ya hace «FUERA DE TEMPORADA».
  **Evidencia**: etiqueta `antes de la compra` en la fila de «Consumos por terreno» (`ComprasView`) y
  `ANTES DE LA COMPRA` en la tarjeta del diario (`DiarioView`), junto a las de `RN-023` y `RN-032`.
  `ComprasView.test.tsx` la exige presente con el aviso y ausente sin el; el test de integracion
  comprueba que la senal llega tanto a `GET /consumptions` como a `GET /diary`.
- [x] **CA-4**: La regla queda escrita en `docs/01-producto/reglas-de-negocio.md`.
  **Evidencia**: `RN-043 — Consumo anterior a su compra permitido con aviso`, redactada en paralelo a
  `RN-023`, con el porque de no bloquear y el criterio de derivarla en lectura. Referenciada desde
  `contratos-api.md` §7 y desde `03-modulos/diario-y-operativa/README.md`.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/ComprasView.tsx](../../../../../prototype/terrenario-mvp/src/components/ComprasView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| ComprasView | RN-031, RN-032, RN-043 (nueva), RN-023 (analogia) | completo | Vocabulario unico en los dos formularios y aviso de fecha anterior a la compra; `ComprasView.test.tsx` (4 tests) y `PurchaseCaptureFrictionTests` (3 tests) |
| DiarioView | RN-033, RN-043 | completo | Etiqueta «ANTES DE LA COMPRA» junto a «FUERA DE TEMPORADA»; `is_before_purchase_date` verificado en `GET /diary` |

## Notas y decisiones

- Los dos son del mismo formulario: separarlos obligaria a tocarlo dos veces.
- De las dos vias que abria el alcance para `P-057` se elige **combinar** los dos historicos, no un
  ambito en el endpoint: un ambito dejaria vivos dos vocabularios en la misma pantalla, que es la
  causa del punto. Razonamiento completo en el [tech-design](./tech-design.md).
- Las **imputaciones no cuentan** en el vocabulario: copian el material de su compra, asi que no
  aportan nombres nuevos y solo desordenarian la frecuencia.
- La ruta `GET /api/v1/purchases/products` **no se mueve** pese a devolver ya material de los dos
  libros: renombrarla romperia el contrato sin que nadie gane nada. Arruga de nombre anotada.
- El aviso de `RN-043` se **deriva en lectura**: si se corrige la fecha de la compra, desaparece solo.
- El aviso llega tambien al diario, que es la vista principal (`RN-033`) y donde `RN-023` ya rotula
  «FUERA DE TEMPORADA»: un aviso que solo estuviera en el libro solo lo veria quien fuera a buscarlo.

## Resultado

Implementada en la rama `feature/MVP-708--roces-de-captura-en-compras-y-consumos`. Diseno tecnico y
verificacion en [tech-design.md](./tech-design.md). `P-057` y `P-058` quedan marcados como resueltos
en el registro de `MVP-999`.
