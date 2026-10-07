---
bloque: "09-desarrollos"
documento: "brief-workspaces-colaboracion"
actualizado_en: "2026-10-07"
landing:
  slug: "workspaces-colaboracion"
  path: "/funcionalidades/workspaces-colaboracion"
  cluster: "funcionalidad"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "software agrícola para trabajar en equipo"
  keywords_secundarias: ["compartir gestión de una explotación", "gestión agrícola familiar multiusuario"]
  intencion: "comercial"
  canibaliza_con: ["gestion-de-workspaces", "explotacion-familiar"]
enlazado:
  entrantes: ["trabajadores-y-tareas", "explotacion-familiar"]
  salientes: ["trabajadores-y-tareas", "diario-de-campo", "gestion-de-workspaces"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Workspaces y colaboración

## 1. Por qué existe

- **Problema**: varias personas de una familia/equipo necesitan consultar la misma explotación sin duplicar hojas.
- **Momento / persona**: evaluación; propietario que invita a familia o colaboradores.
- **Misión de posicionamiento**: explicar comercialmente la colaboración multiusuario por Workspace. La guía futura cubre pasos de invitación; la landing de explotación familiar conserva el foco de segmento.

## 2. Objetivo de posicionamiento

- **Objetivo**: presentar Workspace compartido como capacidad del producto y captar intención comercial de gestión agrícola colaborativa.
- **Métrica / horizonte**: Search Console (impresiones/clics/consultas) y sesiones propias a 90 días; baseline individual pendiente.
- **Rol**: apoyo del pilar `software-gestion-agricola`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| software agrícola para trabajar en equipo | principal | comercial | H1/intro |
| compartir gestión de una explotación | secundaria | comercial | sección/FAQ |
| gestión agrícola familiar multiusuario | secundaria | comercial | bullets |

- **No atacar**: consultas «cómo invitar» (guía futura) ni queries de software agrícola genérico.

## 4. Contenido

H1 actual «Workspaces y colaboración: la misma explotación, varias personas». Explicar unidad compartida, invitación por email/enlace de un solo uso y permisos planos actuales. CTA: acceder. 550–750 palabras.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Workspace es ámbito de datos/autoridad; invitación por email o enlace | `docs/03-modulos/identidad-y-workspaces/README.md`, Scope | [x] |
| Miembros tienen permisos planos en MVP | misma ficha, fuera de scope (RN-034) | [x] |
| Al salir un miembro se conserva su histórico operativo | `docs/03-modulos/identidad-y-workspaces/README.md`, ciclo de vida; verificar copy vigente antes de publicar | [ ] |

- **Excluir**: roles granulares, permisos por registro y acceso anónimo.

## 6. Media

Ninguna en el catálogo. Si se agrega captura del flujo de invitación: Workspace demo y correos ficticios; nunca enlaces/tokens reales.

## 7. Enlazado

Mantener entrada desde `trabajadores-y-tareas` y `explotacion-familiar`; salida al diario y guía cuando se implemente. La home es enlace de navegación común, no sustituto de enlace contextual.

## 8. Metadatos

Title actual «Workspaces y colaboración agrícola | Terrenario». FAQ propias. Reescribir descripción social con énfasis comercial, sin repetir textos de la futura guía.

## 9. Distribución

Landing: `/funcionalidades/workspaces-colaboracion`. Publicaciones y canal/fecha quedan trazados en MKT-109.

## 10. Cierre

- [ ] Verificar flujo actual de invitación y salida; cerrar afirmaciones pendientes; enlaces y meta diferenciados; tests/build verdes.
