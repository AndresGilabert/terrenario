# IA_KB_Template

> Estructura de base de conocimiento reutilizable para agentes de IA, desarrolladores y jefes de producto.
> Diseñada para vivir dentro del repositorio del proyecto bajo `docs/`.

## Licenciamiento

Este repositorio se publica con licencia propietaria (All rights reserved).

- Consulta `LICENSE` para los términos completos.
- La visibilidad pública del repositorio no concede derechos de uso, copia, modificación o distribución.
- No se aceptan contribuciones externas por el momento.

---

## Quick Start — 5 minutos

### 1. Copia la estructura a tu proyecto

**PowerShell (Windows):**

```powershell
# Desde la raíz de tu repositorio
git clone https://github.com/tu-org/IA_DOC_Template .kb-template
Copy-Item -Recurse .kb-template/docs ./docs
Copy-Item .kb-template/AGENTS.md ./AGENTS.md
Copy-Item .kb-template/CONTRIBUTING.md ./CONTRIBUTING.md
Copy-Item .kb-template/.pre-commit-config.yaml ./.pre-commit-config.yaml
Copy-Item -Recurse .kb-template/.github ./.github
Remove-Item -Recurse -Force .kb-template
```

**bash / macOS / Linux:**

```bash
# Desde la raíz de tu repositorio
git clone https://github.com/tu-org/IA_DOC_Template .kb-template
cp -r .kb-template/docs ./docs
cp .kb-template/AGENTS.md ./AGENTS.md
cp .kb-template/CONTRIBUTING.md ./CONTRIBUTING.md
cp .kb-template/.pre-commit-config.yaml ./.pre-commit-config.yaml
cp -r .kb-template/.github ./.github
rm -rf .kb-template
```

### 2. Instala las dependencias de validación

```bash
pip install pyyaml
npm install -g markdownlint-cli   # opcional, para linting de markdown
```

### 3. Instala los pre-commit hooks (recomendado)

```bash
pip install pre-commit
pre-commit install
```

### 4. Declara el estado de plantilla

Después de copiar la estructura, revisa y conserva `docs/00-meta/template-state.md`.
Ese archivo declara:

- qué versión de la plantilla usa el proyecto
- qué archivos forman el núcleo sincronizable
- cuándo se revisó por última vez la alineación con la plantilla

### 5. Adapta los archivos de contexto del proyecto

| Archivo | Qué rellenar primero |
|---------|---------------------|
| `docs/01-producto/vision-y-objetivos.md` | Visión, misión, objetivo del producto |
| `docs/01-producto/stakeholders.md` | Equipo y roles |
| `docs/02-arquitectura/vision-general.md` | Diagrama C4 del sistema |
| `docs/03-modulos/_vision-general.md` | Mapa de módulos / dominios |
| `AGENTS.md` | Ajustar el mapa de navegación a tu proyecto |

### 6. Documenta tu primer desarrollo

```bash
# Crea la carpeta de la épica
mkdir -p docs/09-desarrollos/epicas/PROJ-001--nombre-epica

# Copia la plantilla
cp docs/09-desarrollos/_plantilla/epica-spec.md \
   docs/09-desarrollos/epicas/PROJ-001--nombre-epica/spec.md

# Crea la primera historia dentro de la épica
mkdir -p docs/09-desarrollos/epicas/PROJ-001--nombre-epica/PROJ-010--primera-historia
cp docs/09-desarrollos/_plantilla/feature-spec.md \
   docs/09-desarrollos/epicas/PROJ-001--nombre-epica/PROJ-010--primera-historia/spec.md
```

### 7. Valida y genera índices

```bash
python docs/00-meta/scripts/validar_kb.py --validar
python docs/00-meta/scripts/validar_kb.py --generar-indices
```

Opción rápida (comando único recomendado):

```bash
python docs/00-meta/scripts/validar_pipeline_kb.py --check-indices-clean
```

---

## Modelo de actualización de plantilla

Esta plantilla **no se sincroniza automáticamente** una vez copiada al proyecto.

El modelo soportado es:

1. **Versionado de plantilla**: el proyecto declara la versión adoptada en `docs/00-meta/template-state.md`.
2. **Núcleo sincronizable**: solo ciertos archivos deben alinearse con nuevas versiones de la plantilla.
3. **Migración guiada**: cada cambio de versión relevante debe acompañarse de instrucciones de migración.

### Núcleo sincronizable

El núcleo sincronizable recomendado incluye:

- `AGENTS.md`
- `CONTRIBUTING.md`
- `.github/copilot-instructions.md`
- `.pre-commit-config.yaml`
- `docs/00-meta/convenciones.md`
- `docs/00-meta/scripts/validar_kb.py`
- `docs/00-meta/scripts/README.md`
- `docs/00-meta/plantillas/`

### Contenido local del proyecto

No debe sobrescribirse automáticamente al actualizar la plantilla:

- `docs/01-producto/`
- `docs/02-arquitectura/`
- `docs/03-modulos/`
- `docs/05-infraestructura/`
- `docs/06-integraciones/`
- `docs/09-desarrollos/`
- `docs/10-releases/`
- `docs/99-glosario/`

### Cómo actualizar un proyecto que ya usa la plantilla

1. Revisar `docs/00-meta/changelog.md` en la versión nueva.
2. Leer `docs/00-meta/upgrade-template.md`.
3. Aplicar la migración correspondiente desde `docs/00-meta/migraciones/`.
4. Sincronizar solo el núcleo.
5. Ejecutar `python docs/00-meta/scripts/validar_kb.py --validar`.
6. Actualizar `template_version` y `template_last_reviewed` en `docs/00-meta/template-state.md`.

Si quieres apoyo operativo para el paso 4:

```bash
# Ver plan de sincronización del núcleo
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --plan

# Aplicar sincronización del núcleo
python docs/00-meta/scripts/sync_template_core.py --source ../IA_DOC_Template --apply
```

---

## Estructura de la KB

```text
docs/
├── 00-meta/          Gobernanza, plantillas, convenciones, script de validación
├── 01-producto/      Visión, stakeholders, roadmap, KPIs, personas
├── 02-arquitectura/  C4 Model, ADRs, modelo de datos, contratos API
├── 03-modulos/       Bounded Contexts (DDD): un subdirectorio por módulo
├── 04-ingenieria/    Estándares de código, Git, testing, code review
├── 05-infraestructura/ Entornos, CI/CD, observabilidad, runbooks
├── 06-integraciones/ Sistemas externos: un subdirectorio por integración
├── 07-seguridad/     Modelo de seguridad, autenticación, privacidad (OWASP)
├── 08-procesos/      DoR, DoD, gestión de incidentes, proceso de release
├── 09-desarrollos/   Épicas e historias activas (bloque dinámico)
├── 10-releases/      Changelog por versión semántica
└── 99-glosario/      Lenguaje ubicuo del dominio (DDD)
```

---

## Para agentes de IA

Lee `AGENTS.md` en la raíz del repositorio. Contiene:

- Mapa de navegación rápida por tipo de pregunta
- Reglas obligatorias antes de generar código o documentación
- Instrucciones por rol (agente de desarrollo, producto, documentación)

---

## Documentación de uso

| Audiencia | Documento |
|-----------|----------|
| Agentes de IA | [`AGENTS.md`](./AGENTS.md) |
| Desarrolladores | [`CONTRIBUTING.md`](./CONTRIBUTING.md) |
| Product Managers | [`docs/00-meta/guia-pm.md`](./docs/00-meta/guia-pm.md) |
| Nuevos miembros | [`docs/04-ingenieria/onboarding.md`](./docs/04-ingenieria/onboarding.md) |
| Convenciones y naming | [`docs/00-meta/convenciones.md`](./docs/00-meta/convenciones.md) |
| Script de validación | [`docs/00-meta/scripts/validar_kb.py`](./docs/00-meta/scripts/validar_kb.py) |
| Script de sincronización | [`docs/00-meta/scripts/sync_template_core.py`](./docs/00-meta/scripts/sync_template_core.py) |
| Plantilla de release | [`docs/00-meta/plantillas/release-notes.md`](./docs/00-meta/plantillas/release-notes.md) |
| Estado de plantilla | [`docs/00-meta/template-state.md`](./docs/00-meta/template-state.md) |
| Guía de upgrade | [`docs/00-meta/upgrade-template.md`](./docs/00-meta/upgrade-template.md) |
