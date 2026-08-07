---
bloque: 01-producto
documento: reglas-de-negocio
actualizado_en: "2026-07-28"
---

# Reglas de Negocio Globales

> Este documento recoge las reglas de negocio que aplican de forma transversal al producto.
> Las reglas específicas de un módulo están en `../03-modulos/{modulo}/modelo-dominio.md`.
>
> **IMPORTANTE para agentes de IA**: Antes de generar código que implique lógica de negocio,
> verifica que no contradiga ninguna regla de este documento.

---

## Convenciones

Cada regla sigue el formato:

- **ID**: `RN-XXX` (identificador único, no reusar IDs aunque se elimine la regla)
- **Estado**: `activa` | `obsoleta` | `en-revisión`
- **Fuente**: de dónde viene esta regla (legal, producto, acuerdo contractual, etc.)

---

## Reglas activas

### RN-001 — Unidad base por registro operativo

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: terrenos, actividades, produccion

Todo registro operativo debe estar asociado a un terreno. No se admiten actividades o cosechas sin terreno.

---

### RN-002 — Responsable y tiempo obligatorios en actividad

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades

No se permite registrar actividad sin responsable y tiempo dedicado.

---

### RN-003 — Coste manual obligatorio en MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades, compras-consumo

En MVP el coste operativo se registra manualmente y no se recalcula automáticamente.

---

### RN-004 — Regla de campos de cosecha en MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

Cada registro de cosecha requiere `kgs` obligatorio. Los campos `rendimiento` y `litros` son opcionales, pero no pueden coexistir en el mismo registro.

---

### RN-005 — Dashboard MVP en pantalla unica

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

El dashboard MVP se presenta en una sola pantalla con scroll vertical, sin navegacion por pestanas de bloques.

---

### RN-006 — Estrategia de refresco del dashboard

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

En MVP no existe actualizacion continua en segundo plano. Los datos se actualizan al entrar al dashboard o mediante recarga manual.

**Excepción**: Ninguna en MVP.

---

### RN-007 — Conservacion de filtros en recarga

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

La recarga manual del dashboard mantiene los filtros activos del usuario. Se materializa (MVP-405) con
los filtros en la **URL** (`?season_id=…&plot_ids=…`): la recarga los conserva y el enlace es
compartible.

---

### RN-008 — Filtro por defecto inicial

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard, diario, cosechas, compras

Sin filtro explícito se aplica por defecto la **temporada de trabajo del usuario** (MVP-209; su
`active_season_id` o, en su defecto, la `WorkingSeasonPolicy`). En el dashboard se aplican además
todos los terrenos activos.

El defecto rige en **todas las vistas operativas**: dashboard, diario, cosechas y compras (con sus
consumos). Hasta MVP-701 solo lo aplicaba el dashboard y las otras tres arrancaban en «todas las
temporadas», de modo que dos pantallas del producto respondían con cifras distintas a «cuánto llevo
esta campaña» (`P-082`).

El servidor resuelve el defecto y devuelve el ámbito aplicado —en el `scope` del dashboard y en
`meta.scope` de las listas— para posicionar los filtros sin duplicar la regla en el cliente. Si el
cliente resolviera el defecto, la regla viviría en dos sitios y volvería a divergir.

El histórico completo sigue siendo elegible, pero como **acto explícito**: `season_id=all`. La
ausencia del parámetro ya significa «aplica el defecto», así que «todas» necesita valor propio.

Un `season_id` que no exista en el Workspace **cae al defecto** en vez de dar error o ampliar el
ámbito: desde MVP-705 el filtro viaja en la URL y al cambiar de Workspace puede quedar el de otro.

---

### RN-009 — Widgets minimos obligatorios de MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

El dashboard MVP debe mostrar estos cuatro widgets:

1. Resumen de temporada
2. Kg por destino
3. Kg por terreno
4. Evolucion de rendimiento

---

### RN-010 — Tratamiento de datos incompletos en kg/arbol

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard, terrenos

Si faltan arboles en uno o mas terrenos, el KPI global kg/arbol excluye esos terrenos y el widget muestra aviso de "dato incompleto".

---

### RN-011 — Orden y visualizacion de kg por terreno

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

El grafico de kg por terreno se renderiza en barras verticales. El orden es fijo por kg descendente, con desempate alfabetico por nombre de terreno. No hay orden manual.

---

### RN-012 — Categoria de destino no clasificado

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion, dashboard

La categoria canónica es `desconocido` y forma parte de la visualización de kg por destino. La UI puede mostrar el alias legible "Sin destino".

---

### RN-013 — Unidad canonica de rendimiento

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion, dashboard

La unidad canonica de rendimiento es litros por cada 100 kg de aceituna (L/100kg).

---

### RN-014 — Entradas equivalentes para rendimiento

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

El sistema acepta para rendimiento:

1. L/100kg informado directamente.
2. kg aceite/100kg informado directamente.
3. Calculo automatico desde kg entregados y litros obtenidos.

---

### RN-015 — Reglas de historico para comparativas

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

Se muestra promedio historico desde el primer anio disponible. Promedios 5 anios y 10 anios solo se muestran cuando exista historico suficiente.

---

### RN-016 — Conversion kg-L con densidad por defecto

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

La densidad por defecto para conversion entre kg y litros de aceite es 0.92 kg/L. Se permite override por almazara, aplicable por defecto a futuros registros y editable por registro.

---

### RN-017 — Cumplimiento obligatorio de proteccion de datos UE y Espana

**Estado**: activa
**Fuente**: legal
**Módulos afectados**: todos

Todo el proyecto debe cumplir de forma obligatoria y continua con RGPD (Reglamento UE 2016/679) y LOPDGDD (LO 3/2018). Cuando aplique por tipo de funcionalidad o canal, tambien deben cumplirse LSSI-CE y ePrivacy.

Ningun requisito funcional, tecnico o de negocio puede aprobarse si entra en conflicto con obligaciones legales de proteccion de datos.

---

### RN-018 — Metodo de login MVP orientado a simplicidad

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion

Para el usuario final Antonio, el metodo de acceso principal del MVP es Google Login. El flujo debe minimizar friccion y evitar gestion de contrasenas locales.

---

### RN-019 — Passkeys planificadas para fase futura

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion

Passkeys se documentan como evolucion posterior al MVP y no bloquean la salida inicial con Google Login.

---

### RN-020 — Trazabilidad obligatoria de abandono en login

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion, observabilidad

Toda sesion que alcance pantalla de login debe ser trazable hasta exito o abandono para medir conversion del embudo y detectar barreras de acceso.

La trazabilidad debe cumplir privacidad por diseno y no registrar PII sensible en claro.

---

### RN-021 — Temporada operativa obligatoria con autoseleccion

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: temporadas, actividades, produccion, compras-consumo

Toda actividad, cosecha y compra del MVP debe quedar asociada a una temporada. La UI autoselecciona la **temporada de trabajo del usuario** (RN-022) para minimizar friccion; el campo queda visible y cambiable.

---

### RN-022 — Temporada de trabajo por usuario y estado independiente

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: temporadas, dashboard, workspaces

Reformulada en `MVP-209` (2026-07-30). El enunciado anterior —«una sola temporada activa por
Workspace»— fundia dos conceptos distintos en el booleano `is_active`. Se separan:

- **Estado de la temporada** (informativo, derivado, independiente de la de trabajo): `planificada`
  (no cerrada y aun no iniciada, `start_date > hoy`), `abierta` (no cerrada y ya iniciada; incluye
  campañas pasadas no cerradas, que siguen recibiendo registros tardios) y `cerrada` (cierre manual,
  RN-024). Sobre las tres se puede añadir, editar y borrar.
- **Temporada de trabajo**: sobre cual se registra por defecto y se carga al iniciar. Es **por
  usuario** (`workspace_members.active_season_id`): un usuario puede trabajar en una campaña sin
  cambiar la de otro miembro del mismo Workspace. Pueden coexistir varias campañas abiertas.

Ya **no** hay «una unica activa por Workspace»: se retira el indice unico parcial
`ux_seasons_workspace_active`. Sin temporada de trabajo fijada, se resuelve un defecto (la campaña
abierta que contiene hoy, si la hay). Enunciado anterior corregido por decision del PO (2026-07-30,
hallazgo `P-045`).

---

### RN-023 — Fecha fuera de rango permitida con aviso

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: temporadas, actividades, produccion, compras-consumo

Si la fecha de un registro queda fuera del rango de la temporada asociada, el sistema permite guardar el registro pero debe mostrar un aviso no bloqueante.

---

### RN-024 — Temporada cerrada informativa

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: temporadas, actividades, produccion, compras-consumo

El estado `cerrada` de una temporada es informativo en MVP y no bloquea nuevas altas ni ediciones. Es
el unico estado que fija una accion explicita del usuario (los otros dos se derivan de las fechas,
RN-022). Cerrar significa «ya no espero mas registros aqui», pero la temporada sigue siendo editable;
reabrir la devuelve a `abierta` o `planificada` segun sus fechas. Fijar una temporada cerrada como la de
trabajo **no** la reabre (`MVP-209`).

---

### RN-025 — Tarea obligatoria en actividad

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades

Toda actividad del MVP debe incluir una tarea. La tarea puede seleccionarse desde el catálogo del Workspace o introducirse en texto libre.

---

### RN-026 — Catalogo de tareas por Workspace con aprendizaje local

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades, tareas

Cada Workspace mantiene su propio catálogo de tareas. El catálogo arranca vacío, es editable por miembros del Workspace y el sistema puede ofrecer guardar una tarea libre para reutilizarla después.

---

### RN-027 — Miembros del Workspace expuestos como trabajadores

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: trabajadores, actividades, workspaces

Todo miembro de un Workspace aparece automáticamente como trabajador seleccionable en actividades. El sistema también permite trabajadores sin cuenta vinculada.

---

### RN-028 — Alta mínima de terreno en MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: terrenos, dashboard

El alta mínima de terreno exige `nombre` y `tipo_propiedad`. El resto de campos, incluido `num_arboles`, es opcional en MVP.

---

### RN-029 — Produccion MVP limitada al nucleo operativo

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

La producción MVP se limita a `producto`, `kgs`, `destino` y uno entre `rendimiento` o `litros`. Quedan fuera de alcance precio, molturación y balance.

---

### RN-030 — Producto de cosecha obligatorio y catalogado

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

Toda cosecha del MVP debe informar un `producto` obligatorio procedente de un catálogo global fijo no editable por usuarios.

---

### RN-031 — Compras con material libre y sugerencias desde historico

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: compras-consumo

En compras, el producto o material se registra como texto libre. La UI puede sugerir valores desde el histórico del Workspace para acelerar la captura.

---

### RN-032 — Consumo sin compra previa y sin recalculo historico

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: compras-consumo

Se permite registrar consumo operativo aunque no exista compra previa, asignando coste 0 con aviso. Si la compra se registra después, el sistema no recalcula los costes históricos ya guardados.

---

### RN-033 — Diario cronologico unificado como vista principal

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades, produccion, compras-consumo, dashboard

La vista principal del MVP es un diario cronológico unificado que mezcla actividades, cosechas y compras/consumos por fecha.

---

### RN-034 — Permisos planos por Workspace en MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: workspaces, autenticacion, autorizacion

En MVP todos los miembros del Workspace pueden operar y administrar registros, maestros, temporadas e invitaciones. Los permisos granulares se dejan para fases posteriores.

---

### RN-035 — Invitaciones por email y por enlace

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: workspaces, autenticacion

El flujo multiusuario del MVP debe soportar invitaciones a Workspace por email y por enlace compartible.

---

### RN-036 — Google como unico proveedor real del MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion

El MVP sale con Google OIDC como único proveedor real de autenticación. Otros proveedores se consideran evolución posterior.

---

### RN-037 — Borrado de registro operativo con confirmacion explicita

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades, produccion, compras-consumo

El MVP permite eliminar registros operativos, pero la UI debe exigir **confirmación explícita** antes de ejecutar la acción, y el registro eliminado deja de aparecer en el diario, en los listados y en el dashboard.

La eliminación es **lógica** (`deleted_at`), no física: el mismo criterio que la baja de Workspace (RN-039). Un borrado accidental sobre operativa ya capturada es recuperable, y el MVP no expone ninguna vía de restauración —papelera o deshacer— porque no la necesita para cumplir la regla: basta con que el dato no se pierda. La purga real de lo eliminado se decide junto a la política de retención (`MVP-999`, P-033).

Enunciado anterior («el MVP permite borrado **físico**») corregido en la revisión de cierre de MVP-002 (`MVP-299`, 3ª pasada, hallazgo `G-1`): contradecía al modelo de datos, que declara `deleted_at` en `ACTIVITY`, `HARVEST` y `PURCHASE` y fija el borrado lógico como convención de persistencia de las entidades operativas. Decisión del PO (2026-07-28).

---

### RN-038 — Un Workspace nunca queda sin propietario

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: workspaces, autorizacion

Todo Workspace vivo tiene en todo momento al menos una persona propietaria con acceso. La salida de
quien lo es debe resolver la propiedad antes de completarse: si hay otras personas propietarias, el
Workspace se reasigna automaticamente; si es la unica, se le exige decidir entre traspasar la
propiedad a un miembro activo o dar de baja el Workspace. La baja de una cuenta que sea propietaria
unica de algun Workspace no puede completarse hasta resolverlos todos.

---

### RN-039 — La baja de un Workspace es logica, nunca fisica

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: workspaces

Dar de baja un Workspace lo marca como eliminado (`deleted_at`) sin borrar ningun dato. Un Workspace
dado de baja deja de resolver contexto activo y de aparecer en el selector, y sus recursos con ambito
de Workspace dejan de ser accesibles, pero siguen intactos en base de datos. La retencion o purga
posterior de los Workspaces dados de baja queda fuera del MVP.

---

### RN-040 — La reactivacion de un Workspace la autoriza quien lo dio de baja

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: workspaces, notificaciones

Al darse de baja un Workspace, sus miembros activos reciben un enlace de un solo uso y con caducidad
para solicitar su traspaso y reactivacion. La solicitud solo la puede autorizar la persona que dio de
baja el Workspace; al autorizarla, el Workspace vuelve a estar activo y su propiedad pasa a quien lo
solicito. Quien dio de baja el Workspace puede ademas volver a levantarlo por su cuenta en cualquier
momento.

### RN-041 — Todo lo que se conserva tiene plazo

**Estado**: activa
**Fuente**: cumplimiento (RGPD, principio de limitacion del almacenamiento)
**Módulos afectados**: identidad, workspaces, diario, produccion

El producto conserva por diseno lo que se da de baja: la baja de un Workspace es logica (RN-039), la
eliminacion de un registro operativo tambien (RN-037) y una cuenta dada de baja conserva su fila
anonimizada porque el historico operativo guarda quien lo registro. Todo eso se conserva **24 meses**
desde su baja y despues se purga fisicamente.

Los **datos personales no esperan a ese plazo**: la baja de cuenta los borra o anonimiza en el acto
—nombre, correo e identificador del proveedor de identidad, tanto en la cuenta como en los maestros de
sus Workspaces y en las invitaciones que la nombraban—. Lo que se conserva es la fila anonimizada, que
ya no identifica a nadie.

El plazo vive tambien en codigo, no solo en la documentacion, para que sea verificable
(`AccountRetentionPolicy`), y desde `MVP-504` **hay una rutina que lo ejecuta**: una pasada diaria
purga lo que cumplio los 24 meses (`RetentionPurgeService`). Antes el plazo estaba declarado y no lo
aplicaba nadie, que es peor que no declararlo.

Una **cuenta anonimizada puede sobrevivir al plazo** si todavia la referencia algo vivo: las FK hacia
`users` son `Restrict` para no borrar por accidente el rastro de quien hizo que. No es una excepcion a
la regla ni una fuga —la fila dejo de identificar a nadie en el momento de la baja—, es limpieza
pendiente, y la rutina la cuenta en su informe.

### RN-042 — Ninguna tecnologia no esencial se activa sin consentimiento

**Estado**: activa
**Fuente**: cumplimiento (LSSI-CE, ePrivacy)
**Módulos afectados**: cliente web

Toda cookie, almacenamiento o recurso de terceros debe estar inventariado y clasificado antes de
activarse (ver `docs/07-seguridad/privacidad-datos.md`). Las **estrictamente necesarias** —las que
sostienen el servicio que la persona ha pedido— no requieren consentimiento. Cualquier otra exige
consentimiento **previo**, con la opcion mas protectora por defecto y revocable en cualquier momento.

El MVP no usa ninguna tecnologia no esencial: no hay **analitica de terceros**, publicidad ni
perfilado, y las tipografias se autoalojan para no transferir la IP de cada visitante a un tercero.
Por eso **no se muestra banner de cookies**: la guia de la AEPD reserva el banner para las tecnologias
no exentas, y mostrarlo cuando solo se usan las tecnicas normaliza el clic automatico sin proteger
nada. Lo que si hay es un panel donde consultar el inventario.

Si existe **medicion propia**: el embudo de acceso (`MVP-105`/`MVP-601`) y el uso del producto
(`MVP-602`). Encaja en el supuesto de medicion de audiencia exenta y esa evaluacion se rehace —no se
da por hecha— cada vez que la medicion crece, con el detalle en
`docs/07-seguridad/privacidad-datos.md`. Las condiciones que la sostienen son cuatro y ninguna es
opcional: primera parte, identificadores aleatorios que mueren con la pestaña, **solo recuentos
agregados conservados** y ausencia de perfilado. Cualquier medida que se salga de ahi exige
consentimiento previo.

## Reglas obsoletas

| ID | Nombre | Motivo de obsolescencia | Fecha |
|----|--------|------------------------|-------|
| RN-LEGACY-001 | Dashboard solo v2 | Se redefine alcance y se incorpora dashboard operativo simple en MVP | 2026-06-30 |
