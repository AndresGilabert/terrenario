---
bloque: 01-producto
documento: kpis
actualizado_en: "2026-06-30"
---

# KPIs y Métricas del Producto

> Los KPIs se revisan en la reunión de revisión de sprint / mensualmente.

---

## KPIs de negocio

| KPI | Descripción | Valor actual | Objetivo | Frecuencia de revisión |
|-----|-------------|-------------|---------|----------------------|
| Terrenos con informacion completa de temporada | % de terrenos activos con datos minimos para calculo de KPIs globales (produccion + arboles) | Pendiente de baseline | >= 90% en temporada | Mensual |
| Registros de cosecha con destino informado | % de registros de cosecha con destino distinto de "Sin destino" | Pendiente de baseline | >= 80% en temporada | Mensual |
| Tiempo de consulta operativa | Tiempo estimado para revisar el estado de temporada en dashboard | Pendiente de baseline | <= 2 minutos | Mensual |

## KPIs de producto

| KPI | Descripción | Valor actual | Objetivo | Frecuencia de revisión |
|-----|-------------|-------------|---------|----------------------|
| Uso del dashboard en sesiones activas | % de sesiones donde el usuario accede al dashboard al menos una vez | Pendiente de baseline | >= 85% | Semanal |
| Uso de recarga manual | Numero promedio de recargas manuales por sesion con dashboard abierto | Pendiente de baseline | Referencia operativa (sin umbral inicial) | Semanal |
| Cobertura de widgets MVP | % de widgets MVP con datos mostrables sin error bloqueante | Pendiente de baseline | 100% (con estados vacio/incompleto cuando aplique) | Semanal |
| Conversion login (pantalla -> exito) | % de sesiones con `login_screen_viewed` que terminan en `login_google_success` | Pendiente de baseline | >= 85% | Semanal |
| Tasa de abandono en login | % de sesiones con pantalla de login que no completan autenticacion | Pendiente de baseline | <= 15% | Semanal |
| Tiempo medio de login exitoso | Tiempo desde `login_screen_viewed` hasta `login_google_success` | Pendiente de baseline | <= 45s | Semanal |

## KPIs técnicos / de calidad

| KPI | Descripción | Valor actual | Objetivo | Umbral de alerta |
|-----|-------------|-------------|---------|-----------------|
| Disponibilidad (uptime) | % uptime del sistema | — | 99.9% | < 99.5% |
| Tiempo de respuesta P95 | Latencia P95 de la API principal | — | < 300ms | > 500ms |
| Tasa de errores | % de requests con error 5xx | — | < 0.1% | > 1% |
| Cobertura de tests | % de cobertura de código | — | > 80% | < 70% |

## Dashboard y fuentes de datos

| Métrica | Fuente | Dashboard |
|---------|--------|-----------|
| Produccion total (kg) | Registros de cosecha por temporada | Resumen de temporada |
| Litros de aceite total | Registros de molienda / conversion de rendimiento | Resumen de temporada |
| Rendimiento promedio (L/100kg) | Registros de rendimiento por cosecha | Resumen + Evolucion |
| Kg por arbol | Produccion por terreno + numero de arboles por terreno | Resumen de temporada |
| Kg por destino | Registros de cosecha con categoria destino | Grafico de destinos |
| Kg por terreno | Agregacion de cosecha por terreno | Grafico de barras |
| Historico de rendimiento | Serie temporal por temporada | Evolucion de rendimiento |
| Conversion login | Eventos de embudo de autenticacion | Dashboard de autenticacion |
| Abandono login | Eventos de embudo de autenticacion | Dashboard de autenticacion |

## Alertas activas

> Ver configuración completa en `../05-infraestructura/observabilidad.md`

| Alerta | Condición | Canal de notificación |
|--------|-----------|----------------------|
| Dato incompleto para kg/arbol | Existen terrenos activos sin numero de arboles en temporada | Banner informativo en widget de resumen |
| Sin historico previo | No hay temporadas anteriores para comparativa | Mensaje contextual en widget de evolucion |
