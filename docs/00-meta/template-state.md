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

## Políticas posibles

- `synced`: el núcleo debe mantenerse alineado con la plantilla
- `manual`: las actualizaciones del núcleo se revisan caso a caso
- `frozen`: el proyecto deja de incorporar mejoras automáticas de la plantilla

## Uso esperado en proyectos consumidores

1. Revisar este archivo al adoptar la plantilla.
2. Actualizar `template_version` después de cada migración completada.
3. Actualizar `template_last_reviewed` al finalizar la revisión de upgrade.
