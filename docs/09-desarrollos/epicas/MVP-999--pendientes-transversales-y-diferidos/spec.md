---
id: "MVP-999"
tipo: epica
titulo: "Pendientes transversales y diferidos"
estado: borrador
prioridad: media
hito: "Hito Z — Cierre de pendientes transversales"
tickets: []
historias: []
depende_de: []
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "priorizacion", "calidad"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "triage", "planificacion"]
  etiquetas: ["mvp", "pendientes", "transversal"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-07-26"
---
<!-- actualizado_en refleja la ultima anotacion en el registro de puntos (P-011..P-016 detectados durante el desarrollo de MVP-107; P-013..P-016 resueltos en el mismo). -->

# EPICA MVP-999 — Pendientes transversales y diferidos

## Contexto

Durante el desarrollo aparecen requisitos o ajustes que no encajan de forma clara en las epicas activas, o que no bloquean la planificacion inmediata y conviene diferir para no frenar la entrega de valor principal.

Sin una epica explicita para estos casos, los pendientes quedan dispersos y se pierde capacidad de priorizacion transversal.

## Objetivo

Centralizar y priorizar las historias detectadas fuera del encaje natural de las epicas activas, manteniendo trazabilidad y control de alcance sin detener el desarrollo en curso.

## Requisitos de usuario de alto nivel

- **Como** Product Owner, **quiero** un contenedor unico para pendientes transversales o diferibles, **para** no bloquear la planificacion de las epicas activas.
- **Como** equipo de desarrollo, **quiero** que esos pendientes se conviertan en historias formales, **para** tratarlos con criterios de prioridad y calidad equivalentes al resto del backlog.

## Alcance

- Alta de historias que no encajan de forma clara en epicas MVP-001..MVP-006.
- Alta de historias detectadas durante revisiones de epica que pueden posponerse sin bloquear hitos activos.
- Priorizacion y secuenciacion de pendientes transversales al cierre del roadmap principal.

## Fuera de alcance

- Resolver incidencias criticas que bloqueen epicas activas: esas deben ubicarse en su epica correspondiente.
- Sustituir la refinacion normal de historias dentro de cada epica.
- Acumular trabajo indefinidamente sin decision de prioridad.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias dadas de alta en MVP-999 tienen justificacion de por que no encajan en otra epica activa.
- [ ] **CA-2**: Cada historia de MVP-999 tiene criterios de aceptación verificables y trazabilidad de origen.
- [ ] **CA-3**: El backlog de MVP-999 se revisa y prioriza de forma periodica hasta su cierre.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

## Registro de puntos para revision final

Usa esta seccion para anotar hallazgos durante el desarrollo de otras epicas sin crear historias todavia.
Cuando una epica cierre su `MVP-x99`, estos puntos deben revisarse, priorizarse y convertirse en historias si aplica.

| Punto | Fecha deteccion | Origen (epica/historia) | Tipo | Descripcion breve | Impacto | Bloqueante | Destino propuesto | Estado de revision | Historia creada |
|---|---|---|---|---|---|---|---|---|---|
| P-001 | 2026-07-24 | MVP-001 / MVP-103 | ux | Definir inventario y plantillas unificadas para todos los emails salientes del producto, incluyendo criterios de contenido legal (RGPD/LOPDGDD y LSSI/ePrivacy si aplica), para planificar su maquetacion coherente en un bloque transversal. | medio | no | MVP-999 | pendiente | - |
| P-002 | 2026-07-24 | MVP-001 / MVP-104 | funcional | Administracion de miembros del Workspace: listar miembros activos y revocar acceso (transicion de `status` a `revocado`) desde la UI. El estado `revocado` del catalogo `worker_member_status` y el metodo de dominio `WorkspaceMember.Revoke()` ya estan implementados en MVP-104, pero no hay endpoint ni pantalla que los use. Queda fuera del alcance de MVP-104 ("Administracion avanzada de miembros"). Encaja en MVP-204 (maestro de trabajadores y miembros). | medio | no | MVP-204 | aprobado-crear-historia | MVP-204 (HU-3, CA-4..CA-6) |
| P-003 | 2026-07-24 | MVP-001 / MVP-104 | funcional | El estado `invitado` del catalogo `worker_member_status` debe existir y usarse: debe poder verse la lista de personas que pertenecen a un Workspace con su estado (`invitado`/`activo`/`revocado`) y, para las que esten en `invitado`, poder reenviar la invitacion por email o por enlace igual que la primera vez. Confirmado como requisito (no como mero valor reservado). Encaje analizado: administracion de miembros = misma superficie que P-002, por lo que se incorpora a MVP-204 (HU-3/HU-5, CA-4/CA-5/CA-6). Nota tecnica: `workspace_members.user_id` es NOT NULL con FK a `users` y el invitado por email puede no tener cuenta aun; la representacion del estado `invitado` (vista unificada sobre `workspace_invitations` vs. fila materializada) se decide en el tech-design de MVP-204. No impacta MVP-104 (el selector solo mira membresias `activo`). | bajo | no | MVP-204 | aprobado-crear-historia | MVP-204 (HU-3, HU-5, CA-4..CA-6) |
| P-004 | 2026-07-24 | MVP-001 / MVP-102 / MVP-104 | funcional | Gestion del ciclo de vida del Workspace: hoy existe alta (`POST /api/v1/workspaces`) y cambio de activo (`PUT /api/v1/workspaces/active`), pero no hay plan explicito para edicion (renombrado/ajustes) ni eliminacion (baja logica o fisica) de Workspaces existentes. Definir alcance MVP/post-MVP, reglas de seguridad (quien puede hacerlo), precondiciones (workspace activo, miembros, datos historicos) y contrato API/UI. **Trasladado a triage prioritario de la epica en curso (MVP-199).** | alto | si | MVP-001 / MVP-199 | aprobado-crear-historia | MVP-199 (triage en curso) |
| P-005 | 2026-07-25 | MVP-001 / MVP-105 | seguridad | Headers de seguridad HTTP obligatorios por `docs/07-seguridad/autenticacion-autorizacion.md` (`Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`) no implementados en `Program.cs` y no asignados a ninguna historia. Detectado al cerrar el perimetro de seguridad en MVP-105. **Decision con PO: por ser seguro por defecto y transversal, se implementa YA en MVP-105 (`SecurityHeadersMiddleware`) en vez de diferirlo, para que todo desarrollo posterior lo herede sin retrofit.** | medio | no | MVP-105 | resuelto | MVP-105 |
| P-006 | 2026-07-25 | MVP-001 / MVP-105 | observabilidad | `X-Request-Id` en todas las respuestas, exigido por las convenciones de `docs/02-arquitectura/contratos-api.md` (trazabilidad y correlacion de errores 500), no implementado ni asignado. Transversal a toda la API. **Decision con PO: refuerza directamente la "trazabilidad minima" de MVP-105 y es seguro por defecto, se implementa YA en MVP-105 (`RequestIdMiddleware`) en vez de diferirlo.** | medio | no | MVP-105 | resuelto | MVP-105 |
| P-007 | 2026-07-25 | MVP-001 / MVP-105 | tecnico | Cliente HTTP comun en el frontend con manejo centralizado de 401/403 de scope. Hoy cada `*.service.ts` hace `fetch` a mano. Con el enforcement introducido en MVP-105, cuando lleguen recursos con ambito de Workspace conviene un cliente unico que reaccione a `AUTH_WORKSPACE_SCOPE_REQUIRED` (forzar onboarding/seleccion de Workspace) y `AUTH_WORKSPACE_FORBIDDEN` (refrescar contexto), ademas del refresh de token ya existente. **Decision con PO: se difiere a MVP-202 (primer maestro con recurso scoped), donde la UX del 403 quedara definida; construirlo ahora sin consumidor real seria una abstraccion prematura.** | bajo | no | MVP-202 | aprobado-crear-historia | MVP-202 (enganchar al primer recurso con scope) |
| P-008 | 2026-07-25 | MVP-001 / MVP-106 (rev. MVP-199) | legal/ux | Paginas de **Politica de Privacidad** y **Terminos del Servicio** para el usuario final y **consentimiento de cookies**. Hoy los enlaces del login (`LoginPage.tsx:119,123`) apuntan a rutas inexistentes; MVP-106 solo corrige el comportamiento roto de los enlaces, no crea el contenido. El marco de cumplimiento existe solo como doc interno (`docs/07-seguridad/privacidad-datos.md`, RGPD/LOPDGDD, LSSI/ePrivacy). Requiere contenido legal validado. Enlazar con P-001 (emails y criterios legales). | medio | no | MVP-005 / MVP-502 | pendiente | - |
| P-009 | 2026-07-25 | MVP-001 / MVP-101 (rev. MVP-199) | tecnico | Codigo muerto en el login: boton "Acceder como invitado / Demo" (`LoginPage.tsx:105-114`) que nunca se cablea (`onDemoAccess` no se pasa desde `App.tsx`). Retirar o decidir su uso. **Resuelto: retirada oportunista en MVP-106 (prop `onDemoAccess` y bloque demo eliminados de `LoginPage.tsx`).** | bajo | no | MVP-106 | resuelto | MVP-106 |
| P-010 | 2026-07-25 | MVP-001 / MVP-102 (rev. MVP-199) | ux | El asistente de creacion de Workspace muestra "Paso 1 de 3" (`CreateWorkspacePage.tsx`) pero los pasos 2-3 (temporada inicial, MVP-201) no existen aun en el build; el indicador puede confundir. Ajustar el copy/indicador mientras MVP-201 no este entregado. | bajo | no | MVP-999 | pendiente | - (posible ajuste en MVP-106) |
| P-011 | 2026-07-26 | MVP-001 / MVP-107 | ux/tecnico | La bandeja/campanita de invitaciones recibidas (MVP-107) se refresca solo al montar la sesion: una invitacion recibida mientras el usuario ya esta dentro no aparece hasta recargar o re-loguear. El tiempo real y los canales externos estan fuera de alcance de MVP-107. Evaluar un refresco ligero al recuperar foco de ventana y/o polling con intervalo, generalizable cuando el centro de notificaciones cubra mas tipos (RU-31). | bajo | no | MVP-999 | pendiente | - |
| P-012 | 2026-07-26 | MVP-001 / MVP-107 | tecnico/calidad | El frontend sigue sin arnes de tests unitarios (`vitest`/`jest`), ya señalado en MVP-106. La logica añadida en MVP-107 (`NotificationsContext` con tracking de "vistas", `useInvitationActions`, mapeo de aptitud) queda cubierta solo por tipado + build + lint + QA manual. Introducir arnes minimo de frontend y cubrir esta logica de decision. Encaje analizado: candidato a MVP-501 (cobertura minima del nucleo) o historia propia si se quiere antes; se registra aqui para priorizar. | medio | no | MVP-501 / MVP-999 | pendiente | - |
| P-013 | 2026-07-26 | MVP-001 / MVP-107 | funcional/ux | Crear un Workspace **adicional** desde la app no estaba previsto en ninguna historia: MVP-102 cubre solo el **primero** ("primer acceso"; multiples explicitamente fuera de alcance) y MVP-104 solo el selector/cambio. El backend `POST /api/v1/workspaces` no restringe al primero, pero la UI solo abria el asistente en el estado "cero Workspaces" (`OnboardingRoute` redirige a `/app` si ya hay uno). Detectado al revisar el flujo durante MVP-107, que lo hizo mas visible (el invitado sin Workspace puede "crear el propio" pero quien ya tiene uno no). **Resuelto en MVP-107 por decision de producto**: accion "＋ Nuevo Workspace" en `WorkspaceSwitcher` → `/app/workspaces/new`, reutilizando `CreateWorkspacePage` en modo `additional`. Edicion/borrado siguen en P-004. | medio | no | MVP-107 | resuelto | MVP-107 |
| P-014 | 2026-07-26 | MVP-001 / MVP-107 (defecto en MVP-104) | bug/calidad | **Bug real en producción de la funcionalidad de listar/cambiar Workspaces (MVP-104).** `WorkspaceRepository.ListActiveMembershipsAsync` ordenaba con `.OrderBy(membership => membership.Name)` sobre el DTO proyectado en el `Join`; EF Core no lo sabe traducir a SQL y `GET /api/v1/workspaces` devolvía **HTTP 500** desde su entrega. El frontend lo tragaba a lista vacía, así que el selector solo mostraba el Workspace activo y parecía que los demás "desaparecían". Invisible hasta ahora porque el selector se deshabilitaba con lista vacía. **Resuelto en MVP-107**: ordenar por la columna real antes de proyectar. Brecha de testing asociada: los tests unitarios usan repositorios mockeados (NSubstitute) y no ejercitan la traducción EF; se añadió una regresión con SQLite real. La cobertura de integración contra PostgreSQL para todos los endpoints sigue pendiente en **MVP-501**. | alto | no | MVP-107 (fix) / MVP-501 (integración) | resuelto | MVP-107 |
| P-015 | 2026-07-26 | MVP-001 / MVP-107 (auditoria de fidelidad) | ux/tecnico | **Deuda transversal de sistema de diseño.** El frontend real nunca portó los fundamentos del prototipo (`prototype/terrenario-mvp`): tipografía **Inter + Plus Jakarta Sans** (clase `.font-headline`), iconografía **Material Symbols Outlined** y utilidades (`.ambient-shadow`, `.hide-scrollbar`). Sin esa base, cada pantalla improvisaba (emojis en vez de iconos, `font-bold` en vez de display). Detectado en la auditoría de fidelidad al cerrar la épica durante MVP-107. **Resuelto ahora**: fuentes + Material Symbols + utilidades portadas a `index.html`/`index.css`; emojis→iconos Material y `font-headline` aplicados en Login, onboarding, Landing (hero a 2 columnas, sin métricas inventadas por coherencia con MVP-106), selector, campanita y pantallas de invitación. La paleta de color ya se respetaba. | medio | no | MVP-107 (fix) | resuelto | MVP-107 |
| P-016 | 2026-07-26 | MVP-001 / MVP-107 (auditoria de fidelidad) | ux/tecnico | **Shell de aplicación ausente.** El área operativa era un placeholder centrado; MVP-104/107 improvisó una cabecera mínima en vez del shell del prototipo (Sidebar + TopNavbar). Detectado en la misma auditoría. **Resuelto ahora**: `AppSidebar` (marca, selector de Workspace, navegación con los 8 módulos del producto —los de épicas MVP-002..004 deshabilitados con "Pronto", sin enlaces rotos— y footer de usuario) + `AppTopbar` (título, campanita, menú móvil) + `AppLayout` con contenedor de contenido común (corrige la coherencia de tamaño/espaciado entre secciones). Las vistas operativas se encenderán en sus épicas. | medio | no | MVP-107 (fix) | resuelto | MVP-107 |

### Criterios de uso del registro

- `Estado de revision`: `pendiente`, `en-analisis`, `aprobado-crear-historia`, `descartado`.
- `Destino propuesto`: usar `MVP-999` solo si no encaja claramente en una epica activa o si es diferible.
- `Historia creada`: informar el ID final cuando el punto pase a historia formal.
- No borrar filas historicas: si un punto se descarta, conservarlo con motivo en `Descripcion breve`.

### Flujo al cierre de epicas

1. Cada historia `MVP-x99` revisa este registro y anade nuevos puntos detectados.
2. Se reevalua impacto y destino propuesto para cada punto pendiente.
3. Se crean historias nuevas para los puntos aprobados y se vincula el ID en `Historia creada`.
4. Se mantiene en `MVP-999` solo lo que siga pendiente de planificacion.

## Notas y decisiones

- MVP-999 no debe utilizarse para ocultar deuda critica de una epica activa.
- Si un pendiente termina encajando en una epica existente, debe moverse a esa epica en la siguiente refinacion.
- Se acuerda usar MVP-999 como contenedor transversal para documentar los envios de email del producto y planificar una maquetacion coherente al cierre del roadmap principal.
