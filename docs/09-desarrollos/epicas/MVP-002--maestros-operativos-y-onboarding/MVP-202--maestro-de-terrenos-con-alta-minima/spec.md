---
id: "MVP-202"
tipo: feature
titulo: "Maestro de terrenos con alta mínima"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito B — Base operativa preparada"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-002--maestros-operativos-y-onboarding"
depende_de: ["MVP-201"]
bloquea: ["MVP-003", "MVP-004"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["terrenos"]
  modulo_path: "03-modulos/"
  componentes: ["terrenos"]
  etiquetas: ["mvp", "masters", "terrenos"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-07-28"
---

# MVP-202 — Maestro de terrenos con alta mínima

## Contexto

Todo registro operativo del MVP depende de un terreno. La KB ya cerró que el alta debe ser ligera: `nombre` y `tipo_propiedad` obligatorios, con el resto de campos opcionales e informativos.

## Objetivo

Permitir crear, editar, listar e inactivar terrenos del Workspace con la mínima fricción necesaria para empezar a registrar actividad.

## Requisitos de usuario

### HU-1 — Crear un terreno rápidamente

**Como** usuario del Workspace,
**quiero** dar de alta un terreno con los datos mínimos,
**para** empezar a asociar registros operativos sin esperar a completar toda la ficha.

### HU-2 — Mantener información adicional opcional

**Como** usuario que quiere más contexto,
**quiero** poder guardar datos opcionales del terreno,
**para** enriquecer la explotación sin bloquear el alta inicial.

## Alcance (in-scope)

- Alta de terrenos con `nombre` y `tipo_propiedad` obligatorios.
- Edición y listado de terrenos del Workspace.
- Soporte de alias, propietario, referencia catastral, ubicación y `num_arboles` como datos opcionales.
- Inactivación de terrenos con histórico en lugar de borrado lógico agresivo en el maestro.

## Fuera de alcance (out-of-scope)

- Mapas, coordenadas funcionales o geolocalización avanzada.
- Validación fuerte de referencia catastral.
- Historización de `num_arboles` por temporada.

## Criterios de aceptación

- [x] **CA-1**: Un usuario puede crear un terreno con `nombre` y `tipo_propiedad` como único mínimo obligatorio. _(`POST /api/v1/plots` con solo `name`+`ownership_type` → 201; verificado por API y UI conducida. `tipo_propiedad` es el catálogo cerrado `plot_ownership_type` = `{propia, cedida}`.)_
- [x] **CA-2**: Los campos opcionales del terreno pueden añadirse o editarse después sin impedir el uso del terreno en el MVP. _(Campos opcionales; `PATCH` parcial que no borra los campos omitidos; añadir `tree_count` después retira el aviso de dato incompleto. Verificado en BD.)_
- [x] **CA-3**: Los terrenos con uso histórico pueden inactivarse sin romper la integridad de los registros asociados. _(`is_active` reversible vía `PATCH { is_active:false }`, sin borrado; listado con filtro de estado. Verificado por API y UI.)_

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/TerrenosView.tsx](../../../../../prototype/terrenario-mvp/src/components/TerrenosView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TerrenoModal.tsx](../../../../../prototype/terrenario-mvp/src/components/TerrenoModal.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx](../../../../../prototype/terrenario-mvp/src/components/TerrenoDetailModal.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| TerrenoModal (`PlotFormModal`) | RN-028 | cubierto | Alta/edición con solo `name`+`ownership_type` obligatorios; opcionales bajo aviso |
| TerrenosView (`TerrenosView`) | RN-001, RN-028 | cubierto | Listado, búsqueda, filtro de inactivos, aviso de dato incompleto (RN-010) |
| TerrenoDetailModal | RN-001 | diferido | El detalle con histórico (cosechas/labores) depende de MVP-003/004 (MVP-999, P-019) |

## Notas y decisiones

- La ausencia de `num_arboles` (`tree_count`) se trata como dato incompleto en dashboard (RN-010); aquí no bloquea. La respuesta expone `has_tree_count` para que la UI lo marque.
- **`tipo_propiedad`** se cierra como catálogo `plot_ownership_type` = `{propia, cedida}` (decisión con PO), formalizado en `contratos-api.md`.
- **Divergencias con el ER canónico** (corregidas y documentadas): se añade `is_active` (no estaba); `location` en texto libre en vez de `latitude`/`longitude`; `soil_metadata` diferido. Registrado en MVP-999 (P-019).
- Esta historia estrena el **cliente HTTP común** con manejo centralizado de 401/403 de scope, cerrando la deuda P-007/P-018 de MVP-999 (recursos _scoped_: terrenos y temporadas).
- Detalle técnico de la implementación: [tech-design.md](./tech-design.md).
