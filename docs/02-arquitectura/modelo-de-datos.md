---
bloque: 02-arquitectura
documento: modelo-de-datos
actualizado_en: "2026-07-17"
---

# Modelo de Datos Global — Terrenario MVP

> Este documento describe las entidades y relaciones del modelo de datos compartido.
> El modelo de datos específico de cada módulo está en `../03-modulos/{modulo}/modelo-datos.md`.

---

## Diagrama entidad-relación

```mermaid
erDiagram
    USUARIO {
        uuid id PK
        string google_sub
        string nombre
        string email
        timestamp creado_en
        boolean activo
    }

    WORKSPACE {
        uuid id PK
        uuid owner_id FK
        string nombre
        timestamp creado_en
        timestamp actualizado_en
    }

    TRABAJO_USUARIO_WORKSPACE {
        uuid id PK
        uuid workspace_id FK
        uuid usuario_id FK
        boolean en_curso
        timestamp unido_en
    }

    TRABAJADOR {
        uuid id PK
        uuid workspace_id FK
        string nombre
        boolean activo
        uuid cuenta_usuario_id FK
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
        float latitud
        float longitud
        integer num_arboles
        jsonb metadatos_suelo
        timestamp creado_en
        timestamp actualizado_en
    }

    TEMPORADA {
        uuid id PK
        uuid workspace_id FK
        string nombre
        fecha inicio
        fecha fin
        boolean esta_cerrada
        timestamp creado_en
        timestamp actualizado_en
    }

    Cosecha {
        uuid idPK
        uuid workspace_id FK
        uuid terreno_id FK
        uuid temporada_id FK
        uuid trabajo_id FK
        string producto
        decimal peso_bruto_kg
        decimal rendimiento_pct -- null si no aplica (almendra, etc.)
        decimal litros_obtenidos -- null si se calcula desde rendimiento
        string destino
        timestamp creado_en
        timestamp actualizado_en
    }

    ACTIVIDAD {
        uuid id PK
        uuid workspace_id FK
        uuid trabajo_id FK
        uuid terreno_id FK
        uuid temporada_id FK
        uuid responsable_id FK
        fecha fecha_regstro
        decimal horas_dedicadas -- puede ser 0
        string tarea
        decimal coste_total_manual
        timestamp creado_en
        timestamp actualizado_en
    }

    COMPRA {
        uuid id PK
        uuid workspace_id FK
        string producto
        decimal cantidad_total
        decimal coste_total
        decimal precio_unitario -- calculado auto
        fecha fecha_compra
        timestamp creado_en
        timestamp actualizado_en
    }
    
    TRABAJO_CONSUMO {
        work_id PK
        uuid compra_id FK
        uuid terreno_id FK
        decimal cantidad_consumida
        timestamp creado_en
    }

    USUARIO ||--o{ TRABAJO_USUARIO_WORKSPACE : "pertenece a"
    WORKSPACE ||--o{ TRABAJO_USUARIO_WORKSPACE : "tiene miembros"
    WORKSPACE ||--o{ TRABAJADOR : "mantiene"
    WORKSPACE ||--o{ TERRENO : "contiene"
    WORKSPACE ||--o{ TEMPORADA : "define"
    WORKSPACE ||--o{ Cosecha : "registra"
    WORKSPACE ||--o{ ACTIVIDAD : "recopila"
    WORKSPACE ||--o{ COMPRA : "contiene"
    USUARIO ||--o| TRABAJO : "tiene cuenta vinculada"
    TERRENO ||--o{ Cosecha : "produce"
    TERRENO ||--o{ ACTIVIDAD : "recibe"
    TERMINO ||--o{ TRABAJO_CONSUMO : "recibe consumo de"

```

---

## Entidades principales

### USUARIO

**Tabla**: `usuarios`
**Módulo owner**: Auth

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `google_sub` | string | Sí | Sub del token Google OIDC (identificador externo único) |
| `nombre` | string | Sí | Nombre completo extraído de Google |
| `email` | string | Sí | Email obtenido de Google (no editable por el usuario en MVP) |
| `creado_en` | timestamp | Sí | Fecha de alta |
| `activo` | boolean | Sí | Permite baja/anonimización sin afectar datos históricos — cumple RGPD/LOPDGDD |

### WORKSPACE

**Tabla**: `workspaces`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `owner_id` | UUID (FK → usuarios.id) | Sí | Propietario del Workspace; solo uno por Workspace |
| `nombre` | string | Sí | Nombre descriptivo de la explotación (ej: "Explotación García") |
| `creado_en` | timestamp | Sí | Fecha de creación |
| `actualizado_en` | timestamp | Sí | Última actualización |

### TRABAJO_USUARIO_WORKSPACE (pertenencia)

**Tabla**: `usuarios_workspaces`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Sí | Al que pertenece |
| `usuario_id` | UUID (FK → usuarios.id) | Sí | Miembro |
| `en_curso` | boolean | Sí | True hasta que el usuario acepte la invitación explícitamente |
| `unido_en` | timestamp | Sí | Fecha de aceptación |

### TRABAJADOR

**Tabla**: `trabajadores`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Sí | Workspace propietario |
| `nombre` | string | Sí | Nombre completo visible en los registros de actividad |
| `activo` | boolean | Sí | False no se borra, solo se oculta de selectores para trabajo nuevo |
| `cuenta_usuario_id` | UUID (FK → usuarios.id) o null | No | Si el trabajador tiene cuenta propia vinculada |
| `tarifa_hora` | decimal(10,2) | No | Tarifa por defecto solo relevante si tiene cuenta (para calculo automatico de coste) |
| `creado_en` | timestamp | Sí | Fecha de creación del trabajador |
| `actualizado_en` | timestamp | Sí | Última modificación |

### TERRENO

**Tabla**: `terrenos`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Sí | Workspace propietario |
| `nombre` | string | Sí | Nombre descriptivo del terreno |
| `alias` | string | No | Alias corto para visualización rápida |
| `tipo_propiedad` | enum | Sí | `'propietario_proyecto'`, `'cedido_inquilinato'`, `'usufructo'` o similar |
| `propietario` | string | Necesario | Nombre del propietario real (campo informativo) |
| `referencia_catastral` | string | No | Identificador catastral si existe |
| `latitud` | decimal(10,7) | No | Latitud WGS84 |
| `longitud` | decimal(10,7) | No | Longitud WGS84 |
| `num_arboles` | integer | No | Número de árboles. Clave para KPI kg/arbol del dashboard |
| `metadatos_suelo` | JSONB | No | Metadatos adicionales libres (textura, riego, cultivo principal, etc.) |
| `creado_en` | actualizado_en | timestamp Sí | Filled automáticamente por infraestructura |
| `actualizado_en` | timestamp | Sí | Última modificación |

### TEMPORADA

**Tabla**: `temporadas`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Sí | Workspace propietario. Las temporadas se crean por Workspace, no globalmente |
| `nombre` | string | Sí | Etiqueta descriptiva (ej: "Cosecha 2026-2027") |
| `inicio` | date | Necesario | Fecha de inicio del periodo |
| `fin` | date | No | Fecha de fin opcional si no se sabe aÃºn |
| `esta_cerrada` | boolean | Sí | Cierre administrativo: NO bloquea edición de registros, solo marca el estado visualmente |
| `cultivo_activo` | string | No | Nombre del cultivo principal de la campaña (ej: "oliva", "almendra") |
| `creado_en` timestamp |Sí | Fecha de creación |
| `actualizado_en` | timestamp | Sí | Última modificación |

### COSECHA

**Tabla**: `cosechas`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Sí | Workspace propietario. Todos los datos operativos de cosecha son exclusivos por workspace |
| `terreno_id` | UUID (FK terrains.id) | Sí | Tereno donde se recolectó |
| `temporada_id` | UUID (FK temporada.id) | Necesario | Temporada a la que pertense el registro (obligatorio) |
| `producto` | string | Sí | Nombre del producto/colecta |
| `peso_bruto_kg` | decimal(10,2) | Sí | Peso bruto recibido siempre obligatorio. Base para calcular rendimiento y litros |
| `rendimiento_pct` | decimal(4,3) | No | (L/100kg o kg aceite/100kg). Se calcula si se conoce litros_obtenidos primero |
| `litros_obtenidos` | decimal(10,2) | Necesario | Litros de aceite obtenidos. Se calcula si rendimiento_pct está disponible. Acepto al menos uno obligatorio |
| `destino` | enum | Necesario | Uno fijo: `'aceite_personal'`, `'venta_aceituna'`, `'aceite_venta'`, `'sin_destino'`. Fijo en MVP, no configurable por usuario |
| `creado_en` | timestamp | Sí | Fecha de creación |
| `actualizado_en` | actualizado_por_lastedit` | timestamp | Última modificación y autor (usuario logueado) |

### ACTIVIDAD / TRABAJO DIARIO

**Tabla**: `actividades`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Sí | Workspace propietario |
| `terreno_id` | UUID (FK terrains.id) | Necesario | Tereno exacto donde se ejecutó la tarea |
| `temporada_id` | UUID (FK temporada.id) | Necesario | Temporada vinculada obligatoriamente |
| `responsable_id` | UUID (FK → trabajadores.id) | Sí | Trabajador asignado a la actividad |
| `fecha_registro` | date | Sí | Fecha del trabajo |
| `horas_dedicadas` | decimal(5,2) | Sí | Puede ser 0 (anotación rápida) pero se debe registrar. Sin límite máximo |
| `tarea` | string | Necesario | Descripción de la tarea ejecutada — en futuras versiones seleccionada desde catálogo maestro |
| `coste_total_manual` | decimal(10,2) | Necesario | Coste total manual obligatorio para trabajadores sin tarifa configurada. El usuario puede sobreescrir cualquier importe. Si el trabajador tiene cuenta con tarifa, se muestra por defecto `horas * tarifa_per` permite editarlo si desea|
| `creado_en` | timestamp | Sí | Fecha de creación |
| `actualizado_por_ultima_edicion` | string (FK a usuarios.id / nullable) | No | Indica quién último modificó el registro (útil para trazabilidad de trabajo cruzado medianoche) |

### COMPRA

**Tabla**: `compras`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `workspace_id` | UUID (FK → workspaces.id) | Necesario | Workspace propietario |
| `producto` | string | Sí | Producto/materia comprado |
| `cantidad_total` | decimal(10,2) | Sí | Cantidad total adquirida en la compra |
| `coste_total` | decimal(10,2) | Sí | Coste total pagado al proveedor |
| `precio_unitario` | decimal(10,4) | Calculable auto | Derivado directamente de `coste_total / cantidad_total` (redondeada a4 decimales) |
| `fecha_compra` | date | Sí | Fecha real de la compra al proveedor/tienda |
| `creado_en` | timestamp | Necesario | Fecha de entrada en sistema |
| `actualizado_en` | timestamp | Necesario | Última modificación |

### TRABAJO_CONSUMO (vinculación compra → terrenos)

**Tabla**: `trabajos_consumo`

| Campo | Tipo | Obligatorio | Descripción |
|-------|------|------------|-------------|
| `id` | UUID | Sí | Identificador único |
| `compra_id` | UUID (FK → compras.id) | Necesario | Compra de la que se imputa el consumo |
| `terreno_id` | UUID( terrains.id) | Necesario | Terreno donde se consumió el material |
| `cantidad_consumida` | decimal(10,2) | Sí | Cantidad aplicada a ese terreno (el total nunca tiene porque ser igual al compra.total, los excedentes desaparecen sin rastro) |
| `creado_en` | timestamp | Necesario | Fecha de asignación del consumo |

---

## Convenciones de base de datos aplicadas

| Convención | Descripción |
|-----------|-------------|
| Naming de tablas | snake_case, plural (ej: `trabajadores`, `cosechas`) |
| Primary keys | UUID v4 por defecto |
| Timestamps | `creado_en`, `actualizado_en` en todas las tablas |
| Soft deletes | Campo `eliminado_en` nullable (se usa activo/inactivo para trabajadores); NO se implementa soft delete para otras entidades en MVP |
| Todos los registros operativos tienen `workspace_id` FK | Esto garantiza aislamiento completo entre WorkSpaces y permite el selector de contexto en la UI |

## Decisiones de diseño relevantes

1. **trabajadores cuenta_usuario_id nullable:** un trabajador puede existir sin cuenta propia. Si Antonio crea a "Juan" como trabajador externo, `cuenta_usuario_id` será null. Si Carlos (con cuenta) ayuda, su fila de Trabajador tendrá `cuenta_usuario_id` apuntando a su Usuario → permitiendo la futura vinculación.
2. **coste_total_manual vs calculo:** el campo `coste_total_manual` es siempre obligatorio en ACTIVIDAD. Cuando el trabajador tiene cuenta con tarifa configurada, se rellena automáticamente (`horas * tarifa_hora = coste_calculado`) pero permite edición manual. Cuando no lo tiene (trabajador externo), Antonio completa el campo manualmente al crear el registro.
3. **pendiente de compra en aplicación:** la lógica de `precio_unitario` en compras permite calcular coste proporcional si se aplica a un trabajo concreto (cantidad_aplicada × precio_unitario). Si no existe ninguna compra previa del producto, los registros asociados quedan a coste 0 y son visibles, permitiendo completarlos después. No hay recálculo retroactivo de costes historiccs cuando llega la compra.

## Migraciones

> Las migraciones se gestionan con **Alembic (Python SQLAlchemy ORM)**.
> Ver proceso en `../05-infraestructura/ci-cd.md`.
