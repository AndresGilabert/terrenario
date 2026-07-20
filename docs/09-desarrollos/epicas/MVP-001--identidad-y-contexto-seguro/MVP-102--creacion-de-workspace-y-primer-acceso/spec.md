---
id: "MVP-102"
tipo: feature
titulo: "Creación de Workspace y primer acceso guiado"
estado: borrador
prioridad: critica
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-101"]
bloquea: ["MVP-103", "MVP-104"]
relacionado_con: ["MVP-002"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["workspaces", "onboarding"]
  modulo_path: "03-modulos/"
  componentes: ["workspaces", "onboarding", "workspace-ui"]
  etiquetas: ["mvp", "workspace", "onboarding"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-102 — Creación de Workspace y primer acceso guiado

## Contexto

Tras autenticarse, el usuario necesita un contenedor de negocio donde vivirán todos los datos operativos. Sin un flujo de creación de Workspace simple, el alta termina en una aplicación vacía y sin contexto activo.

## Objetivo

Permitir que un usuario autenticado cree su primer Workspace y quede dentro de un contexto operativo inicial sin fricción innecesaria.

## Requisitos de usuario

### HU-1 — Crear un Workspace inicial

**Como** usuario autenticado,
**quiero** crear un Workspace en el primer acceso,
**para** empezar a registrar datos en mi propia explotación.

### HU-2 — Entrar directamente al contexto creado

**Como** usuario que acaba de crear su Workspace,
**quiero** quedar ubicado en ese Workspace activo,
**para** no tener que configurar manualmente el contexto antes de continuar.

## Alcance (in-scope)

- Flujo de creación de Workspace tras primer login.
- Activación automática del Workspace recién creado.
- Vínculo del usuario creador como miembro activo del Workspace.
- Estructura mínima necesaria para que el siguiente paso de onboarding pueda continuar.

## Fuera de alcance (out-of-scope)

- Configuración completa de maestros del Workspace.
- Invitación de otros miembros durante este flujo.
- Permisos avanzados de administración.
- Creación de múltiples Workspaces en una sola interacción inicial.

## Criterios de aceptación

- [ ] **CA-1**: Un usuario autenticado puede crear un Workspace desde el primer acceso en un flujo corto y comprensible.
- [ ] **CA-2**: Al finalizar la creación, el Workspace nuevo queda activo para el usuario creador.
- [ ] **CA-3**: La información mínima del Workspace queda disponible para continuar el onboarding del MVP.

## Maquetas y referencias visuales

- Referencia funcional: roadmap y cierre funcional del MVP en KB.

## Notas y decisiones

- Esta historia prepara el terreno para que `MVP-002` cree la primera temporada y el resto de maestros.
- Debe mantenerse mínima: crear contexto, no configurar aún el dominio.