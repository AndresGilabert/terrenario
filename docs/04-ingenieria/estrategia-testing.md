---
bloque: 04-ingenieria
documento: estrategia-testing
actualizado_en: "2026-07-30"
---

# Estrategia de Testing

---

## Pirámide de tests

```text
        /\
       /  \
      / E2E \          ← Pocos, lentos, cobertura de flujos críticos
     /--------\
    / Integration \    ← Tests de integración entre capas
   /--------------\
  /   Unit Tests   \   ← Muchos, rápidos, cobertura de lógica de dominio
 /------------------\
```

---

## Niveles y cobertura mínima

| Nivel | Qué testea | Cobertura mínima | Velocidad |
|-------|-----------|-----------------|-----------|
| **Unitario** | Lógica de dominio, use cases en aislamiento | 80% de la capa de dominio | < 1s por test |
| **Integración** | Interacción entre capas (API → DB, API → integraciones activas) | Flujos principales | < 10s por test |
| **E2E** | Flujos completos desde el cliente | Flujos críticos de negocio | < 60s por test |

---

## Convenciones de naming de tests

```csharp
public class {ClaseOFuncion}Tests
{
    [Fact]
    public void Deberia_{ComportamientoEsperado}_Cuando_{Condicion}()
    {
        // Arrange / Act / Assert
    }
}
```

Ejemplo:

```csharp
public class CosechaServiceTests
{
    [Fact]
    public void Deberia_Fallar_Cuando_SeInformanRendimientoYLitros()
    {
        // Arrange / Act / Assert
    }

    [Fact]
    public void Deberia_Fallar_Cuando_KgsEsMenorOIgualACero()
    {
        // Arrange / Act / Assert
    }
}
```

---

## Qué siempre debe tener tests

- Todas las reglas de negocio del dominio
- Todos los casos de error de la API (400, 401, 403, 409, 422, etc.)
- Los flujos felices de los principales casos de uso
- Cualquier bug corregido debe tener un test de regresión

---

## Herramientas

| Herramienta | Propósito |
|------------|-----------|
| xUnit | Runner de tests unitarios e integración (backend) |
| FluentAssertions · NSubstitute | Aserciones y dobles del backend |
| EF Core + SQLite (en memoria) | Repositorios y arnés de integración con SQL real |
| ASP.NET Core WebApplicationFactory | Tests de API/integración HTTP y smoke E2E de servidor |
| Vitest · Testing Library · jsdom | Tests unitarios y de vista del frontend |
| Playwright | Tests E2E de navegador — **no montado todavía**, ver más abajo |

## Arnés real del proyecto (MVP-501)

### Backend

- **Unitarios**: dominio y handlers, con repositorios doblados.
- **Repositorio sobre SQLite real** (no `InMemory`): ejercitan la traducción a SQL de EF y cazan los
  «could not be translated» que los mocks no ven. La lección viene de `P-014`, un `HTTP 500` en
  `GET /workspaces` que sobrevivió a 130 tests en verde.
- **Integración y smoke E2E**: `WebApplicationFactory` levanta el `Program.cs` real —autenticación
  JWT, middlewares, filtros de scope, controladores, EF— contra una base SQLite propia de cada clase
  de test. Solo se sustituyen la base de datos y el proveedor de identidad de Google.

```bash
dotnet test src/backend/Terrenario.sln
```

### Frontend

- **Vitest + Testing Library** sobre `jsdom`. Cubre la **lógica de decisión** —cliente HTTP, contextos,
  gating de acciones, filtros—, no la maquetación: se consulta por rol, etiqueta accesible y texto
  visible, nunca por clase CSS.
- Config propia (`vitest.config.ts`) separada del build, y tipos de test en `tsconfig.test.json`.

```bash
npm test --prefix src/frontend/terrenario-web
```

### Qué significa aquí «smoke E2E»

El smoke E2E entregado en `MVP-501` es **E2E de servidor**: recorre el núcleo del MVP (login,
Workspace, temporada, maestros, labor, cosecha, compra, imputación, diario y dashboard) de punta a
punta por la API real, pero **no ejercita el cliente React en un navegador**.

La cobertura de navegador con Playwright queda **pendiente y registrada** (`MVP-999`, `P-064`): el
login es Google OIDC y no puede automatizarse sin sembrar sesión inyectando un token de desarrollo.
Cualquier lectura del gate de despliegue debe tener presente esa distinción.

---

## Tests en el CI

- Los tests unitarios se ejecutan en cada push
- Los tests de integración se ejecutan en cada PR
- Los tests E2E se ejecutan en el pipeline de release
- Un PR no puede mergearse si los tests fallan

## Gate mínimo para deploy a producción (fase A)

Para permitir deploy a `prod` en fase A son obligatorios:

1. Tests unitarios en verde
2. Tests de integración crítica en verde
3. Smoke E2E en verde

## Nota operativa de coherencia

El equipo actual es de 1 persona. No se añaden procesos de QA que impliquen coordinación formal extra fuera del gate automático/manual de pipeline.

## Trazabilidad KB

1. Gate de despliegue y pipeline: `../05-infraestructura/ci-cd.md`
2. Proceso de release: `../08-procesos/proceso-release.md`
