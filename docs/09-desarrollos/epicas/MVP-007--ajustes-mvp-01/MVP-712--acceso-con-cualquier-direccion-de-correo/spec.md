---
id: "MVP-712"
tipo: feature
titulo: "Acceso con cualquier direccion de correo"
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
  dominios: ["identidad", "ux", "contenido"]
  modulo_path: "03-modulos/"
  componentes: ["login", "landing", "invitaciones"]
  etiquetas: ["mvp", "ajustes", "acceso"]
  nivel_riesgo: bajo
creado_en: "2026-08-07"
actualizado_en: "2026-08-08"
---

# MVP-712 — Acceso con cualquier direccion de correo

> **Origen**: `P-089` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

`RN-018` y `RN-036` fijan Google OIDC como unico proveedor del MVP, asi que la limitacion es una
decision y no un defecto. Pero tiene dos consecuencias que si son de producto:

**(a)** Un correo de Hotmail, Outlook o cualquier otro **si puede** usar la aplicacion creando una
Cuenta de Google con esa direccion, y ni el login ni la landing lo dicen. «Gestiona tu finca de forma
sencilla. Sin contrasenas complicadas, accede directamente con tu cuenta de Google» se lee como
«necesitas un Gmail».

**(b)** Invitar a una direccion que no tenga Cuenta de Google es un **callejon sin salida**: la
comprobacion de aptitud exige coincidencia de correo y responde «Esta invitacion esta dirigida a otra
cuenta de correo. Entra con esa cuenta para aceptarla», sin decir que la salida es dar de alta esa
direccion en Google.

## Objetivo

Que nadie se quede fuera por creer que necesita un Gmail, sin tocar el modelo de identidad del MVP.

## Requisitos de usuario

### HU-1 — Entender que mi correo sirve

**Como** persona con una direccion que no es de Gmail,
**quiero** saber que puedo usar la aplicacion con ella,
**para** no descartarla antes de intentarlo.

### HU-2 — Salir del callejon de la invitacion

**Como** persona invitada en una direccion sin Cuenta de Google,
**quiero** que se me diga que hacer,
**para** poder aceptar la invitacion en vez de abandonarla.

## Alcance (in-scope)

- Texto en el **login** que aclare que sirve cualquier direccion de correo dada de alta como Cuenta de
  Google, con enlace al alta.
- Lo mismo en la **landing** publica, donde se decide si probar el producto.
- Ampliacion del mensaje de **aptitud de invitacion** (`email_mismatch`) para que explique la salida
  cuando el motivo probable es que la direccion no sea una Cuenta de Google.
- Mencion en el correo de invitacion, que es donde llega la persona por primera vez.

## Fuera de alcance (out-of-scope)

- **Segundo proveedor de identidad** (Microsoft u otro): rompe `RN-036`, obliga a decidir vinculacion
  de cuentas y a rehacer la aptitud de invitaciones. Queda en backlog como evolucion.
- Passkeys (`RN-019`), que ya estan declaradas como fase futura.
- Enlace magico por correo.

## Criterios de aceptación

- [x] **CA-1**: El login explica que sirve cualquier direccion dada de alta como Cuenta de Google, con
  enlace al alta. Bajo el boton «Continuar con Google», separado por una linea: «No hace falta que tu
  correo sea de Gmail: sirve el de Hotmail, Outlook o el de tu cooperativa, siempre que des de alta
  esa misma direccion como Cuenta de Google. Es gratis y no crea un buzon nuevo», con el enlace «Dar
  de alta mi direccion como Cuenta de Google» a `https://accounts.google.com/signup`. Cubierto por
  tres pruebas en `LoginPage.test.tsx` (el texto, que no promete que valga cualquier correo sin mas, y
  que el alta es un enlace en pestana nueva con `rel="noreferrer"`).

- [x] **CA-2**: La landing publica dice lo mismo antes de pedir nada. La frase va **bajo el CTA del
  hero**, antes de los beneficios y de cualquier peticion, precedida de «Se entra con una Cuenta de
  Google.» y con el mismo enlace. El texto es literalmente el mismo del login porque sale de una
  constante compartida (`lib/google-account.ts`): si cada pantalla lo redactase por su cuenta, la que
  se quedara corta volveria a dejar fuera a quien esta historia recupera. Cubierto por
  `LandingPage.test.tsx` (nuevo; la landing no tenia cobertura).

- [x] **CA-3**: El aviso de aptitud por correo no coincidente explica la salida, no solo el problema.
  `email_mismatch` pasa a decir: «Esta invitacion esta dirigida a otra cuenta de correo. Entra con esa
  cuenta para aceptarla. Si esa direccion todavia no esta dada de alta como Cuenta de Google, dala de
  alta con ella —no hace falta que sea un Gmail— y vuelve a abrir este enlace», con el enlace al alta
  a continuacion. El enlace **solo** acompana a este motivo (`shouldOfferGoogleSignup`): en una
  invitacion caducada, anulada o ya usada, darse de alta no arregla nada. Cubierto por cinco pruebas
  en `invitation-ui.test.ts`.

- [x] **CA-4**: El correo de invitacion lo menciona. Nota nueva justo debajo de la llamada a la
  accion: «Esta misma direccion sirve, sea o no de Gmail: solo tiene que estar dada de alta como
  Cuenta de Google. Si todavia no lo esta, puedes darla de alta gratis en
  `https://accounts.google.com/signup` y aceptar la invitacion con ella». Se compone en
  `InvitationEmailComposer` a traves de `ProductEmailTemplate` (`MVP-715`): no se reintroduce
  composicion ad-hoc y la direccion viaja como **texto**, no como segundo boton. Cubierto por dos
  pruebas en `InvitationEmailComposerTests` y visible en `artifacts/correos/invitacion-a-workspace.*`,
  que los tests de vista previa regeneran.

- [x] **CA-5**: `RN-036` sigue intacta: no se anade ningun proveedor. No se ha tocado ni un fichero
  del flujo de autenticacion —`auth.service.ts`, `OAuthCallback.tsx`, `Application/Auth`,
  `Infrastructure/Auth`— ni la aptitud de invitacion en servidor (`PreviewInvitationHandler`,
  `WorkspaceInvitation`). El diff es texto, un modulo de constantes y sus pruebas. En
  `reglas-de-negocio.md` se anade una **aclaracion** a `RN-036` (Cuenta de Google no es lo mismo que
  direccion de Gmail) que no altera el enunciado de la regla.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| LoginPage | RN-018, RN-036 | hecho | El aviso y el enlace al alta bajo el boton de Google; 3 pruebas en `LoginPage.test.tsx` |
| LandingPage | RN-018, RN-036, RN-042 | hecho | Mismo texto bajo el CTA del hero, como enlace y no como recurso; 3 pruebas en `LandingPage.test.tsx` |
| AcceptInvitationPage | RN-035, RN-036 | hecho | `email_mismatch` explica la salida y ofrece el alta; 5 pruebas en `invitation-ui.test.ts` |
| Correo de invitacion | RN-035, RN-036 | hecho | Nota nueva bajo la llamada a la accion; 2 pruebas en `InvitationEmailComposerTests` y vista previa regenerada |

## Notas y decisiones

- Es la historia mas barata de la epica y probablemente la que mas usuarios recupera: el coste de que
  alguien crea que no puede entrar es que no entra, y nadie se entera.
- **No se ramifica el aviso de `email_mismatch`.** El motivo tiene dos causas —entrar con otra cuenta
  que si se tiene, o no tener ninguna en la direccion invitada— y no se pueden separar: el preview
  **no revela** el correo destinatario a proposito (quien abre el enlace no siempre es la persona
  invitada), asi que averiguarlo seria ademas filtrarlo. El mensaje nombra las dos salidas.
- El texto **no dice «cualquier correo vale»**: dar de alta la direccion en Google es un paso real que
  la persona tiene que dar, y prometer lo contrario cambiaria un abandono silencioso por una promesa
  incumplida.
