---
id: "ADR-0010"
titulo: "Envío de email transaccional por SMTP genérico en el MVP"
estado: aceptada
fecha: "2026-07-24"
decisores: ["@andres"]
etiquetas: ["email", "invitaciones", "integraciones", "infraestructura"]
---

# ADR-0010 — Envío de email transaccional por SMTP genérico en el MVP

## Estado

`aceptada`

## Contexto

MVP-103 introduce el primer correo transaccional del producto: la invitación a un Workspace
(RN-035). La KB ya preveía un `email-service` en `../componentes.md` y en
`../../06-integraciones/vision-general.md`, pero nunca fijó proveedor ni, sobre todo, **qué hay que
configurar y provisionar para que un correo salga de verdad**.

El estado previo a esta decisión era un hueco silencioso: el código tenía el puerto de envío, la
documentación decía "proveedor pendiente" y no existía en ninguna parte la cuenta de envío
(servidor, credenciales, remitente) ni su gestión como secreto. Un entorno podía desplegarse
"correctamente" y no enviar ni una sola invitación sin que nada lo advirtiese.

Condicionantes del MVP:

1. El volumen esperado es bajo: invitaciones puntuales entre miembros de una explotación agrícola.
2. No hay proveedor contratado y la elección depende de negocio, no de ingeniería.
3. El dominio remitente definitivo tampoco está decidido (ver "Pendiente" más abajo).
4. La invitación tiene un canal alternativo real: el enlace compartible. El correo acelera el
   flujo, pero no es la única vía.

## Decisión

**El MVP envía el correo transaccional por SMTP genérico**, con MailKit como cliente, y toda la
cuenta de envío se configura por la sección `Email` sin tocar código.

| Clave | Fuera del repositorio | Descripción |
|---|---|---|
| `Email:Host` | **sí** | Servidor SMTP. Vacío = sin cuenta configurada |
| `Email:Port` | no | `587` con STARTTLS, `465` con TLS implícito |
| `Email:SecurityMode` | no | `starttls`, `ssl`, `none` (solo relay local) o `auto` |
| `Email:Username` | **sí** | Usuario de autenticación SMTP |
| `Email:Password` | **sí** | Contraseña o contraseña de aplicación |
| `Email:FromAddress` | **sí** | Remitente. Vacío = sin cuenta configurada |
| `Email:FromName` | no | Nombre visible del remitente |

**Toda la identidad de la cuenta vive fuera del repositorio**, no solo la contraseña: este
repositorio es público y `Host`, `Username` y `FromAddress` identifican una cuenta concreta de un
servicio de terceros que, una vez commiteada, queda en el historial de git de forma permanente.
`appsettings.json` mantiene esas claves vacías para documentar la forma de la sección; los valores
van a User Secrets en local y al Secret Manager del proveedor por entorno. Poner ahí una cuenta
concreta tiene además un segundo efecto: `appsettings.json` es la base de **todos** los entornos, así
que un sandbox de desarrollo commiteado se hereda en producción y se traga el correo real.
| `Email:TimeoutSeconds` | no | Tiempo máximo de conexión y envío |

Consecuencias directas de la decisión:

1. SMTP es el mínimo común denominador: Google Workspace, Brevo, Amazon SES, SendGrid, Mailgun y
   cualquier servidor corporativo lo hablan. **Cambiar de proveedor es cambiar configuración**, y
   la decisión de negocio no bloquea el desarrollo.
2. Si `Email:Host` o `Email:FromAddress` están vacíos, el sistema **no finge**: el arranque emite
   un warning, la invitación se emite igualmente y la API responde `email_sent: false` para que la
   interfaz ofrezca el enlace. Un entorno sin cuenta es visible, no silencioso.
3. El fallo de envío nunca invalida la invitación: el correo se manda después del commit.
4. El puerto `IInvitationEmailSender` se mantiene. Si más adelante interesa la API HTTP de un
   proveedor (webhooks de rebote, métricas de entrega), entra como adaptador nuevo sin tocar el
   caso de uso.

## Pendiente de decisión de negocio: cuenta remitente

La elección del remitente **no está cerrada**. Las dos rutas viables, con lo que exige cada una:

| Ruta | Configuración | Requisitos | Aptitud |
|---|---|---|---|
| Dominio propio (`no-reply@terrenario.com`) | SMTP del proveedor transaccional elegido | Verificar el dominio y publicar **SPF**, **DKIM** y **DMARC** en su DNS | Única ruta válida para producción |
| Cuenta Google Workspace / Gmail existente | `smtp.gmail.com:587` con STARTTLS | **Contraseña de aplicación** (nunca la del usuario) y 2FA activo; límite de envío diario de la cuenta | Válida para desarrollo y arranque, no para producción |
| Bandeja de pruebas (Mailtrap o similar) | SMTP sandbox del servicio | Credenciales de la propia bandeja | Solo desarrollo: no entrega a nadie, por lo que no vale para validar entrega real |

Sin SPF/DKIM alineados, las invitaciones acaban en spam con alta probabilidad. Al cerrar la ruta
hay que actualizar este ADR, `../../06-integraciones/vision-general.md` y las variables por entorno
de `../../05-infraestructura/entornos.md`.

## Alternativas consideradas

### Opción A: SMTP genérico con MailKit (elegida)

**Pros**: un solo adaptador para cualquier proveedor; no ata el MVP a una decisión de negocio aún
abierta; MailKit es el cliente estándar en .NET y maneja STARTTLS y TLS implícito correctamente.
**Contras**: añade una dependencia; sin webhooks no hay trazabilidad de rebotes ni de entregas.

### Opción B: `System.Net.Mail.SmtpClient`

**Pros**: sin dependencias nuevas.
**Contras**: Microsoft lo desaconseja explícitamente para desarrollo nuevo; su manejo de
STARTTLS es limitado y da problemas con proveedores modernos.

### Opción C: API HTTP de un proveedor concreto (Resend, SendGrid, Brevo...)

**Pros**: mejor trazabilidad de entrega, webhooks de rebote y reputación gestionada.
**Contras**: exige contratar la cuenta antes de poder desarrollar y ata el código a ese proveedor.
Queda como evolución natural cuando el volumen justifique la trazabilidad.

### Opción D: no enviar correo en el MVP y quedarse solo con el enlace compartible

**Pros**: cero infraestructura.
**Contras**: RN-035 exige los dos canales; el email es el que evita el paso manual de compartir.

## Consecuencias

### Positivas

- El desarrollo deja de estar bloqueado por una decisión de negocio pendiente.
- La ausencia de cuenta de envío es observable (warning al arrancar y `email_sent: false`) en lugar
  de degradarse en silencio.
- Provisionar un entorno es rellenar siete claves de configuración y un secreto.

### Negativas / Trade-offs

- Sin webhooks, un rebote (buzón inexistente, correo en spam) no se detecta: el emisor cree que
  salió porque el servidor lo aceptó. Mitigación disponible: compartir el enlace directamente.
- SMTP con autenticación básica obliga a custodiar una contraseña; en Gmail exige contraseña de
  aplicación y 2FA.
- Una dependencia más que mantener y vigilar en avisos de seguridad.

### Neutrales

- El proveedor de email es **encargado del tratamiento** a efectos de RGPD: al contratarlo hay que
  firmar el DPA correspondiente y registrarlo en `../../07-seguridad/privacidad-datos.md`.
- Si el envío de correo activa cumplimiento condicionado de LSSI/ePrivacy, se documenta en la
  historia correspondiente. La invitación es comunicación transaccional solicitada por un miembro,
  no comercial.

## Referencias

- `../componentes.md` — componente `email-service`
- `../../06-integraciones/vision-general.md` — catálogo de integraciones y plan de fallback
- `../../05-infraestructura/entornos.md` — variables y secretos por entorno
- `../../05-infraestructura/desarrollo-local.md` — configuración de la cuenta en local
- `../../09-desarrollos/epicas/MVP-001--identidad-y-contexto-seguro/MVP-103--invitaciones-por-email-y-enlace/tech-design.md`
  — diseño técnico que introduce el envío
