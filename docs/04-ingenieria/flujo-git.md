---
bloque: 04-ingenieria
documento: flujo-git
actualizado_en: "2026-07-29"
---

# Flujo de Git

---

## Estrategia de branching

Usamos una variante de **Git Flow** con `develop` como rama de integración:

```text
main          ← estado promocionado, solo recibe merges desde `develop`
        └── develop   ← rama de integración y preproducción
  └── feature/{TICKET_ID}--{nombre-feature}   ← desarrollo de features
  └── bugfix/{TICKET_ID}--{nombre-bug}         ← corrección de bugs
  └── spike/{TICKET_ID}--{descripcion}         ← investigación técnica
  └── chore/{TICKET_ID}--{descripcion}         ← tareas de mantenimiento
```text
---

## Nomenclatura de ramas

```text
{tipo}/{TICKET_ID}--{descripcion-en-kebab-case}
```text
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
[Dev crea rama + tech-design.md] → estado: en-progreso
        ↓
[QA / Testing] → estado: en-testing
        ↓
[Merge a develop] → estado: completado  ← DoD cumplido
        ↓
[PR develop → main + release] → estado: completado en producción
```text
> **Regla fundamental**: sin `spec.md` con estado `aprobado` en `docs/09-desarrollos/epicas/`, el desarrollo no comienza.

## Regla de revision por epica (MVP-x99)

Al dar de alta una epica MVP nueva, se crea tambien su historia de cierre `MVP-x99` con titulo `Revision epica`.

Reglas:

1. `MVP-x99` se ejecuta siempre al final de la epica.
2. `MVP-x99` depende de todas las historias funcionales previas de la epica.
3. La epica no puede marcarse como `completado` hasta cerrar `MVP-x99`.
4. Los ajustes y faltas detectados durante la epica se centralizan en `MVP-x99`.
5. De `MVP-x99` salen nuevas historias: a la epica correspondiente si encajan, o a `MVP-999` si son transversales o diferibles.

---

## Convenciones de commits

Seguimos **Conventional Commits**:

```text
{tipo}({scope}): {descripción corta en imperativo}

[cuerpo opcional — explica el QUÉ y el POR QUÉ, no el CÓMO]

[pie opcional — referencias a tickets]
```text
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
```text
---

## Alta documental previa obligatoria

Antes de crear rama de trabajo, el ticket debe tener alta documental en `09-desarrollos`.

Condiciones mínimas:

1. Carpeta épica y carpeta historia creadas con slug derivado automáticamente del título del ticket fuente.
2. `spec.md` de épica y `spec.md` de historia creados desde plantilla oficial.
3. Bloque `tickets.*` informado cuando exista ticket fuente.
4. Validación KB en verde.

> **Regla de bloqueo**: si la alta documental no cumple estos puntos, no se inicia rama de desarrollo.

---

## Proceso de Pull Request

1. La rama parte siempre de `develop`.
2. El nombre del PR incluye el ID del ticket: `[RF-E04-001] Implementación DummyProviderClient`.
3. El PR debe tener `spec.md` en estado `aprobado` antes de crear la rama.
4. El `tech-design.md` debe estar completo antes de solicitar revisión.
5. Si no hay otro developer disponible, el propio autor puede hacer la auto-revisión del PR; Copilot puede usarse como revisor complementario.
6. El CI debe pasar completamente (tests + validación de KB).
7. Hacer **squash merge** a `develop` para mantener el historial limpio.

## Promoción a `main`

1. Solo se puede abrir PR de `develop` hacia `main`.
2. Ese PR requiere revisión manual; puede ser auto-revisión si no hay otro revisor disponible.
3. El merge a `main` se usa únicamente para promover el estado ya validado en `develop`.
4. Se hace **merge commit**, no squash: `main` conserva el historial commit a commit para poder
   etiquetar puntos concretos.

### Promoción de hito frente a release a producción

No todo merge a `main` es una salida a producción. Se distinguen dos cosas, y conviene no leer `main`
como «desplegable en producción» sin mirar la etiqueta:

| | **Promoción de hito** | **Release a producción** |
|---|---|---|
| Cuándo | Al cerrar un hito del roadmap (una o varias épicas con su `MVP-x99` cerrada) | Cuando se supera el gate de salida |
| Requisito | Épicas del hito en `completado` y CI en verde | Gate de release del MVP: **`MVP-005`** (cobertura de tests, hardening, cumplimiento RGPD/LOPDGDD y checklist final) |
| Etiqueta | `vX.Y.0-hito-{letra}`, publicada como **pre-release** | `vX.Y.Z`, release normal |
| Uso | Desplegable en **fase cerrada** (piloto interno, demo, entorno de pruebas) | Desplegable a producción con usuarios reales |
| Notas | `docs/10-releases/vX.Y.0-hito-{letra}.md` | `docs/10-releases/vX.Y.Z.md` |

Reglas:

- Cada hito promocionado se **etiqueta con un tag anotado** sobre el commit que lo cierra, de modo que
  siempre haya una referencia concreta que desplegar en fase cerrada sin depender de la punta de
  `main`.
- El tag va acompañado de su documento en `docs/10-releases/`, con el estado de despliegue explícito.
- Un hito promocionado **no autoriza el despliegue a producción**: mientras `MVP-005` no esté
  entregada, cualquier despliegue es de fase cerrada.

Hitos promocionados hasta ahora:

| Tag | Hito | Épicas | Estado |
|---|---|---|---|
| `v0.1.0-hito-a` | Hito A — Base segura y multiusuario | `MVP-001` | Pre-release, fase cerrada |
| `v0.2.0-hito-b` | Hito B — Base operativa preparada | `MVP-002` | Pre-release, fase cerrada |
| `v0.3.0-hito-c` | Hito C — Registro operativo end-to-end | `MVP-003` | Pre-release, fase cerrada |

---

## Protecciones de ramas

`develop` y `main` están protegidas:

- Push directo prohibido.
- Requiere PR con revisión.
- Requiere CI en verde.
- No se puede reescribir el historial.

Protección adicional de `main`:

- Solo acepta PRs originados desde `develop`.
