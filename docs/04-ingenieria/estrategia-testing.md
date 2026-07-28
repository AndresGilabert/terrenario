---
bloque: 04-ingenieria
documento: estrategia-testing
actualizado_en: "2026-07-18"
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
| xUnit | Runner de tests unitarios e integración |
| Playwright | Tests E2E |
| ASP.NET Core WebApplicationFactory | Tests de API/integración HTTP |

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
