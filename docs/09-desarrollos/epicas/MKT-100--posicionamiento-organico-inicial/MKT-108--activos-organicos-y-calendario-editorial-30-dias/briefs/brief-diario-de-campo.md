---
bloque: "09-desarrollos"
documento: "brief-diario-de-campo"
actualizado_en: "2026-10-07"
landing:
  slug: "diario-de-campo"
  path: "/funcionalidades/diario-de-campo"
  cluster: "funcionalidad"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "diario de campo agrícola"
  keywords_secundarias: ["registro de labores agrícolas", "diario de actividades de una finca"]
  intencion: "comercial"
  canibaliza_con: []
enlazado:
  entrantes: ["software-gestion-agricola", "gestion-terrenos", "compras-y-consumos"]
  salientes: ["gestion-terrenos", "trabajadores-y-tareas", "compras-y-consumos", "control-cosechas"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Diario de campo

## 1. Por qué existe

- **Problema**: trabajos, compras y cosechas en soportes separados impiden reconstruir cronológicamente qué ocurrió.
- **Momento / persona**: evaluación y uso recurrente; propietario que registra jornadas y quiere consultar su histórico (`personas.md`).
- **Misión de posicionamiento**: captar búsquedas comerciales sobre diario de campo digital para explotación pequeña, presentando la cronología como capacidad del producto, no como tutorial de registro.

## 2. Objetivo de posicionamiento

- **Objetivo**: ser la URL funcional de referencia para «diario de campo agrícola» y conducir a registro/uso del producto.
- **Métrica / horizonte**: Search Console y sesiones propias por landing, revisión a 90 días contra baseline MKT-107; baseline local pendiente.
- **Rol**: apoyo funcional al pilar `software-gestion-agricola`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| diario de campo agrícola | principal | comercial | H1/meta |
| registro de labores agrícolas | secundaria | comercial | intro/bullets |
| diario de actividades de una finca | secundaria | comercial | FAQ |

- **No atacar**: cómo registrar cosecha, compras concretas ni tutorial de alta inicial; corresponden a otras landings/guías.

## 4. Contenido y estructura

- **H1**: Diario de campo: todo lo que pasa en tu explotación, por fecha.
- **H2**: cronología unificada; qué registra una actividad; responsables y coste manual; preguntas frecuentes.
- **CTA**: acceder a Terrenario. Extensión objetivo: 600–800 palabras, sin estirar el contenido.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Diario mezcla actividades, compras, consumos y cosechas | `docs/03-modulos/diario-y-operativa/README.md`, Qué es (RN-033) | [x] |
| Actividad registra tarea, responsable, tiempo y coste manual | misma ficha, Scope (RN-002/003) | [x] |
| La tarea libre puede quedar aprendida para reutilizar | `docs/03-modulos/maestros-operativos/README.md`, scope tareas | [x] |

- **Excluir**: generación automática de partes, tarifas o costes automáticos, recomendaciones agronómicas.

## 6. Media

No hay captura asociada hoy. Propuesta opcional: una pantalla del diario con Workspace sintético; no publicar hasta anonimizar y revisar privacidad.

## 7. Enlazado

Portada y landings de terrenos/compras enlazan aquí. Salientes a terrenos, trabajadores y cosechas por relaciones operativas.

## 8. Metadatos

Title actual: «Diario de campo agrícola | Terrenario». Mantener description única centrada en cronología; FAQ actuales propias del tema. Crear OG/Twitter solo con imagen local.

## 9. Distribución

Landing web; pieza derivada y canal/fecha a registrar en MKT-109. URL destino: `/funcionalidades/diario-de-campo`.

## 10. Cierre

- [ ] Diferenciar capacidad comercial de tutorial; actualizar enlaces, FAQ y metadatos; tests/build verdes.
