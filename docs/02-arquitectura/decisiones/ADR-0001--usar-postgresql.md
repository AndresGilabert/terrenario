---
id: "ADR-0001"
titulo: "Ejemplo: Usar PostgreSQL como base de datos principal"
estado: aceptada
fecha: "2025-01-15"
decisores: ["@techlead", "@arquitecto"]
etiquetas: ["base-de-datos", "infraestructura"]
---

# ADR-0001 — Usar PostgreSQL como base de datos principal

## Estado

`aceptada`

## Contexto

El sistema necesita una base de datos relacional para almacenar los datos transaccionales
del negocio. Se evaluaron varias opciones teniendo en cuenta la experiencia del equipo,
los requisitos de consistencia ACID y el coste operativo.

## Decisión

Usamos **PostgreSQL 15** como base de datos principal para todos los servicios que
requieran persistencia de datos transaccionales.

## Alternativas consideradas

### Opción A: PostgreSQL

**Pros**: ACID completo, extensiones maduras (UUID, JSONB, full-text search), excelente soporte cloud, equipo con experiencia.
**Contras**: Escalado horizontal más complejo que NoSQL.

### Opción B: MySQL / MariaDB

**Pros**: Muy extendido, soporte amplio.
**Contras**: Características más limitadas que PostgreSQL (JSONB, CTEs recursivos), menos atractivo para el equipo.

### Opción C: MongoDB

**Pros**: Escalado horizontal sencillo, flexible para datos no estructurados.
**Contras**: Sin transacciones ACID en versiones antiguas, nuestro dominio es inherentemente relacional.

## Consecuencias

### Positivas

- Soporte completo de ACID y transacciones complejas
- JSONB para campos semi-estructurados sin sacrificar consistencia
- Excelente integración con los ORMs del stack actual

### Negativas / Trade-offs

- El escalado horizontal requiere soluciones adicionales (pgBouncer, read replicas)

### Neutrales

- El equipo debe mantenerse actualizado con las versiones LTS de PostgreSQL
