---
id: "ADR-0009"
titulo: "Idioma de los identificadores: inglés en el código, español en la documentación"
estado: aceptada
fecha: "2026-07-24"
decisores: ["@andres"]
etiquetas: ["convenciones", "modelo-de-datos", "api", "naming"]
---

# ADR-0009 — Idioma de los identificadores: inglés en el código, español en la documentación

## Estado

`aceptada`

## Contexto

La KB fija en `../../99-glosario/glosario.md` que los términos van "en inglés en el código y en
español en las conversaciones y documentación", pero nunca se concretó qué cuenta como "código".
El resultado es que el proyecto arrancó con criterios mezclados:

1. `../modelo-de-datos.md` define `WORKSPACE` y `USUARIO_WORKSPACE` con campos en español
   (`nombre`, `creado_en`, `unido_en`) pero define `COSECHA`, `ACTIVIDAD` y `COMPRA` con columnas
   de trazabilidad en inglés (`created_by`, `created_at`, `updated_at`, `version`) junto a
   `eliminado_en` en español.
2. Las clases y propiedades de C# se escribieron en inglés (`User.DisplayName`, `Workspace.Name`)
   mientras sus columnas se mapearon a español (`nombre`).
3. `../contratos-api.md` mezcla rutas y campos en español (`/terrenos`, `nombre`) con códigos de
   error en inglés (`VALIDATION_ACTIVITY_HOURS_RANGE`) y códigos híbridos
   (`VALIDATION_REQUIRED_TAREA_NOMBRE`).
4. `../../05-infraestructura/desarrollo-local.md` documentaba las columnas de `usuarios` en inglés
   mientras la migración real de MVP-101 las creaba en español.

Sin una regla explícita, cada historia nueva vuelve a tomar la misma decisión por su cuenta y la
deriva crece. El coste de fijarla es mínimo ahora (dos tablas implementadas, nada desplegado) y
crece con cada módulo del MVP.

## Decisión

**Todo identificador del sistema se escribe en inglés.** La documentación se sigue redactando en
español, pero **nunca traduce identificadores**: los cita literalmente en el idioma del código.

Alcance de "identificador":

| Artefacto | Convención | Ejemplo |
|---|---|---|
| Clases, interfaces y propiedades | PascalCase inglés | `WorkspaceMember.JoinedAt` |
| Variables y funciones | camelCase inglés | `getActiveWorkspace` |
| Tablas de base de datos | snake_case plural inglés | `workspace_members` |
| Columnas de base de datos | snake_case inglés | `joined_at`, `is_active` |
| Rutas de API | kebab-case plural inglés | `/api/v1/workspaces/active` |
| Campos de request/response | snake_case inglés | `access_token`, `name` |
| Códigos de error | SCREAMING_SNAKE_CASE inglés | `VALIDATION_REQUIRED_WORKSPACE_NAME` |
| Nombres de eventos | inglés | `workspace.member.invited` |

Ejemplo de redacción correcta en documentación:

> El login del usuario guarda el nombre visible en la columna `display_name` de la tabla `users`.

La frase describe el sistema en español; `display_name` y `users` se citan tal cual.

### Excepción: valores de catálogo del dominio

Los **valores** de los catálogos cerrados **se mantienen en español** porque son vocabulario de
negocio fijado por decisiones ya aceptadas, no identificadores de código:

- `harvest_destination`: `venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido`
- `season_status`: `planificada`, `activa`, `cerrada`
- `worker_member_status`: `invitado`, `activo`, `revocado`

`desconocido` está fijado como literal canónico en [ADR-0006](./ADR-0006--contratos-rest-v1-y-reglas-cosecha-mvp.md)
y en RN-012 de `../../01-producto/reglas-de-negocio.md`. El **nombre** del catálogo sí es un
identificador y va en inglés; sus **valores** son datos y se quedan como están. Cambiarlos exigiría
superseder ADR-0006 y revisar RN-004, RN-012 y RN-022.

## Alternativas consideradas

### Opción A: inglés en identificadores, español en documentación y valores de dominio

**Pros**: alineado con la convención ya escrita en el glosario; consistente con el ecosistema .NET
y con las herramientas; evita que cada historia reabra el debate; la documentación sigue siendo
legible para perfiles de negocio.
**Contras**: obliga a renombrar el esquema de MVP-101 y a reescribir el modelo de datos canónico.

### Opción B: español en base de datos, inglés en el código de aplicación

**Pros**: no exige tocar lo ya implementado; acerca el esquema al lenguaje del PO.
**Contras**: es exactamente el estado que ha generado la deriva; obliga a mapear dos idiomas en
cada entidad y deja indefinido el idioma de rutas, campos de API y códigos de error.

### Opción C: español en todo el stack

**Pros**: máxima cercanía al lenguaje ubicuo de negocio.
**Contras**: contradice el glosario vigente; choca con las convenciones del framework y con
identificadores externos ya en inglés (`access_token`, `google_sub`, `expires_at`).

## Consecuencias

### Positivas

- Un único criterio verificable en revisión de código para todo el MVP restante.
- El modelo de datos canónico deja de contradecirse a sí mismo.
- Elimina la capa mental de traducción entre entidad de dominio y tabla.

### Negativas / Trade-offs

- Migración de renombrado sobre el esquema de MVP-101 (`users`, `refresh_tokens`).
- Reescritura de `../modelo-de-datos.md` y `../contratos-api.md`, incluidas entidades todavía no
  implementadas, cuyos nombres en inglés quedan fijados antes de desarrollarlas.
- La documentación mezcla visualmente dos idiomas al citar identificadores.

### Neutrales

- Los textos de interfaz de usuario y los mensajes de error orientados al usuario final siguen en
  español: son contenido, no identificadores.
- Los valores de catálogo quedan como excepción explícita y revisable.

## Referencias

- `../../99-glosario/glosario.md` — convención de lenguaje ubicuo
- `../../04-ingenieria/estandares-codigo.md` — tabla de convenciones de naming
- `../modelo-de-datos.md` — modelo canónico afectado
- `../contratos-api.md` — contratos afectados
- [ADR-0006](./ADR-0006--contratos-rest-v1-y-reglas-cosecha-mvp.md) — literal canónico `desconocido`
