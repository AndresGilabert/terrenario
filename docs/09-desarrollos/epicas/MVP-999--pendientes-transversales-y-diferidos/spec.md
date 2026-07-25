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
actualizado_en: "2026-07-25"
---
<!-- actualizado_en refleja la ultima anotacion en el registro de puntos (P-005..P-007 detectados en MVP-105). -->

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
