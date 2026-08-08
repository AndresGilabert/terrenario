---
id: "MVP-799"
tipo: feature
titulo: "TDD: Revision de cierre de la epica MVP-007"
estado: completado
tickets: []
epica: "MVP-007--ajustes-mvp-01"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["calidad", "gobernanza"]
  modulo_path: "03-modulos/"
  componentes: ["revision", "verificacion"]
  etiquetas: ["mvp", "ajustes", "revision"]
  nivel_riesgo: medio
creado_en: "2026-08-08"
actualizado_en: "2026-08-08"
---

# TDD: MVP-799 — Revisión de cierre de la épica MVP-007

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen

Las dieciséis historias están `completado` y mergeadas. Los seis criterios de la épica se cumplen,
verificados contra el sistema en marcha.

**El hallazgo con más filo no es de producto: es de proceso.** Al comprobar la trazabilidad, quince
filas del registro seguían diciendo «pendiente de crear historia» sobre puntos que ya estaban
construidos — `P-055` entre ellas, el punto cuya pérdida motivó el `CA-6` de esta épica.

## Veredicto por criterio (CA-1)

| CA | Veredicto | Evidencia |
|---|---|---|
| **CA-1** — Todas las historias en `completado` | ✅ | 16 de 16 en `completado`; los PR #73…#89 mergeados en `develop` |
| **CA-2** — Las cifras coinciden entre pantallas | ✅ | Contraste contra la API con datos reales, abajo |
| **CA-3** — Cambiar de contexto actualiza todas las vistas | ✅ | Cambio de campaña desde el shell con `navigation.length` en 1 |
| **CA-4** — Reportar una incidencia con contexto suficiente | ✅ | Correo entregado por SMTP real, 4.501 bytes, inspeccionado |
| **CA-5** — Reglas de negocio actualizadas | ✅ | Las cinco, comprobadas contra el producto y no solo contra su texto |
| **CA-6** — Ningún punto aprobado sin historia | ⚠️ **con salvedad** | Ninguno sin construir, pero **quince sin anotar**: `P-096` |

## CA-2 — El contraste numérico

El método es el que detectó `P-082`: contar en cada superficie y comparar. Con los datos que había,
sin embargo, **el contraste habría pasado sin tocar nada de lo que `MVP-707` y `MVP-708` construyeron**
— ninguna cosecha tenía precio y no existía ninguna imputación. Así que se creó el caso que sí puede
divergir y después se restauró el estado anterior.

Campaña 2026, todas las cifras del servidor y también sumadas fila a fila a mano:

| | diario | cosechas | compras | consumos | `summary` | `economics` | a mano |
|---|---|---|---|---|---|---|---|
| nº de cosechas | 4 | 4 | — | — | 4 | 4 | 4 |
| kg | 4460,5 | 4460,5 | — | — | 4460,5 | — | 4460,5 |
| nº de compras | 2 | — | 2 | — | — | — | — |
| coste de compras | — | — | 300,00 € | — | — | — | 300,00 € |
| nº de consumos | 1 | — | — | 1 | — | — | 1 |
| coste imputado | 2,50 € | — | — | 2,50 € | — | — | — |
| gasto | 390,00 € | — | — | — | — | 390,00 € | 300 compras + 90 mano de obra |
| ingresos | 949,20 € | — | — | — | — | 949,20 € | 949,20 € |
| cosechas con precio | 2 | — | — | — | — | 2 | 2 |

Dos lecturas que hay que hacer explícitas porque parecen discrepancias y no lo son:

- **390 € en el diario frente a 300 € en compras** no es una divergencia: el diario es el libro
  unificado e incluye la mano de obra de las actividades (90 €). El libro de compras solo cuenta
  compras.
- **El gasto no sube al imputar.** Con una imputación viva de 2,50 € sobre una compra de 250 € ya
  contada, el gasto sigue en 390 €. Contar las dos cosas sería duplicar, y es exactamente el riesgo que
  `MVP-707` evitó haciendo que el panel **pregunte al diario** en vez de recalcular.

## CA-3 — Cambio de contexto

Cambiar de campaña desde el shell deja diario, cosechas y panel mostrando la elegida, y
`performance.getEntriesByType('navigation').length` sigue valiendo **1**: no hubo recarga.

Un efecto de `MVP-704` que conviene registrar: **con un modal abierto el conmutador ni siquiera se
alcanza**. Está dentro del subárbol marcado como inerte, no puede recibir foco y un clic en sus
coordenadas aterriza en el velo. La parte más fea del agravante de `P-081` —cambiar de contexto con un
formulario a medias detrás— quedó directamente inalcanzable en vez de haber que resolverla.

También se comprobaron los dos estados de `MVP-703`: con una tarea pendiente en el catálogo, `/app`
muestra el checklist —que es lo correcto, porque queda algo que preparar— y en cuanto se completa,
lleva al diario.

## CA-4 — El canal de feedback, entregado de verdad

`MVP-711` dejó el envío real sin comprobar. Se levantó el receptor SMTP local y se envió un reporte con
el transporte de producción. Llegó: `multipart/alternative`, 4.501 bytes.

Lo que importa del contenido es lo que **no** lleva. Se envió a propósito una ruta con identificadores
de temporada y de terreno en la query y un fragmento:

```text
/app/compras?season_id=de851105-…&plot_ids=99802d87-…#fila-3
```

En el correo entregado:

| | |
|---|---|
| id de temporada | ausente |
| id de terreno | ausente |
| query completa | ausente |
| fragmento | ausente |
| nombre del Workspace | ausente |
| ruta recortada `/app/compras` | presente |
| `X-Request-Id` | presente |
| navegador | presente |
| versión desplegada | `1.0.0+b3a104d…`, el commit real de `develop` |

Sin buzón configurado, el canal responde `503 FEEDBACK_CHANNEL_UNAVAILABLE` en vez de tragarse el
reporte en silencio. Comprobado antes de configurarlo.

## CA-6 — La salvedad, y por qué es el hallazgo principal

Ningún punto aprobado se quedó sin construir. Pero **quince filas del registro decían que sí**:

`P-021`, `P-055`, `P-072`, `P-075`, `P-078`, `P-081`, `P-082`, `P-083`, `P-084`, `P-085`, `P-086`,
`P-087`, `P-090`, `P-091`, `P-092`.

Las quince con su historia de destino `completado` y mergeada. Se verificaron una a una —incluido
`P-021`, que apuntaba a `MVP-403`, de una épica anterior, y cuyo código está en `TemporadasView` con su
comentario— y ninguna era funcionalidad perdida. Solo faltaba cerrar la fila.

Cinco historias sí lo hicieron bien (`P-057`, `P-058`, `P-080`, `P-088`, `P-089`), así que el proceso
existe. Lo que no existe es nada que lo compruebe: **el registro depende de que alguien se acuerde**, y
eso es literalmente cómo se perdió `P-055`. Que `P-055` volviera a aparecer en esta lista —esta vez ya
construido, pero igual de mal anotado— es la prueba de que el `CA-6` de la épica no se cierra con
diligencia, sino con una comprobación. Es `P-096`.

## Los nueve derivados

Cada uno se verificó antes de registrarlo. **Tres resultaron mayores de lo que decía el reporte que los
originó**, que es el argumento para no cerrar una revisión sobre informes ajenos.

| | Hallazgo | Riesgo | Crecido al verificar |
|---|---|---|---|
| `P-096` | El registro de puntos no está respaldado por el gate | alto | — |
| `P-097` | 74 de 210 `.md` sin BOM; la regla no la comprueba nadie | medio | de «probablemente haya más» |
| `P-098` | 11 ficheros de `artifacts/correos` versionados contra el `.gitignore` | bajo | de 1 fichero |
| `P-099` | Arrancar con token guardado no programa el refresco de sesión | medio | — |
| `P-100` | `Email:SecurityMode` no reconocido cae a StartTLS en silencio | bajo | — |
| `P-101` | `formatDate` duplicado en 7 ficheros | bajo | de 3 vistas |
| `P-102` | `CS0649`: `_userId` declarado y nunca asignado en 2 clases de test | bajo | — |
| `P-103` | `MSB3277`: EF Core 9.0.1 gana a 9.0.18 en los tests | bajo | — |
| `P-104` | El drawer de móvil es el último overlay sin trampa de foco | bajo | — |

Los nueve van con destino **`por decidir`**: ninguno se asigna a una historia por iniciativa de la
revisión, que es como se empezó a perder `P-055`.

## Qué no se ha verificado

- **Nada en producción.** Todo lo de aquí es contra el entorno de desarrollo con la API compilada de
  `develop`. `Feedback__Recipient` sigue sin dar de alta en App Service, así que en producción el canal
  está visible y responde que no está disponible.
- **El aspecto** de las tres etiquetas de `MVP-708` y del aviso de fecha, y el `datalist` nativo al
  teclear. Su lógica sí está cubierta por tests de vista.
- **Los textos de `MVP-712`** en pantalla y el correo de invitación en un cliente real.
- **Un móvil real**: el icono tras «añadir al inicio» y el raspado de la tarjeta social de `MVP-710`
  siguen dependiendo del enlace publicado.

## Estado de los datos de desarrollo

Todo lo creado para verificar se ha deshecho: los precios de las dos partidas, la imputación, la tarea
del catálogo y el token de desarrollo de `public/`. La base queda como estaba.
