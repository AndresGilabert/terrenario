---
id: "MVP-505"
tipo: feature
titulo: "TDD: Cumplimiento funcional de salida"
estado: completado
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["cumplimiento", "privacidad", "identidad"]
  modulo_path: "03-modulos/"
  componentes: ["legal-pages", "consent", "account-closure", "retention"]
  etiquetas: ["mvp", "legal", "rgpd", "release-blocker"]
  nivel_riesgo: alto
creado_en: "2026-07-31"
actualizado_en: "2026-07-31"
---

# TDD: MVP-505 — Cumplimiento funcional de salida

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

Tres obligaciones de cumplimiento que ninguna historia de revisión podía resolver porque son
**capacidades funcionales**, no revisiones. Se construyen aquí para que `MVP-503` tenga qué verificar
y `MVP-504` pueda autorizar la salida.

| Punto | Qué se entrega |
|---|---|
| `P-008` | Páginas legales públicas y enlazadas, e inventario de tecnologías (sin banner: ver más abajo) |
| `P-024` | Baja de cuenta con anonimización inmediata, reutilizando la guarda de no-orfandad de `MVP-206` |
| `P-033` | Política de retención con plazo explícito (`RN-041`), declarada en la KB **y en código** |

## Componentes afectados

| Componente | Tipo | Descripción |
|---|---|---|
| `Domain/Users/User.cs` | modificado | `DeletedAt` y `Anonymize()` |
| `Application/Account/` | nuevo | `CloseAccountHandler`, `AccountRetentionPolicy` |
| `Controllers/AccountController.cs` | nuevo | `GET`/`POST /api/v1/account/closure` |
| `Migrations/…_AddUserDeletedAt` | nuevo | Columna `users.deleted_at` |
| `IRefreshTokenStore` · `IWorkspaceInvitationRepository` | modificado | Revocación masiva de sesiones y anulación de invitaciones por correo |
| `UserRepository` | modificado | Una cuenta dada de baja deja de reconocerse |
| `frontend/components/legal/` | nuevo | Armazón y contenido de las dos páginas |
| `frontend/components/settings/PrivacyPanel · DeleteAccountPanel` | nuevo | Inventario y baja de cuenta |
| `frontend/index.css` · `index.html` · `vite.config.ts` | modificado | Tipografías autoalojadas y CSP cerrada a `'self'` |
| `docs/07-seguridad/privacidad-datos.md` | modificado | Retención (RN-041) e inventario (RN-042) |
| `docs/01-producto/reglas-de-negocio.md` | modificado | Alta de `RN-041` y `RN-042` |
| `docs/02-arquitectura/contratos-api.md` | modificado | Endpoints de baja y rutas legales |

## Diseño detallado

### Baja de cuenta: anonimización inmediata, purga diferida

Decisión del PO (2026-07-30). Al confirmar, los datos personales desaparecen **ya**; la fila
sobrevive anonimizada y se purga al vencer el plazo.

El motivo de conservar la fila no es comodidad técnica: cada actividad, cosecha y compra guarda quién
la registró. Borrarla dejaría el histórico operativo de **terceros** —las otras personas del
Workspace— sin autoría, o lo arrastraría en cascada. Lo que el derecho de supresión exige son los
datos personales, y esos sí desaparecen.

El paso decisivo es el `google_sub`: se sustituye por un valor derivado del id, así que **deja de
coincidir con el que devuelve Google**. Si la persona vuelve a entrar con la misma cuenta, el login
no la reconoce y crea una cuenta nueva y vacía. Eso es lo que separa una supresión de una simple
desactivación. El correo pasa a `deleted+{id}@terrenario.invalid` — dominio reservado por el RFC 2606,
así que no puede existir— y cumple el índice único sin colisionar.

Qué más se limpia, y por qué cada cosa:

1. **Membresías**: se revocan. El vínculo no se borra —los registros que lo referencian siguen siendo
   válidos (CA-7 de `MVP-204`)— pero deja de tener acceso.
2. **Maestro de responsables**: el nombre vive también ahí (RN-036/`MVP-208`). Sin esto, el dato
   personal sobreviviría justo donde más se ve.
3. **Invitaciones pendientes a su correo**: se anulan por el agregado, no con un `UPDATE` masivo,
   porque la transición a `anulada` tiene reglas.
4. **Sesiones**: todas revocadas y cookie borrada.

**La regla de no-orfandad se llama, no se reimplementa.** `MVP-206` dejó
`WorkspaceOwnershipGuard.EnsureAccountClosureAllowedAsync` implementada y probada explícitamente como
punto de enganche de esta historia; era la condición con la que se registró `P-024`.

La confirmación es **una frase tecleada**, no un clic, y se comprueba también en servidor: una
operación irreversible no puede depender de que el cliente se porte bien.

### Por qué no hay banner de cookies

Es la decisión que más conviene dejar razonada, porque a primera vista parece que falta algo.

Se inventariaron todas las tecnologías de almacenamiento y terceros. **Todas son estrictamente
necesarias**: la cookie de sesión, el token de la pestaña, el recordatorio de avisos vistos y el
inicio de sesión con Google. No hay analítica, publicidad ni perfilado.

Quedaba **una** excepción real: las tipografías se cargaban desde el CDN de Google, lo que transfiere
la IP de cada visitante a un tercero sin base jurídica clara. Ese es exactamente el supuesto que
obligaría a pedir consentimiento. Había dos salidas: pedirlo, o quitar la transferencia. Se
**autoalojan** (`@fontsource/*`, `material-symbols`): elimina el problema en vez de gestionarlo, evita
un banner que degradaría la experiencia de todo visitante, mantiene la fidelidad al sistema de diseño
y permite además **cerrar la CSP a `'self'`** en `style-src` y `font-src`.

Sin ninguna tecnología no esencial, mostrar un banner sería **peor** cumplimiento: la guía de la AEPD
lo reserva para las tecnologías no exentas, y enseñarlo cuando solo se usan las técnicas normaliza el
clic automático sin proteger nada. Lo que la norma sí exige es informar, y eso se entrega: páginas
legales accesibles y un panel con el inventario. `RN-042` deja escrita la obligación para el día que
entre algo no esencial.

### Retención (CA-5)

`RN-041` extiende los 24 meses que ya regían para «cuenta cancelada» a todo lo que el producto
conserva por diseño y no tenía plazo: Workspaces de baja (RN-039), registros operativos eliminados
(RN-037), solicitudes de reactivación cerradas e invitaciones terminales.

El plazo vive **también en código** (`AccountRetentionPolicy`), no solo en el documento, y la
respuesta de la baja devuelve la fecha concreta de purga. Es lo que permite que `MVP-503` lo
verifique contra el sistema en vez de leerlo.

### Contenido legal con marcadores

Decisión del PO: el contenido se redacta completo y correcto, describiendo **lo que el sistema hace
de verdad** —los tratamientos, bases jurídicas, encargados y plazos salen de `privacidad-datos.md`—,
con marcadores visibles para los datos que solo puede aportar el negocio. Una plantilla genérica que
describiera otro producto no cumpliría nada.

Las páginas muestran un **aviso de documento pendiente**: mientras queden marcadores, no son
publicables, y quien las lea tiene que saberlo.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Borrado físico de la fila de usuario | Rompe la autoría del histórico operativo de terceros, o lo arrastra en cascada |
| Periodo de gracia antes de anonimizar | Retrasa el efecto del derecho de supresión sin que nadie lo haya pedido |
| Mantener Google Fonts y pedir consentimiento | Degrada la experiencia de todo visitante para gestionar un problema que se puede eliminar |
| Banner de cookies «por si acaso» | Mala práctica reconocida cuando solo hay tecnologías técnicas |
| `DELETE /api/v1/account` | La operación necesita cuerpo (la confirmación) y `MVP-206` ya fijó el patrón `POST …/closure` |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Las páginas legales se publican con marcadores sin rellenar | media | Aviso visible en la propia página y bloqueo explícito registrado para el gate de `MVP-504` |
| Una baja deja datos personales atrás | baja | Cinco pasos cubiertos por tests que comprueban **la base de datos**, no solo el código de respuesta |
| El expurgo no llega a ejecutarse | alta | La rutina periódica es infraestructura y **no se entrega**: queda anotado para `MVP-504` |

## Plan de testing

- [x] Integración: 12 casos contra API y PostgreSQL reales —bloqueo por no-orfandad, confirmación
      exacta, borrado de los tres campos identificativos, imposibilidad de volver a entrar, revocación
      de sesiones, salida de Workspaces ajenos con anonimización en el maestro y anulación de
      invitaciones—.
- [x] Frontend: 6 casos del panel de baja (bloqueo informado, frase exacta sensible a mayúsculas,
      cierre de sesión al completar, mensaje de error de la API).
- [x] Verificación conducida en navegador: páginas legales, enlaces vivos en login y landing, panel de
      privacidad, panel de baja bloqueado correctamente con datos reales y **cero peticiones a
      dominios de Google**.

## Resultado

| Suite | Antes | Después |
|---|---|---|
| Backend | 654 | **666** |
| Frontend | 81 | **87** |

## Lo que esta historia deja abierto

- **Los marcadores del contenido legal**: solo el negocio puede rellenarlos. Bloqueo para `MVP-504`.
- **La programación del expurgo**: la política, el plazo y el cálculo están; ejecutarlo
  periódicamente es una decisión de infraestructura. Anotado para `MVP-504`.

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Migración preparada (`users.deleted_at`) y aplicada en local
- [x] Tests escritos y pasando
- [x] Contrato de API actualizado
- [x] `RN-041` y `RN-042` dadas de alta
- [x] `P-008`, `P-024` y `P-033` cerrados en `MVP-999`
- [x] Sin `TODO` sin resolver en este documento
