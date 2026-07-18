---
bloque: 04-ingenieria
documento: estandares-codigo
actualizado_en: "2026-07-18"
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

## Convenciones de naming

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Variables y funciones | camelCase | `getTerrenoById` |
| Clases e interfaces | PascalCase | `RegistroCosecha` |
| Constantes | SCREAMING_SNAKE_CASE | `MAX_RETRY_ATTEMPTS` |
| Archivos | kebab-case | `registro-cosecha.service.cs` |
| Tablas de DB | snake_case plural | `registros_cosecha` |

---

## Linting y formateo

| Herramienta | Propósito | Configuración |
|------------|-----------|--------------|
| Roslyn Analyzers | Linting/analizador estático .NET | `.editorconfig` + reglas del proyecto |
| dotnet format | Formateo automático | `.editorconfig` |

**El CI falla si hay errores de linting o formateo.**

---

## Estructura de un módulo / servicio

```
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
