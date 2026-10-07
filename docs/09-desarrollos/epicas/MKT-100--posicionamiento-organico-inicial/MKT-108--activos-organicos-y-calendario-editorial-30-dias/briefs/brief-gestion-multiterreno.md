---
bloque: "09-desarrollos"
documento: "brief-gestion-multiterreno"
actualizado_en: "2026-10-07"
landing:
  slug: "gestion-multiterreno"
  path: "/para/gestion-multiterreno"
  cluster: "perfil"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "gestión de varias parcelas agrícolas"
  keywords_secundarias: ["control de producción por terreno", "gestionar fincas agrícolas dispersas"]
  intencion: "comercial"
  canibaliza_con: ["gestion-terrenos", "dashboard-campana"]
enlazado:
  entrantes: ["gestion-terrenos"]
  salientes: ["gestion-terrenos", "dashboard-campana", "control-cosechas"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Gestión multiterreno

## 1. Por qué existe

- **Problema**: sumar parcelas diferentes oculta diferencias de coste y producción; el usuario necesita un criterio común por terreno.
- **Momento / persona**: evaluación; propietario con varios terrenos separados, segmento respaldado por `docs/01-producto/personas.md`.
- **Misión de posicionamiento**: captar intención comercial del usuario que gestiona varias parcelas y necesita visión desglosada; gestión-terrenos explica la ficha, no este caso de uso.

## 2. Objetivo de posicionamiento

- **Objetivo**: explicar pertinencia del producto para varias parcelas y enlazar a capacidades de terreno/dashboard.
- **Métrica / horizonte**: consultas Search Console, clics y sesiones propias; revisión a 90 días, baseline por URL pendiente.
- **Rol**: landing de perfil; apoyo del pilar general.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| gestión de varias parcelas agrícolas | principal | comercial | H1/meta |
| control de producción por terreno | secundaria | comercial | intro |
| gestionar fincas agrícolas dispersas | secundaria | comercial | FAQ |

- **No atacar**: alta de ficha individual (gestión-terrenos) ni dashboard como funcionalidad aislada.

## 4. Contenido

H1 actual «Terrenario para quien gestiona varios terrenos». H2: ficha por parcela, kilos/rendimiento desglosados, costes registrados por terreno, FAQ. CTA acceso. Extensión 500–700 palabras.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Terreno es dimensión requerida del registro operativo | `docs/01-producto/vision-y-objetivos.md`, R1 | [x] |
| Dashboard desglosa kilos por terreno y costes según registros | `docs/03-modulos/produccion-y-dashboard/README.md`, Scope; diario README | [x] |
| Datos incompletos se marcan y no se estiman | `docs/03-modulos/produccion-y-dashboard/README.md`, Conceptos clave | [x] |

- **Excluir**: límites máximos no verificados, cartografía comparativa, optimización o recomendaciones automáticas.

## 6. Media

No hay media actual. Considerar captura solo con datos ficticios si mejora la comprensión.

## 7. Enlazado

La home y `gestion-terrenos` proporcionan entrada. Salientes a terrenos, dashboard y cosechas; conservar una relación contextual explicable.

## 8. Metadatos

Title actual «Terrenario para gestión multiterreno». Sin SEO social propio; distinguir mensaje por caso de uso y no repetir title genérico.

## 9. Distribución

Destino `/para/gestion-multiterreno`; canal/fecha de distribución se definen en MKT-109.

## 10. Cierre

- [ ] Diferenciar caso de uso, validar indicadores, revisar enlaces/meta y pasar tests/build.
