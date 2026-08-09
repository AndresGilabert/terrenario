---
id: "MVP-714"
tipo: feature
titulo: "Higiene de datos: retencion de sesiones y secretos en el repositorio"
estado: completado
prioridad: baja
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["privacidad", "cumplimiento", "seguridad"]
  modulo_path: "03-modulos/"
  componentes: ["retencion", "refresh-tokens", "repositorio"]
  etiquetas: ["mvp", "ajustes", "cumplimiento"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-714 — Higiene de datos: retencion de sesiones y secretos en el repositorio

> **Origen**: `P-071` y `P-076` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-071`** — Los refresh tokens revocados o caducados no tienen plazo de expurgo. La tabla de
retencion de `privacidad-datos.md` dice «hasta caducidad o revocacion», pero una vez caducado o
revocado el hash se queda en `refresh_tokens` indefinidamente: solo se limpia si se purga la cuenta
entera. No se incluyo en la rutina de `RN-041` **a proposito**, porque esa regla enumera cinco
categorias y esta no es una: anadirla es una decision de producto.

**Decision del PO (2026-08-06)**: se anade con **plazo corto** —dias o semanas tras la caducidad—, no
los 24 meses del resto, que para un dato de sesion es conservador de mas.

**`P-076`** — Una direccion de correo personal del titular esta versionada en
`prototype/terrenario-mvp/src/data/initialData.ts`, en un repositorio **publico** y en el historial de
git. **Decision del PO**: sustituirla por una direccion de ejemplo y aceptar que el historial la
conserva. Reescribir el historial invalidaria clones, tags y referencias existentes —incluidos los tags
de release desde los que se despliega— y no recuperaria lo ya copiado.

## Objetivo

Cerrar los dos restos de higiene que quedaron abiertos tras el gate de salida, sin abrir ninguno nuevo.

## Requisitos de usuario

### HU-1 — Que todo lo que se conserva tenga plazo

**Como** responsable de cumplimiento,
**quiero** que ninguna categoria de dato quede sin plazo de expurgo,
**para** que `RN-041` signifique lo que dice.

## Alcance (in-scope)

- Nueva categoria en `RN-041` para tokens de refresco revocados o caducados, con plazo corto y motivo
  escrito.
- Linea correspondiente en `RetentionPurgeService`.
- Actualizacion de la tabla de retencion de `docs/07-seguridad/privacidad-datos.md`.
- Sustitucion de la direccion personal en `initialData.ts` por una de ejemplo, con nota de que el
  historial la conserva.

## Fuera de alcance (out-of-scope)

- Reescritura del historial de git: descartada por el PO.
- Revisar el resto de plazos de `RN-041`, ya cerrados en `MVP-505`.
- Barrido general de secretos en el repositorio.

## Criterios de aceptación

- [x] **CA-1**: `RN-041` incluye la categoria de tokens de refresco con su plazo y su motivo. Sexta
  categoria: **30 dias** desde la revocacion o la caducidad, **lo primero que ocurra**. El motivo
  escrito es doble: es un dato de sesion y no historico operativo, y es la categoria que mas filas
  genera (la rotacion crea una por cada refresco). Y la eleccion de 30 y no menos tambien esta
  argumentada: es el mismo orden que la vida del propio token —«un token muerto no dura mas de lo que
  habria durado vivo»— y deja cuatro ciclos de la revision operativa semanal para investigar una
  sesion sospechosa. El plazo vive tambien en codigo
  (`AccountRetentionPolicy.RefreshTokenRetentionDays`), no solo en la regla.
- [x] **CA-2**: La rutina de expurgo la aplica, verificado con datos sembrados y no solo por lectura del
  codigo. Tres tests de integracion contra **PostgreSQL real** en `RetentionPurgeTests`: uno siembra
  las cuatro situaciones a la vez —caducado hace 31 dias, revocado hace 31 dias, muerto hace una
  semana y **sesion viva**— y comprueba en una sola pasada que se van los dos primeros y se quedan los
  dos ultimos; otro fija la frontera exacta del dia 30 (una hora antes se purga, una hora despues no);
  el tercero cubre la purga conjunta de cuenta y token. Comprobado ademas **por mutacion**: anulando el
  plazo (`tokenCutoff = now`) fallan 2 tests, y con el predicado siempre cierto fallan los mismos 2.
  Suite completa en verde: 844 pruebas, 0 fallos.
- [x] **CA-3**: `privacidad-datos.md` refleja el plazo nuevo. Fila propia en la tabla general
  («Hasta caducidad o revocacion, **mas 30 dias**»), fila en la tabla de `RN-041` con la columna «desde
  cuando cuenta», y un apartado que explica por que la sesion tiene un plazo distinto del resto. De
  paso se retira una nota caducada que afirmaba que la rutina seguia esperando una programacion
  periodica de infraestructura: `MVP-504` (`B-3`) la entrego como `RetentionPurgeWorker`. Y se corrige
  la misma frase donde tambien estaba: el tratamiento `T6` de `checklist-cumplimiento-mvp.md` decia
  «hasta caducidad o revocacion», que es literalmente lo que `P-071` senalaba.
- [x] **CA-4**: `initialData.ts` no contiene ninguna direccion personal real, y queda anotado que el
  historial de git si la conserva. `userEmail` pasa a `juan.perez@ejemplo.test` —coherente con el
  `userName: 'Juan Pérez'` de al lado y sobre un TLD reservado (RFC 2606), no registrable ni
  entregable—. El comentario de cabecera dice que estos datos son de maqueta y ninguno puede ser real,
  que el historial conserva la original, y por que se acepta: reescribirlo invalidaria clones, tags y
  referencias de release sin recuperar lo ya copiado.
- [x] **CA-5**: Ninguna otra direccion personal real aparece en la copia de trabajo del repositorio.
  Barrido por patron de correo sobre `src/`, `prototype/`, `docs/` e `infra/`, **reevaluado desde cero**
  y sin heredar clasificaciones anteriores: 31 direcciones distintas, todas de ejemplo, de servicio
  (`no-reply@terrenario.com`) o marcadores (`tu-cuenta@gmail.com`), salvo dos. La de `initialData.ts`
  queda sustituida. La otra, `hola@andresgilabert.dev` en `legal-entity.ts` y tres documentos, **se
  conserva a proposito**: es el contacto del responsable del tratamiento donde se ejercen los derechos
  de los arts. 15-22, que la LSSI (art. 10) y el RGPD (art. 13) obligan a publicar; ya es publica en la
  Politica de Privacidad, esta versionada con ese motivo escrito y es sobreescribible por
  `VITE_LEGAL_PRIVACY_EMAIL`. Retirarla no la haria menos publica y dejaria el documento legal
  incompleto.

## Maquetas y referencias visuales

No aplica: cumplimiento y saneamiento de datos.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| initialData del prototipo | RN-017, RN-041 | hecho | `userEmail` es `juan.perez@ejemplo.test`; barrido de correos sobre `src/`, `prototype/`, `docs/` e `infra/` sin ninguna direccion personal filtrada |

## Notas y decisiones

- La eleccion conservadora aqui es **la sustitucion**, no la reescritura: el dato lleva meses expuesto,
  reescribir no lo recupera y si rompe las referencias desde las que se despliega.
- **Los 30 dias son una decision de producto pendiente de confirmar por el PO.** El spec dejaba el
  plazo en «dias o semanas»; se elige el extremo alto de ese rango porque por debajo se pierde la
  unica utilidad que justifica conservar la fila —poder mirar una sesion sospechosa en la revision
  operativa semanal—, y porque atarlo a la vida del propio token hace la regla legible sin consultar
  ninguna tabla.
- **`P-071` daba por sabido algo que era falso**: afirmaba que purgar la cuenta arrastraba los tokens
  «por cascada». `refresh_tokens` **no tiene FK** hacia `users` —verificado en la migracion
  `InitialAuth`, en el snapshot del modelo y en el `modelBuilder`—, asi que las filas quedaban
  huerfanas indefinidamente. El problema no era que tuvieran plazo largo: era que no tenian ninguno.
  Queda corregido en `RN-041`, en `privacidad-datos.md` y en el comentario del servicio.
- **No se anade la FK.** Seria el arreglo estructural, pero exige migracion sobre una tabla caliente
  para conseguir a los 24 meses lo que el plazo nuevo consigue a los 30 dias: al cerrar una cuenta se
  revocan todos sus tokens, asi que un mes despues no queda ninguno.
- **`hola@andresgilabert.dev` se queda.** Es real, pero es publicacion obligatoria por LSSI art. 10 y
  RGPD art. 13, no una filtracion. La evaluacion se rehizo desde cero en vez de heredarla.
- Aviso operativo: **la primera pasada en produccion borrara casi toda la tabla `refresh_tokens`**
  (todo lo muerto desde `MVP-101`). Es el objetivo, va en una transaccion con cerrojo, y el informe de
  esa ejecucion dara un numero grande que no es un incidente.
