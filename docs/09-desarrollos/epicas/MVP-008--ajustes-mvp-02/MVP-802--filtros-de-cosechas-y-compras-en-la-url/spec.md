---
id: "MVP-802"
tipo: feature
titulo: "Filtros de cosechas y compras en la URL"
estado: completado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: ["MVP-801"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "frontend"]
  modulo_path: "03-modulos/"
  componentes: ["produccion", "compras-consumo", "navegacion"]
  etiquetas: ["mvp", "ajustes", "filtros", "url"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-802 — Filtros de cosechas y compras en la URL

> **Origen**: `P-109` del registro de `MVP-999`, detectado en la segunda revision completa del MVP
> (2026-08-10).

## Contexto

`RN-007` dice que «la recarga mantiene los filtros activos del usuario», y se materializo llevandolos a
la URL en dos vistas: el dashboard (`MVP-405`) y el diario (`MVP-705`, tras `P-072`, «es donde mas
duele»). Las otras dos vistas operativas se quedaron fuera y su estado vive en memoria.

Verificado en el navegador: filtrar Cosechas por «Campana 2026» pasa la tabla de 1 a 4 filas y la URL
sigue siendo `/app/cosechas`; al recargar vuelve a la campana de trabajo. Cosechas tiene **tres**
filtros —terreno, temporada y destino— y Compras los suyos, asi que no es el caso de un unico control
que se pueda rehacer de memoria.

El efecto practico es doble: se pierde el trabajo de filtrar al recargar o al volver de otra pantalla,
y no hay forma de enviar a nadie «mira estas partidas». Dos de las cuatro vistas operativas se
comportan de una manera y dos de otra ante la misma accion del usuario.

## Objetivo

Que las cuatro vistas operativas conserven y compartan sus filtros de la misma forma, con la misma
pieza y las mismas dos condiciones de higiene que ya rigen en el diario.

## Requisitos de usuario

### HU-1 — No perder el filtro al recargar

**Como** titular de la explotacion,
**quiero** que recargar Cosechas o Compras me deje donde estaba,
**para** no volver a montar el filtro cada vez.

### HU-2 — Poder enviar lo que estoy viendo

**Como** titular de la explotacion,
**quiero** que la direccion reproduzca lo que tengo filtrado,
**para** poder guardarlo o compartirlo con quien colabora conmigo.

## Alcance (in-scope)

- Filtros de **Cosechas** en la URL: `?plot_id=…&season_id=…&destination=…`.
- Filtros de **Compras** en la URL, incluidos los de sus consumos.
- Reutilizacion de la pieza que `MVP-705` dejo montada para el diario, no una copia por vista: la
  leccion de `P-082` es que un defecto duplicado acaba divergiendo.
- Las dos condiciones de higiene de `RN-007`, ya vigentes en el diario: **los valores por defecto no se
  escriben** (ni «todos», ni la pagina 1, ni la temporada por defecto, que la resuelve el servidor) y
  la busqueda rebotada **sustituye** la entrada de historial en vez de anadir una por caracter.
- Actualizacion de `RN-007` para que su lista de modulos afectados incluya las cuatro vistas.

## Fuera de alcance (out-of-scope)

- Cambiar los filtros disponibles, anadir nuevos o alterar su comportamiento de consulta.
- Persistir los filtros entre sesiones o entre dispositivos.
- Llevar a la URL el estado de las vistas de maestros: no son vistas operativas y `RN-007` no las
  nombra.

## Criterios de aceptación

- [x] **CA-1**: Filtrar Cosechas por terreno, temporada o destino escribe el filtro en la URL, y
  recargar la pagina mantiene exactamente la misma lista.
  **Evidencia** (navegador conducido, Workspace «Rafa»): elegir «Campaña 2026» pasa la tabla de **1 a 4
  filas** —el escenario exacto de `P-109`— y la direccion pasa a
  `/app/cosechas?season_id=de851105…`. Recargar con `?season_id=…&destination=aceite_para_venta` deja
  1 fila con los dos controles ya posicionados.
- [x] **CA-2**: Lo mismo en Compras y en su bloque de consumos.
  **Evidencia**: `/app/compras?season_id=all&product=Abono` recarga con el buscador relleno y el
  selector en «Todas las temporadas»; el test de componente comprueba que **las dos** peticiones —libro
  y consumos— reciben `season_id` y `product`.
- [x] **CA-3**: Un enlace copiado de cualquiera de las dos vistas reproduce en otra sesion la misma
  seleccion de filtros.
  **Evidencia**: es el mismo mecanismo que `CA-1`, comprobado navegando a la direccion desde cero en
  vez de recargando. Los tests piden al servidor exactamente lo que trae la direccion.
- [x] **CA-4**: La URL **no** contiene los valores por defecto: sin filtros explicitos, la direccion
  queda limpia.
  **Evidencia**: `/app/cosechas` sin parametros muestra la campana de trabajo y la direccion sigue
  limpia; volver un filtro a «todos» borra su parametro. Fijado con test.
- [x] **CA-5**: Con un `season_id` de otro Workspace en la URL, las dos vistas siguen cayendo al
  defecto y mostrando en el control la campana aplicada, sin reintroducir `P-108`.
  **Evidencia**: `/app/cosechas?season_id=<de otro Workspace>` deja el control en «Campana 2025» y la
  direccion en `/app/cosechas`. Hay un test equivalente en cada una de las dos vistas, y el doble de
  cliente HTTP aplica `RN-008` como el servidor para que la prueba no pase por casualidad.
- [x] **CA-6**: `RN-007` describe el comportamiento de las cuatro vistas operativas.
  **Evidencia**: `RN-007` lista los parametros de las cuatro, dice que los consumos comparten los del
  libro, y recoge que la mecanica vive en **una sola pieza** y por que.

## Notas y decisiones

- Va **despues de `MVP-801`**, nunca antes: llevar el filtro a la URL es justo el mecanismo que expone
  `P-107`/`P-108`, asi que hacerlo primero propagaria el defecto a dos vistas mas. `CA-5` existe para
  fijar esa dependencia con una comprobacion y no con una recomendacion.
- La alternativa que se descarta es **acotar `RN-007`** a dashboard y diario y cerrar el tema. Se
  descarta porque el enunciado de la regla es general y porque el usuario no tiene forma de saber que
  dos pantallas recuerdan y dos no.
