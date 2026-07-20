---
bloque: 99-glosario
documento: glosario
actualizado_en: "2026-07-18"
---

# Glosario — Lenguaje Ubicuo del Dominio

> Este glosario define los términos del dominio de negocio que deben usarse de forma
> consistente en el código, la documentación y las conversaciones del equipo.
>
> Basado en el principio de **Ubiquitous Language** de Domain-Driven Design (DDD).
>
> Los términos específicos de un módulo están en `../03-modulos/{modulo}/modelo-dominio.md`.

---

## Convenciones

- Los términos se escriben en **inglés en el código** y en **español en las conversaciones y documentación**
- Si hay ambigüedad, siempre prevalece la definición de este glosario
- Solicitar la actualización de este glosario cuando se introduzca un nuevo concepto de dominio

---

## Términos del dominio

### Workspace

Unidad organizativa principal del producto. Todo dato operativo pertenece a un Workspace.

---

### Terreno

Unidad base de registro operativo. Actividades, cosechas y consumos se asocian a un terreno.

---

### Temporada

Periodo temporal de trabajo y análisis usado como eje de filtrado y agregación de KPIs.

---

### Trabajador

Entidad de responsable operativo en actividades. Puede estar vinculado o no a cuenta de usuario.

---

### Cosecha

Registro de producción por fecha, terreno y temporada, con `kgs` obligatorio y campos netos opcionales excluyentes.

---

### Destino desconocido

Categoría canónica `desconocido` usada cuando no se informa destino comercial final.

---

### Épica

Conjunto de historias de usuario que comparten un objetivo de negocio común.
En la KB, cada épica tiene su propia carpeta en `docs/09-desarrollos/epicas/`.

**Referencia de tickets**: bloque `tickets.*` en el frontmatter.

---

### Historia de usuario / Feature

Unidad de trabajo documentable y desarrollable en un sprint.
Siempre pertenece a una épica y tiene su `spec.md` (qué) y `tech-design.md` (cómo).

---

### ADR (Architecture Decision Record)

Registro de una decisión arquitectural significativa: contexto, decisión tomada
y consecuencias. Los ADRs son inmutables una vez aceptados — se superseden, no se borran.

---

### Bounded Context / Módulo

Límite explícito dentro del cual un modelo de dominio es coherente y consistente.
En este proyecto, cada módulo bajo `docs/03-modulos/` representa un Bounded Context.

---

### DoR (Definition of Ready)

Criterios que debe cumplir un ticket para poder ser llevado a desarrollo.
Ver `../08-procesos/definition-of-ready.md`.

### DoD (Definition of Done)

Criterios que debe cumplir un ticket para considerarse completado.
Ver `../08-procesos/definition-of-done.md`.

---

## Términos a evitar (y sus alternativas)

| Término a evitar | Usar en su lugar | Motivo |
|-----------------|-----------------|--------|
| "finca" (cuando el dato es técnico) | "terreno" | Mantener consistencia con entidades de dominio |
| "equipo" (cuando se refiere al acceso) | "workspace" | Evitar ambigüedad entre organización y personas |
| "campaña" (si no está definida) | "temporada" | Unificar eje temporal en toda la KB |
| "ticket" | "historia" o "épica" | Dentro del contexto de la KB para evitar confusión con tickets de soporte |
