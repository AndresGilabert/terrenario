---
id: "MVP-199"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-101", "MVP-102", "MVP-103", "MVP-104", "MVP-105"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "scope-control"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "qa", "stabilization"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-07-26"
---

# MVP-199 — Revision epica

## Contexto

Durante la ejecucion de una epica aparecen ajustes, puntos ciegos y necesidades no previstas en las historias originales. Si no se centralizan antes del cierre, se dispersan y se pierde trazabilidad para decidir el trabajo posterior.

## Objetivo

Ejecutar una revision final de la epica para validar el funcionamiento global, consolidar los pendientes detectados y convertirlos en nuevas historias planificables.

## Requisitos de usuario

### HU-1 — Consolidar pendientes de la epica

**Como** Product Owner,
**quiero** reunir en un solo punto los ajustes y requisitos detectados durante la epica,
**para** evitar omisiones y cerrar el alcance con trazabilidad.

### HU-2 — Verificar calidad funcional final

**Como** equipo de producto y desarrollo,
**quiero** revisar el estado final de la epica sobre el flujo integrado,
**para** abrir nuevas historias concretas con evidencias de error o falta.

## Alcance (in-scope)

- Revision integral del comportamiento entregado por la epica.
- Consolidacion de puntos ciegos y requisitos pendientes detectados durante las historias previas.
- Creacion de nuevas historias para cubrir errores, faltas o ajustes detectados.
- Priorizacion inicial de los nuevos items segun impacto funcional y de negocio.

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los nuevos cambios detectados.
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [x] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
      Revision del 2026-07-25 sobre el frontend real (`src/frontend/terrenario-web`) y el backend
      (`src/backend/Terrenario.Api`); evidencias por archivo y linea en el registro de triage.
- [x] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
      Consolidados los 7 puntos reportados por el PO y los hallazgos adicionales R-A..R-I en el
      registro de triage de esta historia.
- [x] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique.
      Creadas `MVP-106` y `MVP-107` en esta epica; los diferidos quedan en `MVP-999` (P-008..P-010);
      R-I ya estaba cubierto por T-001/P-004.

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Registro de triage de la epica en curso

Usa esta seccion para decidir cuanto antes los puntos de alcance critico detectados dentro de MVP-001, sin diferirlos a fases finales del MVP.

| Punto | Fecha deteccion | Origen (epica/historia) | Tipo | Descripcion breve | Impacto | Bloqueante | Estado de revision | Decision esperada |
|---|---|---|---|---|---|---|---|---|
| T-001 | 2026-07-24 | MVP-001 / MVP-102 / MVP-104 | funcional | Gestion del ciclo de vida del Workspace: hoy existe alta (`POST /api/v1/workspaces`) y cambio de activo (`PUT /api/v1/workspaces/active`), pero no hay plan explicito para edicion (renombrado/ajustes) ni eliminacion (baja logica o fisica) de Workspaces existentes. Definir alcance MVP/post-MVP, reglas de seguridad (quien puede hacerlo), precondiciones (workspace activo, miembros, datos historicos) y contrato API/UI. | alto | si | resuelto (derivado fuera de MVP-001) | 2a revision 2026-07-26: la 1a pasada creo la creacion de adicionales (P-013) dentro de MVP-107 pero no la edicion/eliminacion, dejando una asimetria. Decision del PO (2026-07-26): NO se resuelve en MVP-001; el ciclo de vida (renombrar/eliminar) se traslada a **MVP-002 / MVP-204** junto a P-002/P-003 (= P-004 en MVP-999). MVP-001 cierra sin este flujo |
| R-01 (punto 1) | 2026-07-25 | MVP-001 / MVP-101 | ux | Landing: enlace "Funcionalidades" apunta al ancla `#funciones` sin seccion destino (`LandingPage.tsx:23`); no hace scroll. | bajo | no | aprobado-crear-historia | Arreglar ya en la epica: retirar el enlace. Historia MVP-106 |
| R-02 (punto 2) | 2026-07-25 | MVP-001 / MVP-101 | ux | Landing: 5 CTAs de acceso redundantes ("Ingresar", "Empezar Gratis", "Empezar gratis con Google", "Crear mi Workspace gratis", "Iniciar Sesion"); el hero "con Google" no inicia Google, solo navega a `/login`. | medio | no | aprobado-crear-historia | Coherencia de copy (quitar "Ingresar", "Acceder"/"Acceder a la plataforma", sin "gratis"). Historia MVP-106 |
| R-03 (punto 3) | 2026-07-25 | MVP-001 / MVP-101 | ux/legal | Login: enlaces "Politica de Privacidad" y "Terminos del Servicio" (`LoginPage.tsx:119,123`) apuntan a `/privacidad` y `/terminos` inexistentes; el catch-all (`App.tsx:42`) los redirige a la landing. Ademas no existe contenido legal para el usuario final. | medio | no | aprobado-crear-historia | Split: comportamiento roto de enlaces -> MVP-106; contenido legal + cookies -> diferido P-008 (MVP-999) |
| R-04 (punto 4) | 2026-07-25 | MVP-001 / MVP-101 | bug | Callback OIDC muestra "No se pudo iniciar sesion" durante un login valido: `OAuthCallback.tsx:45-46` borra `oauth_state`/`pkce_code_verifier` en la 1a pasada; ante doble montaje (StrictMode/remount) la 2a pasada pinta el error antes de que resuelva el intercambio. | alto | no | aprobado-crear-historia | Arreglar YA en la epica (mandato del PO). Historia MVP-106 |
| R-05 (punto 5) | 2026-07-25 | MVP-001 / MVP-103 | ux | Pantalla de invitacion: "Usar otra cuenta" hace logout (fuera de lugar); "Unirme al Workspace" falla con 403 `AUTH_INVITATION_EMAIL_MISMATCH` (`WorkspaceInvitation.cs:87`) solo tras pulsar porque el preview no valida el email. | alto | no | aprobado-crear-historia | Rediseno de la aceptacion con validacion previa. Historia MVP-107 |
| R-06 (punto 6) | 2026-07-25 | MVP-001 / MVP-103 | funcional | No se puede RECHAZAR una invitacion: no hay endpoint ni UI (solo preview/accept/create/list). El invitado con email no coincidente solo puede volver al login: imposibilidad de acceder. | alto | si | aprobado-crear-historia | Anadir rechazo (endpoint + UI) y salida a mis Workspaces/onboarding. Historia MVP-107 |
| R-07 (punto 7) | 2026-07-25 | MVP-001 / MVP-103 | ux/funcional | El flujo obliga a decidir en el momento (pagina-gate). No hay modal descartable, ni centro de notificaciones (campanita), ni bandeja de invitaciones RECIBIDAS por el usuario (solo listado de emitidas por el Workspace). | medio | no | aprobado-crear-historia | Entra completo en la epica (decision del PO): modal no bloqueante + campanita + bandeja. Historia MVP-107 |
| R-C | 2026-07-25 | MVP-001 / MVP-103 | tecnico | El preview de invitacion (`PreviewInvitationHandler`) no informa si la cuenta autenticada puede aceptar; el mismatch se descubre tras pulsar aceptar. | medio | no | aprobado-crear-historia | Ampliar el contrato del preview. Va con MVP-107 |
| R-D | 2026-07-25 | MVP-001 / MVP-103 | funcional | No existe endpoint que liste invitaciones RECIBIDAS por la cuenta autenticada; solo `GET /api/v1/workspaces/invitations` (emitidas). Imprescindible para la campanita/bandeja. | medio | no | aprobado-crear-historia | Nuevo endpoint backend. Va con MVP-107 |
| R-E | 2026-07-25 | MVP-001 / MVP-101 | tecnico | Codigo muerto: boton "Acceder como invitado / Demo" (`LoginPage.tsx:105-114`) nunca se cablea (`onDemoAccess` sin pasar en `App.tsx`). | bajo | no | aprobado-crear-historia | Limpieza -> diferido P-009 (MVP-999) o retirada oportunista en MVP-106 |
| R-F | 2026-07-25 | MVP-001 / MVP-102 | ux | Onboarding muestra "Paso 1 de 3" pero los pasos 2-3 (MVP-201) no existen aun; puede confundir en el build actual. | bajo | no | aprobado-crear-historia | Ajuste de copy -> diferido P-010 (MVP-999) o ajuste en MVP-106 |
| R-G | 2026-07-25 | MVP-001 / MVP-104 / MVP-105 | gobernanza | Inconsistencia de estado: MVP-104 y MVP-105 figuran `en-progreso` en `_indice.md` y en su front-matter, pero todos sus CA estan marcados `[x]`. | bajo | no | resuelto | Resuelto en esta revision (decision del PO 2026-07-25): MVP-104 y MVP-105 pasan a `completado`; `_indice.md` regenerado |
| R-I | 2026-07-25 | MVP-001 / MVP-102 / MVP-104 | funcional | Edicion/eliminacion de Workspace no planificada. | alto | si | descartado (duplicado) | Ya contemplado por T-001 (= P-004 en MVP-999). Sin fila nueva de trabajo |

## Resultado de la revision (2026-07-25)

Revision realizada con el PO sobre el flujo integrado. Los 7 puntos reportados y los hallazgos
adicionales (R-A..R-I) se clasifican asi:

- **Arreglar ya, dentro de MVP-001 — historia `MVP-106`** (correcciones de lo entregado):
  puntos 1, 2, 3a (comportamiento de enlaces) y 4, mas R-A y R-B.
- **Alcance nuevo, dentro de MVP-001 — historia `MVP-107`** (invitaciones no bloqueantes +
  notificaciones): puntos 5, 6 y 7, mas R-C y R-D. Decision del PO: el punto 7 entra completo.
- **Diferido a `MVP-999`**: contenido legal + cookies (P-008), limpieza de codigo muerto (P-009)
  y copy de onboarding (P-010).
- **Housekeeping de esta revision (R-G)**: alinear el `estado` de MVP-104/MVP-105 con sus CA al
  cerrar QA.
- **Ya contemplado (R-I)**: ciclo de vida del Workspace, cubierto por T-001 (= P-004).

Con la decision del PO, el alcance de la epica MVP-001 se amplia con MVP-106 y MVP-107; la epica
no se cierra hasta entregarlas.

## Resultado de la 2a revision (2026-07-26)

Segunda pasada de cierre tras la entrega de `MVP-106` (PR #9) y `MVP-107` (PR #10), verificando la
implementacion **contra el codigo real** (frontend `src/frontend/terrenario-web` y backend
`src/backend/Terrenario.Api`), no solo build/tests.

**Verificacion de entrega (todas las historias entregadas y conformes):**

- `MVP-106` — 3/3 CA CUMPLEN: callback OIDC idempotente (`processedCodes` a nivel de modulo, sin
  borrar artefactos PKCE hasta resolver), landing con patron de acceso unico (sin "gratis",
  "Ingresar", "#funciones" ni "con Google" enganoso) y enlaces legales deshabilitados
  ("proximamente"). P-009 (codigo muerto Demo) retirado.
- `MVP-107` — 3/3 CA CUMPLEN: acceso post-login directo sin pagina-gate, preview con aptitud
  (`viewer.can_accept` sin PII), rechazo (`POST /api/v1/invitations/{token}/reject`), campanita +
  bandeja de recibidas (`GET /api/v1/invitations/received`) y modal descartable. Ademas quedan
  resueltos P-013 (crear Workspace adicional), P-014 (bug EF 500 en `GET /workspaces` con
  regresion SQLite real), P-015 (design-system) y P-016 (shell de app).
- `MVP-101..105` — sin regresiones detectadas; sesion base completa (login + refresh + logout).

**Reconciliacion de gobernanza aplicada (habilita el CA-1 de la epica):**

- `MVP-106`: `en-progreso` → `completado`, CA marcados `[x]`.
- `MVP-107`: `aprobado` → `completado`, CA marcados `[x]`.
- `MVP-101`: checklist de CA cerrado (`[x]`); ya estaba `completado`.
- `_indice.md` regenerado (8/8).
- `MVP-199`: `borrador` → `completado`.

**Alcance absorbido en MVP-001 (trazado, no defecto):** creacion de Workspaces adicionales (P-013)
y los fundamentos transversales de UI —design-system (P-015) y shell (P-016)— entraron via
MVP-107. Documentado en `MVP-999` que esa base ya existe para MVP-002..004.

**Pendientes reruteados/confirmados en esta pasada:**

- `T-001`/`P-004` — ciclo de vida de Workspace (renombrar/eliminar): decision del PO, **fuera de
  MVP-001**, se traslada a **MVP-002 / MVP-204** (pendiente de crear). Cierra la asimetria
  documentalmente: crear adicionales existe, editar/eliminar se plantea alli.
- `P-008` — contenido legal (Privacidad/Terminos) + consentimiento de cookies: **se mantiene
  diferido** a MVP-005/MVP-502 (decision del PO). Vigilar como requisito previo a salida a
  produccion/beta publica por RGPD/LOPDGDD/LSSI.
- `P-010` — copy "Paso 1 de 3" del onboarding: sigue visible (verificado en
  `CreateWorkspacePage.tsx`); permanece `pendiente` en MVP-999. Deuda cosmetica menor.
- `P-011` (refresco campanita), `P-012` (arnes tests frontend): correctamente diferidos.

**Conclusion:** con la reconciliacion aplicada, las 8 historias quedan en `completado`, los CA de
la epica (CA-1/CA-2/CA-3) se cumplen y **MVP-001 queda cerrada**. No quedan puntos bloqueantes
dentro de la epica; los pendientes tienen destino en otras epicas o en MVP-999.

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.
- La implementacion de los puntos detectados es alcance de MVP-106 y MVP-107, no de esta historia
  (que es de gobierno de alcance).
