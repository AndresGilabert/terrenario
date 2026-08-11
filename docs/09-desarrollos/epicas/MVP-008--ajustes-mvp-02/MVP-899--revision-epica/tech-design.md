---
id: "MVP-899"
tipo: feature
titulo: "TDD: Revision epica MVP-008"
estado: completado
tickets: []
epica: "MVP-008--ajustes-mvp-02"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "gobernanza"]
  modulo_path: "03-modulos/"
  componentes: ["revision", "verificacion"]
  etiquetas: ["mvp", "ajustes", "revision"]
  nivel_riesgo: medio
creado_en: "2026-08-11"
actualizado_en: "2026-08-11"
---

# TDD: MVP-899 — Revisión de cierre de la épica MVP-008

> **Referencia al spec**: [spec.md](./spec.md)

## Cómo se hizo esta revisión

Contra el **sistema en marcha**: API en el puerto 5127 contra la base de datos de desarrollo real, y
navegador conducido sobre el cliente. No es una relectura de la KB.

La épica dejó dos encargos propios y los dos se cumplen provocando, no leyendo: las **guardas nuevas**
de `MVP-809` y `MVP-810` se hicieron fallar a propósito y se aporta su salida literal.

Estado de partida: `develop` en `fd14488`, con las **once** historias funcionales dentro.

## Veredicto por criterio de aceptación de la épica

### CA-1 · Todas las historias en `completado`

**CUMPLE.** Las once historias funcionales figuran `completado` en el `_indice.md`, que se regenera con
el script y no a mano. `MVP-899` cierra la lista.

### CA-2 · El ámbito de temporada, coherente en las cuatro vistas

**CUMPLE.** Es el escenario que originó la épica: un `season_id` de otro Workspace en la URL.

Contra la API, el **mismo** identificador ajeno (`a8b10696…`, campaña de «Test 02») desde «Rafa»:

| Endpoint | `scope.season` |
|---|---|
| `GET /dashboard/summary` | Campana 2025 · `plots: 2` |
| `GET /diary` | Campana 2025 |
| `GET /harvests` | Campana 2025 |
| `GET /purchases` | Campana 2025 |

Los cuatro resuelven **el mismo ámbito**. Antes de `MVP-801`, el primero respondía `scope.season: null`
con todos los agregados a cero.

En el navegador, con ese mismo identificador en la dirección:

| Vista | Lo que rotula | Dirección después |
|---|---|---|
| Visión General | «Producción de Campana 2025, sobre 2 terrenos», **sin estado vacío** | `/app/vision-general` |
| Diario | Campana 2025 | `/app/diario` |
| Cosechas | Campana 2025 | `/app/cosechas` |
| Compras | Campana 2025 | `/app/compras` |

Ninguna afirma un ámbito distinto del que aplica, y las cuatro **corrigen la dirección**. Se comprobó
además con `plot_ids` ajenos: el ámbito cae en los 2 terrenos activos en vez de quedarse en `plots: 0`.

### CA-3 · Un enlace reproduce los filtros y recargar no los pierde

**CUMPLE.** Verificado en el navegador durante `MVP-802`: elegir «Campaña 2026» en Cosechas pasa la
tabla de **1 a 4 filas** —el escenario exacto de `P-109`— y la dirección lo recoge; abrir
`/app/cosechas?season_id=…&destination=aceite_para_venta` desde cero deja 1 fila con los dos controles
posicionados; `/app/compras?season_id=all&product=Abono` recarga con el buscador relleno.

La mecánica vive en **una sola pieza** (`lib/list-url-state.ts`) de la que el diario es un envoltorio.

### CA-4 · El gate de KB falla ante un requisito MVP sin destino

**CUMPLE, provocado.** Se añadió un `RU-99` marcado «Estado: MVP» y se ejecutó el validador. Dos
sondas, porque hay dos formas de no tener destino:

Sin fila en la matriz:

```text
❌ 1 error(es) encontrado(s):
  ERROR: …/definicion-requisitos-usuario.md: RU-99 no tiene fila en la matriz de trazabilidad.
  Todo requisito tiene que declarar donde se recoge y en que estado esta.
```

Con fila pero con la celda de destino vacía —la condición literal del criterio—:

```text
❌ 2 error(es) encontrado(s):
  ERROR: …: RU-99 esta 'en revisión' en la matriz, pero no nombra la historia que lo esta construyendo.
  ERROR: …: RU-99 esta marcado 'Estado: MVP' y no tiene destino declarado. Escribe en la matriz de
  trazabilidad la regla (RN-xxx), la historia (MVP-xxx), el punto del registro (P-xxx) o el ADR que lo
  recoge.
```

Retirada la sonda, el validador vuelve a `0 advertencia(s), 0 errores`.

### CA-5 · La primera carga baja de los 5,57 MB

**CUMPLE.** Medido con el instrumento del propio `build`:

| Recurso | Antes (`P-115`) | Después |
|---|---:|---:|
| Documento | 4,4 kB | 4,4 kB |
| JavaScript | 591,5 kB | 591,5 kB |
| CSS | 74,1 kB | 62,3 kB |
| **Iconos** | **3.776,0 kB** | **74,2 kB** |
| Tipografías de texto | 145,4 kB | 145,4 kB |
| **Primera carga** | **4.591,4 kB** | **881,1 kB** |
| **Total `dist/assets`** | **5.593,1 kB** | **1.227,2 kB** |

El **−80,8 %** de la primera carga sale de los iconos, que pesaban casi seis veces la aplicación entera.

El límite se provocó reintroduciendo **la regresión exacta que vigila** —volver a importar la fuente
completa— y el `build` falló:

```text
[plugin terrenario-presupuesto-primera-carga]
RolldownError: El peso de la primera carga se ha pasado del presupuesto (MVP-810):
  - Primera carga: 4657.6 kB supera el umbral de 1000.0 kB (sobran 3657.6 kB). Es lo que descarga
    alguien que abre la aplicación por primera vez, y el usuario objetivo trabaja con mala cobertura
    (RT-01, MVP-709).
  - Total de dist/assets: 5003.8 kB supera el umbral de 1400.0 kB (sobran 3603.8 kB). Aunque no todo se
    descargue en la primera visita, es lo que hay que publicar y mantener.
```

Restaurado, el `build` vuelve a pasar con 881,1 kB.

### CA-6 · Ningún punto con destino `MVP-008` sin historia, ninguna fila en `triado`

**CUMPLE.** Los **18** puntos con destino `MVP-008` están en `resuelto`, cada uno con su historia:

| Historia | Puntos |
|---|---|
| `MVP-801` | `P-107`, `P-108` |
| `MVP-802` | `P-109` |
| `MVP-803` | `P-095` |
| `MVP-804` | `P-113` |
| `MVP-805` | `P-110` |
| `MVP-806` | `P-036`, `P-041` |
| `MVP-807` | `P-048`, `P-049` |
| `MVP-808` | `P-011`, `P-029` |
| `MVP-809` | `P-112`, `P-114` |
| `MVP-810` | `P-115` |
| `MVP-811` | `P-116`, `P-117`, `P-118` |

Dos que la épica nombraba y **no** están en esa lista, con motivo:

- **`P-111`** sigue en `backlog-post-mvp`, y es correcto: la épica solo prometía **corregir el estado**
  de `RU-32`/`RU-33`/`RU-34`, no construir la planificación de tareas. Marcarlo resuelto diría que
  existe algo que no existe.
- **`P-069`** ya estaba `resuelto` desde el gate de `MVP-504`; `MVP-809` cerró su tarea documental
  residual.

## Contraste numérico

`MVP-799` dejó escrita la lección: con los datos que hay, un contraste puede pasar limpio sin tocar
nada. Se comprueba igual, porque **si hubiera cambiado sería un defecto grave**:

| Medida | KB (2026-08-10) | API hoy | Base de datos |
|---|---|---|---|
| Kilos de la campaña | 4.460,50 kg | 4.460,5 | **4.460,50** |
| Partidas | 4 | 4 | **4** |
| Aceite | 930,65 L | 930,65 | — |
| Rendimiento | 20,86 L/100kg | 20,86 | — |
| Kg por árbol | 17,16 | 17,16 | — |
| Gasto | 390 € | 390,0 | — |

Ninguna cifra se movió. Once historias tocando ámbito, filtros, maestros y lecturas no han alterado lo
que el producto suma.

## Comprobaciones puntuales sobre el sistema en marcha

| Qué | Resultado |
|---|---|
| `RN-037` en maestros, con uso | `422 BUSINESS_RULE_MASTER_IN_USE`: «No se puede eliminar el terreno «Matorral»: **1 actividad y 3 cosechas** lo referencian» |
| `RN-037` en maestros, sin uso | Terreno de prueba creado (`usage_count: 0`), borrado con `204`, **estado restaurado** |
| `RN-034`, abandonar siendo propietario único | `422 BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED` |
| `can_revoke` de la única miembro | `false` — coincide con la guarda |
| `RU-21`, autoría | «creado por: Andrés Gilabert · última edición: Andrés Gilabert» |
| `P-117`, 404 de enrutado | Los tres bordes con `RESOURCE_NOT_FOUND` y `application/json` |
| `P-116`, consola al entrar en `/app` | **Sin aviso de React** |
| `RN-044`, aviso de duplicado | «Ya hay una partida… **1000 kg, Aceite para venta**», conviviendo con el de `RN-023` |
| Iconos tras el subconjunto | 28 distintos en el diario, 22 en Terrenos, **ninguno cae a texto**; `FILL 1` sólido frente a `FILL 0` contorno |

## Suites

| Suite | Resultado |
|---|---|
| Backend | **1.051** en verde, `build -warnaserror` con **0 advertencias** |
| Cliente | **355** en verde |
| Gate de KB | `PIPELINE EXIT: 0` |

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Migraciones de base de datos preparadas — no aplica: la revisión no cambia esquema
- [x] Tests escritos y pasando — la revisión no añade cobertura propia; verifica la que hay
- [x] Documentación de API actualizada — no aplica
- [x] Módulo afectado actualizado en `docs/03-modulos/` — no aplica
- [x] Sin `TODO` sin resolver en este documento
