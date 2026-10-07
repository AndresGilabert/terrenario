---
bloque: "09-desarrollos"
documento: "brief-explotacion-familiar"
actualizado_en: "2026-10-07"
landing:
  slug: "explotacion-familiar"
  path: "/para/explotacion-familiar"
  cluster: "perfil"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "software de gestión para explotación agrícola familiar"
  keywords_secundarias: ["gestión agrícola compartida en familia", "compartir datos de una finca familiar"]
  intencion: "comercial"
  canibaliza_con: ["workspaces-colaboracion", "agricultor-particular"]
enlazado:
  entrantes: []
  salientes: ["workspaces-colaboracion", "trabajadores-y-tareas", "diario-de-campo"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Explotación familiar

## 1. Por qué existe

- **Problema**: el trabajo de una explotación familiar depende de conversaciones y de una sola persona que conserva los apuntes.
- **Momento / persona**: descubrimiento/evaluación; propietario que colabora con familiares (`docs/01-producto/personas.md`).
- **Misión de posicionamiento**: captar la intención comercial específica de gestión compartida por una familia; no competir con funcionalidad general de Workspaces ni con perfil individual.

## 2. Objetivo de posicionamiento

- **Objetivo**: mostrar el caso de uso familiar y derivar a la capacidad Workspace/colaboración.
- **Métrica / horizonte**: Search Console y sesiones propias por URL, revisión a 90 días desde baseline disponible. Sin volumen o posición estimados.
- **Rol**: landing de perfil; apoya `workspaces-colaboracion`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| software de gestión para explotación agrícola familiar | principal | comercial | H1/meta |
| gestión agrícola compartida en familia | secundaria | comercial | intro |
| compartir datos de una finca familiar | secundaria | comercial | FAQ |

- **No atacar**: aplicación para agricultor particular ni «cómo invitar usuarios» (guía Workspace futura).

## 4. Contenido

H1 actual «Terrenario para explotaciones familiares». H2: un espacio común, registro por persona, permisos actuales, preguntas. CTA acceso. Extensión 500–700 palabras.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Workspace reúne datos compartidos y admite invitaciones por email/enlace | `docs/03-modulos/identidad-y-workspaces/README.md`, Scope | [x] |
| En MVP miembros operan con permisos planos | misma ficha, fuera de scope (RN-034) | [x] |
| Actividad conserva responsable y tiempo | `docs/03-modulos/diario-y-operativa/README.md`, Scope | [x] |

- **Excluir**: roles familiares, aprobaciones, mensajería interna o permisos por usuario.

## 6. Media

Ninguna actualmente. Si se producen capturas, usar Workspace y miembros ficticios.

## 7. Enlazado

La portada la enlaza; no tiene `relatedSlugs` entrantes actualmente. Salientes a Workspaces, trabajadores y diario. Añadir entrada contextual solo desde casos familiares relacionados.

## 8. Metadatos

Title «Terrenario para explotaciones familiares». Reescribir meta social de forma propia; evitar repetir copy de colaboración.

## 9. Distribución

Destino `/para/explotacion-familiar`; calendario de canal/fecha en MKT-109.

## 10. Cierre

- [ ] Caso familiar claramente distinto del producto genérico; claims y enlaces verificados; tests/build verdes.
