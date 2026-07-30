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
actualizado_en: "2026-07-30"
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
- **Integración.** Nuevo arnés de **API real** con `WebApplicationFactory` sobre **SQLite**: levanta
  el `Program.cs` de producción entero —autenticación JWT, middlewares, filtros de scope,
  controladores, handlers, dominio, EF— contra una base de datos de verdad.
- **Smoke E2E.** Un recorrido en secuencia por el núcleo del MVP (login → Workspace → temporada →
  maestros → labor → cosecha → compra → imputación → diario → dashboard), sobre ese mismo arnés.

Sin cambios en código de producción: esta historia **solo añade cobertura**. Los defectos que la
cobertura ha destapado se registran, no se arreglan aquí (ver «Hallazgos»).

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `src/frontend/terrenario-web/vitest.config.ts` | nuevo | Config del arnés de frontend, separada de la de build |
| `src/frontend/terrenario-web/src/test/` | nuevo | Preparación común (`setup.ts`) y doble del cliente HTTP (`http.ts`) |
| `src/frontend/terrenario-web/tsconfig.test.json` | nuevo | Comprobación de tipos de los tests, fuera del build de producción |
| `src/frontend/terrenario-web/package.json` | modificado | Dependencias del arnés y scripts `test`, `test:watch`, `test:coverage` |
| `src/backend/Terrenario.Api.Tests/Integration/` | nuevo | Arnés de integración (`TerrenarioApiFactory`, `ApiSession`) y sus tests |
| `src/backend/Terrenario.Api.Tests/*.csproj` | modificado | `Microsoft.AspNetCore.Mvc.Testing` |
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

`TerrenarioApiFactory : WebApplicationFactory<Program>` levanta la aplicación real. Solo se sustituyen
**dos** cosas, y las dos por un motivo declarado:

1. **PostgreSQL por SQLite en memoria**, una base por clase de test. Mantiene el arnés sin dependencia
   de Docker (decisión del PO al abrir la historia). El precio está registrado: ver «Lo que esta
   historia no cierra».
2. **Google** (`IGoogleOidcService`). Es un proveedor externo cuyo consentimiento no se puede
   automatizar. Todo lo demás del login —alta de usuario, emisión del JWT, cookie de refresco,
   resolución del Workspace— sí se ejercita.

Dos detalles de fontanería que costaron y conviene no volver a descubrir:

- Sustituir `DbContextOptions` **no basta**. Desde EF 9 cada `AddDbContext` deja registrada su acción
  de configuración (`IDbContextOptionsConfiguration<TContext>`) y **todas** se aplican al mismo objeto
  de opciones: sin retirar la de Npgsql, el contexto acaba con dos proveedores y EF se niega a
  arrancar. Hay que quitar esos descriptores, no solo las opciones.
- La base en memoria de SQLite vive mientras haya **una conexión abierta**. El arnés abre una en el
  constructor y la cierra al final; si no, el esquema desaparece entre peticiones.

`ApiSession` encapsula el token y la cabecera `If-Match`, para que los tests hablen de flujos («da de
alta un terreno») y no de cabeceras.

El **smoke E2E** es un solo test que recorre el flujo entero, a propósito. Trocearlo obligaría a
resembrar el estado en cada trozo y dejaría de comprobar lo único que aporta: que las piezas encajan
**en secuencia**, que es donde fallan los sistemas y donde no llegan los tests de handler.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Integración contra PostgreSQL real con Testcontainers | Decisión del PO: exige Docker en local y en CI y sube el tiempo de cada corrida. Deja `P-031` abierto, que queda registrado |
| Smoke E2E de navegador con Playwright | Decisión del PO. El login es Google OIDC y no se puede automatizar sin sembrar sesión inyectando un JWT; el coste no compensaba en esta pasada. Hueco registrado como `P-064` |
| Doblar `fetch` en vez del cliente HTTP en los tests de vista | Acoplaría cada test de vista al transporte, que ya tiene su propia cobertura. Los tests dirían menos y se romperían más |
| Trocear el smoke E2E en un test por paso | Cada trozo tendría que resembrar el estado y se perdería justo lo que el smoke aporta: la secuencia |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| SQLite no reproduce el SQL de PostgreSQL (tipos de fecha, índices funcionales) | alta | Registrado en `P-031`; el arnés cubre traducción de LINQ y contratos HTTP, que es donde estaban los fallos reales (`P-014`) |
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
| Backend (unitarios + repos SQLite) | 576 | 576 |
| Backend (integración + smoke E2E) | 0 | 19 |
| Frontend | **no existía arnés** | 70 |

## Hallazgos

Defectos reales que ha destapado la cobertura. Ninguno se corrige aquí: esta historia es de
cobertura, y arreglar de paso lo que los tests encuentran mezclaría el diagnóstico con la cura.

| Hallazgo | Qué pasa | Destino |
|---|---|---|
| `F-01` | `expiresLabel` calcula los días con `Math.ceil` sobre una fracción: una invitación que vence **hoy a las 18:00** rotula «Caduca mañana», y «Caduca hoy» solo aparece cuando **ya ha caducado**. El test lo fija como comportamiento actual, con nota | `P-065` en `MVP-999` |
| `F-02` | `react-router` 7.12–8.2 tiene un aviso de seguridad **high** (bypass de CSRF en modo RSC). La aplicación es una SPA y no usa modo RSC, así que no es explotable hoy, pero es una dependencia con CVE abierto de cara al gate de release | `MVP-502` (hardening) |
| `F-03` | No hay cobertura E2E de **navegador**: lo que se entrega es E2E de servidor | `P-064` en `MVP-999` |

## Lo que esta historia no cierra

- **`P-031`** (EF+SQLite y `ORDER BY` sobre `DateTimeOffset`) sigue **abierto**. La decisión de no usar
  PostgreSQL en el arnés lo mantiene tal cual: los órdenes en memoria que existen solo por el test no
  se pueden revertir todavía.
- **E2E de navegador**: ver `F-03`.

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Sin migraciones (esta historia no toca el esquema)
- [x] Tests escritos y pasando
- [x] `estrategia-testing.md` actualizada con el arnés real
- [x] Hallazgos registrados en `MVP-999`
- [x] Sin `TODO` sin resolver en este documento
