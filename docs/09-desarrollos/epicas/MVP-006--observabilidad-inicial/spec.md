---
id: "MVP-006"
tipo: epica
titulo: "Observabilidad inicial"
estado: completado
prioridad: media
hito: "Hito F — Operación medible"
tickets: []
historias: ["MVP-601", "MVP-602", "MVP-603", "MVP-699"]
depende_de: ["MVP-001", "MVP-003", "MVP-004", "MVP-005"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["observabilidad", "kpis-producto", "operacion"]
  modulo_path: "03-modulos/"
  componentes: ["telemetria-login", "uso-dashboard", "alertas-basicas"]
  etiquetas: ["mvp", "observability", "telemetry"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-08-06"
---

# EPICA MVP-006 — Observabilidad inicial

## Contexto

La KB exige medir el embudo de login y los primeros indicadores de uso del dashboard para saber si el MVP realmente reduce fricción y aporta valor. En un equipo pequeño, esta observabilidad debe ser intencional y acotada, no una plataforma de monitoring sobredimensionada.

Esta épica cierra la capacidad de operar y aprender del MVP una vez el producto ya es desplegable.

## Objetivo

Disponer de la telemetría mínima necesaria para detectar abandono en login, uso del dashboard y degradaciones operativas iniciales en los primeros Workspaces activos.

## Requisitos de usuario de alto nivel

- **Como** responsable del producto, **quiero** medir si los usuarios entran y usan el dashboard, **para** validar que el MVP está resolviendo la fricción principal.
- **Como** responsable técnico, **quiero** alertas y señales básicas de degradación, **para** reaccionar rápido sin una operación compleja.

## Alcance

- Telemetría del embudo de login hasta éxito o abandono.
- Métricas de uso del dashboard y recarga manual.
- Alertas básicas de salud operativa alineadas con los KPIs técnicos definidos en KB.
- Señales mínimas para detectar widgets sin datos mostrables o degradaciones funcionales relevantes.

## Fuera de alcance

- Data warehouse o analítica avanzada de producto.
- Observabilidad distribuida compleja o explotación full de trazas.
- Experimentación A/B o analítica de comportamiento avanzada.

## Criterios de aceptación de la épica

- [x] **CA-1**: Todas las historias de la épica están en estado `completado`.
- [x] **CA-2**: El equipo puede medir de forma trazable el embudo de login y el uso del dashboard sin exponer PII sensible en claro.
- [x] **CA-3**: Existen alertas básicas o señales equivalentes para detectar degradaciones iniciales del MVP en operación.

> Veredicto sustentado en la pasada de verificación de `MVP-699`. **CA-3 se dio por cumplido solo tras
> corregir `R-03`**: las cinco alertas existían y disparaban, pero la sonda de salud ocupaba el 87 % del
> divisor del SLO y `HighErrorRate` no podía saltar con tráfico realista.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- `MVP-601` — Telemetría mínima del embudo de login.
- `MVP-602` — Métricas de uso del dashboard y recarga manual.
- `MVP-603` — Alertas básicas y señales de degradación inicial.
- `MVP-699` — Revision epica.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia para todas las historias de esta epica:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo solo aporta referencia de eventos UX observables.
- Si hay contradiccion, prevalece la KB.

Referencia base del prototipo:

- [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)
- [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx)
- [prototype/reports/mvp-prototype-coverage.md](../../../../prototype/reports/mvp-prototype-coverage.md)

Matriz historia -> utilidad del prototipo:

| Historia | Referencias de prototipo | Cobertura |
|---|---|---|
| MVP-601 | [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx) | Referencia para instrumentar eventos de login en UI |
| MVP-602 | [prototype/terrenario-mvp/src/components/DashboardView.tsx](../../../../prototype/terrenario-mvp/src/components/DashboardView.tsx) | Referencia para eventos de filtros/uso de widgets |
| MVP-603 | [prototype/terrenario-mvp/src/App.tsx](../../../../prototype/terrenario-mvp/src/App.tsx) | Referencia de rutas y vistas para definir señales de degradacion |

## Notas y decisiones

- Esta épica no antecede al núcleo MVP; lo acompaña cuando ya existe algo estable que medir.
- Debe mantenerse deliberadamente pequeña para no competir con la entrega funcional principal.

### Decisiones de cierre

- **Qué se conserva**: contadores diarios agregados, no una traza de eventos. Los KPI de la KB salen
  igual y no se persiste ningún identificador, así que la medición no añade categoría de dato a
  `RN-041` ni saca a la medición del supuesto de exención de `RN-042`. La evaluación se rehízo —no se
  dio por hecha— al ampliar la medición más allá del acceso, y se actualizó lo publicado.
- **Sin interfaz, a propósito**: las señales se consultan por HTTP con llave de servicio. Es coherente
  con el «N/A en fase C» de la tabla de dashboards; una pantalla queda diferida (`P-074`) y abriría
  antes la pregunta de quién puede verla, que hoy no tiene respuesta porque no hay roles.
- **Punto ciego declarado**: un proceso muerto no se vigila a sí mismo. La caída total depende de una
  sonda externa que reinicia pero no avisa (`P-077`).
- La épica deja siete puntos derivados en `MVP-999` (`P-073` a `P-079`).
