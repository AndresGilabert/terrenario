---
bloque: 08-procesos
documento: definition-of-ready
actualizado_en: "2026-06-30"
---

# Definition of Ready (DoR)

> Un ticket está **listo para desarrollo** cuando cumple TODOS estos criterios.
> Si alguno no se cumple, el ticket vuelve al PM para completar la definición.

---

## Criterios obligatorios

### Documentación

- [ ] Existe `spec.md` en `docs/09-desarrollos/epicas/{epica}/{ticket}/`
- [ ] El nombre de carpeta de épica/historia sigue la derivación automática de slug desde el título del desarrollo o ticket fuente
- [ ] Los nombres y rutas cumplen los límites de longitud definidos en `docs/00-meta/convenciones.md`
- [ ] El `spec.md` tiene estado `aprobado` en el frontmatter
- [ ] El frontmatter YAML está completo y válido
- [ ] El módulo afectado está identificado en `ai_context.modulo_path`
- [ ] Si existe ticket externo, el `spec.md` incluye trazabilidad completa (sistema, ID o URL, fecha, campos importados, pendientes)
- [ ] Los campos no recuperables de la fuente externa están marcados como pendientes, sin contenido inventado

### Especificación

- [ ] El contexto y el problema a resolver están claros
- [ ] El objetivo es concreto y medible
- [ ] El alcance (in-scope y out-of-scope) está definido
- [ ] Hay al menos 3 criterios de aceptación verificables
- [ ] No hay `TODO` sin resolver en el `spec.md`

### Dependencias

- [ ] Las dependencias con otros tickets están identificadas en `depende_de`
- [ ] Si depende de trabajo aún no completado, está marcado y acordado con el equipo

### Cumplimiento normativo (obligatorio)

- [ ] Se ha evaluado impacto de proteccion de datos contra `../07-seguridad/privacidad-datos.md`
- [ ] Se confirma cumplimiento de RGPD (UE 2016/679) y LOPDGDD (LO 3/2018)
- [ ] Si aplica, se documenta cumplimiento de LSSI-CE/ePrivacy
- [ ] Si aplica alto riesgo, se planifica EIPD antes de implementar
- [ ] No hay conflicto entre requisitos de negocio y obligaciones legales (si hay conflicto, el ticket no esta Ready)

### Tamaño

- [ ] El ticket es realizable en un sprint (si es más grande, dividir en historias)
- [ ] El esfuerzo estimado está relleno en el frontmatter

### Diseño (si aplica)

- [ ] Si hay maquetas o flujos visuales, están referenciados en el spec
- [ ] Si hay decisiones técnicas previas relevantes, el desarrollador las conoce

---

## ¿Quién valida el DoR?

El **PM** es responsable de que el ticket cumpla el DoR antes de presentarlo en la planning.
El **equipo técnico** puede rechazar un ticket en la planning si el DoR no se cumple.
