---
bloque: 00-meta
documento: template-state
actualizado_en: "2026-07-15"
template_id: ia-doc-template
template_version: "1.1.0"
template_core_policy: synced
template_last_reviewed: "2026-07-15"
template_repo: "https://github.com/tu-org/IA_DOC_Template"
template_core_paths:
  - "AGENTS.md"
  - "CONTRIBUTING.md"
  - ".github/copilot-instructions.md"
  - ".pre-commit-config.yaml"
  - "docs/00-meta/README.md"
  - "docs/00-meta/convenciones.md"
  - "docs/00-meta/upgrade-template.md"
  - "docs/00-meta/migraciones/"
  - "docs/00-meta/scripts/validar_kb.py"
  - "docs/00-meta/scripts/sync_template_core.py"
  - "docs/00-meta/scripts/README.md"
  - "docs/00-meta/plantillas/"
---

# Estado de la Plantilla

Este archivo declara qué versión de la plantilla usa el proyecto y qué partes deben tratarse
como núcleo sincronizable frente a contenido propio del proyecto.

## Núcleo sincronizable

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

## Contenido local del proyecto

No debe sobrescribirse automáticamente al actualizar la plantilla:

- `docs/01-producto/`
- `docs/02-arquitectura/`
- `docs/03-modulos/`
- `docs/05-infraestructura/`
- `docs/06-integraciones/`
- `docs/09-desarrollos/`
- `docs/10-releases/`
- `docs/99-glosario/`

## Desviaciones locales del núcleo

El flujo de upgrade contempla «resolver conflictos manuales si el proyecto ha personalizado archivos
del núcleo» (paso 5), pero no dónde apuntar **cuáles**. Esta tabla existe para eso: sin ella, cada
upgrade obliga a redescubrir la personalización leyendo diffs, y lo más probable es que se pierda.

| Archivo del núcleo | Qué se cambió | Por qué | Al actualizar la plantilla |
|---|---|---|---|
| `AGENTS.md` | Nota sobre módulos | Describía `modulo-ejemplo/`, retirado en `MVP-716` | Reaplicar: la plantilla volverá a hablar del módulo de ejemplo |
| `.github/copilot-instructions.md` | Instrucciones «al tocar un módulo» y de documentación | Mandaban leer `modelo-dominio.md` y `eventos.md` de cada módulo, que **nunca han existido** en este proyecto, y tratar `modulo-ejemplo/` como contenedor de ejemplo | Reaplicar |
| `docs/00-meta/README.md` | Tabla de módulos | Listaba `modulo-ejemplo` y una fila vacía | Reaplicar con el catálogo real de `docs/03-modulos/_vision-general.md` |

Las tres se cambiaron en `MVP-716` por el mismo motivo: **apuntaban a un directorio que dejó de
existir**. Dejarlas como estaban habría dejado instrucciones activas hacia un sitio inexistente, que
es peor que la deriva.

Desviación **estructural** que no es de un archivo concreto: `docs/00-meta/plantillas/modulo-readme.md`
prescribe siete documentos por módulo y aquí cada módulo tiene **un solo `README.md`** que enlaza los
`tech-design.md` de sus historias. Con seis módulos serían 42 ficheros, y varios no tendrían contenido
posible —no hay bus de eventos (`ADR-0002`), así que `eventos.md` estaría vacío por construcción—. La
plantilla no se toca; la desviación se declara aquí.

---

## Políticas posibles

- `synced`: el núcleo debe mantenerse alineado con la plantilla
- `manual`: las actualizaciones del núcleo se revisan caso a caso
- `frozen`: el proyecto deja de incorporar mejoras automáticas de la plantilla

## Uso esperado en proyectos consumidores

1. Revisar este archivo al adoptar la plantilla.
2. Actualizar `template_version` después de cada migración completada.
3. Actualizar `template_last_reviewed` al finalizar la revisión de upgrade.
