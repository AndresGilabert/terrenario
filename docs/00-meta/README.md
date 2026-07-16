# Base de Conocimiento — Guía de navegación para Agentes de IA

> Este archivo es el **punto de entrada** de la KB. Los agentes deben leerlo antes de navegar.
> Última actualización: ver `changelog.md`

---

## Estructura de bloques

| Bloque | Propósito | Estándar de referencia |
|--------|-----------|----------------------|
| `01-producto/` | Visión de negocio, stakeholders, roadmap, KPIs | Product Wiki |
| `02-arquitectura/` | Diseño técnico del sistema, ADRs | C4 Model + MADR |
| `03-modulos/` | Bounded Contexts y dominios funcionales | DDD |
| `04-ingenieria/` | Estándares del equipo técnico | Engineering Handbook |
| `05-infraestructura/` | Entornos, CI/CD, observabilidad, runbooks | SRE Book |
| `06-integraciones/` | Sistemas y APIs externos | API-First |
| `07-seguridad/` | Seguridad, privacidad, cumplimiento | OWASP |
| `08-procesos/` | DoR/DoD, gestión de incidentes, releases | Agile/Scrum |
| `09-desarrollos/` | Épicas, historias y diseños técnicos activos | RFC + Spec-Driven |
| `10-releases/` | Changelog por versión semántica | Keep a Changelog |
| `99-glosario/` | Lenguaje ubicuo del dominio | DDD Ubiquitous Language |

---

## Módulos del sistema

> Ver detalle completo en [`../03-modulos/_vision-general.md`](../03-modulos/_vision-general.md)
> Actualiza esta tabla cuando añadas un módulo nuevo.
> `modulo-ejemplo` es un ejemplo estructural de la plantilla y debe eliminarse cuando se cree el primer modulo real.

| Módulo | Descripción | Owner | Ruta |
|--------|-------------|-------|------|
| `modulo-ejemplo` | Ejemplo de estructura documental, no modulo real del proyecto | plantilla | [03-modulos/modulo-ejemplo/](../03-modulos/modulo-ejemplo/README.md) |
| _(añadir módulos del proyecto aquí)_ | | | |

---

## Épicas activas

> Actualizado manualmente. Ver detalle completo en [`../09-desarrollos/epicas/`](../09-desarrollos/epicas/)
> El script `validar_kb.py --generar-indices` regenera los `_indice.md` de cada épica.
> Esta plantilla no incluye épicas reales del proyecto. Crea la primera desde `../09-desarrollos/_plantilla/`.

| ID | Épica | Estado | Hito | Índice |
|----|-------|--------|------|--------|
| _(añadir épicas del proyecto aquí)_ | | | | |

---

## Decisiones técnicas recientes

> Ver listado completo en [`../02-arquitectura/decisiones/`](../02-arquitectura/decisiones/)
> Si todavía no existe un ADR real, crear uno desde [`../00-meta/plantillas/adr.md`](./plantillas/adr.md)
> o desde [`../02-arquitectura/decisiones/_plantilla-adr.md`](../02-arquitectura/decisiones/_plantilla-adr.md).

| ADR | Título | Estado |
|-----|--------|--------|
| _(añadir ADRs reales del proyecto aquí)_ | | |

---

## Cómo navegar esta KB como agente de IA

1. **Pregunta de negocio** → empieza por [`01-producto/`](../01-producto/vision-y-objetivos.md)
2. **Pregunta de arquitectura o decisión técnica** → [`02-arquitectura/`](../02-arquitectura/vision-general.md)
3. **Pregunta sobre un dominio funcional** → [`03-modulos/{nombre-modulo}/README.md`](../03-modulos/_vision-general.md)
4. **Pregunta sobre una feature concreta** → busca en [`09-desarrollos/epicas/`](../09-desarrollos/epicas/) por ID de ticket; usa el `_indice.md` de la épica para ver el estado de las historias
5. **Pregunta sobre un término del dominio** → [`99-glosario/glosario.md`](../99-glosario/glosario.md)
6. **Pregunta sobre cómo hacer algo en el equipo** → [`04-ingenieria/`](../04-ingenieria/flujo-git.md) u [`08-procesos/`](../08-procesos/definition-of-ready.md)
7. **Pregunta sobre seguridad o PII** → [`07-seguridad/modelo-seguridad.md`](../07-seguridad/modelo-seguridad.md) — **leer antes de generar código que maneje datos de usuario**

---

## Herramientas de mantenimiento

### Estado y upgrade de plantilla

- Estado actual adoptado por un proyecto consumidor: [`template-state.md`](./template-state.md)
- Guía general de actualización: [`upgrade-template.md`](./upgrade-template.md)
- Migraciones entre versiones: [`migraciones/`](./migraciones/)

### Script de validación

Ubicado en [`scripts/validar_kb.py`](./scripts/validar_kb.py). Requiere Python 3.9+ y `pyyaml`.

```bash
# Validar estructura y frontmatter (falla con código 1 si hay errores)
python docs/00-meta/scripts/validar_kb.py --validar

# Regenerar todos los _indice.md de épicas
python docs/00-meta/scripts/validar_kb.py --generar-indices

# Ambas operaciones a la vez
python docs/00-meta/scripts/validar_kb.py --validar --generar-indices
```

Ver documentación completa del script en [`scripts/README.md`](./scripts/README.md).
