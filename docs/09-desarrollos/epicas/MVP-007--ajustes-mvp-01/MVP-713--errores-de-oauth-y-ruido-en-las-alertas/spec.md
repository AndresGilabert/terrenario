---
id: "MVP-713"
tipo: feature
titulo: "Errores de OAuth y ruido en las alertas"
estado: completado
prioridad: media
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
  dominios: ["autenticacion", "observabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["google-oidc", "slo", "alertas"]
  etiquetas: ["mvp", "ajustes", "bug"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-713 — Errores de OAuth y ruido en las alertas

> **Origen**: `P-079` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

`GoogleOidcService` mapea **cualquier** respuesta no exitosa del endpoint de token de Google a
`AUTH_GOOGLE_EXCHANGE_FAILED`, que `AuthController` traduce a **HTTP 500**. Eso incluye `invalid_grant`,
que es lo que Google devuelve ante un codigo ya usado o expirado: basta con recargar la pantalla de
callback para provocarlo.

Consecuencias verificadas en la revision de `MVP-699`: cuenta contra el SLO de tasa de error (objetivo
< 0,1 %) y contra `HighErrorRate`, que es **critica**. Un solo 500 de este tipo sobre 70 peticiones dio
1,43 % y disparo la alerta, con envio de correo real.

Es comportamiento anterior a la epica de observabilidad (`MVP-101`/`MVP-502`), pero solo tiene
consecuencia desde `MVP-603`, porque antes no lo miraba nadie.

## Objetivo

Que un error del cliente deje de contarse como fallo del servidor, para que las alertas criticas solo
salten cuando pasa algo de verdad.

## Requisitos de usuario

### HU-1 — Alertas en las que se pueda confiar

**Como** responsable tecnico,
**quiero** que `HighErrorRate` no salte porque alguien recargo la pantalla de vuelta de Google,
**para** que siga sirviendo cuando el problema sea real.

## Alcance (in-scope)

- Distinguir el vocabulario cerrado de errores de OAuth 2.0: `invalid_grant` e `invalid_request` son de
  cliente (401/400); `invalid_client`, `unauthorized_client` y las caidas de Google son de servidor
  (500).
- Mensaje util al usuario en el caso de cliente: el codigo ha caducado o ya se uso, hay que volver a
  entrar.
- Que los casos de cliente dejen de contar en el SLO de tasa de error.

## Fuera de alcance (out-of-scope)

- Cambiar el flujo de autenticacion o el proveedor.
- Revisar el resto de codigos de error de la API.
- Redefinir umbrales de alerta o SLOs: el problema es la clasificacion, no el umbral.

## Criterios de aceptación

- [x] **CA-1**: Recargar la pantalla de callback con un codigo ya usado devuelve 400/401, no 500, y la
  pantalla lo explica. `invalid_grant` responde **401 `AUTH_GOOGLE_CODE_INVALID`** e `invalid_request`,
  **400 `AUTH_GOOGLE_REQUEST_INVALID`**, verificado sobre la aplicacion real en
  `GoogleOAuthErrorContractTests` (recorre `Program.cs` entero: pipeline, filtros y controlador). La
  pantalla dice «El acceso ha caducado o esta pagina ya se habia usado. Vuelve a entrar con Google» en
  vez de «Error al completar el acceso», cubierto en `OAuthCallback.test.tsx`.
- [x] **CA-2**: Un fallo de configuracion (`invalid_client`) o una caida de Google siguen devolviendo
  500, que es lo que son. Mismo test de contrato, tercer caso. Y el **defecto** de la tabla es 500: un
  `error` desconocido, ausente o ilegible tambien se cuenta como fallo propio, porque clasificar por
  descarte convertiria una averia nueva en un 4xx silencioso.
- [x] **CA-3**: Los casos de cliente no incrementan la tasa de error del SLO. Comprobado pasando la
  respuesta del callback por la instrumentacion real (`RequestMetricsMiddleware`): `invalid_grant` e
  `invalid_request` suman en `api.requests.4xx` y **no** en `api.requests.5xx`, mientras que
  `invalid_client` sigue sumando en `5xx`. Siguen en el **divisor** (`api.requests`): la peticion se
  sirvio y la disponibilidad y la latencia la cuentan.
- [x] **CA-4**: Reproducido el escenario que disparo la alerta en `MVP-699` y comprobado que ya no la
  dispara. La ventana de la revision —69 peticiones servidas mas un codigo caducado— pasa por
  `AlertEvaluator` y `HighErrorRate` **no** se dispara; el mismo experimento con `invalid_client` **si**
  la dispara, que es lo que confirma que el arreglo no la ha dejado ciega.
- [x] **CA-5**: Test de regresion sobre el mapeo de codigos de OAuth.
  `GoogleOAuthErrorMappingTests` fija el vocabulario cerrado de RFC 6749 §5.2 —`invalid_grant`,
  `invalid_request`, `invalid_client`, `unauthorized_client`— con su codigo de API y su estado HTTP, y
  cubre tambien lo no clasificado (valor vacio, nulo, desconocido y `INVALID_GRANT` en mayusculas).

## Maquetas y referencias visuales

No aplica: es comportamiento de servidor y de la pantalla de vuelta de Google.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| LoginPage / callback | docs/05-infraestructura/observabilidad.md | hecho | Un codigo caducado responde 401, cuenta como 4xx y `HighErrorRate` ya no se dispara con el escenario de `MVP-699`; la pantalla explica que hay que volver a entrar |

## Notas y decisiones

- Una alerta critica que salta sin motivo se acaba ignorando **tambien cuando el motivo es real**. Ese
  es el dano de este punto, no el codigo de estado.
