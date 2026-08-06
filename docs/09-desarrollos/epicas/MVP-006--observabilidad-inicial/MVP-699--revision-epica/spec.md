---
id: "MVP-699"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: media
sprint: ""
hito: "Hito F — Operación medible"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-006--observabilidad-inicial"
depende_de: ["MVP-601", "MVP-602", "MVP-603"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "scope-control"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "qa", "stabilization"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-08-06"
---

# MVP-699 — Revision epica

## Contexto

Durante la ejecucion de una epica aparecen ajustes, puntos ciegos y necesidades no previstas en las historias originales. Si no se centralizan antes del cierre, se dispersan y se pierde trazabilidad para decidir el trabajo posterior.

## Objetivo

Ejecutar una revision final de la epica para validar el funcionamiento global, consolidar los pendientes detectados y convertirlos en nuevas historias planificables.

## Requisitos de usuario

### HU-1 — Consolidar pendientes de la epica

**Como** Product Owner,
**quiero** reunir en un solo punto los ajustes y requisitos detectados durante la epica,
**para** evitar omisiones y cerrar el alcance con trazabilidad.

### HU-2 — Verificar calidad funcional final

**Como** equipo de producto y desarrollo,
**quiero** revisar el estado final de la epica sobre el flujo integrado,
**para** abrir nuevas historias concretas con evidencias de error o falta.

## Alcance (in-scope)

- Revision integral del comportamiento entregado por la epica.
- Consolidacion de puntos ciegos y requisitos pendientes detectados durante las historias previas.
- Creacion de nuevas historias para cubrir errores, faltas o ajustes detectados.
- Priorizacion inicial de los nuevos items segun impacto funcional y de negocio.

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los nuevos cambios detectados.
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [ ] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
- [ ] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
- [ ] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique.

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.

---

## Resultado de la revisión (2026-08-06)

### Cómo se hizo

Verificación sobre el sistema en marcha, no releyendo la KB: API real, base de datos real, consulta de
los endpoints publicados y rastreo de la traza generada por una sesión autenticada. Cada hallazgo se
numera `R-xx` y dice si se corrigió aquí o se derivó.

### Qué se verificó, y con qué evidencia

| Qué | Cómo | Resultado |
|---|---|---|
| Estado de las historias | Frontmatter de los tres `spec.md` | `MVP-601`, `MVP-602` y `MVP-603` en `completado` |
| Visibilidad de las señales | Contraste de las métricas almacenadas contra la respuesta de `/ops/signals` | No están solo en base de datos, pero faltaba la serie temporal (`R-01`) |
| Interfaz | `grep` sobre el cliente | **Ninguna pantalla** consume las señales; derivado (`P-074`) |
| Rama de error del embudo | `POST /auth/google/callback` con código inválido contra la API real | `login_google_error` emitido con las seis dimensiones y su `error_code` |
| Ausencia de PII | Rastreo de nombre, correo, JWT, `Bearer` e identificadores sobre la traza de una sesión autenticada | **Cero coincidencias** en líneas de telemetría; la única dirección del log va enmascarada (`a***@gmail.com`) |
| Dilución del SLO | Una hora de sonda (60) más 8 peticiones de negocio, contando el divisor real | La sonda era el **87 %** del divisor (`R-03`) |
| Alertas de extremo a extremo | Parada del contenedor de PostgreSQL en `MVP-603`, y un 500 real en esta pasada | `ServiceDown` y `HighErrorRate` disparadas y resueltas, con correo enviado |

### Hallazgos

#### `R-01` · La observabilidad no tiene serie temporal: solo ventanas fijas — **corregido aquí**

**Pregunta de partida del PO**: ¿la observabilidad del embudo y del resto de parámetros es visible de
algún modo, o solo consultando la base de datos?

**Qué se comprobó**, contrastando las métricas realmente almacenadas contra la respuesta del endpoint:

- **No está solo en la base de datos**: `GET /api/v1/ops/signals` publica los tres SLO, el embudo de 7
  días, el uso del producto, el monitoreo de negocio mínimo y el estado de las cinco alertas. Hay
  además logs estructurados y aviso por correo.
- **No hay ninguna interfaz**: confirmado con `grep`, ninguna pantalla del cliente consume estas
  señales. Es coherente con el «N/A en fase C» de la tabla de dashboards de la KB.
- **Siete cosas eran solo consultables por SQL**, y una de ellas pesa mucho más que las otras seis: el
  informe solo ofrecía **ventanas fijas** (7 días, 30 días y 30 minutos). Ni un dato por día.

**Por qué importa**: se podía ver que la conversión de la semana es del 82 %, pero no si la semana
anterior fue 90 % o 70 %, ni qué día cayó. Y `kpis.md` declara todos los objetivos «pendientes de
baseline», con las primeras cuatro semanas destinadas a fijarlo — que es exactamente lo que no se
puede hacer sin comparar semanas. Los datos estaban en la tabla; lo que faltaba era exponerlos.

**Corrección aplicada**: `GET /api/v1/ops/signals?days=N` devuelve una **serie por día** (28 días por
defecto, acotada a 1..400) con conversión, uso del dashboard, cobertura de widgets, tasa de error,
P95, altas y minutos observados de cada día. Los días sin datos se emiten con recuentos a `0` y
cocientes a `null`: omitirlos escondería que ese día no se observó nada.

El parámetro **no mueve las ventanas de los SLO**: esas las define la KB y son parte del objetivo, no
una preferencia de consulta. Hay un test que lo fija.

**Evidencia**: sembrando dos días en la base y consultando `?days=10` contra la API real, la serie
muestra la caída de 90 % (día -8) a 60 % (día -1) y los días sin datos como `null`, mientras
`error_rate_7d` sigue ignorando el día -8 por quedar fuera de su ventana.

El resto del desglose que sigue siendo solo-SQL (qué widget falla, qué código de error de Google, qué
recurso se crea, historial de alertas, visitas frente a sesiones, histograma de latencia) se deriva a
`MVP-999` (`P-073`), y la pantalla de operación a `P-074`, por decisión del PO.

#### `R-02` · La señal de «sesión activa» nunca se emite en onboarding — **derivado**

`TelemetryController` documenta que no exige Workspace activo *«a propósito: una sesión en onboarding
también es una sesión activa, y dejarla fuera del divisor subiría el KPI»*. Pero `app_session_started`
se emite desde `AppLayout`, que está anidado dentro de `RequireWorkspace`, y esa guarda redirige a
`/onboarding` cuando no hay Workspace.

El endpoint lo permite y el cliente no lo manda nunca: **el divisor sigue excluyendo justo lo que se
dijo que incluía**. El KPI mide «de las sesiones que llegaron al área operativa, cuántas abrieron el
panel», que es una pregunta más estrecha y más favorable que la que declara la KB.

No se corrige aquí porque la salida correcta depende de qué se decida que es una «sesión activa», y eso
es producto, no implementación. Derivado a `MVP-999` (`P-078`).

#### `R-03` · La sonda de salud diluía el SLO hasta desactivar la alerta — **corregido aquí**

`RequestMetricsMiddleware` contaba **toda** petición a `/api`, incluida la sonda de salud del
alojamiento y la ingesta de telemetría. Medido en esta pasada: una hora de sonda más ocho peticiones de
negocio dejaba la sonda en el **87 % del divisor**.

La consecuencia es aritmética y grave. Con tráfico realista —1440 sondas al día frente a 200
peticiones reales—:

```text
5 % de fallo real  ->  10 errores / 1640 peticiones = 0,61 %
umbral de HighErrorRate: 1 %  ->  NO SALTA
```

Un producto fallando una de cada veinte veces no habría disparado la alerta crítica. La latencia sufría
lo mismo: 67 de 69 muestras caían en el cubo de 50 ms porque la sonda es trivial, así que el P95
publicado era el de la sonda, no el del producto.

**Corrección**: la sonda de salud, la consulta de señales y la ingesta de telemetría salen del SLO. No
se descartan —se cuentan en `api.internal.*` y se publican en el informe—, porque un recorte del
divisor que no se ve se acaba leyendo como si ese tráfico nunca hubiera existido.

**Evidencia tras la corrección**: el mismo experimento deja `api.requests = 8` (el tráfico real) y
`api.internal.requests = 66`. Con 200 peticiones y un 5 % de fallo, la alerta ahora sí salta.

#### `R-04` · Un código de Google caducado devuelve 500 y dispara una alerta crítica — **derivado**

Cualquier respuesta no exitosa del endpoint de token de Google se mapea a
`AUTH_GOOGLE_EXCHANGE_FAILED` → **HTTP 500**, incluido `invalid_grant`, que es lo que Google devuelve
ante un código ya usado o expirado. Recargar la pantalla de callback basta para provocarlo.

Verificado: un solo 500 de este tipo sobre 70 peticiones dio 1,43 % y disparó `HighErrorRate`
—**crítica**— con envío de correo real.

Es comportamiento anterior a esta épica, pero **solo tiene consecuencia desde `MVP-603`**: antes no lo
miraba nadie. Se deriva porque la corrección está en autenticación, fuera del alcance de una épica de
observabilidad. `MVP-999` (`P-079`).

#### `R-05` · Desarrollar en local enviaba correos de alerta reales — **corregido aquí**

`Ops:AlertsEnabled` venía a `true` también en desarrollo. Con cuenta de envío y destinatario
configurados en la máquina de trabajo, cualquier error transitorio mientras se programa acababa en un
correo de alerta. Pasó durante esta misma revisión.

**Corrección**: `appsettings.Development.json` apaga la vigilancia. Las alertas se prueban con sus
tests, no dejándolas sueltas en local.

### Veredicto por criterio de la épica

| Criterio | Veredicto | Sustento |
|---|---|---|
| **CA-1** — Todas las historias en `completado` | ✅ | `MVP-601`, `MVP-602` y `MVP-603` verificados en frontmatter |
| **CA-2** — Medir embudo y uso de forma trazable, sin PII en claro | ✅ | Las cinco etapas del embudo y las cuatro señales de uso se emiten y se agregan; rastreo de PII sobre la traza real sin ninguna coincidencia. Con la salvedad de `R-02`, que estrecha el divisor de un KPI sin invalidar la trazabilidad |
| **CA-3** — Alertas o señales equivalentes para detectar degradaciones | ✅ **tras corregir `R-03`** | Antes de la corrección el criterio estaba cumplido en la forma —las cinco alertas existen y disparan— pero la principal no podía saltar con tráfico realista. Con el divisor arreglado, sí |

### Lo que esta revisión deja dicho para la siguiente

- La observabilidad **no tiene interfaz**, y es una decisión consciente alineada con el «N/A en fase C»
  de la KB, no un olvido (`P-074`).
- **Un proceso muerto no se vigila a sí mismo**: la caída total depende de una sonda externa que hoy
  reinicia pero no avisa (`P-077`).
- Las dos primeras semanas de datos reales servirán para saber si los volúmenes mínimos de alerta (20
  peticiones, 10 pantallas) son los adecuados para el tráfico que tenga el producto.
