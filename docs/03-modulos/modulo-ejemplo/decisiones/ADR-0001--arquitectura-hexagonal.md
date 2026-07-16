---
id: "ADR-0001"
titulo: "Usar arquitectura hexagonal en un modulo de ejemplo"
estado: aceptada
fecha: "2025-01-20"
decisores: ["@techlead", "@equipo-modulo"]
etiquetas: ["arquitectura", "modulo-ejemplo"]
---

# ADR-0001 — Usar arquitectura hexagonal en un modulo de ejemplo

> ADR de ejemplo ligado al modulo de ejemplo neutral.
> No debe interpretarse como una decisión arquitectural real del proyecto.

## Estado

`aceptada`

## Contexto

El modulo necesita integrar con multiples proveedores externos que pueden cambiar a lo largo del tiempo.
La logica de dominio debe poder testearse de forma aislada, sin depender de servicios externos ni de base de datos real.

## Decisión

Implementar el modulo con **arquitectura hexagonal (Ports & Adapters)**:

- La lógica de dominio no tiene dependencias externas
- Los proveedores externos son adaptadores intercambiables que implementan el puerto `ExternalGateway`
- Los repositorios implementan el puerto `EntityRepository`

## Alternativas consideradas

### Opción A: Arquitectura hexagonal

**Pros**: Dominio testeable en aislamiento, proveedores intercambiables, logica protegida de cambios externos.
**Contras**: Mayor complejidad inicial, más capas de abstracción.

### Opción B: Arquitectura en capas tradicional (MVC)

**Pros**: Más simple, equipo familiar con ella.
**Contras**: La logica de negocio termina acoplada a la implementacion externa, dificil de testear.

## Consecuencias

### Positivas

- Los tests unitarios del dominio no necesitan mocks de red ni base de datos
- Cambiar de proveedor externo requiere solo un nuevo adaptador, sin tocar el dominio

### Negativas / Trade-offs

- Curva de aprendizaje inicial para el equipo
- Más archivos y capas que un MVC simple
