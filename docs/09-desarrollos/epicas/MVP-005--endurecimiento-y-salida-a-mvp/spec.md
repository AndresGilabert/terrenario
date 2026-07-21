---
id: "MVP-005"
tipo: epica
titulo: "Endurecimiento y salida a MVP"
estado: borrador
prioridad: alta
hito: "Hito E — Salida controlada a MVP"
tickets: []
historias: ["MVP-501", "MVP-502", "MVP-503", "MVP-504"]
depende_de: ["MVP-001", "MVP-002", "MVP-003", "MVP-004"]
bloquea: ["MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "seguridad", "cumplimiento"]
  modulo_path: "03-modulos/"
  componentes: ["testing", "security-hardening", "release-gates"]
  etiquetas: ["mvp", "hardening", "release"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# EPICA MVP-005 — Endurecimiento y salida a MVP

## Contexto

La KB ya define un gate explícito de testing, seguridad y cumplimiento para permitir salida a producción. Si este bloque se deja implícito, el MVP puede dar la impresión de estar terminado solo por cubrir funcionalidad, sin evidencia suficiente de calidad o cumplimiento.

Esta épica agrupa el cierre técnico necesario antes de exponer el sistema a uso real.

## Objetivo

Llevar el núcleo funcional del MVP a un estado desplegable con riesgo controlado, cobertura mínima de pruebas y evidencia de cumplimiento legal y técnico.

## Requisitos de usuario de alto nivel

- **Como** responsable del producto, **quiero** que el MVP salga con controles mínimos de calidad y seguridad, **para** no validar usuarios reales sobre una base inestable.
- **Como** equipo técnico, **quiero** gates claros de salida, **para** saber cuándo el MVP es desplegable de forma responsable.

## Alcance

- Cobertura mínima de tests unitarios, de integración y smoke E2E según estrategia definida.
- Hardening de seguridad en autenticación, autorización, validación y gestión de PII.
- Revisión de checklist RGPD/LOPDGDD para flujos del MVP.
- Criterios de salida y checklist final de despliegue a staging/producción.
- Cierre de deuda bloqueante detectada durante construcción del núcleo MVP.

## Fuera de alcance

- Reingeniería mayor de arquitectura.
- Observabilidad avanzada o explotación analítica de telemetría.
- Automatizaciones post-MVP de sincronización u operación offline.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [ ] **CA-2**: Los gates mínimos de tests definidos en la KB están en verde para el alcance MVP.
- [ ] **CA-3**: Existe evidencia documental suficiente de cumplimiento y salida controlada antes de pasar a producción.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-501` — Cobertura mínima de tests del núcleo MVP.
- `MVP-502` — Hardening de seguridad y validación de PII.
- `MVP-503` — Checklist de cumplimiento RGPD/LOPDGDD del MVP.
- `MVP-504` — Gate final de release y salida controlada a staging/producción.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia para todas las historias de esta epica:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo solo aporta referencia visual para UX y navegacion.
- Si hay contradiccion, prevalece la KB.

Referencia base del prototipo:

- [prototype/terrenario-mvp/README.md](../../../../prototype/terrenario-mvp/README.md)
- [prototype/terrenario-mvp/src/App.tsx](../../../../prototype/terrenario-mvp/src/App.tsx)
- [prototype/reports/mvp-prototype-coverage.md](../../../../prototype/reports/mvp-prototype-coverage.md)

Matriz historia -> utilidad del prototipo:

| Historia | Referencias de prototipo | Cobertura |
|---|---|---|
| MVP-501 | [prototype/terrenario-mvp/src/App.tsx](../../../../prototype/terrenario-mvp/src/App.tsx) | Referencia de smoke visual para E2E manual; cobertura automatizada no incluida |
| MVP-502 | [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx), [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx) | Referencia de pantallas sensibles; hardening real de seguridad no implementado |
| MVP-503 | [prototype/terrenario-mvp/src/types.ts](../../../../prototype/terrenario-mvp/src/types.ts) | Referencia para inventario de datos en UI; cumplimiento legal debe definirse en KB y backend |
| MVP-504 | [prototype/terrenario-mvp/README.md](../../../../prototype/terrenario-mvp/README.md) | Referencia para run local; gate de release debe seguir criterios de KB |

## Notas y decisiones

- Esta épica no debe usarse para introducir nuevas capacidades funcionales salvo correcciones bloqueantes.
- La finalidad es estabilizar, no ensanchar alcance.
