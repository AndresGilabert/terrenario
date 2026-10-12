---
id: "PRD-101"
tipo: mejora
titulo: "TDD: Evolución diaria del rendimiento con regresiones"
estado: completado
tickets: []
epica: "PRD-100--dashboard-listo-para-la-promocion"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["dashboard", "kpis", "historico"]
  modulo_path: "03-modulos/produccion-y-dashboard"
  componentes: ["dashboard", "evolucion-rendimiento"]
  etiquetas: ["dashboard", "regresion", "bkl-002"]
  nivel_riesgo: medio
creado_en: "2026-10-12"
actualizado_en: "2026-10-12"
---

# TDD: PRD-101 — Evolución diaria del rendimiento con regresiones

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

El cálculo sigue en el backend, dentro de `GetYieldEvolutionAsync` de `DashboardQueryService`. Se
sustituye la agrupación por mes/semana por una serie diaria y la media histórica por rectas de regresión
por mínimos cuadrados. El cliente solo dibuja lo que recibe. Estado verificado el 2026-10-12: hoy se agrupa
con `PeriodKey` y la historia se resume en medias (`BuildHistory`).

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `Application/Dashboard/DashboardQueryService` | modificado | Serie diaria, regresiones y nueva ventana histórica; se retiran `PeriodKey` y `YieldGranularity` |
| `Controllers/DashboardController` | modificado | `yield-evolution` deja de aceptar `granularity` |
| `components/dashboard/VisionGeneralView` | modificado | `YieldEvolutionWidget` pasa de barras a líneas y rectas |
| `services/dashboard.service.ts` | modificado | Tipos de la nueva respuesta |
| Tests de `DashboardQueryService` y del widget | modificado | Los de agrupación mensual/semanal se sustituyen |

## Diseño detallado

### Lógica de negocio

- **Valor diario**: rendimiento ponderado por kilos de las cosechas del día (la media ponderada existente,
  `WeightedYield`). Un día sin dato de aceite no es un punto.
- **Regresión**: mínimos cuadrados ordinarios, sin pesos, con x = fecha y y = L/100kg. Requiere al menos
  dos fechas distintas; con una no se devuelve recta.
- **Temporada actual**: recta sobre sus días con dato, limitada entre su primera y última fecha.
- **Histórico**: cada cosecha anterior se traslada al año actual sumándole los años necesarios para que
  caiga en la ventana, con el mecanismo actual de `YearsBack`. Cada fecha trasladada de cada año es un punto.
  La recta general usa todos los años; la de 5 años, los situados a 5 años o menos; la de 10, a 10 o menos.
  Las de 5 y 10 solo se devuelven si el histórico alcanza esa profundidad (RN-015).
- **Extensión de las rectas históricas**: de la fecha más temprana a la más tardía trasladadas.

### Ventana del histórico

La ventana de MVP-404 era el rango de las cosechas actuales ±7 días, y con ella no hay histórico para las
semanas aún sin cosechar, que es justo lo que el PO quiere ver. La ventana pasa a ser el calendario de la
temporada activa (`start_date`..`end_date`, o un año desde su inicio si no tiene fin), que ya era el criterio
sin cosechas. Se mantienen el filtro de terrenos y el descarte de días de otra época del año. Es una
decisión de diseño tomada en la implementación, no del PO: si el calendario de una temporada no cubre bien
su periodo real de cosecha, el histórico dejará fuera días comparables, y se corrige ajustando las fechas de
la temporada.

### API / Contratos

`GET /api/v1/dashboard/yield-evolution` (`season_id?`, `plot_ids?[]`). Propuesta de respuesta:

```yaml
scope: {...}
data:                    # un punto por día con dato
  - { date: "2026-11-20", yield_l_per_100kg: 19.4, kg: 3200 }
trend:                   # null si hay menos de 2 fechas distintas
  from: { date, value }
  to:   { date, value }
history:
  general:        { from: {date, value}, to: {date, value} } | null
  last_5_years:   idem | null
  last_10_years:  idem | null
  prior_years_with_data: 2
```

Cada recta se entrega como dos extremos; el cliente no calcula regresiones. Es un cambio incompatible en
`/api/v1`. Verificado el 2026-10-12 que el único consumidor en el repositorio es `dashboard.service.ts`, que
se despliega junto al backend, así que se modifica el contrato en el sitio en vez de versionarlo, según
[ADR-0006](../../../02-arquitectura/decisiones/ADR-0006--contratos-rest-v1-y-reglas-cosecha-mvp.md).

### Manejo de errores

Sin cambios: ámbito no resoluble o sin terrenos devuelve serie vacía y sin rectas, y un fallo del widget
se muestra de forma aislada (`MVP-706`).

## Alternativas descartadas

| Alternativa | Por qué se descartó |
| ----------- | ------------------- |
| Ponderar por kilos en la regresión | El rendimiento graso no depende de los kilos del día (decisión del PO) |
| Mantener mes/semana como opción | Conserva el comportamiento que se quiere sustituir |
| Calcular la regresión en el cliente | Duplica reglas de RN-015 fuera del servidor |
| Prolongar la recta actual al futuro | Sugiere una predicción con pocos datos; la referencia futura la dan las históricas |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
| ------ | ------------ | ---------- |
| La ventana por calendario de temporada incluye fechas no comparables | media | Validar con datos reales y mantener el descarte por época |
| Regresión sobre pocos puntos engañosa | media | Mínimo de dos fechas; leyenda con los años con dato |
| Rectas distorsionadas por el cambio de año en temporadas que lo cruzan | media | Operar con fechas completas, como hoy, y probar el cruce |
| Cambio incompatible de contrato | baja | Consumidores verificados y `contratos-api.md` actualizado en la misma entrega |

## Plan de testing

> Estrategia en `docs/04-ingenieria/estrategia-testing.md`.

- [x] Unitarios: valor diario ponderado, mínimos cuadrados con valores conocidos, una fecha sin recta,
  profundidad de 5 y 10 años, día sin dato de aceite, histórico solo sin cosechas, filtro de terreno, cruce de año
  (`DashboardQueryServiceTests`).
- [x] Integración: no aplica, el endpoint no tiene prueba de integración previa y la forma de la respuesta
  la fijan los tipos del cliente y del controlador.
- [x] Cliente: puntos y rectas, caso de un solo punto, solo histórico y posición de los puntos
  (`YieldEvolutionWidget.test.tsx`).
- [x] Verificación visual por el PO el 2026-10-12.

## Checklist de implementación

- [x] Diseño técnico revisado y aprobado, con la decisión de la ventana tomada
- [x] Tests escritos y pasando
- [x] Documentación actualizada: RN-015, `contratos-api.md`, MVP-404 (nota de sustitución) y la ficha del módulo
- [x] Sin `TODO` sin resolver en este documento
