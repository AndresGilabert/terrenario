# Changelog — Base de Conocimiento

Todos los cambios estructurales relevantes en la KB se registran aquí.
Formato basado en [Keep a Changelog](https://keepachangelog.com/es/).

---

## [Sin publicar]

### Añadido

- Estructura inicial de la KB
- Plantillas para documentar integraciones externas (`integracion-especificacion.md` e `integracion-gestion-errores.md`)
- Estado de plantilla (`template-state.md`) y guía de actualización controlada (`upgrade-template.md`)
- Migración documentada de plantilla `v1.0.0 -> v1.1.0`
- Script `sync_template_core.py` para planificar y aplicar sincronización del núcleo de plantilla
- Plantilla específica para notas de release (`release-notes.md`)
- Estructura real de `docs/09-desarrollos/epicas/` con seis épicas MVP alineadas con el roadmap aprobado
- Historias iniciales de la épica `MVP-001` para dejar definido el Hito A del MVP
- Historias iniciales de la épica `MVP-002` para dejar definido el Hito B del MVP
- Historias iniciales de la épica `MVP-003` para dejar definido el Hito C del MVP
- Historias iniciales de la épica `MVP-004` para dejar definido el Hito D del MVP
- Historias iniciales de las épicas `MVP-005` y `MVP-006` para cerrar la salida controlada y la operación medible del MVP

### Cambiado

- Actualizada documentacion de producto para reflejar definicion funcional confirmada del dashboard MVP (alcance, reglas, journeys y KPIs iniciales).
- Actualizada la KB para cerrar decisiones funcionales del MVP y convertir el roadmap en una propuesta por bloques ejecutables con dependencias explícitas.
- Definido marco de cumplimiento obligatorio de proteccion de datos (RGPD + LOPDGDD), con clasificacion de obligatorio vs condicionado vs recomendado y criterios de enforcement en DoR/DoD.
- Definida estrategia de autenticacion para MVP (Google Login), Passkeys en fase futura y trazabilidad obligatoria del embudo de login para detectar abandono.
- Marcado `docs/03-modulos/modulo-ejemplo/` como módulo de ejemplo no vinculante y excluido del contexto funcional real del proyecto.
- Eliminado un runbook de ejemplo de servicio concreto, dejando como referencia la plantilla oficial de runbooks.
- Eliminada una integracion de ejemplo de proveedor concreto, sustituyendola por plantillas para integraciones reales.
- Eliminada `docs/09-desarrollos/epicas/PROJ-100--checkout-refactor/` por ser una épica de ejemplo, manteniendo `docs/09-desarrollos/_plantilla/` como base para desarrollos reales.
- Definido un modelo de actualización por versión + núcleo sincronizable para proyectos consumidores de la plantilla.
- Actualizado el flujo Git para usar `develop` como rama de integración previa a `main` y hacer opcional la aprobación externa en proyectos unipersonales.

---

## [1.0.0] — 2026-06-09

### Añadido

- Estructura completa de la KB con 12 bloques numerados
- Sistema de frontmatter YAML con soporte multi-plataforma de tickets
- Plantillas para feature spec, tech design, ADR, bug report, runbook y módulo
- Sistema de 3 capas de enforcement: agentes IA, humanos y CI/CD
- `AGENTS.md` con instrucciones universales para agentes de IA
- `CONTRIBUTING.md` con guía de contribución para desarrolladores
- `docs/00-meta/guia-pm.md` con guía para Product Managers
- Script de validación `validar_kb.py` con modos `--validar` y `--generar-indices`
- GitHub Actions workflow para validación automática en PRs
- Pre-commit hooks para validación local
- Módulo de ejemplo: `03-modulos/modulo-ejemplo/`
- Ejemplo de épica con historia anidada en `09-desarrollos/`
