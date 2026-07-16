---
bloque: 04-ingenieria
documento: flujo-git
actualizado_en: "2026-06-25"
---

# Flujo de Git

---

## Estrategia de branching

Usamos un flujo con **`main` + `develop`**:

```text
main          ← código en producción, solo releases validadas
develop       ← rama de integración, validación conjunta antes de release
        └── feature/{ID}--{descripcion}              ← desarrollo de features
        └── bugfix/{ID}--{descripcion}               ← corrección de bugs no urgentes
        └── spike/{ID}--{descripcion}                ← investigación técnica
        └── chore/{ID}--{descripcion}                ← tareas de mantenimiento
        └── hotfix/{ID}--{descripcion}               ← fixes urgentes con retorno posterior a develop
```

Objetivo de cada rama:

- `main`: refleja únicamente código listo para producción.
- `develop`: concentra la validación integrada del conjunto de cambios antes de promoverlos a `main`.
- ramas de trabajo: nacen desde `develop` y vuelven a `develop`, salvo hotfixes urgentes.

---

## Nomenclatura de ramas

```text
{tipo}/{TICKET_ID}--{descripcion-en-kebab-case}
```

Ejemplos:

- `feature/RF-E04-001--implementacion-dummy-provider-client`
- `bugfix/RF-E05-001--fix-jwt-validation-gateway`
- `spike/RF-E03-005--evaluar-scrutor-scan`

| Tipo | Cuándo |
|------|--------|
| `feature/` | Nueva funcionalidad |
| `bugfix/` | Corrección de bug no urgente |
| `hotfix/` | Fix urgente en producción |
| `spike/` | Investigación técnica |
| `chore/` | Tareas de mantenimiento sin impacto en producto |

---

## Ciclo de vida de una feature

```text
[PM crea spec.md] → estado: borrador
        ↓
[Revisión de equipo] → estado: en-revision
        ↓
[Aprobación] → estado: aprobado  ← el ticket está listo para desarrollo (DoR)
        ↓
[Dev crea rama desde develop + tech-design.md] → estado: en-progreso
        ↓
[Merge a develop + validación integrada] → estado: en-testing
        ↓
[Promoción de develop a main + release] → estado: completado  ← DoD cumplido
```

> **Regla fundamental**: sin `spec.md` con estado `aprobado` en `docs/09-desarrollos/epicas/`, el desarrollo no comienza.

---

## Convenciones de commits

Seguimos **Conventional Commits**:

```text
{tipo}({scope}): {descripción corta en imperativo}

[cuerpo opcional — explica el QUÉ y el POR QUÉ, no el CÓMO]

[pie opcional — referencias a tickets]
```

**Tipos válidos:**

| Tipo | Cuándo |
|------|--------|
| `feat` | Nueva funcionalidad |
| `fix` | Corrección de bug |
| `docs` | Solo cambios en documentación |
| `refactor` | Refactoring sin nueva feature ni bug fix |
| `test` | Añadir o corregir tests |
| `chore` | Mantenimiento, dependencias |

**Ejemplo:**

```text
feat(valuation): conectar handler de valoración con ProviderClientFactory

El handler ahora resuelve el cliente de proveedor via factory
en lugar de instanciarlo directamente.

Closes RF-E04-004
```

---

## Alta documental previa obligatoria

Antes de crear rama de trabajo, el desarrollo debe tener alta documental en `09-desarrollos`.

Condiciones mínimas:

1. Carpeta épica y carpeta historia creadas con slug derivado automáticamente del título del desarrollo o de la fuente externa, si existe.
2. `spec.md` de épica y `spec.md` de historia creados desde plantilla oficial.
3. Si existe ticket externo, la sección de trazabilidad correspondiente está completa.
4. Naming y rutas dentro de los límites definidos en `docs/00-meta/convenciones.md`.
5. Validación KB en verde.

> **Regla de bloqueo**: si la alta documental no cumple estos puntos, no se inicia rama de desarrollo.

---

## Proceso de Pull Request

1. Las ramas `feature/`, `bugfix/`, `spike/` y `chore/` parten siempre de `develop`.
2. El nombre del PR incluye el ID del ticket: `[RF-E04-001] Implementación DummyProviderClient`.
3. El PR debe tener `spec.md` en estado `aprobado` antes de crear la rama.
4. El `tech-design.md` debe estar completo antes de solicitar revisión o merge.
5. El PR hacia `develop` debe pasar completamente el CI (tests + validación de KB).
6. La aprobación de otro developer es **opcional** y depende del tamaño del equipo y del modelo de trabajo.
7. En proyectos con un único developer, no debe exigirse aprobación externa para poder mergear.
8. La promoción de `develop` a `main` se hace mediante PR o merge controlado tras validación integrada satisfactoria.
9. Hacer **squash merge** cuando ayude a mantener el historial limpio.

Flujo recomendado de PR:

1. `feature/*` → `develop`
2. validación conjunta en `develop`
3. `develop` → `main`

---

## Protecciones de ramas

`main` está protegida:

- Push directo prohibido.
- Requiere integración previa validada desde `develop`.
- Requiere CI en verde.
- No se puede reescribir el historial.

`develop` también debería estar protegida:

- Push directo desaconsejado salvo en proyectos unipersonales si el equipo así lo decide.
- Requiere CI en verde antes de mergear.
- Debe ser la base de validación integrada del sprint o lote de cambios.

Regla sobre aprobaciones:

- Si hay varios developers, puede exigirse revisión por pares.
- Si el proyecto es unipersonal, la aprobación de terceros no debe ser obligatoria.
