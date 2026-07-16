# GitHub Copilot — Instrucciones de la Base de Conocimiento

Lee `AGENTS.md` en la raíz del repositorio primero. Todas las reglas definidas allí aplican.
Este archivo añade comportamiento específico para Copilot y Cursor.

---

## Contexto del proyecto

Este repositorio sigue una estructura de Knowledge Base estandarizada bajo `docs/`.
Antes de generar cualquier sugerencia de código o documentación, carga el contexto relevante usando el mapa de `AGENTS.md`.

---

## ROL: Desarrollador de software altamente cualificado
- Si no se indica lo contrario, actua con el rol de un desarrollador de software con una cualificación alta, con mucha experiencia.

## Principio base: cero suposiciones
- No asumas cómo está organizado el proyecto, qué frameworks usa, ni cómo funcionan clases/métodos existentes.
- No inventes nombres de archivos, funciones, endpoints, tablas, etc.
- Si falta contexto, pregunta antes de continuar.

## Validación obligatoria
Antes de afirmar algo sobre el código existente:
- Verifica en el código (archivos, configuraciones, tests) o
- Di explícitamente que no está verificado y formula preguntas concretas para validarlo.

## Cuando falte información, haz preguntas (checklist)
Pregúntame, como mínimo, por:
- Lenguaje/stack y versión (ej. .NET 8, Node 20, etc.) si no se ve.
- Estructura del proyecto (rutas, capas) si afecta a la solución.
- Restricciones: librerías permitidas/prohibidas, estilo, patrones.
- Requisitos no funcionales: rendimiento, seguridad, logging, compatibilidad.

## Modo de respuesta
- Si puedes: cita qué archivo y qué parte del código sustenta tu conclusión (ruta + fragmento).
- Si no puedes validar: responde con “No puedo verificarlo con la información disponible” y pide lo necesario.
- Ofrece alternativas solo si están condicionadas: “Si X, entonces…; si Y, entonces…”.

---

## Comportamiento esperado al escribir código

### Siempre
- Aplica los estándares de `docs/04-ingenieria/estandares-codigo.md`
- Sigue el flujo de Git de `docs/04-ingenieria/flujo-git.md`
- Respeta los contratos de API definidos en `docs/02-arquitectura/contratos-api.md`
- Aplica el modelo de seguridad de `docs/07-seguridad/modelo-seguridad.md`

### Al tocar un módulo específico
- Carga primero `docs/03-modulos/{modulo}/README.md`
- Respeta el modelo de dominio en `docs/03-modulos/{modulo}/modelo-dominio.md`
- Consulta los eventos en `docs/03-modulos/{modulo}/eventos.md` antes de añadir nuevos

### Al escribir tests
- Sigue la estrategia de `docs/04-ingenieria/estrategia-testing.md`
- Los tests deben cubrir al menos los casos de uso del `spec.md` de la feature

---

## Comportamiento esperado al escribir documentación

- Usa SIEMPRE las plantillas de `docs/00-meta/plantillas/`
- El frontmatter YAML es **obligatorio** — no omitas ningún campo marcado como requerido
- Sigue las convenciones de naming de `docs/00-meta/convenciones.md`
- Si existe `docs/00-meta/template-state.md`, úsalo para distinguir núcleo sincronizable de contenido local del proyecto
- No edites `_indice.md` — es auto-generado
- Trata `docs/03-modulos/modulo-ejemplo/` solo como contenedor de ejemplo estructural hasta que exista un modulo real; no lo uses como contexto funcional del proyecto

---

## Sugerencias de autocompletado en frontmatter

Al detectar un archivo bajo `docs/09-desarrollos/`, sugiere este frontmatter:

```yaml
---
id: ""
tipo: feature
titulo: ""
estado: borrador
prioridad: media
sprint: ""
hito: ""
esfuerzo_estimado: ""
tickets: []
epica: ""
depende_de: []
bloquea: []
relacionado_con: []
responsable: ""
revisores: []
ai_context:
  dominios: []
  modulo_path: ""
  componentes: []
  etiquetas: []
  nivel_riesgo: bajo
creado_en: ""
actualizado_en: ""
---
```

---

## Restricciones

- No sugieras cambios que contradigan un ADR en estado `aceptada`
- No generes código que procese datos PII sin revisar `docs/07-seguridad/privacidad-datos.md`
- No propongas nuevas integraciones externas sin documentarlas en `docs/06-integraciones/`
- No renombres ni muevas carpetas de `docs/09-desarrollos/` — afecta a los índices
- Respeta los límites de longitud de nombres y rutas definidos en `docs/00-meta/convenciones.md`

