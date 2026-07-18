---
id: "ADR-0004"
titulo: "Acceso a datos MVP con EF Core y evolución a EF + Dapper"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["persistencia", "ef-core", "dapper"]
---

# ADR-0004 - Acceso a datos MVP con EF Core y evolución a EF + Dapper

## Estado

`aceptada`

## Contexto

Se requiere acelerar la entrega del MVP manteniendo una ruta de evolución para optimizar lecturas analíticas en fases posteriores.

## Decisión

1. En MVP se usa **Entity Framework Core code-first** como mecanismo principal de acceso a datos.
2. En post-MVP se permite evolucionar a modelo **híbrido EF Core + Dapper** para consultas analíticas complejas del dashboard.

## Alternativas consideradas

### Opción A: EF Core exclusivo

**Pros**: productividad alta, migraciones integradas, menor complejidad inicial.
**Contras**: tuning de consultas complejas puede requerir trabajo adicional.

### Opción B: Dapper exclusivo

**Pros**: control SQL total y rendimiento predecible.
**Contras**: mayor esfuerzo de desarrollo/mantenimiento en MVP.

### Opción C: Híbrido desde día 1

**Pros**: equilibrio entre productividad y control.
**Contras**: aumenta complejidad técnica temprana.

## Consecuencias

### Positivas

- MVP más rápido con menor carga de infraestructura de código.
- Ruta de evolución definida y explícita.

### Negativas / Trade-offs

- Necesidad de reglas claras al introducir Dapper en fases posteriores.

### Neutrales

- Se revisará el paso a C con métricas reales de rendimiento en producción.
