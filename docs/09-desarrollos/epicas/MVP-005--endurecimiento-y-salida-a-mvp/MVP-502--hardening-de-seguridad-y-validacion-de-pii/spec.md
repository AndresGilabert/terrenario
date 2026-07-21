---
id: "MVP-502"
tipo: feature
titulo: "Hardening de seguridad y validación de PII"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-001", "MVP-003", "MVP-004"]
bloquea: ["MVP-503", "MVP-504"]
relacionado_con: ["MVP-105", "MVP-601"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seguridad", "privacidad"]
  modulo_path: "03-modulos/"
  componentes: ["auth", "authorization", "logging", "pii-controls"]
  etiquetas: ["mvp", "security", "privacy"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-502 — Hardening de seguridad y validación de PII

## Contexto

El MVP trata identidad, membresía y datos operativos con componentes que pueden contener PII. La KB exige no exponer PII en logs, errores o URLs, y reforzar validación, autorización y seguridad por defecto antes de producción.

## Objetivo

Reducir el riesgo operativo del MVP reforzando controles de autenticación, autorización, validación de entrada y tratamiento seguro de PII.

## Requisitos de usuario

### HU-1 — Operar con seguridad por defecto

**Como** responsable técnico,
**quiero** que el MVP falle de forma segura y limite exposición de datos,
**para** no validar usuarios reales sobre una base débil.

### HU-2 — Evitar fugas de datos personales

**Como** responsable de cumplimiento,
**quiero** que logs, errores y flujos críticos eviten PII sensible en claro,
**para** respetar privacidad por diseño en el MVP.

## Alcance (in-scope)

- Revisión y refuerzo de autorización por Workspace.
- Revisión de validación de entrada en bordes API.
- Revisión de logs, errores y trazabilidad para evitar PII sensible en claro.
- Revisión de manejo seguro de identidad social y tokens.

## Fuera de alcance (out-of-scope)

- Certificaciones formales o auditorías externas.
- Rediseño mayor de arquitectura de seguridad.
- Hardening avanzado post-MVP que no bloquee salida inicial.

## Criterios de aceptación

- [ ] **CA-1**: Las operaciones críticas del MVP aplican controles de autorización y validación coherentes con la KB.
- [ ] **CA-2**: Logs, errores y trazas del MVP evitan exposición de PII sensible en claro.
- [ ] **CA-3**: El manejo de autenticación social y contexto de Workspace queda revisado antes de release.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| LoginPage | RN-017, RN-018, RN-036 | parcial | UX de acceso definida; sin hardening real |
| Ajustes/App | docs/07-seguridad/modelo-seguridad.md | falta | No se implementan controles de seguridad avanzados |

## Notas y decisiones

- Esta historia endurece el MVP ya construido; no debe introducir cambios funcionales de producto salvo los bloqueantes.
