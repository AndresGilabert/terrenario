---
bloque: 02-arquitectura
documento: modelo-de-datos
actualizado_en: "2026-07-17"
---

# Modelo de Datos Global - Terrenario MVP

> Modelo de datos canonico para MVP online-first, alineado con ADR-0001..ADR-0006 y acuerdos de sesion.

---

## Diagrama entidad-relacion

```mermaid
erDiagram
    USUARIO {
        uuid id PK
        string google_sub
        string nombre
        string email
        timestamp creado_en
        timestamp actualizado_en
        boolean activo
    }

    WORKSPACE {
        uuid id PK
        uuid owner_id FK
        string nombre
        timestamp creado_en
        timestamp actualizado_en
    }

    USUARIO_WORKSPACE {
        uuid id PK
        uuid workspace_id FK
        uuid usuario_id FK
        string rol
        boolean activo
        timestamp unido_en
    }

    TRABAJADOR {
        uuid id PK
        uuid workspace_id FK
        uuid cuenta_usuario_id FK
        string nombre
        boolean activo
        decimal tarifa_hora
        timestamp creado_en
        timestamp actualizado_en
    }

    TERRENO {
        uuid id PK
        uuid workspace_id FK
        string nombre
        string alias
        string tipo_propiedad
        string propietario
        string referencia_catastral
        decimal latitud
        decimal longitud
        integer num_arboles
        jsonb metadatos_suelo
        timestamp creado_en
        timestamp actualizado_en
    }

    TEMPORADA {
        uuid id PK
        uuid workspace_id FK
        string nombre
        date inicio
        date fin
        boolean esta_cerrada
        string cultivo_activo
        timestamp creado_en
        timestamp actualizado_en
    }

    COSECHA {
        uuid id PK
        uuid workspace_id FK
        uuid terreno_id FK
        uuid temporada_id FK
        date fecha
        string producto
        decimal kgs
        decimal rendimiento
        decimal litros
        string destino
        uuid created_by FK
        timestamp created_at
        uuid updated_by FK
        timestamp updated_at
        bigint version
        timestamp eliminado_en
    }

    ACTIVIDAD {
        uuid id PK
        uuid workspace_id FK
        uuid terreno_id FK
        uuid temporada_id FK
        uuid trabajador_id FK
        date fecha
        decimal horas
        string tarea
        decimal coste_manual
        uuid created_by FK
        timestamp created_at
        uuid updated_by FK
        timestamp updated_at
        bigint version
        timestamp eliminado_en
    }

    COMPRA {
        uuid id PK
        uuid workspace_id FK
        string producto
        decimal cantidad_total
        decimal coste_total
        decimal precio_unitario
        date fecha_compra
        uuid created_by FK
        timestamp created_at
        uuid updated_by FK
        timestamp updated_at
        bigint version
        timestamp eliminado_en
    }

    CONSUMO_COMPRA {
        uuid id PK
        uuid workspace_id FK
        uuid compra_id FK
        uuid terreno_id FK
        decimal cantidad_consumida
        timestamp creado_en
    }

    USUARIO ||--o{ USUARIO_WORKSPACE : participa_en
    WORKSPACE ||--o{ USUARIO_WORKSPACE : tiene_miembros
    WORKSPACE ||--o{ TRABAJADOR : mantiene
    WORKSPACE ||--o{ TERRENO : contiene
    WORKSPACE ||--o{ TEMPORADA : define
    WORKSPACE ||--o{ COSECHA : registra
    WORKSPACE ||--o{ ACTIVIDAD : registra
    WORKSPACE ||--o{ COMPRA : registra
    WORKSPACE ||--o{ CONSUMO_COMPRA : registra
    TERRENO ||--o{ COSECHA : produce
    TERRENO ||--o{ ACTIVIDAD : recibe
    TERRENO ||--o{ CONSUMO_COMPRA : consume
    TEMPORADA ||--o{ COSECHA : agrupa
    TEMPORADA ||--o{ ACTIVIDAD : agrupa
    TRABAJADOR ||--o{ ACTIVIDAD : ejecuta
    COMPRA ||--o{ CONSUMO_COMPRA : reparte
```

---

## Entidades y reglas clave

### TRABAJADOR

| Campo | Tipo | Obligatorio | Descripcion |
|-------|------|-------------|-------------|
| `cuenta_usuario_id` | UUID (nullable) | No | Permite vincular un trabajador a una cuenta del sistema cuando exista |
| `tarifa_hora` | decimal(10,2) | No | Valor de referencia para sugerencia de coste; no sustituye `coste_manual` en actividad |

### COSECHA

| Campo | Tipo | Obligatorio | Descripcion |
|-------|------|-------------|-------------|
| `kgs` | decimal(10,2) | Si | Obligatorio en todo registro de cosecha |
| `rendimiento` | decimal(10,4) | No | Opcional. Si viene informado, `litros` no debe enviarse |
| `litros` | decimal(10,2) | No | Opcional. Si viene informado, `rendimiento` no debe enviarse |
| `destino` | enum | Si | Catalogo fijo: `venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido` |
| `version` | bigint | Si | Control de concurrencia optimista para `If-Match` |

### ACTIVIDAD

| Campo | Tipo | Obligatorio | Descripcion |
|-------|------|-------------|-------------|
| `horas` | decimal(5,2) | Si | Debe ser `> 0` en MVP |
| `coste_manual` | decimal(10,2) | Si | Obligatorio en MVP. Se permite sugerir valor por tarifa y editar manualmente |
| `version` | bigint | Si | Control de concurrencia optimista para `If-Match` |

### COMPRA

| Campo | Tipo | Obligatorio | Descripcion |
|-------|------|-------------|-------------|
| `cantidad_total` | decimal(10,2) | Si | Cantidad total comprada |
| `coste_total` | decimal(10,2) | Si | Coste total pagado |
| `precio_unitario` | decimal(10,4) | Si | Derivado de `coste_total / cantidad_total` y persistido para trazabilidad |

### CONSUMO_COMPRA

| Campo | Tipo | Obligatorio | Descripcion |
|-------|------|-------------|-------------|
| `cantidad_consumida` | decimal(10,2) | Si | Cantidad imputada al terreno |

---

## Convenciones de persistencia (MVP)

| Convencion | Aplicacion |
|-----------|------------|
| Motor de base de datos | PostgreSQL |
| ORM y migraciones | EF Core code-first |
| Claves primarias | UUID |
| Trazabilidad minima | `created_by`, `created_at`, `updated_by`, `updated_at` |
| Concurrencia | `version` por registro operativo + `If-Match` |
| Borrado en entidades operativas | Logico mediante `eliminado_en` |
| Aislamiento multi-tenant | `workspace_id` obligatorio en entidades operativas |

---

## Reglas de consistencia funcional

1. El MVP opera 100% online. No existe esquema local de sincronizacion diferida.
2. El destino canonico no clasificado es `desconocido` (la UI puede mostrar alias "Sin destino").
3. Actividad siempre exige `coste_manual`; no se acepta modo solo calculado.
4. Cosecha exige `kgs` y acepta exactamente uno de `rendimiento` o `litros`.
5. El cierre de temporada no bloquea edicion de registros operativos.

---

## Evolucion post-MVP prevista

1. Introducir outbox/sync para escenarios offline con cola de errores.
2. Evaluar capa hibrida EF Core + Dapper en consultas analiticas de dashboard.
3. Endurecer estrategia de backup y restauracion con pruebas periodicas.
