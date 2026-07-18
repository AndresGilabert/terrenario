---
id: "ADR-0006"
titulo: "Contratos REST v1 y reglas de validación de cosecha en MVP"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["api", "validacion", "mvp"]
---

# ADR-0006 - Contratos REST v1 y reglas de validación de cosecha en MVP

## Estado

`aceptada`

## Contexto

Se necesita estabilizar contratos de API para MVP online y alinear validaciones con decisiones funcionales ya aprobadas.

## Decisión

1. La API del MVP se expone como REST versionada en `/api/v1`.
2. En cosecha, `kgs` es obligatorio.
3. `rendimiento` y `litros` son opcionales, pero no pueden coexistir en el mismo registro.
4. Catálogo de destinos cerrado con literal canónico `desconocido`.

## Alternativas consideradas

### Opción A: REST v1 + validaciones explícitas de dominio

**Pros**: simplicidad operativa, claridad contractual.
**Contras**: requiere gobernanza de versionado para cambios futuros.

### Opción B: GraphQL en MVP

**Pros**: flexibilidad de consulta.
**Contras**: complejidad adicional para equipo y seguridad en fase inicial.

### Opción C: Protocolos binarios internos/externos

**Pros**: eficiencia en throughput.
**Contras**: complejidad innecesaria para MVP.

## Consecuencias

### Positivas

- Contratos claros y testeables.
- Menor ambigüedad en validación de cosecha.
- Consistencia de taxonomía en analítica y dashboard.

### Negativas / Trade-offs

- Requiere disciplina de versionado para breaking changes.

### Neutrales

- Se evaluarán cambios de protocolo solo con evidencia de necesidad real.
