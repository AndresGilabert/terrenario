---
id: "MVP-402"
tipo: feature
titulo: "Reglas de producción, catálogo y destinos"
estado: completado
prioridad: critica
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-401"]
bloquea: ["MVP-403", "MVP-404", "MVP-405"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["produccion", "validaciones"]
  modulo_path: "03-modulos/"
  componentes: ["cosechas", "catalogos", "validaciones"]
  etiquetas: ["mvp", "produccion", "reglas"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-29"
---

# MVP-402 — Reglas de producción, catálogo y destinos

## Contexto

No basta con poder guardar cosechas: la producción del MVP debe seguir reglas cerradas para ser comparable. La KB fija catálogo global de productos, destino cerrado con `desconocido`, unidad canónica de rendimiento y exclusión mutua entre `rendimiento` y `litros`.

## Objetivo

Garantizar que las cosechas se registran bajo reglas homogéneas y compatibles con el cálculo posterior de KPIs del dashboard.

## Requisitos de usuario

### HU-1 — Registrar producción con reglas coherentes

**Como** usuario del Workspace,
**quiero** que la app valide producto, destino y rendimiento de forma consistente,
**para** evitar datos ambiguos o incompatibles entre sí.

### HU-2 — No bloquear una cosecha por falta de destino final

**Como** usuario operativo,
**quiero** poder usar `desconocido` como destino,
**para** no retrasar el registro cuando todavía no conozco el cierre comercial o de uso.

## Alcance (in-scope)

- Catálogo global fijo de productos de cosecha.
- Validación de producto obligatorio.
- Taxonomía cerrada de destinos, incluyendo `desconocido`.
- Regla XOR entre `rendimiento` y `litros`.
- Soporte de unidad canónica L/100kg y entradas equivalentes de rendimiento.

## Fuera de alcance (out-of-scope)

- Gestión de catálogo editable por usuarios.
- Fórmulas económicas derivadas de precio o balance.
- Reglas avanzadas de molturación o integración con almazaras.

## Criterios de aceptación

- [x] **CA-1**: El sistema exige producto válido, `kgs` y destino dentro del marco funcional del MVP.
- [x] **CA-2**: `rendimiento` y `litros` no pueden coexistir en la misma cosecha.
- [x] **CA-3**: `desconocido` se admite como destino válido sin degradar la consistencia posterior del dashboard.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/CosechaModal.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechaModal.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/CosechasView.tsx](../../../../../prototype/terrenario-mvp/src/components/CosechasView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| CosechaModal | RN-030, RN-012 | cubierto | Catalogos cerrados validados en servidor; `desconocido` admitido y rotulado «Sin destino» |
| CosechaModal | RN-004, RN-013, RN-014 | cubierto | Selector excluyente con los tres origenes de RN-014 y equivalente en L/100kg al escribir |
| CosechasView | RN-013 | cubierto | Rendimiento efectivo en unidad canonica, marcando lo derivado, y media ponderada por kilos |

## Notas y decisiones

- Esta historia cierra la semántica de producción del MVP antes de explotar datos en dashboard.
- **La unidad de entrada no se persiste: se convierte.** RN-013 fija L/100kg como canónica y RN-014
  admite además kg de aceite por 100 kg. Guardar la unidad de origen obligaría a cada consumidor del
  dato a convertir antes de comparar, y una sola omisión daría un promedio mal sin avisar. Se convierte
  una vez, con la densidad de RN-016, y lo persistido es siempre lo mismo.
- **El rendimiento «calculado» de RN-014 se expone como derivado, no como columna**: `effective_yield`
  junto a `yield_source`. Así una cosecha que declaró litros cuenta en los promedios sin contradecir la
  exclusión de RN-004, y la UI no presenta como declarado algo que se ha deducido.
- **`desconocido` es el canon; «Sin destino» es solo alias visual** (RN-012). El servidor rechaza el
  alias como valor: es la deriva que convertiría un catálogo cerrado en texto libre por acumulación.
- **Diferido con destino**: el override de densidad por almazara que contempla RN-016 queda en
  `MVP-999` (`P-061`); no existe la entidad almazara en el MVP.
