---
id: "MVP-803"
tipo: mejora
titulo: "TDD: Cosechas en movil y tableta"
estado: completado
tickets: []
epica: "MVP-008--ajustes-mvp-02"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "accesibilidad"]
  modulo_path: "03-modulos/"
  componentes: ["produccion", "compras-consumo", "shell"]
  etiquetas: ["mvp", "ajustes", "responsive", "movil"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# TDD: MVP-803 — Cosechas en móvil y tableta

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Dos cambios, y el segundo se amplió al medirlo:

1. **El punto de corte del lateral pasa de `md:` a `lg:`** (decisión del PO). En el tramo de tableta se
   usa el menú desplegable que ya existe para móvil, y el contenido recupera la pantalla entera.
2. **Maqueta de tarjeta** por debajo de `lg:` para las listas que no caben. El `spec` la pedía para
   Cosechas dando por hecho que Compras «ya tenía maqueta adaptada»; **no era cierto**, y se amplió con
   la medida delante (decisión del PO, 2026-08-10).

| Vista | Tabla | Ancho a 375 px | ¿Se arrastraba? |
|---|---|---|---|
| Cosechas | 8 columnas, 897 px | contenedor de 341 px | sí |
| Compras (libro) | 8 columnas, 881 px | contenedor de 341 px | **sí** |
| Compras (consumos) | 6 columnas | contenedor de 341 px | sí |
| Diario | tarjetas desde siempre | — | no |
| Terrenos, Miembros | tarjetas | — | no (0 desbordes medidos) |

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `frontend/.../common/RecordCard.tsx` | nuevo | La tarjeta de un registro operativo y su lista |
| `frontend/.../lib/use-media-query.ts` | modificado | `useIsWide()` — el corte de `lg:`, distinto del de `useIsDesktop()` |
| `frontend/.../layout/AppLayout.tsx` | modificado | El lateral aparece en `lg:` en vez de en `md:` |
| `frontend/.../layout/AppTopbar.tsx` | modificado | El botón de menú se ve hasta `lg:` |
| `frontend/.../layout/MobileNavDrawer.tsx` | modificado | El cajón está disponible hasta `lg:` |
| `frontend/.../harvests/CosechasView.tsx` | modificado | Tarjetas por debajo de `lg:`; acciones extraídas |
| `frontend/.../purchases/ComprasView.tsx` | modificado | Lo mismo en sus **dos** listas |
| Tests de las dos vistas | modificado | Casos con `matchMedia` a 375 px |

## Diseño detallado

### Por qué mover el punto de corte no bastaba

`P-095` se registró como un problema del corte del lateral: a 768 px exactos aparecía **justo en ese
ancho** y se llevaba 256, de modo que al contenido le quedaban 448. La medida que cambia el diagnóstico
es la de 375 px: **la tabla mide 897 px dentro de un contenedor de 341**, y ahí el lateral ni siquiera
está. Mover el corte no arregla eso.

Tratarlo como «poner Cosechas a la altura de las demás listas» resuelve los dos anchos de una vez, que
es lo que hace esta historia.

### Dos cortes distintos, y por qué no son el mismo

`useIsDesktop()` (640 px, `sm:`) separa «móvil» de «lo demás» y lo usa `MobileDisclosure` para plegar
controles. `useIsWide()` (1024 px, `lg:`) separa «cabe una tabla ancha» de «no cabe». **A 768 px las
dos respuestas son distintas**, y confundirlas es exactamente lo que dejaba la tabla de Cosechas en 448
px de sitio.

### Un árbol, no dos ocultos con CSS

La elección se hace con una media query en JavaScript y no con `hidden lg:block`, siguiendo el criterio
que `MobileDisclosure` dejó escrito: pintar los dos árboles metería en el DOM **dos juegos de botones
por registro**, con la misma etiqueta accesible cada uno. Aquí son hasta tres botones por compra.

Por el mismo motivo las acciones se extraen a `HarvestActions`, `PurchaseActions` y
`ConsumptionActions`: la tabla y la tarjeta comparten **el mismo** componente, así que la etiqueta
accesible no puede divergir entre maquetas. Es lo que `CA-4` comprueba.

### La tarjeta

La jerarquía es la de la lectura, no la de la tabla: de qué es el registro, cuándo, la cifra que manda
en grande a la derecha, el resto en dos columnas **con su rótulo** —sin cabecera de tabla, un número
suelto no dice de qué es— y las acciones al pie.

Es una sola pieza (`RecordCard`) y no una por vista, por el mismo motivo que `list-url-state` en
`MVP-802`: lo que se busca es precisamente que las cuatro listas se lean igual.

## Medidas antes y después

Workspace «Rafa», Cosechas con la campaña 2026 (4 partidas), medido en el navegador.

| Ancho | Antes | Después |
|---|---|---|
| **375** | tabla de 897 px en contenedor de 341, con arrastre horizontal | 4 tarjetas, **0 desbordes** en el documento |
| **768** | lateral visible (256 px), contenido útil **448 px**, tabla arrastrándose | lateral plegado en el menú, contenido útil **689 px**, tarjetas |
| **1024** | contenido útil 689 px, tabla de 897 con arrastre | **igual** |
| **1440** | contenido útil 1120 px, tabla de 1118, sin arrastre | **igual** |

A 768 px la ganancia medida es de **+241 px**, no los +256 del lateral: los 15 px de diferencia son la
barra de desplazamiento vertical que aparece al alargarse la página con las tarjetas. Se aporta la
cifra medida, no la teórica.

Lo que **no** cambia a 375 px es el desbordamiento propio de la tira de resumen (`SummaryStrip`), que
es un carrusel deliberado de `MVP-702` y se comporta igual en el diario: medido en las dos pantallas,
`scrollWidth` 776 en Cosechas y 1532 en el diario, con `overflow-x: auto` propio. El documento no se
desplaza en ninguna de las dos.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Solo mover el punto de corte del lateral | A 375 px el lateral no está y la tabla igualmente no cabe. Es el replanteamiento que documenta el `spec` |
| Plegar el lateral a iconos en el tramo de tableta | Daría ~192 px y conservaría la navegación visible, pero obliga a resolver rótulos al pasar el ratón y diez entradas en tres secciones sin texto, y añade un tercer estado del shell |
| Pintar tabla y tarjetas y ocultar una con CSS | Dos juegos de botones por registro en el DOM, con la misma etiqueta accesible. Es el criterio de `MobileDisclosure` |
| Quitar columnas de la tabla en anchos pequeños | `CA-1` pide que **toda** la información sea legible sin desplazar; una tabla con columnas escondidas pierde datos sin decirlo |
| Dejar Compras fuera, como decía el `spec` | Su premisa era falsa: medido, se arrastra igual que Cosechas. Cerrar la épica arreglando la mitad de un defecto idéntico |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| El tramo 768–1023 pierde la navegación visible | media | El menú desplegable sigue alcanzable; comprobado que el botón se ve a 768 y que el lateral no ocupa |
| La tarjeta pierde información respecto a la tabla | media | Test que comprueba que la tarjeta contiene terreno, fecha, campaña, kilos, producto, rendimiento con su origen, importe y destino |
| Las etiquetas accesibles divergen entre maquetas | baja | Las acciones son **el mismo** componente en las dos; `CA-4` lo comprueba |
| A 1024 px cambia algo respecto a `MVP-702` | baja | Medido antes y después: mismo ancho útil, misma tabla, mismo lateral |

## Plan de testing

- [x] Tests de componente (Cosechas): por debajo del corte no se pinta tabla, la tarjeta conserva toda
  la información de la partida —incluido el rendimiento derivado marcado como tal— y las acciones
  mantienen su etiqueta accesible
- [x] Tests de componente (Compras): las **dos** listas pasan a tarjetas, la tarjeta de compra conserva
  el reparto por terrenos de `MVP-304`, y los tres botones de la compra y los dos del consumo conservan
  su etiqueta
- [x] Verificación en navegador de los cuatro anchos de referencia, con las cifras de arriba
- [x] Regresión: los 296 tests del cliente en verde, incluidos los que dan por hecho la tabla en
  escritorio (el doble de `matchMedia` declara 1280 px)

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Migraciones de base de datos preparadas — no aplica
- [x] Tests escritos y pasando
- [x] Documentación de API actualizada — no aplica: no cambia ningún contrato
- [x] Módulo afectado actualizado en `docs/03-modulos/` — es maqueta, no comportamiento: no hay regla
  que actualizar
- [x] Sin `TODO` sin resolver en este documento
