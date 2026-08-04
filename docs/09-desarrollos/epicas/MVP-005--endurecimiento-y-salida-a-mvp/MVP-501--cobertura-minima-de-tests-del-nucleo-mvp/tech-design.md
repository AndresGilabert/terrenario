---
id: "MVP-501"
tipo: feature
titulo: "TDD: Cobertura mínima de tests del núcleo MVP"
estado: completado
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "testing"]
  modulo_path: "03-modulos/"
  componentes: ["unit-tests", "integration-tests", "smoke-e2e"]
  etiquetas: ["mvp", "testing", "quality-gate"]
  nivel_riesgo: alto
creado_en: "2026-07-30"
actualizado_en: "2026-07-31"
---

# TDD: MVP-501 — Cobertura mínima de tests del núcleo MVP

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

La historia monta **las dos capas que faltaban** de la pirámide de
[`estrategia-testing.md`](../../../../04-ingenieria/estrategia-testing.md) y deja la base como estaba:

- **Unitarios.** El backend ya los tenía (576 tests antes de esta historia) y el dominio crítico está
  cubierto; no había nada que construir. El **frontend no tenía arnés ninguno** desde `MVP-106`
  (`P-012`/`P-023`): se monta **Vitest + Testing Library** y se cubre la lógica de decisión que hasta
  hoy solo sostenían el tipado, el build y la QA manual.
- **Integración.** Nuevo arnés de **API real** con `WebApplicationFactory` sobre **PostgreSQL en
  contenedor**: levanta el `Program.cs` de producción entero —autenticación JWT, middlewares, filtros
  de scope, controladores, handlers, dominio, EF— contra el **mismo motor que producción**. Los tests
  de repositorio, que corrían sobre SQLite, se mueven al mismo arnés.
- **Smoke E2E.** Un recorrido en secuencia por el núcleo del MVP (login → Workspace → temporada →
  maestros → labor → cosecha → compra → imputación → diario → dashboard), sobre ese mismo arnés.

El núcleo de la historia **solo añade cobertura**. Los dos defectos que esa cobertura destapó se
corrigen también aquí por decisión del PO (2026-07-31): no pasar el PR con deuda conocida. Son
cambios acotados y con test de regresión propio (ver «Hallazgos y su corrección»).

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `src/frontend/terrenario-web/vitest.config.ts` | nuevo | Config del arnés de frontend, separada de la de build |
| `src/frontend/terrenario-web/src/test/` | nuevo | Preparación común (`setup.ts`) y doble del cliente HTTP (`http.ts`) |
| `src/frontend/terrenario-web/tsconfig.test.json` | nuevo | Comprobación de tipos de los tests, fuera del build de producción |
| `src/frontend/terrenario-web/package.json` | modificado | Dependencias del arnés y scripts `test`, `test:watch`, `test:coverage` |
| `src/backend/Terrenario.Api.Tests/Integration/` | nuevo | Arnés (`PostgresTestServer`, `RepositoryTestBase`, `TerrenarioApiFactory`, `ApiSession`) y sus tests |
| `src/backend/Terrenario.Api.Tests/*/*RepositorySqliteTests.cs` | renombrado | Pasan a `*RepositoryPostgresTests` sobre el arnés común (11 ficheros) |
| `src/backend/Terrenario.Api/Infrastructure/Data/Repositories/` | modificado | `P-031`: ocho órdenes en memoria devueltos a SQL |
| `src/backend/Terrenario.Api.Tests/*.csproj` | modificado | `Microsoft.AspNetCore.Mvc.Testing`; SQLite fuera, `Testcontainers.PostgreSql` dentro |
| `src/frontend/terrenario-web/src/lib/invitation-ui.ts` | modificado | Corrección de `F-01`: caducidad por días de calendario |
| `src/frontend/.../*.tsx` (28 ficheros) | modificado | Corrección de `F-02`: `react-router-dom` → `react-router@8` |
| `docs/04-ingenieria/estrategia-testing.md` | modificado | El arnés real, sus herramientas y el hueco de E2E de navegador |

## Diseño detallado

### Arnés de frontend

Vitest con entorno `jsdom` y `@testing-library/react`. Dos decisiones que conviene dejar dichas:

- **Config separada de `vite.config.ts`.** El build lleva Tailwind y las reglas de troceado del
  bundle; en un test no aportan nada y lo hacen más lento y más frágil. Vitest prioriza
  `vitest.config.ts` cuando existe.
- **Los tests se comprueban en `tsconfig.test.json`**, referenciado desde el `tsconfig.json` raíz y
  excluido de `tsconfig.app.json`. Así `npm run build` sigue sin cargar los tipos de Vitest, pero un
  error de tipos en un test **sí** rompe la compilación: es lo que se quiere de un gate.

Para las vistas se sustituye el **cliente HTTP** (`useApiClient`), no `fetch`. Lo que estos tests
cubren es la decisión de la vista —qué pide y qué acciones ofrece según lo que recibe—, no el
transporte, que tiene sus propios tests. El doble (`src/test/http.ts`) enruta por prefijo de path y
gana **el más específico**: `/api/v1/workspace-members` no puede quedarse con las llamadas a
`/api/v1/workspace-members/{id}/revoke` solo por estar declarada antes.

Qué se cubre y por qué:

| Módulo | Por qué está aquí |
|---|---|
| `services/http-client` | Punto único por el que pasa toda la operativa *scoped*: si su reacción a `AUTH_UNAUTHENTICATED` / `AUTH_WORKSPACE_SCOPE_REQUIRED` / `AUTH_WORKSPACE_FORBIDDEN` se rompe, la sesión deja de cerrarse o desvía sin motivo |
| `contexts/NotificationsContext` | El tracking de «vistas» en `localStorage` decide qué invitación se ofrece en el modal; es el caso que `P-012` nombra |
| `lib/post-login-redirect` | Único punto del cliente que filtra un destino de navegación (rechaza absolutos y *protocol-relative*) |
| `lib/invitation-ui` | Mensajes de aptitud del preview de invitación (MVP-107, R-C) |
| `components/members/MiembrosView` | La vista con más gating puro: `status` × `can_revoke` × `is_self` × `channel` (`P-023`) |
| `components/diary/DiarioView` | Vista principal del MVP y superficie que `MVP-506` reescribe: red de regresión de esa historia |

### Arnés de integración y smoke E2E

`TerrenarioApiFactory : WebApplicationFactory<Program>` levanta la aplicación real contra la base que
prepara `PostgresTestServer`. Lo **único** que se sustituye es **Google** (`IGoogleOidcService`): es un
proveedor externo cuyo consentimiento no se puede automatizar. Todo lo demás del login —alta de
usuario, emisión del JWT, cookie de refresco, resolución del Workspace— sí se ejercita.

`PostgresTestServer` levanta **un contenedor por ejecución** (`postgres:15-alpine`, la misma familia
que el entorno de desarrollo) y crea **una base de datos por clase de test**: las clases siguen
corriendo en paralelo sin pisarse los datos y el arranque del contenedor, que es lo caro, se paga una
vez. El esquema se crea aplicando las **migraciones reales** en vez de `EnsureCreated`, lo que de paso
valida que aplican limpias — algo que SQLite no podía comprobar de ninguna manera.

Dos detalles de fontanería que costaron y conviene no volver a descubrir:

- **El pool hay que acotarlo.** Con el pool por defecto (100 conexiones por origen) y ~20 clases en
  paralelo, PostgreSQL responde `53300: sorry, too many clients already` y el fallo aparece en
  `InitializeAsync`, donde se lee como un fallo del test y no del arnés. El arnés fija
  `MaxPoolSize=4` por base y arranca el contenedor con `max_connections=400`.
- **Un `Dispose()` heredado no basta**: la base se prepara en `InitializeAsync` (es asíncrona), así que
  cualquier campo que dependa de ella no puede inicializarse en el constructor.

`ApiSession` encapsula el token y la cabecera `If-Match`, para que los tests hablen de flujos («da de
alta un terreno») y no de cabeceras.

### `P-031` — Devolver a SQL lo que el test había echado a memoria

Este es el motivo por el que el arnés acabó en PostgreSQL y no en SQLite. EF+SQLite no traduce
`ORDER BY` sobre `DateTimeOffset`, y eso había dejado **ocho consultas de producción escritas hacia
atrás**, todas con el mismo comentario: «se ordena en memoria porque el test». Un test que obliga a
empeorar el código que prueba deja de ser una red de seguridad.

| Consulta | Cómo estaba | Cómo queda |
|---|---|---|
| `WorkspaceRepository.FindDefaultForUserAsync` | Traía **todas** las membresías activas para quedarse con una | `ORDER BY` + `LIMIT 1` en SQL |
| `WorkspaceRepository.FindOtherActiveOwnerAsync` | Traía **todos** los copropietarios para quedarse con uno | `ORDER BY` + `LIMIT 1` en SQL |
| `WorkspaceInvitationRepository.ListPendingAsync` | Sin `ORDER BY`; reordenaban dos handlers | `ORDER BY created_at DESC` en SQL |
| `WorkspaceReactivationRequestRepository.ListPendingAuthorizationsAsync` | Ordenaba tras materializar | `ORDER BY` antes de proyectar |
| `Activity` · `Harvest` · `Purchase` · `Consumption` `ListAsync` | Fecha en SQL, desempate por captura en memoria | Orden completo en SQL |

Las dos primeras no eran solo estética: materializaban la tabla para descartarla. Los dos `OrderBy`
redundantes que quedaban en `ListWorkspaceInvitationsHandler` y `ListWorkspacePeopleHandler` se
retiran también.

El riesgo de mover el desempate a SQL era que EF perdiera el orden al proyectar sobre un `JOIN`; los
tests de repositorio y el smoke E2E lo comprueban de punta a punta y pasan.

El **smoke E2E** es un solo test que recorre el flujo entero, a propósito. Trocearlo obligaría a
resembrar el estado en cada trozo y dejaría de comprobar lo único que aporta: que las piezas encajan
**en secuencia**, que es donde fallan los sistemas y donde no llegan los tests de handler.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Mantener SQLite en el arnés | Primera versión de esta historia. Se revierte al cerrar `P-031`: no compensa un arnés que obliga a degradar ocho consultas de producción. Contrapartida aceptada: los tests del backend **exigen Docker** |
| Smoke E2E de navegador con Playwright | Decisión del PO. El login es Google OIDC y no se puede automatizar sin sembrar sesión inyectando un JWT; el coste no compensaba en esta pasada. Hueco registrado como `P-064` |
| Doblar `fetch` en vez del cliente HTTP en los tests de vista | Acoplaría cada test de vista al transporte, que ya tiene su propia cobertura. Los tests dirían menos y se romperían más |
| Trocear el smoke E2E en un test por paso | Cada trozo tendría que resembrar el estado y se perdería justo lo que el smoke aporta: la secuencia |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Los tests del backend dejan de correr sin Docker | media | Contrapartida asumida al cerrar `P-031`, declarada en `estrategia-testing.md`. Docker ya estaba en el entorno de desarrollo (`terrenario-pg`) y lo hay en el CI |
| El contenedor alarga la ejecución de la suite | media | Un contenedor por ejecución y una base por clase: la suite completa (595 tests) tarda ~28 s |
| El smoke E2E se vuelve frágil al crecer el flujo | media | Un único test, con secciones numeradas y aserciones sobre contrato, no sobre textos de UI |
| Los tests de vista se acoplan a las clases de Tailwind | baja | Se consulta por rol, etiqueta accesible y texto visible; nunca por clase CSS |

## Plan de testing

- [x] Tests unitarios (backend): ya existentes, verificados en verde.
- [x] Tests unitarios (frontend): arnés nuevo, 70 tests sobre la lógica de decisión.
- [x] Tests de integración: API real sobre SQLite; scope de Workspace, aislamiento, errores del
      contrato, cabeceras transversales y filtros del diario.
- [x] Smoke E2E: recorrido del núcleo del MVP sobre la API real.

Cómo se ejecuta:

```bash
dotnet test src/backend/Terrenario.sln
```

```bash
npm test --prefix src/frontend/terrenario-web
```

## Resultado

| Suite | Antes | Después |
|---|---|---|
| Backend — unitarios y repositorios | 576 (repos sobre SQLite) | 576 (repos sobre **PostgreSQL real**) |
| Backend — integración y smoke E2E | 0 | 19 |
| Frontend | **no existía arnés** | 72 |

La suite completa del backend (595 tests, contenedor incluido) tarda unos **28 s**.

## Hallazgos y su corrección

La cobertura nueva destapó tres cosas. Los **dos defectos** se corrigen en esta rama por decisión del
PO (2026-07-31): no arrastrar deuda conocida al PR. El tercero no es un defecto.

### `F-01` — La etiqueta de caducidad mentía el último día

`expiresLabel` contaba con `Math.ceil` sobre una fracción de día. Consecuencia: cualquier invitación
con tiempo restante —aunque venciera esa misma tarde— rotulaba «Caduca mañana», y «Caduca hoy» solo
salía cuando **ya había caducado**, que es justo cuando el texto es falso.

Se pasa a contar **días de calendario** (diferencia entre medianoches locales), que es como cuenta
una persona: «mañana» es el día siguiente en el calendario, no «dentro de más de 24 horas». Una
invitación vencida se rotula **«Caducada»**, coherente con el badge `CADUCADA` que ya usa «Miembros y
accesos». El servidor no devuelve caducadas en la bandeja, pero una invitación puede vencer con la
pantalla abierta y ahí el texto tiene que decir la verdad.

Cubierto por seis casos, incluidos los dos límites que fallaban (vence esta tarde, vence de
madrugada) y el de fracción contra calendario (7 días y 10 horas ⇒ «7 días», no «8»).

### `F-02` — Aviso de seguridad *high* en `react-router`

`GHSA-qwww-vcr4-c8h2` afecta a `react-router` **7.12.0–8.2.0**. No es explotable en esta aplicación
(SPA sin modo RSC), pero es un CVE abierto que el gate de `MVP-504` tendría que justificar.

**No hay salida dentro de 7.x**: la corrección está en **8.3.0**, y `react-router-dom` no publica
8.x porque en v8 el paquete se consolidó en `react-router` (`react-router-dom` queda como legado de
v6/v7). Las tres opciones eran:

| Opción | Por qué se descartó / eligió |
|---|---|
| Bajar a `react-router-dom@7.11.0` | Lo que propone `npm audit fix --force`. Sale del rango vulnerable retrocediendo siete *minors*: cambia deuda de seguridad por deuda de versión |
| Aceptar el aviso con justificación | Deja el CVE vivo en el gate y obliga a re-justificarlo en cada release |
| **Migrar a `react-router@8.3.0`** | **Elegida.** Es la línea mantenida y elimina el aviso de raíz |

La migración es mecánica: los 12 símbolos que usa el proyecto —`BrowserRouter`, `MemoryRouter`,
`Routes`, `Route`, `Navigate`, `Outlet`, `Link`, `NavLink`, `useNavigate`, `useLocation`, `useParams`,
`useSearchParams`— los exporta `react-router` directamente. 28 ficheros cambian solo el especificador
del import. El agrupador de *chunks* de `vite.config.ts` (`node_modules[\\/]react-router`) sigue
casando sin tocarlo.

`npm audit` pasa de **2 avisos *high*** a **0 vulnerabilidades**.

#### Verificación de la migración

Un salto de *major* no se da por bueno con el build en verde. Verificado en navegador real, con API y
PostgreSQL de desarrollo levantados y sesión sembrada:

| Qué | Resultado |
|---|---|
| Landing y render inicial | correcto |
| `useNavigate` («Acceder» → `/login`) | correcto |
| Guardas `ProtectedRoute` + `RequireWorkspace` | correcto |
| Rutas anidadas y `Outlet` (shell `AppLayout`) | correcto |
| `NavLink` del lateral (navegación de cliente, sin recarga) | correcto |
| `useSearchParams` (persistencia de filtros del dashboard, MVP-405) | correcto |
| Ruta comodín `/app/*` → 404 dentro del shell | correcto |
| Consola del navegador | sin errores |
| Llamadas a la API | todas `200` |

### `F-03` — Sin E2E de navegador

**No es un defecto**: es la consecuencia de la decisión de alcance sobre el arnés (Playwright
descartado). Se registra como `P-064` y queda pendiente de decisión de producto.

## Lo que esta historia no cierra

Queda **un** punto, y no es un defecto del código entregado sino una decisión de alcance:

- **`P-064`** — E2E de **navegador**. Playwright sigue descartado (ver `F-03`). El gate de `MVP-504`
  debe leer «smoke E2E en verde» sabiendo que es de servidor.

`P-031` **sí queda cerrado**: era el otro punto de esta lista y se resolvió moviendo el arnés a
PostgreSQL real.

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Sin migraciones (esta historia no toca el esquema)
- [x] Tests escritos y pasando
- [x] `estrategia-testing.md` actualizada con el arnés real
- [x] Defectos detectados (`F-01`, `F-02`) corregidos y con regresión propia
- [x] Migración de *major* verificada en navegador real, no solo con el build
- [x] `npm audit` sin vulnerabilidades
- [x] Hallazgos y pendientes registrados en `MVP-999`
- [x] Sin `TODO` sin resolver en este documento
