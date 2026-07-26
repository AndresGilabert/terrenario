---
id: "MVP-107"
tipo: feature
titulo: "TDD: Invitaciones no bloqueantes y centro de notificaciones"
estado: en-progreso
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "invitaciones", "notificaciones"]
  modulo_path: "03-modulos/"
  componentes: ["invitaciones", "notificaciones", "workspace-members", "ui-shell"]
  etiquetas: ["mvp", "invite", "notifications", "ux"]
  nivel_riesgo: medio
creado_en: "2026-07-26"
actualizado_en: "2026-07-26"
---

# TDD: MVP-107 — Invitaciones no bloqueantes y centro de notificaciones

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Amplía el flujo de invitaciones de MVP-103 sin tocar sus reglas de negocio (un solo uso, caducidad
de 7 días, email vs enlace). Se apoya en tres cambios de backend y un rediseño de la superficie de
entrada en el frontend:

1. **Aptitud en el preview (R-C).** `GET /api/v1/invitations/{token}` pasa a exponer si la cuenta
   autenticada puede aceptar (`viewer.can_accept` + `viewer.reason`), calculado con el mismo orden
   de validación que `Accept`, de modo que el 403 `AUTH_INVITATION_EMAIL_MISMATCH` se anticipa en
   lugar de dispararse tras pulsar. No se filtra el email destinatario: solo se compara.
2. **Rechazo (punto 6).** Nuevo estado de dominio `rechazada` y método `WorkspaceInvitation.Reject`,
   con endpoints por token (`POST /invitations/{token}/reject`) y por id
   (`POST /invitations/received/{id}/reject`). Rechazar no crea membresía ni cierra sesión.
3. **Bandeja de recibidas (punto 7 / R-D).** Nuevo `GET /api/v1/invitations/received` que lista las
   invitaciones por email dirigidas a la cuenta autenticada (por email canónico), separado del
   listado de emitidas por el Workspace activo. Alimenta la campanita y el modal.

En el cliente, el acceso post-login deja de pasar por una puerta obligatoria: se llega al Workspace
activo o, si no hay ninguno, al asistente de creación —o, si hay invitaciones pendientes, a una
pantalla de decisión que las prioriza—. La aceptación de invitación se ofrece de forma no bloqueante
en un modal descartable y en un centro de notificaciones (campanita + bandeja) en una cabecera nueva.

El modelo de "notificación" del MVP se limita a invitaciones: no se introduce tabla genérica de
notificaciones (el spec y las notas de refinamiento lo dejan fuera salvo justificación).

## Decisiones de producto (usabilidad)

Dos bifurcaciones de la spec se resolvieron con el PO antes de implementar:

- **Aceptar desde la bandeja cambia al Workspace aceptado.** Coherente con la aceptación por enlace
  de MVP-103 ("aceptar = entrar"): reemite la sesión situada en el destino y navega a `/app`. Se
  descartó "unirse en segundo plano" por romper esa coherencia.
- **Invitado sin Workspace: se prioriza la pantalla de invitación.** Quien inicia sesión con
  invitaciones pendientes y ningún Workspace propio aterriza en una pantalla de decisión
  (`ReceivedInvitationsPage`) con un enlace secundario "Prefiero crear mi propio Workspace", en vez
  de caer directo al asistente de creación. Evita que se sienta obligado a crear uno.

## Diagrama de arquitectura / flujo

```mermaid
sequenceDiagram
    participant U as Usuario (invitado)
    participant FE as Frontend (SPA)
    participant BE as Backend API (.NET)
    participant DB as PostgreSQL

    Note over U,FE: Tras login se va directo a /app u onboarding (CA-1)
    FE->>BE: GET /api/v1/invitations/received (Bearer)
    BE->>DB: SELECT por email canónico + estado=pendiente + canal=email
    BE->>FE: 200 { data: [ { id, workspace, invited_by, expires_at } ] }
    FE->>U: Campanita con contador + modal de la primera no vista

    alt Acepta (desde modal/bandeja, por id)
        FE->>BE: POST /api/v1/invitations/received/{id}/accept
        BE->>BE: Autoriza por titularidad de email; Accept + membresía
        BE->>FE: 200 { workspace, access_token, expires_in, already_member }
        FE->>U: Sesión situada en el Workspace → /app
    else Rechaza (por id)
        FE->>BE: POST /api/v1/invitations/received/{id}/reject
        BE->>DB: UPDATE estado=rechazada (sin membresía)
        BE->>FE: 204 No Content
        FE->>U: Sigue en la plataforma; la invitación sale de la bandeja
    else Cierra el modal
        FE->>U: Invitación queda pendiente (marcada como "vista")
    end
```

## Componentes afectados

### Backend

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `Domain/Workspaces/InvitationStatuses.cs` | modificado | Nuevo estado `rechazada` |
| `Domain/Workspaces/WorkspaceInvitation.cs` | modificado | `Reject`, `IsAddressedTo`, columnas `RejectedAt`/`RejectedByUserId`, guarda de estado en `Accept` (no aceptar una rechazada) |
| `Common/Errors/ErrorCodes.cs` | modificado | `BUSINESS_RULE_INVITATION_ALREADY_REJECTED` |
| `Domain/Workspaces/IWorkspaceInvitationRepository.cs` | modificado | `FindByIdAsync`, `ListReceivedPendingAsync` |
| `Infrastructure/Data/Repositories/WorkspaceInvitationRepository.cs` | modificado | Implementación de los dos nuevos métodos |
| `Infrastructure/Data/Repositories/WorkspaceRepository.cs` | modificado | Fix del `OrderBy` no traducible en `ListActiveMembershipsAsync` (P-014, defecto de MVP-104) |
| `Application/Invitations/Commands/InvitationCommands.cs` | modificado | `InvitationPreview` con aptitud, `ReceivedInvitationSummary`, `InvitationViewerReasons` |
| `Application/Invitations/PreviewInvitationHandler.cs` | modificado | Aptitud de la cuenta autenticada (mismo orden que `Accept`) |
| `Application/Invitations/AcceptInvitationHandler.cs` | modificado | `HandleByIdAsync` (bandeja) con núcleo compartido |
| `Application/Invitations/RejectInvitationHandler.cs` | nuevo | Rechazo por token y por id |
| `Application/Invitations/ListReceivedInvitationsHandler.cs` | nuevo | Bandeja de recibidas (excluye caducadas y Workspaces de los que ya se es miembro) |
| `Controllers/InvitationsController.cs` | modificado | Rutas `received`, `{token}/reject` y `received/{id}/accept` y `.../reject`; preview con aptitud |
| `Infrastructure/Data/TerrenarioDbContext.cs` | modificado | Columnas de rechazo, índice `(email, status)`, FK de `RejectedByUserId` |
| `Infrastructure/Data/Migrations/*_AddInvitationRejection.cs` | nuevo | Columnas y índice |
| `Program.cs` | modificado | DI de `RejectInvitationHandler` y `ListReceivedInvitationsHandler` |

### Frontend

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `types/invitation.types.ts` | modificado | `rechazada`, `InvitationViewerReason`, `viewer` en preview, `ReceivedInvitation` |
| `services/invitation.service.ts` | modificado | `rejectInvitation`, `listReceivedInvitations`, `acceptReceivedInvitation`, `rejectReceivedInvitation`, manejo de 204 |
| `contexts/WorkspaceContext.tsx` | modificado | `acceptInvitationById` (reemite sesión y sitúa el Workspace); `loadWorkspaces` no borra la lista ante fallo y recarga con el token nuevo (P-014) |
| `contexts/NotificationsContext.tsx` | nuevo | Bandeja de recibidas, contador, aceptar/rechazar y tracking de "nuevas" para el modal |
| `lib/invitation-ui.ts` | nuevo | Textos de aptitud y caducidad |
| `components/notifications/ReceivedInvitationCard.tsx` | nuevo | Tarjeta reutilizable con Aceptar/Rechazar |
| `components/notifications/useInvitationActions.ts` | nuevo | Estado de acción compartido (ocupado/error/navegación) |
| `components/notifications/NotificationBell.tsx` | nuevo | Campanita + bandeja desplegable (CA-3) |
| `components/notifications/InvitationModal.tsx` | nuevo | Modal no bloqueante descartable (HU-2) |
| `components/layout/AppHeader.tsx` · `AppLayout.tsx` | nuevo | Cabecera con selector + campanita y el modal sobre `/app` |
| `components/invitations/AcceptInvitationPage.tsx` | modificado | Aptitud, aceptar/rechazar, sin "Usar otra cuenta", salida siempre disponible |
| `components/invitations/ReceivedInvitationsPage.tsx` | nuevo | Decisión priorizada para invitado sin Workspace |
| `components/workspace/WorkspaceSwitcher.tsx` | modificado | Acción "＋ Nuevo Workspace" y desplegable siempre disponible (ver adenda P-013) |
| `components/onboarding/CreateWorkspacePage.tsx` | modificado | Prop `mode` (`onboarding`/`additional`) para reutilizar el alta desde la app |
| `App.tsx` | modificado | `NotificationsProvider`, `AppLayout`, ruta `/app/workspaces/new`, onboarding que prioriza invitación; el selector sale de `AppHome` a la cabecera |

## Diseño detallado

### Modelo de datos

Se añaden a `workspace_invitations` dos columnas nullable para la trazabilidad del rechazo, un
índice para la bandeja y una FK de restricción:

```sql
ALTER TABLE workspace_invitations ADD COLUMN rejected_at          TIMESTAMPTZ NULL;
ALTER TABLE workspace_invitations ADD COLUMN rejected_by_user_id  UUID NULL REFERENCES users(id) ON DELETE RESTRICT;
CREATE INDEX idx_workspace_invitations_email_status ON workspace_invitations(email, status);
```

- El rechazo **sí** es un estado persistido (`rechazada`), a diferencia de la caducidad, que se
  sigue derivando de `expires_at`. Un rechazo es una decisión del usuario que hay que recordar.
- El índice `(email, status)` sirve a la consulta de la bandeja, que filtra invitaciones por email
  y estado pendiente.

### API / Contratos

```yaml
# GET /api/v1/invitations/{token}   (preview, ahora con aptitud)
responses:
  200:
    body:
      id: uuid
      channel: string
      status: string
      workspace: { id: uuid, name: string }
      invited_by: string|null
      expires_at: timestamptz
      is_expired: boolean
      viewer:
        can_accept: boolean         # anticipa el resultado de aceptar (R-C)
        reason: string|null         # email_mismatch | expired | already_used | already_rejected | already_member

# POST /api/v1/invitations/{token}/reject   (rechazo por enlace)
responses:
  204: {}                            # sin cuerpo
  403: { error: { code: "AUTH_INVITATION_EMAIL_MISMATCH" } }
  404: { error: { code: "INVITATION_NOT_FOUND" } }
  422: { error: { code: "BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED" } }

# GET /api/v1/invitations/received   (bandeja de recibidas por la cuenta autenticada)
responses:
  200:
    body:
      data:
        - { id: uuid, channel: "email", workspace: { id, name }, invited_by: string|null,
            expires_at: timestamptz, created_at: timestamptz }
      meta: { total: integer }

# POST /api/v1/invitations/received/{id}/accept
responses:
  200: { workspace, access_token, expires_in, already_member }   # igual que aceptar por token
  404: { error: { code: "INVITATION_NOT_FOUND" } }               # también si no va dirigida a la cuenta

# POST /api/v1/invitations/received/{id}/reject
responses:
  204: {}
  404: { error: { code: "INVITATION_NOT_FOUND" } }
```

No exige Workspace activo (`InvitationsController` es `[Authorize]`, no `[RequireWorkspaceScope]`):
es la vía por la que un usuario sin ninguno descubre y acepta el primero.

### Lógica de negocio

**Aptitud del preview.** `EvaluateAptitude` replica el orden de `Accept`: ya aceptada → `already_used`;
rechazada → `already_rejected`; caducada → `expired`; no dirigida a la cuenta → `email_mismatch`; ya
miembro → `already_member` (con `can_accept=true`, porque aceptar es idempotente y sitúa la sesión);
en otro caso, apta. El email destinatario nunca se devuelve: solo se compara contra el de la cuenta.

**Dos vías de autorización.** La aceptación/rechazo por **token** se autoriza por posesión del enlace
(flujo de MVP-103). La aceptación/rechazo por **id** —desde la bandeja, donde la persona nunca tuvo
el token— se autoriza por **titularidad del email**: el JWT prueba que la cuenta es dueña del correo,
y una invitación que no va dirigida a esa cuenta (o de canal `enlace`, que no tiene destinatario) se
trata como **inexistente** (`INVITATION_NOT_FOUND`) para no revelar su existencia.

**Rechazo.** `Reject` valida antes el desajuste de email (un tercero con el correo reenviado no
declina la invitación de otra persona) y es idempotente ante un segundo rechazo del mismo
destinatario (doble clic). Rechazar una invitación caducada se permite: limpia la bandeja sin efecto
colateral. `Accept` pasa a rechazar explícitamente una invitación ya `rechazada`.

**Bandeja.** `ListReceivedInvitationsHandler` consulta por email canónico (minúsculas), excluye
caducadas y Workspaces de los que ya se es miembro, y resuelve nombre de Workspace e invitador. Solo
incluye el canal `email`: el enlace compartible no tiene destinatario y no aparece en la bandeja de
nadie.

### Flujo en el cliente

- **Acceso post-login (CA-1).** `OAuthCallback` ya navegaba a `/app` (o al destino de un deep-link de
  invitación). `RequireWorkspace` deriva a `/onboarding` si no hay Workspace; el nuevo `OnboardingRoute`
  muestra la pantalla de invitación priorizada si hay recibidas, o el asistente de creación.
- **Modal no bloqueante (HU-2).** `NotificationsContext` marca la primera invitación "no vista"
  (`localStorage`) como `newInvitation`; `InvitationModal` la ofrece sobre `/app` y se cierra dejándola
  pendiente. La campanita mantiene el contador con todas las pendientes.
- **Rediseño del deep-link.** `AcceptInvitationPage` usa `viewer.can_accept`: si no es apta, muestra el
  motivo y solo ofrece salir; si lo es, Aceptar/Rechazar; si ya es miembro, "Entrar al Workspace". Se
  retira "Usar otra cuenta" (cerraba sesión) y siempre hay salida a la plataforma.

### Manejo de errores

| Situación | Código HTTP | Código de error |
| --------- | ----------- | --------------- |
| Rechazo/aceptación por id no dirigida a la cuenta o de canal enlace | 404 | `INVITATION_NOT_FOUND` |
| Rechazo por token de invitación por email de otra cuenta | 403 | `AUTH_INVITATION_EMAIL_MISMATCH` |
| Rechazar una invitación ya aceptada | 422 | `BUSINESS_RULE_INVITATION_ALREADY_ACCEPTED` |
| Aceptar una invitación ya rechazada | 422 | `BUSINESS_RULE_INVITATION_ALREADY_REJECTED` |

`InvitationErrorMapper` no cambia: los nuevos códigos `BUSINESS_RULE_*` caen en 422 por su prefijo.

## Impacto en la usabilidad

- **Acceso**: una invitación pendiente ya no bloquea la entrada; el usuario decide cuando quiere. La
  campanita da visibilidad in-app sin depender del correo.
- **Deep-link**: se elimina el callejón sin salida (el 403 tras pulsar y el "Usar otra cuenta" que
  cerraba sesión). El motivo de no-aptitud se muestra por adelantado.
- **Cabecera nueva**: `/app` gana una cabecera mínima (selector + campanita). El selector de Workspace
  se mueve de `AppHome` a la cabecera; no se pierde ninguna función.
- **Aceptar desde la bandeja cambia de Workspace activo**: es coherente con "aceptar = entrar", pero
  puede sorprender a mitad de tarea. Se asumió como decisión de producto (ver arriba).
- No se detectan roturas de usabilidad adicionales que requieran decisión.

### Adenda: alta de Workspace adicional (P-013)

Durante la revisión del flujo se detectó que **crear un Workspace adicional no estaba previsto en
ninguna historia**: el backend `POST /api/v1/workspaces` no está limitado al primero, pero la UI solo
abría el asistente de creación en el estado "cero Workspaces" (`OnboardingRoute` redirige a `/app` si
ya hay uno). MVP-107 lo hizo más visible, porque la pantalla del invitado sin Workspace ofrece "crear
mi propio Workspace" mientras que quien ya tiene uno no encontraba esa acción.

Por decisión de producto se incluye la corrección en este PR (registrada como **P-013 → resuelto en
MVP-107** en `MVP-999`): el `WorkspaceSwitcher` gana una acción "＋ Nuevo Workspace" hacia
`/app/workspaces/new`, que reutiliza `CreateWorkspacePage` en modo `additional` (sin el indicador de
onboarding, con "Cancelar" en lugar de "Cerrar sesión"). No cubre edición ni borrado de Workspaces,
que siguen en P-004 (triage de MVP-199).

### Adenda: fidelidad al prototipo — sistema de diseño y shell (P-015, P-016)

Al cerrar la épica se hizo una **auditoría de fidelidad** entre el prototipo (`prototype/terrenario-mvp`)
y el frontend desarrollado. Reveló deuda transversal (no de MVP-107) que, por decisión, se salda en
este PR para no cerrar la épica arrastrándola:

- **P-015 — Fundamentos de diseño:** se portan tipografía (Inter + Plus Jakarta Sans, clase
  `.font-headline`), iconografía **Material Symbols Outlined** y utilidades a `index.html`/`index.css`;
  se sustituyen emojis por iconos Material y se aplica la tipografía display en Login, onboarding,
  Landing (hero a dos columnas, sin métricas inventadas por coherencia con MVP-106), selector,
  campanita y pantallas de invitación. Verificado en runtime que las fuentes e iconos cargan
  (`document.fonts.check` = true), no como texto de reserva.
- **P-016 — Shell:** `AppSidebar` + `AppTopbar` + `AppLayout` sustituyen al placeholder y a la
  cabecera mínima. La navegación lista los 8 módulos del producto; los de épicas posteriores
  (MVP-002..004) quedan **deshabilitados con "Pronto"** (honesto, sin enlaces rotos). El `AppLayout`
  aporta un **contenedor de contenido común** (misma anchura y padding) para que todas las secciones
  mantengan tamaño y espaciado coherentes.

Ambos puntos quedan registrados como **resueltos en MVP-107** en `MVP-999`.

### Adenda: bug de listado de Workspaces (P-014, defecto de MVP-104)

Al depurar el selector se encontró un **bug real y preexistente de MVP-104**:
`WorkspaceRepository.ListActiveMembershipsAsync` ordenaba con `.OrderBy(membership => membership.Name)`
sobre el **DTO proyectado** en el `Join`. EF Core no sabe traducir esa expresión a SQL, así que
`GET /api/v1/workspaces` devolvía **HTTP 500** desde la entrega de MVP-104. El frontend tragaba el
error a lista vacía, por lo que el selector solo mostraba el Workspace activo y los demás parecían
"desaparecer". Era invisible porque el selector se deshabilitaba con la lista vacía; solo salió a la
luz al hacer el desplegable siempre disponible en esta historia.

Verificación end-to-end (con un JWT de desarrollo acuñado con la clave RSA local, sin depender del
login de Google): `GET /api/v1/workspaces` pasó de 500 a `200` con las dos membresías, y el selector
de la UI mostró ambos Workspaces y los mantuvo tras cambiar de contexto.

Correcciones incluidas:

- **Backend (la causa):** ordenar por la columna real (`x.Workspace.Name`) **antes** de proyectar.
- **Frontend (endurecimiento):** `loadWorkspaces` no borra la lista buena ante un fallo transitorio
  y recarga con el token reemitido; el `WorkspaceSwitcher` siempre incluye el Workspace activo.
- **Regresión:** test de `WorkspaceRepository` contra **SQLite real** (no InMemory), que sí ejercita
  la traducción a SQL y habría cazado el fallo; los tests con repos mockeados no lo veían. La
  cobertura de integración completa contra PostgreSQL sigue en MVP-501.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
| ----------- | ------------------- |
| Aceptar/rechazar la bandeja por token | La persona invitada nunca tuvo el token (viajó por email); solo se conserva su hash. La bandeja opera por id con autorización por email |
| Endpoint único aceptar/rechazar por id para cualquier canal | El enlace no tiene destinatario: aceptar por id un enlace ajeno abriría el Workspace a quien adivine el id. La bandeja se limita a canal `email` dirigido a la cuenta |
| Tabla genérica de `notifications` | El MVP solo notifica invitaciones; una tabla genérica es alcance de la épica de asignación de tareas (RU-31). Se modela sobre `workspace_invitations` |
| Revelar el email destinatario en el preview | Filtra PII de la persona invitada a quien tenga el enlace; basta comparar contra la cuenta autenticada y devolver un booleano + motivo |
| Marcar "vistas" en servidor | Un flag de lectura por invitación es estado extra sin valor en el MVP; el modal se autolimita con `localStorage` y la bandeja siempre muestra todas las pendientes |
| Caducar/limpiar invitaciones rechazadas con un job | El estado `rechazada` ya las saca de la bandeja y de la aceptación; no hace falta proceso en segundo plano |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
| ------ | ------------ | ---------- |
| Aceptar desde la campanita saca al usuario de su Workspace actual | media | Decisión de producto asumida; el selector permite volver de inmediato |
| La bandeja no se refresca en vivo: una invitación nueva no aparece hasta recargar | media | Fuera de alcance el tiempo real; se refresca al montar la sesión. Propuesto refresco en foco/periódico para MVP-999 |
| Colisión de rutas `received` vs `{token}` | baja | El segmento literal `received` tiene prioridad sobre el parámetro en el enrutado de ASP.NET Core; `received/{id}/…` tiene distinto número de segmentos que `{token}/…` |
| N+1 al resolver Workspace e invitador por invitación en la bandeja | baja | Volumen despreciable en MVP (pocas invitaciones por cuenta); optimizable con un `IN`/join si crece |
| El `localStorage` de "vistas" crece | baja | Se poda en cada carga a los ids que siguen pendientes |

## Plan de testing

> Referencia: `docs/04-ingenieria/estrategia-testing.md`

- [x] Tests unitarios (backend, `dotnet test` en verde — 130 tests, 71 nuevos respecto a MVP-103):
  - `WorkspaceInvitation`: rechazo por el destinatario, idempotencia del rechazo, rechazo por otra
    cuenta, no rechazar una aceptada, no aceptar una rechazada, rechazar caducada, `IsAddressedTo`.
  - `PreviewInvitationHandler`: apta, desajuste de email sin revelar destinatario, caducada, ya
    miembro (apta con motivo).
  - `AcceptInvitationHandler`: aceptación por id desde la bandeja, ocultar por email no coincidente,
    ocultar canal enlace.
  - `RejectInvitationHandler`: rechazo por token y por id, no encontrada, ocultar por email/canal.
  - `ListReceivedInvitationsHandler`: consulta por email canónico, exclusión de caducadas y de
    Workspaces de los que ya se es miembro.
  - `WorkspaceRepository` contra **SQLite real** (no InMemory): `ListActiveMembershipsAsync` devuelve
    todas las membresías ordenadas por nombre sin error de traducción (regresión de P-014).
- [ ] Tests de integración: los endpoints nuevos contra PostgreSQL, junto al resto de la épica
  (MVP-501).
- [ ] Tests E2E: invitar por email → aparecer en la bandeja del invitado → aceptar/rechazar,
  pendiente del arnés E2E de la épica.
- [ ] Tests unitarios de frontend: el frontend aún no tiene arnés (`vitest`/`jest`); `NotificationsContext`,
  `useInvitationActions` y el mapeo de motivos quedan cubiertos por tipado + build + lint y validación
  manual. Propuesto arnés de frontend en MVP-999 (ver spec / registro).
- Resultado local: `dotnet test` en verde (129 tests). `npm run build` sin errores de TypeScript y
  `npm run lint` sin advertencias nuevas (la de `only-export-components` de `NotificationsContext`
  sigue el patrón ya existente en `AuthContext`/`WorkspaceContext`).

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Migración de base de datos preparada (`AddInvitationRejection`)
- [x] Tests de backend escritos y pasando
- [x] Documentación de API actualizada en este documento y en `docs/02-arquitectura/contratos-api.md`
- [x] Puntos de coherencia registrados en `MVP-999`
- [ ] Módulo de Workspaces documentado en `docs/03-modulos/` (se consolidará con MVP-104/204)
- [x] Sin `TODO` sin resolver en este documento
