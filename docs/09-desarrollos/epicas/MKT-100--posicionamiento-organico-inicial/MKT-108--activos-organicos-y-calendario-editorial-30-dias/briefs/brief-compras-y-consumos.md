---
bloque: "09-desarrollos"
documento: "brief-compras-y-consumos"
actualizado_en: "2026-10-07"
landing:
  slug: "compras-y-consumos"
  path: "/funcionalidades/compras-y-consumos"
  cluster: "funcionalidad"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "control de compras y consumos agrícolas"
  keywords_secundarias: ["registrar gastos de materiales agrícolas", "repartir consumo por parcela"]
  intencion: "comercial"
  canibaliza_con: []
enlazado:
  entrantes: ["diario-de-campo", "dashboard-campana"]
  salientes: ["diario-de-campo", "dashboard-campana"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Compras y consumos

## 1. Por qué existe

- **Problema**: quien compra insumos para varias parcelas necesita recordar qué adquirió, dónde se consumió y qué coste asignó.
- **Momento / persona**: evaluación; propietario que lleva gastos por terreno.
- **Misión de posicionamiento**: atraer intención comercial de control de compras/consumos por parcela, no posicionar inventario ni stock.

## 2. Objetivo de posicionamiento

- **Objetivo**: explicar el flujo de compra e imputación para evaluar su adecuación a una explotación pequeña.
- **Métrica / horizonte**: consultas, impresiones y clics en Search Console + sesiones propias a 90 días; baseline por landing pendiente.
- **Rol**: apoyo del pilar `software-gestion-agricola`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| control de compras y consumos agrícolas | principal | comercial | H1/meta |
| registrar gastos de materiales agrícolas | secundaria | comercial | intro |
| repartir consumo por parcela | secundaria | comercial | sección/FAQ |

- **No atacar**: inventario de existencias/stock (no disponible) ni tutorial paso a paso.

## 4. Contenido

H1 actual: «Compras y consumos: qué compraste, dónde se usó y cuánto costó». Estructura: compra, imputación por terreno, consumo sin compra previa y límites del coste histórico. CTA: acceso. Extensión 500–700 palabras.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Compra almacena producto, cantidad y coste total | `docs/03-modulos/diario-y-operativa/README.md`, Scope (RN-031) | [x] |
| Consumo se imputa a terrenos y puede preceder a compra, con aviso y sin recálculo histórico | misma ficha (RN-032/043) | [x] |

- **Excluir**: stock tracking, alertas de reposición y conciliación económica automática.

## 6. Media

Ninguna asociada en el catálogo actual. No prometer evidencia visual hasta tener captura sintética.

## 7. Enlazado

Enlaces desde diario y hacia diario/dashboard por el uso operativo y el agregado económico. Sin guía dedicada planificada en este ciclo.

## 8. Metadatos

Title actual «Compras y consumos agrícolas | Terrenario». Revisar description para evitar promesa de inventario; FAQ propias. OG/social: crear solo si hay material propio.

## 9. Distribución

Landing web y pieza derivada en MKT-109; canal y fecha deben constar allí. Destino: `/funcionalidades/compras-y-consumos`.

## 10. Cierre

- [ ] Copy limitado a compras/imputaciones disponibles; metadatos/enlaces revisados; tests/build verdes.
