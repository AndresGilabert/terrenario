# Guía para Product Managers — Base de Conocimiento

> Eres el propietario de las especificaciones de feature.
> Sin `spec.md` aprobado → el desarrollo no comienza. Eres el guardián de la calidad de definición.

---

## Tu responsabilidad en la KB

Como PM, eres responsable de:

1. **Crear y mantener** el `spec.md` de cada épica e historia antes de que empiece el desarrollo
2. **Validar** que el frontmatter esté completo y los tickets referenciados sean correctos
3. **Actualizar el estado** de las historias conforme avanzan
4. **Mantener actualizado** el campo `historias:` en la épica cuando se añaden o eliminan historias

---

## Ciclo de vida de una feature: tu parte

```text
1. DESCUBRIMIENTO
   └── Identificas la necesidad (ticket en Jira / Zendesk / etc.)

2. ESPECIFICACIÓN  ← Tu trabajo principal
   ├── Creas la carpeta: docs/09-desarrollos/epicas/{EPICA}/{TICKET_ID}--{slug}/
   ├── Copias la plantilla oficial desde docs/09-desarrollos/_plantilla/
   ├── Rellenas spec.md con: contexto, objetivo, criterios de aceptación, out-of-scope
   └── Estado inicial: borrador

3. REVISIÓN
   ├── Compartes con el equipo técnico para validar viabilidad
   ├── Ajustas según el feedback
   └── Estado: en-revision → aprobado

4. DESARROLLO  ← El equipo técnico toma el relevo
   └── Estado: en-progreso (el dev crea el tech-design.md)

5. TESTING
   └── Estado: en-testing

6. COMPLETADO
   └── Estado: completado
```

---

## Cómo crear la especificación de una historia

### Paso 1: Localizar la épica

```text
docs/09-desarrollos/epicas/{EPICA_ID}--{epica-slug}/
```

Si la épica no existe, créala primero usando `docs/00-meta/plantillas/feature-spec.md`
usando `docs/09-desarrollos/_plantilla/epica-spec.md`.

### Paso 2: Crear la carpeta de la historia

```text
docs/09-desarrollos/epicas/{EPICA_ID}--{epica-slug}/{TICKET_ID}--{titulo-slug}/
```

### Paso 3: Copiar la plantilla

```text
docs/09-desarrollos/_plantilla/feature-spec.md → spec.md
```

### Paso 4: Rellenar el frontmatter

Campos que debes rellenar tú:

| Campo | Tu responsabilidad |
|-------|--------------------|
| `id` | ID del ticket principal |
| `titulo` | Título claro y descriptivo |
| `estado` | Empieza siempre en `borrador` |
| `prioridad` | Decide tú según el negocio |
| `sprint` / `hito` | Según planificación |
| `tickets` | Añade referencias externas si existen |
| `epica` | ID--slug de la épica padre |
| `responsable` | Tu @usuario |
| `ai_context.dominios` | Qué dominios de negocio afecta |

### Paso 5: Actualizar la épica

Añade el ID de la nueva historia al campo `historias:` del `spec.md` de la épica.

---

## Secciones obligatorias de un spec.md

```markdown
## Contexto
Por qué existe esta feature. Qué problema resuelve. Para quién.

## Objetivo
Qué debe conseguir esta feature al finalizar. Medible si es posible.

## Alcance (in-scope)
Qué está dentro de esta historia. Sé específico.

## Fuera de alcance (out-of-scope)
Qué NO está en esta historia (evita ambigüedades).

## Criterios de aceptación
- [ ] CA-1: Descripción del criterio verificable
- [ ] CA-2: ...

## Maquetas / Referencias visuales
(opcional) Links a Figma, capturas, ejemplos de referencia.

## Notas y decisiones
Decisiones tomadas durante la definición, contexto adicional.
```

---

## Definition of Ready (DoR)

Un ticket está **listo para desarrollo** cuando su `spec.md` cumple:

- [ ] Frontmatter completo y válido
- [ ] Estado: `aprobado`
- [ ] Secciones de contexto, objetivo y alcance rellenadas
- [ ] Al menos 3 criterios de aceptación verificables
- [ ] Módulo(s) afectado(s) identificado(s) en `ai_context.modulo_path`
- [ ] Revisado con el equipo técnico (sin `TODO` sin resolver)

Ver criterios completos en `../08-procesos/definition-of-ready.md`.

---

## Errores comunes

| Error | Solución |
|-------|----------|
| Crear el spec después de que haya empezado el desarrollo | El DoR es previo. Habla con el equipo. |
| Dejar `estado: borrador` cuando ya está aprobado | Actualiza el estado para desbloquear el agente de IA |
| Si existe ticket externo y no queda referenciado | Añade la referencia en `tickets` y documenta la trazabilidad de la fuente |
| Cambiar el ID de la carpeta a mitad del desarrollo | No cambies nunca el ID — afecta a todos los índices |
| Poner criterios de aceptación ambiguos ("que funcione bien") | Los CA deben ser verificables y concretos |
