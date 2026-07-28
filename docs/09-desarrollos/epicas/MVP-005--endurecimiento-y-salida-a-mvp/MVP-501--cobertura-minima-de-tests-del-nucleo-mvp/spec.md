---
id: "MVP-501"
tipo: feature
titulo: "Cobertura mínima de tests del núcleo MVP"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-001", "MVP-002", "MVP-003", "MVP-004"]
bloquea: ["MVP-504"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "testing"]
  modulo_path: "03-modulos/"
  componentes: ["unit-tests", "integration-tests", "smoke-e2e"]
  etiquetas: ["mvp", "testing", "quality-gate"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-501 — Cobertura mínima de tests del núcleo MVP

## Contexto

La KB define un gate explícito para salida a producción: unit tests, integración crítica y smoke E2E en verde. Sin esta base, el MVP quedaría funcionalmente definido pero sin evidencia mínima de estabilidad.

## Objetivo

Cubrir el núcleo funcional del MVP con la batería mínima de tests requerida para permitir una salida controlada.

## Requisitos de usuario

### HU-1 — Validar reglas críticas del dominio

**Como** equipo técnico,
**quiero** que las reglas críticas del MVP estén cubiertas por tests,
**para** detectar regresiones antes de llegar a producción.

### HU-2 — Validar los flujos esenciales de extremo a extremo

**Como** responsable del despliegue,
**quiero** disponer de smoke tests E2E del núcleo,
**para** saber si el MVP es desplegable con un riesgo razonable.

## Alcance (in-scope)

- Tests unitarios del dominio crítico del MVP.
- Tests de integración de flujos y errores principales.
- Smoke E2E de login, captura diaria, cosecha, compra/imputación y dashboard.
- Alineación con umbrales y estrategia definidos en la KB.

## Fuera de alcance (out-of-scope)

- Cobertura exhaustiva de todos los edge cases no críticos.
- Performance testing profundo.
- Automatización compleja de QA fuera del gate mínimo.

## Criterios de aceptación

- [ ] **CA-1**: Los tests unitarios críticos del dominio MVP están implementados y pasan en verde.
- [ ] **CA-2**: Los tests de integración crítica del MVP están implementados y pasan en verde.
- [ ] **CA-3**: Existe smoke E2E para los flujos mínimos exigidos por la estrategia de testing.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| App shell | docs/04-ingenieria/estrategia-testing.md | parcial | Smoke manual posible sobre rutas MVP |
| Build Vite | docs/04-ingenieria/estrategia-testing.md | cubierto | Evidencia: npm run build ejecutado correctamente |

## Notas y decisiones

- Esta historia define el mínimo obligatorio, no una cobertura idealizada.
- **Puntos de `MVP-999` asignados aquí** (3ª pasada de `MVP-299`, 2026-07-28):
  - **`P-012` + `P-023`** — el frontend sigue **sin arnés de tests unitarios** (`vitest`/`jest`) desde
    `MVP-106`. Toda la lógica de decisión acumulada en las vistas de `MVP-104`/`MVP-107`/`MVP-204`/
    `MVP-208` (combinación de listados, gating por `can_revoke`/`is_self`, modo de la oferta de
    temporada, aptitud de invitaciones) está cubierta solo por tipado, build, lint y QA manual. Son el
    mismo punto registrado dos veces: se tratan como uno.
  - **`P-031`** — EF Core con SQLite no traduce `ORDER BY` sobre `DateTimeOffset`, lo que ya obligó a
    ordenar en memoria en cuatro consultas de `MVP-204` y `MVP-206`: se está degradando la consulta de
    producción para que el arnés la pueda ejercitar. Al montar la cobertura contra PostgreSQL hay que
    decidir un criterio único y revertir los órdenes en memoria que solo existan por el test.
  - La cobertura de **integración contra PostgreSQL** es además la que habría cazado `P-014` (el 500
    de `GET /workspaces`), que pasó 130 tests con repositorios mockeados.
