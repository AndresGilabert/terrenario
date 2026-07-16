---
bloque: 01-producto
documento: user-journeys
actualizado_en: "2026-07-16"
---

# User Journeys

> Los journeys describen los flujos de usuario más críticos del producto.
> Cada journey debe mapearse desde la perspectiva del usuario, no del sistema.

---

## Journey 1: Revisar estado de temporada en dashboard

**Persona**: Persona A (usuario principal)
**Objetivo del usuario**: TODO Entender rapidamente el estado operativo y detectar desviaciones
**Duración estimada**: 1-2 minutos

### Pasos

TODO :: Información de ejemplo para la plantilla, al definir los pasos reales del proyecto, eliminar los actuales y esta línea de TODO

| Paso | Acción del usuario | Lo que ve/recibe | Emoción | Puntos de fricción |
|------|--------------------|-----------------|---------|-------------------|
| 1 | Entra al dashboard | Vista unica con scroll vertical y 4 widgets | 😊 | Ninguno |
| 2 | Revisa resumen del periodo | Volumen total, tasa de exito e indicador principal | 😊 | Puede aparecer aviso de dato incompleto |
| 3 | Revisa distribucion por categoria | Distribucion por categorias del dominio | 😐 | Categoria sin clasificar requiere limpieza de dato |
| 4 | Revisa distribucion por entidad | Barras verticales ordenadas por valor descendente | 😊 | Scroll horizontal si hay muchas entidades |
| 5 | Revisa evolucion temporal | Serie del periodo y promedio historico disponible | 😐 | Si no hay historico previo, mensaje contextual |
| 6 | Pulsa recarga manual si lo necesita | Datos actualizados manteniendo filtros activos | 😊 | Dependencia de calidad de conectividad |

### Oportunidades de mejora

TODO :: Información de ejemplo para la plantilla, al definir las oportunidades de mejora reales del proyecto, eliminar los actuales y esta línea de TODO

- Destacar visualmente campos incompletos que impiden KPIs mas precisos.
- Introducir recomendaciones guiadas en base a variaciones del indicador principal (fase posterior).

---


## Flujos de error más comunes

TODO :: Información de ejemplo para la plantilla, al definir los flujos de error reales del proyecto, eliminar los actuales y esta línea de TODO

| Flujo | Qué sale mal | Qué experimenta el usuario | Solución actual |
|-------|-------------|---------------------------|----------------|
| KPI principal incompleto | Faltan datos base en una o mas entidades | Ve aviso de "dato incompleto" y valor parcial | Excluir registros incompletos del calculo global y mostrar aviso |
| Sin historico previo | No hay temporadas anteriores registradas | No ve comparativa con temporada anterior | Mostrar mensaje "sin historico previo" |
| Datos sin clasificar | Parte de registros queda en "Sin clasificar" | Grafico por categoria menos accionable | Mostrar categoria explicita para forzar limpieza de datos |
| Abandono en login | El usuario ve login pero no completa autenticacion | No llega a convertirse en usuario activo | Trazabilidad del embudo y mejoras UX sobre pasos con mayor caida |
