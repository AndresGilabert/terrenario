---
id: "MVP-712"
tipo: feature
titulo: "Acceso con cualquier direccion de correo"
estado: borrador
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
actualizado_en: "2026-08-07"
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

- [ ] **CA-1**: El login explica que sirve cualquier direccion dada de alta como Cuenta de Google, con
  enlace al alta.
- [ ] **CA-2**: La landing publica dice lo mismo antes de pedir nada.
- [ ] **CA-3**: El aviso de aptitud por correo no coincidente explica la salida, no solo el problema.
- [ ] **CA-4**: El correo de invitacion lo menciona.
- [ ] **CA-5**: `RN-036` sigue intacta: no se anade ningun proveedor.

## Maquetas y referencias visuales

- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| LoginPage | RN-018, RN-036 | parcial | El acceso funciona; el texto excluye sin querer |

## Notas y decisiones

- Es la historia mas barata de la epica y probablemente la que mas usuarios recupera: el coste de que
  alguien crea que no puede entrar es que no entra, y nadie se entera.
