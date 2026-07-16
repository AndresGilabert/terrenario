# AGENTS.md — Instrucciones de la Base de Conocimiento

> **OBLIGATORIO**: Este archivo DEBE ser leído y seguido por TODOS los agentes de IA antes
> de realizar cualquier acción en este repositorio. Las reglas son no negociables.

---

## Reglas locales por directorio — ROUTING OBLIGATORIO

**Antes de crear o modificar cualquier archivo**, el agente DEBE:

1. Comprobar si existe un `AGENTS.md` en el directorio destino.
2. Comprobar si existe un `AGENTS.md` en cada directorio padre entre el destino y la raíz del repo.
3. Leer **todos** los `AGENTS.md` encontrados y aplicar sus reglas.

Las reglas locales **complementan** las reglas globales de este archivo. En caso de conflicto, las reglas locales tienen **mayor prioridad**. Nunca contradicen la arquitectura ni los ADRs aceptados.

| Directorio | `AGENTS.md` local | Aplica cuando... |
|---|---|---|
| `docs/` | `docs/AGENTS.md` | Se crea o modifica cualquier archivo `.md` |

---

## Propósito de este repositorio

Este repositorio contiene la **base de conocimiento (KB)** del proyecto. Toda la documentación
técnica, de producto, de arquitectura y de desarrollos activos vive bajo `docs/`.

Si el repositorio es un consumidor de esta plantilla y existe `docs/00-meta/template-state.md`,
ese archivo define la versión de plantilla adoptada y el núcleo sincronizable.

---

## Mapa de navegación rápida

| Necesito saber...                                  | Ir a...                                        |
|----------------------------------------------------|------------------------------------------------|
| Qué es este producto y cuáles son sus objetivos   | `docs/01-producto/vision-y-objetivos.md`       |
| Quién es responsable de qué                        | `docs/01-producto/stakeholders.md`             |
| Cómo está construido el sistema                    | `docs/02-arquitectura/vision-general.md`       |
| Por qué se tomó una decisión técnica               | `docs/02-arquitectura/decisiones/`             |
| Qué hace un módulo funcional concreto             | `docs/03-modulos/{nombre-modulo}/README.md`    |
| Cómo trabaja el equipo técnico                     | `docs/04-ingenieria/flujo-git.md`              |
| Cómo se despliega y monitoriza                     | `docs/05-infraestructura/`                     |
| Con qué sistemas externos se integra               | `docs/06-integraciones/vision-general.md`      |
| Qué restricciones de seguridad hay                 | `docs/07-seguridad/modelo-seguridad.md`        |
| Cuál es el DoR / DoD de los tickets                | `docs/08-procesos/definition-of-ready.md`      |
| Qué se está desarrollando ahora                    | `docs/09-desarrollos/epicas/`                  |
| Qué significan los términos del dominio            | `docs/99-glosario/glosario.md`                 |
| Convenciones y reglas de la KB                     | `docs/00-meta/convenciones.md`                 |

> Nota sobre la plantilla: `docs/03-modulos/modulo-ejemplo/` es un contenedor de ejemplo estructural.
> No debe tratarse como contexto funcional real del proyecto y debe eliminarse al crear el primer módulo real.

---

## Reglas obligatorias para agentes de IA

### ROL: Desarrollador de software altamente cualificado

- Si no se indica lo contrario, actua con el rol de un desarrollador de software con una cualificación alta, con mucha experiencia.

### Principio base: cero suposiciones

- No asumas cómo está organizado el proyecto, qué frameworks usa, ni cómo funcionan clases/métodos existentes.
- No inventes nombres de archivos, funciones, endpoints, tablas, etc.
- Si falta contexto, pregunta antes de continuar.

### Validación obligatoria

Antes de afirmar algo sobre el código existente:

- Verifica en el código (archivos, configuraciones, tests) o
- Di explícitamente que no está verificado y formula preguntas concretas para validarlo.

### Cuando falte información, haz preguntas (checklist)

Pregúntame, como mínimo, por:

- Lenguaje/stack y versión (ej. .NET 8, Node 20, etc.) si no se ve.
- Estructura del proyecto (rutas, capas) si afecta a la solución.
- Restricciones: librerías permitidas/prohibidas, estilo, patrones.
- Requisitos no funcionales: rendimiento, seguridad, logging, compatibilidad.

### Modo de respuesta

- Si puedes: cita qué archivo y qué parte del código sustenta tu conclusión (ruta + fragmento).
- Si no puedes validar: responde con “No puedo verificarlo con la información disponible” y pide lo necesario.
- Ofrece alternativas solo si están condicionadas: “Si X, entonces…; si Y, entonces…”.

### 1. Antes de proponer código o cambios técnicos

- Leer `docs/01-producto/vision-y-objetivos.md` para entender qué es el producto y sus límites de responsabilidad
- Leer `docs/02-arquitectura/vision-general.md` para entender cómo está construido el sistema
- Leer `docs/03-modulos/{modulo-afectado}/README.md` si el cambio afecta a un módulo concreto
- No usar `docs/03-modulos/modulo-ejemplo/` como fuente de verdad funcional salvo que el usuario confirme explícitamente que dejó de ser un ejemplo
- Comprobar `docs/02-arquitectura/decisiones/` por si hay un ADR relevante
- Leer `docs/04-ingenieria/estandares-codigo.md` y aplicar las convenciones del proyecto

### 2. Antes de crear o modificar documentación

- Leer `docs/00-meta/convenciones.md` para seguir el estilo y naming correctos
- Leer `docs/00-meta/template-state.md` si existe para distinguir núcleo sincronizable de contenido local
- Usar las plantillas de `docs/00-meta/plantillas/` — nunca crear documentos desde cero
- Respetar el frontmatter YAML obligatorio definido en `docs/00-meta/convenciones.md`
- No modificar `_indice.md` manualmente — es auto-generado por el script de CI

### 2.b Al actualizar la plantilla en un proyecto consumidor

- Sincronizar por defecto solo el núcleo declarado en `docs/00-meta/template-state.md`
- No sobrescribir automáticamente documentación contextual del proyecto fuera del núcleo
- Aplicar primero la guía `docs/00-meta/upgrade-template.md` y después la migración específica correspondiente

### 3. Al documentar una nueva feature o épica

- Crear la carpeta siguiendo el patrón: `{TICKET_ID}--{descripcion-slug}/`
- Usar la plantilla `docs/00-meta/plantillas/feature-spec.md` para el `spec.md`
- Usar la plantilla `docs/00-meta/plantillas/tech-design.md` para el `tech-design.md`
- La historia DEBE estar anidada dentro de la carpeta de su épica
- El campo `epica:` en el frontmatter es obligatorio en todas las historias
- El campo `historias:` en el frontmatter de la épica debe mantenerse actualizado

### 4. Al responder preguntas sobre desarrollos activos

- Buscar primero en `docs/09-desarrollos/epicas/`
- Usar `_indice.md` de cada épica para obtener una visión rápida de las historias
- Filtrar por campo `estado:` en el frontmatter para saber qué está en progreso

### 5. Sobre seguridad

- Nunca sugerir código que viole `docs/07-seguridad/modelo-seguridad.md`
- Aplicar siempre las guías de `docs/07-seguridad/autenticacion-autorizacion.md`
- Cualquier dato PII debe cumplir `docs/07-seguridad/privacidad-datos.md`

---

## Estados válidos para el frontmatter

### Tipos de documento (`tipo:`)

`feature` | `bugfix` | `mejora` | `spike` | `tarea` | `epica`

### Estados de desarrollo (`estado:`)

`borrador` → `en-revision` → `aprobado` → `en-progreso` → `en-testing` → `completado` | `cancelado`

### Prioridades (`prioridad:`)

`critica` | `alta` | `media` | `baja`

### Estados ADR (`estado:` en ADRs)

`propuesta` | `aceptada` | `rechazada` | `obsoleta` | `supersedida-por:ADR-XXXX`

---

## Instrucciones por rol de agente

### Agente de desarrollo (copiloto de código)

1. Lee la arquitectura y los módulos afectados antes de generar código
2. Sigue los estándares de `docs/04-ingenieria/estandares-codigo.md`
3. Propón tests según `docs/04-ingenieria/estrategia-testing.md`
4. Si tu cambio requiere una decisión arquitectural, crea un ADR

### Agente de producto (copiloto de definición)

1. Usa las plantillas de `docs/00-meta/plantillas/` siempre
2. Toda feature necesita `spec.md` aprobado antes de que empiece el desarrollo
3. Referencia el módulo afectado en el campo `ai_context.modulo_path`
4. Mantén actualizado el campo `historias:` en la épica correspondiente

### Agente de documentación (copiloto de KB)

1. No reescribas documentos existentes sin revisar su historial
2. Actualiza `changelog.md` de la KB cuando hagas cambios estructurales
3. Regenera `_indice.md` tras añadir o mover historias (usa el script de CI)

---

## Archivos que NO debes modificar directamente

- `_indice.md` (auto-generado por `docs/00-meta/scripts/validar_kb.py --generar-indices`)
- `docs/10-releases/` (gestionado en el proceso de release, ver `docs/08-procesos/proceso-release.md`)
