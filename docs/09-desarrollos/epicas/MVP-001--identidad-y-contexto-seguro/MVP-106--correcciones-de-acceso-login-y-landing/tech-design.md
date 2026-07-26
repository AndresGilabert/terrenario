---
id: "MVP-106"
tipo: feature
titulo: "TDD: Correcciones de acceso: login y landing"
estado: en-progreso
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["autenticacion", "landing", "calidad"]
  modulo_path: "03-modulos/"
  componentes: ["oauth-callback", "landing", "login-ui"]
  etiquetas: ["mvp", "auth", "ux", "correccion"]
  nivel_riesgo: medio
creado_en: "2026-07-26"
actualizado_en: "2026-07-26"
---

# TDD: MVP-106 — Correcciones de acceso: login y landing

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Historia de corrección de lo ya entregado en la épica MVP-001 (derivada del triage de MVP-199).
No introduce funcionalidad de negocio: sanea la **superficie de acceso** (landing + login +
callback OIDC) para que sea coherente y libre de información contradictoria. Tres frentes, todos
en el frontend (`src/frontend/terrenario-web`):

1. **Callback OIDC idempotente (CA-1, R-04).** El intercambio del `code` de Google debe ejecutarse
   una sola vez y no descartar los artefactos PKCE hasta confirmar el resultado, para que un login
   válido nunca renderice la pantalla "No se pudo iniciar sesión".
2. **Coherencia de CTAs en la landing (CA-2, R-01/R-02).** Un único patrón de acceso, sin la
   palabra "gratis", sin promesas no confirmadas, sin botones que digan "con Google" sin iniciarlo
   y sin el enlace de navegación roto "Funcionalidades".
3. **Enlaces legales del login sin destino roto (CA-3, R-03a).** Los enlaces "Política de
   Privacidad" y "Términos del Servicio" dejan de navegar a rutas inexistentes que el catch-all
   redirigía a la landing; quedan en un estado definido ("próximamente", deshabilitados) hasta que
   exista contenido legal (diferido a P-008 / MVP-999).

Adicionalmente se retira, de forma oportunista, el **código muerto** del botón "Acceder como
invitado / Demo" del login (P-009 de MVP-999), por estar en el mismo archivo que se sanea y no
cablearse nunca.

## Causa raíz del defecto del callback (CA-1)

`OAuthCallback` ejecuta el intercambio en un `useEffect(…, [])`. En React StrictMode (dev) el
efecto se invoca dos veces sobre el montaje inicial y, ante navegación, puede remontarse. La versión
entregada **borraba `oauth_state` y `pkce_code_verifier` antes** de lanzar el intercambio
(`OAuthCallback.tsx:45-46`). En la segunda pasada esos artefactos ya no estaban, se entraba en la
rama de validación `!codeVerifier` y se pintaba "Parámetros de autenticación inválidos" →
"No se pudo iniciar sesión", pese a que el intercambio de la primera pasada estaba resolviéndose
correctamente. Resultado: parpadeo de error en un login **válido** (información contradictoria).

## Diseño detallado

### 1. Callback OIDC idempotente (CA-1)

Dos medidas complementarias sobre `components/auth/OAuthCallback.tsx`:

- **Guarda de idempotencia por `code`.** Un `Set<string>` a nivel de módulo (`processedCodes`)
  registra los códigos en curso/consumidos. El `code` de Google es de un solo uso; ante doble
  montaje o remount, las pasadas posteriores detectan el `code` ya presente y salen temprano
  **manteniendo el spinner** ("Completando el acceso…"), sin volver a intercambiar ni tocar los
  artefactos. Sobrevive tanto al doble `useEffect` de StrictMode como a un remount real (donde un
  `useRef` se reiniciaría).
- **Descarte diferido de artefactos PKCE.** `oauth_state` y `pkce_code_verifier` solo se eliminan
  **una vez resuelto** el intercambio (en `.then` y en `.catch`), nunca antes. Defensa en
  profundidad: aunque una pasada concurrente escapara a la guarda, encontraría los artefactos
  presentes y no caería en la rama de "parámetros inválidos".

La guarda se comprueba **después** de descartar el flujo de `error` de Google y de leer `code`, y
**antes** de la validación de artefactos, que es donde se producía el falso negativo.

### 2. Coherencia de CTAs en la landing (CA-2)

Sobre `components/marketing/LandingPage.tsx`, un único patrón de acceso:

| Ubicación | Antes | Después |
| --------- | ----- | ------- |
| Navbar (enlace ghost) | "Ingresar" | *(eliminado)* |
| Navbar (botón) | "Empezar Gratis" | "Acceder" |
| Nav (ancla) | "Funcionalidades" → `#funciones` (sin sección destino) | *(eliminado)* |
| Hero (CTA principal) | "Empezar gratis con Google" (navegaba a `/login`, no iniciaba Google) | "Acceder a la plataforma" |
| Bloque de reclamos | "✅ Sin tarjeta de crédito" · "✅ Configuración en 2 minutos" | *(eliminado)* |
| CTA inferior | "Crear mi Workspace gratis" | "Acceder a la plataforma" |
| Footer | "Iniciar Sesión" | "Acceder" |

Se conserva el enlace de nav "Beneficios" (ancla `#beneficios` con sección destino real) y el badge
"Gestión agrícola sencilla" (no es una promesa de gratuidad). Todos los CTA siguen navegando a
`/login`, que es el único punto de acceso real.

### 3. Enlaces legales del login sin destino roto (CA-3)

Sobre `components/auth/LoginPage.tsx`, los `<a href="/privacidad">` / `<a href="/terminos">` (que
provocaban navegación completa a rutas inexistentes redirigidas a `/` por el catch-all de
`App.tsx:42`) se sustituyen por `<button type="button" disabled>` con `title="Disponible
próximamente"` y `aria-label` explicativo. No inician ninguna navegación ni recarga y comunican un
estado honesto hasta que exista el contenido legal (P-008, diferido).

### 4. Limpieza de código muerto del login (P-009)

Se elimina la prop `onDemoAccess` y el bloque "Acceder como invitado / Demo" de `LoginPage`. Nunca
se cableaba desde `App.tsx` (confirmado por búsqueda: la prop no se pasa en ningún punto), por lo
que su retirada no altera ningún flujo. `LoginPage` pasa de `React.FC<LoginPageProps>` a `React.FC`.

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `src/frontend/.../components/auth/OAuthCallback.tsx` | modificado | Guarda de idempotencia por `code` + descarte diferido de artefactos PKCE (CA-1) |
| `src/frontend/.../components/marketing/LandingPage.tsx` | modificado | Único patrón de acceso; retirada de "gratis", reclamos, "Ingresar", "Funcionalidades" y CTA "con Google" (CA-2) |
| `src/frontend/.../components/auth/LoginPage.tsx` | modificado | Enlaces legales deshabilitados ("próximamente") sin navegación (CA-3) + retirada del demo muerto (P-009) |

Sin cambios de backend, contratos API, base de datos ni rutas.

## Impacto en la usabilidad

- **Landing**: la reducción de 5 CTA redundantes a un único patrón ("Acceder" / "Acceder a la
  plataforma") elimina la duda entre botones equivalentes sin quitar ninguna vía de acceso real
  (todas llevaban a `/login`). No se rompe ningún recorrido.
- **Login**: los enlaces legales dejan de expulsar al usuario a la landing; el estado
  "próximamente" es menos funcional que un contenido real pero honesto y no rompe el flujo de
  acceso. El contenido legal llega con P-008.
- No se detectan roturas de usabilidad que requieran decisión de producto.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
| ----------- | ------------------- |
| Guarda con `useRef(false)` en vez de `Set` a nivel de módulo | El `useRef` se reinicia ante un remount real del componente; solo cubre el doble `useEffect` de StrictMode, no el "remount" que exige el spec |
| Seguir borrando los artefactos PKCE antes del intercambio y silenciar el error | Trata el síntoma, no la causa; deja el intercambio sin la defensa en profundidad del descarte diferido |
| Crear páginas legales mínimas ahora | Contenido legal validado está fuera de alcance (P-008 / MVP-999); aquí solo se corrige el comportamiento roto |
| Construir la sección "Funcionalidades" | Fuera de alcance; el spec retira el enlace, no crea la sección |
| Diferir la retirada del demo muerto a MVP-999 | Está en el mismo archivo que se sanea y no tiene consumidor; retirarlo ahora evita retrofit y elimina una prop muerta |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
| ------ | ------------ | ---------- |
| El `Set` de códigos crece durante la sesión de pestaña | baja | Volumen despreciable (un login = un `code`); vive solo hasta el reload de la página |
| Un reintento legítimo tras error queda bloqueado por la guarda | baja | Tras error el usuario va a `/login` y genera un `code` nuevo, no presente en el `Set` |
| El estado "próximamente" se percibe como incompleto | baja | Es honesto y transitorio; el contenido llega con P-008 (MVP-999) |

## Plan de testing

> Referencia: `docs/04-ingenieria/estrategia-testing.md`

El frontend no dispone todavía de arnés de tests unitarios (no hay `vitest`/`jest` en
`package.json`); la verificación se apoya en tipado, build y lint, más validación funcional manual.

- [x] `tsc -b` sin errores de tipos (incluye la retirada de la prop `onDemoAccess`).
- [x] `npm run build` (tsc + vite) en verde.
- [x] `npm run lint` (oxlint) sin advertencias nuevas (las 3 existentes son previas: el
  `exhaustive-deps` del `useEffect(…, [])` del callback es intencional y ya estaba).
- [ ] Validación funcional manual (QA de la historia):
  - CA-1: login válido con Google en modo dev (StrictMode) sin parpadeo de "No se pudo iniciar
    sesión"; acceso completado a `/app`.
  - CA-2: landing con un único patrón de acceso; sin "gratis", "Ingresar", "Funcionalidades" ni
    reclamos; ningún botón promete Google sin iniciarlo.
  - CA-3: enlaces legales del login sin navegación ni recarga; estado "próximamente".
- [ ] Tests E2E de la superficie de acceso: pendientes del arnés de E2E de la épica.

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Callback OIDC idempotente (guarda por `code` + descarte diferido de PKCE)
- [x] Landing con un único patrón de acceso coherente y sin enlace roto
- [x] Enlaces legales del login en estado definido sin navegación
- [x] Código muerto del demo retirado (P-009)
- [x] Build y lint en verde
- [x] Sin `TODO` sin resolver en este documento
- [x] P-009 marcado como resuelto en el registro de `MVP-999`; contenido legal (P-008) y copy de
  onboarding (P-010) permanecen diferidos
