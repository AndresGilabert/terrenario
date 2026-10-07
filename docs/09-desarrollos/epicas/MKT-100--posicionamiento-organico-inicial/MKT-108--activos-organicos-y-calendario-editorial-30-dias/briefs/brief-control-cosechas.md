---
bloque: "09-desarrollos"
documento: "brief-control-cosechas"
actualizado_en: "2026-10-07"
landing:
  slug: "control-cosechas"
  path: "/funcionalidades/control-cosechas"
  cluster: "funcionalidad"
  estado: publicada
  tipo_trabajo: "revision"
seo:
  keyword_principal: "control de cosecha de olivar"
  keywords_secundarias: ["rendimiento de aceite de oliva por cosecha", "registro de cosecha de aceituna"]
  intencion: "comercial"
  canibaliza_con: ["gestion-de-cosechas"]
enlazado:
  entrantes: ["gestion-terrenos", "diario-de-campo", "dashboard-campana"]
  salientes: ["dashboard-campana", "gestion-terrenos", "diario-de-campo", "gestion-de-cosechas"]
media: []
historia: "MKT-108--activos-organicos-y-calendario-editorial-30-dias"
responsable: "@andres"
revisores: []
creado_en: "2026-10-07"
---

# Brief de landing — Control de cosechas

## 1. Por qué existe

- **Problema**: el productor de olivar quiere ordenar kilos, destino y rendimiento por parcela/campaña y evaluar si Terrenario cubre ese flujo.
- **Momento / persona**: evaluación; propietario de olivar personal o familiar.
- **Misión de posicionamiento**: captar intención comercial de control de cosecha de olivar y rendimiento de aceite. La guía separada cubrirá cómo capturarla paso a paso.

## 2. Objetivo de posicionamiento

- **Objetivo**: consolidar esta página como landing comercial del subtema cosecha de olivar y reforzar pilar general.
- **Métrica / horizonte**: impresiones/clics de consultas asignadas y sesiones propias; revisar a 90 días frente al baseline MKT-107, no fijar ranking sin datos.
- **Rol**: landing funcional de apoyo; pilar `software-gestion-agricola`.

## 3. Keywords

| Keyword | Tipo | Intención | Cobertura |
| --- | --- | --- | --- |
| control de cosecha de olivar | principal | comercial | H1/meta |
| rendimiento de aceite de oliva por cosecha | secundaria | comercial | H2/FAQ |
| registro de cosecha de aceituna | secundaria | comercial | bullets |

- **No atacar**: «cómo registrar una cosecha» (guía informacional `/guias/gestion-de-cosechas`).

## 4. Contenido

H1 actual: «Controla tu cosecha de olivar, terreno a terreno». H2: captura y datos; unidad L/100 kg; desglose por terreno/destino; histórico disponible; FAQ. CTA: empezar registro. Extensión: 700–900 palabras con desarrollo único; corregir fallback que repite bullets en el bloque funcionalidades.

## 5. Verificación factual

| Afirmación | Fuente | Verificada |
| --- | --- | --- |
| Kilos obligatorios; rendimiento y litros excluyentes | `docs/03-modulos/produccion-y-dashboard/README.md`, Scope; RN-004/014/016 | [x] |
| Unidad canónica: litros por 100 kg; incompletos se marcan | misma ficha, Conceptos clave (RN-013) | [x] |
| El destino pertenece a catálogo cerrado | misma ficha, Scope (RN-012/030) | [x] |

- **Excluir**: molturación, precio de mercado, predicción, cálculo agronómico o consejo de cosecha.

## 6. Media

Actualmente sin media en el catálogo. Capturas futuras deben usar datos sintéticos y pasar revisión de privacidad; recursos siempre same-origin.

## 7. Enlazado

Enlaces contextuales desde terrenos, diario y dashboard. Añadir enlace a guía al estar implementada; no contar enlace global de portada como relación contextual.

## 8. Metadatos

SEO social propio ya definido. Mantener title/description centrados en producto y distinguirlos de la guía; 3 FAQ actuales propias, revisar factualidad.

## 9. Distribución

Web: `/funcionalidades/control-cosechas`; pieza de distribución y canal/fecha a registrar en MKT-109.

## 10. Cierre

- [ ] Corregir repetición, enlazar guía, revisar datos estructurados/meta y validar tests/build.
