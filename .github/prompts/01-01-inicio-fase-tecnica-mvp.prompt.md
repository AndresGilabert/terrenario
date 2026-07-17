# Prompt reutilizable — Inicio de fase técnica MVP

Actúa como Tech Lead/Arquitecto Senior y prepara el arranque técnico de Terrenario para MVP.

## Contexto del repositorio (base de verdad documental)
- Producto: docs/01-producto/definicion-requisitos-usuario.md
- Visión: docs/01-producto/vision-y-objetivos.md
- Reglas de negocio: docs/01-producto/reglas-de-negocio.md
- Seguridad: docs/07-seguridad/modelo-seguridad.md
- Privacidad: docs/07-seguridad/privacidad-datos.md
- Arquitectura (a completar):
  - docs/02-arquitectura/vision-general.md
  - docs/02-arquitectura/tech-stack.md
  - docs/02-arquitectura/contratos-api.md

## Decisiones de negocio ya cerradas para MVP (no reabrir)
1. Todos los costes son manuales (sin cálculo automático por tarifas ni otros parámetros).
2. En cosecha, Kgs es obligatorio.
3. Rendimiento y litros son alternativos excluyentes.
4. Si se informa rendimiento, no se informan litros.
5. Si se informan litros, no se informa rendimiento.
6. Maestro formal de trabajadores obligatorio.
7. Balance fuera de MVP.
8. Coste de molturación fuera de MVP.
9. Sin filtro de propietario en dashboard (el contexto lo da el Workspace activo).
10. Sin migración desde Excel en MVP.
11. Catálogo de destinos MVP cerrado:
    - Venta de aceituna
    - Aceite para venta
    - Aceite personal
    - Desconocido
12. Política de baja:
    - Anonimización inmediata de datos operativos.
    - Conservación separada de evidencias mínimas legales durante 24 meses.

## Objetivo de esta fase
Convertir esta definición funcional en baseline técnico implementable, sin contradicciones, con trazabilidad completa de reglas.

## Entregables obligatorios
1. Arquitectura base cerrada (sin TODOs) en:
   - docs/02-arquitectura/vision-general.md
   - docs/02-arquitectura/tech-stack.md
   - docs/02-arquitectura/contratos-api.md
2. Propuesta de modelo de datos MVP (entidades, relaciones, claves, estados).
3. Matriz de validaciones por entidad/caso de uso (obligatorio/opcional/reglas de negocio).
4. Contratos API iniciales para flujos MVP (alta/edición/listado de terrenos, temporadas, trabajadores, actividades, cosechas, compras, dashboard).
5. Reglas de cálculo de KPIs y fuentes de datos por widget.
6. Diseño técnico de offline/sincronización y cola de errores para MVP.
7. Checklist de cumplimiento RGPD/LOPDGDD aplicado a los flujos MVP.
8. Plan de implementación por incrementos (fases técnicas), con riesgos y mitigaciones.
9. Plan de testing (unitario, integración, e2e, datos incompletos, conflictos offline).

## Criterios de calidad de la respuesta
1. No asumir: verificar en documentación antes de afirmar.
2. Señalar explícitamente cualquier contradicción residual detectada.
3. Si falta una decisión para implementar, formular preguntas cerradas y priorizadas.
4. Mantener trazabilidad: cada decisión técnica debe enlazar con regla/requisito origen.
5. Incluir riesgos técnicos, legales y de datos.
6. Proponer orden de implementación orientado a entregar valor temprano.

## Formato de salida esperado
1. Resumen ejecutivo técnico (máximo 15 líneas).
2. Decisiones técnicas propuestas (tabla: decisión, motivo, impacto, dependencia).
3. Modelo de datos MVP.
4. Contratos API MVP.
5. Diseño offline/sync.
6. Seguridad y privacidad aplicada.
7. Plan de implementación por sprints técnicos.
8. Preguntas bloqueantes (solo si realmente faltan datos).

## Uso en otros proyectos
Antes de ejecutar, reemplaza rutas y decisiones cerradas por las equivalentes del proyecto destino.
