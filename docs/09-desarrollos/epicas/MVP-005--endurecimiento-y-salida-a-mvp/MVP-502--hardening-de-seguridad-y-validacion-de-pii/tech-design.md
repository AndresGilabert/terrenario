---
id: "MVP-502"
tipo: feature
titulo: "TDD: Hardening de seguridad y validación de PII"
estado: completado
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seguridad", "privacidad"]
  modulo_path: "03-modulos/"
  componentes: ["auth", "authorization", "logging", "pii-controls"]
  etiquetas: ["mvp", "security", "privacy"]
  nivel_riesgo: alto
creado_en: "2026-07-31"
actualizado_en: "2026-07-31"
---

# TDD: MVP-502 — Hardening de seguridad y validación de PII

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Historia de endurecimiento: no añade capacidades de producto. Cierra los dos puntos que `MVP-999` le
asignó —`P-027` y `P-043`, que viven en el mismo borde de transporte— y ejecuta la **auditoría** que
piden CA-1, CA-2 y CA-3, corrigiendo lo que encuentra.

| Bloque | Qué se hace |
|---|---|
| `P-027` — `PATCH` con cuerpo no UTF-8 ⇒ `500` | Lector común de cuerpos parciales, con la transcodificación protegida, y filtro que traduce a `400` |
| `P-043` — códigos de validación del alta | Cada anotación declara su código de dominio; ningún mensaje del framework llega en inglés al cliente |
| CA-2 — PII en logs | El intercambio con Google dejaba de registrar cargas ajenas en claro |
| CA-1 — CSP | La política existía solo en las respuestas de la API (JSON); ahora también en el documento del SPA, que es donde protege |
| CA-1 — determinismo del traspaso | El sucesor del Workspace no era determinista con `joined_at` empatado |

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `Common/Http/PartialUpdateBody.cs` | nuevo | Lector común de cuerpos `PATCH` y primitiva `JsonText` con transcodificación protegida |
| `Common/Http/InvalidRequestBodyFilter.cs` | nuevo | Traduce un cuerpo ilegible a `400` en un único sitio |
| `Common/Errors/ApiValidationAttributes.cs` | nuevo | `RequiredField` / `MaxTextLength`: anotaciones que declaran su código de dominio |
| `Common/Errors/ModelStateErrorTranslator.cs` | nuevo | Traducción del `ModelState` al contrato de error |
| `Controllers/*.cs` (8) | modificado | Migración al lector común y a las anotaciones con código |
| `Infrastructure/Auth/GoogleOidcService.cs` | modificado | Deja de registrar la respuesta ajena en claro |
| `Infrastructure/Data/Repositories/WorkspaceRepository.cs` | modificado | Desempate determinista del sucesor |
| `Application/Workspaces/GetWorkspaceClosureOptionsHandler.cs` | modificado | Mismo desempate, para que anuncie al sucesor real |
| `frontend/vite.config.ts` | modificado | Plugin que inyecta la CSP del SPA en el build de producción |
| `docs/02-arquitectura/contratos-api.md` | modificado | El contrato describe los códigos nuevos |
| `docs/07-seguridad/autenticacion-autorizacion.md` | modificado | CSP del cliente y por qué la de la API no bastaba |

## Diseño detallado

### `P-027` — Un cuerpo mal codificado es un `400`, no un `500`

El patrón de edición parcial de esta API recibe el cuerpo como
`[FromBody] Dictionary<string, JsonElement>`, que es lo que permite distinguir «el campo no viene» de
«viene vacío». El problema es que `JsonElement.GetString()` lanza `InvalidOperationException`
(«Cannot transcode invalid UTF-8 JSON text») **después** del binding: ningún controlador la
capturaba, así que la API respondía `500` a un error del cliente. Además de mentir sobre de quién es
la culpa, ensuciaba la observabilidad con errores de servidor que no lo eran.

La corrección tiene tres piezas:

1. **`JsonText.Read(element, key)`** — la primitiva que faltaba: leer texto de un `JsonElement` con
   red. Es el único sitio donde se captura la excepción de transcodificación.
2. **`PartialUpdateBody`** — el lector común que pide el punto. Expone las lecturas de texto y, como
   `Try…`, las de tipo. Los `Try…` son deliberados: el **código** de error de un tipo inválido es de
   dominio —cada maestro tiene el suyo— y debe seguir decidiéndolo el controlador. Se centraliza la
   lectura, no la política.
3. **`InvalidRequestBodyFilter`** — traduce a `400 VALIDATION_FORMAT_INVALID`. Va en el borde de
   transporte, como el filtro de scope de MVP-105: repetir el `try/catch` en ocho controladores
   garantizaría que alguno se quedara sin él.

**Se corrigen los ocho controladores, no los tres que el punto nombra.** `P-027` citaba `Plots`,
`Workers` y `Tasks`, pero el mismo patrón —y con él el mismo `500`— estaba también en `Seasons`,
`Activities`, `Harvests`, `Purchases` y `Consumptions`, que llegaron después de registrarse el punto.
Los tres primeros pasan al lector completo (sus lectores eran triviales); los otros cinco conservan
sus lectores de fecha e identificador —que llevan códigos de dominio propios— pero **todas** sus
lecturas de texto pasan por `JsonText`. Resultado: cero llamadas a `GetString()` sin red en
controladores.

### `P-043` — El alta y la edición responden lo mismo

`InvalidModelStateResponseFactory` colapsaba **toda** la validación de binding a
`VALIDATION_REQUIRED`, así que «falta el nombre», «el nombre está en blanco» y «el nombre es
demasiado largo» eran indistinguibles en el `POST`, mientras el `PATCH` —que valida en el dominio—
devolvía el código específico. Y cuando el fallo lo generaba el framework, el mensaje salía **en
inglés** («The request field is required.») y la UI lo mostraba tal cual al usuario.

El obstáculo real: **`ModelState` solo transporta un texto**. No hay hueco para un código, ni forma
fiable de saber desde la fábrica qué atributo falló. La solución es que el atributo componga el
mensaje como `CÓDIGO␟texto` y la fábrica lo descomponga. El separador es `U+001F` (Unit Separator),
que no aparece en texto escrito por personas. La convención vive **solo** en
`ApiValidationAttributes` y en `ModelStateErrorTranslator`: ningún otro sitio la ve.

`ModelStateErrorTranslator` decide en tres casos, por orden:

1. **Anotación propia** (`RequiredField`, `MaxTextLength`): trae su código dentro.
2. **Error del binder** (hay excepción, o el mensaje es una plantilla conocida de ASP.NET): el valor
   llegó pero no se puede interpretar ⇒ `VALIDATION_FORMAT_INVALID` y mensaje propio en español,
   nombrando el campo como lo envió el cliente (`$.start_date` → `'start_date'`).
3. **Mensaje propio sin código**: `VALIDATION_REQUIRED`, el comportamiento anterior, que sigue siendo
   correcto para «falta un dato».

`VALIDATION_FORMAT_INVALID` es el código nuevo, y distingue **«falta»** de **«llegó, pero no se puede
interpretar»**: son dos arreglos distintos para el cliente.

### CA-2 — Lo que la auditoría de PII encontró (y lo que no)

Buena parte del trabajo ya estaba hecho en épicas anteriores, y conviene dejarlo dicho para que la
revisión de `MVP-503` no lo busque otra vez:

| Control | Estado | Dónde |
|---|---|---|
| Emails enmascarados en logs | ya cumplido | `EmailMasking` (MVP-103) |
| Refresh tokens e invitaciones guardados solo como hash | ya cumplido | `RefreshTokenStore`, `IOneTimeTokenService` |
| Sin PII en query params | ya cumplido | ningún `[FromQuery]` de email |
| Sin `console.log` en el cliente | ya cumplido | frontend limpio |
| `X-Request-Id` entrante acotado (evita inyección en trazas) | ya cumplido | `RequestIdMiddleware` (MVP-105) |
| El preview de invitación no revela el email destinatario | ya cumplido | `PreviewInvitationHandler` (MVP-107) |
| Cookie de refresco `HttpOnly` + `SameSite=Strict` + `Path` acotado | ya cumplido | `AuthController` |

Lo que **sí** había que corregir es el intercambio con Google:

- Registraba el **cuerpo entero** de la respuesta de error. Es una carga de un tercero sobre la que
  no tenemos control, que acompaña a una petición que lleva el `code` y el `client_secret`.
  `privacidad-datos.md` es explícito: «los tokens y credenciales del proveedor no se almacenarán en
  claro en logs». Ahora se extrae **solo** el campo `error` de OAuth 2.0 (RFC 6749 §5.2), que es
  vocabulario cerrado; si el cuerpo no tiene esa forma, no se registra nada de él.
- Registraba `ex.Message` de `InvalidJwtException`, que puede arrastrar fragmentos del propio
  `id_token`. Ahora solo el **tipo** de la excepción.

### CA-1 — La CSP estaba donde no servía

`SecurityHeadersMiddleware` (MVP-105) emite `Content-Security-Policy: default-src 'self'` en las
respuestas de la **API**. Pero esas respuestas son JSON: no son un contexto de ejecución de scripts,
así que la política ahí no protege prácticamente de nada. **El documento HTML del SPA no tenía
ninguna**, y es justo donde la CSP mitiga XSS — y donde importa, porque el token de acceso vive en
`sessionStorage`.

Se añade un plugin de Vite (`terrenario-csp`) que inyecta la política en el `index.html` **solo en el
build de producción**: en desarrollo, Vite necesita scripts en línea para el preámbulo de React
Refresh y un WebSocket para el HMR, así que una política estricta rompería el arranque sin proteger
nada. La política y sus concesiones están documentadas en
[`autenticacion-autorizacion.md`](../../../../07-seguridad/autenticacion-autorizacion.md).

### CA-1 — El sucesor del Workspace no era determinista

Lo destapó la suite: `FindOtherActiveOwnerAsync_Deberia_DevolverAlCopropietarioMasAntiguo` empezó a
fallar de forma intermitente al mover el orden a SQL en `MVP-501`.

La causa no es el test. RN-038/CA-5 promete un sucesor **determinista** para el traspaso automático,
y el criterio era «el copropietario activo más antiguo» por `joined_at` — pero dos personas pueden
tener **el mismo** `joined_at`: la resolución del reloj es de milisegundos y un alta en lote entra a
la vez. Con el orden en memoria el resultado dependía del orden físico de las filas y parecía
estable; en SQL es abiertamente arbitrario. La regla nunca fue determinista: solo lo aparentaba.

Se cierra con un desempate por `UserId`, **en los dos sitios**: el repositorio que traspasa y el
handler que **anuncia** el sucesor en la confirmación. Sin el segundo, la pantalla podía nombrar a
una persona y el traspaso acabar en otra.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Capturar la transcodificación en cada controlador | Ocho sitios que hay que acertar por separado, y el noveno que llegue se olvidará |
| Codificar el error de validación resolviendo la anotación por metadatos | Con `[Required]` y `[StringLength]` en la misma propiedad no se puede saber **cuál** falló: el mensaje es la única señal que `ModelState` conserva |
| Migrar los cinco controladores operativos al lector completo | Sus lectores de fecha e identificador llevan códigos de dominio propios; reescribirlos era mucha superficie para un arreglo de transcodificación. Se enruta la lectura de texto, que es donde estaba el fallo |
| CSP como `meta` también en desarrollo | Rompería el HMR y el preámbulo de React Refresh sin proteger nada: el servidor de desarrollo no se expone |
| Dejar la CSP del SPA para el gate de `MVP-504` | Sería aplazar un control de seguridad a una historia cuyo alcance es verificar, no construir |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Un cliente dependía de recibir `VALIDATION_REQUIRED` en el alta | baja | El frontend solo discrimina `VALIDATION_CONSUMPTION_OVERFLOW`; del resto muestra `message`. Verificado |
| La CSP rompe la aplicación desplegada | media | Verificada sobre el **build de producción** servido de verdad, con la API real: fuentes e iconos cargados, todas las llamadas en `200`, cero violaciones en consola |
| El separador `U+001F` aparece en un mensaje real | muy baja | Es un carácter de control: no existe en texto escrito por personas. La descodificación falla de forma segura (trata el mensaje como sin código) |

## Plan de testing

- [x] Unitarios: `ModelStateErrorTranslatorTests` (8 casos, incluidas las ramas de mensajes del
      framework que desde fuera cuestan de provocar) y `PartialUpdateBodyTests` (11 casos).
- [x] Integración: `TransportValidationTests` — 14 casos sobre la API real. Cubre el cuerpo no UTF-8
      en **los ocho** controladores, la distinción falta/demasiado largo en los cuatro maestros, el
      mensaje en español ante formato inválido y una guarda de no-regresión del camino feliz.
- [x] Regresión de determinismo: `FindOtherActiveOwnerAsync_Deberia_SerDeterminista_…`.
- [x] Verificación manual conducida de la CSP sobre el build de producción.

## Resultado

| Suite | Antes | Después |
|---|---|---|
| Backend | 610 | **631** |
| Frontend | 72 | 72 |

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Sin migraciones (esta historia no toca el esquema)
- [x] Tests escritos y pasando
- [x] Contrato de API actualizado con los códigos nuevos
- [x] `autenticacion-autorizacion.md` actualizado con la CSP del cliente
- [x] `P-027` y `P-043` cerrados en `MVP-999`
- [x] Sin `TODO` sin resolver en este documento
