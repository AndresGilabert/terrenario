---
bloque: 01-producto
documento: vision-y-objetivos
actualizado_en: "2026-07-16"
---

# Visión y Objetivos del Producto

## Plantilla de vision y objetivos (draft)

### Visión

Define aqui una vision clara y breve del producto en terminos de valor para el usuario y para el negocio.

## Reglas de negocio que definen el producto

Estas reglas impactan directamente cómo se modela el sistema y sus validaciones.

TODO :: Información de ejemplo para la plantilla, al definir reglas reales del proyecto, eliminar las actuales y esta línea de TODO

| # | Regla | Definición |
|---|-------|-----------|
| R1 | Entidad base obligatoria | Todo registro operativo debe asociarse a una entidad principal del dominio. |
| R2 | Trazabilidad minima | Toda accion relevante debe registrar responsable y marca temporal. |
| R3 | Integridad de datos | No se permiten estados inconsistentes ni datos incompletos en operaciones criticas. |
| R4 | Reglas de calculo centralizadas | Los calculos de negocio se ejecutan en backend y no por entrada manual libre. |
| R5 | Unidad canonica por registro | Cada registro usa una unidad principal para evitar ambiguedades. |
| R6 | Alcance de MVP acotado | El MVP prioriza simplicidad operativa y tiempo de salida al mercado. |

## Corte del alcance MVP v1

De las 8 funcionalidades clave definidas más abajo, esta es la separación confirmada:

### ✅ Incluidas en MVP v1

TODO :: Información de ejemplo para la plantilla, al definir el alcance real del MVP del proyecto, eliminar las actuales y esta línea de TODO

| # (fue clave) | Funcionalidad | Alcance v1 |
|---|--------------|------------|
| #1 | Gestion de entidades principales | Alta, edicion y consulta de la entidad principal del dominio. |
| #2 | Registro operativo | Captura de acciones, eventos o transacciones con trazabilidad minima. |
| #3 | Flujo principal de negocio | Implementacion de punta a punta del caso de uso principal. |
| #4 | Reporte operativo basico | Vista consolidada con indicadores esenciales para operar. |
| #5 | Seguridad base | Autenticacion y autorizacion iniciales del MVP. |

> **Criterio transversal aplicado al MVP v1:** Simplicidad para el usuario final y reduccion de friccion en flujos criticos.

### ❌ Excluidas de MVP v1

| # (funcionalidad clave) | Funcionalidad | Destino |
|---|--------------|----------|
| #6 | Analitica avanzada | v2+ |
| #7 | Automatizaciones inteligentes | v2+ |
| #8 | Colaboracion avanzada | v3+ |
| #9 | Personalizacion profunda | v3+ |

## Problemas que resuelve:

TODO :: Información de ejemplo para la plantilla, al definir los problemas que resuelve reales del proyecto, eliminar las actuales y esta línea de TODO

1. **Informacion dispersa:** Los equipos no tienen una fuente unica de verdad.
2. **Trazabilidad incompleta:** Cuesta reconstruir quien hizo que y cuando.
3. **Baja visibilidad operativa:** Falta un estado consolidado del proceso principal.
4. **Dificultad para priorizar:** Sin datos confiables, se toman decisiones reactivas.
5. **Procesos manuales:** Tareas repetitivas consumen tiempo y generan errores.

## Funcionalidades Clave

### 1. Gestion de entidades principales

- Alta y edicion de la entidad principal del dominio.
- Campos minimos obligatorios y metadatos extensibles.

### 2. Registro operativo y trazabilidad

Implementar un módulo que permita:

- Registrar acciones o transacciones relevantes.
- Asociar responsable y marcas temporales.
- Mantener historial auditable de cambios.
- Aplicar validaciones de calidad de dato.

### 3. Flujo principal de negocio

- Implementar el caso de uso central de extremo a extremo.
- Definir estados claros del ciclo de vida.

### 4. Reporte operativo

Vista resumida con indicadores clave para seguimiento diario.

### 5. Seguridad base

- Autenticacion inicial.
- Modelo basico de autorizacion por rol.

### 6. Datos colaborativos

Capacidad de generar estadísticas agregadas y anónimas a partir de los datos compartidos por la comunidad de usuarios.

### 7. Análisis Predictivo

- Motor de recomendaciones sobre historico operativo y senales externas.
- Sugerencias accionables con trazabilidad de criterio.

### 8. Control de Acceso Multi-Usuario

- El sistema debe soportar permisos por rol.
- Granularidad avanzada se reserva para fases posteriores.

### 9. Simplicidad Nivel "Usuario Final"

Priorizar claridad de interfaz y tareas guiadas sobre complejidad visual.

## Objetivos estratégicos

| Objetivo | Métrica de éxito | Horizonte |
|----------|-----------------|-----------|
| TODO | TODO | TODO |

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

## Resumen funcional confirmado (pre-roadmap / pre-ADR)

Este documento es una base de plantilla para definir el alcance funcional del MVP de cualquier dominio.

### Dashboard MVP como pieza central

- El dashboard es parte del MVP y se consulta en una sola pantalla con scroll vertical.
- No hay actualizacion continua en segundo plano en MVP: los datos se refrescan al abrir o al recargar manualmente.
- Al recargar manualmente, se conservan los filtros activos.
- Filtro por defecto en primer acceso: todos los elementos activos + periodo actual.
- En MVP no se muestra marca de "ultima actualizacion" para evitar ruido visual.

### Widgets iniciales confirmados

1. Resumen de temporada
2. Indicador por categoria
3. Indicador por entidad
4. Evolucion temporal del indicador principal

### Reglas clave de lectura de datos

- Si faltan datos base, el KPI global excluye esos registros y muestra "dato incompleto".
- El grafico por entidad usa barras verticales con orden fijo por valor descendente y desempate alfabetico.
- El widget de destinos incluye categoria "Sin destino" como categoria valida.
- La evolucion del indicador principal usa una unidad canonica definida por el dominio.
- En comparativas historicas se muestra promedio historico disponible; los promedios 5y/10y solo aparecen cuando exista suficiente historico.
