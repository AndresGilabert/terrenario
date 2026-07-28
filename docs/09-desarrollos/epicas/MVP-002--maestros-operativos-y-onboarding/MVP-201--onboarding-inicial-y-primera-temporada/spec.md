---
id: "MVP-201"
tipo: feature
titulo: "Onboarding inicial del Workspace y primera temporada"
estado: completado
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
actualizado_en: "2026-07-28"
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

> **Ajuste por decisión de producto (2026-07-27)**: no se crea temporada por defecto. La CA-1 se
> reinterpreta como "se **ofrece** crear una temporada inicial activa, de forma cancelable", en vez
> de "el Workspace termina con una temporada" (que impondría un dato no querido). Ver Notas.

- [x] **CA-1 (reinterpretada)**: Al crear un Workspace nuevo se ofrece crear una temporada inicial activa, sin configuración compleja y de forma cancelable. _(Guarda de oferta tras el alta; verificado por UI y API.)_
- [x] **CA-2**: El usuario puede continuar desde ese punto con la configuración de maestros del MVP. _(El Home muestra la temporada activa o el acceso para crearla; base de autoselección RN-021 lista.)_
- [x] **CA-3**: El sistema mantiene la restricción de una única temporada activa por Workspace desde el primer uso. _(Índice único parcial `ux_seasons_workspace_active` + 409; test SQLite de la invariante.)_

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep1.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep1.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/OnboardingStep2.tsx](../../../../../prototype/terrenario-mvp/src/components/OnboardingStep2.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| OnboardingStep1 | RN-021 | cubierto | Alta de Workspace; sin contador "Paso X de Y" (P-010 resuelto) |
| OnboardingStep2 | RN-021, RN-022 | cubierto | Pantalla de creación de temporada (oferta cancelable); `POST /seasons`; UI conducida verificada |

## Notas y decisiones

- Esta historia acelera el arranque, pero no sustituye la historia específica de gestión completa de temporadas (MVP-203).
- **Decisión de producto (2026-07-27)**: **no se crea ninguna temporada por defecto**. La temporada
  es un acto explícito y **cancelable** del usuario. La app **ofrece** crearla en dos momentos:
  (a) al crear un Workspace y (b) cuando el Workspace activo no tiene temporada (p. ej. al
  seleccionarlo). "Ahora no" entra a la app sin crear ninguna; queda un acceso para hacerlo después.
- Este mecanismo **también cubre los Workspaces preexistentes** sin temporada (al activarlos se
  ofrece crearla), por lo que no hace falta ningún backfill de datos.
- El endpoint de creación (`POST /api/v1/seasons`) es mínimo (crea la primera temporada activa; 409
  si ya hay). El CRUD completo (varias temporadas, editar, cerrar, cambiar de activa) es MVP-203;
  registrado como punto en MVP-999 (P-017).
- Detalle técnico de la implementación: [tech-design.md](./tech-design.md).
