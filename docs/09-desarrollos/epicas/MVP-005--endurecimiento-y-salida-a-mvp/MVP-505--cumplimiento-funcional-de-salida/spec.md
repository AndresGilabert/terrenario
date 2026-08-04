---
id: "MVP-505"
tipo: feature
titulo: "Cumplimiento funcional de salida: páginas legales, consentimiento y baja de cuenta"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "5d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-106", "MVP-206"]
bloquea: ["MVP-503", "MVP-504"]
relacionado_con: ["MVP-502", "MVP-299"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["cumplimiento", "privacidad", "identidad"]
  modulo_path: "03-modulos/"
  componentes: ["legal-pages", "consent", "account-closure", "retention"]
  etiquetas: ["mvp", "legal", "rgpd", "release-blocker"]
  nivel_riesgo: alto
creado_en: "2026-07-28"
actualizado_en: "2026-08-03"
---

# MVP-505 — Cumplimiento funcional de salida: páginas legales, consentimiento y baja de cuenta

## Contexto

`MVP-005` cierra el MVP con tres historias de **revisión** —tests (`MVP-501`), hardening técnico
(`MVP-502`) y checklist documental de cumplimiento (`MVP-503`)— más el gate final (`MVP-504`). La
revisión de cierre de `MVP-002` (`MVP-299`, 3ª pasada, 2026-07-28) detectó que eso deja un hueco:
**tres obligaciones de cumplimiento que ninguna de esas historias puede resolver porque son
capacidades funcionales, no revisiones**.

- **`P-008` — Páginas legales y consentimiento.** Los enlaces a Política de Privacidad y Términos del
  Servicio del login (`LoginPage.tsx`) apuntan hoy a rutas inexistentes: `MVP-106` corrigió el
  comportamiento roto, no creó el contenido. Tampoco hay consentimiento de cookies. El marco existe
  solo como documentación interna (`docs/07-seguridad/privacidad-datos.md`).
- **`P-024` — Baja de cuenta (derecho de supresión).** `MVP-206` entregó la **regla de no-orfandad**
  (`WorkspaceOwnershipGuard.EnsureAccountClosureAllowedAsync` y
  `GET /api/v1/workspaces/ownership-obligations`), explícitamente como «punto de enganche» para una
  historia futura que nunca se creó. Hoy no existe forma de que una persona elimine su cuenta.
- **`P-033` — Retención y expurgo.** Los Workspaces dados de baja permanecen indefinidamente por
  diseño (RN-039), y desde la reformulación de `RN-037` los registros operativos eliminados también.
  «No se borra nada» necesita una política de cuánto tiempo.

El resultado sin esta historia es que `MVP-503` **detectaría** el incumplimiento y `MVP-504`
**bloquearía** la salida, sin ninguna historia que lo resuelva. Decisión del PO (2026-07-28): se crea
esta historia dentro de `MVP-005`, como «corrección bloqueante» en el sentido de sus propias notas.

## Objetivo

Cerrar las obligaciones de cumplimiento que el MVP necesita para exponerse a usuarios reales: que la
persona pueda leer a qué se compromete, decidir sobre cookies no esenciales y borrar su cuenta, y que
el sistema tenga una política declarada de cuánto conserva lo que no borra.

## Requisitos de usuario

### HU-1 — Saber a qué me comprometo antes de entrar

**Como** persona que va a acceder por primera vez,
**quiero** poder leer la política de privacidad y los términos del servicio desde el propio login,
**para** decidir con información y no sobre enlaces rotos.

### HU-2 — Decidir sobre las cookies no esenciales

**Como** visitante,
**quiero** que se me pida consentimiento antes de activar cualquier cookie o tecnología no esencial,
**para** que mi decisión se respete y quede registrada.

### HU-3 — Borrar mi cuenta

**Como** persona usuaria,
**quiero** poder eliminar mi cuenta y mis datos personales desde la aplicación,
**para** ejercer mi derecho de supresión sin tener que escribir a nadie.

### HU-4 — Saber cuánto se conserva lo que doy de baja

**Como** responsable del producto,
**quiero** una política declarada de retención y expurgo de lo dado de baja,
**para** que «no se borra nada» no signifique «se guarda para siempre sin criterio».

## Alcance (in-scope)

- **Páginas legales** de Política de Privacidad y Términos del Servicio, con contenido validado y
  accesibles desde el login, la landing y la aplicación, sustituyendo los enlaces rotos actuales.
- **Consentimiento de cookies / tecnologías no esenciales**, con decisión persistente y revocable, y
  la opción más protectora por defecto.
- **Baja de cuenta**: confirmación explícita, borrado o anonimización de los datos personales de la
  cuenta, revocación de sesiones y refresh tokens, y resolución previa de las obligaciones de
  propiedad **reutilizando** el punto de enganche de `MVP-206` (`RN-038`), sin reimplementar la regla.
- **Política de retención y expurgo** de Workspaces dados de baja, registros operativos eliminados
  (`deleted_at`, RN-037), solicitudes de reactivación cerradas o caducadas e invitaciones terminales:
  declarada en la KB y, donde proceda, con el mecanismo que la aplique.
- Alta de las reglas de negocio que salgan de aquí (retención, consentimiento) en
  `docs/01-producto/reglas-de-negocio.md`.

## Fuera de alcance (out-of-scope)

- Redacción jurídica externa o revisión por asesoría legal: esta historia entrega el contenido y el
  mecanismo; la validación legal es de negocio.
- Portabilidad y exportación de datos, y el resto de derechos ARCO más allá de la supresión.
- Gestión de consentimiento por finalidades múltiples o CMP de terceros.
- Inventario y maquetación unificada de los emails salientes (`P-001`/`P-030`), que sigue en
  `MVP-999`. Aquí solo entra el correo que la baja de cuenta necesite.
- Hardening técnico de seguridad y PII, que es `MVP-502`.

## Criterios de aceptación

- [x] **CA-1**: La Política de Privacidad y los Términos del Servicio existen como páginas del
  producto y son alcanzables desde el login, la landing y la aplicación; no queda ningún enlace legal
  apuntando a una ruta inexistente.
- [x] **CA-2**: Antes de activar cualquier cookie o tecnología no esencial se pide consentimiento; la
  opción por defecto es la más protectora y la decisión se puede revocar después.
- [x] **CA-3**: Una persona puede eliminar su cuenta desde la aplicación con confirmación explícita;
  tras la baja no puede iniciar sesión, sus sesiones y refresh tokens quedan revocados y sus datos
  personales quedan borrados o anonimizados.
- [x] **CA-4**: La baja de cuenta **no deja Workspaces huérfanos**: si hay obligaciones de propiedad
  sin resolver, el sistema las expone y bloquea la baja hasta resolverlas, reutilizando la guarda de
  `MVP-206` (`RN-038`) sin duplicar la regla.
- [x] **CA-5**: Existe una política de retención y expurgo declarada en la KB para lo dado de baja y
  lo eliminado lógicamente, con plazo explícito, y `MVP-503` puede verificarla contra el sistema.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de
> requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| LoginPage | docs/07-seguridad/privacidad-datos.md | falta | Los enlaces legales existen pero apuntan a rutas inexistentes (`P-008`) |
| AjustesView | RN-038, docs/07-seguridad/privacidad-datos.md | falta | No hay baja de cuenta; el prototipo tampoco la contempla (`P-024`) |
| Landing / shell | docs/07-seguridad/privacidad-datos.md | falta | No hay consentimiento de cookies ni acceso a las páginas legales |

## Notas y decisiones

- **Origen.** Resuelve `P-008`, `P-024` y `P-033` de `MVP-999`, registrados desde `MVP-001` y
  `MVP-002` con destino «`MVP-005`/`MVP-502`» pero **sin encaje real** en ninguna de sus historias:
  `MVP-502` es hardening técnico, `MVP-503` es revisión documental con «nuevas políticas» fuera de
  alcance y `MVP-504` es el gate. Detectado en la 3ª pasada de `MVP-299` (2026-07-28).
- **Por qué dentro de `MVP-005` y no en `MVP-999`.** La épica dice que no debe usarse para introducir
  capacidades funcionales «salvo correcciones bloqueantes», y estas lo son: sin páginas legales ni
  derecho de supresión, el checklist de `MVP-503` no puede darse por cumplido y `MVP-504` no puede
  autorizar la salida. Dejarlo en `MVP-999` equivaldría a salir a usuarios reales con un
  incumplimiento conocido.
- **Reutilizar, no reimplementar.** `MVP-206` dejó la regla de no-orfandad implementada y probada; la
  baja de cuenta **la llama**. Es la condición que `P-024` fijó al registrarse.
- **Relación con `RN-037`.** La reformulación de `RN-037` en la 3ª pasada de `MVP-299` (la eliminación
  de registros operativos pasa a ser lógica) aumenta la superficie de esta historia: ahora también hay
  registros operativos conservados indefinidamente, no solo Workspaces.
- **Dependencia con `MVP-503`.** Esta historia debe entregarse **antes** de la revisión de
  cumplimiento, no después: `MVP-503` verifica, no construye.

## Resultado de la entrega (2026-07-31)

Diseño técnico completo en [tech-design.md](./tech-design.md). `P-008`, `P-024` y `P-033` cerrados.

| Suite | Antes | Después |
|---|---|---|
| Backend | 654 | **666** |
| Frontend | 81 | **87** |

### CA-1 — Páginas legales

Política de Privacidad y Términos del Servicio como páginas **públicas** (`/legal/privacidad`,
`/legal/terminos`): se leen antes de entrar, que es cuando hacen falta. Enlazadas desde login, landing
y Ajustes. Los botones deshabilitados con «próximamente» del login desaparecen: `MVP-106` arregló el
enlace roto, no la falta de contenido.

El contenido describe **lo que el sistema hace de verdad** —tratamientos, bases jurídicas, encargados
y plazos salen de `privacidad-datos.md`—, no una plantilla genérica. Los datos del responsable van
como **marcadores visibles** (decisión del PO) y la página avisa de que está pendiente de completar.

### CA-2 — Consentimiento: por qué **no** hay banner

Se inventariaron todas las tecnologías de almacenamiento y terceros. **Todas son estrictamente
necesarias**: cookie de sesión, token de la pestaña, recordatorio de avisos vistos e inicio de sesión
con Google. No hay analítica, publicidad ni perfilado.

Quedaba **una** excepción real: las tipografías se cargaban del CDN de Google, lo que transfiere la IP
de cada visitante a un tercero sin base jurídica clara —justo el supuesto que obligaría a pedir
consentimiento—. En vez de gestionarlo con un banner, **se elimina**: las tipografías se autoalojan.
Eso evita degradar la experiencia de todo visitante, mantiene la fidelidad al sistema de diseño y
permite además **cerrar la CSP a `'self'`**.

Sin ninguna tecnología no esencial, mostrar un banner sería **peor** cumplimiento: la guía de la AEPD
lo reserva para las tecnologías no exentas, y enseñarlo cuando solo se usan las técnicas normaliza el
clic automático sin proteger nada. Lo que se entrega es lo que la norma sí exige: **informar**, con un
panel de privacidad en Ajustes que lista el inventario. `RN-042` deja escrita la obligación de recabar
consentimiento previo, con la opción más protectora por defecto y revocable, el día que entre algo no
esencial.

> **Esta es la decisión de la historia que conviene revisar expresamente**, porque a primera vista
> parece que falta un banner. Está razonada en el tech-design y es revisable en `MVP-503`.

### CA-3 y CA-4 — Baja de cuenta

Anonimización inmediata + purga diferida (decisión del PO). Al confirmar desaparecen **ya** el nombre,
el correo y el identificador de Google: en la cuenta, en los maestros de responsables de sus
Workspaces (RN-036) y en las invitaciones pendientes dirigidas a su correo. Se revocan todas las
sesiones y se borra la cookie de refresco.

Lo decisivo: el `google_sub` deja de coincidir con el de Google, así que **volver a entrar con la
misma cuenta crea una cuenta nueva y vacía**. Eso separa una supresión de una desactivación.

La fila sobrevive anonimizada porque cada actividad, cosecha y compra guarda quién la registró:
borrarla dejaría sin autoría el histórico operativo de **terceros**. Ya no identifica a nadie.

**La regla de no-orfandad se llama, no se reimplementa** (CA-4): `WorkspaceOwnershipGuard` de
`MVP-206`, que se dejó implementada y probada como punto de enganche de esta historia. Era la
condición con la que se registró `P-024`. Si quedan Workspaces de propiedad única, la baja responde
`422` y la pantalla los lista con su salida.

La confirmación es **una frase tecleada**, no un clic, y se comprueba también en servidor.

### CA-5 — Retención y expurgo

`RN-041` extiende los 24 meses que ya regían para «cuenta cancelada» a todo lo que el producto
conserva por diseño y no tenía plazo: Workspaces de baja (RN-039), registros operativos eliminados
(RN-037), solicitudes de reactivación cerradas e invitaciones terminales. El plazo vive **también en
código** y la respuesta de la baja devuelve la fecha concreta de purga, para que `MVP-503` pueda
verificarlo contra el sistema en vez de leerlo.

### Verificación

- Backend: **666 tests** (654 antes), 12 de ellos de la baja contra API y PostgreSQL reales,
  comprobando **qué queda escrito** y no solo el código de respuesta.
  **Incidencia de entorno resuelta**: al cerrar la historia, Smart App Control de Windows bloqueó el
  ensamblado de Testcontainers y la suite cayó a 156 fallos sin ningún cambio de código. El PO
  desactivó la protección y la suite **vuelve a pasar entera (666)**, lo que confirma que ninguno de
  esos fallos era de lógica. La causa de fondo —el arnés depende de un ensamblado que cualquier
  política de Application Control puede bloquear— queda registrada como `P-069` para el gate de
  `MVP-504`.
- Frontend: **87 tests** (81 antes); build, lint y `npm audit` limpios.
- En navegador: páginas legales, enlaces vivos en login y landing, panel de privacidad, panel de baja
  correctamente bloqueado con datos reales, y **cero peticiones a dominios de Google**.

### Lo que esta historia deja abierto (para `MVP-504`)

1. **Los marcadores del contenido legal**: solo el negocio puede rellenarlos. Hasta entonces las
   páginas existen y son revisables, pero **no son publicables**.
2. **La programación del expurgo**: la política, el plazo y el cálculo están; ejecutarlo
   periódicamente es una decisión de infraestructura.
