# validar_kb.py — Documentación

Scripts de validación y mantenimiento de la Knowledge Base.
Ubicación principal: `docs/00-meta/scripts/`

Scripts disponibles:

- `validar_kb.py`
- `sync_template_core.py`
- `validar_pipeline_kb.py`

---

## `validar_pipeline_kb.py` — Pipeline unificado de validación

Ejecuta en un solo comando:

1. `validar_kb.py --validar`
2. `validar_kb.py --generar-indices`
3. comprobación opcional de `_indice.md` sin cambios pendientes
4. `markdownlint` con `docs/00-meta/.markdownlint.json`

### Uso

```bash
# Ejecucion completa recomendada
python docs/00-meta/scripts/validar_pipeline_kb.py --check-indices-clean

# Modo PR: bloquear solo cambios actuales
python docs/00-meta/scripts/validar_pipeline_kb.py --solo-cambios --base-ref origin/develop --check-indices-clean

# Si no quieres ejecutar markdownlint en un contexto concreto
python docs/00-meta/scripts/validar_pipeline_kb.py --skip-markdownlint
```

### Requisitos

- Python 3.9+
- `pyyaml`
- `markdownlint-cli` disponible en PATH

---

## `sync_template_core.py` — Sincronización del núcleo de plantilla

Ayuda a comparar y copiar el núcleo sincronizable declarado en `docs/00-meta/template-state.md`
desde una copia local de la plantilla hacia un proyecto consumidor.

### Uso

```bash
# Ver plan sin modificar archivos
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --plan

# Aplicar copia del núcleo
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --apply

# Aplicar y eliminar archivos ausentes en la fuente dentro del núcleo
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --apply --delete
```

### Qué hace

- Lee `template_core_paths` desde `docs/00-meta/template-state.md`
- Compara el núcleo del proyecto consumidor con la plantilla fuente
- Muestra un plan de `create`, `update`, `delete` o `unchanged`
- En modo `--apply`, copia únicamente rutas del núcleo
- En modo `--delete`, elimina solo archivos del núcleo ausentes en la fuente
- Si la fuente declara otra `template_version`, actualiza `template_version` y `template_last_reviewed`

### Límites de seguridad

- No toca rutas fuera de `template_core_paths`
- No reescribe documentación contextual del proyecto fuera del núcleo
- Requiere una ruta local de la plantilla fuente; no descarga nada desde remoto

---

## `validar_kb.py` — Validación

---

## Requisitos

```bash
pip install pyyaml
```

Python 3.9 o superior.

---

## Uso

```bash
# Ejecutar desde la raíz del repositorio
python docs/00-meta/scripts/validar_kb.py [opciones]
```

### Opciones

| Opción              | Descripción                                                                 |
| ------------------- | --------------------------------------------------------------------------- |
| `--validar`         | Valida estructura de carpetas, naming y frontmatter YAML                    |
| `--generar-indices` | Regenera todos los `_indice.md` de las épicas                               |
| `--solo-cambios`    | Trata hallazgos en docs legacy como warning y bloquea solo cambios actuales |
| `--base-ref <ref>`  | Ref git para calcular cambios en `--solo-cambios` (default: `main`)         |

Se pueden combinar: `--validar --generar-indices`

Modo recomendado para transición de reglas en repos con histórico legacy:

```bash
python docs/00-meta/scripts/validar_kb.py --validar --solo-cambios --base-ref main
```

---

## `--validar` — Qué comprueba

### Naming de carpetas en `09-desarrollos/epicas/`

Todas las carpetas de épicas e historias deben seguir el patrón:

```text
{TICKET_ID}--{descripcion-en-kebab-case}
```

Ejemplos válidos: `PROJ-100--mi-epica`, `PROJ-123--mi-historia`
Ejemplos inválidos: `checkout-refactor`, `PROJ100--checkout`, `PROJ-100_checkout`

Además, el validador exige coherencia entre carpeta y frontmatter:

- El `id` del frontmatter debe coincidir con el ID de la carpeta (`ID`).
- En historias, `epica` debe coincidir exactamente con la carpeta padre.

También valida límites preventivos de longitud:

- Nombre de carpeta de épica o historia: máximo 64 caracteres.
- Ruta relativa al repo bajo `docs/`: máximo 180 caracteres.
- Ruta absoluta local: máximo 220 caracteres.

### Frontmatter YAML de historias y épicas

Campos obligatorios en historias (`tipo != epica`):

| Campo         | Descripción                                               |
| ------------- | --------------------------------------------------------- |
| `id`          | ID del ticket                                             |
| `tipo`        | `feature`, `bugfix`, `mejora`, `spike`, `tarea` o `epica` |
| `titulo`      | Título descriptivo                                        |
| `estado`      | Estado válido (ver abajo)                                 |
| `epica`       | ID--slug de la épica padre                                |
| `responsable` | @usuario                                                  |
| `ai_context`  | Bloque completo (ver abajo)                               |

Campos obligatorios en épicas (`tipo: epica`): igual que historias, sin `epica`.

**Estados válidos** (`estado:`):
`borrador` | `en-revision` | `aprobado` | `en-progreso` | `en-testing` | `completado` | `cancelado`

**Tipos válidos** (`tipo:`):
`feature` | `bugfix` | `mejora` | `spike` | `tarea` | `epica`

**Bloque `ai_context`** — campos esperados:
`dominios`, `modulo_path`, `componentes`, `etiquetas`, `nivel_riesgo`

`nivel_riesgo` válidos: `bajo` | `medio` | `alto` | `critico`

### Estado de plantilla

Si existe `docs/00-meta/template-state.md`, el validador comprueba:

- `template_id`
- `template_version`
- `template_core_policy`
- `template_last_reviewed`
- `template_repo`

Si el archivo no existe, el validador emite una advertencia, no un error.

### Calidad mínima por estado

Para estados `en-revision`, `aprobado`, `en-progreso`, `en-testing` y `completado`,
el validador bloquea placeholders como `TODO`, `por definir` y `pendiente de refinamiento`.

### Trazabilidad externa

Si el documento incluye la sección `## Trazabilidad externa`, el validador exige:

- `Sistema:`
- `Fecha de sincronización o revisión:`

El bloque `tickets` es opcional. El validador admite el formato nuevo basado en lista y el formato legacy basado en mapa.

### ADRs

Los archivos ADR deben seguir el patrón de nombre:

```text
ADR-{XXXX}--{titulo-slug}.md
```

Y tener frontmatter con `id`, `titulo`, `estado`, `fecha`.

**Estados ADR válidos**: `propuesta` | `aceptada` | `rechazada` | `obsoleta` | `supersedida-por:ADR-XXXX`

---

## `--generar-indices` — Qué genera

Por cada carpeta de épica en `09-desarrollos/epicas/`, regenera o crea el archivo `_indice.md`
con una tabla de todas las historias encontradas en subdirectorios directos.

### Ejemplo de `_indice.md` generado

```markdown
# Índice — PROJ-100: Título de la épica

> **Progreso**: 1/2 completadas · **Hito**: vX.Y.Z
> _Generado automáticamente por `validar_kb.py`. No editar manualmente._

| Historia | Título | Estado | Responsable | Prioridad |
|----------|--------|--------|-------------|-----------|
| [PROJ-123](./PROJ-123--mi-historia/spec.md) | Título de historia | 🔄 en-progreso | @dev1 | alta |
| [PROJ-124](./PROJ-124--otra-historia/spec.md) | Otra historia | 📝 borrador | @dev2 | alta |
```

### Emojis de estado en el índice

| Estado        | Emoji |
| ------------- | ----- |
| `borrador`    | 📝     |
| `en-revision` | 👀     |
| `aprobado`    | ✅     |
| `en-progreso` | 🔄     |
| `en-testing`  | 🧪     |
| `completado`  | ✔️    |
| `cancelado`   | ❌     |

---

## Códigos de salida

| Código | Significado                                       |
| ------ | ------------------------------------------------- |
| `0`    | Sin errores                                       |
| `1`    | Errores de validación encontrados (bloquea el CI) |

Las advertencias (`WARN`) no provocan salida con código 1 — son informativas.

---

## Integración con CI/CD

El workflow `.github/workflows/validar-kb.yml` ejecuta el script automáticamente
en cada Pull Request que toca archivos bajo `docs/`.

El job `validar-kb` falla el PR si:

- Hay errores de validación (`--validar` retorna código 1)
- Hay archivos `_indice.md` desactualizados tras ejecutar `--generar-indices`

---

## Integración con pre-commit

El archivo `.pre-commit-config.yaml` en la raíz del repo ejecuta el script
localmente antes de cada `git commit` que afecte a `docs/`.

```bash
# Instalar hooks la primera vez
pip install pre-commit
pre-commit install

# Ejecutar manualmente sobre todos los archivos
pre-commit run --all-files
```

---

## Flujo estándar para alta de épica e historia

Objetivo: evitar dependencia de decisiones manuales del desarrollador en naming y estructura.

1. Definir el `id` de la épica e historia dentro de la KB.
2. Derivar automáticamente el slug desde el título del desarrollo o de la fuente externa según `docs/00-meta/convenciones.md`.
3. Crear estructura documental con plantillas oficiales.
4. Si existe fuente externa, completar su trazabilidad y los campos pendientes de refinamiento.
5. Ejecutar validación y generación de índices.

Resultado esperado:

- Carpetas y documentos homogéneos en todo el equipo.
- Trazabilidad externa verificable cuando exista.
- Cero variación por prompts personalizados.

---

## Upgrade de plantilla

La plantilla se actualiza mediante:

1. `docs/00-meta/template-state.md` en el proyecto consumidor
2. `docs/00-meta/changelog.md` en el repositorio de la plantilla
3. `docs/00-meta/upgrade-template.md` como guía general
4. `docs/00-meta/migraciones/` para saltos de versión concretos

Política recomendada:

- Sincronizar solo el núcleo de plantilla
- No sobrescribir contenido contextual del proyecto fuera del núcleo
- Actualizar `template_version` tras completar la migración

---

## Extender el script

Para añadir nuevas validaciones, editar la función `validar_desarrollos()` o
`validar_adrs()` en el script. Cada nueva regla debe:

1. Usar `error(msg)` para fallos bloqueantes
2. Usar `warn(msg)` para advertencias no bloqueantes
3. Incluir la ruta del archivo afectado en el mensaje
