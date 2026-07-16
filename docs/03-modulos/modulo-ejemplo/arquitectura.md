---
modulo: modulo-ejemplo
documento: arquitectura
actualizado_en: "2026-07-16"
---

# Modulo de ejemplo — Arquitectura Interna

> Documento de ejemplo. No describe arquitectura real del proyecto.

---

## Diagrama de componentes

```mermaid
flowchart TD
    API["API Layer\n(REST Controllers)"]
    APP["Application Layer\n(Use Cases / Commands)"]
    DOM["Domain Layer\n(Entities, Rules)"]
    INFRA_DB["Infrastructure\n(PostgreSQL Repository)"]
    INFRA_EXT["Infrastructure\n(External Adapter)"]
    INFRA_EVT["Infrastructure\n(Event Publisher)"]

    API --> APP
    APP --> DOM
    APP --> INFRA_DB
    APP --> INFRA_EXT
    APP --> INFRA_EVT
```

---

## Patrón arquitectural

### Arquitectura hexagonal (Ports & Adapters)

- **Domain layer**: entidades y reglas de negocio puras, sin dependencias externas
- **Application layer**: casos de uso / comandos, orquesta el dominio
- **Infrastructure layer**: adaptadores para DB, servicios externos y bus de eventos

**Justificación**: ver `decisiones/ADR-0001--arquitectura-hexagonal.md`

---

## Flujo principal: procesar una operacion

```mermaid
sequenceDiagram
    participant Cliente
    participant API
    participant UC as CreateEntityUseCase
    participant EXT as External Adapter
    participant DB as Repository
    participant EVT as Event Publisher

    Cliente->>API: POST /example-module/items
    API->>UC: CreateEntityCommand
    UC->>EXT: validate(payload)
    EXT-->>UC: ValidationResult
    UC->>DB: save(entity)
    UC->>EVT: publish(EntityCreated)
    UC-->>API: EntityDTO
    API-->>Cliente: 201 Created
```

---

## Decisiones técnicas relevantes

| ADR | Decisión |
|-----|----------|
| [ADR-0001](./decisiones/ADR-0001--arquitectura-hexagonal.md) | Arquitectura hexagonal |

---

## Configuración y variables de entorno

| Variable | Descripción | Requerida |
|----------|-------------|-----------|
| `EXAMPLE_PROVIDER` | Proveedor externo activo | Si |
| `EXAMPLE_API_KEY` | Credencial de integracion externa | Si |
| `EXAMPLE_WEBHOOK_SECRET` | Secreto para validar webhooks | Si |
| `EXAMPLE_EXPIRY_HOURS` | Horas hasta expirar entidades pendientes | No (default: 24) |
