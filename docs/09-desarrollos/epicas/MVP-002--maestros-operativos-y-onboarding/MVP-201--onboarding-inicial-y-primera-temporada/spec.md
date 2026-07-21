---
id: "MVP-201"
tipo: feature
titulo: "Onboarding inicial del Workspace y primera temporada"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-102", "MVP-104"]
bloquea: ["MVP-202", "MVP-203", "MVP-204", "MVP-205"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["onboarding", "temporadas", "workspaces"]
  modulo_path: "03-modulos/"
  componentes: ["workspace-onboarding", "temporadas"]
  etiquetas: ["mvp", "onboarding", "temporada"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-201 — Onboarding inicial del Workspace y primera temporada

## Contexto

Tras crear un Workspace, el usuario no debería encontrarse una aplicación vacía. La KB ya fija que el sistema debe proponer o crear automáticamente una primera temporada para acelerar el arranque y permitir que el resto de maestros se creen con contexto operativo.

## Objetivo

Dejar a un Workspace recién creado en un estado inicial utilizable, con temporada base activa y sin pasos innecesarios previos al registro de datos.

## Requisitos de usuario

### HU-1 — Arrancar con una temporada inicial

**Como** usuario que acaba de crear un Workspace,
**quiero** disponer de una primera temporada activa,
**para** poder empezar a operar sin configuración adicional compleja.

### HU-2 — Entender el siguiente paso del onboarding

**Como** usuario nuevo,
**quiero** entrar a una aplicación preparada para completar maestros básicos,
**para** no perderme tras el primer acceso.

## Alcance (in-scope)

- Propuesta o creación automática de primera temporada al crear Workspace.
- Marcado de esa temporada como activa.
- Preparación mínima del flujo de onboarding posterior a la creación del Workspace.
- Persistencia del contexto necesario para continuar con maestros base.

## Fuera de alcance (out-of-scope)

- Configuración completa de terrenos, trabajadores o tareas.
- Personalización avanzada del onboarding.
- Creación de múltiples temporadas en el flujo inicial.

## Criterios de aceptación

- [ ] **CA-1**: Un Workspace nuevo termina con una temporada inicial activa sin requerir configuración manual compleja.
- [ ] **CA-2**: El usuario puede continuar desde ese punto con la configuración de maestros del MVP.
- [ ] **CA-3**: El sistema mantiene la restricción de una única temporada activa por Workspace desde el primer uso.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep1.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep1.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep2.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep2.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| OnboardingStep1 | RN-021 | parcial | Wizard inicial disponible |
| OnboardingStep2 | RN-021, RN-022 | parcial | Configuracion de temporada visible |

## Notas y decisiones

- Esta historia acelera el arranque, pero no sustituye la historia específica de gestión completa de temporadas.
