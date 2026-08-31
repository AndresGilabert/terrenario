---
id: "MKT-110"
tipo: tarea
titulo: "Loop de referral organico"
estado: borrador
prioridad: media
sprint: ""
hito: "Post-MVP — Crecimiento orgánico"
esfuerzo_estimado: "1d"
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
depende_de: ["MKT-101", "MKT-106", "MKT-107", "MKT-109"]
bloquea: ["MKT-199"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["marketing", "adquisicion", "conversion"]
  modulo_path: "03-modulos/"
  componentes: ["landing-publica", "tracking-campaigns"]
  etiquetas: ["referral", "organic", "utm", "manual-share"]
  nivel_riesgo: bajo
creado_en: "2026-08-31"
actualizado_en: "2026-08-31"
---

# MKT-110 — Loop de referral organico

## Contexto

Existe ya una invitación a Workspace (`MVP-103`), pero esa vía da acceso a **tu propia explotación** y no
sirve para captar usuarios nuevos ajenos. Esta historia es una acción de marketing independiente: no
reutiliza ni modifica el flujo de invitación a Workspace y no requiere cambios de producto ni de
backend.

## Objetivo

Dar a cualquier usuario un enlace o plantilla compartible con parámetros de campaña propios, para que
pueda recomendar Terrenario a otro agricultor que cree su propio Workspace, y poder medir esa
recomendación como canal de adquisición diferenciado.

## Requisitos de usuario

### HU-1 — Recomendar el producto sin fricción

**Como** usuario de Terrenario,
**quiero** disponer de un mensaje y enlace ya preparados para compartir por WhatsApp u otros canales,
**para** recomendar el producto a otro agricultor sin tener que redactarlo yo mismo.

### HU-2 — Distinguir la adquisición por recomendación del resto de tráfico orgánico

**Como** responsable de crecimiento,
**quiero** que el enlace de recomendación lleve parámetros de campaña propios (UTM de primera parte),
**para** medir cuántas visitas y altas proceden de recomendación entre agricultores frente a otros
canales.

## Alcance (in-scope)

- Copy y plantilla de mensaje compartible (sin cambios de producto ni backend).
- Enlace con parámetros de campaña de primera parte hacia una landing pública.
- Publicación del copy en un canal accesible para cualquier usuario (por ejemplo, la propia landing o
  un documento compartido), sin nueva pantalla dentro de la aplicación.

## Fuera de alcance (out-of-scope)

- Generación automática de enlaces o códigos de referido únicos por usuario.
- Cualquier cambio en el flujo de invitación a Workspace (`MVP-103`).
- Recompensas, incentivos o programas de afiliación.

## Criterios de aceptación

- [ ] **CA-1**: Existe una plantilla de mensaje y enlace de recomendación lista para compartir.
- [ ] **CA-2**: El enlace de recomendación lleva parámetros de campaña propios y distinguibles del resto
      de tráfico orgánico.
- [ ] **CA-3**: El resultado del canal de recomendación se incorpora al resumen operativo periódico
      (`MKT-101`).
