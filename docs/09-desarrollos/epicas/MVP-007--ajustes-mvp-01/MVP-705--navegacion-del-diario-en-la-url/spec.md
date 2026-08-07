---
id: "MVP-705"
tipo: feature
titulo: "Navegacion del diario en la URL"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: ["MVP-701"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "filtros", "router"]
  etiquetas: ["mvp", "ajustes", "ux"]
  nivel_riesgo: bajo
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-705 — Navegacion del diario en la URL

> **Origen**: `P-072` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

Cambiar de pagina, buscar o filtrar por responsable en `/app/diario` deja la URL sin rastro. La misma
accion en Vision General produce `/app/vision-general?season_id=...`, porque `VisionGeneralView` usa
`useSearchParams` como fuente unica y asi se decidio expresamente en `MVP-405`.

No es un defecto de `MVP-506` —sus criterios no lo pedian— sino **una decision ya tomada que no se
aplico a la vista que mas la necesita**: el diario tiene cinco filtros y paginacion, asi que es donde
mas duele no poder compartir un enlace ni volver a la pagina 3 despues de abrir un registro.

## Objetivo

Que el estado de navegacion del diario viva en la URL, igual que el del dashboard, y que un enlace al
diario reproduzca exactamente lo que veia quien lo comparte.

## Requisitos de usuario

### HU-1 — Volver donde estaba

**Como** titular de la explotacion,
**quiero** que al volver atras el diario conserve filtros y pagina,
**para** no rehacer la busqueda cada vez que abro un registro.

## Alcance (in-scope)

- `type`, `plot_id`, `season_id`, `worker_id`, `search` y `page` como fuente unica en la URL.
- Convivencia con el rebote de 350 ms de la busqueda y con la guarda de respuestas obsoletas que
  `MVP-506` anadio por un fallo real (una respuesta vieja dejaba el muro vacio).
- Control del historial del navegador: no puede generarse una entrada por pulsacion de tecla.
- Coherencia con el defecto de temporada que fija `MVP-701`: el valor por defecto no ensucia la URL.

## Fuera de alcance (out-of-scope)

- Anadir filtros nuevos al diario.
- Persistir filtros entre sesiones o por usuario.

## Criterios de aceptación

- [ ] **CA-1**: Aplicar cualquiera de los seis filtros o cambiar de pagina se refleja en la URL.
- [ ] **CA-2**: Pegar esa URL en otra pestana reproduce la misma vista.
- [ ] **CA-3**: El boton «atras» del navegador devuelve al estado anterior de filtros, y escribir en la
  busqueda no genera una entrada de historial por caracter.
- [ ] **CA-4**: La busqueda sigue esperando 350 ms antes de disparar la peticion y una respuesta
  obsoleta sigue sin poder vaciar el muro.
- [ ] **CA-5**: Los valores por defecto (incluida la temporada de trabajo de `MVP-701`) no aparecen en
  la URL.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/DiarioView.tsx](../../../../../prototype/terrenario-mvp/src/components/DiarioView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| DiarioView | RN-007 (conservacion de filtros en recarga) | falta | Hoy solo lo cumple el dashboard |

## Notas y decisiones

- `RN-007` ya exige conservar filtros en recarga. Esta historia no crea regla nueva: aplica la que ya
  estaba a la vista que se quedo fuera.
