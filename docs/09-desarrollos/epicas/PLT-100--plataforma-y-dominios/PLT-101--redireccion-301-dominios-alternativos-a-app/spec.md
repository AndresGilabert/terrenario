---
id: "PLT-101"
tipo: bugfix
titulo: "Redireccion 301 de dominios alternativos a app.terrenario.com"
estado: aprobado
prioridad: critica
sprint: ""
hito: "Post-MVP — Plataforma"
esfuerzo_estimado: "0.5d"
tickets: []
epica: "PLT-100--plataforma-y-dominios"
depende_de: []
bloquea: []
relacionado_con: ["MKT-100"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["plataforma", "infraestructura", "dominios"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["dominios", "app-service", "dns"]
  etiquetas: ["post-mvp", "plataforma", "seo", "accesibilidad"]
  nivel_riesgo: medio
creado_en: "2026-09-02"
actualizado_en: "2026-09-02"
---

# PLT-101 — Redirección 301 de dominios alternativos a app.terrenario.com

## Contexto

El producto se sirve únicamente en `https://app.terrenario.com`. La organización tiene comprados y
bajo su control `terrenario.com`, `www.terrenario.com`, `terrenario.es` y `www.terrenario.es`, pero
ninguno está enlazado al App Service ni tiene contenido: quien teclea cualquiera de esos cuatro
dominios no encuentra nada, lo que es un problema real de accesibilidad al sitio.

## Objetivo

Que los cuatro dominios alternativos enlacen al mismo App Service y redirijan con `301` a
`https://app.terrenario.com`, conservando la ruta y la query string de la petición original.

## Requisitos de usuario

### HU-1 — Llegar al producto real desde cualquier dominio comprado

**Como** visitante que teclea `terrenario.com`, `www.terrenario.com`, `terrenario.es` o
`www.terrenario.es`,
**quiero** llegar a la aplicación real conservando la página que pedía,
**para** no encontrarme con un dominio que no responde.

## Alcance (in-scope)

- Enlace de los cuatro dominios (`terrenario.com`, `www.terrenario.com`, `terrenario.es`,
  `www.terrenario.es`) al App Service existente, con certificado gestionado.
- Redirección `301` desde esos cuatro dominios a `https://app.terrenario.com`, manteniendo ruta y
  query string.
- Actualización del runbook de publicación en Azure con los registros DNS necesarios.

## Fuera de alcance

- Contenido propio en los dominios de redirección.
- Cambios en el dominio canónico (`app.terrenario.com`) o en el modelo de origen único.
- Redirecciones distintas de 301 (por ejemplo, por ruta o por dominio de destino).

## Criterios de aceptación

- [ ] **CA-1**: Los cuatro dominios están enlazados al App Service con certificado gestionado (HTTPS).
- [ ] **CA-2**: Una petición a cualquiera de los cuatro dominios responde `301` a
  `https://app.terrenario.com` con la misma ruta y query string.
- [ ] **CA-3**: El dominio canónico (`app.terrenario.com`) no cambia de comportamiento.
