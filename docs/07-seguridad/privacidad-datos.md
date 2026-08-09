---
bloque: 07-seguridad
documento: privacidad-datos
actualizado_en: "2026-08-08"
---

# Privacidad de Datos y GDPR

> **IMPORTANTE para agentes de IA**: Antes de generar código que maneje datos de usuarios,
> leer este documento. Cualquier dato PII requiere tratamiento especial.
>
> **Politica de cumplimiento obligatorio**: Todo el proyecto debe cumplir en todo momento la normativa europea y espanola de proteccion de datos aplicable.
> Lo obligatorio no es negociable ni puede relajarse por criterios de plazo, coste o conveniencia tecnica.

---

## Marco normativo aplicable

### Obligatorio siempre (base legal minima del proyecto)

| Norma | Ambito | Estado de cumplimiento |
|-----------|---------|-------------|
| Reglamento (UE) 2016/679 (RGPD/GDPR) | Tratamiento de datos personales de personas en la UE | **Obligatorio** |
| Ley Organica 3/2018 (LOPDGDD, Espana) | Desarrollo nacional del RGPD y derechos digitales en Espana | **Obligatorio** |

### Obligatorio segun escenario (condicionado)

| Norma | Cuando aplica | Estado |
|-----------|---------|-------------|
| Ley 34/2002 (LSSI-CE, Espana) | Servicios de la sociedad de la informacion, comunicaciones electronicas y uso de cookies/tecnologias similares | **Obligatorio si aplica** |
| Directiva ePrivacy 2002/58/CE (y transposicion nacional) | Confidencialidad de comunicaciones y reglas de cookies/trackers | **Obligatorio si aplica** |
| Evaluacion de Impacto en Proteccion de Datos (EIPD, RGPD art. 35) | Tratamientos de alto riesgo para derechos y libertades | **Obligatorio si aplica** |
| Notificacion de brechas a autoridad y afectados (RGPD arts. 33 y 34) | Violacion de seguridad de datos personales | **Obligatorio si aplica** |

### Recomendado (no sustituye obligaciones legales)

| Referencia | Tipo | Estado |
|-----------|---------|-------------|
| Guias AEPD (cookies, evaluacion de riesgos, anonimización) | Guia interpretativa | Recomendado |
| ISO/IEC 27001 e ISO/IEC 27701 | Buenas practicas certificables | Recomendado |
| NIST Privacy Framework | Buenas practicas | Recomendado |

---

## Reglas de cumplimiento transversal

1. Todo nuevo requisito funcional, tecnico o de datos debe analizar impacto en RGPD + LOPDGDD antes de aprobarse.
2. Si una funcionalidad no puede cumplir una obligacion legal aplicable, no entra en desarrollo.
3. Si una norma es "obligatoria si aplica", el ticket debe dejar evidencia de si aplica o no, con justificacion.
4. Ningun PR que trate datos personales puede aprobarse sin validar esta politica.

## Clasificación de datos

| Categoría | Ejemplos | Tratamiento |
|-----------|---------|-------------|
| **PII básico** | Nombre, email, teléfono | Cifrado en reposo, acceso restringido |
| **PII de terceros introducida por el usuario** | Nombre de una persona de la cuadrilla (`workers.name`), nombre del propietario de un terreno cedido (`plots.owner_name`), texto libre de una labor (`activities.description`) | El usuario del Workspace es quien la introduce y **responde de tener base legítima**; el producto la trata por su cuenta (encargo). Ver más abajo |
| **PII sensible** | Datos bancarios, documentos de identidad | Cifrado en reposo + en tránsito, acceso muy restringido |
| **Datos de comportamiento** | Logs de uso, historial | Minimizacion, pseudonimizacion y/o anonimizacion segun finalidad |
| **Datos públicos** | IDs, referencias | Sin restricciones especiales |

## Datos personales de terceros introducidos por el usuario (MVP-503)

Verificado sobre el esquema real: además de los datos de la cuenta, el producto almacena datos
personales que **el usuario introduce sobre otras personas**, y que esas personas no han facilitado
ni pueden gestionar por sí mismas.

| Dato | Dónde | Quién es esa persona |
|---|---|---|
| Nombre de la cuadrilla | `workers.name` | Alguien que trabaja en la explotación y puede no tener cuenta |
| Nombre del propietario del terreno | `plots.owner_name` | El arrendador de un terreno cedido (RN-028) |
| Texto libre de una labor | `activities.description` | Puede mencionar a cualquiera |

Consecuencias, y por qué importan:

1. **El titular del Workspace es responsable del tratamiento** de esos datos; el producto actúa como
   encargado. Los Términos del Servicio lo dicen expresamente: quien registra a su cuadrilla debe
   informarles y tener base legítima.
2. **Esas personas no pueden ejercer sus derechos desde el producto**, porque no tienen cuenta. Su vía
   es el titular del Workspace, o el contacto de privacidad.
3. **La baja de cuenta no los borra**, y es correcto: pertenecen al Workspace, no a la cuenta de quien
   se va. Se van con el Workspace cuando este se da de baja (RN-039 + RN-041).
4. **Minimización**: `owner_name` y `description` son opcionales y de texto libre. La política pide no
   introducir más datos de terceros de los necesarios; el producto no lo puede impedir.

---

## Canal de sugerencias e incidencias (MVP-711)

La aplicación tiene desde `MVP-711` un canal por el que una persona con cuenta puede contar un fallo
o pedir algo. Se implementa como **formulario propio que envía un correo**, sin herramienta de
tickets ni widget de terceros; esa decisión es de producto, pero tiene aquí su motivo: cualquier
proveedor externo sería un **encargado del tratamiento** nuevo (art. 28) y, si carga scripts, activaría
`RN-042` y obligaría a recabar consentimiento previo.

### Qué se recoge

| Dato | Origen | Por qué está |
|---|---|---|
| Tipo (`incidencia` / `sugerencia`) y texto libre | Lo escribe la persona | Es el reporte |
| Nombre y correo de la cuenta | Ya los tiene el producto (RN-036) | Poder responder: un canal de soporte del que no se puede contestar deja de serlo |
| Versión desplegada | La resuelve el servidor | Saber sobre qué versión ocurrió |
| Pantalla desde la que se reporta | Ruta del cliente (`/app/diario`), **sin query ni fragmento** | Reproducir el problema |
| `X-Request-Id` de la última petición fallida | Cabecera de respuesta de la API (`P-006`) | Saltar del reporte a la traza del servidor |
| Navegador | Cabecera `User-Agent` de la propia petición | Descartar que sea cosa de un navegador concreto |

**Qué no se recoge, y es deliberado**: nada de la explotación. Ni Workspace, ni temporada, ni
filtros, ni identificadores de registros. La query de la URL se **recorta en el servidor** porque los
filtros del panel llevan identificadores de terreno desde `MVP-403`: un canal de soporte no puede ser
una vía lateral por la que datos operativos acaben en un buzón de correo. Tampoco se adjuntan
capturas ni ficheros (fuera de alcance del spec).

El texto libre es de la persona y **puede contener lo que ella decida escribir**, incluidos datos de
terceros. El producto no lo puede impedir —es el mismo límite que en `activities.description`—, y por
eso el formulario pide «qué estabas haciendo y qué pasó», no datos.

### Con qué base

**Interés legítimo** (art. 6.1.f) en mantener y corregir el servicio, ponderado así: el tratamiento lo
inicia la propia persona, los datos son los mínimos para atender lo que pide, no hay perfilado ni
cesión a nadie, y la expectativa razonable de quien escribe a un canal de soporte es exactamente que
se le lea y se le pueda contestar. La alternativa —consentimiento— sería artificiosa para un
tratamiento que la persona provoca al pulsar «Enviar».

**Transparencia (art. 13) en el momento**: la pantalla enumera, **antes** de enviar, qué acompaña al
mensaje —incluidos el nombre y el correo de la cuenta— y dice expresamente que no se envía nada de la
explotación. No se deja para un documento aparte.

### Cuánto se conserva

El producto **no almacena el reporte**: no hay tabla de reportes, ni estados, ni seguimiento dentro de
la aplicación. Lo que existe es el correo en el buzón de operación (`Feedback:Recipient`), y ahí es
donde aplica el plazo.

| Qué | Desde cuándo cuenta | Retención | Acción al expirar |
|---|---|---|---|
| Reporte del canal en el buzón de operación | Recepción | **24 meses** (máximo) | Borrado del buzón |

Los 24 meses son el mismo criterio de `RN-041`, no un plazo nuevo: es el techo, y lo normal es
borrarlo al cerrar el asunto. Es el único plazo del producto que **no ejecuta ninguna rutina**, porque
lo conservado no está en la base de datos sino en una bandeja de correo; se anota aquí precisamente
para que sea una obligación escrita y no una costumbre.

**No añade nada al inventario de almacenamiento en el navegador.** El contexto del reporte —dónde
estaba y qué petición falló— vive en **memoria** de la pestaña, no en `sessionStorage` ni en
`localStorage`, para no ampliar lo que `RN-042` obliga a inventariar. El precio es que se pierde al
recargar la página, y se acepta.

---

## Reglas especificas para autenticacion social

Cuando se use un proveedor externo de identidad (por ejemplo Google):

1. Solo se recogeran los datos estrictamente necesarios para crear y mantener la cuenta.
2. Se documentara el origen de los datos y la base juridica del tratamiento.
3. Los tokens y credenciales del proveedor no se almacenaran en claro en logs, URLs ni mensajes de error.
4. Si el proveedor entrega atributos adicionales no necesarios, se descartaran por defecto.
5. Cualquier ampliacion a otros proveedores debera revisarse antes de activarse para confirmar cumplimiento RGPD + LOPDGDD.

---

## Encargados del tratamiento (proveedores externos con acceso a PII)

Todo proveedor externo que trate datos personales por cuenta del proyecto es **encargado del
tratamiento** (RGPD art. 28) y exige contrato de encargo (DPA) firmado antes de entrar en produccion.

| Proveedor | Datos tratados | Finalidad | Estado |
|-----------|---------|---------|---------|
| Microsoft Azure | Todo lo almacenado | Alojamiento de la aplicacion y la base de datos | ✅ Contratado, anexo de tratamiento de datos **en vigor** |
| Arsys | Email del destinatario, nombre de quien invita y del Workspace | Envio de invitaciones a Workspace | ✅ Contratado, anexo **en vigor**: ver [ADR-0010](../02-arquitectura/decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md) |

Con estos dos no hay contrato que negociar: el anexo de tratamiento de datos va **incorporado al
contratar el servicio**. Confirmado por el negocio el 2026-08-04, con lo que se cierra `B-2` del gate.

### Google no es encargado

Cuando una persona entra con **su** cuenta de Google, Google trata esos datos bajo su propia politica
y no por cuenta del proyecto: actua como **responsable independiente**, no como encargado del art. 28.
Por eso no procede contrato de encargo con Google, y por eso sale de la tabla anterior. Lo que si
procede es informarlo, y se informa en la Politica de Privacidad.

Este encuadre lo aporto la asesoria del negocio (2026-08-04) y corrige la clasificacion anterior de
`MVP-503`, que lo listaba como encargado.

### Transferencias internacionales

| Via | Destino | Garantia |
|-----|---------|----------|
| Alojamiento (Azure) | Region **Espana** | Sin transferencia: los datos se almacenan en la UE |
| Correo (Arsys) | Espana | Sin transferencia |
| Inicio de sesion (Google) | EE. UU. | Comunicacion a un **responsable independiente**, regida por sus condiciones y por sus garantias: clausulas contractuales tipo de la Comision Europea y decision de adecuacion del Marco de Privacidad de Datos UE-EE. UU. |

**Ningun encargado trata datos fuera de la UE.**

La transferencia a Google es **inevitable mientras el acceso sea con Google** (`RN-036`): no hay
alternativa que ofrecer a quien no la acepte, mas alla de no crear la cuenta. Queda declarada en la
Politica de Privacidad en vez de omitirse.

### Identidad del responsable

Los datos publicados en las paginas legales viven en un solo sitio,
`src/frontend/terrenario-web/src/config/legal-entity.ts`, y cada campo admite override por variable
de entorno `VITE_LEGAL_*`. Estan versionados a proposito: la LSSI obliga a publicarlos, asi que no
hay nada que proteger, y un `.env` no llega al despliegue.

### Atencion manual del acceso y la portabilidad (arts. 15 y 20)

La **supresion** se ejerce desde la aplicacion (`MVP-505`). El **acceso** y la **portabilidad** se
atienden a mano mientras el MVP este en validacion: con pocos usuarios y un plazo legal de un mes,
consultar la base y entregar el resultado es conforme. Decision tomada en el gate de `MVP-504` (B-4).

Que se entrega ante una solicitud, por orden de a quien pertenece el dato:

| Bloque | Contenido | ¿Portabilidad (art. 20)? |
|--------|-----------|--------------------------|
| Cuenta | `display_name`, `email`, `google_sub`, fechas de alta y actualizacion | Si |
| Participacion | Workspaces y rol, invitaciones enviadas y recibidas | Si |
| Explotacion | Terrenos, temporadas, labores, cosechas, compras y consumos de los Workspaces **de su propiedad** | Los aporto, si; pero no son datos personales *sobre* la persona |
| Agregados del dashboard | Costes, medias y totales calculados | **No**: son datos derivados |

**Dos limites que hay que aplicar al preparar la respuesta**, no despues:

1. **Datos de terceros.** `workers.name`, `plots.owner_name` y las menciones en texto libre de
   `activities.description` son de otras personas. Se entregan porque el solicitante ya los conoce
   —los introdujo el—, pero no son su derecho de portabilidad y conviene decirlo en la respuesta.
2. **Workspaces compartidos.** Si el Workspace tiene mas miembros, el historico incluye lo que
   registraron ellos. Se entrega solo lo de los Workspaces **de su propiedad**, y se advierte de que
   el contenido puede tener aportaciones de terceros.

**Formato**: el art. 20 exige estructurado y legible por maquina —JSON o CSV, no PDF—. El art. 15 no
exige formato, asi que se puede responder con el mismo fichero.

**Plazo**: un mes desde la solicitud, prorrogable dos mas si es compleja, avisando.

La automatizacion de esto es una **funcion de producto**, mas amplia que la obligacion legal, y esta
registrada aparte (`MVP-999`, `P-070`).

---

## Principios GDPR aplicados

| Principio | Implementación |
|-----------|---------------|
| **Minimización** | Solo recoger los datos necesarios para el servicio |
| **Limitación de almacenamiento** | Política de retención activa (ver tabla abajo) |
| **Exactitud** | El usuario puede corregir sus datos |
| **Integridad y confidencialidad** | Cifrado + control de acceso |
| **Responsabilidad** | Logs de auditoría para accesos a PII |

## Base de legitimacion del tratamiento (RGPD art. 6)

Todo tratamiento de datos personales debe mapearse a una base juridica valida antes de implementarse:

| Base juridica | Uso esperado en proyecto |
|---------|---------|
| Ejecucion de contrato | Operativa principal del servicio solicitado por el usuario |
| Cumplimiento de obligacion legal | Conservacion legal de ciertos registros, cuando corresponda |
| Interes legitimo | Solo tras test de ponderacion documentado |
| Consentimiento | Casos especificos (ej. cookies no tecnicas, marketing), siempre revocable |

Si no existe base juridica valida, el tratamiento queda prohibido.

---

## Política de retención de datos

| Tipo de dato | Retención | Acción al expirar |
|-------------|-----------|------------------|
| Datos de cuenta activa | Duración de la cuenta | — |
| Datos de cuenta cancelada | 24 meses tras cancelación | Anonimización / borrado |
| Sesión (token de refresco) | Hasta caducidad o revocación, **más 30 días** (RN-041) | Borrado físico |
| Logs de transacciones de pago | 5 años (si existe obligacion legal aplicable al caso) | Archivado seguro |
| Logs de acceso / auditoría | 12 meses | Borrado |
| Datos de comportamiento | 6 meses | Anonimización |

### Lo que el producto conserva por diseño (MVP-505, RN-041)

El MVP toma varias decisiones de **no borrar**: la baja de un Workspace es lógica (RN-039), la
eliminación de un registro operativo también (RN-037), y una cuenta dada de baja conserva su fila
anonimizada porque cada actividad, cosecha y compra guarda quién la registró.

Todas son decisiones legítimas —borrar en cascada destruiría el histórico operativo de terceros— pero
«no se borra nada» **necesitaba un plazo**: sin él es «se guarda para siempre sin criterio», que es lo
que el principio de limitación del almacenamiento prohíbe. `RN-041` lo fija extendiendo el mismo
criterio de 24 meses que ya regía para la cuenta cancelada:

| Qué se conserva | Desde cuándo cuenta | Retención | Acción al expirar |
|---|---|---|---|
| Cuenta dada de baja (fila anonimizada) | `users.deleted_at` | 24 meses | Borrado físico de la fila |
| Workspace dado de baja y todo su contenido (RN-039) | `workspaces.deleted_at` | 24 meses | Borrado físico |
| Registro operativo eliminado lógicamente (RN-037) | `deleted_at` del registro | 24 meses | Borrado físico |
| Solicitud de reactivación cerrada o caducada (RN-040) | Cierre o caducidad | 24 meses | Borrado físico |
| Invitación en estado terminal (aceptada, rechazada, anulada o caducada) | Última transición | 24 meses | Borrado físico |
| Token de refresco revocado o caducado (MVP-714) | `revoked_at` o `expires_at`, lo primero que ocurra | **30 días** | Borrado físico |
| Reporte del canal de sugerencias e incidencias (MVP-711) | Recepción en el buzón de operación | 24 meses (máximo) | Borrado del buzón, **a mano**: no está en la base de datos |

#### Por qué la sesión tiene un plazo distinto (MVP-714, `P-071`)

La fila de `refresh_tokens` es un **dato de sesión** —hash del token, cuenta y fechas—, no histórico
operativo que nadie más pueda reconstruir. Aplicarle los 24 meses del resto sería conservador de más
justo en la categoría que más filas genera: la rotación crea una fila por cada refresco, así que un
usuario activo deja miles al año.

Los 30 días son el mismo orden que la vida del propio token (`Auth:RefreshToken:LifetimeSeconds`, 30
días), de modo que la regla se lee como «un token muerto no dura más de lo que habría durado vivo».
Y dejan cuatro ciclos de la revisión operativa semanal de `observabilidad.md` para investigar una
sesión sospechosa antes de que el rastro desaparezca, que es lo único que justifica conservarlo un
solo día.

Corrige además una suposición equivocada de `P-071`: se creía que purgar la cuenta arrastraba sus
tokens por cascada y que el único problema era el plazo. **No hay tal cascada** —`refresh_tokens` no
tiene FK hacia `users`—, así que las filas quedaban huérfanas indefinidamente. Con el plazo propio
desaparecen 30 días después de morir, mucho antes de que la cuenta llegue a purgarse.

**Los datos personales no esperan a ese plazo.** La baja de cuenta los borra o anonimiza en el acto
—nombre, correo e identificador del proveedor de identidad, tanto en la cuenta como en los maestros de
sus Workspaces y en las invitaciones que la nombraban—. Lo que se conserva 24 meses es la **fila
anonimizada**, que ya no identifica a nadie y solo sostiene la autoría del histórico operativo.

Los plazos viven también en código (`AccountRetentionPolicy`) para que sean verificables y no solo
declarados: la respuesta de la baja devuelve la fecha de purga concreta.

Y **hay quien los ejecuta** desde `MVP-504` (`B-3`): `RetentionPurgeWorker` hace una pasada diaria
dentro de la propia API —con cerrojo para que dos réplicas no purguen a la vez— y
`RetentionPurgeService` aplica las seis categorías. Se retira aquí la nota que decía que la rutina
seguía esperando una programación periódica de infraestructura: eso dejó de ser cierto al entregar
`B-3`.

---

## Inventario de tecnologías de almacenamiento y terceros (MVP-505, RN-042)

Evidencia para la revisión de LSSI-CE / ePrivacy. Se mantiene actualizado: **toda tecnología nueva
entra en esta tabla antes de activarse**.

> **Verificado contra el código en `MVP-503`** (2026-08-03). La primera versión de esta tabla, escrita
> en `MVP-505`, declaraba una clave que no existía y omitía cinco que sí. Un inventario de
> cumplimiento que no coincide con el sistema no sirve de evidencia: esta tabla se contrasta con
> `grep` sobre el cliente, no de memoria.

| Tecnología | Dónde | Para qué | Clasificación |
|---|---|---|---|
| Cookie `refresh_token` | Navegador (`HttpOnly`, `SameSite=Strict`, `Path=/api/v1/auth`) | Mantener la sesión que la persona ha pedido al entrar | **Estrictamente necesaria** |
| `sessionStorage` `terrenario_at` | Navegador | Token de acceso de la sesión en curso; muere al cerrar la pestaña | **Estrictamente necesaria** |
| `sessionStorage` `pkce_code_verifier` | Navegador | Verificador PKCE del intercambio OAuth. Sin él el acceso no es seguro | **Estrictamente necesaria** (seguridad) |
| `sessionStorage` `oauth_state` | Navegador | Parámetro `state` anti-CSRF del retorno de Google | **Estrictamente necesaria** (seguridad) |
| `sessionStorage` `terrenario_post_login_redirect` | Navegador | Recordar a dónde iba quien abrió un enlace de invitación sin sesión | **Estrictamente necesaria** (funcional) |
| `localStorage` `terrenario:seen_invitations` | Navegador | No repetir el aviso de una invitación ya vista | **Estrictamente necesaria** (funcional) |
| `sessionStorage` `terrenario_login_flow` y `terrenario_login_started` | Navegador | Correlacionar el embudo de login (RN-020) | **Medición propia** — ver más abajo |
| `sessionStorage` `terrenario_session` (MVP-601) | Navegador | Identificador aleatorio de la sesión de navegador, dimensión mínima del embudo (RN-020) | **Medición propia** — ver más abajo |
| `sessionStorage` `terrenario_usage_marks` (MVP-602) | Navegador | Recordar qué hitos ya se han contado en esta sesión, para no contar una sesión como si fueran varias | **Medición propia** — ver más abajo |
| Google Identity (OIDC) | Servidor | Autenticación de acceso (RN-036) | **Estrictamente necesaria**: es el método de acceso que la persona elige |
| Tipografías e iconos | **Autoalojados** | Sistema de diseño | Sin transferencia a terceros |

> **`MVP-711` no añade ninguna fila a esta tabla**, y no por casualidad. El canal de sugerencias e
> incidencias necesita recordar dónde estaba quien reporta y qué petición le falló; ese contexto se
> guarda **en memoria de la pestaña** en vez de en `sessionStorage`, de modo que no hay nada nuevo que
> inventariar ni clasificar. Tampoco hay widget, script, iframe ni recurso de terceros: la CSP no se
> toca y `RN-042` sigue sin activarse.

### El matiz de la telemetría del embudo de login (RN-020)

`MVP-505` afirmó que «no hay analítica». **Es más exacto decir que no hay analítica de terceros ni
perfilado**: sí existe una medición propia del embudo de login (`MVP-105`, RN-020), que guarda un
identificador de flujo aleatorio en `sessionStorage` y emite tres eventos —pantalla vista, clic en
Google y abandono— para saber dónde se cae el acceso.

Por qué se concluye que **no requiere consentimiento**:

- Es **de primera parte**: no interviene ningún tercero y el dato no sale del sistema.
- **No contiene PII**: solo el nombre del evento y un identificador aleatorio, no vinculado a la
  cuenta (la traza de éxito y error se emite en servidor, no desde el cliente).
- **No hay seguimiento entre sitios ni perfilado**, ni se conserva más allá de la sesión: el
  identificador vive en `sessionStorage` y muere al cerrar la pestaña.
- Es **medición de audiencia estrictamente propia y agregada** de un único flujo, que es el supuesto
  que las autoridades europeas tratan como exento o de riesgo mínimo.

Queda registrado como decisión motivada, no como omisión. Si la medición creciera —más eventos, más
retención, o cualquier herramienta de terceros— dejaría de encajar en este supuesto y `RN-042`
obligaría a recabar consentimiento previo.

#### Qué cambia con `MVP-601` y por qué sigue encajando

`MVP-601` completa las dimensiones del embudo (`session_id`, `device_type`) y **conserva el resultado
en servidor** como contadores diarios (`telemetry_daily_counters`). Esto es exactamente el «más
eventos, más retención» que el párrafo anterior señalaba como límite, así que la evaluación se rehace
en vez de darse por hecha:

- Lo que se conserva son **cifras, no filas de evento**: un contador por día y por métrica
  («120 pantallas vistas el 6 de agosto»). **Ningún identificador se persiste**, ni el de sesión ni el
  de flujo, así que no hay nada que reidentificar, nada que exportar por portabilidad y nada que
  expurgar por supresión. Se descartó a propósito la alternativa de una tabla con una fila por evento,
  que sí habría sido un dato conservado.
- El `session_id` es **de sesión de navegador**: aleatorio, no derivado de la cuenta, en
  `sessionStorage`, y muere al cerrar la pestaña igual que el de flujo.
- El `device_type` se deriva de dos señales genéricas —puntero grueso y ancho de ventana—, no de la
  cadena de agente de usuario: agrupa, no distingue. No es huella de dispositivo.
- Sigue siendo **de primera parte, agregada y sin perfilado**, que es el supuesto de exención.

El límite se mantiene donde estaba: cualquier herramienta de terceros, cualquier identificador que
sobreviva a la sesión o cualquier medida a nivel de persona dejaría de encajar y `RN-042` exigiría
consentimiento previo.

#### Qué cambia con `MVP-602`: ya no se mide solo el acceso

`MVP-602` extiende la medición **más allá del embudo de login**, a cómo se usa el producto: entrada al
área autenticada, entrada al dashboard, pulsación de «Actualizar» y si cada widget se pudo mostrar. Es
el cambio de alcance más grande de la medición desde que existe, así que se evalúa entero:

- Lo que se conserva sigue siendo **solo recuentos diarios**, con el mismo diseño que `MVP-601`: no hay
  fila por evento ni identificador persistido. La pregunta que se puede responder es «cuántas sesiones
  abrieron el panel», nunca «quién lo abrió».
- La señal **no lleva usuario ni Workspace**, aunque el endpoint sea autenticado y el servidor los
  conozca. Es una decisión, no un descuido, y está sostenida por un test que fija el conjunto cerrado
  de campos de la traza.
- El identificador de sesión y la marca de hitos ya contados viven en `sessionStorage` y **mueren al
  cerrar la pestaña**. La marca existe justamente para **no** contar de más: sin ella, una sesión que
  entra ocho veces al dashboard parecería ocho.
- Sigue sin haber **perfilado, seguimiento entre sitios ni terceros**, y no se mide nada del contenido
  de la explotación: se mide el uso de la interfaz, no lo que se registra en ella.

Conclusión: la medición crece en superficie pero **no en poder de identificación**, que es la variable
de la que depende la exención. No se recaba consentimiento y queda registrado por qué.

Lo que **sí** obligaría a replantearlo, para que el límite no quede en una frase vaga: medir a nivel de
persona o de Workspace, conservar cualquier identificador más allá de la pestaña, medir el contenido
registrado, o incorporar cualquier herramienta de terceros.

**No hay publicidad, perfilado ni tecnologías de terceros.** Por eso el producto **no muestra banner
de cookies**: la guía de la AEPD es explícita en que el banner es para las tecnologías **no exentas**,
y mostrarlo cuando solo se usan las técnicas es una mala práctica que además normaliza el clic
automático.

Lo que sí hay es un **aviso de privacidad accesible** desde la aplicación y un panel donde la persona
puede consultar este inventario en cualquier momento. Si en el futuro se incorpora cualquier
tecnología no esencial, `RN-042` exige recabar consentimiento **antes** de activarla, con la opción
más protectora por defecto y revocable.

> **Decisión de diseño (MVP-505)**: las tipografías Inter, Plus Jakarta Sans y Material Symbols se
> **autoalojan** en vez de cargarse desde el CDN de Google. Servirlas desde un tercero transfiere la
> dirección IP de cada visitante a ese tercero sin base jurídica clara, que es justo el supuesto que
> obligaría a pedir consentimiento. Autoalojarlas **elimina el problema** en vez de gestionarlo, y de
> paso permite cerrar la CSP a `'self'`.
>
> **Extensión (MVP-710)**: el mismo criterio se aplica a los recursos de marca —favicon, iconos de
> aplicación, `manifest.webmanifest` e imagen de la tarjeta social—. Se generan y se sirven desde el
> propio origen; ni el documento ni el manifest apuntan a ningún dominio ajeno. Importa señalarlo
> porque la imagen social es un caso fácil de pasar por alto: **la piden los servidores de WhatsApp o
> de Facebook, no el navegador del visitante**, así que alojarla en un tercero no aparecería en
> ninguna herramienta de red de la propia página.

---

## Derechos del usuario (GDPR Art. 15-22)

| Derecho | Proceso |
|---------|---------|
| Acceso | Procedimiento DSAR con registro de solicitud y respuesta en plazo legal |
| Rectificación | Correccion de datos inexactos por solicitud del titular |
| Supresión (derecho al olvido) | Borrado/anonimizacion cuando proceda legalmente |
| Portabilidad | Exportacion estructurada en formato interoperable |
| Oposición al tratamiento | Evaluacion de base juridica y bloqueo del tratamiento cuando corresponda |
| Limitacion del tratamiento | Marcado de restriccion temporal en sistemas afectados |

Plazo de referencia operativo para respuesta a derechos: 1 mes (prorrogable en casos complejos con justificacion).

---

## Checklist obligatorio por ticket/feature con datos personales

- [ ] Identificado si hay datos personales (si/no, con evidencia)
- [ ] Identificada base juridica del tratamiento
- [ ] Verificado principio de minimizacion
- [ ] Definida retencion y borrado/anonimizacion
- [ ] Verificado impacto en derechos del titular
- [ ] Verificado si aplica EIPD
- [ ] Verificado si aplica LSSI-CE / ePrivacy (cookies, comunicaciones)
- [ ] Actualizada documentacion funcional/tecnica de cumplimiento

---

## Lo que NO hacer con datos PII

- No loguear PII en logs de aplicación o errores
- No incluir PII en URLs (query params o paths)
- No almacenar datos financieros sensibles en claro; usar tokenización cuando aplique
- No enviar PII en mensajes de error devueltos al cliente
- No incluir PII en los tests (usar datos sintéticos)

---

## Nota de gobernanza

Este documento es normativa interna de cumplimiento del proyecto. No sustituye asesoramiento juridico profesional, pero su cumplimiento es obligatorio para todo el equipo.
