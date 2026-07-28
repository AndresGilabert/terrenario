---
id: "MVP-406"
tipo: feature
titulo: "Navegación del área operativa: agrupación del menú, sección activa y ruta desconocida"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito D — Visibilidad operativa MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
depende_de: ["MVP-401", "MVP-403"]
bloquea: []
relacionado_con: ["MVP-299", "MVP-305"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "navegacion"]
  modulo_path: "03-modulos/"
  componentes: ["app-shell", "sidebar", "routing"]
  etiquetas: ["mvp", "ux", "navegacion", "deuda"]
  nivel_riesgo: bajo
creado_en: "2026-07-28"
actualizado_en: "2026-07-28"
---

# MVP-406 — Navegación del área operativa: agrupación del menú, sección activa y ruta desconocida

## Contexto

El shell de la aplicación (`AppSidebar` + `AppTopbar` + `AppLayout`) se entregó en `MVP-107` con los
módulos de `MVP-002`..`MVP-004` deshabilitados y etiquetados «Pronto». A medida que las épicas los han
ido encendiendo, la navegación ha acumulado tres deudas que se registraron por separado y que
comparten superficie:

- **`P-025`** — El menú es una **lista plana de 10 entradas** que mezcla maestros (Terrenos,
  Temporadas, Trabajadores, Tareas, Miembros y accesos) con operativa (Diario, Visión General,
  Cosechas, Compras) y Ajustes.
- **`P-037`** — No marca la **sección activa**: `AppSidebar` navega con `button` más `navigate()` en
  vez de `NavLink`, así que no hay estado seleccionado ni `aria-current`.
- **`P-046`** — No hay **pantalla de ruta desconocida**: `App.tsx` mapea `/app/*` a `HomeView` y el
  resto a `/`, de modo que un enlace roto o un error de tecleo renderiza el Home sin decir nada y la
  persona cree que ha llegado a donde quería.

El PO decidió (2026-07-28, 3ª pasada de `MVP-299`) resolverlas **aquí y no antes**: al cerrar esta
épica están encendidos los diez módulos, el menú alcanza su tamaño definitivo y la reestructuración se
hace una sola vez en lugar de retocarse en cada épica.

## Objetivo

Dejar la navegación del área operativa a la altura de una aplicación con todos sus módulos activos:
que se entienda de un vistazo dónde está cada cosa, dónde estoy yo y qué ha pasado cuando una ruta no
existe.

## Requisitos de usuario

### HU-1 — Encontrar las cosas en un menú que ya tiene diez entradas

**Como** usuario del área operativa,
**quiero** que el menú agrupe lo que hago a diario, los maestros y la configuración,
**para** no recorrer una lista plana cada vez que busco una sección.

### HU-2 — Saber en qué sección estoy

**Como** usuario que navega entre secciones,
**quiero** ver marcada la sección activa,
**para** orientarme sin releer el título de la página.

### HU-3 — Entender que una dirección no existe

**Como** usuario que llega por un enlace roto o se equivoca al teclear,
**quiero** que la aplicación me diga que esa dirección no existe y me devuelva a un sitio útil,
**para** no creer que he llegado donde quería.

## Alcance (in-scope)

- Agrupación del menú lateral por secciones sobre el shell existente, sin recrearlo. Propuesta de
  partida: «Operativa» / «Maestros» / «Configuración» (`P-025`).
- Marcado de la sección activa con `NavLink` y `aria-current`, accesible por teclado y lector de
  pantalla (`P-037`).
- Pantalla de ruta desconocida bajo `/app/*` y fuera de él, con salida clara al Home o a la landing
  según haya sesión o no (`P-046`).

## Fuera de alcance (out-of-scope)

- Rediseño del shell, de la topbar o del selector de Workspace.
- Búsqueda global, favoritos o navegación personalizable.
- Reordenar o renombrar los módulos del producto: solo se agrupan los que ya existen.
- Menú contextual por rol: los permisos del MVP son planos (RN-034).

## Criterios de aceptación

- [ ] **CA-1**: El menú lateral presenta los módulos agrupados por secciones con un criterio estable,
  y ninguna entrada queda fuera de grupo.
- [ ] **CA-2**: La sección en la que se encuentra el usuario aparece marcada visualmente y expuesta
  con `aria-current`, en escritorio y en el menú móvil.
- [ ] **CA-3**: Una ruta inexistente —bajo `/app` o fuera— muestra una pantalla que lo explica y
  ofrece una salida, en vez de renderizar el Home o redirigir en silencio.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB. El prototipo no contempla ni agrupación de menú ni pantalla de error.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| App / shell lateral | RN-034 | falta | El menú es una lista plana de 10 entradas sin agrupar (`P-025`) |
| App / shell lateral | docs/04-ingenieria/estandares-codigo.md | falta | Sin sección activa ni `aria-current` (`P-037`) |
| App / enrutado | — | falta | Sin pantalla de ruta desconocida: `/app/*` cae en el Home (`P-046`) |

## Notas y decisiones

- **Origen.** Consolida `P-025`, `P-037` y `P-046` de `MVP-999`, detectados durante `MVP-205` y la 2ª
  pasada de `MVP-299`. Los tres tocan la misma superficie y se registraron con la indicación de
  resolverlos en una sola pasada.
- **Por qué en `MVP-004` y no antes.** Decisión del PO (2026-07-28): hacerlo en `MVP-003`, con ocho
  entradas activas, obligaría a retocar el menú otra vez al encender Cosechas y Visión General.
  Después de esta épica no queda ningún módulo por encender en el MVP.
- **Deuda de fundación, no funcionalidad nueva.** No añade capacidades de producto: corrige la
  navegación que `P-016` dejó preparada para un número de módulos que ya se ha alcanzado.
