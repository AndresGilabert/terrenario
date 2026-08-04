---
bloque: 07-seguridad
documento: privacidad-datos
actualizado_en: "2026-08-04"
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
| Google (OIDC) | `sub`, nombre, email | Autenticacion de acceso | Activo. **Contrato de encargo por verificar** (`MVP-504`, B-2) |
| Arsys | Email del destinatario, nombre de quien invita y del Workspace | Envio de invitaciones a Workspace | Proveedor decidido (`MVP-504`, B-1). **Sin contratar**: ver [ADR-0010](../02-arquitectura/decisiones/ADR-0010--envio-de-email-transaccional-por-smtp.md) |
| Microsoft Azure | Todo lo almacenado | Alojamiento de la aplicacion y la base de datos | Proveedor decidido (`MVP-504`, B-1). **Sin contratar** (B-2) |

Decidir el proveedor **no cierra** la obligacion del art. 28: el contrato de encargo con cada uno de
los tres sigue siendo el bloqueo `B-2` del gate de salida.

### Transferencias internacionales

| Via | Destino | Garantia |
|-----|---------|----------|
| Alojamiento (Azure) | Region **Espana** | Sin transferencia: los datos se almacenan en la UE |
| Correo (Arsys) | Espana | Sin transferencia |
| Inicio de sesion (Google) | EE. UU. | **Si hay transferencia**: clausulas contractuales tipo de la Comision Europea y decision de adecuacion del Marco de Privacidad de Datos UE-EE. UU. |

La transferencia a Google es **inevitable mientras el acceso sea con Google** (`RN-036`): no hay
alternativa que ofrecer a quien no la acepte, mas alla de no crear la cuenta. Queda declarada en la
Politica de Privacidad en vez de omitirse.

### Identidad del responsable

Los datos publicados en las paginas legales viven en un solo sitio,
`src/frontend/terrenario-web/src/config/legal-entity.ts`, y cada campo admite override por variable
de entorno `VITE_LEGAL_*`. Estan versionados a proposito: la LSSI obliga a publicarlos, asi que no
hay nada que proteger, y un `.env` no llega al despliegue.

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

**Los datos personales no esperan a ese plazo.** La baja de cuenta los borra o anonimiza en el acto
—nombre, correo e identificador del proveedor de identidad, tanto en la cuenta como en los maestros de
sus Workspaces y en las invitaciones que la nombraban—. Lo que se conserva 24 meses es la **fila
anonimizada**, que ya no identifica a nadie y solo sostiene la autoría del histórico operativo.

El plazo vive también en código (`AccountRetentionPolicy`) para que sea verificable y no solo
declarado: la respuesta de la baja devuelve la fecha de purga concreta.

> **Pendiente de despliegue**: la rutina que ejecuta el expurgo al vencer el plazo necesita una
> programación periódica, que es una decisión de infraestructura. Queda anotado en el gate de
> `MVP-504`. La política, el plazo y el cálculo de la fecha de purga sí están.

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
| Google Identity (OIDC) | Servidor | Autenticación de acceso (RN-036) | **Estrictamente necesaria**: es el método de acceso que la persona elige |
| Tipografías e iconos | **Autoalojados** | Sistema de diseño | Sin transferencia a terceros |

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
