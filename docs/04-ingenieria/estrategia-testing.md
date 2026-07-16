---
bloque: 04-ingenieria
documento: estrategia-testing
actualizado_en: ""
---

# Estrategia de Testing

---

## Pirámide de tests

```text
        /\
       /  \
      / E2E \          <- Pocos, lentos, cobertura de flujos críticos
     /--------\
    / Integration \    <- Tests de integración entre capas
   /--------------\
  /   Unit Tests   \   <- Muchos, rápidos, cobertura de lógica de dominio
 /------------------\
```

---

## Niveles y cobertura mínima

| Nivel | Qué testea | Cobertura mínima | Velocidad |
|-------|-----------|-----------------|-----------|
| **Unitario** | Lógica de dominio, use cases en aislamiento | 80% de la capa de dominio | < 1s por test |
| **Integracion** | Interacción entre capas (API -> DB, API -> proveedor externo) | Flujos principales | < 10s por test |
| **E2E** | Flujos completos desde el cliente | Flujos críticos de negocio | < 60s por test |

---

## Convenciones de naming de tests

```text
describe('{ClaseOFunción}', () => {
  describe('{método o escenario}', () => {
    it('debería {comportamiento esperado} cuando {condición}', () => { ... })
  })
})
```

Ejemplo:

```text
describe('DomainEntity', () => {
  describe('refund()', () => {
    it('debería lanzar error cuando el estado no es capturada', () => { ... })
    it('debería lanzar error cuando el importe supera el original', () => { ... })
  })
})
```

---

## Qué siempre debe tener tests

- Todas las reglas de negocio del dominio
- Todos los casos de error de la API (400, 402, 409, etc.)
- Los flujos felices de los principales casos de uso
- Cualquier bug corregido debe tener un test de regresión

---

## Herramientas

| Herramienta | Propósito |
|------------|-----------|
| TODO (Jest / Pytest / etc.) | Runner de tests unitarios e integración |
| TODO (Playwright / Cypress) | Tests E2E |
| TODO (supertest / httpx) | Tests de API |

---

## Tests en el CI

- Los tests unitarios se ejecutan en cada push
- Los tests de integración se ejecutan en cada PR
- Los tests E2E se ejecutan en el pipeline de release
- Un PR no puede mergearse si los tests fallan
