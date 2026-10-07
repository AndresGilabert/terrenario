---
bloque: "09-desarrollos"
documento: "brief-dashboard-campana"
actualizado_en: "2026-10-07"
landing:
  slug: "dashboard-campana"
  path: "/funcionalidades/dashboard-campana"
  cluster: "funcionalidad"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "dashboard de campaña agrícola"
  keywords_secundarias: ["indicadores de cosecha por terreno", "resumen de producción agrícola"]
  intencion: "comercial"
  canibaliza_con: ["gestion-de-temporadas"]
enlazado:
  entrantes: ["software-gestion-agricola", "gestion-terrenos", "control-cosechas"]
  salientes: ["control-cosechas", "compras-y-consumos", "gestion-terrenos"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Dashboard de campaña

## 1. Por qué existe

- **Problema**: al final de una temporada, los datos anotados no son fáciles de sumar y comparar sin rehacer cálculos.
- **Momento / persona**: evaluación y consulta; propietario de explotación que quiere una vista de resultados.
- **Misión de posicionamiento**: captar intención comercial sobre panel/resumen de campaña agrícola, haciendo visible el alcance real de los indicadores; la guía de temporadas cubrirá el «cómo».

## 2. Objetivo de posicionamiento

- **Objetivo**: ser la página funcional del dashboard y derivar a registro de cosechas/temporadas.
- **Métrica / horizonte**: consultas, impresiones, clics y sesiones propias; comparar a 90 días con baseline MKT-107 disponible, sin inventar umbrales de posición.
- **Rol**: apoyo de `software-gestion-agricola`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| dashboard de campaña agrícola | principal | comercial | H1/meta |
| indicadores de cosecha por terreno | secundaria | comercial | bullets/sección |
| resumen de producción agrícola | secundaria | comercial | intro/FAQ |

- **No atacar**: cómo crear temporadas (guía `/guias/gestion-de-temporadas`) ni software de gestión agrícola genérico.

## 4. Contenido

H1 actual «Dashboard de campaña: la foto completa de tu temporada». Desarrollar producción, rendimiento, kilos por terreno/destino, valor económico y estados incompletos. CTA: acceder. Extensión: 600–800 palabras.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Panel incluye widgets de producción, destino, terreno y evolución | `docs/03-modulos/produccion-y-dashboard/README.md`, Scope (RN-009) | [x] |
| Faltantes como número de árboles se excluyen/señalan, no se estiman | misma ficha, Conceptos clave (RN-010/011) | [x] |
| Valor económico lee costes manuales de actividad/compra | `docs/03-modulos/diario-y-operativa/README.md`, coste manual | [x] |

- **Excluir**: BI ad hoc, exportaciones, predicción o comparativas avanzadas fuera del histórico soportado.

## 6. Media

Sin media actual. Una captura de demo puede proponerse en la revisión; debe mostrar solo datos sintéticos.

## 7. Enlazado

Entrada desde el pilar y terrenos; salida a cosechas, compras y terrenos. La guía de temporada enlazará aquí cuando exista.

## 8. Metadatos

Title actual «Dashboard de campaña agrícola | Terrenario». No tiene objeto `seo` propio; revisar description social y FAQ sin reutilizar las de cosechas.

## 9. Distribución

Landing destino: `/funcionalidades/dashboard-campana`. Formato/canal/fecha derivados se registran en MKT-109.

## 10. Cierre

- [ ] Confirmar cada indicador en módulo; separar comercial de tutorial; revisar FAQ, meta y enlaces; tests/build verdes.
