---
bloque: "09-desarrollos"
documento: "brief-gestion-de-cosechas"
actualizado_en: "2026-10-07"
landing:
  slug: "gestion-de-cosechas"
  path: "/guias/gestion-de-cosechas"
  cluster: "guia"
  estado: borrador
  tipo_trabajo: "nueva"
seo:
  keyword_principal: "cómo registrar una cosecha de aceituna en Terrenario"
  keywords_secundarias: ["anotar kilos de aceituna por terreno", "introducir rendimiento de aceite de oliva"]
  intencion: "informacional"
  canibaliza_con: ["control-cosechas"]
enlazado:
  entrantes: ["/"]
  salientes: ["control-cosechas", "dashboard-campana"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Gestión de cosechas

## 1. Por qué existe

- **Problema**: el usuario puede no conocer el orden y los campos disponibles para dejar registrada una cosecha de aceituna.
- **Momento / persona**: uso; propietario de olivar en época de recolección.
- **Misión de posicionamiento**: capturar consultas informacionales de captura paso a paso. `control-cosechas` conserva la intención comercial de evaluar la funcionalidad.

## 2. Objetivo de posicionamiento

- **Objetivo**: servir como tutorial factual enlazado a la funcionalidad y ayudar a la persona a completar datos sin inferir campos.
- **Métrica / horizonte**: consultas, impresiones, clics y sesiones propias; revisión a 90 días tras baseline disponible.
- **Rol**: guía de soporte.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| cómo registrar una cosecha de aceituna en Terrenario | principal | informacional | H1/pasos |
| anotar kilos de aceituna por terreno | secundaria | informacional | pasos |
| introducir rendimiento de aceite de oliva | secundaria | informacional | sección/FAQ |

- **No atacar**: evaluación «control de cosecha de olivar» (funcionalidad); molturación/cálculo de precio.

## 4. Contenido

H1 propuesto: «Cómo registrar una cosecha de aceituna». Secuencia a comprobar en producto: abrir cosechas, seleccionar terreno/temporada, fecha, kilos, destino y dato de rendimiento permitido. Extensión 650–900 palabras, capturas sintéticas.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Kilos obligatorios; rendimiento/litros opcionales y excluyentes | `docs/03-modulos/produccion-y-dashboard/README.md`, Scope (RN-004/014/016) | [x] |
| Rendimiento se expresa en litros por 100 kg | misma ficha, Conceptos clave (RN-013) | [x] |
| Destino es catálogo cerrado y existe «Sin destino» | misma ficha, Scope (RN-012/030) | [x] |

- **Excluir**: cálculo de molturación, precio de aceite, recomendaciones/estimaciones de cosecha.

## 6. Media

Pendiente: capturas de demo tras confirmar flujo de UI; datos sintéticos y medios locales con alt.

## 7. Enlazado

Portada y relación contextual con `control-cosechas`, salida a dashboard. Conservar anchor informacional, distinto del comercial.

## 8. Metadatos

Title/description deben usar «cómo registrar» y no replicar el title comercial de cosecha. FAQ propias sobre campos obligatorios y alternativas de rendimiento.

## 9. Distribución

Ventana 26–29 oct; pieza derivada/canal en MKT-109. Destino `/guias/gestion-de-cosechas`.

## 10. Cierre

- [ ] Verificar flujo en producto; brief aprobado; capturas anonimizada; metadatos/enlaces/tests/build.
