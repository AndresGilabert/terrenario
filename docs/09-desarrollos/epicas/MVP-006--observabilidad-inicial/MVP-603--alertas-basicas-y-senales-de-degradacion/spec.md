---
id: "MVP-603"
tipo: feature
titulo: "Alertas básicas y señales de degradación"
estado: completado
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
actualizado_en: "2026-08-06"
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

- [x] **CA-1**: Existen señales básicas para disponibilidad, tasa de error y latencia del MVP.
- [x] **CA-2**: Existen alertas o equivalentes para degradaciones graves del embudo de login definidas en la KB.
- [x] **CA-3**: El equipo puede usar estas señales para la revisión operativa mínima del MVP.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| App routing | docs/05-infraestructura/observabilidad.md | cubierto | Cada peticion a `/api` alimenta los tres SLO; `/api/v1/health` responde 200 sano y **503 con la base de datos parada**, verificado de verdad |
| Dashboard/Diario | docs/05-infraestructura/observabilidad.md | cubierto | La cobertura de widgets (MVP-602) entra en el informe operativo; las cinco alertas de la KB se disparan y se resuelven |

## Notas y decisiones

- Esta historia debe mantenerse ligera y proporcional al tamaño del equipo actual.
- **Punto ciego declarado**: un proceso muerto no se vigila a si mismo. Dentro de la aplicacion,
  `ServiceDown` cubre la degradacion observable (base de datos inalcanzable); la caida total la
  detecta la sonda externa del alojamiento contra `/api/v1/health`, que esta historia configura.
- Por eso el informe expone `healthy_minutes_30d` y **no** `uptime`: publicar como disponibilidad los
  minutos en que el proceso estuvo vivo para contarlos seria siempre un 100 %.
- **Volumen minimo antes de alertar** (20 peticiones, 10 pantallas de acceso): sin el, una madrugada
  con tres peticiones y un 500 dispararia una alerta critica, y una alerta que salta sin motivo se
  acaba ignorando tambien cuando el motivo es real.
- Los umbrales **no son configurables por despliegue**: un SLO que se puede bajar desde un ajuste deja
  de ser un acuerdo.
- La revision semanal esta escrita como runbook en
  `docs/05-infraestructura/runbooks/revision-operativa.md`.
- El detalle tecnico y las alternativas descartadas estan en [tech-design.md](./tech-design.md).
