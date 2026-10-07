---
id: "MKT-108"
tipo: tarea
titulo: "Activos organicos y calendario editorial 30 dias"
estado: en-progreso
prioridad: media
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "2d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-102", "MKT-103"]
bloquea: ["MKT-109"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["marketing", "contenido"]
  modulo_path: "03-modulos/"
  componentes: ["landing-publica"]
  etiquetas: ["content", "calendar", "organic-distribution", "seo", "media"]
  nivel_riesgo: bajo
creado_en: "2026-08-31"
actualizado_en: "2026-10-07"
---

# MKT-108 — Activos organicos y calendario editorial 30 dias

## Objetivo

Definir y preparar las piezas de contenido que alimentan las landings y su distribución orgánica,
y convertir el conjunto de landings autogeneradas en contenido revisado, diferenciado y con soporte
de manual mediante capturas autoalojadas. El vídeo queda como capacidad futura, no implementada.

## Documentos de esta historia

| Documento | Contenido |
| --------- | --------- |
| [inventario-landings.md](./inventario-landings.md) | Inventario de 12 landings en catálogo, auditoría y estado de guías previstas |
| [calendario-editorial-octubre.md](./calendario-editorial-octubre.md) | Calendario de 30 días replanificado del 7 de octubre al 5 de noviembre de 2026, con dos landings por semana |
| [tech-design.md](./tech-design.md) | Estado real del clúster `guia` y soporte de imágenes; vídeo pendiente |
| [briefs/](./briefs/) | Brief por landing actual y guía planificada, con misión y límites SEO |

## Requisitos de usuario

### HU-1 — Tener contenido listo para publicar sin improvisar

**Como** responsable de crecimiento,
**quiero** un calendario editorial de 30 días con piezas y copies mapeados a una landing destino,
**para** ejecutar la distribución orgánica sin depender de decisiones ad hoc cada semana.

### HU-2 — No prometer lo que el producto no hace

**Como** responsable de producto,
**quiero** que ninguna pieza de contenido describa una funcionalidad inexistente,
**para** no generar expectativas que el producto no cumple.

### HU-3 — Dejar de publicar landings autogeneradas

**Como** responsable de crecimiento,
**quiero** que cada landing tenga un brief que justifique por qué existe, qué keyword persigue y
qué afirmaciones la sostienen,
**para** que ninguna página vuelva a publicarse duplicando el contenido de otra.

### HU-4 — Documentar la plataforma con páginas tipo manual

**Como** persona que evalúa o ya usa Terrenario,
**quiero** páginas públicas que expliquen paso a paso cómo se configura y se opera cada área,
con capturas de producto,
**para** resolver mis dudas sin abrir un ticket ni adivinar.

## Alcance (in-scope)

- Paquete inicial de piezas y copies por canal.
- Calendario editorial de 30 días, del 7 de octubre al 5 de noviembre de 2026, a 2 landings por semana.
- Mapeo pieza -> landing destino.
- Auditoría de las 12 landings actualmente en catálogo y plan de corrección.
- Plantilla de brief de landing, obligatoria para crear o revisar cualquier landing.
- Clúster informacional `/guias/`: `como-empezar-en-terrenario` ya está implementada; las guías de
  Workspaces, temporadas y cosechas quedan planificadas. La guía de inicio cubre la intención de
  configuración inicial, sin duplicarla en otra URL.
- Soporte actual de imágenes autoalojadas en las guías. El vídeo queda pendiente y no se anuncia
  como capacidad disponible.
- El diseño/renderizado de vídeo en landings queda fuera de alcance hasta una decisión y validación
  técnica específica.
- Brief individual para las 12 landings del catálogo y las 3 guías planificadas, con motivo,
  misión de posicionamiento, intención, keywords, límites de canibalización y fuentes.

## Fuera de alcance

- Revisión de las 5 landings que el calendario desplaza al siguiente ciclo.
- Datos estructurados `HowTo` para las guías.
- Traducción o versiones en otros idiomas.

## Criterios de aceptación

- [ ] **CA-1**: Existe calendario con dos entregas de landing semanales, fechas y canal objetivo `web propia`; los canales para las piezas derivadas se cierran en MKT-109.
- [ ] **CA-2**: Cada entrega de landing y cada pieza derivada tiene su URL destino registrada; las piezas derivadas se completan en el registro de MKT-109.
- [ ] **CA-3**: El plan evita promesas no soportadas por funcionalidades reales.
- [ ] **CA-4**: Cada landing actual y cada guía planificada tiene un brief con motivo, misión de
  posicionamiento, keywords, enlazado y verificación factual.
- [ ] **CA-5**: Las 12 landings actuales están auditadas, con hallazgos verificados y acción asignada.
- [ ] **CA-6**: El clúster documental cubre inicio, Workspaces, temporadas y cosechas; la guía de
  inicio sustituye la propuesta duplicada de configuración inicial y cada guía nueva se enlaza
  contextualmente con su landing comercial.
- [ ] **CA-7**: Las guías admiten imágenes autoalojadas, con texto alternativo y datos anonimizados,
  sin recursos de terceros (`RN-042`). Vídeo queda fuera de lo implementado en esta historia.
