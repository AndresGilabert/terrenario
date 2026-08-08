---
id: "MVP-707"
tipo: feature
titulo: "Valor economico de la campana"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-701", "MVP-706"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "dashboard", "producto"]
  modulo_path: "03-modulos/"
  componentes: ["harvests", "dashboard", "modelo-datos"]
  etiquetas: ["mvp", "ajustes", "alcance", "economia"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-707 — Valor economico de la campana

> **Origen**: `P-084` y `P-092` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-084`** — Peticion del PO: en las cosechas cuyo destino es la venta de aceituna no se puede indicar
el importe por kilo. Verificado: `HARVEST` no tiene ningun campo economico —ni en el modelo, ni en el
contrato, ni en el formulario— porque `RN-029` limita la produccion a producto, kilos, destino y uno
entre rendimiento o litros, «sin capa comercial ni de molturacion».

La consecuencia va mas alla del campo pedido: el MVP **solo registra gasto** (compras y consumos,
`RN-003`) y **ningun ingreso**, asi que la pregunta «¿me ha salido a cuenta la campana?» no tiene
respuesta en el producto.

**`P-092`** — La Vision General no publica ninguna cifra economica, ni siquiera el gasto, que **ya
existe y ya se calcula**: el diario unificado lo rotula en su cabecera de totales. Resultado: el gasto
de la campana se ve en la vista de registro cronologico y no en la de resumen, que es donde se busca.

## Objetivo

Que la campana tenga una lectura economica minima: cuanto ha entrado, cuanto ha salido, y que las dos
cifras vivan donde se resume la campana.

## Requisitos de usuario

### HU-1 — Apuntar a cuanto se vendio

**Como** titular de la explotacion,
**quiero** registrar el precio por kilo al que vendi una partida,
**para** saber lo que me ha dado cada cosecha sin llevar la cuenta aparte.

### HU-2 — Ver el balance de la campana

**Como** titular de la explotacion,
**quiero** ver en el resumen lo que he gastado y lo que he ingresado,
**para** hacerme una idea de como va la campana de un vistazo.

## Alcance (in-scope)

- `unit_price` **opcional** en `HARVEST`, con importe derivado (`kgs x unit_price`), ofrecido en el
  formulario cuando el destino es de venta y admitido —sin insistir— en el resto.
- Migracion **aditiva**: ninguna cosecha existente cambia de significado; sin precio, no hay importe.
- Importe visible en el listado de cosechas y en la entrada del diario.
- Tarjeta de **gasto** y tarjeta de **ingreso** de la campana en la Vision General, respetando el
  ambito de temporada y terrenos ya aplicado.
- Matiz de `RN-029` y ampliacion de `RN-009` con la lectura economica.

## Fuera de alcance (out-of-scope)

- **Margen, rentabilidad y desglose de gasto por tipo**: el PO acoto el alcance al minimo. Dos cifras,
  no un modulo de contabilidad.
- **Maestro de almazaras y empresas compradoras** (`P-062`), con precio pactado y rendimiento por
  defecto: queda en backlog con el resto del cluster de modelo de produccion.
- Categoria y proveedor en compras (`P-054`): backlog.
- Impuestos, facturas, cobros, pagos y cualquier forma de documento comercial.

## Criterios de aceptación

- [x] **CA-1**: Registrar una cosecha con destino de venta ofrece el precio por kilo, y el importe se
  calcula y se muestra sin teclearlo. Verificado en UI: con destino de venta la etiqueta es «Precio de
  venta por kilo»; escribir `0,75` sobre 1.000 kg actualiza el importe a `= 750,00 €` mientras se
  escribe. En el resto de destinos el campo sigue disponible con etiqueta secundaria, porque quien
  vende parte de una partida destinada a consumo propio tambien quiere apuntarlo.
- [x] **CA-2**: El precio es opcional: una cosecha sin el se guarda igual y no aparece con importe cero.
  Verificado en el listado real: tres de las cuatro partidas rotulan **«Sin dato»**, no «0,00 €». Un
  cero explicito se **rechaza** (`VALIDATION_HARVEST_UNIT_PRICE_RANGE`): quien no lo sepa deja el campo
  vacio.
- [x] **CA-3**: Editar los kilos o el precio recalcula el importe; el importe no se persiste como dato
  independiente que pueda divergir. Verificado contra la API: `PATCH kgs = 1.000` sobre una partida con
  precio `0,62` deja `amount = 620,00 €` sin tocar el precio.
- [x] **CA-4**: La Vision General muestra gasto e ingreso de la campana, coherentes con el ambito de
  temporada y terrenos aplicado y con lo que suma el diario para el mismo periodo. **Coinciden por
  construccion**: el panel no recalcula el gasto, se lo pregunta al diario, que es donde vive la regla
  de que cuenta como gasto (`R-01` de MVP-399). Medido: `390,00 €` de gasto y `620,00 €` de ingreso en
  las dos pantallas.
- [x] **CA-5**: Sin ninguna cosecha con precio, la tarjeta de ingreso dice «sin dato», no «0 €».
  Verificado contra la API antes de poner ningun precio: `income: null`, `harvests_with_price: 0`.
- [x] **CA-6**: `RN-029` y `RN-009` actualizadas en `docs/01-producto/reglas-de-negocio.md`, y el ER y
  `contratos-api.md` recogen el campo nuevo (`unit_price`, `amount` derivado y el endpoint
  `GET /api/v1/dashboard/economics`).

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| CosechaModal | RN-029 (matizada), RN-012 | hecho | Precio por kilo opcional e importe en vivo, verificado en el formulario real |
| DashboardView | RN-009 (ampliada) | hecho | Tarjetas de gasto e ingreso como quinto widget, coherentes con el diario |

## Notas y decisiones

- **Este es el unico punto de la epica que amplia el alcance funcional del MVP.** El PO eligio
  deliberadamente el minimo que no arrastra el cluster de modelo de produccion (`P-059` a `P-063`):
  precio por kilo en la cosecha, no maestro de compradoras.
- El importe es **derivado, no columna**: guardarlo permitiria que divergiera de kilos por precio, y
  entonces habria dos verdades.
- Va **despues** de `MVP-706`: las dos tocan `VisionGeneralView`.
