---
id: "MVP-501"
tipo: feature
titulo: "Cobertura mínima de tests del núcleo MVP"
estado: completado
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
actualizado_en: "2026-07-31"
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

- [x] **CA-1**: Los tests unitarios críticos del dominio MVP están implementados y pasan en verde.
- [x] **CA-2**: Los tests de integración crítica del MVP están implementados y pasan en verde.
- [x] **CA-3**: Existe smoke E2E para los flujos mínimos exigidos por la estrategia de testing.
  **Matiz declarado**: el smoke entregado es E2E **de servidor** (API real de punta a punta), no de
  navegador. Ver Notas.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| App shell | docs/04-ingenieria/estrategia-testing.md | cubierto | Smoke E2E de servidor sobre el núcleo del MVP (19 tests de integración) |
| Build Vite | docs/04-ingenieria/estrategia-testing.md | cubierto | Evidencia: `npm run build` y `npm run lint` en verde |
| Vistas React | docs/04-ingenieria/estrategia-testing.md | parcial | 72 tests de Vitest sobre la lógica de decisión; sin E2E de navegador (`P-064`) |

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

## Resultado de la entrega (2026-07-30 · 2ª pasada 2026-07-31)

Diseño técnico completo en [tech-design.md](./tech-design.md).

| Suite | Antes | Después |
|---|---|---|
| Backend — unitarios y repositorios | 576 (repos sobre SQLite) | 576 (repos sobre **PostgreSQL real**) |
| Backend — integración y smoke E2E de servidor | 0 | 19 |
| Frontend | **no existía arnés** | 72 |

- **`P-012` + `P-023` resueltos**: el frontend tiene arnés (Vitest + Testing Library) y la lógica de
  decisión señalada está cubierta —cliente HTTP común, `NotificationsContext` con su tracking de
  «vistas», gating de `can_revoke`/`is_self`/canal en «Miembros y accesos», filtros y borrado del
  diario, y el filtro de destino post-login—.
- **`P-031` resuelto** (2ª pasada, decisión del PO 2026-07-31): el arnés pasa de SQLite a
  **PostgreSQL real en contenedor** (Testcontainers), y con él los 11 ficheros de tests de
  repositorio. Eso permite **revertir las ocho consultas de producción** que estaban escritas hacia
  atrás —ordenando en memoria lo que la base sabe ordenar, y en dos casos materializando la tabla
  entera para quedarse con una fila— solo para que el arnés pudiera ejecutarlas. Contrapartida
  aceptada: **los tests del backend exigen Docker**.

### Alcance de CA-3: E2E de servidor, no de navegador

Se entrega un recorrido en secuencia por el núcleo del MVP —login, Workspace, temporada, maestros,
labor, cosecha, compra, imputación, diario y dashboard— sobre la **API real**: mismo `Program.cs`,
misma autenticación, mismos filtros de scope y SQL de verdad. Lo único simulado es el intercambio con
Google, que es un proveedor externo.

Lo que **no** cubre es el cliente React en un navegador. Playwright quedó descartado en esta pasada
por decisión del PO: el login es Google OIDC y automatizarlo exige sembrar sesión inyectando un token
de desarrollo. El hueco se registra como **`P-064`** en `MVP-999` y debe tenerse presente al leer el
gate de `MVP-504`.

### Hallazgos derivados y su corrección

La cobertura nueva destapó tres cosas. **Decisión del PO (2026-07-31): los defectos se corrigen en
esta misma rama**, para no arrastrar deuda conocida al PR.

- **`F-01` — corregido aquí.** `expiresLabel` contaba los días con `Math.ceil` sobre una fracción, así
  que una invitación que vencía **hoy a las 18:00** rotulaba «Caduca mañana» y «Caduca hoy» solo
  aparecía cuando **ya había caducado** —momento en el que además era falso—. Ahora se cuenta en
  **días de calendario** y una invitación vencida se rotula «Caducada». `P-065` nace y se cierra en la
  misma pasada.
- **`F-02` — corregido aquí.** `react-router` 7.12–8.2 arrastra un aviso de seguridad **high**
  (`GHSA-qwww-vcr4-c8h2`). **No hay arreglo en la línea 7.x**: la corrección está en **8.3.0**, y
  `react-router-dom` no publica 8.x porque en v8 el paquete se consolidó en `react-router`. Se migra
  el frontend entero (28 ficheros) de `react-router-dom` a `react-router@8.3.0`. `npm audit` pasa de
  2 avisos *high* a **0 vulnerabilidades**.
- **`F-03` → `P-064`**: el hueco de E2E de navegador descrito arriba. **No es un defecto**, sino la
  consecuencia de la decisión de alcance sobre el arnés; queda pendiente de decisión de producto y es
  lo único que esta historia deja abierto.

### Verificación de la migración a `react-router` 8

Un salto de *major* no se da por bueno con el build en verde. Verificado en navegador real, con la
API y PostgreSQL de desarrollo levantados y sesión sembrada:

| Qué | Resultado |
|---|---|
| Landing y render inicial | correcto |
| `useNavigate` («Acceder» → `/login`) | correcto |
| Guardas `ProtectedRoute` + `RequireWorkspace` | correcto |
| Rutas anidadas y `Outlet` (shell `AppLayout`) | correcto |
| `NavLink` del lateral (navegación de cliente) | correcto |
| `useSearchParams` (persistencia de filtros del dashboard, MVP-405) | correcto |
| Ruta comodín `/app/*` → 404 dentro del shell | correcto |
| Consola del navegador | sin errores |
| Llamadas a la API | todas `200` |
