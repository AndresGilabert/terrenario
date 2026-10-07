---
bloque: "09-desarrollos"
documento: "brief-gestion-terrenos"
actualizado_en: "2026-10-07"
landing:
  slug: "gestion-terrenos"
  path: "/funcionalidades/gestion-terrenos"
  cluster: "funcionalidad"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "gestión de terrenos agrícolas"
  keywords_secundarias: ["ficha de parcela agrícola", "registrar terrenos de una explotación"]
  intencion: "comercial"
  canibaliza_con: ["como-empezar-en-terrenario"]
enlazado:
  entrantes: ["software-gestion-agricola", "diario-de-campo", "control-cosechas"]
  salientes: ["diario-de-campo", "control-cosechas", "dashboard-campana"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Gestión de terrenos

## 1. Por qué existe

- **Problema**: la información de parcelas y su vínculo con el trabajo queda dispersa; la persona no ve cómo una ficha organiza los registros posteriores.
- **Momento / persona**: evaluación; propietario de pequeña explotación (Antonio; `docs/01-producto/personas.md`).
- **Misión de posicionamiento**: captar la intención comercial de quien evalúa una herramienta para registrar y organizar terrenos agrícolas; demostrar la ficha como unidad base, no enseñar el tutorial de alta.

## 2. Objetivo de posicionamiento

- **Objetivo**: ser la landing comercial del tema «gestión de terrenos agrícolas» y apoyar el pilar del producto.
- **Métrica / horizonte**: impresiones, clics y consultas en Search Console; comparar con baseline de MKT-107 a 90 días. No hay volumen ni ranking objetivo verificado.
- **Rol / pilar**: apoyo funcional; refuerza `software-gestion-agricola`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| gestión de terrenos agrícolas | principal | comercial | H1/meta |
| ficha de parcela agrícola | secundaria | comercial | sección de alta mínima |
| registrar terrenos de una explotación | secundaria | comercial | FAQ |

- **No atacar**: «cómo añadir mi primer terreno» (guía de inicio); control de cosecha/rendimiento (landing específica).

## 4. Contenido y estructura

- **H1**: Gestión de terrenos: la ficha de cada parcela, siempre a mano.
- **H2**: qué identifica una ficha; alta mínima y datos opcionales; cómo se vinculan registros; FAQ.
- **Propuesta / CTA**: ficha sencilla que sirve de base a la actividad; «Acceder a Terrenario».
- **Extensión**: 550–750 palabras, solo si aporta respuesta real.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Nombre y tipo de propiedad permiten alta mínima; otros campos pueden completarse después | `docs/03-modulos/maestros-operativos/README.md`, scope y alta mínima RN-028 | [x] |
| Registros operativos requieren terreno | `docs/01-producto/vision-y-objetivos.md`, R1 | [x] |
| Número de olivos permite indicadores por árbol si está informado | `docs/03-modulos/produccion-y-dashboard/README.md`, dato incompleto | [x] |

- **Excluir**: mapas/GPS, importación catastral automática, optimización o cálculo no implementados.

## 6. Media

Sin media asignada en el catálogo actual. No reutilizar capturas hasta tener una captura sintética aprobada.

## 7. Enlazado

La portada enlaza a la URL. Añadir enlaces contextuales desde el pilar y el diario; salientes a cosechas y dashboard porque reutilizan el terreno como dimensión.

## 8. Metadatos

- **Title** actual: «Gestión de terrenos agrícolas | Terrenario».
- **Description** actual: describe ficha, propietario, ubicación y árboles; mantenerla factual y única.
- **FAQ / social**: 2 FAQ actuales; revisar respuesta propia, añadir SEO social solo con copy/imagen únicos.

## 9. Distribución

Web: `/funcionalidades/gestion-terrenos`. Pieza derivada: pendiente de canal/fecha en MKT-109; destino obligatorio esta URL.

## 10. Cierre

- [ ] Brief validado por responsable; enlazado contextual añadido; FAQ/meta revisadas; tests y build pasan.
