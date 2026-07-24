---
bloque: 04-ingenieria
documento: estandares-codigo
actualizado_en: "2026-07-24"
---

# Estándares de Código

> Estas convenciones son obligatorias para todo el código del proyecto.
> Los agentes de IA deben leer este documento antes de generar código.

---

## Principios generales

1. **Legibilidad sobre brevedad**: el código se lee más veces de las que se escribe
2. **Nombres descriptivos**: variables, funciones y clases deben revelar su intenciÃ³n
3. **Funciones pequeñas**: una función, una responsabilidad
4. **Sin comentarios de "qué"**: el código debe ser autoexplicativo; los comentarios explican el "por qué"

---

## Idioma de los identificadores

> Decisión completa y alternativas descartadas: [ADR-0009](../02-arquitectura/decisiones/ADR-0009--idioma-de-identificadores-en-codigo.md).

**Todo identificador del sistema se escribe en inglés.** La documentación se redacta en español,
pero **nunca traduce identificadores**: los cita literalmente en el idioma del código.

Redacción correcta en documentación:

> El login del usuario guarda el nombre visible en la columna `display_name` de la tabla `users`.

Aplica a: clases, interfaces, propiedades, variables, funciones, nombres de archivo, tablas y
columnas de base de datos, rutas de API, campos de request/response, códigos de error y nombres
de eventos.

**No aplica a**:

- Textos de interfaz y mensajes dirigidos al usuario final, que van en español por ser contenido.
- Los **valores** de los catálogos cerrados del dominio (`desconocido`, `venta_aceituna`,
  `planificada`, `invitado`...), que son vocabulario de negocio. El **nombre** del catálogo sí es
  un identificador y va en inglés (`harvest_destination`).

---

## Convenciones de naming

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Variables y funciones | camelCase inglés | `getPlotById` |
| Clases e interfaces | PascalCase inglés | `HarvestRecord` |
| Constantes | SCREAMING_SNAKE_CASE inglés | `MAX_RETRY_ATTEMPTS` |
| Archivos | kebab-case inglés | `harvest-record.service.cs` |
| Tablas de DB | snake_case plural inglés | `harvest_records` |
| Columnas de DB | snake_case inglés | `created_at`, `is_active` |
| Rutas de API | kebab-case plural inglés | `/api/v1/workspaces/active` |
| Códigos de error | SCREAMING_SNAKE_CASE inglés | `VALIDATION_REQUIRED_WORKSPACE_NAME` |

Los booleanos persistidos usan prefijo `is_` (`is_active`, `is_closed`).

---

## Linting y formateo

| Herramienta | Propósito | Configuración |
|------------|-----------|--------------|
| Roslyn Analyzers | Linting/analizador estático .NET | `.editorconfig` + reglas del proyecto |
| dotnet format | Formateo automático | `.editorconfig` |

**El CI falla si hay errores de linting o formateo.**

---

## Estructura de un módulo / servicio

```text
src/
├── {modulo}/
│   ├── domain/           # Entidades, value objects, reglas
│   ├── application/      # Casos de uso, comandos, queries
│   ├── infrastructure/   # Adaptadores, repositorios, migraciones
│   └── interfaces/       # Controllers, DTOs, mappers
```

---

## Manejo de errores

- Usar errores tipados, nunca `throw new Exception("mensaje genérico")` sin clasificar
- Nunca capturar y silenciar excepciones
- Los errores de dominio se propagan como excepciones de dominio
- Los errores de infraestructura se loguean y se transforman en errores de aplicación

---

## Seguridad en el código

- **Nunca** incluir credenciales, tokens o secrets en el cÃ³digo o en los tests
- Validar todas las entradas en los límites del sistema (controllers / API handlers)
- Usar consultas parametrizadas; **nunca** concatenar SQL
- Ver modelo de seguridad completo en `../07-seguridad/modelo-seguridad.md`

---

## Code smells a evitar

- Números mágicos sin nombre de constante
- Clases con más de 300 líneas
- Funciones con más de 3 parámetros (usar objetos)
- Condicionales anidados de más de 2 niveles
- Lógica de negocio en controllers o repositorios
