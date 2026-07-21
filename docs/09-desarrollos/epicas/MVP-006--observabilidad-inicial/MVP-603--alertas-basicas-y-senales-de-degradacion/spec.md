---
id: "MVP-603"
tipo: feature
titulo: "Alertas básicas y señales de degradación"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito F — Operación medible"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-006--observabilidad-inicial"
depende_de: ["MVP-601", "MVP-602", "MVP-504"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "operacion"]
  modulo_path: "03-modulos/"
  componentes: ["alerts", "slo-signals", "health-metrics"]
  etiquetas: ["mvp", "alerts", "operations"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-21"
---

# MVP-603 — Alertas básicas y señales de degradación

## Contexto

Una vez existe telemetría mínima de login y dashboard, el equipo necesita un mínimo de señales para detectar degradaciones del MVP sin montar una plataforma pesada de operación. La KB ya fija SLOs y alertas básicas iniciales.

## Objetivo

Activar señales y alertas básicas que permitan detectar caídas, errores, latencia anómala y problemas graves de login en el MVP.

## Requisitos de usuario

### HU-1 — Detectar degradaciones críticas del MVP

**Como** responsable técnico,
**quiero** recibir señales básicas de degradación,
**para** actuar rápido cuando el MVP falle en los primeros usuarios reales.

## Alcance (in-scope)

- Señales de disponibilidad, 5xx y latencia P95.
- Alertas básicas ligadas a abandono/login y caída de conversión cuando aplique.
- Señales mínimas de salud operativa alineadas con SLOs definidos.

## Fuera de alcance (out-of-scope)

- Stack de observabilidad avanzado o distribuido.
- Gestión automatizada compleja de incidentes.
- Dashboards operativos sofisticados.

## Criterios de aceptación

- [ ] **CA-1**: Existen señales básicas para disponibilidad, tasa de error y latencia del MVP.
- [ ] **CA-2**: Existen alertas o equivalentes para degradaciones graves del embudo de login definidas en la KB.
- [ ] **CA-3**: El equipo puede usar estas señales para la revisión operativa mínima del MVP.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| App routing | docs/05-infraestructura/observabilidad.md | parcial | Rutas clave para definir alertas de degradacion |
| Dashboard/Diario | docs/05-infraestructura/observabilidad.md | falta | No hay alertas implementadas en prototipo |

## Notas y decisiones

- Esta historia debe mantenerse ligera y proporcional al tamaño del equipo actual.
