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
