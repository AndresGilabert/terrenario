---
id: "MKT-107"
tipo: tarea
titulo: "Alta en webmaster y baseline de indexacion"
estado: aprobado
prioridad: media
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "1d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-101", "MKT-105", "MKT-106"]
bloquea: ["MKT-110"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seo", "operacion"]
  modulo_path: "03-modulos/observabilidad"
  componentes: ["search-console", "bing-webmaster", "ops-reporting"]
  etiquetas: ["baseline", "indexation", "organic-monitoring"]
  nivel_riesgo: bajo
creado_en: "2026-08-31"
actualizado_en: "2026-09-01"
---

# MKT-107 — Alta en webmaster y baseline de indexacion

## Objetivo

Dejar configurada la lectura base de cobertura, clics e impresiones orgánicas sobre landings publicadas.

## Requisitos de usuario

### HU-1 — Conocer el estado de indexación real

**Como** responsable de crecimiento,
**quiero** verificar el sitio en Search Console y Bing Webmaster y enviar el sitemap,
**para** poder consultar cobertura, clics e impresiones orgánicas reales sin depender de suposiciones.

### HU-2 — Tener un punto de partida para medir progreso

**Como** responsable de crecimiento,
**quiero** un baseline documentado de indexación y tráfico orgánico inicial,
**para** poder comparar la evolución en las siguientes revisiones periódicas.

## Alcance (in-scope)

- Alta y verificación en Search Console y Bing Webmaster.
- Envío de `sitemap.xml`.
- Baseline inicial de indexación y tráfico orgánico.

## Criterios de aceptación

- [ ] **CA-1**: El sitio queda verificado en ambos paneles.
- [ ] **CA-2**: El sitemap queda enviado y aceptado.
- [ ] **CA-3**: Existe baseline documentado para revisión semanal.
