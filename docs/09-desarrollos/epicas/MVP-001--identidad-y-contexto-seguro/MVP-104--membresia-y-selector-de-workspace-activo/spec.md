---
id: "MVP-104"
tipo: feature
titulo: "Membresía y selector de Workspace activo"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-101", "MVP-102", "MVP-103"]
bloquea: ["MVP-105", "MVP-002"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "membresia", "contexto-activo"]
  modulo_path: "03-modulos/"
  componentes: ["workspace-members", "workspace-selector", "ui-shell"]
  etiquetas: ["mvp", "workspace", "membership"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-104 — Membresía y selector de Workspace activo

## Contexto

Un usuario puede pertenecer a varios Workspaces y el producto debe hacer visible en todo momento sobre cuál está operando. Sin un selector claro y un modelo mínimo de membresía, la multi-explotación queda confusa y propensa a errores de contexto.

## Objetivo

Permitir que el usuario vea sus Workspaces disponibles, cambie el Workspace activo y mantenga ese contexto durante la navegación del MVP.

## Requisitos de usuario

### HU-1 — Ver mis Workspaces disponibles

**Como** usuario con acceso a varios Workspaces,
**quiero** ver la lista de Workspaces a los que pertenezco,
**para** saber entre cuáles puedo alternar.

### HU-2 — Cambiar el Workspace activo

**Como** usuario multi-Workspace,
**quiero** seleccionar el Workspace activo desde la interfaz,
**para** operar sobre el contexto correcto sin ambigüedad.

## Alcance (in-scope)

- Listado de membresías activas del usuario.
- Selector visible de Workspace activo.
- Persistencia razonable del Workspace activo durante la sesión.
- Cambio de contexto sin mezcla de datos entre Workspaces.
- Estados mínimos de membresía (`invitado`, `activo`, `revocado`) reflejados en el flujo.

## Fuera de alcance (out-of-scope)

- Jerarquías de Workspaces.
- Favoritos, agrupaciones o filtros avanzados de Workspaces.
- Administración avanzada de miembros más allá del alcance base.

## Criterios de aceptación

- [ ] **CA-1**: Un usuario puede ver todos sus Workspaces disponibles y distinguir el activo.
- [ ] **CA-2**: Al cambiar de Workspace, la aplicación actualiza el contexto sin mostrar datos cruzados del Workspace anterior.
- [ ] **CA-3**: El contexto activo queda disponible para las operaciones posteriores del MVP.

## Maquetas y referencias visuales

- Referencia funcional: requisito de selector visible en `vision-y-objetivos.md`.

## Notas y decisiones

- Esta historia es condición práctica para que los siguientes módulos sean realmente Workspace-first.
- No introduce permisos granulares; solo hace visible y operativo el contexto activo.