---
id: "MKT-102"
tipo: feature
titulo: "Landings publicas P0 de funcionalidades y casos"
estado: en-testing
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
revisores: ["@andres"]
ai_context:
  dominios: ["marketing", "seo", "conversion"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["landing-publica", "rutas-publicas"]
  etiquetas: ["landing", "public-pages", "organic"]
  nivel_riesgo: medio
creado_en: "2026-08-31"
actualizado_en: "2026-09-20"
---

# MKT-102 — Landings publicas P0 de funcionalidades y casos

## Contexto

Sin contenido público orientado a búsqueda, no hay base para indexación ni aterrizaje de usuarios fríos.

## Objetivo

Publicar el conjunto inicial de landings definidas para funcionalidades y casos de uso.

## Reapertura de 2026-09-20

La primera entrega resolvió publicación, indexabilidad y enlazado, pero dejó las landings con una
estructura editorial demasiado breve para explicar el problema, desarrollar la solución y sostener
la captación desde búsquedas específicas. Se reabre la historia para convertir
`/funcionalidades/control-cosechas` en una landing especializada en **cosecha de olivar** y para
fijar un estándar de construcción reutilizable en el resto de landings.

Decisiones de producto confirmadas en la reapertura:

- La intención de búsqueda de `/funcionalidades/control-cosechas` será exclusivamente olivar.
- El término canónico visible será «rendimiento de aceite», explicado como litros por cada 100 kg
  de aceituna (`RN-013`); no se presentará como análisis químico de rendimiento graso.
- Solo se publicarán capacidades y beneficios verificables en la KB y en el producto. Se excluyen
  precio o gratuidad, aplicaciones nativas Android/iOS, autoría «por olivicultores» y promesas de
  aumentar o maximizar la rentabilidad mientras no exista evidencia que las respalde.

## Requisitos de usuario

### HU-1 — Decidir con información antes de acceder

**Como** persona que busca gestionar su explotación agrícola y llega desde un buscador,
**quiero** encontrar una página pública que explique una funcionalidad concreta de Terrenario,
**para** decidir si el producto resuelve mi problema antes de crear cuenta.

### HU-2 — Navegar entre funcionalidades relacionadas

**Como** visitante de una landing,
**quiero** poder llegar a otras landings de funcionalidades o casos de uso relacionados,
**para** entender el alcance completo del producto sin volver a buscar en el buscador.

### HU-3 — Disponer de contenido indexable antes del rastreo técnico

**Como** responsable de crecimiento,
**quiero** que existan URLs públicas con contenido sustantivo por funcionalidad y caso de uso,
**para** tener superficie indexable real antes de publicar `robots.txt` y `sitemap.xml`.

### HU-4 — Entender el problema, la solución y el siguiente paso

**Como** olivicultor que llega desde una búsqueda sobre control de cosecha,
**quiero** recorrer una landing que explique mi problema, las capacidades reales de Terrenario, los
beneficios verificables y las dudas frecuentes,
**para** decidir con suficiente contexto si accedo a la aplicación.

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
- Estructura editorial ampliable por landing: hero, problema, funcionalidades, beneficios, FAQ,
  enlazado relacionado y CTA final.
- Especialización de `/funcionalidades/control-cosechas` en cosecha de olivar.

## Fuera de alcance (out-of-scope)

- Blog o centro editorial completo.
- Multilenguaje.

## Criterios de aceptación

- [ ] **CA-1**: Las 10 landings públicas responden en producción con contenido útil.
- [ ] **CA-2**: Todas las landings enlazan a `/login` y a landings relacionadas.
- [ ] **CA-3**: La home enlaza al menos a los clústeres principales de funcionalidades.
- [ ] **CA-4**: La landing de control de cosechas tiene contenido visible y pre-renderizado sobre el
  problema, las capacidades, los beneficios verificables, las FAQ y el CTA para cosecha de olivar.
- [ ] **CA-5**: `title`, description, `h1`, FAQ y datos estructurados mantienen la misma intención de
  búsqueda y no afirman capacidades, plataformas, precios ni resultados no documentados.
- [ ] **CA-6**: El estándar de ingeniería documenta los requisitos de contenido, SEO, evidencia,
  enlazado, activos y validación que debe cumplir cualquier landing nueva o modificada.
