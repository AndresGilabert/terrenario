# Convenciones de la Base de Conocimiento

---

## Naming de archivos y carpetas

| Elemento            | Patrón                         | Ejemplo                              |
| ------------------- | ------------------------------ | ------------------------------------ |
| Carpeta de épica    | `{ID}--{epica-slug}`           | `PROJ-100--mi-epica`                 |
| Carpeta de historia | `{ID}--{historia-slug}`        | `PROJ-123--flujo-principal`          |
| ADR                 | `ADR-{XXXX}--{titulo-slug}.md` | `ADR-0001--titulo-slug.md`           |
| Runbook             | `RB-{XXX}--{titulo-slug}.md`   | `RB-001--titulo-slug.md`             |
| Release             | `v{MAJOR}.{MINOR}.{PATCH}.md`  | `v2.1.0.md`                          |
| Módulo              | `{nombre-en-kebab-case}/`      | `provider-connectors/`               |
| Integración         | `{nombre-en-kebab-case}/`      | `sistema-externo/`                   |

- Usar siempre **kebab-case** para nombres de archivos y carpetas
- Sin espacios, sin mayúsculas (excepto el prefijo del identificador: `PROJ-`, `ADR-`, `RB-`)
- Usar `--` (doble guión) como separador entre ID y slug

## Longitud máxima de nombres y rutas

Para evitar problemas en Windows, Git y herramientas de CI:

1. El nombre completo de cada carpeta de épica o historia no debe superar **64 caracteres**.
2. La ruta relativa al repo de cualquier archivo bajo `docs/` no debe superar **180 caracteres**.
3. La ruta absoluta del archivo en el entorno local no debe superar **220 caracteres**.
4. Si el título original es demasiado largo, el slug debe recortarse antes de crear la carpeta.

Presupuesto recomendado para `docs/09-desarrollos/`:

- Mantener el slug de épicas e historias en **48 caracteres o menos** siempre que sea posible.
- Si el identificador es especialmente largo, reducir más el slug para seguir cumpliendo el límite de 64 caracteres.
- Preferir títulos cortos y semánticos sobre copiar títulos extensos del sistema de tickets.

## Derivación automática de slug desde el título del desarrollo

El slug de carpeta para épicas e historias se deriva automáticamente del título del desarrollo en la KB.
Si existe un ticket externo, puede usarse su título como fuente inicial, pero no es obligatorio.
No se define manualmente por el desarrollador, salvo excepción aprobada por el PO y el líder técnico.

Reglas de normalización del slug:

1. Convertir a minúsculas.
2. Eliminar tildes y diacríticos.
3. Sustituir cualquier carácter no alfanumérico por guion.
4. Colapsar múltiples guiones consecutivos en uno solo.
5. Eliminar guiones al inicio y al final.
6. Limitar la longitud del slug para que la carpeta final no supere 64 caracteres.
7. Si el resultado queda vacío, usar slug `sin-titulo`.

Formato final de carpetas:

- Épica: `ID--slug-derivado`
- Historia: `ID--slug-derivado`

Ejemplo:

- Título fuente: Revisión de la propuesta técnica, definición de artefactos técnicos y planteamiento técnico a bajo nivel
- Slug: `revision-de-la-propuesta-tecnica-definicion-de-artefactos-tecnicos-y-planteamiento`
- Carpeta historia: `INT-423--revision-de-la-propuesta-tecnica-definicion-de-artefactos-tecnicos-y-planteamiento`

Criterio de gobernanza:

- La fuente de verdad para naming es esta convención y su implementación en el script de validación.
- No depender de prompts ad hoc.
- El campo `id` identifica el desarrollo dentro de la KB. Puede coincidir con un ticket externo, pero no es obligatorio.

---

## Frontmatter YAML obligatorio

### Todos los documentos de desarrollos (`09-desarrollos/`)

```yaml
---
# ─── IDENTIFICACIÓN (todos obligatorios) ────────────
id: "PROJ-123"
tipo: feature             # feature | bugfix | mejora | spike | tarea | epica
titulo: "Título descriptivo en lenguaje natural"
estado: borrador          # borrador | en-revision | aprobado | en-progreso | en-testing | completado | cancelado

# ─── PLANIFICACIÓN (obligatorios en historias, opcionales en épicas) ───
prioridad: media          # critica | alta | media | baja
sprint: ""                # formato: "YYYY-QN-SN" → ej: "2025-Q2-S3"
hito: ""                  # versión semántica → ej: "v2.1.0"
esfuerzo_estimado: ""     # formato libre → ej: "3d", "1w", "13 puntos"

# ─── TICKETS / REFERENCIAS EXTERNAS (opcional) ────────────────────────
tickets: []               # opcional. Ejemplo:
# - sistema: jira
#   id: PROJ-123
#   url: https://jira.example.com/browse/PROJ-123
# - sistema: github
#   id: owner/repo#456
#   url: https://github.com/owner/repo/issues/456

# ─── RELACIONES ───────────────────────────────────────────────────────
epica: ""                 # ID--slug de la épica padre (obligatorio en historias)
historias: []             # lista de IDs de historias (solo en épicas)
depende_de: []            # IDs que bloquean este trabajo
bloquea: []               # IDs que este trabajo bloquea
relacionado_con: []       # referencias sin dependencia dura

# ─── EQUIPO ───────────────────────────────────────────────────────────
responsable: ""           # @usuario (obligatorio)
revisores: []             # [@usuario1, @usuario2]

# ─── CONTEXTO PARA AI AGENTS / RAG (todos obligatorios) ──────────────
ai_context:
  dominios: []            # dominios de negocio → ej: ["operaciones", "reporting"]
  modulo_path: ""         # ruta al módulo → ej: "03-modulos/modulo-a"
  componentes: []         # servicios afectados → ej: ["modulo-a-service"]
  etiquetas: []           # tags libres → ej: ["api", "breaking-change"]
  nivel_riesgo: bajo      # bajo | medio | alto | critico

# ─── AUDITORÍA ────────────────────────────────────────────────────────
creado_en: ""             # formato ISO: "YYYY-MM-DD"
actualizado_en: ""        # formato ISO: "YYYY-MM-DD"
---
```

### Regla de trazabilidad externa

- El bloque `tickets` es **opcional** en todos los estados.
- Si existe una fuente externa, cada referencia debe indicar al menos `sistema` y, cuando exista, `id` y/o `url`.
- La trazabilidad documental debe reflejar el sistema real de origen: Jira, Linear, GitHub, Azure DevOps, Zendesk u otros.
- Si no existe ticket externo, el documento puede vivir solo con el `id` interno de la KB.

### ADRs (`02-arquitectura/decisiones/` y `03-modulos/*/decisiones/`)

```yaml
---
id: "ADR-0001"
titulo: "Título de la decisión arquitectural"
estado: propuesta         # propuesta | aceptada | rechazada | obsoleta | supersedida-por:ADR-XXXX
fecha: "YYYY-MM-DD"
decisores: []             # [@usuario1, @usuario2]
etiquetas: []             # ["base-de-datos", "infraestructura"]
---
```

### Releases (`10-releases/`)

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

## Estilo de escritura

- Escribir en **español** (salvo términos técnicos consolidados en inglés: API, deploy, sprint...)
- Usar **voz activa** y frases cortas
- Un concepto por párrafo
- Las secciones de `spec.md` responden a: **qué**, **por qué** y **criterios de aceptación**
- Las secciones de `tech-design.md` responden a: **cómo**, **qué alternativas se descartaron** y **qué riesgos hay**

---

## Tamaño de los documentos

Para garantizar calidad de chunking en RAG:

| Tipo de documento     | Longitud recomendada |
| --------------------- | -------------------- |
| `spec.md`             | 300–800 palabras     |
| `tech-design.md`      | 500–1500 palabras    |
| `README.md` de módulo | 200–500 palabras     |
| ADR                   | 200–600 palabras     |
| Runbook               | 300–1000 palabras    |
| Release notes         | 200–800 palabras     |

Divide documentos más largos en sub-secciones o archivos separados.

---

## Diagramas

- Preferir **Mermaid** embebido en Markdown (renderizable en GitHub, GitLab, Notion)
- Para diagramas complejos, guardar el fuente en `assets/` junto al documento
- Formato de archivos en `assets/`: `.png`, `.svg`, `.drawio`, `.excalidraw`

---

## Lo que NO debes hacer

- No crear documentos sin frontmatter YAML completo
- No editar `_indice.md` manualmente
- No usar mayúsculas ni espacios en nombres de archivos
- No duplicar información entre documentos — enlaza, no copies
- No dejar `TODO` sin resolver antes de cambiar `estado` a `aprobado`
