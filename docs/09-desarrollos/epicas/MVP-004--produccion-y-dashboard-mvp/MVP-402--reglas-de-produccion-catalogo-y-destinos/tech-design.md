---
id: "MVP-402"
tipo: feature
titulo: "TDD: Reglas de producción, catálogo y destinos"
estado: completado
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "validaciones"]
  modulo_path: "03-modulos/"
  componentes: ["cosechas", "catalogos", "validaciones"]
  etiquetas: ["mvp", "produccion", "reglas"]
  nivel_riesgo: alto
creado_en: "2026-07-29"
actualizado_en: "2026-07-29"
---

# TDD: MVP-402 — Reglas de producción, catálogo y destinos

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Cierra la **semántica** de producción del MVP antes de explotar los datos en el dashboard. `MVP-401`
entregó la entidad; esta historia hace que dos cosechas de dos Workspaces distintos sean comparables.

- **Catálogos cerrados validados en servidor**: `harvest_product` (RN-030) y `harvest_destination`
  (RN-012), con `desconocido` como valor de pleno derecho.
- **Unidad canónica L/100kg** (RN-013) y las **tres entradas equivalentes** de RN-014, con la densidad
  de RN-016 para convertir el rendimiento graso.
- **Rendimiento efectivo** derivado: una cosecha que declaró litros tiene rendimiento igualmente, así
  que la exclusión de RN-004 deja de costar información.

Sin migración: no hay columna nueva. La unidad de entrada no se persiste, y el rendimiento efectivo es
derivado.

### Decisiones de producto y de diseño tomadas en esta historia

- **La unidad de entrada no se guarda; se convierte.** RN-013 fija L/100kg como unidad canónica y
  RN-014 admite además kg de aceite por 100 kg. Guardar la unidad de origen obligaría a cada
  consumidor —listado, diario y los cuatro widgets del dashboard— a convertir antes de comparar, y una
  sola omisión produciría un promedio silenciosamente mal. Se convierte **una vez, en el borde de
  aplicación**, y lo persistido es siempre lo mismo.
- **La conversión vive en la capa de aplicación, no en el dominio.** La unidad de entrada es una
  concesión al usuario (las almazaras dan «rendimiento graso» en kg/100kg), no una propiedad de la
  cosecha. El agregado no conoce unidades: recibe la canónica y punto.
- **`yield_unit` no es un `FieldUpdate<T>`.** No es un campo del recurso, así que «ausente» significa
  «la canónica», no «conserva la anterior». Y la unidad aplica **solo al valor que llega en esta
  petición**: un `PATCH` que no toca el rendimiento no puede volver a dividir por la densidad lo ya
  persistido. Hay test que lo fija.
- **El rendimiento efectivo se calcula en lectura, no se persiste.** RN-014 admite el rendimiento
  «calculado desde kg entregados y litros obtenidos», y RN-004 prohíbe guardar los dos valores a la
  vez. Persistir el derivado sería duplicar un dato que ya está implícito y que quedaría obsoleto al
  corregir los kilos. Se expone como `effective_yield` + `yield_source` (`informado` / `calculado`),
  de modo que la UI no presente como declarado algo que se ha deducido.
- **La densidad de RN-016 vive en una sola constante.** El override por almazara que la regla contempla
  queda fuera del MVP —no existe la entidad almazara— y se registra en `MVP-999` (`P-061`). Así el día
  que se parametrice cambia de origen, no de fórmula.
- **La cota de rendimiento se aplica sobre la canónica.** En modo rendimiento graso el formulario
  rechaza por encima del 92 %, que es el equivalente de 100 L/100kg: no puede salir más aceite que
  fruto. Validar sobre la unidad de entrada habría dejado pasar valores imposibles.
- **«Sin destino» es alias visual, no valor.** El canon en base de datos es `desconocido` (RN-012) y el
  servidor **rechaza** «Sin destino» como destino. Hay test que lo fija, porque es justo la clase de
  deriva que convertiría el catálogo cerrado en texto libre por acumulación.
- **Los catálogos se validan en servidor y se ofrecen desde cliente.** El cliente mantiene sus
  constantes, como ya hacen `plot_ownership_type` y `season_status`; lo que cambia es que ahora el
  servidor **es la autoridad**: un cliente desactualizado recibe `400` en vez de escribir basura. No se
  añade endpoint de catálogos porque ninguna otra taxonomía cerrada del MVP lo tiene y su divergencia
  ya no puede llegar a la base de datos.

### Lo que esta historia cierra de `MVP-401`

| Pendiente que dejó `MVP-401` | Cómo queda |
|---|---|
| Catálogo `harvest_product` validado en servidor | `HarvestProducts` + `VALIDATION_PRODUCT_INVALID` con los valores admitidos en el mensaje |
| Taxonomía cerrada de `destination` validada en servidor | `HarvestDestinations` + `VALIDATION_DESTINATION_INVALID`, con `desconocido` admitido |
| Entradas equivalentes de rendimiento | `yield_unit` (`l_100kg` / `kg_100kg`) convertido con RN-016, y derivación desde litros |
| Rendimiento medio del listado con partidas que declaran litros | Usa `effective_yield`, así que entran todas; el resumen dice sobre cuántas partidas promedia |

## Contrato

Aditivo sobre `contratos-api.md` §6:

- `yield_unit?` en `POST`/`PATCH`: `l_100kg` (por defecto) o `kg_100kg`.
- `effective_yield` y `yield_source` en la representación de la cosecha.
- `yield` en la entrada del diario para las cosechas.
- Nuevo error `VALIDATION_HARVEST_YIELD_UNIT_INVALID`.

Los códigos `VALIDATION_PRODUCT_INVALID` y `VALIDATION_DESTINATION_INVALID` ya estaban contratados;
lo que cambia es que ahora los emite una comprobación de **pertenencia al catálogo**, no de longitud.

## Arquitectura de la solución

```text
Domain/Harvests/HarvestCatalogs.cs   HarvestProducts · HarvestDestinations · HarvestYieldUnits
                                     · HarvestYieldConversion (densidad RN-016 y derivación)
Domain/Harvests/Harvest.cs           valida pertenencia a los catálogos (RN-030, RN-012)
Domain/Harvests/IHarvestRepository   HarvestView.EffectiveYield / YieldSource (derivados)
Application/Harvests/HarvestHandlers YieldNormalizer: unidad de entrada → canónica
```

Frontend: el modal ofrece los **tres orígenes** de RN-014 más «todavía no lo sé», y anticipa el
equivalente en L/100kg mientras se escribe —el cálculo bueno lo hace el servidor, pero nadie debería
teclear un número a ciegas—. El listado muestra el rendimiento efectivo marcando lo derivado, y el
diario lo añade a la tarjeta de cosecha.

## Estrategia de pruebas

`HarvestProductionRulesTests` (17 casos) cubre las cuatro reglas de la historia:

| Regla | Qué se fija |
|---|---|
| RN-030 | Un producto fuera del catálogo es `400`; el catálogo tiene un solo valor en el MVP |
| RN-012 | Los cuatro destinos canónicos se admiten; «Sin destino» se rechaza; `desconocido` no degrada el resto del registro (CA-3) |
| RN-013/RN-014/RN-016 | 20 kg/100kg → 21,7391 L/100kg; la canónica no se toca; unidad desconocida es `400`; un `PATCH` que no toca el rendimiento no reconvierte lo persistido |
| RN-014 (3) | `effective_yield` derivado de litros (220 L de 1.000 kg = 22 L/100kg), preferencia por el informado, `null` sin dato, y `yield_source` coherente |

**Verificación end-to-end conducida**: producto `"aceituna picual"` → `400
VALIDATION_PRODUCT_INVALID` con la lista de valores admitidos; destino `"Sin destino"` → `400
VALIDATION_DESTINATION_INVALID`; `yield: 20, yield_unit: kg_100kg` → persistido `21.7391`;
`yield_unit: "porcentaje"` → `400 VALIDATION_HARVEST_YIELD_UNIT_INVALID`; listado con las tres
partidas mostrando `effective_yield` `20.9302 calculado`, `21.7391 informado` y `19.1587 calculado`;
diario con el rendimiento en la entrada de cosecha; y `PATCH {yield: 18, yield_unit: kg_100kg}` →
`19.5652` con la versión al día.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Que al corregir una cosecha informada en kg/100kg se vea el valor convertido y parezca otro dato | El modal lo dice: la unidad canónica es la que se guardó (RN-013), y el modo de rendimiento graso sigue disponible para reescribirlo. Registrado como decisión, no como defecto |
| Que la densidad fija distorsione el rendimiento de una almazara concreta | RN-016 ya admite override; queda en `MVP-999` (`P-061`) con la constante aislada en un único sitio |
| Que un cliente desactualizado ofrezca valores retirados del catálogo | El servidor es la autoridad desde esta historia: responde `400` con los valores admitidos en el mensaje |
