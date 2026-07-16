---
bloque: 00-meta
documento: upgrade-template
actualizado_en: "2026-07-15"
---

# Upgrade de la Plantilla

Esta guía define cómo aplicar mejoras de la plantilla a un proyecto que ya la está usando.

## Principio general

La copia inicial de la plantilla no mantiene vínculo automático con su repositorio de origen.
Las mejoras posteriores se incorporan mediante migraciones guiadas y sincronización selectiva.

## Qué se actualiza

Actualizar por defecto solo el núcleo declarado en `template-state.md`.

Puede apoyarse el proceso con `python docs/00-meta/scripts/sync_template_core.py --source <ruta-plantilla> --plan`
para inspeccionar diferencias, y con `--apply` para copiar cambios del núcleo.

## Qué no se sobrescribe

No sobrescribir automáticamente documentación contextual del proyecto, especialmente:

- visión de producto
- arquitectura real
- módulos reales
- integraciones reales
- desarrollos activos
- glosario adaptado al dominio

## Flujo recomendado

1. Identificar la versión actual en `template-state.md`.
2. Revisar `changelog.md` para entender el alcance de la nueva versión.
3. Abrir la migración específica en `migraciones/`.
4. Aplicar cambios solo sobre el núcleo sincronizable.
5. Resolver conflictos manuales si el proyecto ha personalizado archivos del núcleo.
6. Ejecutar `python docs/00-meta/scripts/validar_kb.py --validar`.
7. Actualizar `template_version` y `template_last_reviewed`.

## Script de ayuda

Uso recomendado:

```bash
# Ver diferencias sin modificar archivos
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --plan

# Aplicar copia del núcleo desde la plantilla
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --apply
```

Comportamiento del script:

- solo opera sobre `template_core_paths`
- no toca rutas fuera del núcleo
- no elimina archivos locales salvo que se use `--delete`
- actualiza `template_version` y `template_last_reviewed` al aplicar si la fuente declara una versión distinta

## Criterios de seguridad para el upgrade

- Si un archivo mezcla reglas de plantilla con decisiones reales del proyecto, revisar manualmente.
- Si un cambio de plantilla afecta al contrato documental, aplicar primero la migración antes de editar contenido.
- Si un proyecto usa `template_core_policy: frozen`, no actualizar sin decisión explícita del equipo.
