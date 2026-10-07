---
bloque: "09-desarrollos"
documento: "brief-software-gestion-agricola"
actualizado_en: "2026-10-07"
landing:
  slug: "software-gestion-agricola"
  path: "/funcionalidades/software-gestion-agricola"
  cluster: "funcionalidad"
  estado: en-revision
  tipo_trabajo: "revision"
seo:
  keyword_principal: "software de gestión agrícola"
  keywords_secundarias:
    - "programa de gestión agrícola"
    - "software agrícola para pequeñas explotaciones"
    - "app para gestionar el campo"
  intencion: "comercial"
  canibaliza_con: ["control-cosechas"]
enlazado:
  entrantes: []
  salientes: ["gestion-terrenos", "diario-de-campo", "dashboard-campana"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-09-28"
---

# Brief de landing — Software de gestión agrícola

> Brief de revisión del pilar comercial. La misión/keywords están definidas; el trabajo pendiente
> es revisión editorial y verificación final antes de cerrar la revisión del 9–11 de octubre.

## 1. Por qué existe esta landing

- **Problema del usuario**: el agricultor que busca «un programa para llevar el campo» no sabe qué
  esperar de una herramienta de gestión agrícola ni si le encaja a su escala. Necesita entender el
  alcance completo del producto en una sola página.
- **Momento del recorrido**: descubrimiento. Es la puerta de entrada genérica al sitio.
- **Persona destino**: agricultor particular y responsable de explotación familiar
  (`docs/01-producto/personas.md`).
- **Qué pasa hoy sin esta landing**: se perdería la URL pilar que responde a quien busca una
  herramienta general para ordenar una pequeña explotación. La página ya tiene CTA y FAQ propios;
  el código actual no confirma el clon literal descrito en una auditoría anterior. Su principal
  mejora pendiente es sustituir la repetición de bullets en la sección `funcionalidades` por un mapa
  editorial de áreas reales.

## 2. Objetivo de posicionamiento

- **Misión de posicionamiento**: captar intención comercial genérica de comparación/elección de
  software de gestión agrícola para explotaciones pequeñas y servir como pilar del clúster; no
  competir por tutoriales ni por control de cosecha de olivar.
- **Objetivo principal**: consolidar esta URL como página pilar para «software de gestión agrícola».
- **Métrica de éxito**: impresiones, clics y posición media de consultas asignadas en Search Console;
  sesiones de entrada propias a esta ruta. MKT-107 aporta el baseline de indexación, no una promesa
  de posición; si el dato por landing no está disponible, registrarlo como pendiente.
- **Horizonte de evaluación**: 90 días desde la publicación de la revisión.
- **Rol en el clúster**: **pilar comercial**.
- **Landing pilar a la que refuerza**: no aplica — es el pilar.

## 3. Palabras clave

| Keyword | Tipo | Intención | Dónde se cubre |
| ------- | ---- | --------- | -------------- |
| software de gestión agrícola | principal | comercial | H1, intro, meta title |
| programa de gestión agrícola | secundaria | comercial | H2 del bloque de problema |
| software agrícola para pequeñas explotaciones | secundaria | comercial | Sección de beneficios |
| app para gestionar el campo | secundaria | comercial | FAQ |
| gestión de terrenos, diario de campo, campañas | semántica | — | Bloque de funcionalidades |

- **Consultas de cola larga que debe resolver el texto**: «qué es un software de gestión agrícola»,
  «programa para llevar las cuentas del campo», «cómo llevar el control de una finca pequeña».
- **Keywords que esta landing NO debe atacar**:
  - «control de cosecha de olivar» y «rendimiento de aceite» → pertenecen a `control-cosechas`.
  - «cómo configurar…», «paso a paso» → pertenecen al clúster de guías.

## 4. Contenido y estructura

- **H1 propuesto**: Software de gestión agrícola (se mantiene: es exacto y no hay motivo para
  moverlo estando ya indexado).
- **Esquema de H2**: problema de información dispersa; mapa de módulos reales; ventajas de consultar
  la explotación por campaña/terreno; preguntas frecuentes.
- **Propuesta de valor**: toda la explotación registrada en un solo sitio, campaña a campaña, sin
  convertir el trabajo de campo en trabajo administrativo.
- **CTA final actual**: «Centraliza y controla la gestión de tu olivar» / «Acceder a Terrenario».
  Mantener la propuesta propia, revisar el plural incorrecto «distintos fuentes» del texto CTA.
- **Extensión objetivo**: 900–1.100 palabras.

### Bloques previstos

| Bloque | Tipo | Contenido | Media asociada |
| ------ | ---- | --------- | -------------- |
| Problema | sección | 3 items, revisados y con tildes corregidas | — |
| Funcionalidades | sección | El array `items: []` activa fallback a los cuatro bullets del hero; sustituir la repetición por un mapa de módulos | Sin media asignada |
| Beneficios | sección | Copy propio; revisión de ortografía y afirmaciones, sin clon literal confirmado de cosechas | — |
| FAQ | FAQ | 3 preguntas propias en `LANDING_FAQS['software-gestion-agricola']`; verificar factualidad | — |

## 5. Verificación factual — cero promesas no soportadas

| Afirmación | Fuente | Verificada |
| ---------- | ------ | ---------- |
| Cada registro se vincula a un terreno | `docs/01-producto/vision-y-objetivos.md`, R1 | [x] |
| El diario reúne actividades, compras, consumos y cosechas | `docs/03-modulos/diario-y-operativa/README.md`, Qué es | [x] |
| Cosechas y panel se organizan por temporada y terreno | `docs/03-modulos/produccion-y-dashboard/README.md`, Scope | [x] |
| El usuario puede registrar temporadas anteriores | `landings.ts`, contenido actual; confirmar en flujo si se amplía esta afirmación | [ ] |
| Datos incompletos se señalan y no se inventan | `docs/03-modulos/produccion-y-dashboard/README.md`, Conceptos clave | [x] |

- **Funcionalidades excluidas del texto**: precio y molturación, balance de cosecha, inteligencia
  artificial, permisos granulares y analítica de terceros (`ADR-0011`). No existen hoy.

## 6. Media (imágenes y vídeo)

| Fichero en `public/` | Tipo | Qué muestra | Texto alternativo | Peso | Datos |
| -------------------- | ---- | ----------- | ----------------- | ---- | ----- |
| — | — | No hay media propia asignada a esta landing | — | — | — |

- [x] No referenciar asset inexistente.
- [ ] Si se añade captura futura: datos sintéticos, `alt`, dimensiones y presupuesto del [tech-design](../tech-design.md#peso-y-presupuesto).
- [x] No hay vídeo implementado en el clúster actual.

## 7. Enlazado interno

- **Entrantes**: `/guias/como-empezar-en-terrenario` la enlaza como pilar. La portada incluye
  también el enlace de navegación general; no equivale a relación contextual.
- **Salientes**: `gestion-terrenos`, `diario-de-campo`, `dashboard-campana` — el recorrido real de
  alta y uso, no un enlazado arbitrario.
- **Huérfana**: [ ] no; recibe enlace contextual desde la guía y enlace global desde la portada.

## 8. Datos estructurados y metadatos

- **Meta title**: Software de gestión agrícola | Terrenario (48 car., se mantiene).
- **Meta description actual**: «Gestiona tus bancales con el software de gestión agrícola de
  Terrenario.» Revisar longitud y claridad en la edición; no declarar una meta description ausente.
- **Open Graph / Twitter**: ya definidos; revisar que la descripción no repita literalmente la de
  `control-cosechas`.
- **FAQ para `FAQPage`**: existen 3 preguntas propias; revisar gramática y que respuesta de inicio
  no prometa duración de onboarding no garantizada.

## 9. Distribución

| Pieza | Canal | Formato | Fecha | Landing destino |
| ----- | ----- | ------- | ----- | --------------- |
| Revisión del pilar y mapa de módulos | Web: landing propia; derivación social en MKT-109 | actualización de landing | 9–11 oct | `software-gestion-agricola` |

## 10. Checklist de cierre

- [ ] Brief aprobado antes de tocar `src/content/landings.ts`.
- [ ] Sección 5 completa y verificada.
- [ ] Un único `H1`.
- [ ] Meta description reescrita dentro de longitud.
- [x] FAQ y CTA propios en el catálogo actual.
- [ ] Sección `funcionalidades` deja de repetir los bullets por fallback.
- [x] CTA final propio; corregir error de concordancia.
- [ ] Tildes corregidas: permitirá, controlarás, ordenarás.
- [x] Enlace contextual entrante desde la guía de inicio.
- [ ] `npm test` y `npm run build` en verde.
