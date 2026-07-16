---
bloque: 01-producto
documento: vision-y-objetivos
actualizado_en: "2026-07-13"
---

# Visión y Objetivos del Producto

## Visión y Objetivos: Plataforma de Gestión Agrícola Personal (Draft)

### Visión:

Crear una plataforma centralizada y sencilla que sirva como herramienta digital para el registro, seguimiento y análisis integral de pequeñas explotaciones agrícolas personales. Su objetivo es transformar la gestión manual y fragmentada del trabajo agrícola en un proceso estructurado y medible, permitiendo a los propietarios tener visibilidad completa sobre su rendimiento, costes y recursos sin requerir una actividad profesional dedicada exclusivamente al sector.

## Reglas de negocio que definen el producto

Estas reglas impactan directamente cómo se modela el sistema y sus validaciones.

| # | Regla | Definición |
|---|-------|-----------|
| R1 | Unidad base = Terreno | Todo registro (actividad, cosecha, consumo material, coste) DEBE estar asociado a un terreno. Los terrenos pueden ser propios o cedidos, gestionados simultáneamente desde el día 1. |
| R2 | Toda actividad tiene responsable + tiempo | No se permite registrar una actividad sin asignar un responsable y horas ejecutadas. Responsable = campo texto (no cuenta de usuario). |
| R3 | Coste calculado, no manual | Los costes de materiales se calculan automáticamente desde la compra vinculada. El coste mano de obra se calcula: `horas * tarifa_responsable`. Nadie introduce montantes monetarios directamente. |
| R4 | Compra + consumo aproximado | Se registra la compra (producto, cantidad total, coste total). Antonio asigna consumos aproximados por terreno. Coste proporcional si vinculado a compra específico. NO hay stock tracking ni saldo acumulativo. Los excedentes se pierden/se desechan naturalmente. |
| R5 | Cosecha con unidad única por registro | Una cosecha usa UNA sola unidad de medida (kg OR litros OR plantas). Nunca múltiple en un mismo registro. |
| R6 | MVP v1: único usuario | Solo Antonio. Sin multi-usuario granular ni cuentas para Carlos. |

## Corte del alcance MVP v1

De las 8 funcionalidades clave definidas más abajo, esta es la separación confirmada:

### ✅ Incluidas en MVP v1

| # (fue clave) | Funcionalidad | Alcance v1 |
|---|--------------|------------|
| #1 | Gestión de Terrenos | Propios + cedidos. Campos minimos: nombre, tipo_propiedad, ubicación, metadatos_suelo simple (JSONB) |
| #2 | Registro de Actividades y Recursos | Qué se hizo, quién, cuándo, en qué terreno, duración. Responsable = texto (no cuenta). Coste calculado desde tarifa. |
| #3 | Gestión de Producción | Producto, unidad única (kg OR litros OR plantas), terreno fuente. Registros multidimensionales excluidos v1 → solo un campo por registro. |
| #4 | Inventario/Consumo → Compras + consumo aprox | Registrar producto comprado y costo asignado a terrenos. Sin stock tracking ni saldo acumulado. Excedentes naturales (pérdidas, desecho) no regulados. |
| #5 | Dashboard operativo MVP | Dashboard simple con 4 widgets iniciales (resumen de temporada, kg por destino, kg por terreno, evolución de rendimiento) para consulta rápida y comparación histórica básica. |

> **Criterio transversal #9 aplicado a todo el MVP v1:** Simplicidad "Usuario Final". Interfaz tipo diario personal para Antonio. Registro rápido y sin login para Carlos.

### ❌ Excluidas de MVP v1

| # (funcionalidad clave) | Funcionalidad | Destino |
|---|--------------|----------|
| #5 | Inteligencia de Negocio/Analytics Avanzado | v2+: analitica profunda, exploración ad-hoc y comparativas avanzadas |
| #6 | Datos Colaborativos/anónimos | v3+ |
| #7 | Análisis Predictivo Meteo + IA | v2+ |
| #8 | Control acceso Multi-Usuario (roles/perimisos) | fuera de scope hasta tener usuarios reales. Solo Antonio desde dia 1 |

## Problemas que resuelve:

1. **Gestión Fragmentada:** Los agricultores carecen de una única fuente de verdad para gestionar múltiples terrenos con diversas actividades, costes y cosechas.
2. **Trazabilidad Incompleta del Trabajo:** Es difícil registrar de forma sistemática cuándo, quién y cuánto tiempo se dedicó a cada tarea en diferentes terrenos.
3. **Control Financiero/Material:** Existe dificultad para llevar un control consolidado e inmediato de los costes asociados (materiales comprados, mano de obra) y su uso específico por terreno o actividad.
4. **Análisis Desconectado:** La información recolectada es puntual o no se puede correlacionar fácilmente con el rendimiento total, impidiendo la optimización basada en datos.
5. **Falta de conocimiento:** Los agricultores personales carecen de una formación y experiencia profesional para tomar decisiones colegiadas para optimizar las tareas y el rendimiento de las cosechas.

## Funcionalidades Clave:

1. **Gestión de Terrenos:** Capacidad para registrar y administrar múltiples terrenos gestionados por el usuario, distinguiendo entre propiedades propias o cedidas. Incluye registro completo de terrenos (propios o cedidos) con metadatos específicos del cultivo/suelo inicial.

2. **Registro de Actividades y Recursos (Mano de Obra/Costes):** Implementar un módulo que permita registrar trabajos específicos (qué se hizo), asociar fecha y responsable (persona que lo realizó), cuantificar el tiempo dedicado y los costes asociados, y mantener trazabilidad total de productos/materiales comprados (fertilizantes, fitosanitarios), obligando a especificar el terreno exacto de consumo.

3. **Gestión de Producción:** Módulo para registrar sistematizadamente las cosechas, especificando producto recolectado, unidades (KGs, etc.) y terrenos fuente. Cada registro de cosecha mantiene una única unidad principal para evitar ambigüedad operativa.

4. **Inventario y Consumo:** Registro detallado de productos o materiales adquiridos (fertilizantes, fitosanitarios). Es vital la capacidad de rastrear dónde y en qué terreno se ha consumido cada material.

5. **Inteligencia de Negocio (Dashboard):** Un dashboard centralizado que proporcione análisis consolidado de producción general y desagregada por terreno, con capacidad de desglosar métricas a nivel de unidad biológica (por árbol/planta) dentro de un terreno.

6. **Datos Colaborativos:** Capacidad de generar estadísticas agregadas y anónimas a partir de los datos compartidos por la comunidad de usuarios.

7. **Análisis Predictivo:** Un motor estadístico que cruce la producción con las condiciones meteorológicas (integración Meteo API) para sugerir ventanas temporales óptimas para realizar tareas o cosechar, maximizando el rendimiento biológico. No solo reporta el clima actual, sino que modela: "Si la semana próxima habrá X precipitación, debes realizar Y tarea en Z terreno para optimizar C cosechas" (Combinación de Meteo + IA).

8. **Control de Acceso Multi-Usuario:** El sistema debe soportar permisos altamente granulares, aunque la implementación inicial se centrará en tres roles definidos: Administrador, Editor y Lector. Estos roles deben poder aplicarse por Terreno o por grupo de terrenos.

9. **Simplicidad Nivel "Usuario Final":** Mantener la alta tecnología de IA y análisis, pero presentarla con la interfaz de un diario personal altamente potente, priorizando la acción sencilla sobre los gráficos complejos para el usuario non-profesional.

## Objetivos estratégicos

| Objetivo | Métrica de éxito | Horizonte |
|----------|-----------------|-----------|
| TODO | TODO | Q1 2025 |

## KPIs principales

> Ver detalle en `kpis.md`.

| KPI | Valor actual | Objetivo |
|-----|-------------|---------|
| TODO | — | — |

## Roadmap de alto nivel

> Ver detalle en `roadmap.md`.

| Horizonte | Foco |
|-----------|------|
| Corto plazo (0-3 meses) | TODO |
| Medio plazo (3-6 meses) | TODO |
| Largo plazo (6-12 meses) | TODO |

## Decisión de identidad de marca

- Nombre comercial adoptado: **Terrenario**.
- Dominio principal adoptado: **terrenario.com**.
- Fecha de decisión: **2026-07-13**.
- Criterio: nombre de marca claro, con presencia y no limitado a un cultivo específico.
- Impacto: se utilizará como referencia principal en comunicación de producto, web y materiales comerciales.

## Resumen funcional confirmado (pre-roadmap / pre-ADR)

Este producto es una plataforma de gestion agricola personal orientada a pequenas explotaciones de olivar. El MVP prioriza simplicidad y utilidad diaria para un perfil no tecnico, con foco en registrar datos operativos y convertirlos en visibilidad accionable.

### Dashboard MVP como pieza central

- El dashboard es parte del MVP y se consulta en una sola pantalla con scroll vertical.
- No hay actualizacion continua en segundo plano en MVP: los datos se refrescan al abrir o al recargar manualmente.
- Al recargar manualmente, se conservan los filtros activos.
- Filtro por defecto en primer acceso: todos los terrenos + temporada actual.
- En MVP no se muestra marca de "ultima actualizacion" para evitar ruido visual.

### Widgets iniciales confirmados

1. Resumen de temporada
2. Kg por destino de venta/uso
3. Kg por terreno
4. Evolucion de rendimiento de fruto

### Reglas clave de lectura de datos

- Si faltan arboles en algun terreno, el KPI global de kg/arbol excluye esos terrenos y muestra "dato incompleto".
- El grafico de kg por terreno usa barras verticales, sin agrupacion en "Otros", con orden fijo por kg descendente y desempate alfabetico.
- El widget de destinos incluye categoria "Sin destino" como categoria valida.
- La evolucion de rendimiento usa unidad canonica L/100kg y permite entradas equivalentes (L/100kg, kg/100kg o calculo desde kg entregados + litros obtenidos).
- En comparativas historicas se muestra promedio historico disponible; los promedios 5y/10y solo aparecen cuando exista suficiente historico.
