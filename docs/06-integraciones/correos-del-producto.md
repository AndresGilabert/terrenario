---
bloque: 06-integraciones
documento: correos-del-producto
actualizado_en: "2026-08-08"
---

# Correos del producto — inventario y maquetación

> Qué correos salen de Terrenario, quién los recibe y qué garantiza la plantilla común.
> Origen: `MVP-715`, que cierra `P-001` y `P-030` del registro de `MVP-999`.
> Ampliado por `MVP-711`, que añade el sexto: el del canal de feedback.

El transporte es común desde `MVP-206` (`SmtpMailer`, [ADR-0010](../02-arquitectura/decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md)).
Desde `MVP-715` también lo es **la composición**: todos los correos de este inventario se arman con
`ProductEmailTemplate` y ninguno construye marcado por su cuenta.

---

## Inventario

Son **seis**. `MVP-715` encontró cinco donde su spec decía «al menos cuatro» —contaba entre ellos unas
notificaciones de baja de cuenta que **no existen**, porque dar de baja la cuenta no envía ningún
correo; lo que sí sale por ese camino es el aviso de baja de Workspace, ya que cerrar la cuenta obliga
antes a resolver los Workspaces de propiedad única (RN-038)— y descubrió dos que nadie había contado:
los avisos de alerta de operación de `MVP-603`, que eran el único correo del producto sin maquetación
ninguna.

El sexto lo añade `MVP-711`: el reporte del **canal de sugerencias e incidencias**. Es el primero que
llega después de que exista este inventario, y por eso importa cómo llegó: por la plantilla común y
por el catálogo ejecutable, que es justo lo que `MVP-715` dejó pedido para cuando apareciera.

| Correo | Disparador | Destinatario | Contenido | Emisor |
|---|---|---|---|---|
| Invitación a Workspace | `POST /api/v1/workspaces/invitations` y su reenvío (`.../{id}/resend`) | La dirección invitada, que **puede no tener cuenta** | Quién invita y a qué Workspace, enlace de aceptación de un solo uso, caducidad, qué hacer si no la esperaba y que esa misma dirección sirve aunque no sea de Gmail (`MVP-712`) | `SmtpInvitationEmailSender` |
| Baja de Workspace | `POST /api/v1/workspaces/active/closure` | Cada miembro activo del Workspace que se da de baja | Quién lo dio de baja, que no se ha borrado nada y enlace de un solo uso para pedir traspaso y reactivación | `SmtpWorkspaceLifecycleEmailSender` |
| Solicitud de traspaso y reactivación | `POST /api/v1/workspaces/reactivations/{token}/request` | Quien dio de baja el Workspace | Quién lo pide y enlace a su bandeja de autorizaciones (autorizar exige entrar con su cuenta) | `SmtpWorkspaceLifecycleEmailSender` |
| Alerta de operación disparada | `AlertMonitor`, en su barrido de cada minuto | `Ops:AlertEmail` | Nombre y severidad de la alerta, detalle y runbook | `AlertNotifier` |
| Alerta de operación resuelta | `AlertMonitor`, al detectar la transición | `Ops:AlertEmail` | Nombre de la alerta, cuánto duró y detalle | `AlertNotifier` |
| Sugerencia o incidencia del usuario (MVP-711) | `POST /api/v1/feedback` desde «Sugerencias e incidencias» | `Feedback:Recipient` | Lo que ha escrito la persona, quién es y el contexto técnico: versión desplegada, pantalla, `X-Request-Id` del último fallo y navegador | `SmtpFeedbackEmailSender` |

Los tres primeros van a personas; los tres últimos, a buzones de operación. Se maquetan igual a
propósito: un correo del producto es un correo del producto, y en los de alerta el motivo del envío y
el modo de apagarlos son además información operativa útil para quien herede esa bandeja.

El del canal de feedback es el único que **lo dispara una persona** y el único cuyo cuerpo contiene
texto que escribe ella, que es exactamente el caso en el que olvidarse de escapar duele. Escapa la
plantilla, no el emisor, así que no hay nada que recordar.

**Sin cuenta de envío configurada no sale ninguno.** El arranque lo advierte y las invitaciones se
comparten por enlace (`email_sent: false`); ver el plan de fallback en
[vision-general.md](./vision-general.md).

---

## La plantilla común

`ProductEmailTemplate` (`Infrastructure/Email/`) impone la misma estructura a los seis:

- **Cabecera**: el nombre del remitente (`Email:FromName`) como texto, nunca como imagen.
- **Cuerpo**: un titular y los párrafos del correo.
- **Llamada a la acción**: como mucho una, y siempre acompañada del enlace en claro por si el botón
  no se renderiza.
- **Pie legal**: motivo del envío, forma de dejar de recibirlo e identificación del responsable.

Cada correo aporta **solo texto**. La plantilla es la única que sabe de HTML y, por tanto, la única
responsable de escapar: ningún emisor puede olvidarse de escapar un nombre de Workspace escrito por
una persona.

### Versión en texto plano

Las dos versiones —HTML y texto— salen del mismo contenido, así que no pueden decir cosas distintas.
El texto plano no es un descarte: es lo que ve quien lee el correo con un lector de pantalla o con un
cliente que no renderiza HTML.

### Sin imágenes ni recursos remotos

Ni imágenes, ni tipografías web, ni hojas de estilo externas. Dos motivos y los dos cuentan:

- El correo es la **única vía del producto hacia alguien que todavía no tiene cuenta**. Un cliente que
  bloquea remotos por defecto dejaría la invitación convertida en un hueco gris.
- Cualquier recurso remoto delata al servidor que lo aloja el momento exacto en que se abre el
  mensaje. Eso es seguimiento de apertura, y aquí no se ha pedido ni se quiere.

El aspecto sale de estilos en línea y de tipografías del sistema, que es lo único que respetan los
clientes de correo.

---

## Contenido legal

El pie que la plantilla añade a todos cubre lo que `P-001` pedía garantizar desde la plantilla y no
correo a correo (RGPD arts. 13 y 21, LSSI art. 10):

| Elemento | De dónde sale |
|---|---|
| Responsable del tratamiento: titular, NIF y domicilio | `src/frontend/terrenario-web/src/config/legal-entity.json` |
| Dirección de ejercicio de derechos | El mismo fichero (`privacyEmail`) |
| Enlace a la Política de Privacidad | `Legal:PrivacyPolicyUrl` en `appsettings.json` |
| Motivo del envío | Lo declara cada correo; la plantilla lo exige |
| Cómo dejar de recibirlo | Lo declara cada correo, cuando existe forma |

**La identidad tiene un solo origen.** Es el mismo fichero del que se alimentan la Política de
Privacidad y los Términos publicados; la API lo incrusta al compilar (`<EmbeddedResource>` en
`Terrenario.Api.csproj`) en vez de reescribir el NIF en C#. Es el criterio que ya se aplicaba a la
CSP, que la API lee del build del cliente en lugar de duplicarla. Cada campo se puede sobreescribir
por despliegue con `Legal:*`; un valor en blanco cae al versionado, igual que en el cliente.

Sobre el «cómo dejar de recibirlo», los seis no son iguales y **decirlo importa**:

- **Invitación**: es el único que llega a quien no es usuario, así que no puede ofrecer «sal del
  Workspace». Dice que no hay ninguna lista, que no se vuelve a escribir si no se acepta y que para
  que la dirección deje de constar basta escribir a la dirección de derechos del pie.
- **Baja de Workspace** y **solicitud de reactivación**: son avisos imprescindibles del servicio y no
  se pueden desactivar. Se dice tal cual, en vez de ofrecer una baja que no existe.
- **Alertas**: se apagan retirando la dirección de `Ops:AlertEmail`.
- **Sugerencias e incidencias**: se apagan retirando la dirección de `Feedback:Recipient`, con la
  consecuencia dicha: el canal deja de existir para quien lo intente usar.

---

## Cómo revisar un correo

`ProductEmailPreviewTests` escribe el HTML y el texto plano de los seis en `artifacts/correos/` cada
vez que corre la suite:

```bash
dotnet test src/backend/Terrenario.sln --filter FullyQualifiedName~ProductEmailPreview
```

Los ficheros salen del mismo código que compone los correos que se envían, así que lo que se
inspecciona es el correo y no una maqueta parecida. La carpeta está fuera del control de versiones:
es salida reproducible, no contenido del proyecto.

Lo transversal —pie legal, motivo del envío, versión en texto plano y ausencia de recursos remotos—
lo comprueba `ProductEmailInventoryTests` sobre el inventario entero, no correo a correo. Un correo
nuevo que no entre en `ProductEmailCatalog` se queda sin esas garantías comprobadas, que es la forma
de que el olvido se note.

### Y para ver el envío, no solo el cuerpo

El HTML no cubre lo único que no puede romperse en el envío: el sobre. Que salga
`multipart/alternative` y no solo HTML, el juego de caracteres, la codificación de los acentos y el
`From`/`Subject` tal y como los ve la bandeja.

Para eso está la **bandeja de pruebas de Mailtrap**, que el proyecto usa en local y que está
configurada en `Email:*` de user-secrets: los correos se capturan sin entregarse a nadie y se ven en
su web, con previsualización por cliente. El montaje está en
[desarrollo-local.md](../05-infraestructura/desarrollo-local.md#opción-recomendada-en-local-bandeja-de-pruebas-mailtrap).

> Este enlace existe porque faltaba. En `MVP-715` se construyó un receptor SMTP casero para hacer
> exactamente lo que Mailtrap ya hacía, por no haber leído la guía de desarrollo local; el receptor se
> retiró en el arreglo de `P-106`. Si estás aquí buscando cómo probar un envío, la respuesta es
> Mailtrap y no hace falta escribir nada.

---

## Lo que no hay, y por qué

- **Aviso de invitación anulada** (`P-039`): descartado por el Product Owner. Basta con que se avise
  al intentar unirse, que es lo que ya hace el preview del enlace.
- **Notificaciones in-app** (`P-011`, `P-029`): en backlog post-MVP.
- **Correo de la baja de cuenta**: no existe hoy. Si `MVP-505` acaba necesitando uno, entra por esta
  plantilla como los demás.
- **Acuse de recibo al usuario que reporta** (`MVP-711`): el canal no devuelve un correo de «hemos
  recibido tu mensaje». La confirmación se da **en pantalla**, que es donde está la persona en ese
  momento; un correo automático añadiría un séptimo envío para decir lo que ya se ha dicho.
- **Seguimiento de aperturas, plataforma de envío o marketing**: fuera de alcance por decisión de
  producto, y las dos primeras incompatibles con la regla de no usar recursos remotos.
