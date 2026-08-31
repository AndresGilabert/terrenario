---
bloque: 05-infraestructura
documento: observabilidad
actualizado_en: "2026-08-31"
---

# Observabilidad — Monitoring, Alertas y SLOs

---

## Stack de observabilidad

| Herramienta | Propósito | Estado |
|------------|-----------|--------|
| Logs estructurados + request-id | Diagnóstico base de errores y trazabilidad | implementado |
| Telemetría propia (`MVP-601`/`602`/`603`) | Embudo de login, uso del panel, señales operativas en `/api/v1/ops/signals` y alertas por correo | implementado |
| Métricas operativas básicas | Disponibilidad, 5xx y latencia p95 | implementado |
| Analítica web de terceros (Google Analytics o equivalente) | Medición de adquisición web | **postergada** (ADR-0011) |
| Sentry | Error tracking de aplicación | **no implementado** (ADR-0008 §3, `P-129`) |

> **Sentry no está montado** y no hay cuenta ni dependencia: lo declaró ADR-0008 en fase C y nunca se
> llegó a integrar, porque la observabilidad del MVP se construyó a medida en la épica `MVP-006`. Se
> deja en la tabla —marcado— en vez de borrarlo, porque sigue siendo la herramienta objetivo si en
> fase A se decide externalizar el error tracking. Ver la nota de realidad de ADR-0008.

Estado fase A: stack liviano y coste controlado.
Escalado a stack completo: al entrar en fase B o antes si hay 2 meses seguidos con alertas críticas recurrentes.

---

## SLOs por servicio

### Servicio principal / API

| SLI | SLO | Ventana de medición |
|-----|-----|---------------------|
| Disponibilidad | 99.9% | Rolling 30 días |
| Latencia P95 | < 300ms | Rolling 7 días |
| Tasa de error (5xx) | < 0.1% | Rolling 7 días |

### Embudo de autenticacion (MVP)

| SLI | SLO | Ventana de medición |
|-----|-----|---------------------|
| Conversion login (pantalla -> exito) | >= 85% | Rolling 7 dias |
| Tasa de abandono login | <= 15% | Rolling 7 dias |
| Tiempo medio de login exitoso | <= 45s | Rolling 7 dias |

---

## Alertas activas

| Alerta | Condición | Severidad | Canal | Runbook |
|--------|-----------|-----------|-------|---------|
| `HighErrorRate` | Tasa 5xx > 1 % durante 30 min | critica | canal de incidentes | `runbooks/revision-operativa.md` |
| `HighLatency` | P95 > 500 ms durante 30 min | warning | canal de incidentes | `runbooks/revision-operativa.md` |
| `ServiceDown` | Health check falla > 1min | critica | canal de incidentes | `runbooks/revision-operativa.md` |
| `LoginAbandonmentSpike` | Abandono login > 25% durante 30min | 🟠 alta | canal privado interno de incidentes | `../08-procesos/gestion-incidentes.md` |
| `LoginSuccessDrop` | Conversion login < 70% durante 30min | 🟠 alta | canal privado interno de incidentes | `../08-procesos/gestion-incidentes.md` |

### Como estan implementadas (MVP-603)

Una **vigilancia dentro de la propia aplicacion**, cada minuto, sobre la ventana de 30 minutos que
piden las condiciones de arriba. Mismo patron que el expurgo de `RN-041` y el volcado de telemetria:
viaja con la aplicacion y no anade infraestructura que hoy no existe. Con el tamano de equipo actual,
una plataforma de alertado seria desproporcionada.

Cuando una alerta **cambia de estado** se emite `alert.fired` (o `alert.resolved`) como traza
estructurada y, si hay destinatario configurado (`Ops__AlertEmail`), tambien un correo por el
transporte SMTP que ya existe. Se avisa **solo en la transicion**: una degradacion de dos horas
mandaria ciento veinte avisos identicos y el canal dejaria de leerse justo cuando hace falta.

**Volumen minimo antes de juzgar**: 20 peticiones para la tasa de error y la latencia, 10 pantallas de
acceso para el embudo. Sin esto, una madrugada con tres peticiones y un 500 daria un 33 % de error, y
una alerta que salta sin motivo se acaba ignorando tambien cuando el motivo es real.

### Que cuenta como fallo del servicio (MVP-713)

El numerador de la tasa de error son las respuestas **5xx**, asi que **clasificar bien un error es
decidir si mueve el SLO**. No es un detalle de contrato: es la definicion de la medida.

`MVP-713` (`P-079`) corrige el caso que lo puso en evidencia. `POST /auth/google/callback` traducia
**cualquier** respuesta no exitosa del endpoint de token de Google a `AUTH_GOOGLE_EXCHANGE_FAILED` →
500, incluido `invalid_grant`, que es lo que Google devuelve ante un codigo ya usado o caducado:
recargar la pantalla de vuelta bastaba para provocarlo. Medido en la revision de `MVP-699` (`R-04`):
un solo 500 de este tipo sobre 70 peticiones dio **1,43 %** y disparo `HighErrorRate` —critica— con
envio de correo real.

Desde `MVP-713` la respuesta depende de **de quien es el error**, siguiendo el vocabulario cerrado de
OAuth 2.0 (la tabla completa esta en `../02-arquitectura/contratos-api.md`, §0.c bis):

| Caso | HTTP | ¿Cuenta en la tasa de error? |
|---|---|---|
| Codigo ya usado o caducado (`invalid_grant`) | 401 | No. Sigue en el **divisor**: la peticion se sirvio |
| Peticion incompleta (`invalid_request`) | 400 | No |
| Configuracion nuestra (`invalid_client`, `unauthorized_client`) | 500 | **Si** |
| Caida de Google o respuesta ilegible | 500 | **Si** |

El defecto va hacia el 500: lo que no se puede atribuir con certeza a quien llama se sigue contando
como fallo propio. Es la misma direccion que `R-03` —donde se saco del SLO lo que no era trafico de
nadie, pero **contandolo aparte** en `api.internal.*`— y la contraria de la que parece comoda: un
fallo propio contado como error de cliente desaparece de las alertas, y una alerta ciega es peor que
una ruidosa.

El **nivel de log** se clasifica igual. Un codigo caducado se registra como `Information` y solo el
fallo propio como `Warning`/`Error`: el canal por el que se diagnostican las averias no puede estar
lleno de gente recargando una pantalla.

Lo que **no** cambia es la traza del embudo: el intento sigue emitiendo `login_google_error` con su
`error_code`, asi que la conversion de login sigue viendo el fallo. Son dos preguntas distintas —«¿ha
podido entrar la gente?» y «¿esta roto el servicio?»— y solo la segunda es la que gobierna
`HighErrorRate`.

**Punto ciego, declarado**: un proceso muerto no se vigila a si mismo. Dentro de la aplicacion,
`ServiceDown` cubre la degradacion observable —la base de datos inalcanzable, que deja el producto
inservible aunque el proceso viva—. La **caida total** la detecta la sonda externa de la plataforma
contra `GET /api/v1/health`, configurada en `infra/azure/configurar-api.sh`. Por eso
`healthy_minutes_30d` se llama asi y no `uptime`: cuenta minutos **observados**, y los minutos en los
que no habia nadie observando no aparecen.

## Comprobacion de salud

`GET /api/v1/health` (anonima) responde `200` con `{"status":"healthy"}` o **`503`** con
`{"status":"degraded"}` cuando no alcanza la base de datos. Es `503` y no `200` con un cuerpo que diga
que va mal porque las sondas miran el codigo de estado.

La usan tres cosas: la sonda del alojamiento, el smoke de publicacion (`deploy.yml`) y la propia
vigilancia interna.

## Senales operativas

`GET /api/v1/ops/signals` devuelve en una sola respuesta los tres SLO, el embudo de login, el uso del
producto, el monitoreo de negocio minimo, una **serie por dia** y el estado de las cinco alertas. Se
autentica con **llave de servicio** (`X-Ops-Key`, autenticacion M2M de
`../07-seguridad/autenticacion-autorizacion.md`), no con sesion de usuario: quien consulta esto es el
equipo. **Sin llave configurada el endpoint no existe** (404): desplegar sin configurarlo debe impedir
consultarlo, no abrirlo.

La serie diaria (`daily`, `?days=N`, 28 por defecto) se anade en `MVP-699` (`R-01`): las ventanas fijas
contestan «como va la semana» pero no «va mejor o peor que la anterior», ni «que dia se torcio», y sin
eso no se puede fijar el baseline que `../01-producto/kpis.md` encarga a las primeras cuatro semanas.
El parametro **no mueve las ventanas de los SLO**: esas las define la KB y son parte del objetivo.

**No hay interfaz**: estas senales no se ven en ninguna pantalla del producto, se consultan por HTTP.
Es coherente con el «N/A en fase C» de la tabla de dashboards, y esta anotado como pendiente en
`MVP-999` por si el volumen de uso lo justifica mas adelante.

Ver `runbooks/revision-operativa.md` para la revision semanal.

## Regla de umbrales

1. Baseline inicial de 4 semanas.
2. Revisión mensual de umbrales.

Los umbrales **no son configurables por despliegue**: viven en el codigo con su origen en esta tabla y
en `../01-producto/kpis.md`. Poder bajarlos desde un ajuste convertiria un SLO acordado en una
preferencia.

---

## Dashboards

| Dashboard | URL | Audiencia |
|-----------|-----|-----------|
| Overview del sistema | N/A en fase C (revision manual de logs y metricas) | Todos |
| Infraestructura | N/A en fase C (revision manual de logs y metricas) | DevOps / SRE |
| Autenticacion | N/A en fase C (revision manual de embudo login) | Producto + Ingenieria |

## Monitoreo de negocio mínimo (fase A)

Revisión semanal de 15 minutos sobre:

1. `logins_activos_semana`
2. `registros_creados_semana`
3. `tasa_error_funcional_visible`

## Telemetria del login (obligatoria)

Eventos requeridos:

1. `login_screen_viewed`
2. `login_google_clicked`
3. `login_google_success`
4. `login_google_error`
5. `login_abandonment`

Dimensiones minimas para analisis:

1. `timestamp`
2. `session_id`
3. `flow_id`
4. `channel`
5. `device_type`
6. `error_code` (si aplica)

Reglas de calidad de telemetria:

1. Todo evento de login debe incluir `flow_id` para reconstruir embudo completo.
2. El evento `login_abandonment` se emite por timeout de inactividad o cierre/salida sin exito.
3. Ningun evento puede incluir PII en claro.

### Como se explota (MVP-601)

Cada evento sale por **dos caminos** y ninguno sustituye al otro:

1. **Log estructurado** (`auth.funnel`), con las seis dimensiones. Sirve para mirar un caso concreto
   mientras el log siga a mano. Fuera de desarrollo el log se emite en **JSON con `timestamp`**: con el
   formateador de texto, las dimensiones salen interpoladas dentro de una frase y reconstruir el embudo
   pasaria por analizar prosa.
2. **Contadores diarios agregados**, en la tabla `telemetry_daily_counters` de la propia base de datos.
   Es lo que hace calculables las ventanas de 7 y 30 dias que piden los SLO: los logs de App Service no
   se retienen de forma fiable, asi que sobre ellos esas ventanas no existen.

Por que **contadores** y no una traza de eventos persistida: un contador responde a todos los KPI de la
KB y **no conserva ningun identificador**, asi que no anade una categoria de dato personal a `RN-041`
ni al inventario de `RN-042`. La traza individual habria permitido analisis no previstos, que es
justamente lo que la KB deja fuera de alcance en esta epica.

Contadores del embudo:

| Contador | Que cuenta |
|---|---|
| `login.screen_viewed` | Entradas a la pantalla de login |
| `login.google_clicked` | Clics en «Continuar con Google» |
| `login.success` | Accesos completados |
| `login.error` y `login.error.{codigo}` | Fallos del intercambio, en total y por codigo. Desde `MVP-713` el desglose distingue `auth_google_code_invalid` (codigo caducado o reusado) de `auth_google_exchange_failed` (fallo propio o de Google), que antes se mezclaban bajo el segundo |
| `login.abandonment` | Abandonos (por inactividad o por salida sin exito) |
| `login.success.duration_ms.sum` · `login.success.timed` | Suma de duraciones y su divisor, para el «tiempo medio de login exitoso» |

Se guarda **suma y divisor**, no la media: las medias no se agregan entre dias, asi que la de la semana
no es la media de las medias diarias. El divisor es `login.success.timed` y no `login.success` porque un
reinicio deja exitos cuyo instante de inicio se desconoce; contarlos con duracion cero rebajaria la
media y haria creer que el acceso es mas rapido de lo que es.

Con esto, los KPI de `../01-producto/kpis.md` salen de una consulta:

- Conversion de login = `login.success` / `login.screen_viewed`
- Tasa de abandono = `login.abandonment` / `login.screen_viewed`
- Tiempo medio de login exitoso = `login.success.duration_ms.sum` / `login.success.timed`

Retencion: los contadores se conservan 400 dias (`Telemetry:RetentionDays`) y se podan a diario. No es
un plazo de `RN-041` —no hay datos personales que expurgar—, sino higiene de tabla.

### Uso del producto (MVP-602)

Mismo mecanismo —log estructurado (`product.usage`) mas contador diario— para las senales de uso.

| Contador | Que cuenta |
|---|---|
| `app.session_started` | **Sesiones activas**: las que llegan al **area operativa** (el shell, tras la guarda de Workspace). **Es el divisor** del uso del dashboard. Una sesion que se queda en el onboarding **no** cuenta: es la definicion, no un olvido (MVP-703) |
| `dashboard.viewed` | Entradas al dashboard, todas |
| `dashboard.session_with_view` | Sesiones que abren el dashboard **al menos una vez** |
| `dashboard.manual_refresh` | **Discontinuada (MVP-706)**: contaba las pulsaciones de «Actualizar», botón retirado al reescribir RN-006. El contador conserva su histórico en la tabla, pero el informe de `GET /api/v1/ops/signals` ya no lo publica. El endpoint sigue aceptando el evento para no responder `400` a un cliente cacheado |
| `dashboard.widget.rendered` · `dashboard.widget.blocked` | Widgets que se pudieron mostrar y los que no |
| `dashboard.widget.{widget}.{ok\|empty\|error}` | Desglose, para saber **cual** falla y no solo que algo falla |

KPI de producto de `../01-producto/kpis.md`:

- Uso del dashboard en sesiones activas = `dashboard.session_with_view` / `app.session_started`
  — **de las sesiones que entran al area operativa, cuantas abren el panel** (MVP-703)
- ~~Recargas manuales por sesion~~ — retirada en MVP-706 junto con el boton que la alimentaba
- Cobertura de widgets MVP = `dashboard.widget.rendered` / (`rendered` + `blocked`)

Tres matices que cambian lo que significan estas cifras:

1. **Sesiones, no visitas.** `dashboard.session_with_view` existe porque el KPI pregunta por sesiones:
   quien entra ocho veces en una sesion sigue siendo una sesion, y contar visitas daria porcentajes por
   encima del 100 %.
2. **La sesion activa se cuenta al entrar al area operativa**, no al abrir el dashboard. Contarla en el
   propio dashboard haria que el porcentaje fuese siempre 100 %.

   Hasta `MVP-703` el codigo y esta pagina describian cosas distintas: el endpoint de ingesta decia
   admitir la senal sin Workspace porque «una sesion en onboarding tambien es una sesion activa», pero
   el cliente la emite desde el shell —que cuelga de la guarda de Workspace— y por tanto no la manda
   nunca en onboarding (`P-078`). Se fija la definicion **que se cumple**: la sesion activa es la que
   llega al area operativa. Emitirla tambien en onboarding se descarto a proposito: meteria en el
   divisor sesiones en las que el panel todavia no existe.

   > **Ruptura de serie declarada (MVP-703).** Hasta esa historia, con los maestros poblados el Home
   > **era** la Vision General, asi que casi toda sesion activa abria el panel por el mero hecho de
   > entrar y el KPI rondaba el 100 % por construccion. Desde MVP-703 el arranque es el diario
   > (RN-033), asi que abrir el panel pasa a ser una **eleccion**: el porcentaje bajara, y esa bajada
   > **no es una perdida de uso**. Las dos series no son comparables entre si; el corte esta en la
   > fecha de despliegue de MVP-703.
3. **`empty` no es `error`.** El KPI admite expresamente los estados vacio/incompleto: un Workspace que
   aun no ha cosechado no tiene el dashboard roto. Solo `error` resta cobertura.

Limite conocido: la senal de widget bloqueado viaja **por la propia API**, asi que cubre el fallo de un
widget concreto, no una caida total del servicio —en ese caso tampoco llegaria la senal—. La
disponibilidad se mide aparte (`MVP-603`).

---

## Estructura de logs

Todo log de producción debe incluir:

```json
{
  "timestamp": "2025-06-01T10:00:00.000Z",
  "level": "info",
  "service": "terrenario-api",
  "trace_id": "uuid",
  "span_id": "uuid",
  "message": "Descripción",
  "context": {
    "flow_id": "uuid",
    "event_name": "login_screen_viewed"
  }
}
```

**No loguear nunca**: datos de tarjeta, contraseñas, tokens, PII sin anonimizar.
Ver `../07-seguridad/privacidad-datos.md`.

## Trazabilidad KB

1. KPIs y objetivos de uso: `../01-producto/kpis.md`
2. Seguridad y privacidad de logs: `../07-seguridad/modelo-seguridad.md`
3. Arquitectura y evolución post-MVP: `../02-arquitectura/vision-general.md`
