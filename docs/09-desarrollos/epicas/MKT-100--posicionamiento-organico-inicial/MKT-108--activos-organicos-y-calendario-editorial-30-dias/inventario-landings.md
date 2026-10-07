---
bloque: "09-desarrollos"
documento: "inventario-landings"
actualizado_en: "2026-10-07"
---

# MKT-108 — Inventario y auditoría de landings públicas

> Fuente auditada: [landings.ts](../../../../../src/frontend/terrenario-web/src/content/landings.ts)
> (12 landings + `HOME_META`), revisada contra el catálogo actual el 2026-10-07.
>
> Las keywords de los briefs son hipótesis de asignación por intención y copy actual, no datos de
> volumen ni posiciones observadas. Validarlas con Search Console antes de fijar objetivos de ranking.

## 1. Cómo se organizan hoy

Las landings son **contenido tipado** en un único fichero TypeScript. No hay CMS ni Markdown: cada
página es un objeto `LandingContent` que el script
[prerenderizar-landings.mjs](../../../../../src/frontend/terrenario-web/scripts/prerenderizar-landings.mjs)
convierte en HTML estático (`dist/{cluster}/{slug}/index.html`) mediante `renderToStaticMarkup`,
sin bundle de JavaScript (`ADR-0012`). El mismo script genera `robots.txt` y `sitemap.xml`, así que
**dar de alta una landing es añadir un objeto al array**: el resto se propaga solo.

Tres clústeres, marcados por el campo `cluster`:

| Clúster | Ruta | Landings | Intención |
| ------- | ---- | -------- | --------- |
| `funcionalidad` | `/funcionalidades/{slug}` | 8 | Comercial: qué hace el producto |
| `perfil` | `/para/{slug}` | 3 | Comercial: para quién es el producto |
| `guia` | `/guias/{slug}` | 1 | Informacional: cómo empezar y utilizar la plataforma |

Inventario completo:

| # | Slug | Clúster | Estructura | `seo` propio | FAQ | Brief |
| - | ---- | ------- | ---------- | ------------ | --- | ----- |
| 1 | `software-gestion-agricola` | funcionalidad | bullets + 3 secciones + CTA | sí | 3 propias | [en revisión](./briefs/software-gestion-agricola.md) |
| 2 | `gestion-terrenos` | funcionalidad | bullets | no | 2 | [relleno](./briefs/brief-gestion-terrenos.md) |
| 3 | `diario-de-campo` | funcionalidad | bullets | no | 2 | [relleno](./briefs/brief-diario-de-campo.md) |
| 4 | `control-cosechas` | funcionalidad | bullets + 3 secciones + CTA | sí | 3 propias | [relleno](./briefs/brief-control-cosechas.md) |
| 5 | `compras-y-consumos` | funcionalidad | bullets | no | 2 | [relleno](./briefs/brief-compras-y-consumos.md) |
| 6 | `dashboard-campana` | funcionalidad | bullets | no | 2 | [relleno](./briefs/brief-dashboard-campana.md) |
| 7 | `workspaces-colaboracion` | funcionalidad | bullets | no | 2 | [relleno](./briefs/brief-workspaces-colaboracion.md) |
| 8 | `trabajadores-y-tareas` | funcionalidad | bullets | no | 2 | [relleno](./briefs/brief-trabajadores-y-tareas.md) |
| 9 | `agricultor-particular` | perfil | bullets | no | 2 | [relleno](./briefs/brief-agricultor-particular.md) |
| 10 | `explotacion-familiar` | perfil | bullets | no | 2 | [relleno](./briefs/brief-explotacion-familiar.md) |
| 11 | `gestion-multiterreno` | perfil | bullets | no | 2 | [relleno](./briefs/brief-gestion-multiterreno.md) |
| 12 | `como-empezar-en-terrenario` | guia | 6 pasos, 5 capturas | no | 3 | [en revisión](./briefs/brief-como-empezar-en-terrenario.md) |

## 2. Qué falla — hallazgos verificados

Los hallazgos siguientes están contrastados contra el código, no estimados.

### H-1 · `software-gestion-agricola` repite el hero en la sección de módulos (pendiente)

La revisión del 2026-10-07 del código actual **no confirma** el clon descrito en la auditoría
anterior: su `finalCta` y sus FAQ son propios y no reutiliza los de `control-cosechas`. Ese hallazgo
quedó obsoleto y se retira. El solapamiento real pendiente es editorial:

- ambas páginas hablan de cosechas, pero con misiones distintas: visión general de producto frente
  a registro/rendimiento de cosecha de olivar;
- la sección `funcionalidades` de la landing general tiene `items: []`; el componente sustituye
  este array vacío por `content.bullets`, así que renderiza contenido, pero repite los cuatro bullets
  del hero en lugar de ofrecer una explicación editorial distinta.

Acción: reescribir el bloque como mapa de módulos, verificar afirmaciones contra sus fichas y
conservar FAQ/CTA propios. No etiquetar como duplicado literal mientras una nueva comparación no
lo demuestre.

### H-2 · La sección `funcionalidades` repite los bullets por fallback

En `software-gestion-agricola` y `control-cosechas`, el bloque `funcionalidades` tiene `items: []`.
`ContentLandingPage.tsx` usa entonces `content.bullets`; no es una sección vacía, sino una
repetición del contenido destacado. Revisar si el bloque aporta explicación adicional o si debe
eliminarse; no describirlo como contenido ausente.

### H-3 · Errores ortográficos y gramaticales en la landing cabecera

En `software-gestion-agricola`: «permitira», «controlaras», «ordenaras» (faltan tildes). Está en la
landing cabecera. También aparecen «movíl», «disponble», «Con sulta» y «distintos fuentes» en
beneficios/CTA. Revisar y corregir en la revisión editorial, sin afirmar que el tráfico ya exista.

### H-4 · Contenido fino en 9 de las 11 landings comerciales heredadas

Nueve de las once landings comerciales heredadas son principalmente `intro` + 3 bullets, sin
secciones editoriales ni CTA final y con 2 FAQ. La nueva guía se excluye de este recuento. El brief
debe definir qué explicación adicional sirve a la intención, no añadir texto de relleno.

### H-5 · Metadatos sociales incompletos

Solo 2 de las 12 landings actuales definen `seo` (Open Graph / Twitter / descripción estructurada).
Las demás dependen de metadatos sociales derivados por defecto. Cada revisión decidirá metadatos
únicos cuando exista una imagen y un copy realmente propios.

### H-6 · Landings sin relaciones contextuales entrantes

En el grafo `relatedSlugs`, `agricultor-particular` y `explotacion-familiar` no reciben enlaces
contextuales y `software-gestion-agricola` solo recibe uno desde la nueva guía. No son páginas
huérfanas del sitio: la portada enlaza a todas las landings del catálogo. Acción: añadir relaciones
contextuales pertinentes al revisar los briefs, sin alterar el hub de portada.

### H-7 · Soporte actual de imágenes; vídeo no implementado

La guía de inicio ya admite imágenes en sus pasos y usa cinco capturas autoalojadas. El tipo
compartido solo tiene imagen (`src`, `alt`, `width`, `height`); no se ha implementado un bloque de
vídeo. Las otras landings siguen mayoritariamente sin capturas.

### H-8 · Clúster informacional iniciado; faltan tres guías previstas

El clúster `/guias/` ya incluye `como-empezar-en-terrenario`. Faltan las guías planificadas para
invitar y gestionar Workspaces, gestionar temporadas, y registrar cosechas. La guía de inicio ya
cubre configuración inicial; crear además `/guias/configuracion-inicial` competiría por la misma
intención y no se planifica como URL separada.

## 3. Arquitectura de contenido actual y evolución planificada

El catálogo ya utiliza un tercer clúster, **informacional**, con ruta propia para separar su
intención de la comercial y evitar canibalización:

| Clúster | Ruta | Intención | Formato |
| ------- | ---- | --------- | ------- |
| `funcionalidad` | `/funcionalidades/{slug}` | Comercial | Beneficio y capacidad |
| `perfil` | `/para/{slug}` | Comercial | Caso de uso por persona |
| **`guia`** | **`/guias/{slug}`** | **Informacional** | **Manual paso a paso; imágenes autoalojadas** |

### Landings nuevas del clúster `guia`

| Slug | Ruta | Keyword principal | Pilar que refuerza | Brief |
| ---- | ---- | ----------------- | ------------------ | ----- |
| `como-empezar-en-terrenario` | `/guias/como-empezar-en-terrenario` | cómo empezar en Terrenario / primeros pasos | `software-gestion-agricola` (implementada; despliegue productivo por confirmar) | [relleno](./briefs/brief-como-empezar-en-terrenario.md) |
| `gestion-de-workspaces` | `/guias/gestion-de-workspaces` | cómo invitar a personas a un Workspace agrícola | `workspaces-colaboracion` (planificada) | [borrador](./briefs/brief-gestion-de-workspaces.md) |
| `gestion-de-temporadas` | `/guias/gestion-de-temporadas` | cómo crear y gestionar temporadas agrícolas | `dashboard-campana` (planificada) | [borrador](./briefs/brief-gestion-de-temporadas.md) |
| `gestion-de-cosechas` | `/guias/gestion-de-cosechas` | cómo registrar una cosecha de aceituna | `control-cosechas` (planificada) | [borrador](./briefs/brief-gestion-de-cosechas.md) |

> **Nota de nomenclatura.** El usuario pidió «grupos de trabajo o workgroups»; el producto llama a
> ese concepto **Workspace** y así aparece en la interfaz y en la documentación de módulos. La guía
> usa «Workspace» como término canónico y menciona «grupo de trabajo» solo como sinónimo dentro del
> texto, para captar la consulta sin inventar un término que el producto no usa.

### Regla anticanibalización guía ↔ funcionalidad

Cada guía y su funcionalidad se emparejan y se revisan **en la misma semana** del calendario, con
un reparto explícito de intención:

- La landing de **funcionalidad** responde *qué permite hacer* y por qué merece la pena. Keyword
  comercial.
- La landing de **guía** responde *cómo se hace, paso a paso, con capturas*. Keyword informacional
  de cola larga (`cómo…`, `paso a paso`, `tutorial`).
- Se enlazan contextualmente en ambas direcciones cuando ambas páginas estén implementadas. La
  portada ya enlaza a todas las landings del catálogo; los briefs no etiquetarán como huérfana una
  URL que recibe ese enlace global.

### Verificación factual pendiente

El contenido de las tres guías pendientes debe contrastarse, antes de redactarse, contra las fichas de
[catálogo de módulos](../../../../03-modulos/_vision-general.md) —`identidad-y-workspaces`,
`maestros-operativos`, `diario-y-operativa`, `produccion-y-dashboard`— y contra
[reglas de negocio](../../../../01-producto/reglas-de-negocio.md). Los pasos concretos
de cada flujo **no se dan por conocidos**: se comprueban en el producto y se registran en la sección
5 del brief correspondiente.

## 4. Plan de corrección por landing

| Landing | Acción | Prioridad | Fecha planificada |
| ------- | ------ | --------- | ----------------- |
| `como-empezar-en-terrenario` | Completar brief, verificar capturas anonimizadas y confirmar despliegue | guía de inicio | 7–8 oct (implementación en código ya hecha) |
| `software-gestion-agricola` | Corregir copy, diferenciar mapa de módulos y cerrar misión comercial | pilar | 9–11 oct |
| `gestion-de-workspaces` | Nueva guía de invitaciones y colaboración | informacional | 12–15 oct |
| `workspaces-colaboracion` | Revisión comercial y enlace contextual recíproco | apoyo | 16–18 oct |
| `gestion-de-temporadas` | Nueva guía de temporadas, fechas y temporada de trabajo | informacional | 19–22 oct |
| `dashboard-campana` | Revisión del alcance real de indicadores y enlace a guía | apoyo | 23–25 oct |
| `gestion-de-cosechas` | Nueva guía de captura de cosecha y rendimiento | informacional | 26–29 oct |
| `control-cosechas` | Revisión factual/editorial y enlace a guía | apoyo | 30 oct–1 nov |
| `gestion-terrenos` | Revisión de ficha y alta mínima | apoyo | 2–3 nov |
| `diario-de-campo` | Revisión del eje cronológico y límites | apoyo | 4–5 nov |
| Otras 5 landings comerciales actuales | Compras, trabajadores y tres perfiles | siguiente ciclo | fechas por acordar |

El detalle de fechas está en [calendario-editorial-octubre.md](./calendario-editorial-octubre.md).
