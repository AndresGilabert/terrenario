---
id: "MVP-705"
tipo: feature
titulo: "TDD: Navegacion del diario en la URL"
estado: completado
tickets: []
epica: "MVP-007--ajustes-mvp-01"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "filtros", "router"]
  etiquetas: ["mvp", "ajustes", "ux"]
  nivel_riesgo: bajo
creado_en: "2026-08-08"
actualizado_en: "2026-08-08"
---

# TDD: MVP-705 — Navegación del diario en la URL

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

No hay regla nueva: `RN-007` ya exigía conservar los filtros en la recarga y `MVP-405` la materializó
en la URL para el dashboard. Esta historia **aplica la decisión ya tomada a la vista que se quedó
fuera** y que más la necesita: el diario tiene cinco filtros y paginación.

Lo único con contenido es hacerlo sin romper dos cosas que `MVP-506` puso ahí por un fallo real:

| Lo que había que respetar | Cómo |
|---|---|
| El rebote de 350 ms de la búsqueda | Se conserva **tal cual**; lo que cambia es a dónde escribe |
| La guarda de respuestas obsoletas (`requestSeq`) | No se toca: sigue siendo la misma |

Y una decisión propia: **la búsqueda sustituye la entrada de historial y el resto la añade**. Es lo
que hace compatibles las dos mitades del `CA-3`: que «atrás» devuelva al estado anterior de filtros y
que teclear no genere una entrada por carácter.

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `frontend/.../lib/diary-url-state.ts` | nuevo | La URL como fuente única de los seis filtros y la página |
| `frontend/.../lib/diary-url-state.test.ts` | nuevo | Las dos invariantes: defectos fuera de la URL e historial sano |
| `frontend/.../lib/season-scope.ts` | modificado | Modo **controlado**, para que la elección de campaña viva en la URL sin duplicarse |
| `frontend/.../components/diary/DiarioView.tsx` | modificado | Consume la URL en vez de su propio estado |
| `docs/01-producto/reglas-de-negocio.md` (RN-007) | modificado | La regla deja de hablar solo del dashboard |

## Diseño detallado

### Qué vive en la URL y qué no

```text
/app/diario?type=…&plot_id=…&season_id=…&worker_id=…&search=…&page=…
```

Todo menos **lo que se está tecleando**. El término a medio escribir se queda en el componente: llevarlo
a la URL en cada pulsación llenaría el historial y dispararía una petición por letra, que es justo lo
que `MVP-506` resolvió con el rebote. A la URL llega el término **ya rebotado**, y de ahí sale el que
viaja al servidor.

Cuando la URL cambia por fuera —«atrás», «adelante», un enlace pegado— el cuadro de búsqueda tiene que
seguirla. Lo hace un efecto que compara con lo tecleado, para no pisar lo que se está escribiendo:
mientras se teclea, la URL todavía no ha cambiado.

### Los defectos no se escriben

Es la mitad de `CA-5` y lo que evita que la URL se llene de ruido: `todos`, la página 1 y la búsqueda
vacía **se borran** del parámetro en vez de escribirse.

La otra mitad es la temporada, y ahí el motivo es más fuerte que la estética: desde `MVP-701` el
defecto lo resuelve el servidor (RN-008). Escribirlo en la URL **congelaría la campaña de trabajo del
día en que se compartió el enlace**, así que quien lo abriera un año después vería la campaña vieja
creyendo que ve la suya.

### El historial

| Acción | Escritura | Por qué |
|---|---|---|
| Cambiar un filtro | `push` | «Atrás» tiene que deshacerlo (CA-3) |
| Cambiar de página | `push` | Igual: volver a la página anterior |
| Búsqueda ya rebotada | `replace` | Nueve pulsaciones no pueden ser nueve entradas |

Cualquier cambio de filtro **vuelve a la primera página**, y lo hace en la **misma escritura** que el
filtro. En un efecto aparte, React llegaría a pintar un estado intermedio —filtro nuevo, página vieja—
y saldría una petición de más cuya respuesta puede además llegar la última.

### El hook de temporada, en modo controlado

`useSeasonScope` (MVP-701) guardaba la elección en su propio estado. El diario necesita que viva en la
URL, así que el hook admite ahora un control externo. La alternativa —que la guardaran los dos— dejaría
dos copias de lo mismo que pueden divergir, que es el defecto que MVP-701 vino a arreglar.

Cosechas y compras siguen en modo no controlado: su elección no está en el alcance de esta historia.

## Alternativas descartadas

| Alternativa | Por qué no |
|---|---|
| Llevar también lo tecleado a la URL | Una entrada de historial y una petición por carácter |
| `push` en la búsqueda | «Atrás» tendría que pulsarse una vez por palabra buscada |
| Escribir los valores por defecto | URL ilegible, y la temporada quedaría congelada en un enlace compartido |
| Volver a la página 1 en un efecto aparte | Un render intermedio con filtro nuevo y página vieja, y una petición de más |
| Duplicar la elección de temporada en el hook y en la URL | Dos copias del mismo dato: el defecto que MVP-701 vino a arreglar |

## Riesgos e impacto

- Los enlaces al diario pasan a llevar estado. Es el objetivo; el efecto secundario es que una URL
  guardada en marcadores puede quedar apuntando a un terreno o a un responsable que ya no existe. El
  servidor lo tolera —un filtro obsoleto devuelve lista vacía, y la temporada ajena cae al defecto por
  MVP-701—, así que no rompe la pantalla.
- `useSeasonScope` gana un parámetro opcional. Las dos vistas que ya lo usaban no cambian.

## Plan de testing

| Nivel | Qué cubre |
|---|---|
| Unitario (`diary-url-state.test.ts`) | Cada filtro llega a la URL; los defectos no; una URL con filtros reproduce el estado; cambiar filtro vuelve a la página 1; página basura cae a la 1; **búsqueda `REPLACE` frente a filtro `PUSH`**; quitar filtros deja la URL limpia |
| Unitario (`DiarioView.test.tsx`) | Los 23 tests de `MVP-506` **siguen pasando sin tocarlos**: es la red que garantiza que el rebote y la guarda de respuestas obsoletas no se han roto |
| UI conducida | URL, historial y paginación sobre la aplicación en marcha |

## Verificación realizada

Sobre la aplicación en marcha, en el Workspace «Rafa»:

| Comprobación | Resultado |
|---|---|
| Entrada limpia | `/app/diario` sin ningún parámetro (CA-5) |
| Filtro de tipo | `?type=cosecha`, 4 registros |
| Filtro de terreno encadenado | `?type=cosecha&plot_id=…` |
| **9 pulsaciones en el buscador**, a 60 ms | **1 `replaceState`, 0 `pushState`** y una sola petición (CA-3, CA-4) |
| Cambio de filtro tras buscar | 1 `pushState` |
| «Atrás» | Vuelve a `?search=sulfatado`, y el cuadro de búsqueda recupera el término (CA-3) |
| URL pegada `?season_id=all&page=2` | «Página 2 de 2 · 36 registros», con el selector de campaña en «todas» (CA-2) |
| «Anterior» desde la página 2 | `?season_id=all` — la página 1 **no** se escribe (CA-5) |

> Nota de método: la primera medición dio un falso negativo —el buscador se quedaba a medias y no se
> disparaba nada—. No era la aplicación: mi script capturaba el elemento una sola vez y perdía la
> referencia cuando Vite recargó el módulo en caliente. Repetida buscando el elemento en cada
> pulsación, el resultado es el de la tabla.

## Checklist de implementación

- [x] Los seis filtros y la página, en la URL
- [x] Los valores por defecto no se escriben, incluida la temporada de MVP-701
- [x] La búsqueda sustituye la entrada de historial; filtros y página la añaden
- [x] Rebote de 350 ms y guarda de respuestas obsoletas intactos
- [x] `RN-007` deja de hablar solo del dashboard
- [x] 158 tests de frontend en verde, incluidos los 23 del diario sin tocar
