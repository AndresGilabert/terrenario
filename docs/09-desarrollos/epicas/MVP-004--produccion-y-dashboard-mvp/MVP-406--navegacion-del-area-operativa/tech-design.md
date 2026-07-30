---
id: "MVP-406"
tipo: feature
titulo: "TDD: Navegación del área operativa"
estado: completado
tickets: []
epica: "MVP-004--produccion-y-dashboard-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "navegacion"]
  modulo_path: "03-modulos/"
  componentes: ["app-shell", "sidebar", "routing"]
  etiquetas: ["mvp", "ux", "navegacion", "deuda"]
  nivel_riesgo: bajo
creado_en: "2026-07-30"
actualizado_en: "2026-07-30"
---

# TDD: MVP-406 — Navegación del área operativa

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Deuda de fundación del shell (`P-016`), no funcionalidad nueva. Con los diez módulos del MVP
encendidos, se corrige la navegación en tres frentes que comparten superficie:

- **Menú agrupado** (CA-1, `P-025`): la lista plana de 10 entradas de `AppSidebar` pasa a tres secciones
  —**Operativa** / **Maestros** / **Configuración**— sobre el mismo shell.
- **Sección activa** (CA-2, `P-037`): cada entrada deja de ser `button` + `navigate()` y pasa a
  `NavLink`, que marca la activa visualmente y expone `aria-current="page"` sin estado a mano.
- **Ruta desconocida** (CA-3, `P-046`): nueva `NotFoundView`; `/app/*` deja de caer en el Home y `*`
  deja de redirigir en silencio a `/`.

Solo frontend; sin API, sin migración.

### Decisiones de diseño

- **La agrupación es por frecuencia de uso, no por entidad**: «Operativa» (lo que se hace a diario:
  Diario, Visión General, Cosechas, Compras), «Maestros» (los datos base: Terrenos, Temporadas,
  Trabajadores, Tareas, Miembros y accesos) y «Configuración» (Ajustes). Las diez entradas quedan en
  un grupo y solo uno (CA-1). Es la propuesta de partida del `P-025`.
- **La sección activa la resuelve `NavLink`, no estado propio.** `NavLink` ya compara la ruta y aplica
  la clase activa y `aria-current="page"`; reimplementarlo con `useLocation` sería duplicar lógica del
  router. El marcado activo es el verde de marca relleno, que también sirve de indicador accesible junto
  al `aria-current`. El mismo `AppSidebar` sirve al escritorio y al drawer móvil, así que CA-2 se cumple
  en ambos sin código aparte; el `onNavigate` que cierra el drawer se conserva en el `onClick` del
  `NavLink`.
- **El 404 bajo `/app` vive dentro del shell y fuera de la guarda de temporada.** Dentro del shell para
  no desorientar (se conserva el lateral y el contexto); **fuera** de `RequireSeasonOffer` porque un
  enlace roto no debe forzar a crear una campaña —era un efecto colateral de que el antiguo comodín
  `/app/*` colgara de esa guarda—. El título de la cabecera (`titleForPath`) pasa a decir «Página no
  encontrada» para esas rutas: solo el Home exacto (`/app`) y las desconocidas llegaban al defecto
  «Inicio», así que distinguirlos es seguro.
- **`NotFoundView` tiene dos variantes.** `embedded` (defecto) se pinta dentro del shell —que ya aporta
  cabecera y lateral—; `fullscreen` lleva su propio fondo para las rutas de `*`, que caen fuera del
  shell. La salida se decide por sesión (`useAuth`): al Home (`/app`) si hay sesión, a la landing (`/`)
  si no, en vez de mandar a todos a `/`.

## Arquitectura de la solución

```text
components/errors/NotFoundView.tsx   nuevo — 404 con salida según sesión (embedded | fullscreen)
components/layout/AppSidebar.tsx     NAV_SECTIONS agrupadas; NavLink + aria-current (fuera "Pronto", ya no aplica)
components/layout/AppLayout.tsx      titleForPath: «Página no encontrada» para la ruta desconocida
App.tsx                              /app/* → NotFoundView (dentro del shell, fuera de la guarda); * → NotFoundView fullscreen
```

## Estrategia de pruebas

Cambio de UI/enrutado sin lógica de dominio: se verifica **conducido** en el navegador.

**Verificación end-to-end conducida** (dev server + JWT de dev, Workspace «Rafa»):

- **CA-1** — el menú presenta tres grupos: Operativa (Diario, Visión General, Cosechas, Compras),
  Maestros (Terrenos, Temporadas, Trabajadores, Tareas, Miembros y accesos) y Configuración (Ajustes);
  las diez entradas quedan agrupadas, ninguna fuera.
- **CA-2** — en `/app/cosechas`, exactamente la entrada «Cosechas» lleva `aria-current="page"` y el
  marcado activo; al navegar, el marcado sigue a la ruta.
- **CA-3** — `/app/ruta-que-no-existe` muestra la 404 **dentro del shell** (lateral conservado, cabecera
  «Página no encontrada») sin redirigir ni forzar la oferta de temporada; `/ruta-inexistente` fuera de
  `/app` muestra la 404 a pantalla completa. La salida depende de la sesión: con sesión, «Volver al
  inicio» → `/app`; sin sesión, «Ir a la página principal» → `/`. Sin errores de consola.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Que el comodín `/app/*` capture rutas válidas | Se declara **después** de todas las rutas conocidas; React Router elige la más específica |
| Que el 404 bajo `/app` fuerce la oferta de temporada | Se saca de `RequireSeasonOffer`; solo el Home exacto sigue tras la guarda |
| Regresión de accesibilidad al cambiar `button`→`NavLink` | `NavLink` mantiene el rol de enlace y añade `aria-current`; verificado en el árbol de accesibilidad |
