---
id: "ADR-0008"
titulo: "Infraestructura y observabilidad MVP en fase C"
estado: aceptada
fecha: "2026-07-18"
decisores: ["@po", "@tech-lead"]
etiquetas: ["infraestructura", "ci-cd", "observabilidad", "mvp"]
---

# ADR-0008 - Infraestructura y observabilidad MVP en fase C

## Estado

`aceptada`

## Contexto

La KB ya contenia decisiones operativas distribuidas sobre CI/CD, entornos y observabilidad, pero en `tech-stack.md` aparecian como "ADR pendiente".

Para eliminar ambigüedad de gobernanza en el arranque tecnico, se requiere consolidar y formalizar esas decisiones en un ADR unico para MVP fase C.

Fuentes alineadas:

1. `../../05-infraestructura/ci-cd.md`
2. `../../05-infraestructura/entornos.md`
3. `../../05-infraestructura/observabilidad.md`
4. `../vision-general.md`

## Decisión

Se formalizan las decisiones de infraestructura y observabilidad para MVP fase C:

1. **Docker 27.x** se adopta como base de entornos reproducibles local/CI.
2. **GitHub Actions** se adopta como pipeline CI y gestión de artefactos de CI.
3. **Sentry 8.x** se adopta como herramienta de error tracking operativo inicial.
4. **Terraform 1.9.x** queda definido en stack, con activación diferida a una fase posterior cuando el proyecto salga del modo MVP fase C.
5. **OpenTelemetry 1.x** queda definido en stack, con activación diferida y parametrización final en fase A.
6. La **retención detallada de telemetría** y umbrales finales de alertado se cierran al entrar en fase A.

> **Nota de realidad (2026-08-12, `P-129`).** De los cinco puntos, **tres no se han implementado** y
> conviene decirlo aquí, que es donde se buscan:
>
> | Punto | Estado real |
> |---|---|
> | 1. Docker 27.x | **Implementado**, con un alcance menor al enunciado: PostgreSQL local y los contenedores de la suite de tests (`Testcontainers`, `MVP-501`). No hay `docker-compose.yml` en el repositorio ni imagen de la aplicación: el despliegue publica el artefacto de `dotnet publish` sobre App Service |
> | 2. GitHub Actions | **Implementado** (`ci.yml`, `validar-kb.yml`, `deploy.yml`) |
> | 3. Sentry 8.x | **No implementado.** No hay dependencia ni cuenta. La observabilidad del MVP se construyó en `MVP-601`/`602`/`603` con telemetría en tablas propias, señales en `/api/v1/ops/signals` y alertas por correo |
> | 4. Terraform 1.9.x | **No implementado.** Producción se aprovisionó con scripts `az` en `infra/azure/` |
> | 5. OpenTelemetry 1.x | **No implementado.** Se mantiene el diferimiento |
>
> El punto 3 es el único que no era un diferimiento explícito, así que era el único que la KB
> presentaba como adoptado sin serlo. Si Sentry sigue siendo el objetivo o si la observabilidad
> propia lo sustituye de forma definitiva es una decisión abierta para fase A: hasta entonces este
> ADR **no describe el sistema desplegado** en sus puntos 3 a 5.

## Alternativas consideradas

### Opción A: Crear ADRs separados por herramienta

**Pros**: granularidad alta por tecnología.
**Contras**: mayor sobrecarga documental en etapa MVP.

### Opción B: Mantener todo como "pendiente"

**Pros**: máxima flexibilidad temprana.
**Contras**: huecos de trazabilidad y ambigüedad en decisiones ya aplicadas.

### Opción C: ADR único de consolidación (seleccionada)

**Pros**: cierra huecos rápido, mantiene trazabilidad y evita sobredocumentación.
**Contras**: menos granular que separar un ADR por cada herramienta.

## Consecuencias

### Positivas

- Se elimina el estado "ADR pendiente" para decisiones ya operativas.
- Se alinea el stack tecnico con la realidad documentada en infraestructura.
- Se conserva flexibilidad controlada para Terraform/OpenTelemetry con defer explícito.

### Negativas / Trade-offs

- Será necesario abrir ADR complementario o actualización de este ADR al entrar en fase A para cerrar detalle de telemetría.

### Neutrales

- No altera decisiones previas de backend, base de datos, contratos ni concurrencia.
