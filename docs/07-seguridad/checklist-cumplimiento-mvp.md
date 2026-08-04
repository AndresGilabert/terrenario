---
bloque: 07-seguridad
documento: checklist-cumplimiento-mvp
actualizado_en: "2026-08-04"
---

# Checklist de cumplimiento del MVP (MVP-503)

Evidencia mínima de cumplimiento para autorizar la salida controlada del MVP. Es el documento que
`MVP-504` consulta antes de abrir el gate.

> **Cómo se ha hecho.** Esta revisión **no es una relectura de la KB**: cada afirmación se ha
> contrastado contra el sistema real —esquema de base de datos, código del cliente y comportamiento
> de la API— y las discrepancias encontradas están listadas como hallazgos al final. Una revisión que
> solo confirmara lo que ya decía la documentación no habría servido de nada.

---

## 1. Registro de actividades de tratamiento (RGPD art. 30)

| # | Tratamiento | Datos | Base jurídica (art. 6) | Origen | Conservación |
|---|---|---|---|---|---|
| T1 | Alta y mantenimiento de la cuenta | `google_sub`, nombre, correo | **Ejecución del contrato** (6.1.b) | Google (RN-036) | Vida de la cuenta; tras la baja, anonimizado y purgado a 24 meses (RN-041) |
| T2 | Prestación del servicio de gestión agrícola | Terrenos, temporadas, labores, cosechas, compras, consumos | **Ejecución del contrato** (6.1.b) | El usuario | Vida del Workspace; tras la baja, purgado a 24 meses (RN-039 + RN-041) |
| T3 | Colaboración en Workspace: invitaciones y membresías | Correo de la persona invitada, nombre de quien invita | **Ejecución del contrato** (6.1.b) | El usuario que invita | Hasta transición terminal; purgado a 24 meses (RN-041) |
| T4 | Identificación de responsables de labores | `workers.name` (cuadrilla sin cuenta) | **Interés legítimo del titular del Workspace**, que es el responsable | El usuario | Vida del Workspace |
| T5 | Datos de terceros en maestros y texto libre | `plots.owner_name`, `activities.description` | **Interés legítimo del titular del Workspace** | El usuario | Vida del Workspace |
| T6 | Seguridad de la sesión | Hash de refresh token, `user_id`, caducidades | **Interés legítimo** (6.1.f): impedir accesos no autorizados | El sistema | Hasta caducidad o revocación |
| T7 | Trazabilidad técnica de peticiones | `X-Request-Id`, sin PII | **Interés legítimo** (6.1.f): diagnóstico y seguridad | El sistema | 12 meses |
| T8 | Medición del embudo de acceso (RN-020) | Identificador de flujo aleatorio y nombre del evento; **sin PII** | **Interés legítimo** (6.1.f) | El cliente | Sesión del navegador |

**No hay tratamiento con base en consentimiento**, porque no hay ninguna finalidad que lo requiera
(ver §3). **No hay categorías especiales de datos** (art. 9) ni datos de menores.

### Verificación realizada

Inventario obtenido del **esquema real**, no de la documentación:

```sql
select table_name, column_name from information_schema.columns
where table_schema='public' and column_name ~* 'name|email|sub|token|user';
```

Columnas con dato personal confirmadas: `users.google_sub`, `users.display_name`, `users.email`,
`workers.name`, `plots.owner_name`, `workspace_invitations.email`, `activities.description` (texto
libre) y los `*_user_id` de auditoría. Todas quedan cubiertas por T1–T8.

---

## 2. Principios (RGPD art. 5)

| Principio | Estado | Evidencia |
|---|---|---|
| Licitud, lealtad y transparencia | ✅ | Política de Privacidad accesible **antes de entrar**, desde login y landing (`MVP-505`, CA-1) |
| Limitación de la finalidad | ✅ | Los datos solo se usan para prestar el servicio; no hay cesión ni finalidad secundaria |
| Minimización | ⚠️ | La cuenta recoge solo lo que da Google. **Pero** `plots.owner_name` y `activities.description` son texto libre donde el usuario puede introducir más de lo necesario: el producto no lo puede impedir, y la política se lo pide. Ver `R-03` |
| Exactitud | ⚠️ | Los datos de la cuenta se resincronizan desde Google en cada login (RN-036). **No hay edición de perfil propia** (`P-032`, diferido) |
| Limitación del plazo de conservación | ✅ | `RN-041` fija 24 meses para todo lo que se conserva por diseño. **La rutina de expurgo no está programada**: ver `R-01` |
| Integridad y confidencialidad | ✅ | Aislamiento por Workspace (RN-034, MVP-105), tokens solo como hash, cabeceras de seguridad, CSP en API y cliente (`MVP-502`) |
| Responsabilidad proactiva | ✅ | Este documento, el inventario de tecnologías y las reglas RN-041/RN-042 |

---

## 3. LSSI-CE y ePrivacy: **aplican, y se cumplen sin banner**

**Aplica**: Terrenario es un servicio de la sociedad de la información y almacena información en el
equipo del usuario, así que el art. 22.2 LSSI está en juego.

**Conclusión: no se requiere consentimiento**, porque todo lo que se almacena está exento.

| Qué se almacena | Exención |
|---|---|
| Cookie `refresh_token` | Estrictamente necesaria para la sesión solicitada |
| `terrenario_at` | Ídem, token de acceso de la sesión |
| `pkce_code_verifier`, `oauth_state` | Seguridad del propio acceso (PKCE y anti-CSRF) |
| `terrenario_post_login_redirect` | Funcional: llevar a la persona a donde iba |
| `terrenario:seen_invitations` | Funcional: no repetir un aviso ya visto |
| `terrenario_login_flow`, `terrenario_login_started` | Medición **propia y agregada** de un único flujo, sin PII ni seguimiento entre sitios |

**Sin transferencias a terceros desde el navegador.** `MVP-505` autoalojó las tipografías, que era la
única carga externa; verificado en navegador: **cero peticiones a dominios de Google** al cargar la
aplicación.

Que no haya banner es **la conducta correcta**, no una omisión: la guía de la AEPD reserva el banner
para las tecnologías no exentas, y mostrarlo cuando solo se usan las técnicas normaliza el clic
automático sin proteger nada. Lo que sí se entrega es información: panel de privacidad en Ajustes con
el inventario, y la Política de Privacidad.

`RN-042` deja escrita la obligación de recabar consentimiento previo —con la opción más protectora por
defecto y revocable— el día que entre cualquier tecnología no esencial.

---

## 4. EIPD (RGPD art. 35): **no aplica**

Evaluado contra los nueve criterios del GT29 / EDPB. El tratamiento cumple **uno**, y la EIPD se exige
a partir de dos.

| Criterio | ¿Se cumple? |
|---|---|
| Evaluación o puntuación / perfilado | No |
| Decisiones automatizadas con efecto jurídico | No |
| Observación sistemática | No |
| Datos sensibles o de naturaleza muy personal | No |
| Tratamiento a gran escala | No: MVP con usuarios de validación |
| Asociación o combinación de conjuntos de datos | No |
| Datos de sujetos vulnerables | **Parcialmente**: la cuadrilla no tiene relación directa con el servicio y sus datos los introduce un tercero |
| Uso innovador o aplicación de nuevas tecnologías | No |
| Impedir el ejercicio de un derecho o el acceso a un servicio | No |

**Conclusión: no procede EIPD** para el alcance del MVP. Debe reevaluarse si aparece perfilado,
tratamiento a gran escala o cualquier categoría especial de datos.

---

## 5. Derechos de las personas (arts. 15–22)

| Derecho | Cómo se ejerce | Estado |
|---|---|---|
| Acceso | Contacto de privacidad (`hola@andresgilabert.dev`) | ⚠️ Procedimiento manual, pero con dirección real desde que `MVP-504` cerró `B-1` |
| Rectificación | Los datos de la cuenta se resincronizan desde Google; el resto se edita en la aplicación | ⚠️ Sin edición de perfil propia (`P-032`) |
| **Supresión** | **Desde la propia aplicación**: Ajustes → Eliminar mi cuenta | ✅ `MVP-505`, verificado de punta a punta |
| Portabilidad | Contacto de privacidad | ❌ Sin exportación; fuera de alcance declarado de `MVP-505` (`R-04`) |
| Oposición y limitación | Contacto de privacidad | ⚠️ Procedimiento manual |

**Las personas sin cuenta** —cuadrilla, propietarios de terrenos cedidos— no pueden ejercer sus
derechos desde el producto. Su vía es el titular del Workspace, que es su responsable, o el contacto
de privacidad. Queda dicho en la Política de Privacidad y en los Términos.

---

## 6. Encargados del tratamiento (art. 28)

| Proveedor | Datos | Contrato de encargo |
|---|---|---|
| Microsoft Azure (alojamiento, región España) | Todo lo almacenado | ❌ **Servicio sin contratar** |
| Arsys (correo) | Correo de la persona invitada | ❌ **Servicio sin contratar** (ADR-0010) |

**Google no figura aquí, y es una corrección**: quien entra lo hace con **su** cuenta de Google, así
que Google trata esos datos bajo su propia política y no por cuenta del proyecto. Es **responsable
independiente**, no encargado del art. 28, y no procede contrato de encargo con él. Lo que procede es
informarlo, y se informa. Encuadre aportado por la asesoría del negocio el 2026-08-04; corrige lo que
esta misma revisión había clasificado mal.

Con Azure y Arsys no hay contrato que redactar: el anexo de tratamiento de datos va incorporado al
contratar el servicio. `B-2` es confirmar que está en vigor, y eso llega con la infraestructura.

**Transferencias internacionales**: el alojamiento está en la región de España y el correo es de un
proveedor español, así que la única salida del EEE es la del inicio de sesión con Google, amparada en
cláusulas contractuales tipo y en la decisión de adecuación UE–EE. UU. Declarada en la Política de
Privacidad.

---

## 7. Veredicto

| CA | Veredicto |
|---|---|
| **CA-1** — Evidencia documental mínima RGPD/LOPDGDD | ✅ §1, §2 y §5, verificados contra el sistema |
| **CA-2** — Consta si aplican LSSI/ePrivacy y EIPD | ✅ §3 (aplican, se cumplen sin banner) y §4 (no procede EIPD) |
| **CA-3** — Sostiene la salida controlada | ⚠️ **Con condiciones**: `R-02` ya está cerrado; la salida es sostenible una vez `MVP-504` cierre los contratos de §6 y `R-01` |

**El MVP no es publicable a usuarios reales todavía**, y no por defectos de construcción: faltan
decisiones de negocio e infraestructura. Lo que sí queda demostrado es que **el producto no tiene
deuda de cumplimiento imputable al desarrollo**.

---

## 8. Hallazgos de esta revisión

| # | Hallazgo | Destino |
|---|---|---|
| `R-01` | La **rutina de expurgo no está programada**. La política, el plazo y el cálculo existen (`RN-041`, `AccountRetentionPolicy`), pero nada los ejecuta: hoy nada se purga a los 24 meses | `MVP-504` |
| `R-02` | Los **datos del responsable del tratamiento son marcadores**. Sin ellos, ni la política ni los términos son publicables, y no hay dirección real donde ejercer derechos | ✅ **Cerrado en `MVP-504`** (B-1) |
| `R-03` | **El inventario de tecnologías de `MVP-505` no coincidía con el código**: declaraba `terrenario:privacy_ack`, que no existe, y omitía cinco claves que sí (`pkce_code_verifier`, `oauth_state`, `terrenario_post_login_redirect`, `terrenario_login_flow`, `terrenario_login_started`). **Corregido en esta historia** | Cerrado aquí |
| `R-04` | **`plots.owner_name` no estaba declarado como dato personal** ni en la clasificación de la KB ni en la Política de Privacidad, y está en uso. Es el nombre de un tercero que no tiene cuenta. **Corregido en esta historia**, junto con un bloque nuevo sobre datos de terceros | Cerrado aquí |
| `R-05` | **«No hay analítica» era inexacto**: existe medición propia del embudo de login (RN-020). Se ha analizado y **no requiere consentimiento** —primera parte, sin PII, sin seguimiento entre sitios, vida de sesión— pero la afirmación absoluta no era defendible. **Corregido y motivado** | Cerrado aquí |
| `R-06` | **Sin exportación de datos** (portabilidad, art. 20). Estaba en el fuera-de-alcance de `MVP-505`; se registra para que el gate decida si bloquea la salida | `MVP-999` |

---

## Trazabilidad KB

1. Marco de privacidad y retención: [`privacidad-datos.md`](./privacidad-datos.md)
2. Autenticación y cabeceras: [`autenticacion-autorizacion.md`](./autenticacion-autorizacion.md)
3. Reglas de negocio `RN-041` y `RN-042`: [`../01-producto/reglas-de-negocio.md`](../01-producto/reglas-de-negocio.md)
4. Gate de salida: [`../08-procesos/proceso-release.md`](../08-procesos/proceso-release.md)
