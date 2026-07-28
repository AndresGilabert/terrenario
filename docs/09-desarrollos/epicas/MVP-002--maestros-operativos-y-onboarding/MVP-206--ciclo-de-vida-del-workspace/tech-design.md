---
id: "MVP-206"
tipo: feature
titulo: "TDD: Ciclo de vida del Workspace: renombrar, baja lógica y traspaso de propiedad"
estado: en-progreso
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "membresia", "propiedad", "ciclo-de-vida"]
  modulo_path: "03-modulos/"
  componentes: ["workspaces", "workspace-owner", "notificaciones"]
  etiquetas: ["mvp", "workspace", "ownership", "soft-delete"]
  nivel_riesgo: alto
creado_en: "2026-07-28"
actualizado_en: "2026-07-28"
---

# TDD: MVP-206 — Ciclo de vida del Workspace

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Cierra el ciclo de vida del Workspace sobre lo ya entregado en MVP-102/103/104/107/204, con una
invariante rectora: **un Workspace nunca queda sin propietario ni se pierde por accidente**.

1. **Renombrar** (`Workspace.Rename`) con las validaciones del alta de MVP-102, sin reemitir la
   sesión: el nombre no viaja en el token, así que basta con resincronizar el contexto del cliente.
2. **Baja lógica** (`deleted_at`, `deleted_by_user_id`). Nunca hay borrado físico. Todas las lecturas
   del puerto `IWorkspaceRepository` pasan a filtrar por «vivo», así que un Workspace dado de baja
   deja de resolver contexto y de aparecer en el selector **sin tocar una sola línea** de los
   maestros ya entregados (MVP-202/203/204/205): estos se apoyan en `[RequireWorkspaceScope]`, que
   resuelve el Workspace con ese mismo puerto.
3. **Traspaso de propiedad** sobre `workspaces.owner_id` + el `role` de `workspace_members`, en dos
   variantes: **automática** cuando hay copropietarios (CA-5) y **explícita** cuando el propietario
   es único y prefiere ceder en vez de dar de baja (CA-4).
4. **Reactivación**: entidad nueva `workspace_reactivation_requests` con token de un solo uso
   —mismo esquema que las invitaciones de MVP-103—, correo a los miembros al dar de baja (CA-6) y
   autorización exclusiva de quien dio de baja (CA-7/CA-10).
5. **Regla de no-orfandad de la baja de cuenta** (CA-9) como guarda reutilizable
   (`WorkspaceOwnershipGuard`) más un endpoint de consulta. El **flujo completo** de baja de cuenta
   (RGPD) sigue fuera de alcance (`MVP-999`, P-024): aquí vive solo la regla que deberá respetar.

Todas las operaciones actúan sobre el **Workspace activo** y llevan `[RequireWorkspaceScope]`
(MVP-105), salvo las de reactivación: por definición operan sobre un Workspace que ya no resuelve
contexto, así que se autorizan por titularidad (destinatario del enlace / autor de la baja).

### Decisiones de producto tomadas en esta historia

El spec dejaba cuatro puntos abiertos («a decidir en el `tech-design`» / «pendiente con el PO»).
Decisiones acordadas antes de implementar:

| Punto abierto | Decisión | Motivo |
|---|---|---|
| ¿Quién puede **renombrar**? | **Cualquier miembro activo** | Literalidad de HU-1 («Como miembro del Workspace…») y coherencia con RN-034 y con la revocación de MVP-204 |
| Con **copropietarios**, ¿qué le pasa a quien pide la baja? | **Sale del Workspace**: cede la propiedad y su membresía pasa a `revocado` | El Workspace desaparece de su selector, que es lo que esperaba al pedir dejar de verlo. La UI lo nombra «Salir y ceder mi propiedad» para no dar a entender un borrado |
| Al **traspasar** siendo propietario único, ¿se va? | **Se queda como miembro normal** | Ceder la propiedad no es irse; para salir está la retirada de acceso de MVP-204. Menos destructivo |
| ¿Sobre qué Workspace se opera? | **Solo el activo**, desde «Ajustes» | Mantiene el patrón Workspace-first de MVP-105 (el Workspace nunca viaja como parámetro, RN-034) y evita una pantalla nueva de gestión de Workspaces |

Además, dos decisiones de diseño propias:

- **La reapertura por quien dio de baja es una acción propia**, no un caso de la autorización. El
  spec exige que con un Workspace sin más miembros «la reactivación quede disponible solo para quien
  la dio de baja»: sin nadie a quien notificar no hay solicitud posible, así que hace falta una vía
  directa (`POST /workspaces/reactivations/closed/{id}/reopen`). Es también la que hace **cierta** la
  promesa de reversibilidad que la UI hace al confirmar la baja.
- **Del prototipo `AjustesView` no se porta el «Perfil del titular»** (nombre y email editables): la
  identidad viene de Google y no es editable en el MVP (RN-036). Se registra como punto en `MVP-999`.

## Diagrama de flujo

```mermaid
sequenceDiagram
    participant O as Propietario
    participant M as Miembro
    participant FE as Frontend (SPA)
    participant BE as Backend API (.NET)
    participant DB as PostgreSQL
    participant EM as email-service

    O->>FE: Ajustes · "Dar de baja el Workspace"
    FE->>BE: GET /workspaces/active/closure
    BE->>FE: 200 { mode, successor_name?, candidates[] }

    alt mode = auto_transfer (hay copropietarios, CA-5)
        O->>FE: "Salir y ceder"
        FE->>BE: POST /workspaces/active/closure
        BE->>DB: owner_id = sucesor; solicitante → revocado
        BE->>FE: 200 { outcome: transferred }
    else mode = choose (propietario único, CA-3)
        alt Traspasar (CA-4)
            O->>FE: elige miembro
            FE->>BE: POST /workspaces/active/transfer-ownership
            BE->>DB: owner_id = elegido; solicitante → workspace_member
        else Dar de baja (CA-2/CA-6)
            FE->>BE: POST /workspaces/active/closure
            BE->>DB: deleted_at = now; 1 solicitud (token hash) por miembro activo
            BE->>EM: email a cada miembro con su enlace de un solo uso
        end
    end

    M->>FE: abre /reactivations/{token}
    FE->>BE: GET /workspaces/reactivations/{token}
    M->>FE: "Solicitar traspaso y reactivación"
    FE->>BE: POST /workspaces/reactivations/{token}/request
    BE->>DB: estado → solicitada (enlace consumido, CA-10)
    BE->>EM: aviso a quien dio de baja

    O->>FE: /reactivations · "Autorizar traspaso"
    FE->>BE: POST /workspaces/reactivations/{id}/authorize
    BE->>DB: deleted_at = NULL; owner_id = solicitante; resto de enlaces → cerrada
```

## Componentes afectados

### Backend

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `Domain/Workspaces/Workspace.cs` | modificado | `DeletedAt`/`DeletedByUserId`; `Rename`, `SoftDelete`, `Reactivate`, `TransferOwnershipTo` |
| `Domain/Workspaces/WorkspaceMember.cs` | modificado | `PromoteToOwner` / `DemoteToMember` |
| `Domain/Workspaces/WorkspaceReactivationRequest.cs` | nuevo | Agregado del enlace de un solo uso: `Issue`, `Submit`, `Authorize`, `Deny`, `Close` |
| `Domain/Workspaces/ReactivationRequestStatuses.cs` | nuevo | Catálogo `pendiente`/`solicitada`/`autorizada`/`denegada`/`cerrada` |
| `Domain/Workspaces/SoleOwnedWorkspace.cs` | nuevo | Proyección de la obligación de propiedad única (CA-9) |
| `Domain/Workspaces/IWorkspaceReactivationRequestRepository.cs` | nuevo | Puerto + `ReactivationRequestDetail` (proyección de la bandeja) |
| `Domain/Workspaces/IWorkspaceRepository.cs` | modificado | Contrato de «solo vivos»; `FindIncludingDeletedAsync`, `FindOtherActiveOwnerAsync`, `ListSoleOwnedAsync`, `ListClosedByAsync` |
| `Infrastructure/Data/Repositories/WorkspaceRepository.cs` | modificado | `LiveWorkspaces` como base de todas las lecturas + consultas nuevas |
| `Infrastructure/Data/Repositories/WorkspaceReactivationRequestRepository.cs` | nuevo | Adaptador EF Core |
| `Infrastructure/Data/Migrations/*_AddWorkspaceLifecycle.cs` | nuevo | Columnas de baja + tabla de solicitudes + índices |
| `Infrastructure/Tokens/IOneTimeTokenService.cs` | nuevo | Puerto neutro del token de un solo uso (lo implementa `InvitationTokenService`) |
| `Infrastructure/Email/SmtpMailer.cs` | nuevo | Transporte SMTP común, extraído del emisor de invitaciones |
| `Infrastructure/Email/{IWorkspaceLifecycleEmailSender,SmtpWorkspaceLifecycleEmailSender,WorkspaceLifecycleEmailComposer,WorkspaceLifecycleOptions}.cs` | nuevo | Correos de baja y de solicitud de reactivación |
| `Infrastructure/Invitations/SmtpInvitationEmailSender.cs` | modificado | Pasa a componer y delegar en `SmtpMailer` |
| `Application/Workspaces/Commands/{WorkspaceLifecycleCommands,ReactivationCommands}.cs` | nuevo | Modos, resultados y proyecciones |
| `Application/Workspaces/RenameWorkspaceHandler.cs` | nuevo | HU-1 |
| `Application/Workspaces/GetWorkspaceClosureOptionsHandler.cs` | nuevo | Árbol de decisión resuelto en servidor |
| `Application/Workspaces/CloseWorkspaceHandler.cs` | nuevo | Reasignación automática o baja lógica + avisos |
| `Application/Workspaces/TransferWorkspaceOwnershipHandler.cs` | nuevo | Traspaso explícito (CA-4) |
| `Application/Workspaces/{Preview,Request}ReactivationHandler.cs` | nuevo | Lectura y consumo del enlace (HU-5) |
| `Application/Workspaces/{List,Resolve}ReactivationRequestsHandler.cs` | nuevo | Bandeja y decisión de quien dio de baja (HU-6) |
| `Application/Workspaces/ReopenWorkspaceHandler.cs` | nuevo | Reapertura directa por quien dio de baja |
| `Application/Workspaces/WorkspaceOwnershipGuard.cs` | nuevo | Regla de no-orfandad de la baja de cuenta (CA-9) |
| `Controllers/WorkspacesController.cs` | modificado | `PATCH /active`, `GET` y `POST /active/closure`, `POST /active/transfer-ownership`, `GET /ownership-obligations` |
| `Controllers/WorkspaceReactivationsController.cs` | nuevo | Enlace, bandeja, autorizar/denegar, listar y reabrir |
| `Common/Errors/WorkspaceMemberErrorMapper.cs` | nuevo | Traducción a HTTP extraída de `WorkspaceMembersController` (segundo consumidor) |
| `Common/Errors/{ErrorCodes,ApiError}.cs` | modificado | Códigos de propiedad, baja y reactivación |
| `Infrastructure/Data/TerrenarioDbContext.cs` · `Program.cs` | modificado | Mapeos, índices y DI |

### Frontend

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `types/workspace-lifecycle.types.ts` | nuevo | Modos, opciones, resultados, solicitudes |
| `services/workspace-lifecycle.service.ts` | nuevo | Dos fábricas sobre el cliente HTTP común: ciclo de vida y reactivación |
| `components/settings/AjustesView.tsx` | nuevo | Renombrar + zona de propiedad y baja (`/app/ajustes`) |
| `components/settings/CloseWorkspaceModal.tsx` | nuevo | Diálogo que plantea **la decisión concreta** de cada caso |
| `components/workspace/ReactivationRequestPage.tsx` | nuevo | Pantalla del enlace recibido por email (HU-5) |
| `components/workspace/ReactivationInboxPage.tsx` | nuevo | Bandeja de decisiones + Workspaces dados de baja (HU-6) |
| `contexts/WorkspaceContext.tsx` | modificado | `refreshContext()`: resincroniza activo + lista sin recrear la sesión |
| `App.tsx` | modificado | Rutas `/app/ajustes`, `/reactivations` y `/reactivations/:token` |
| `components/layout/AppSidebar.tsx` · `AppLayout.tsx` | modificado | Enciende «Ajustes» (dejaba de estar en «Pronto») y su título |

## Diseño detallado

### Modelo de datos

```sql
ALTER TABLE workspaces
    ADD COLUMN deleted_at         TIMESTAMPTZ NULL,
    ADD COLUMN deleted_by_user_id UUID NULL REFERENCES users(id) ON DELETE RESTRICT;

CREATE INDEX ix_workspaces_live ON workspaces (deleted_at) WHERE deleted_at IS NULL;

CREATE TABLE workspace_reactivation_requests (
    id                 UUID PRIMARY KEY,
    workspace_id       UUID NOT NULL REFERENCES workspaces(id) ON DELETE CASCADE,
    recipient_user_id  UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    authorizer_user_id UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    token_hash         TEXT NOT NULL,
    status             VARCHAR(20) NOT NULL,
    expires_at         TIMESTAMPTZ NOT NULL,
    created_at         TIMESTAMPTZ NOT NULL,
    requested_at       TIMESTAMPTZ NULL,
    resolved_at        TIMESTAMPTZ NULL
);

CREATE UNIQUE INDEX "IX_workspace_reactivation_requests_token_hash"
    ON workspace_reactivation_requests (token_hash);
```

- **`deleted_by_user_id` con `ON DELETE RESTRICT`**, igual que `invited_by_user_id` de las
  invitaciones: es la única persona que puede autorizar la reactivación (CA-10), así que la
  referencia no puede desaparecer sin más. La cuenta tampoco se borra físicamente hoy.
- **Una solicitud por miembro notificado**, no un enlace común: el traspaso queda atado a quien lo
  pide y un enlace filtrado no sirve a un tercero.
- **`token_hash` único**; el valor en claro solo viaja en el email, como en MVP-103.
- El estado `cerrada` distingue «este enlace dejó de servir porque el Workspace ya volvió» de
  `denegada` (hubo una decisión). No atribuir decisiones que no existieron mantiene el dato honesto.

### API / Contratos

```yaml
# PATCH /api/v1/workspaces/active            [RequireWorkspaceScope]
request:  { name* }
responses:
  200: { id, name }
  400: { error: { code: "VALIDATION_REQUIRED_WORKSPACE_NAME" | "VALIDATION_WORKSPACE_NAME_LENGTH" } }

# GET  /api/v1/workspaces/active/closure     [RequireWorkspaceScope]
responses:
  200: { workspace, is_owner, mode, active_owners, successor_name, candidates[] }
       # mode ∈ auto_transfer | choose | only_delete | not_owner

# POST /api/v1/workspaces/active/closure     [RequireWorkspaceScope]
responses:
  200: { outcome: "transferred" | "deleted", workspace, new_owner_name, notified_members, emails_sent }
  403: { error: { code: "AUTH_WORKSPACE_OWNER_REQUIRED" } }

# POST /api/v1/workspaces/active/transfer-ownership   [RequireWorkspaceScope]
request:  { new_owner_user_id* }
responses:
  200: { outcome: "transferred", ... }
  403: { error: { code: "AUTH_WORKSPACE_OWNER_REQUIRED" } }
  404: { error: { code: "RESOURCE_NOT_FOUND" } }          # no es miembro activo
  422: { error: { code: "BUSINESS_RULE_OWNERSHIP_TRANSFER_TO_SELF" } }

# GET  /api/v1/workspaces/ownership-obligations         (sin scope)
responses:
  200: { data: [{ workspace_id, name, other_active_members, can_transfer }], meta: { total, is_clear } }

# GET  /api/v1/workspaces/reactivations/{token}         (sin scope)
# POST /api/v1/workspaces/reactivations/{token}/request (sin scope)
responses:
  200: { id, workspace, closed_by, status, expires_at, is_expired, can_request }
  404: { error: { code: "REACTIVATION_REQUEST_NOT_FOUND" } }   # inexistente o de otra persona
  422: { error: { code: "BUSINESS_RULE_REACTIVATION_ALREADY_USED"
                       | "BUSINESS_RULE_REACTIVATION_EXPIRED"
                       | "BUSINESS_RULE_WORKSPACE_NOT_DELETED" } }

# GET  /api/v1/workspaces/reactivations                 (sin scope)
# POST /api/v1/workspaces/reactivations/{id}/authorize  → 200 { workspace, new_owner_user_id }
# POST /api/v1/workspaces/reactivations/{id}/deny       → 204
# GET  /api/v1/workspaces/reactivations/closed          → 200 { data: [{ id, name, closed_at }] }
# POST /api/v1/workspaces/reactivations/closed/{id}/reopen → 200 { id, name }
```

### Lógica de negocio

- **Renombrar.** `Workspace.Rename` reutiliza la normalización y los límites del alta (MVP-102) y
  rechaza operar sobre un Workspace dado de baja. Sin guarda de rol (RN-034).
- **Árbol de decisión.** `GetWorkspaceClosureOptionsHandler` traduce el spec a un `mode`. El sucesor
  anunciado en `auto_transfer` se calcula con **el mismo criterio** que aplica el traspaso automático
  (copropietario activo de membresía más antigua), para que la confirmación no prometa una cosa y el
  servidor haga otra.
- **Baja.** `CloseWorkspaceHandler` exige `workspace_owner` (403 `AUTH_WORKSPACE_OWNER_REQUIRED`) y
  bifurca: con sucesor, reasigna y revoca al solicitante; sin él, `SoftDelete` + una solicitud por
  miembro activo restante + correo. Un fallo del proveedor de email **no invalida** la baja (mismo
  criterio que la emisión de invitaciones): se refleja en `emails_sent`.
- **Reactivación.** `Submit` consume el enlace (un solo uso) y exige ser el destinatario; `Authorize`
  exige ser quien dio de baja, reactiva, traspasa y **cierra el resto de enlaces vivos** del
  Workspace para que ninguno encadene una segunda reactivación. Todo en la misma transacción, de
  forma que no existe un instante con el Workspace vivo y sin propietario.
- **No-orfandad (CA-9).** `WorkspaceOwnershipGuard.EnsureAccountClosureAllowedAsync` lanza
  `BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED` mientras `ListSoleOwnedAsync` devuelva algo. Es el
  punto de enganche que el futuro flujo de baja de cuenta (P-024) tendrá que llamar.

### Baja lógica sin retrabajo en los maestros

Ningún maestro de MVP-202/203/204/205 se toca. Todos resuelven su ámbito con
`[RequireWorkspaceScope]` → `IActiveWorkspaceResolver` → `IWorkspaceRepository`, así que basta con
que el repositorio deje de ver los Workspaces dados de baja para que:

- el Workspace desaparezca del selector y deje de resolver contexto (CA-8);
- si era el activo del usuario, la sesión **caiga al Workspace por defecto** (MVP-104), porque
  `FindForMemberAsync` devuelve `null` y el resolutor continúa con `FindDefaultForUserAsync`;
- sus recursos con ámbito dejen de ser accesibles sin borrarse (CA-2).

`FindDefaultForUserAsync` pasa a ordenar en cliente: EF+SQLite no traduce `ORDER BY` sobre
`DateTimeOffset` y esa consulta —justo la que sostiene la caída al Workspace por defecto— quedaba
fuera del alcance de los tests de SQL real. Es el mismo criterio ya adoptado en `ListPendingEmailAsync`
(MVP-204). Se registra como punto transversal en `MVP-999`.

### Cliente (frontend)

«Ajustes» (`/app/ajustes`) enciende la entrada del menú que estaba en «Pronto» y aloja el renombrado
y la zona de propiedad y baja; queda **fuera de la guarda de oferta de temporada**, como el resto de
administración. El diálogo de baja no es un «¿seguro?» genérico: plantea la decisión concreta del
caso y, en `choose`, **el botón permanece deshabilitado hasta elegir** (CA-3). Las pantallas de
reactivación viven fuera del shell y de la guarda de Workspace, porque quien dio de baja su único
Workspace no tiene contexto activo y esa pantalla es justo su forma de recuperarlo.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
| ----------- | ------------------- |
| Borrado físico con confirmación (RN-037) | RN-037 habla de registros operativos; un Workspace arrastra todos los datos de la explotación. El spec exige baja lógica |
| Operar sobre cualquier Workspace por `workspace_id` | Rompe el patrón Workspace-first de MVP-105 (RN-034) y obliga a una pantalla nueva de gestión. Se opera sobre el activo, cambiando con el selector |
| Filtro global de EF (`HasQueryFilter`) para la baja lógica | Invisible y difícil de saltarse cuando hace falta (reactivación). Se prefiere un `LiveWorkspaces` explícito en el puerto, con una única excepción nombrada |
| Un enlace de reactivación común para todos los miembros | El traspaso quedaría atado al enlace y no a quien lo pide; un reenvío del correo abriría la puerta a un tercero |
| Que quien traspasa salga siempre del Workspace | Traspasar no es irse; mezclarlo obligaría a re-invitarse para seguir trabajando. Salir es la retirada de acceso de MVP-204 |
| Reutilizar `IInvitationTokenService` en la reactivación | El nombre ata el puerto a las invitaciones. Se introduce `IOneTimeTokenService` (misma implementación) para no duplicar el esquema ni forzar la semántica |
| Notificar la solicitud solo en la campanita (MVP-107) | El centro de notificaciones solo cubre invitaciones y se refresca al montar sesión (P-011); quien dio de baja su único Workspace puede no tener shell. El correo es la vía fiable; la notificación in-app se registra en `MVP-999` |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
| ------ | ------------ | ---------- |
| Dejar un Workspace sin propietario | baja | Traspaso y reactivación resuelven propiedad en la misma transacción; tests de dominio y de handler sobre cada rama del árbol |
| Que una consulta olvide el filtro de baja lógica | media | `LiveWorkspaces` como base única del repositorio + test SQLite que barre **todas** las lecturas del puerto contra un Workspace dado de baja |
| Reactivación por una persona no autorizada | baja | Token de un solo uso ligado al destinatario + autorización exclusiva de quien dio de baja; ambos casos ocultos como 404. Verificado end-to-end |
| Enlaces antiguos que encadenen reactivaciones | baja | Al autorizar o reabrir se cierran (`cerrada`) todos los enlaces vivos del Workspace |
| Pérdida de datos por una baja precipitada | baja | Baja **lógica**, reversible por quien la hace y solicitable por los miembros; la UI lo dice de forma explícita antes de confirmar |
| Confundir «dar de baja» con «salir» cuando hay copropietarios | media | El servidor devuelve el `mode` y la UI cambia el nombre de la acción a «Salir y ceder mi propiedad», nombrando al sucesor |

## Impacto en la usabilidad

- **«Ajustes» deja de estar en «Pronto»**: es la primera entrada del shell que se enciende fuera de
  los maestros. No rompe ningún flujo previo.
- **La acción se llama por lo que hace.** Con copropietarios no dice «dar de baja» sino «salir y
  ceder mi propiedad», anunciando a quién pasa. Sin copropietarios, la confirmación explica que no
  se borra nada y dónde volver a levantarlo.
- **La decisión del propietario único es obligatoria**, no un valor por defecto: el botón está
  deshabilitado hasta elegir traspasar o dar de baja (CA-3).
- **Nadie queda en un callejón.** Quien da de baja su único Workspace cae en el onboarding con la
  pantalla de reactivación a un enlace de distancia; quien recibe el enlace siempre tiene salida
  hacia la plataforma.
- **Limitación conocida**: el aviso de que hay una solicitud pendiente llega **solo por email**; no
  hay notificación in-app (la campanita de MVP-107 solo cubre invitaciones). Registrado en `MVP-999`.

## Plan de testing

> Referencia: `docs/04-ingenieria/estrategia-testing.md`

- [x] Tests de dominio: `WorkspaceLifecycleTests` (renombrado y sus validaciones, baja lógica que
  conserva los datos, doble baja, renombrar sobre un Workspace dado de baja, reactivación, traspaso y
  traspaso a uno mismo, promoción/degradación de membresía) y `WorkspaceReactivationRequestTests`
  (un solo uso, caducidad, destinatario, autorización exclusiva, cierre de enlaces).
- [x] Tests de handlers (NSubstitute): `RenameWorkspaceHandlerTests`,
  `GetWorkspaceClosureOptionsHandlerTests` (los cuatro modos + sucesor más antiguo + revocados que no
  son candidatos), `CloseWorkspaceHandlerTests` (guardas de propietario, reasignación con salida,
  baja con avisos, baja sin nadie a quien avisar, fallo de correo que no invalida la baja),
  `TransferWorkspaceOwnershipHandlerTests`, `ReactivationHandlersTests` (solicitar, ocultar el enlace
  ajeno, Workspace ya vivo, autorizar con traspaso, cierre del resto de enlaces, denegar),
  `ReopenWorkspaceHandlerTests` y `WorkspaceOwnershipGuardTests` (CA-9).
- [x] Tests contra SQLite real (`WorkspaceLifecycleRepositorySqliteTests`): un Workspace dado de baja
  desaparece de **todas** las lecturas del puerto pero conserva sus datos; caída al Workspace por
  defecto; sucesor determinista ignorando propietarios sin acceso; `ListSoleOwnedAsync` (CA-9).
- [x] Tests de composición de email (`WorkspaceLifecycleEmailComposerTests`): destinatario, enlace y
  escapado del nombre en HTML.
- [x] Verificación end-to-end real (API :5127 + PostgreSQL + UI conducida :5173, con JWT de
  desarrollo firmado con la clave RSA local):
  - Renombrar: nombre vacío → `400`; renombrado por el propietario y **por un miembro no propietario**
    → `200`; el selector y la cabecera reflejan el nombre nuevo sin recrear sesión (CA-1).
  - Árbol de decisión: `choose`, `auto_transfer` (con `successor_name`), `only_delete` y `not_owner`.
  - Guardas: baja por un no propietario → `403 AUTH_WORKSPACE_OWNER_REQUIRED`; traspaso a uno mismo →
    `422`; traspaso a quien no es miembro activo → `404`.
  - CA-4: traspaso explícito → propiedad al elegido y quien traspasa **sigue activo** como miembro.
  - CA-5: baja con copropietarios → `outcome: transferred`, propiedad al copropietario y solicitante
    `revocado` (verificado en base de datos).
  - CA-2/CA-6: baja de propietario único → `deleted_at` persistido, `notified_members: 1`,
    `emails_sent: 1` y solicitud creada con `token_hash`.
  - CA-8: con el claim apuntando al Workspace dado de baja, `GET /workspaces/active` **cae al
    Workspace por defecto**, el dado de baja no aparece en `GET /workspaces` y los recursos con
    ámbito resuelven contra el de reemplazo.
  - CA-10/CA-7: preview desde otra cuenta → `404`; solicitud del destinatario → `200`; segundo uso →
    `422 BUSINESS_RULE_REACTIVATION_ALREADY_USED`; autorización por otra cuenta → `404`; autorización
    por quien dio de baja → Workspace reactivado y propiedad al solicitante (verificado en base de
    datos y en el selector del solicitante).
  - Reapertura: solo la ve y la ejecuta quien dio de baja (`404` para otra cuenta y para un segundo
    intento).
  - CA-9: `GET /workspaces/ownership-obligations` lista las propiedades únicas con `can_transfer`.
  - UI conducida: «Ajustes» renombra y refleja el cambio en el selector; el diálogo de `choose`
    mantiene **deshabilitada** la confirmación hasta elegir y ofrece el desplegable de candidatos; el
    traspaso deja la sección como «no propietario»; el enlace de reactivación solicita el traspaso; la
    bandeja de quien dio de baja autoriza y el Workspace vuelve.
  - Datos de prueba retirados de la base de datos de desarrollo al terminar.
- [ ] Tests de integración contra PostgreSQL de todos los endpoints: pendientes del arnés común (MVP-501).

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Migración de base de datos preparada (`AddWorkspaceLifecycle`) y aplicada en local
- [x] Tests escritos y pasando (dominio + handlers + SQLite real + composición de email)
- [x] Documentación de API actualizada (rutas nuevas y códigos de error)
- [x] Modelo de datos actualizado (`WORKSPACE` con baja lógica; `WORKSPACE_REACTIVATION_REQUEST`)
- [x] Reglas de negocio nuevas formalizadas (RN-038, RN-039, RN-040)
- [x] Puntos de coherencia registrados en `MVP-999` (P-004 resuelto; puntos nuevos)
- [x] Verificación end-to-end real (API + PostgreSQL + UI conducida)
- [x] Sin `TODO` sin resolver en este documento
