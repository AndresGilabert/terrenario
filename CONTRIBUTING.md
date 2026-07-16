# Guía de contribución — Base de Conocimiento

> Toda tarea de desarrollo DEBE estar documentada en la KB.
> Sin `spec.md` en estado `aprobado` → el desarrollo no comienza.

## Alcance de contribuciones

Este repositorio es de propiedad exclusiva de AGILABERT y no acepta contribuciones externas en este momento.

- Las contribuciones, issues o pull requests de terceros no serán revisadas ni incorporadas.
- Esta guía aplica únicamente a personal interno autorizado por AGILABERT.

---

## Regla fundamental

La base de conocimiento en `docs/` no es documentación opcional. Es una **condición de entrada
y salida** para cualquier trabajo en este repositorio:

- **Entrada**: el ticket debe tener `spec.md` con estado `aprobado` antes de crear una rama
- **Salida**: el `tech-design.md` debe estar completo antes de mergear a `develop`, y el conjunto validado se promueve después a `main`

---

## Ciclo de vida de una feature

```text
[PM crea spec.md] → estado: borrador
        ↓
[Revisión de equipo] → estado: en-revision
        ↓
[Aprobación] → estado: aprobado
        ↓
[Dev crea rama + tech-design.md] → estado: en-progreso
        ↓
[Merge a develop + QA / Testing integrado] → estado: en-testing
        ↓
[Promoción de develop a main + release] → estado: completado
```

---

## Estructura de ramas

Sigue las convenciones de `docs/04-ingenieria/flujo-git.md`.
El nombre de la rama debe incluir el ID del ticket:

```text
feature/PROJ-123--flujo-principal
bugfix/PROJ-456--error-timeout-api
spike/PROJ-789--evaluar-integracion-externa
```

Las ramas de trabajo parten de `develop` y vuelven a `develop`.
`main` queda reservada para código ya validado en conjunto y listo para release.

---

## Cómo crear la documentación de una nueva historia

### 1. Identifica la épica a la que pertenece

```text
docs/09-desarrollos/epicas/{TICKET_ID}--{epica-slug}/
```

### 2. Crea la carpeta de la historia dentro de la épica

```text
docs/09-desarrollos/epicas/{EPICA_ID}--{epica-slug}/{HISTORIA_ID}--{historia-slug}/
```

Ejemplo:

```text
docs/09-desarrollos/epicas/PROJ-100--epica-ejemplo/PROJ-123--historia-ejemplo/
```

### 3. Crea los archivos desde las plantillas

```bash
# Copia las plantillas
cp docs/09-desarrollos/_plantilla/feature-spec.md    docs/09-desarrollos/epicas/.../spec.md
cp docs/09-desarrollos/_plantilla/tech-design.md     docs/09-desarrollos/epicas/.../tech-design.md
```

### 4. Rellena el frontmatter YAML completo

Campos obligatorios: `id`, `tipo`, `titulo`, `estado`, `epica`, `responsable`

### 5. Actualiza la épica padre

Añade el ID de la historia al campo `historias:` en el `spec.md` de la épica.

### 6. El `_indice.md` se regenera automáticamente

El workflow de CI ejecuta `validar_kb.py --generar-indices` en cada PR.
No edites `_indice.md` manualmente.

---

## Cómo crear una nueva épica

```text
docs/09-desarrollos/epicas/{TICKET_ID}--{epica-slug}/
├── spec.md       ← usa docs/09-desarrollos/_plantilla/epica-spec.md
└── _indice.md    ← creado vacío; el CI lo rellena
```

---

## Actualización de la plantilla

Los proyectos que usan esta KB deben tratar la plantilla como un activo versionado.

### Política

- La copia inicial de esta plantilla **no mantiene vínculo automático** con el repositorio origen.
- Las mejoras posteriores se aplican mediante migraciones guiadas.
- Solo debe sincronizarse el **núcleo de plantilla**; el contenido contextual del proyecto se mantiene local.

### Núcleo sincronizable

- `AGENTS.md`
- `CONTRIBUTING.md`
- `.github/copilot-instructions.md`
- `.pre-commit-config.yaml`
- `docs/00-meta/README.md`
- `docs/00-meta/convenciones.md`
- `docs/00-meta/upgrade-template.md`
- `docs/00-meta/migraciones/`
- `docs/00-meta/scripts/validar_kb.py`
- `docs/00-meta/scripts/sync_template_core.py`
- `docs/00-meta/scripts/README.md`
- `docs/00-meta/plantillas/`

### Flujo de upgrade

1. Leer `docs/00-meta/template-state.md` del proyecto consumidor.
2. Revisar `docs/00-meta/changelog.md` de la versión nueva.
3. Aplicar la guía general de `docs/00-meta/upgrade-template.md`.
4. Ejecutar `python docs/00-meta/scripts/sync_template_core.py --source <ruta-plantilla> --plan`.
5. Ejecutar la migración específica desde `docs/00-meta/migraciones/`.
6. Si procede, aplicar `python docs/00-meta/scripts/sync_template_core.py --source <ruta-plantilla> --apply`.
7. Actualizar `template_version` y `template_last_reviewed`.
8. Validar la KB y resolver conflictos manuales.

---

## Cómo añadir un módulo nuevo

1. Crear carpeta: `docs/03-modulos/{nombre-modulo}/`
2. Usar la plantilla: `docs/00-meta/plantillas/modulo-readme.md`
3. Crear los archivos del módulo (ver estructura en `docs/03-modulos/_vision-general.md`)
4. Actualizar `docs/03-modulos/_vision-general.md` con el nuevo módulo

---

## Cómo crear un ADR

1. Determina si el ADR es global (`docs/02-arquitectura/decisiones/`) o de módulo (`docs/03-modulos/{modulo}/decisiones/`)
2. Copia la plantilla: `docs/00-meta/plantillas/adr.md`
3. Nombra el archivo: `ADR-{XXXX}--{titulo-slug}.md` (número secuencial)
4. Estado inicial siempre: `propuesta`

---

## Checklist de PR

Antes de abrir un Pull Request, verifica:

- [ ] El `spec.md` de todas las historias afectadas está en estado `aprobado` o superior
- [ ] El `tech-design.md` está completo (secciones sin `TODO`)
- [ ] El frontmatter YAML está completo y válido en todos los archivos creados/modificados
- [ ] Si hay una decisión arquitectural relevante, se ha creado un ADR
- [ ] Los módulos afectados tienen su documentación actualizada en `docs/03-modulos/`
- [ ] El CI pasa (validación de KB + linting de markdown)
- [ ] El PR apunta a `develop`, salvo promociones a `main` o hotfixes urgentes

Regla sobre aprobaciones:

- La revisión por pares es recomendable en equipos con varios developers.
- No debe exigirse aprobación de otros colaboradores en proyectos desarrollados en solitario.

---

## Validación local

Antes de hacer push, ejecuta la validación en local:

```bash
# Instalar dependencias
pip install pyyaml

# Validar estructura y frontmatter
python docs/00-meta/scripts/validar_kb.py --validar

# Regenerar índices de épicas
python docs/00-meta/scripts/validar_kb.py --generar-indices
```

---

## Convenciones de naming

| Elemento          | Patrón                              | Ejemplo                          |
|-------------------|-------------------------------------|----------------------------------|
| Carpeta de épica  | `{ID}--{epica-slug}`                | `PROJ-100--mi-epica`             |
| Carpeta de historia | `{ID}--{historia-slug}`           | `PROJ-123--mi-historia`          |
| ADR               | `ADR-{XXXX}--{titulo-slug}.md`      | `ADR-0001--titulo-slug.md`       |
| Runbook           | `RB-{XXX}--{titulo-slug}.md`        | `RB-001--titulo-slug.md`         |
| Release           | `v{MAJOR}.{MINOR}.{PATCH}.md`       | `v2.1.0.md`                      |

Consulta `docs/00-meta/convenciones.md` para todas las convenciones.
