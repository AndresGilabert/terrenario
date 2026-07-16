---
bloque: 04-ingenieria
documento: guia-code-review
actualizado_en: ""
---

# Guía de Code Review

---

## Propósito del code review

- Detectar bugs antes de que lleguen a producción
- Compartir conocimiento del codebase entre el equipo
- Asegurar que el código cumple los estándares del proyecto
- No es un examen personal — es una colaboración

---

## Responsabilidades

### Autor del PR

- El PR debe ser autoexplicativo (descripción + link al ticket)
- Código funcional y tests pasando antes de pedir revisión
- `spec.md` y `tech-design.md` actualizados
- PRs pequeños y enfocados (máx. ~400 líneas de cambio como referencia)

### Revisor

- Revisar en un máximo de **1 día hábil** desde la solicitud
- Leer el `spec.md` y `tech-design.md` antes de revisar el código
- Distinguir entre bloqueante y sugerencia

---

## Tipos de comentarios

| Prefijo | Tipo | Requiere cambio |
|---------|------|----------------|
| `[bloqueante]` | El PR no puede mergearse sin resolver esto | Sí |
| `[sugerencia]` | Mejora opcional, el autor decide | No |
| `[pregunta]` | Duda o petición de aclaración | Depende de la respuesta |
| `[nit]` | Detalle menor de estilo | No |

---

## Checklist del revisor

### Lógica y corrección

- [ ] ¿El código hace lo que el `spec.md` describe?
- [ ] ¿Los casos de error están cubiertos?
- [ ] ¿Los tests cubren los criterios de aceptación?

### Seguridad

- [ ] ¿Hay validación de entradas en los límites del sistema?
- [ ] ¿No hay secrets o credenciales hardcodeadas?
- [ ] ¿Se aplican las guías de `../07-seguridad/modelo-seguridad.md`?

### Mantenibilidad

- [ ] ¿El código es legible sin necesidad de comentarios?
- [ ] ¿Los nombres son descriptivos?
- [ ] ¿No hay duplicación evidente?

### Documentación

- [ ] ¿El módulo afectado en `../03-modulos/` está actualizado?
- [ ] ¿Si hay una nueva decisión técnica relevante, se ha creado un ADR?
