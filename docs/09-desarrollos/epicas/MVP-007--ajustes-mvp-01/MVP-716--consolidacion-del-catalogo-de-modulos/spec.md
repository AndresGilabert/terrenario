---
id: "MVP-716"
tipo: feature
titulo: "Consolidacion del catalogo de modulos"
estado: borrador
prioridad: baja
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["documentacion", "gobernanza"]
  modulo_path: "03-modulos/"
  componentes: ["kb", "modulos"]
  etiquetas: ["mvp", "ajustes", "documentacion"]
  nivel_riesgo: bajo
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-716 — Consolidacion del catalogo de modulos

> **Origen**: `P-020` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

`docs/03-modulos/` solo contiene `modulo-ejemplo`, que es una plantilla marcada para retirar al crear
el primer modulo real, y el catalogo de `_vision-general.md` sigue vacio.

El checklist de `MVP-201` difería a `MVP-203` documentar el modulo de Temporadas, pero `MVP-202`
(Terrenos) sento el precedente de **no** crear modulo por historia y usar el `tech-design.md` como
documentacion de modulo. Crear un unico modulo aislado seria incoherente y arrastraria retirar
`modulo-ejemplo` y poblar el catalogo.

Con el MVP entregado ya se sabe cuales son los modulos reales, asi que la consolidacion se puede hacer
de una vez y con conocimiento, en vez de por goteo.

## Objetivo

Que la KB describa los modulos que el producto tiene de verdad, y que deje de contener una plantilla
que lleva seis epicas marcada para borrar.

## Requisitos de usuario

### HU-1 — Encontrar la documentacion de un modulo

**Como** persona que se incorpora al proyecto,
**quiero** un catalogo de modulos que corresponda al codigo,
**para** orientarme sin reconstruirlo leyendo historias.

## Alcance (in-scope)

- Identificacion de los modulos reales entregados (identidad y Workspaces, maestros, diario y
  operativa, produccion y dashboard, observabilidad).
- Ficha por modulo en `docs/03-modulos/`, enlazando a los `tech-design.md` existentes en vez de
  duplicarlos.
- Catalogo y mapa de `_vision-general.md` poblados.
- Retirada de `modulo-ejemplo`.

## Fuera de alcance (out-of-scope)

- Reescribir los `tech-design.md` de las historias.
- Documentacion de modulos que no existan todavia.
- Cambios en el codigo.

## Criterios de aceptación

- [ ] **CA-1**: `docs/03-modulos/` contiene una ficha por modulo real, sin duplicar contenido ya escrito.
- [ ] **CA-2**: `_vision-general.md` tiene el catalogo y el mapa poblados.
- [ ] **CA-3**: `modulo-ejemplo` ya no existe y no queda ningun enlace roto a el.
- [ ] **CA-4**: El validador de la KB pasa en verde.

## Maquetas y referencias visuales

No aplica: documentacion.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| — | docs/00-meta/convenciones.md | falta | Solo existe `modulo-ejemplo` |

## Notas y decisiones

- Sin impacto funcional. Se adelanta a esta epica por decision del PO: es documentacion pura, sin
  riesgo, y retira de una vez una plantilla que lleva seis epicas marcada para borrar.
