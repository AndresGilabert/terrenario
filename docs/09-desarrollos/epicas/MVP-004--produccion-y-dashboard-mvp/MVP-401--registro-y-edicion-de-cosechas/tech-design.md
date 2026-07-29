---
id: "MVP-401"
tipo: feature
titulo: "TDD: Registro y edición de cosechas"
estado: completado
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "cosechas"]
  modulo_path: "03-modulos/"
  componentes: ["cosechas", "diario"]
  etiquetas: ["mvp", "produccion", "cosecha"]
  nivel_riesgo: alto
creado_en: "2026-07-29"
actualizado_en: "2026-07-29"
---

# TDD: MVP-401 — Registro y edición de cosechas

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Crea `HARVEST`, la **cuarta entidad operativa** del MVP y la materia prima del dashboard, con su
recurso REST completo y su presencia en el diario cronológico.

- `POST`/`PATCH`/`DELETE`/`GET /api/v1/harvests` con `version` + `If-Match` + `409` (ADR-0005) y
  eliminación **lógica** (`deleted_at`, RN-037). No se reinventa el patrón: es el que estrenó
  `ACTIVITY` en `MVP-301` y ya reutilizan compras y consumos.
- **La cosecha entra en el diario** (`GET /api/v1/diary`) como cuarto tipo, que es lo que completa
  `RN-033` y cierra el hallazgo `G-4` de la revisión de `MVP-003`.
- UI: sección **Cosechas** (`/app/cosechas`) con listado, alta, corrección y borrado confirmado, más
  alta y corrección **desde el propio diario**.

Migración `AddHarvests`, aditiva: crea la tabla y sus índices sin tocar nada existente.

### Decisiones de producto y de diseño tomadas en esta historia

- **El producto de cosecha es un catálogo global fijo de un solo valor: `aceituna_olivar`.** La KB
  exige el catálogo (RN-030) pero **no definía sus valores**: solo aparecía `aceituna_olivar` como
  ejemplo en un JSON de `contratos-api.md`. Decisión del PO (2026-07-29): **la variedad pertenece al
  terreno, no a la cosecha**, y el producto debería vivir a nivel de Workspace para poder modular el
  cálculo de rendimiento según de qué se trate. Mientras eso no exista, el MVP está ligado al olivar y
  ni el registro ni el dashboard distinguen variedades. Las dos ampliaciones quedan registradas en
  `MVP-999` (`P-059`, `P-060`); el campo se mantiene porque RN-030 lo exige y porque es el punto de
  enganche de esa evolución, pero hoy no obliga a elegir nada.
- **`yield` y `liters` son un selector en la UI, no dos campos sueltos.** RN-004 los declara
  excluyentes, así que ofrecerlos a la vez invita a rellenar los dos y a recibir un error que el
  usuario no ha provocado. El formulario pregunta *cómo* se informa el aceite —rendimiento, litros o
  «todavía no lo sé»— y solo enseña el campo que corresponde.
- **El `PATCH` sustituye la pareja completa** cuando llega cualquiera de los dos: enviar solo
  `liters` sobre una cosecha que ya tenía `yield` dejaría los dos informados y el dominio lo
  rechazaría. Es exactamente el criterio que `MVP-301` aplicó al par `task_id`/`task_text` de la
  actividad, y por eso se copia en vez de inventarse otro.
- **La cosecha no tiene coste, y la tarjeta del diario lo dice enseñando kilos.** RN-029 deja fuera
  precio, molturación y balance, así que `cost` es `0` en la proyección. Pintar «0,00 €» donde los
  demás tipos muestran gasto haría creer que la cosecha salió gratis; en su lugar se muestran los
  kilos y el destino. Por lo mismo, `meta` del diario gana `harvests` y `total_kg`: es la magnitud que
  resume una cosecha.
- **La cosecha se corrige *dentro* del diario, a diferencia de la compra.** `MVP-305` mandaba compras
  y consumos a `/app/compras` porque allí viven la imputación, las sugerencias de material y la
  cantidad pendiente. El formulario de cosecha no necesita nada que el diario no tenga ya cargado
  (terrenos y temporadas), así que sacar al usuario de la vista principal sería coste sin beneficio.
- **Filtrar el diario por terreno conserva las cosechas.** La compra se excluye porque es del
  Workspace y solo se reparte por terrenos al imputarla; la cosecha **sí** es de un terreno (RN-001),
  así que el filtro la mantiene y no hay nada que explicar al usuario.
- **`GET /api/v1/harvests/{id}` es nuevo** y lo estrena esta historia, por la misma razón que
  `MVP-305` añadió el de actividades: la entrada del diario es una proyección común y no lleva todos
  los campos que pide el formulario de corrección.
- **Cota de rendimiento en 100 L/100kg.** No puede salir más aceite que fruto, así que un valor por
  encima es siempre un error de tecleo y no una campaña excepcional. Es una guarda de rango, no una
  regla de negocio nueva.

### Lo que esta historia deja abierto a propósito

`MVP-402` —la historia siguiente— cierra la **semántica** de producción, y su alcance lo dice
literalmente. Aquí se entrega la entidad con las validaciones que el propio alcance de `MVP-401`
enumera (kilos, vínculos, exclusión rendimiento/litros); allí se cierran:

| Pendiente | Estado en `MVP-401` | Quién lo cierra |
|---|---|---|
| Catálogo `harvest_product` validado en servidor | El agregado exige producto no vacío y acotado; la UI ofrece solo el valor del catálogo | `MVP-402` (CA-1) |
| Taxonomía cerrada de `destination` validada en servidor | Igual: obligatorio y acotado; la UI ofrece solo los cuatro valores canónicos | `MVP-402` (CA-1/CA-3) |
| Entradas equivalentes de rendimiento (kg aceite/100 kg, derivación desde litros) | El modal calcula el equivalente **solo como ayuda de lectura**; no se persiste ni se agrega | `MVP-402` (RN-014/RN-016) |
| Rendimiento medio del listado con cosechas que declaran **litros** | Hoy solo promedia las que declaran `yield`, así que el resumen dice «Sin datos» si todas informaron litros | `MVP-402` |

Es un hueco de una sola historia y no llega a producción sin cerrarse: `MVP-402` bloquea a `MVP-403`,
`MVP-404` y `MVP-405`, y ninguna de las cuatro sale antes de la promoción del hito.

## Modelo de datos

`HARVEST` sigue el ER canónico de `modelo-de-datos.md` sin divergencias.

| Campo | Tipo | Nota |
|---|---|---|
| `product` | `varchar(60)` | Código de catálogo, no texto libre (RN-030). No es como el `product` de `PURCHASE`, que sí lo es (RN-031) |
| `kgs` | `numeric(10,2)` | Obligatorio y `> 0` (RN-004) |
| `yield` | `numeric(10,4)` nullable | Unidad canónica L/100kg (RN-013). Excluyente con `liters` |
| `liters` | `numeric(10,2)` nullable | Excluyente con `yield` |
| `destination` | `varchar(30)` | Catálogo cerrado con `desconocido` (RN-012) |
| `version` | `bigint` | Token de concurrencia de EF, además de la guarda de aplicación |
| `deleted_at` | `timestamptz` nullable | Baja lógica (RN-037) |

Índices:

- `ix_harvests_live_by_date` — **parcial** sobre `(workspace_id, date)` filtrado por
  `deleted_at IS NULL`, igual que en actividades y compras: el 100% de las lecturas filtra por «vivo».
- `(workspace_id, plot_id)`, `(workspace_id, season_id)` y `(workspace_id, destination)` — los tres
  ejes por los que agregará el dashboard (`MVP-403`/`MVP-404`), puestos desde el principio para no
  reabrir el esquema al llegar allí.

FKs `ON DELETE RESTRICT` a `plots` y `seasons` (los maestros se inactivan, no se borran) y `CASCADE`
al Workspace, como el resto de la operativa.

**La exclusividad `yield`/`liters` la garantiza el agregado, no una restricción de datos**, igual que
el par tarea de `ACTIVITY`: la condición es «como mucho uno» sobre valores ya normalizados.

## Contrato

`contratos-api.md` §6 ya contrataba el recurso. Se añade lo que faltaba:

- `GET /api/v1/harvests/{harvestId}`.
- `meta.total_kg` en el listado, calculado en servidor (mismo criterio que `meta.total_cost` del libro
  de compras).
- `cosecha` pasa de «reservado» a valor vivo de `diary_entry_type`, y la entrada del diario gana
  `kgs` y `destination`. `meta` del diario gana `harvests` y `total_kg`.

Errores: `VALIDATION_HARVEST_KGS_REQUIRED`, `VALIDATION_HARVEST_XOR_YIELD_LITERS`,
`VALIDATION_PRODUCT_INVALID`, `VALIDATION_DESTINATION_INVALID`,
`VALIDATION_HARVEST_YIELD_RANGE`/`_LITERS_RANGE` (nuevos, de rango),
`VALIDATION_HARVEST_REQUIRED_FIELDS`, más los transversales `FOREIGN_KEY_WORKSPACE_MISMATCH`,
`VALIDATION_REQUIRED_IF_MATCH` y `CONFLICT_VERSION_MISMATCH`.

## Arquitectura de la solución

Backend, calcado de `MVP-301` para que las cuatro entidades operativas se comporten igual:

```text
Controllers/HarvestsController.cs        borde de transporte (If-Match, PATCH parcial, 409)
Application/Harvests/HarvestHandlers.cs  Create · Update · Delete · List · Get
Application/Harvests/HarvestLinkResolver guarda de FOREIGN_KEY_WORKSPACE_MISMATCH
Domain/Harvests/Harvest.cs               invariantes (RN-004, RN-012, RN-030) y versión
Domain/Harvests/IHarvestRepository.cs    puerto + HarvestView (filtro de baja lógica dentro)
Infrastructure/.../HarvestRepository.cs  EF Core: proyección con JOIN, orden y traducción del 409
```

`DiaryQueryService` gana un cuarto puerto y un cuarto proyector. No cambia ni la forma de la entrada
ni el orden: es exactamente lo que `MVP-305` había previsto al construir la proyección común.

Frontend:

```text
types/harvest.types.ts            catálogos, etiquetas y payloads
services/harvest.service.ts       sobre el cliente HTTP común (P-007), con If-Match
components/harvests/CosechasView  listado, filtros, resumen y borrado confirmado
components/harvests/HarvestFormModal  alta y corrección, reutilizado por el diario
```

## Estrategia de pruebas

| Nivel | Qué cubre |
|---|---|
| Dominio (`HarvestTests`) | RN-004 (kilos, exclusión rendimiento/litros), RN-012, RN-030, rangos, versión, borrado lógico idempotente y que **no** se valida el rango de temporada (RN-023) |
| Casos de uso (`HarvestHandlersTests`) | 404 por Workspace ajeno, `FOREIGN_KEY_WORKSPACE_MISMATCH`, orden dominio→vínculos, sustitución del par en el `PATCH`, retirada con `null` explícito y 409 antes de mutar |
| SQL real (`HarvestRepositorySqliteTests`) | Traducción de la proyección con `JOIN`, filtro de baja lógica, filtros del listado, orden por fecha de negocio, aislamiento multi-tenant y aviso de RN-023 |
| Diario (`DiaryQueryServiceTests`) | Que la cosecha se mezcla por fecha, que se proyecta con kilos y destino y **sin coste**, que no altera el gasto y que el filtro por terreno la conserva |

**Verificación end-to-end conducida** (no solo tests, lección de `P-014`): API real contra PostgreSQL
y UI conducida en navegador. Comprobado `201` con y sin rendimiento; `400` de XOR, de kilos y de
terreno ajeno; `is_out_of_season_range: true` con fecha de 2019; `400 VALIDATION_REQUIRED_IF_MATCH`
sin cabecera; `PATCH {liters}` que deja `yield: null` y sube la versión; `409` con `current_version`
en el cuerpo; `204` de borrado y desaparición del listado y del diario; diario mezclado con
`harvests: 2`, `total_kg: 2060.5` y `total_cost` intacto; y en la UI el modal precargado, el
equivalente L/100kg al teclear litros, el diálogo de confirmación y la sección activa del menú.

## Impacto en otras piezas

- **`DiarioView`** pasa a mostrar cuatro tipos, con botón «Nueva cosecha» y filtro de tipo `cosecha`.
  El botón de cosecha solo exige terreno y temporada: RN-002 (responsable) es de la actividad, así que
  un Workspace sin trabajadores puede registrar producción aunque todavía no pueda registrar labores.
- **`AppSidebar`** enciende la entrada «Cosechas», que estaba deshabilitada con «Pronto» desde
  `MVP-107`. Queda una sola entrada apagada, «Visión General», que enciende `MVP-403`.
- **`AppLayout`** añade el título contextual de la sección.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Que el catálogo de producto de un solo valor se lea como un olvido | Documentado aquí y en `MVP-999` (`P-059`/`P-060`) con la decisión del PO y su destino |
| Que el hueco de validación de catálogos llegue a producción | `MVP-402` es la historia inmediatamente siguiente y bloquea a las tres del dashboard |
| Que el diario crezca sin paginar con un cuarto tipo | Ya registrado en `P-051`; esta historia no lo empeora estructuralmente (un cuarto puerto con los mismos filtros) |
