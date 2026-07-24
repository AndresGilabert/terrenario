---
id: "MVP-102"
tipo: feature
titulo: "Creación de Workspace y primer acceso guiado"
estado: completado
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
actualizado_en: "2026-07-24"
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

- [x] **CA-1**: Un usuario autenticado puede crear un Workspace desde el primer acceso en un flujo corto y comprensible.
- [x] **CA-2**: Al finalizar la creación, el Workspace nuevo queda activo para el usuario creador.
- [x] **CA-3**: La información mínima del Workspace queda disponible para continuar el onboarding del MVP.

## Diseño técnico

- Diseño técnico de la implementación: [tech-design.md](./tech-design.md)

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep1.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep1.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep2.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep2.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| OnboardingStep1 | RN-034 | cubierto | Alta real de Workspace con membresía activa del creador; tests unitarios de `Workspace` y `CreateWorkspaceHandler` |
| OnboardingStep2 | RN-021, RN-022, RN-023 | falta | Fuera de alcance de esta historia; la temporada inicial se implementa en `MVP-201` |

## Notas y decisiones

- Esta historia prepara el terreno para que `MVP-002` cree la primera temporada y el resto de maestros.
- Debe mantenerse mínima: crear contexto, no configurar aún el dominio.
- El Workspace activo viaja en el claim `workspace_id` del `access_token` y siempre se resuelve en
  servidor. El selector multi-Workspace y el listado de membresías quedan en `MVP-104`.
- El mapeo de `OnboardingStep1` se reasigna de RN-021 a RN-034: la pantalla crea el contenedor de
  negocio y su membresía; RN-021 (temporada operativa) se cubre en `MVP-201`.
