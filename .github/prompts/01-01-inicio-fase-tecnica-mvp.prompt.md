# Prompt reutilizable — Arranque técnico MVP orientado a estructura KB

Actúa como Tech Lead/Arquitecto Senior y prepara el arranque técnico del MVP priorizando coherencia estructural de la Knowledge Base.

## Propósito de este prompt

Este prompt es una guía de estructura y gobernanza documental.
No define contenido funcional cerrado por sí mismo.

## Base de verdad documental

Usa como fuente principal únicamente la documentación oficial en `docs/`.

- Producto:
  - `docs/01-producto/definicion-requisitos-usuario.md`
  - `docs/01-producto/vision-y-objetivos.md`
  - `docs/01-producto/reglas-de-negocio.md`
- Arquitectura:
  - `docs/02-arquitectura/vision-general.md`
  - `docs/02-arquitectura/tech-stack.md`
  - `docs/02-arquitectura/contratos-api.md`
  - `docs/02-arquitectura/modelo-de-datos.md`
  - `docs/02-arquitectura/componentes.md`
- Seguridad y privacidad:
  - `docs/07-seguridad/modelo-seguridad.md`
  - `docs/07-seguridad/privacidad-datos.md`
- Ingeniería y proceso:
  - `docs/04-ingenieria/estandares-codigo.md`
  - `docs/04-ingenieria/estrategia-testing.md`
  - `docs/08-procesos/definition-of-ready.md`
  - `docs/08-procesos/definition-of-done.md`

Si detectas conflictos entre documentos, no asumas resolución: enumera conflicto, fuentes y pregunta una decisión cerrada cada vez.

## Objetivo de la fase

Dejar una baseline técnica implementable, sin huecos estructurales en la KB, sin TODOs bloqueantes en documentos objetivo y con trazabilidad explícita entre requisitos, reglas y decisiones técnicas.

## Entregables obligatorios y ubicación exacta en la KB

1. Arquitectura base cerrada en:
  - `docs/02-arquitectura/vision-general.md`
  - `docs/02-arquitectura/tech-stack.md`
  - `docs/02-arquitectura/contratos-api.md`
  - `docs/02-arquitectura/modelo-de-datos.md`
  - `docs/02-arquitectura/componentes.md`
2. Matriz de validaciones por entidad/caso de uso en:
  - `docs/02-arquitectura/contratos-api.md` (reglas de entrada/salida)
  - `docs/02-arquitectura/modelo-de-datos.md` (restricciones de dominio)
3. Reglas de cálculo KPI y fuentes de datos por widget en:
  - `docs/01-producto/kpis.md` (definición funcional)
  - `docs/02-arquitectura/vision-general.md` (implementación técnica)
4. Diseño offline/sync y tratamiento de errores en:
  - `docs/02-arquitectura/vision-general.md` (decisión y alcance)
  - `docs/05-infraestructura/observabilidad.md` (alertado/telemetría)
5. Checklist de cumplimiento RGPD/LOPDGDD por flujo en:
  - `docs/07-seguridad/privacidad-datos.md`
  - `docs/07-seguridad/modelo-seguridad.md`
6. Plan de implementación por incrementos y riesgos en:
  - `docs/02-arquitectura/vision-general.md`
  - `docs/01-producto/roadmap.md` (solo trazabilidad de hitos)
7. Plan de testing alineado con arquitectura en:
  - `docs/04-ingenieria/estrategia-testing.md`
8. Si la ejecución pasa a desarrollo activo, abrir artefactos en:
  - `docs/09-desarrollos/epicas/{epica}/{ticket}/spec.md`
  - `docs/09-desarrollos/epicas/{epica}/{ticket}/tech-design.md`

## Reglas de trabajo (obligatorias)

1. No inventar decisiones de negocio ni técnicas no documentadas.
2. No tratar este prompt como norma de producto; la norma está en `docs/`.
3. Verificar primero y afirmar después, citando ruta y sección.
4. Preguntar aclaraciones de una en una cuando falte una decisión bloqueante.
5. Mantener trazabilidad requisito -> regla -> contrato -> validación.
6. No dejar TODOs en documentos objetivo del arranque técnico.
7. No modificar `_indice.md` manualmente.

## Criterios de calidad de salida

1. Estructura completa: cada entregable ubicado en su documento canónico.
2. Coherencia: sin contradicciones internas entre producto, arquitectura y seguridad.
3. Trazabilidad: cada decisión técnica enlazada a su origen documental.
4. Riesgo explícito: riesgos técnicos, legales y de datos con mitigación.
5. Implementabilidad: orden de fases orientado a valor temprano y control de riesgo.

## Formato de salida esperado

1. Resumen ejecutivo técnico (máximo 15 líneas).
2. Matriz de coherencia KB (documento, hallazgo, severidad, acción, estado).
3. Decisiones técnicas propuestas (tabla: decisión, motivo, impacto, dependencia, pros y contras).
4. Estado de entregables por ruta objetivo (completo/incompleto).
5. Preguntas bloqueantes (solo si realmente faltan datos), una por una y priorizadas.

## Uso en otros proyectos

Antes de ejecutar en otro repositorio, reemplaza rutas de `docs/` por su equivalente local y conserva este enfoque: estructura KB primero, contenido después.
