---
id: "MVP-202"
tipo: feature
titulo: "Maestro de terrenos con alta mínima"
estado: borrador
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
actualizado_en: "2026-07-20"
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

- [ ] **CA-1**: Un usuario puede crear un terreno con `nombre` y `tipo_propiedad` como único mínimo obligatorio.
- [ ] **CA-2**: Los campos opcionales del terreno pueden añadirse o editarse después sin impedir el uso del terreno en el MVP.
- [ ] **CA-3**: Los terrenos con uso histórico pueden inactivarse sin romper la integridad de los registros asociados.

## Maquetas y referencias visuales

- Referencia funcional: RU-01 y contratos API de terrenos.

## Notas y decisiones

- La ausencia de `num_arboles` se tratará después como dato incompleto en dashboard; aquí no debe bloquear nada.