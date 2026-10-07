---
bloque: "09-desarrollos"
documento: "brief-gestion-de-temporadas"
actualizado_en: "2026-10-07"
landing:
  slug: "gestion-de-temporadas"
  path: "/guias/gestion-de-temporadas"
  cluster: "guia"
  estado: borrador
  tipo_trabajo: "nueva"
seo:
  keyword_principal: "cómo gestionar temporadas agrícolas en Terrenario"
  keywords_secundarias: ["crear una campaña agrícola", "seleccionar temporada de trabajo"]
  intencion: "informacional"
  canibaliza_con: ["dashboard-campana", "como-empezar-en-terrenario"]
enlazado:
  entrantes: ["/"]
  salientes: ["dashboard-campana", "software-gestion-agricola"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Gestión de temporadas

## 1. Por qué existe

- **Problema**: la campaña organiza registros y comparaciones; se confunden temporada creada, temporada activa y la oferta inicial opcional.
- **Momento / persona**: onboarding/uso; propietario que prepara una campaña o cambia de contexto de trabajo.
- **Misión de posicionamiento**: capturar intención informacional de crear/gestionar/seleccionar una temporada y apoyar el dashboard, sin perseguir el término comercial del panel.

## 2. Objetivo de posicionamiento

- **Objetivo**: dar instrucciones precisas de ciclo de temporada y explicar diferencias de conceptos que afectan a registros.
- **Métrica / horizonte**: impresiones/clics/consultas en Search Console y sesiones propias a 90 días; baseline pendiente.
- **Rol**: guía de soporte al dashboard y al pilar.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| cómo gestionar temporadas agrícolas en Terrenario | principal | informacional | H1/pasos |
| crear una campaña agrícola | secundaria | informacional | intro |
| seleccionar temporada de trabajo | secundaria | informacional | sección/FAQ |

- **No atacar**: «dashboard de campaña agrícola» (landing funcional) ni crear una URL separada de configuración inicial.

## 4. Contenido

H1 propuesto: «Cómo crear y gestionar temporadas en Terrenario». Explicar alta, fechas, estados derivados y selección por usuario solo después de verificar en UI. Extensión 650–900 palabras; media capturada en demo si ayuda.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Temporadas tienen fechas; estado se deriva y no se declara manualmente | `docs/03-modulos/maestros-operativos/README.md`, Scope/conceptos (RN-021/022) | [x] |
| Existe temporada de trabajo por usuario | misma ficha (RN-024) | [x] |
| La creación inicial es opcional y cancelable | MVP-201 `tech-design.md`, oferta de temporada | [x] |

- **Excluir**: cierre automático no documentado, apertura obligatoria y comparativas históricas no soportadas.

## 6. Media

Pendiente. Capturas de selector/formulario con datos sintéticos, sin nombres de fincas reales.

## 7. Enlazado

Portada, salida contextual al dashboard; luego añadir enlace desde `como-empezar-en-terrenario` si el texto introduce temporada.

## 8. Metadatos

Title/description propuestos como tutorial de temporadas; FAQ exclusivas sobre fecha/estado/selección, sin duplicar FAQ del dashboard.

## 9. Distribución

Ventana 19–22 oct; distribución en MKT-109. Destino `/guias/gestion-de-temporadas`.

## 10. Cierre

- [ ] Verificar pantallas y terminología activa; brief aprobado; screenshots, enlaces, tests/build.
