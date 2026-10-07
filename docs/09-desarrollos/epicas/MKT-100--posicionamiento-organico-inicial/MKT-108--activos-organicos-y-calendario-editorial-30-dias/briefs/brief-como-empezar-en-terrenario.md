---
bloque: "09-desarrollos"
documento: "brief-como-empezar-en-terrenario"
actualizado_en: "2026-10-07"
landing:
  slug: "como-empezar-en-terrenario"
  path: "/guias/como-empezar-en-terrenario"
  cluster: "guia"
  estado: en-revision
  tipo_trabajo: "nueva"
seo:
  keyword_principal: "cómo empezar en Terrenario"
  keywords_secundarias: ["primeros pasos en Terrenario", "crear un Workspace agrícola", "cómo añadir un terreno en Terrenario"]
  intencion: "informacional"
  canibaliza_con: ["software-gestion-agricola", "gestion-terrenos", "gestion-de-workspaces", "gestion-de-temporadas"]
enlazado:
  entrantes: ["/"]
  salientes: ["software-gestion-agricola", "gestion-terrenos", "dashboard-campana"]
media:
  - "/landings/como-empezar-en-terrenario/01_crear_workspace_terrenario.png"
  - "/landings/como-empezar-en-terrenario/02_crear_temporada_terrenario.png"
  - "/landings/como-empezar-en-terrenario/03_anadir_terreno_terrenario.png"
  - "/landings/como-empezar-en-terrenario/04_nuevo_terreno_terrenario.png"
  - "/landings/como-empezar-en-terrenario/05_nuevo_terreno_formulario_terrenario.png"
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Cómo empezar en Terrenario

## 1. Por qué existe

- **Problema**: una persona que llega por primera vez puede no saber cuál es la secuencia de acceso, Workspace, temporada y terrenos.
- **Momento / persona**: onboarding/consideración; agricultor particular o familiar con poca disponibilidad para aprender herramientas complejas.
- **Qué pasa sin esta landing**: el primer acceso y la preparación básica quedan explicados solo en pantallas de producto; no hay un manual público con capturas.
- **Misión de posicionamiento**: resolver consultas informacionales de inicio y servir como puente hacia las landings comerciales pertinentes. Sustituye la propuesta anterior `/guias/configuracion-inicial`; no crear ambas URLs con la misma intención.

## 2. Objetivo de posicionamiento

- **Objetivo**: capturar consultas de primeros pasos en Terrenario y reducir incertidumbre previa al acceso, sin prometer que todo el alta se completa en un tiempo garantizado.
- **Métrica / horizonte**: impresiones/clics/consultas Search Console y sesiones propias a esta ruta; revisar a 90 días desde el despliegue. El baseline por URL está pendiente de confirmación.
- **Rol**: guía informacional de inicio; refuerza el pilar `software-gestion-agricola` y enlaza a guías específicas posteriores.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| cómo empezar en Terrenario | principal | informacional | H1/title |
| primeros pasos en Terrenario | secundaria | informacional | intro |
| crear un Workspace agrícola | secundaria | informacional | paso 2 |
| cómo añadir un terreno en Terrenario | secundaria | informacional | pasos 4–6 |

- **No atacar**: evaluación de software agrícola genérico (pilar), descripción exhaustiva de Workspace (funcionalidad) ni guías dedicadas de temporadas/cosechas.

## 4. Contenido y estructura

- **H1**: Cómo empezar en Terrenario.
- **Pasos**: iniciar sesión con Cuenta de Google; crear Workspace; decidir si crear temporada; abrir preparación de terrenos; iniciar alta; completar ficha.
- **Propuesta**: un recorrido inicial comprensible, con capturas de la interfaz y campos obligatorios/opcionales diferenciados.
- **CTA**: acceder a Terrenario.
- **Extensión**: 650–900 palabras, incluyendo FAQ; priorizar claridad, no longitud.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| El login usa Google OIDC y una Cuenta de Google puede usar correo no Gmail | `docs/03-modulos/identidad-y-workspaces/README.md`, Conceptos clave; MVP-101 | [x] |
| Crear Workspace fija contexto y lleva a `/app` | MVP-102 `tech-design.md`, flujo principal | [x] |
| Si no hay temporada se ofrece crearla, pero se puede continuar sin ella | MVP-201 `tech-design.md` y `CreateWorkspacePage.tsx` | [x] |
| Alta mínima del terreno pide nombre/tipo; otros campos se completan después | MVP-202 `tech-design.md`; `maestros-operativos/README.md` | [x] |

- **Excluir**: promesa de completar el proceso en menos de 5 minutos; la temporada no es obligatoria; no existe acceso anónimo.

## 6. Media

| Fichero | Qué muestra | Alt | Datos | Peso aproximado |
| --- | --- | --- | --- | --- |
| `01_crear_workspace_terrenario.png` | Campo de nombre para Workspace | Descripción accesible en el contenido | Captura anonimizada | 51 KB |
| `02_crear_temporada_terrenario.png` | Formulario y opción de continuar después | Descripción accesible en el contenido | Captura anonimizada | 34 KB |
| `03_anadir_terreno_terrenario.png` | Apartado Terrenos en preparación | Descripción accesible en el contenido | Sintéticos | 36 KB |
| `04_nuevo_terreno_terrenario.png` | Acción de añadir primer terreno | Descripción accesible en el contenido | Sintéticos | 18 KB |
| `05_nuevo_terreno_formulario_terrenario.png` | Campos de la ficha | Descripción accesible en el contenido | Sintéticos | 41 KB |

Las copias públicas están bajo `src/frontend/terrenario-web/public/landings/como-empezar-en-terrenario/` y se pre-renderizan en HTML. Las dos primeras fueron anonimizadas; originales fuera del repositorio. No hay vídeo. Confirmar alt/dimensiones contra el catálogo actual antes de variar assets.

## 7. Enlazado interno

- **Entrantes**: hub «Ayuda y manuales» de la portada (`/`).
- **Salientes**: `software-gestion-agricola`, `gestion-terrenos` y `dashboard-campana`; enlaces a guías específicas cuando se publiquen.
- **Huérfana**: no; además del hub, comprobar enlace contextual tras publicar el pilar revisado.

## 8. Metadatos

- **Title** actual: «Cómo empezar en Terrenario: primeros pasos».
- **Description** actual: empezar creando Workspace y preparar datos; revisar que no prometa un proceso obligatorio completo.
- **FAQ**: Gmail no requerido (se necesita Cuenta Google), temporada opcional, campos mínimos de terreno.
- **Social**: sin imagen social propia hasta diseñar una; no reutilizar capturas de datos personales.

## 9. Distribución

Destino `/guias/como-empezar-en-terrenario`. Publicación derivada y canal/fecha se registran en MKT-109; no se presupone canal.

## 10. Cierre

- [x] Existe entrada en `LANDING_CONTENTS`, HTML prerenderizado y URL en sitemap del build local.
- [x] Cinco imágenes autoalojadas con alt/dimensiones y copias públicas anonimizadas.
- [ ] Confirmar publicación/despliegue productivo.
- [ ] Completar revisión SEO/ortográfica final y enlazado contextual.
- [ ] No afirmar vídeo ni datos de Search Console no verificados.
