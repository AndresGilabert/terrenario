---
bloque: 00-meta
documento: template-v1.0.0-a-v1.1.0
actualizado_en: "2026-07-15"
---

# Migración de Plantilla — v1.0.0 a v1.1.0

## Objetivo

Introducir un modelo explícito de actualización de plantilla basado en versión declarada
y núcleo sincronizable.

## Cambios incluidos

- Se añade `template-state.md`.
- Se añade `upgrade-template.md`.
- Se documenta la política de núcleo sincronizable.
- Se amplía el validador para comprobar el estado de plantilla.
- Se alinean `README.md`, `CONTRIBUTING.md` y `AGENTS.md` con el nuevo flujo.

## Pasos de migración

1. Copiar al proyecto consumidor:
   - `docs/00-meta/template-state.md`
   - `docs/00-meta/upgrade-template.md`
   - `docs/00-meta/migraciones/template-v1.0.0-a-v1.1.0.md`
2. Actualizar el núcleo sincronizable:
   - `AGENTS.md`
   - `CONTRIBUTING.md`
   - `.github/copilot-instructions.md`
   - `docs/00-meta/convenciones.md`
   - `docs/00-meta/scripts/validar_kb.py`
   - `docs/00-meta/scripts/README.md`
3. Revisar manualmente cualquier personalización previa en esos archivos.
4. Ajustar `template_repo` y, si procede, `template_core_policy`.
5. Ejecutar la validación.

## Verificación final

- `template_version` queda en `1.1.0`
- `template_last_reviewed` refleja la fecha de revisión
- El validador no devuelve errores
