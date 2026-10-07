---
bloque: "09-desarrollos"
documento: "brief-{slug-de-la-landing}"
actualizado_en: ""
landing:
  slug: ""                     # p. ej. gestion-de-temporadas
  path: ""                     # p. ej. /guias/gestion-de-temporadas
  cluster: ""                  # funcionalidad | perfil | guia
  estado: borrador             # borrador | en-revision | publicada | obsoleta
  tipo_trabajo: ""             # nueva | revision
seo:
  keyword_principal: ""
  keywords_secundarias: []
  intencion: ""                # informacional | comercial | transaccional | navegacional
  canibaliza_con: []           # slugs de landings que compiten por la misma intención
enlazado:
  entrantes: []                # slugs que apuntan a esta landing
  salientes: []                # slugs a los que apunta (relatedSlugs)
media: []                      # rutas en public/ usadas por la landing
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: ""
revisores: []
creado_en: ""
---

# Brief de landing — {Título de la landing}

> **Plantilla obligatoria.** Ninguna landing se crea ni se revisa sin este brief relleno y
> aprobado. Sin brief, la landing vuelve a ser contenido autogenerado sin criterio, que es
> exactamente el problema que `MKT-108` corrige.
>
> Ubicación del brief relleno:
> `docs/09-desarrollos/epicas/MKT-100--posicionamiento-organico-inicial/MKT-108--activos-organicos-y-calendario-editorial-30-dias/briefs/{slug}.md`

## 1. Por qué existe esta landing

> Qué pregunta o problema real del usuario resuelve esta página **y ninguna otra del sitio**.
> Si la respuesta ya la da otra landing publicada, esta página no debe existir: amplía la otra.

- **Problema del usuario**:
- **Momento del recorrido** (descubrimiento / evaluación / uso del producto):
- **Persona destino** (ver `docs/01-producto/personas.md`):
- **Qué pasa hoy sin esta landing**:

## 2. Objetivo de posicionamiento

- **Objetivo principal** (ej.: posicionar la keyword X en top 10 / capturar la consulta larga Y /
  dar soporte de autoridad al pilar Z):
- **Métrica de éxito** (ver `docs/01-producto/kpis.md`):
- **Horizonte de evaluación**:
- **Rol en el clúster**: pilar / apoyo / guía de soporte
- **Landing pilar a la que refuerza** (si es de apoyo):

## 3. Palabras clave

| Keyword | Tipo | Intención | Dónde se cubre (H1 / H2 / intro / FAQ) |
| ------- | ---- | --------- | -------------------------------------- |
|         | principal / secundaria / semántica |  |  |

- **Consultas de cola larga que debe resolver el texto**:
- **Keywords que esta landing NO debe atacar** (y qué landing las tiene asignadas):

> Regla anticanibalización: una keyword principal pertenece a **una sola** landing. Si dos páginas
> la reclaman, se decide aquí cuál la mantiene y la otra la cita sin optimizarla.

## 4. Contenido y estructura

- **H1 propuesto**:
- **Esquema de H2** (uno por bloque de contenido):
- **Propuesta de valor en una frase**:
- **CTA final** (título, texto y etiqueta):
- **Extensión objetivo** (palabras):

### Bloques previstos

| Bloque | Tipo | Contenido | Media asociada |
| ------ | ---- | --------- | -------------- |
|        | bullets / sección / pasos / FAQ |  |  |

## 5. Verificación factual — cero promesas no soportadas

> `CA-3` de `MKT-108`: ninguna afirmación puede describir una funcionalidad que el producto no
> tiene hoy. Cada afirmación relevante se contrasta contra la documentación del módulo.

| Afirmación de la landing | Fuente que la sostiene (ruta + sección) | Verificada |
| ------------------------ | --------------------------------------- | ---------- |
|                          | `docs/03-modulos/{modulo}/README.md#...` | [ ]        |

- **Funcionalidades explícitamente excluidas del texto** (no existen hoy):

## 6. Media (imágenes y vídeo)

> Restricciones vinculantes: sin recursos de terceros (`RN-042`), todo autoalojado en `public/`,
> sin datos reales (`docs/07-seguridad/privacidad-datos.md`). Ver el `tech-design.md` de `MKT-108`.

| Fichero en `public/` | Tipo | Qué muestra | Texto alternativo | Peso | Datos usados |
| -------------------- | ---- | ----------- | ----------------- | ---- | ------------ |
|                      | imagen / vídeo |  |  |  | demo / sintéticos |

- [ ] Todas las capturas usan datos de demostración, sin nombres, correos ni cifras reales.
- [ ] Cada imagen declara `alt` descriptivo, `width` y `height`.
- [ ] Cada vídeo es autoalojado, sin audio necesario para entenderlo y con póster propio.
- [ ] El peso total de la media de esta landing está dentro del presupuesto acordado.

## 7. Enlazado interno

- **Entrantes** (desde qué landings y con qué anchor):
- **Salientes** (`relatedSlugs`, y por qué esa relación es operativa y no arbitraria):
- **Huérfana**: [ ] no / [ ] sí — si sí, cómo se resuelve

## 8. Datos estructurados y metadatos

- **Meta title** (≤ 60 car.):
- **Meta description** (≤ 155 car.):
- **Open Graph / Twitter** (título, descripción, imagen y `imageAlt`):
- **FAQ para `FAQPage`** (mínimo 3, propias de esta landing y sin reutilizar las de otra):

## 9. Distribución

- **Piezas orgánicas derivadas** (canal, formato y fecha — se alinean con `MKT-109`):
- **Landing destino de cada pieza**:

## 10. Checklist de cierre

- [ ] Brief aprobado antes de tocar `src/content/landings.ts`.
- [ ] Contenido verificado contra la documentación del módulo (sección 5 completa).
- [ ] Un único `H1`; jerarquía de encabezados correcta.
- [ ] Meta title y description dentro de longitud, sin duplicar los de otra landing.
- [ ] FAQ propias, no reutilizadas de otro slug.
- [ ] Enlazado interno entrante y saliente resuelto.
- [ ] Media verificada: autoalojada, con `alt`, sin datos reales y dentro de presupuesto.
- [ ] Ortografía y tildes revisadas.
- [ ] `npm test` y `npm run build` en verde.
- [ ] Landing incluida en el `sitemap.xml` generado.
