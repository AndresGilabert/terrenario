---
id: "ADR-0005"
titulo: "Concurrencia online con bloqueo optimista y error 409"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["concurrencia", "api", "integridad"]
---

# ADR-0005 - Concurrencia online con bloqueo optimista y error 409

## Estado

`aceptada`

## Contexto

El MVP opera exclusivamente online. Existe riesgo de ediciones concurrentes sobre el mismo registro por usuarios del mismo Workspace.

## Decisión

Se adopta **control optimista de concurrencia**:

1. Las entidades críticas exponen versión de registro.
2. En `PATCH`/`DELETE` se exige versión esperada.
3. Si no coincide, la API devuelve `409 CONFLICT_VERSION_MISMATCH`.

## Alternativas consideradas

### Opción A: Last-write-wins

**Pros**: implementación simple.
**Contras**: puede ocultar pérdidas silenciosas de cambios.

### Opción B: Bloqueo optimista

**Pros**: evita sobrescritura silenciosa y mantiene integridad funcional.
**Contras**: requiere gestión de conflicto en cliente.

### Opción C: Híbrido por entidad

**Pros**: flexibilidad fina.
**Contras**: más complejidad de reglas para MVP.

## Consecuencias

### Positivas

- Comportamiento consistente en edición concurrente.
- Mejor trazabilidad funcional de conflictos.

### Negativas / Trade-offs

- Cliente debe manejar caso 409 y refresco de datos.

### Neutrales

- Se revisará política por entidad cuando existan datos de uso reales.
