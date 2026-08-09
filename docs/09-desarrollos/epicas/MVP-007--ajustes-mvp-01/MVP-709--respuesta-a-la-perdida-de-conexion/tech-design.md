---
id: "MVP-709"
tipo: feature
titulo: "TDD: Respuesta a la perdida de conexion"
estado: completado
tickets: []
epica: "MVP-007--ajustes-mvp-01"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend", "resiliencia"]
  modulo_path: "03-modulos/"
  componentes: ["http-client", "auth", "avisos"]
  etiquetas: ["mvp", "ajustes", "ux", "campo"]
  nivel_riesgo: medio
creado_en: "2026-08-08"
actualizado_en: "2026-08-08"
---

# TDD: MVP-709 — Respuesta a la pérdida de conexión

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Tres piezas y un hallazgo que resultó ser lo más importante de la historia.

| Pieza | Dónde | Qué hace |
|---|---|---|
| `fetchVigilado` + `NetworkError` | `services/http-client.ts` | Distingue «no hubo respuesta» de «el servidor dijo que no» |
| Estado de conexión | `lib/connectivity.ts` | Dos fuentes: los eventos del navegador y lo que le pasa a cada petición |
| Aviso persistente | `components/layout/OfflineBanner.tsx` | Lo dice y lo retira solo |
| **La sesión no se cierra por un corte** | `contexts/AuthContext.tsx` | El hallazgo: ver abajo |

## `NetworkError` hereda de `HttpError`, y no es un atajo

`fetch` solo rechaza cuando la petición **no llega a tener respuesta**: sin red, DNS que no resuelve,
TLS que falla, CORS que bloquea. Cualquier respuesta del servidor —incluido un 500— resuelve. Esa
frontera es exactamente la del `CA-1`, así que la detección es un `try` alrededor del `fetch` y nada
más.

Lo que sí es una decisión es **de qué hereda el error**. Toda la aplicación está escrita como:

```ts
setLoadError(error instanceof HttpError ? error.message : 'No se pudieron cargar las cosechas.');
```

Con una clase aparte, cada una de esas decenas de pantallas caería en su texto genérico —justo el «no
se pudieron cargar los datos» indistinguible de un fallo del servidor que el `CA-1` prohíbe— y habría
que repasarlas una a una, con la certeza de olvidar alguna. Heredando de `HttpError`, el mensaje
correcto aparece en todas a la vez, y quien quiera distinguirlo tiene `esFalloDeRed()`.

El `status` es `0`: es lo que vale «no hubo respuesta», y ningún código HTTP lo representa sin mentir.

**La cancelación no es un corte.** Un `AbortError` se deja pasar tal cual: abortar al cambiar de
pantalla es lo normal, y confundirlo con falta de cobertura pintaría el aviso cada vez que alguien
navega rápido.

## Por qué no basta `navigator.onLine`

Solo sabe si hay *interfaz de red*, no si se llega al servidor. En el campo el caso normal es el
contrario del que detecta: el móvil sigue enganchado a una antena con una barra, `navigator.onLine`
dice `true` y las peticiones mueren igual. Su `false` es fiable; su `true` no significa nada.

Por eso el estado tiene **dos fuentes** y manda la de los hechos:

1. Los eventos `online`/`offline`, que llegan solos, sin coste y **antes** de que nada falle.
2. Lo que le pasa a cada petición: `fetchVigilado` marca la caída cuando una muere sin respuesta y la
   recuperación cuando otra vuelve con una —aunque sea un 500: eso demuestra que hay conexión, que es
   lo único que este estado mide—.

El mensaje distingue los dos casos porque la acción del usuario es distinta: si el móvil dice que no
hay red, toca buscar cobertura; si dice que sí y aun así no se llega, el problema puede estar al otro
lado y reintentar tiene sentido.

## El hallazgo: un corte de cobertura cerraba la sesión

Lo que la historia pedía era «avisar y no perder lo escrito». Al mirar el camino de red apareció algo
peor de lo que el punto describía: **había tres sitios donde una red caída se leía como sesión
inválida**, y cerrar la sesión se lleva por delante todo lo tecleado, que es justo lo que el `HU-2`
quiere evitar.

| Sitio | Antes | Ahora |
|---|---|---|
| Refresco programado (`scheduleRefresh`) | `catch { LOGOUT }` — se dispara solo cada cuarto de hora largo; si saltaba sin cobertura, fuera | Solo cierra si el servidor **responde** que no vale; si fue la red, reintenta a los 30 s |
| `getAccessToken` | `catch { return null }`, y el cliente HTTP traduce `null` a «cerrar sesión» | Propaga el fallo de red: la pantalla dice «sin conexión» y la sesión sigue |
| Arranque con token guardado | `catch { borrar token }` | No se borra si fue la red: no se ha podido comprobar si vale, que no es lo mismo que saber que no vale |

Es seguro: el `refresh_token` es una cookie de larga duración y sigue siendo válido cuando vuelva la
cobertura. Y la contrapartida está cubierta con su propio test — si el servidor responde que la sesión
no vale, se cierra igual que siempre.

**Cuidado con el reintento.** La primera versión reprogramaba con `scheduleRefresh(60)`, pero su
retardo es `caducidad − 60 s`: cualquier valor por debajo del minuto da cero y el reintento se
convierte en un bucle que machaca la red mientras no la haya. Por eso el reintento lleva su propio
retardo explícito.

## El aviso

Persistente y **no descartable**: no es una notificación que se lee y se cierra, es el estado en el
que está la aplicación. Poder quitarlo solo serviría para volver a intentar a ciegas.

Va bajo la cabecera y **fuera del área desplazable**: si fuera dentro, bastaría con bajar por la lista
para dejar de ver que no hay conexión.

`role="status"` y no `alert`: interrumpir a quien está tecleando una labor para decirle que no hay
cobertura empeora el momento en vez de ayudarlo.

Y dice explícitamente **lo que no pasa**: que lo escrito sigue en pantalla. Sin esa frase, lo razonable
al leer «sin conexión» en una aplicación online-first es dar el trabajo por perdido y cerrar.

## `ADR-0002` sigue vigente (CA-4)

Aquí no hay cola de salida, ni reintento automático de operaciones, ni almacenamiento local de nada
que el usuario haya escrito, ni resolución de conflictos. Solo se **sabe** si hay conexión para poder
decirlo, y se evita tirar una sesión por un corte. El único reintento automático es el del refresco de
sesión, que no es una operación del usuario y no crea ni modifica datos.

Operar sin conexión sigue siendo `Hito H`.

## Verificación

20 tests nuevos: 5 del estado de conexión, 6 del cliente HTTP —incluida la cancelación, que no debe
confundirse con un corte—, 4 de la sesión y 5 del aviso. Suite completa **204 en verde**.

La prueba de la sesión se comprobó **rompiéndola a propósito** (sustituyendo la guarda por `false`)
para verificar que falla sin el arreglo: lo hace. Y el escenario se monta pasando por `login()`, no
por el arranque con token guardado, porque **ese camino no programa refresco** y la prueba no habría
llegado a disparar el temporizador.

Con la conexión cortada de verdad —parando el proceso de la API, no simulando el error— y el
formulario de cosecha abierto con 1847 kg escritos:

| Comprobación | Resultado |
|---|---|
| El formulario sigue abierto | sí |
| Los 1847 kg siguen ahí | sí |
| Mensaje en el formulario | «No se ha podido contactar con el servidor. Comprueba la cobertura y vuelve a intentarlo.» |
| Aviso global | «Sin conexión. … Lo que hayas escrito sigue en pantalla…» |
| «Guardar» vuelve a estar disponible | sí |
| La sesión sobrevive | sí; se sigue en `/app/cosechas`, sin pantalla de acceso |
