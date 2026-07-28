---
id: "ADR-0001"
titulo: "Usar PostgreSQL como base de datos relacional del MVP"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["base-de-datos", "persistencia", "mvp"]
---

# ADR-0001 - Usar PostgreSQL como base de datos relacional del MVP

## Estado

`aceptada`

## Contexto

Durante la revisión técnica del MVP se acordó no heredar decisiones de plantilla sin validación explícita del proyecto.

Para Terrenario MVP se requiere:

1. Integridad transaccional fuerte para datos operativos.
2. Consultas agregadas fiables para KPIs del dashboard.
3. Encaje con backend .NET 10 en arquitectura monolito modular online-first.

## Decisión

Se adopta **PostgreSQL 15** como base de datos relacional principal del MVP.

Alcance:

1. Persistencia transaccional de entidades de negocio.
2. Aislamiento por `workspace_id` en tablas operativas.
3. Soporte para crecimiento post-MVP sin cambio de motor.

## Alternativas consideradas

### Opción A: PostgreSQL

**Pros**: ACID completo, extensiones maduras (UUID, JSONB, full-text search), excelente soporte cloud, equipo con experiencia.
**Contras**: Escalado horizontal más complejo que NoSQL.

### Opción B: SQL Server

**Pros**: Integración nativa con ecosistema .NET y tooling maduro.
**Contras**: Coste de licencia/operación potencialmente superior según entorno.

### Opción C: MariaDB / MySQL

**Pros**: Hosting amplio y coste competitivo.
**Contras**: Menor flexibilidad para analítica relacional avanzada frente a PostgreSQL.

## Consecuencias

### Positivas

- Soporte completo de ACID y transacciones complejas
- Buen encaje con EF Core en backend .NET 10
- Capacidad analítica sólida para consultas KPI

### Negativas / Trade-offs

- El escalado horizontal requiere diseño adicional (réplicas, particionado, tuning)

### Neutrales

- El equipo debe mantener disciplina de migraciones y versionado de esquema
