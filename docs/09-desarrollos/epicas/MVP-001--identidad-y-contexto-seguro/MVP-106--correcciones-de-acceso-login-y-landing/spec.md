---
id: "MVP-106"
tipo: feature
titulo: "Correcciones de acceso: login y landing"
estado: aprobado
prioridad: alta
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-101", "MVP-103", "MVP-104", "MVP-105"]
bloquea: []
relacionado_con: ["MVP-199", "MVP-107"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["autenticacion", "landing", "calidad"]
  modulo_path: "03-modulos/"
  componentes: ["oauth-callback", "landing", "login-ui"]
  etiquetas: ["mvp", "auth", "ux", "correccion"]
  nivel_riesgo: medio
creado_en: "2026-07-25"
actualizado_en: "2026-07-25"
---

# MVP-106 — Correcciones de acceso: login y landing

## Contexto

La revisión de cierre de la épica (MVP-199) sobre el flujo entregado detectó varios
defectos de calidad en la superficie de acceso (landing + login) que no cumplen las
especificaciones de MVP-101 ni el objetivo de "baja fricción" de la épica:

- El callback de Google muestra momentáneamente la pantalla "No se pudo iniciar sesión"
  durante un login **válido**, generando información contradictoria.
- La landing presenta CTAs redundantes e incoherentes y un enlace de navegación roto.
- Los enlaces legales del login apuntan a rutas inexistentes que acaban redirigiendo a la
  propia landing.

Son correcciones de lo ya entregado en la épica, por lo que se resuelven dentro de MVP-001.

## Objetivo

Que la superficie de acceso (landing + login + callback OIDC) sea coherente y libre de
información contradictoria: un login válido nunca muestra un estado de error, la landing
ofrece un único mensaje de acceso claro y ningún enlace del acceso lleva a un destino roto.

## Requisitos de usuario

### HU-1 — Acceder con Google sin ver errores contradictorios

**Como** usuario que inicia sesión con Google,
**quiero** que un login correcto no muestre en ningún momento una pantalla de error,
**para** confiar en que el acceso ha funcionado.

### HU-2 — Entender de un vistazo cómo acceder desde la landing

**Como** visitante de la landing,
**quiero** un mensaje de acceso claro y sin promesas ambiguas,
**para** entrar a la plataforma sin dudar entre varios botones equivalentes.

### HU-3 — No encontrar enlaces rotos en el acceso

**Como** usuario en la pantalla de login o en la landing,
**quiero** que los enlaces me lleven a un destino coherente,
**para** no aterrizar por error en la página de inicio.

## Alcance (in-scope)

- **Callback OIDC idempotente** (punto 4 de MVP-199): el intercambio del código se ejecuta
  una sola vez de forma segura ante doble montaje o remount; los artefactos PKCE
  (`oauth_state`, `pkce_code_verifier`) no se descartan hasta confirmar el resultado, de modo
  que un login válido nunca renderiza el estado "No se pudo iniciar sesión".
  Referencia: `src/frontend/terrenario-web/src/components/auth/OAuthCallback.tsx`.
- **Coherencia de CTAs en la landing** (puntos 1 y 2 de MVP-199): un único patrón de acceso.
  - Quitar el enlace "Ingresar" del navbar.
  - Botón superior del navbar → "Acceder".
  - CTA central de la landing → "Acceder a la plataforma".
  - Eliminar la palabra "gratis" y los reclamos "Sin tarjeta de crédito" / "Configuración en
    2 minutos" mientras no sean una promesa de producto confirmada.
  - Ningún botón debe prometer "con Google" si en realidad solo navega a `/login` (el hero
    "Empezar gratis con Google" pasa a un texto de acceso neutro).
  - Retirar el enlace de navegación roto "Funcionalidades" (ancla `#funciones` sin sección
    destino). Referencia: `src/frontend/terrenario-web/src/components/marketing/LandingPage.tsx`.
- **Enlaces legales del login sin destino roto** (punto 3a de MVP-199): los enlaces "Política
  de Privacidad" y "Términos del Servicio" dejan de acabar en la landing. Hasta que exista
  contenido legal (diferido), se dejan en un estado definido y honesto (deshabilitados o con
  indicación de "próximamente") sin provocar recarga ni redirección a `/`.
  Referencia: `src/frontend/terrenario-web/src/components/auth/LoginPage.tsx`.

## Fuera de alcance (out-of-scope)

- Redacción y publicación del **contenido legal** (Política de Privacidad, Términos del
  Servicio) y el **consentimiento de cookies**: diferido y registrado como punto P-008 en
  `MVP-999` (destino propuesto MVP-005 / MVP-502).
- Construcción de una **sección de Funcionalidades** en la landing (se retira el enlace, no se
  crea la sección).
- Rediseño del flujo de invitaciones y notificaciones: es alcance de **MVP-107**.

## Criterios de aceptación

- [ ] **CA-1**: Un login válido con Google completa el acceso sin renderizar en ningún momento
  la pantalla "No se pudo iniciar sesión", incluso ante doble montaje del callback.
- [ ] **CA-2**: La landing presenta un único patrón de acceso coherente ("Acceder" /
  "Acceder a la plataforma"), sin la palabra "gratis", sin el enlace "Ingresar", sin el enlace
  roto "Funcionalidades" y sin ningún botón que prometa iniciar Google sin hacerlo.
- [ ] **CA-3**: Los enlaces "Política de Privacidad" y "Términos del Servicio" del login no
  redirigen a la landing ni provocan recarga; muestran un estado definido hasta que exista
  contenido legal.

## Diseño técnico

- Pendiente de `tech-design.md` en el refinamiento previo a la implementación.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/LandingPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LandingPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Notas y decisiones

- Historia derivada de la revisión de épica **MVP-199** (consolidación de puntos 1, 2, 3a y 4,
  más los hallazgos R-A y R-B).
- La corrección del callback afecta a MVP-101 (acceso con Google); el resto es UX de la
  superficie de acceso. No introduce nueva funcionalidad de negocio.
- Decisión con el PO (2026-07-25): el copy de acceso pierde la referencia "gratis"; el contenido
  legal se difiere y aquí solo se corrige el comportamiento roto de los enlaces.
