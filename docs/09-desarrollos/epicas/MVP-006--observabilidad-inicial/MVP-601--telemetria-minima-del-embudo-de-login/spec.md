---
id: "MVP-601"
tipo: feature
titulo: "Telemetría mínima del embudo de login"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito F — Operación medible"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-006--observabilidad-inicial"
depende_de: ["MVP-105", "MVP-504"]
bloquea: ["MVP-603"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "autenticacion"]
  modulo_path: "03-modulos/"
  componentes: ["telemetria-login", "auth-events"]
  etiquetas: ["mvp", "telemetry", "login"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-601 — Telemetría mínima del embudo de login

## Contexto

La KB exige reconstruir el embudo de login hasta éxito o abandono con eventos mínimos y sin PII en claro. Esta es la pieza de observabilidad más directamente vinculada a la hipótesis de baja fricción del MVP.

## Objetivo

Emitir y conservar la telemetría mínima necesaria para medir conversión, error y abandono en el flujo de login del MVP.

## Requisitos de usuario

### HU-1 — Medir el login de extremo a extremo

**Como** responsable del producto,
**quiero** saber cuántos usuarios ven login, lo inician, lo completan o lo abandonan,
**para** validar si la entrada al MVP es realmente simple.

## Alcance (in-scope)

- Eventos mínimos del embudo de login definidos en observabilidad.
- Dimensiones mínimas para reconstrucción del flujo.
- Exclusión de PII sensible en claro.
- Preparación para explotación de alertas básicas posteriores.

## Fuera de alcance (out-of-scope)

- Analítica avanzada de cohortes o experimentación.
- Dashboards complejos de producto.

## Criterios de aceptación

- [ ] **CA-1**: El sistema emite los eventos mínimos del embudo de login definidos en la KB.
- [ ] **CA-2**: Cada evento contiene las dimensiones mínimas requeridas para reconstruir el flujo.
- [ ] **CA-3**: Ningún evento de login incluye PII sensible en claro.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| LoginPage | RN-020 | falta | No hay eventos de abandono/exito instrumentados |
| App rutas auth | RN-020 | parcial | Puntos de enganche definidos por navegacion |

## Notas y decisiones

- Esta historia convierte la traza mínima de login en una capacidad medible de operación.
