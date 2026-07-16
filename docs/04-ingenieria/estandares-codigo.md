---
bloque: 04-ingenieria
documento: estandares-codigo
actualizado_en: ""
---

# EstÃ¡ndares de CÃ³digo

> Estas convenciones son obligatorias para todo el cÃ³digo del proyecto.
> Los agentes de IA deben leer este documento antes de generar cÃ³digo.

---

## Principios generales

1. **Legibilidad sobre brevedad**: el cÃ³digo se lee mÃ¡s veces de las que se escribe
2. **Nombres descriptivos**: variables, funciones y clases deben revelar su intenciÃ³n
3. **Funciones pequeÃ±as**: una funciÃ³n, una responsabilidad
4. **Sin comentarios de "quÃ©"**: el cÃ³digo debe ser autoexplicativo; los comentarios explican el "por quÃ©"

---

## Convenciones de naming

| Elemento | ConvenciÃ³n | Ejemplo |
|----------|-----------|---------|
| Variables y funciones | camelCase | `getUserById` |
| Clases e interfaces | PascalCase | `PaymentTransaction` |
| Constantes | SCREAMING_SNAKE_CASE | `MAX_RETRY_ATTEMPTS` |
| Archivos | kebab-case | `payment-transaction.service.ts` |
| Tablas de DB | snake_case plural | `payment_transactions` |

---

## Linting y formateo

| Herramienta | PropÃ³sito | ConfiguraciÃ³n |
|------------|-----------|--------------|
| TODO (ESLint / Pylint / etc.) | Linting | `.eslintrc` / `pyproject.toml` |
| TODO (Prettier / Black / etc.) | Formateo automÃ¡tico | `.prettierrc` |

**El CI falla si hay errores de linting o formateo.**

---

## Estructura de un mÃ³dulo / servicio

```text
src/
├── {modulo}/
│   ├── domain/           # Entidades, value objects, reglas
│   ├── application/      # Casos de uso, comandos, queries
│   ├── infrastructure/   # Adaptadores, repositorios, migraciones
│   └── interfaces/       # Controllers, DTOs, mappers
```text
---

## Manejo de errores

- Usar errores tipados, nunca `throw new Error("mensaje genÃ©rico")`
- Nunca capturar y silenciar excepciones
- Los errores de dominio se propagan como excepciones de dominio
- Los errores de infraestructura se loguean y se transforman en errores de aplicaciÃ³n

---

## Seguridad en el cÃ³digo

- **Nunca** incluir credenciales, tokens o secrets en el cÃ³digo o en los tests
- Validar todas las entradas en los lÃ­mites del sistema (controllers / API handlers)
- Usar consultas parametrizadas â€” **nunca** concatenar SQL
- Ver modelo de seguridad completo en `../07-seguridad/modelo-seguridad.md`

---

## Code smells a evitar

- NÃºmeros mÃ¡gicos sin nombre de constante
- Clases con mÃ¡s de 300 lÃ­neas
- Funciones con mÃ¡s de 3 parÃ¡metros (usar objetos)
- Condicionales anidados de mÃ¡s de 2 niveles
- LÃ³gica de negocio en controllers o repositorios
