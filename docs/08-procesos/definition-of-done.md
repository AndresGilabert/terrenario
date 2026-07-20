---
bloque: 08-procesos
documento: definition-of-done
actualizado_en: "2026-07-13"
---

# Definition of Done (DoD)

> Un ticket está **completado** cuando cumple TODOS estos criterios.
> Ningún ticket puede moverse a `completado` sin cumplir el DoD.

---

## Criterios obligatorios

### Código

- [ ] El código implementa todos los criterios de aceptación del `spec.md`
- [ ] El código sigue los estándares de `../04-ingenieria/estandares-codigo.md`
- [ ] El linting pasa sin errores
- [ ] No hay `TODO` ni código comentado pendiente de resolver

### Tests

- [ ] Los tests unitarios del dominio afectado están escritos y pasan
- [ ] Los tests de integración de los flujos afectados pasan
- [ ] La cobertura no ha bajado respecto al estado anterior del módulo
- [ ] Los nuevos bugs tienen test de regresión

### Revisión

- [ ] El PR ha sido revisado por el autor o por un revisor disponible
- [ ] Todos los comentarios `[bloqueante]` resueltos
- [ ] El CI pasa completamente (linting + tests + validación KB)

### Documentación

- [ ] El `tech-design.md` está completo y sin `TODO`
- [ ] El estado del `spec.md` está actualizado a `completado`
- [ ] Si hay cambios en la API del módulo, `../03-modulos/{modulo}/api-referencia.md` está actualizado
- [ ] Si hay cambios en el modelo de datos, `../03-modulos/{modulo}/modelo-datos.md` está actualizado
- [ ] Si hay nuevos eventos, `../03-modulos/{modulo}/eventos.md` está actualizado
- [ ] Si hay una nueva decisión técnica relevante, se ha creado un ADR
- [ ] Si hay datos personales, existe evidencia de cumplimiento RGPD + LOPDGDD en la documentacion del ticket
- [ ] Si aplica LSSI-CE/ePrivacy, queda documentado y validado
- [ ] Si aplica EIPD, esta completada o aprobada antes de paso a produccion

### Deploy

- [ ] El cambio está desplegado en staging y ha pasado las pruebas de QA
- [ ] No hay degradación de los SLOs definidos en `../05-infraestructura/observabilidad.md`

---

## ¿Quién valida el DoD?

El **desarrollador** es responsable de cumplir el DoD antes de marcar el ticket como completado.
El **revisor del PR** valida los criterios técnicos.
El **QA** valida los criterios de aceptación en staging.
