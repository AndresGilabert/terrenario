---
id: "MKT-104"
tipo: feature
titulo: "TDD: Datos estructurados y FAQ por landing"
estado: en-testing
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seo", "marketing"]
  modulo_path: "03-modulos/plataforma-de-aplicacion"
  componentes: ["schema-org", "landing-publica"]
  etiquetas: ["json-ld", "faq", "rich-results"]
  nivel_riesgo: bajo
creado_en: "2026-09-01"
actualizado_en: "2026-09-01"
---

# TDD: MKT-104 - Datos estructurados y FAQ por landing

> **Referencia al spec**: [spec.md](./spec.md)
> **Decisión de arquitectura**: [ADR-0012](../../../../02-arquitectura/decisiones/ADR-0012--prerenderizado-estatico-landings-publicas-mkt-102.md)

## Resumen técnico

Cada una de las diez landings P0 incorporará una sección visible de preguntas frecuentes y un
bloque JSON-LD. El bloque se genera durante el pre-renderizado estático ya adoptado: incluye
`Organization`, `SoftwareApplication` y el `FAQPage` de la landing. No se añaden dependencias,
endpoints, analítica ni datos personales.

## Diseño detallado

`LandingContent` será la única fuente de contenido para el titular, el texto visible y las FAQ.
La vista mostrará las preguntas y respuestas desde esa estructura y el pre-renderizador recibirá el
mismo contenido para serializar el `FAQPage`. Así no existe una segunda copia editorial que pueda
divergir de la UI.

El `Organization` solo declara `name` y `url`. El `SoftwareApplication` declara el nombre de
producto, el tipo `WebApplication` y la categoría de aplicación de gestión agrícola. No se
publican precio, valoraciones, perfiles externos ni identidad legal.

Los datos se insertan como JSON-LD en el `head` de cada HTML estático. La serialización escapará
`<` para impedir que el contenido editorial cierre el elemento `script`.

## Componentes afectados

| Componente | Cambio |
| ---------- | ------ |
| `src/content/landings.ts` | Añade FAQ tipadas, basadas exclusivamente en el contenido ya publicado de cada landing. |
| `src/components/marketing/ContentLandingPage.tsx` | Renderiza la sección FAQ visible en cada landing. |
| `scripts/prerenderizar-landings.mjs` | Inserta los tres tipos de datos estructurados desde el contenido de la landing. |
| Tests de contenido, componente y pre-renderizado | Verifican la correspondencia entre FAQ visible y JSON-LD, junto con la forma de los schemas. |

## Plan de testing

- [x] Tests unitarios: cada landing tiene FAQ no vacías y el contenido es apto para la UI.
- [x] Tests de componente: las FAQ se muestran con su pregunta y respuesta.
- [x] Tests de build: el JSON-LD contiene `Organization`, `SoftwareApplication` y el `FAQPage` que corresponde a la FAQ visible.
- [ ] Tests de integración: no aplica, no hay API ni backend afectados.
- [ ] Tests e2e: no aplica, el HTML estático queda cubierto por build y pruebas unitarias.

## Riesgos e impacto

| Riesgo | Mitigación |
| ------ | ---------- |
| JSON-LD no alineado con la UI | Ambas salidas consumen el mismo campo `faqs` de `LandingContent`. |
| Contenido estructurado inválido por caracteres editoriales | El serializador neutraliza `<` antes de insertar el JSON en el documento. |
| Regresión de indexación | Se preserva el pre-renderizado estático, canonical y `hreflang` definidos en MKT-103. |