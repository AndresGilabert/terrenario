# AGENTS.md — Reglas para la carpeta docs/

> Estas reglas aplican a **todos** los archivos `.md` bajo `docs/`.
> Complementan las reglas globales del `AGENTS.md` raíz — léelo primero.

---

## Reglas de codificación y formato para archivos Markdown

Estas reglas aplican a **TODOS** los archivos `.md` sin excepción.

### Encoding — UTF-8

- **TODOS** los archivos `.md` se guardan en **UTF-8**. El BOM es indiferente: da igual escribirlos con
  él o sin él, y **no hay que añadirlo ni quitarlo**.

> **Por qué ya no se exige el BOM** (`P-097`, retirado en la revisión de `MVP-007`). La regla anterior
> lo hacía obligatorio, pero nada lo comprobaba: al medirlo, **74 de los 210 `.md` de `docs/` no lo
> llevaban** y siete épicas habían pasado sin que se rompiera nada. Los scripts de la KB leen con
> `utf-8-sig`, que acepta las dos formas, y ninguna otra herramienta del flujo depende de él.
>
> Mantenerla tenía además un coste visible: al editar uno de los 74 se le aplicaba el BOM y su diff
> incluía un cambio en el byte 1 que no era de contenido y confundía en la revisión. Pasó en `MVP-712`.
>
> No se normalizaron los 74 ficheros: habría sido un commit tocando un tercio del árbol sin cambiar
> una sola palabra. Si alguna vez aparece una herramienta que sí necesite el BOM, la decisión se
> reabre — pero entonces con una comprobación que la respalde, no con una nota que nadie lee.

---

## Frontmatter YAML obligatorio

Cada `.md` debe incluir frontmatter según su tipo de documento:

### Documentos de KB estándar (`docs/` secciones 00–08, 99)

```yaml
---
bloque: "{id-seccion}"        # ej. 01-producto, 02-arquitectura
documento: "{nombre-archivo}" # slug sin extensión
actualizado_en: "YYYY-MM-DD"
---
```

### Feature / Historia spec (`docs/09-desarrollos/epicas/{EPIC}/{TICKET}/spec.md`)

```yaml
---
id: ""
tipo: feature                  # feature | bugfix | mejora | spike | tarea
titulo: ""
estado: borrador               # borrador | en-revision | aprobado | en-progreso | en-testing | completado | cancelado
prioridad: media               # critica | alta | media | baja
sprint: ""
hito: ""
esfuerzo_estimado: ""
tickets: []
epica: ""
depende_de: []
bloquea: []
relacionado_con: []
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

### Épica (`docs/09-desarrollos/epicas/{EPIC}/spec.md`)

```yaml
---
id: ""
tipo: epica
titulo: ""
estado: borrador
prioridad: media
hito: ""
tickets: []
historias: []
depende_de: []
bloquea: []
relacionado_con: []
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

### Tech Design (`docs/09-desarrollos/epicas/{EPIC}/{TICKET}/tech-design.md`)

```yaml
---
id: ""
tipo: feature
titulo: "TDD: {Título}"
estado: borrador
tickets: []
epica: ""
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

### ADR (`docs/02-arquitectura/decisiones/` o `docs/03-modulos/{modulo}/decisiones/`)

```yaml
---
id: "ADR-XXXX"
titulo: ""
estado: propuesta              # propuesta | aceptada | rechazada | obsoleta | supersedida-por:ADR-XXXX
fecha: "YYYY-MM-DD"
decisores: []
etiquetas: []
---
```

### Release (`docs/10-releases/v{MAJOR}.{MINOR}.{PATCH}.md`)

```yaml
---
id: "vX.Y.Z"
tipo: release
titulo: "Release vX.Y.Z"
estado: borrador
fecha: "YYYY-MM-DD"
hito: ""
responsable: ""
revisores: []
tickets: []
creado_en: "YYYY-MM-DD"
actualizado_en: "YYYY-MM-DD"
---
```

---

## Campos de fecha

- Al **crear**: establecer `creado_en` y `actualizado_en` (o `fecha` en ADRs) con la fecha actual (`YYYY-MM-DD`).
- Al **modificar**: actualizar siempre `actualizado_en` (o `fecha`) con la fecha actual.
- Nunca dejar campos de fecha vacíos (`""`) en un archivo que se escribe o revisa.

---

## Lista de verificación antes de completar una tarea

1. Frontmatter correcto aplicado según el tipo de documento.
2. Campo `actualizado_en` (o `fecha`) actualizado a la fecha de hoy.
3. Ningún `.md` creado sin frontmatter.

---

## Reglas obligatorias para altas en 09-desarrollos

1. El nombre de carpeta de épica e historia se genera automáticamente desde el título del desarrollo o, si existe, desde el ticket fuente.
2. No pedir ni introducir slugs manuales como parte del proceso estándar.
3. Las referencias externas en `tickets` son opcionales y pueden apuntar a distintos sistemas.
4. Si existe ticket fuente, debe documentarse su trazabilidad real en el documento; no se inventa contenido ausente.
5. Las carpetas y archivos deben respetar los límites de longitud definidos en `docs/00-meta/convenciones.md`.
6. El cumplimiento de estas reglas se valida por script y CI.
