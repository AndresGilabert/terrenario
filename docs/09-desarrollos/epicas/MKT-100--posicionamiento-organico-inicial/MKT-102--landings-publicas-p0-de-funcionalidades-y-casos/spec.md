---
id: "MKT-102"
tipo: feature
titulo: "Landings publicas P0 de funcionalidades y casos"
estado: borrador
prioridad: alta
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "3d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: []
bloquea: ["MKT-103", "MKT-104", "MKT-105", "MKT-106", "MKT-108"]
relacionado_con: ["MKT-100"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["marketing", "seo", "conversion"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["landing-publica", "rutas-publicas"]
  etiquetas: ["landing", "public-pages", "organic"]
  nivel_riesgo: medio
creado_en: "2026-08-31"
actualizado_en: "2026-08-31"
---

# MKT-102 — Landings publicas P0 de funcionalidades y casos

## Contexto

Sin contenido público orientado a búsqueda, no hay base para indexación ni aterrizaje de usuarios fríos.

## Objetivo

Publicar el conjunto inicial de landings definidas para funcionalidades y casos de uso.

## Alcance (in-scope)

- Crear las URLs públicas del plan P0:
  - `/funcionalidades/gestion-terrenos`
  - `/funcionalidades/diario-de-campo`
  - `/funcionalidades/control-cosechas`
  - `/funcionalidades/compras-y-consumos`
  - `/funcionalidades/dashboard-campana`
  - `/funcionalidades/workspaces-colaboracion`
  - `/funcionalidades/trabajadores-y-tareas`
  - `/para/agricultor-particular`
  - `/para/explotacion-familiar`
  - `/para/gestion-multiterreno`
- Home pública como hub de enlazado.
- CTA principal en cada landing a `/login`.

## Fuera de alcance (out-of-scope)

- Blog o centro editorial completo.
- Multilenguaje.

## Criterios de aceptación

- [ ] **CA-1**: Las 10 landings públicas responden en producción con contenido útil.
- [ ] **CA-2**: Todas las landings enlazan a `/login` y a landings relacionadas.
- [ ] **CA-3**: La home enlaza al menos a los clústeres principales de funcionalidades.
