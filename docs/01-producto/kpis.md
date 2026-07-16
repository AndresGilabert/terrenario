---
bloque: 01-producto
documento: kpis
actualizado_en: "2026-07-16"
---

# KPIs y Métricas del Producto

> Plantilla de ejemplo: este archivo es una guia base y DEBE reemplazarse con KPIs reales del proyecto.
> Sustituye nombres, definiciones, objetivos, umbrales y frecuencias con datos del dominio real.
> Los KPIs se revisan en la reunion de revision de sprint / mensualmente.

## Instrucciones de uso (plantilla)

1. Reemplaza todas las filas de ejemplo por KPIs reales del proyecto.
2. Define baseline inicial y fuente de datos verificable para cada KPI.
3. Elimina o adapta secciones que no apliquen al producto.
4. Evita dejar terminos genericos como "flujo principal" sin especificar.

---

## KPIs de negocio

> Ejemplos de referencia. Reemplazar por KPIs de negocio reales.

| KPI | Descripción | Valor actual | Objetivo | Frecuencia de revisión |
|-----|-------------|-------------|---------|----------------------|
| Registros completos | % de registros con campos minimos obligatorios completos | Pendiente de baseline | >= 90% | Mensual |
| Trazabilidad operativa | % de operaciones con responsable y timestamp | Pendiente de baseline | >= 95% | Mensual |
| Tiempo de consulta operativa | Tiempo para revisar el estado operativo en dashboard | Pendiente de baseline | <= 2 minutos | Mensual |

## KPIs de producto

> Ejemplos de referencia. Reemplazar por KPIs de adopcion y uso reales.

| KPI | Descripción | Valor actual | Objetivo | Frecuencia de revisión |
|-----|-------------|-------------|---------|----------------------|
| Uso del dashboard en sesiones activas | % de sesiones donde el usuario accede al dashboard al menos una vez | Pendiente de baseline | >= 85% | Semanal |
| Uso de recarga manual | Numero promedio de recargas manuales por sesion con dashboard abierto | Pendiente de baseline | Referencia operativa (sin umbral inicial) | Semanal |
| Cobertura de widgets MVP | % de widgets MVP con datos mostrables sin error bloqueante | Pendiente de baseline | 100% (con estados vacio/incompleto cuando aplique) | Semanal |
| Conversion login (pantalla -> exito) | % de sesiones con `login_screen_viewed` que terminan en `login_google_success` | Pendiente de baseline | >= 85% | Semanal |
| Tasa de abandono en login | % de sesiones con pantalla de login que no completan autenticacion | Pendiente de baseline | <= 15% | Semanal |
| Tiempo medio de login exitoso | Tiempo desde `login_screen_viewed` hasta `login_google_success` | Pendiente de baseline | <= 45s | Semanal |

## KPIs técnicos / de calidad

> Ejemplos de referencia. Ajustar objetivos y umbrales a los SLO/SLA reales del proyecto.

| KPI | Descripción | Valor actual | Objetivo | Umbral de alerta |
|-----|-------------|-------------|---------|-----------------|
| Disponibilidad (uptime) | % uptime del sistema | — | 99.9% | < 99.5% |
| Tiempo de respuesta P95 | Latencia P95 de la API principal | — | < 300ms | > 500ms |
| Tasa de errores | % de requests con error 5xx | — | < 0.1% | > 1% |
| Cobertura de tests | % de cobertura de código | — | > 80% | < 70% |

## Dashboard y fuentes de datos

> Ejemplos de referencia. Deben alinearse con los eventos y tablas reales de tu sistema.

| Métrica | Fuente | Dashboard |
|---------|--------|-----------|
| Volumen total | Registros del flujo principal por periodo | Resumen operativo |
| Tasa de exito | Operaciones completadas / operaciones iniciadas | Resumen operativo |
| Tiempo medio por flujo | Eventos de inicio/fin por proceso | Evolucion temporal |
| Distribucion por categoria | Registros categorizados | Grafico de categorias |
| Distribucion por entidad | Agregacion por entidad principal | Grafico de barras |
| Historico del indicador principal | Serie temporal por periodo | Evolucion temporal |
| Conversion login | Eventos de embudo de autenticacion | Dashboard de autenticacion |
| Abandono login | Eventos de embudo de autenticacion | Dashboard de autenticacion |

## Alertas activas

> Ver configuración completa en `../05-infraestructura/observabilidad.md`
> Ejemplos de referencia. Sustituir por alertas reales operadas por el equipo.

| Alerta | Condición | Canal de notificación |
|--------|-----------|----------------------|
| Dato incompleto para KPI principal | Existen registros activos sin datos base requeridos | Banner informativo en widget de resumen |
| Sin historico previo | No hay periodos anteriores para comparativa | Mensaje contextual en widget de evolucion |
