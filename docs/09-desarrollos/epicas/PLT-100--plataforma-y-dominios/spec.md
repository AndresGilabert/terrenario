---
id: "PLT-100"
tipo: epica
titulo: "Plataforma y dominios"
estado: en-progreso
prioridad: alta
hito: "Post-MVP — Plataforma"
tickets: []
historias: ["PLT-101", "PLT-199"]
depende_de: []
bloquea: []
relacionado_con: ["MVP-002", "MKT-100"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["plataforma", "infraestructura", "dominios"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["dominios", "app-service", "dns"]
  etiquetas: ["post-mvp", "plataforma", "infraestructura"]
  nivel_riesgo: medio
creado_en: "2026-09-02"
actualizado_en: "2026-09-02"
---

# EPICA PLT-100 — Plataforma y dominios

## Contexto

El producto se sirve desde un único origen (`https://app.terrenario.com`), decisión tomada al
publicar el MVP (`publicacion-inicial-en-azure.md`) para evitar problemas de cookie cross-site. La
organización tiene comprados, además, los dominios `terrenario.com`, `terrenario.es` y sus variantes
`www`, sin que ninguno tenga contenido propio ni esté enlazado al App Service: quien llega por ellos
no encuentra nada.

Esta épica agrupa el trabajo de plataforma que no encaja en ningún módulo funcional: dominios,
despliegue y configuración del App Service que sirve el producto entero.

## Objetivo

Que cualquier dominio comprado por la organización que apunte al producto lleve a quien accede a la
aplicación real, sin roturas de accesibilidad ni contenido duplicado de cara a buscadores.

## Requisitos de alto nivel

- Como visitante que teclea `terrenario.com` o `terrenario.es` (con o sin `www`), quiero llegar al
  producto real, para no encontrarme con un dominio que no responde.
- Como responsable de SEO, quiero que buscadores y navegadores aprendan que el dominio canónico es
  `app.terrenario.com`, para no repartir autoridad de enlace entre dominios duplicados.

## Alcance

- Enlace de dominios adicionales al App Service existente y su redirección al dominio canónico.

## Fuera de alcance

- Cualquier contenido propio en los dominios de redirección: no lo tienen ni lo van a tener.
- Cambios en el dominio canónico (`app.terrenario.com`) o en la decisión de origen único.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias en estado `completado`.
- [ ] **CA-2**: Los cuatro dominios de redirección resuelven y llevan al producto real.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

- [PLT-101](./PLT-101--redireccion-301-dominios-alternativos-a-app/spec.md) — Redirección 301 de
  dominios alternativos a `app.terrenario.com`.
- [PLT-199](./PLT-199--revision-epica/spec.md) — Revisión de la épica (en `borrador`: se aprueba y
  ejecuta cuando la épica esté lista para cerrarse).

## Notas y decisiones

- La épica arranca con una sola historia urgente (`PLT-101`); `PLT-199` queda en `borrador` y su
  `depende_de` se amplía con cada historia nueva que entre en `PLT-100`.
