---
id: "ADR-0002"
titulo: "Arquitectura base MVP: monolito modular online-first"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["arquitectura", "mvp", "online-first"]
---

# ADR-0002 - Arquitectura base MVP: monolito modular online-first

## Estado

`aceptada`

## Contexto

El MVP prioriza entrega rápida, simplicidad operativa y trazabilidad. Además, se decidió explícitamente que offline/sync diferido queda fuera de alcance en MVP.

## Decisión

Se adopta arquitectura **monolito modular online-first**:

1. Una API backend única.
2. Módulos de dominio separados dentro del mismo despliegue.
3. Persistencia transaccional central.
4. Todas las operaciones de escritura se validan y confirman online.

## Alternativas consideradas

### Opción A: Monolito modular

**Pros**: menor complejidad operativa, entrega más rápida, consistencia transaccional simple.
**Contras**: escalado independiente por módulo no inmediato.

### Opción B: Microservicios desde el inicio

**Pros**: escalado independiente y aislamiento fuerte por dominio.
**Contras**: alta complejidad de operación para MVP.

### Opción C: Serverless por funciones

**Pros**: elasticidad automática y pago por uso.
**Contras**: complejidad de observabilidad y consistencia distribuida en fase temprana.

## Consecuencias

### Positivas

- Menor coste de operación en MVP.
- Menor tiempo hasta validación con usuarios reales.
- Diseño más simple para QA y soporte.

### Negativas / Trade-offs

- Futuro escalado por dominio requerirá evolución arquitectónica.

### Neutrales

- Se mantiene estrategia de módulos internos para facilitar futura extracción de servicios si fuera necesario.
