---
bloque: 01-producto
documento: definicion-requisitos-usuario
actualizado_en: "2026-08-10"
---

# Definición de requisitos de usuario

> Documento de trabajo para traducir la base actual del Google Sheet/BANCALS.xlsx a requisitos de usuario de la app.
> Objetivo: que la app pueda hacer, como mínimo, todo lo que hoy soporta la hoja actual.

---

## Objetivo

Consolidar la funcionalidad real del negocio a partir de la hoja actual y convertirla en una base de requisitos de usuario verificables, priorizados y alineados con el MVP.

> **Dónde acabó cada requisito**: en [Matriz de trazabilidad RU -> destino](#matriz-de-trazabilidad-ru---destino),
> al final del documento. Cada `RU-xx` declara ahí su destino y su estado real, y el gate de KB falla si
> uno marcado «Estado: MVP» se queda sin destino.

---

## Fuente analizada

Workbook exportado: `BANCALS.xlsx`

Hojas detectadas:

- `PRODUCCIO`
- `DIARI`
- `TASQUES`
- `TERRENYS`
- `TEMPORADES`
- `TREBALLADORS`

---

## Lectura ejecutiva

La hoja actual no es solo un dashboard de consulta. Ya funciona como una base operativa mínima con:

- Maestro de terrenos.
- Maestro de trabajadores.
- Catálogo de tareas.
- Registro de cosechas / producción.
- Diario de trabajos y horas.
- Temporadas con rango temporal.
- Cálculos y campos derivados que alimentan el dashboard.

Por tanto, la app debe igualar al menos este núcleo funcional antes de añadir capacidades nuevas.

El cierre funcional previo al roadmap fija además estas decisiones del MVP:

- La vista principal es un diario cronológico unificado de actividades, cosechas y compras/consumos.
- La temporada es obligatoria en registros operativos, se autoselecciona la activa y solo puede haber una activa por Workspace.
- La producción MVP se limita al núcleo operativo: `producto`, `kgs`, `destino` y uno entre `rendimiento` o `litros`.
- El catálogo de tareas es propio de cada Workspace, arranca vacío y admite texto libre con opción de guardado posterior.
- El MVP sale con Google OIDC como único proveedor real de identidad y sin importación desde la hoja actual.

---

## Matriz de cobertura de producto a requisitos

> Esta matriz sirve para comprobar que la visión, las personas, las reglas y los journeys quedan traducidos a requisitos de usuario o quedan marcados como futuro / fuera de alcance.

| Fuente | Elemento | Cobertura en requisitos | Estado |
|--------|----------|-------------------------|--------|
| [vision-y-objetivos](vision-y-objetivos.md) | Gestión de terrenos | RU-01, HU-01, RN-001 | Cubierto |
| [vision-y-objetivos](vision-y-objetivos.md) | Registro de actividades y recursos | RU-04, RU-05, HU-04, HU-06, RN-002, RN-003 | Cubierto |
| [vision-y-objetivos](vision-y-objetivos.md) | Gestión de producción | RU-03, HU-03, RN-004, RN-013, RN-014 | Cubierto |
| [vision-y-objetivos](vision-y-objetivos.md) | Dashboard operativo | RU-06, RU-07, HU-05, HU-08, HU-09, RN-005..RN-015 | Cubierto |
| [vision-y-objetivos](vision-y-objetivos.md) | Identidad simple / sin contraseña local | RU-08, RT-01, RN-018, RN-019, RN-020 | Cubierto |
| [vision-y-objetivos](vision-y-objetivos.md) | Cumplimiento RGPD/LOPDGDD | RU-09, RT-02, RN-017 | Cubierto |
| [vision-y-objetivos](vision-y-objetivos.md) | Compra + consumo aproximado | RU-10, RN-003 | Cubierto (MVP v1) |
| [vision-y-objetivos](vision-y-objetivos.md) | Datos colaborativos / anónimos | RU-11 | Definido (fase futura) |
| [vision-y-objetivos](vision-y-objetivos.md) | Análisis predictivo meteo + IA | RU-12 | Definido (fase futura) |
| [vision-y-objetivos](vision-y-objetivos.md) | Multi-usuario granular | RU-13 | Definido (fase futura) |
| [personas](personas.md) | Antonio | Toda la base de requisitos está escrita para su perfil principal | Cubierto |
| [personas](personas.md) | Carlos | RU-04, RU-05, RU-08, RT-01 y journeys de trabajo / acceso rápido | Cubierto parcialmente |
| [kpis](kpis.md) | KPIs de negocio y producto | RU-06, RU-07, HU-05, HU-09, HU-10, RN-010..RN-015, KPIs de login | Cubierto |
| [user-journeys](user-journeys.md) | Revisar temporada | RU-06, RU-07, HU-05 | Cubierto |
| [user-journeys](user-journeys.md) | Registrar cosecha | RU-03, HU-03, RN-004, RN-013, RN-014 | Cubierto |
| [user-journeys](user-journeys.md) | Acceso sin contraseña | RU-08, RT-01, RN-018, RN-020 | Cubierto |
| [reglas-de-negocio](reglas-de-negocio.md) | Reglas globales | Toda la sección RN-001..RN-037 está referenciada por dominios y journeys | Cubierto |

---

## Matriz hoja -> app

| Hoja | Qué representa hoy | Capacidad mínima que la app debe cubrir | Gap respecto a la KB actual |
|------|--------------------|------------------------------------------|-----------------------------|
| `PRODUCCIO` | Registro de cosechas y rendimiento por fecha, terreno, producto, kilos, litros, destino, molturación y balance | Alta y consulta de cosechas por terreno y temporada, con unidad principal única, destino, rendimiento y cálculos derivados | Cerrado: MVP limitado a `producto` obligatorio de catálogo global, `kgs`, `destino` y uno entre `rendimiento` o `litros`; quedan fuera precio, molturación y balance |
| `DIARI` | Diario de trabajos por fecha, terreno, trabajador, tarea, horas e importe | Alta de actividades/recursos con terreno, responsable, tarea, horas y coste | Cerrado: tarea obligatoria, catálogo por Workspace con texto libre permitido y coste siempre manual/editable |
| `TASQUES` | Catálogo de tareas reutilizables | Catálogo maestro de tareas seleccionables al registrar actividades | Cerrado: entidad funcional explícita del MVP, editable por Workspace y con opción de guardar tareas libres |
| `TERRENYS` | Maestro de parcelas con propietario, alias, referencia catastral, URL, coordenadas y número de olivos | Maestro de terrenos con identificación, ubicación y nº de olivos para KPIs | Cerrado: alta mínima con `nombre` y `tipo_propiedad`; resto opcional e informativo |
| `TEMPORADES` | Maestro de temporadas con rango de fechas y precio estimado de aceite | Gestión de temporadas como eje de filtrado, histórico y comparativas | Cerrado: temporada obligatoria en registros, autoselección de activa, una activa por Workspace y aviso cuando la fecha quede fuera de rango |
| `TREBALLADORS` | Maestro de personas trabajadoras | Maestro de responsables / trabajadores para actividades y trazabilidad | Cerrado: los miembros del Workspace aparecen automáticamente como trabajadores seleccionables y siguen existiendo trabajadores sin cuenta |

---

## Requisitos de usuario mínimos que ya se desprenden del Sheet

### RU-01 - Registrar terrenos

El usuario debe poder crear y consultar terrenos con alta mínima de `nombre` y `tipo_propiedad`.

Datos opcionales del MVP:

- propietario
- alias o nombre corto
- referencia catastral o identificador único
- ubicación o coordenadas
- número de olivos

**Estado**: MVP

### RU-02 - Registrar temporadas

El usuario debe poder definir temporadas con rango de fechas y usarlas como filtro de consulta y análisis.

Reglas derivadas:

- Solo puede existir una temporada activa por Workspace.
- Toda actividad, cosecha y compra debe quedar asociada a una temporada.
- La app autoselecciona la temporada activa al crear registros operativos.
- Si la fecha cae fuera del rango de la temporada elegida, el sistema permite guardar pero muestra aviso.
- El estado `cerrada` es informativo en MVP y no bloquea altas ni ediciones.

**Estado**: MVP

### RU-03 - Registrar cosechas

El usuario debe poder registrar cosechas con:

- fecha
- terreno
- producto obligatorio
- kilos
- destino
- uno entre litros de aceite o rendimiento
- temporada

Reglas derivadas:

- El `producto` se selecciona desde un catálogo global fijo no editable por usuarios.
- `destino = desconocido` es válido para no bloquear el registro operativo.
- El alcance MVP no incluye precio, molturación ni balance.

**Estado**: MVP

### RU-04 - Registrar trabajos del día

El usuario debe poder registrar actividades con:

- fecha
- terreno
- trabajador o responsable
- tarea
- horas
- coste manual editable

Reglas derivadas:

- La tarea es obligatoria en MVP.
- La tarea puede venir del catálogo del Workspace o introducirse en texto libre.
- Si la tarea se introduce en texto libre, la UI puede ofrecer guardarla en el catálogo del Workspace.
- Los miembros del Workspace aparecen automáticamente como trabajadores seleccionables.
- La tarifa horaria, si existe, solo actúa como referencia; el coste sigue siendo manual/editable.

**Estado**: MVP

### RU-05 - Usar un catálogo de tareas

El usuario debe poder seleccionar tareas desde un catálogo reutilizable para evitar introducir nombres inconsistentes.

Reglas derivadas:

- El catálogo es editable por Workspace.
- El catálogo arranca vacío en MVP.
- Las tareas con histórico asociado pueden inactivarse, pero no eliminarse.

**Estado**: MVP

### RU-06 - Consultar el histórico

El usuario debe poder filtrar y consultar por terreno, temporada, tarea y trabajador para reproducir la lógica actual de la hoja y del PowerBI.

La experiencia principal de consulta operativa es un diario cronológico unificado.

**Estado**: MVP

### RU-07 - Calcular indicadores del dashboard

La app debe generar como mínimo los mismos indicadores que hoy alimenta el dashboard:

- producción total
- litros de aceite
- rendimiento medio
- kg por terreno
- kg por destino
- histórico de rendimiento
- kg por árbol cuando exista dato de olivos

**Estado**: MVP

### RU-08 - Acceder con identidad social simplificada

El usuario debe poder registrarse e iniciar sesión con el menor esfuerzo posible, usando una cuenta externa de identidad social.

Requisitos derivados:

- No exigir contraseña local en el MVP.
- Priorizar un flujo apto para usuarios con baja familiaridad técnica.
- Limitar el MVP a Google como único proveedor real de autenticación.
- Mantener abierta la posibilidad de incorporar otros proveedores sociales compatibles con OIDC/OAuth 2.0 si el negocio lo necesita.
- Minimizar pasos, campos y pantallas en el alta y el acceso.
- Permitir invitaciones a Workspace por email y por enlace compartible.

**Estado**: MVP

### RU-09 - Cumplir la normativa europea de protección de datos

El sistema debe cumplir la normativa aplicable de protección de datos de la Unión Europea y su transposición en España desde el diseño y por defecto.

Requisitos derivados:

- Cumplir RGPD y LOPDGDD en todo tratamiento de datos personales.
- Aplicar minimización, limitación de finalidad, control de acceso y retención definida.
- Registrar y documentar la base jurídica de cualquier dato personal tratado.
- Evitar el almacenamiento innecesario de datos personales en texto plano, logs o URLs.
- No condicionar el uso del sistema a tratamientos que no tengan base legal válida.

**Estado**: MVP

### RU-10 - Registrar compras y consumo aproximado

El usuario debe poder registrar compras de materiales y asignar su consumo aproximado a uno o varios terrenos.

Requisitos derivados:

- Registrar producto o material comprado, cantidad total y coste total.
- Mantener el producto/material como texto libre con sugerencias basadas en histórico.
- Asignar consumos aproximados por terreno cuando el material se utilice en varios lugares.
- Registrar cantidad aproximada consumida y coste proporcional por terreno.
- Permitir registrar consumo operativo aunque la compra aún no exista, dejando coste 0 con aviso.
- Si la compra se registra después, no recalcular costes históricos ya guardados.
- No exigir stock tracking ni saldo acumulado en el MVP.
- Mantener trazabilidad suficiente para entender qué se compró, dónde se consumió y cuánto costó.

**Estado**: MVP

### RU-11 - Consultar estadísticas colaborativas anónimas

El sistema debe poder generar estadísticas agregadas y anónimas a partir de datos compartidos por la comunidad de usuarios en fases posteriores.

Requisitos derivados:

- Trabajar siempre sobre datos agregados o anonimizados.
- No exponer información personal ni datos sensibles de explotaciones concretas.
- Permitir activar esta capacidad solo cuando exista una base legal y de producto aprobada.

**Estado**: Fase posterior

### RU-12 - Recibir análisis predictivo meteo + IA

El sistema debe poder cruzar producción y meteorología para sugerir ventanas óptimas de trabajo o cosecha en una fase futura.

Requisitos derivados:

- Integrar datos meteorológicos como fuente de análisis.
- Generar recomendaciones interpretables para el usuario.
- Mantener esta capacidad como evolución posterior al MVP, sin bloquear el uso básico del producto.

**Estado**: Fase posterior

### RU-13 - Gestionar acceso multiusuario granular

El sistema debe poder evolucionar hacia permisos por usuario, rol y terreno cuando el producto tenga varios usuarios reales.

Requisitos derivados:

- Soportar roles definidos a nivel de sistema.
- Permitir aplicar permisos por terreno o grupo de terrenos.
- Mantener el MVP actual con acceso simple, pero sin cerrar el diseño a la evolución multiusuario.

**Estado**: Fase posterior

---

## Requisitos transversales

> Estos requisitos afectan a todo el sistema y no se limitan a una pantalla o flujo concreto.

### RT-01 - Identidad y acceso de baja fricción

El sistema debe priorizar un acceso muy simple para usuarios de edad media o alta.

Requisitos del bloque:

- El acceso principal debe ser con Google.
- No se debe obligar a crear una contraseña local en el MVP.
- El flujo de alta y acceso debe requerir el mínimo número de pasos posible.
- La experiencia debe funcionar bien en móvil y en escritorio.
- El modelo debe permitir incorporar otros proveedores sociales compatibles con OIDC/OAuth 2.0 sin rediseñar el flujo completo.

### RT-02 - Protección de datos por diseño y por defecto

El sistema debe cumplir RGPD y LOPDGDD desde el diseño, incluyendo los tratamientos de datos que aparezcan en autenticación, actividad y auditoría.

Requisitos del bloque:

- Solo recoger los datos personales necesarios para operar el sistema.
- Definir y documentar la base jurídica de cada tratamiento personal.
- Evitar PII en URLs, logs y mensajes de error.
- Respetar retención, minimización y limitación de finalidad.
- Revisar el impacto legal antes de añadir campos nuevos que identifiquen a una persona.

---

## Requisitos funcionales derivados por dominio

### Terrenos

- Crear, editar y listar terrenos.
- Exigir `nombre` y `tipo_propiedad` en el alta.
- Guardar propietario, alias y referencia catastral como datos opcionales.
- Soportar coordenadas o enlace externo de ubicación como datos informativos.
- Guardar número de olivos como dato opcional para KPIs.

### Temporadas

- Definir temporadas con fechas de inicio y fin.
- Usarlas como base de filtrado del dashboard y de los registros.
- Mantener una única temporada activa por Workspace.
- Asociar obligatoriamente toda actividad, cosecha y compra a una temporada.
- Permitir fechas fuera de rango con aviso no bloqueante.

### Producción

- Registrar cosechas por terreno y fecha.
- Exigir producto, `kgs`, destino y temporada.
- Manejar una unidad principal por cosecha.
- Aceptar uno entre `rendimiento` o `litros`.
- Guardar destino, incluyendo `desconocido`.
- Usar un catálogo global fijo de productos de cosecha.
- Excluir de MVP molturación, precio y balance.

### Compras y consumo

- Registrar compras de materiales y consumos aproximados por terreno.
- Mantener trazabilidad del material comprado y usado.
- Usar producto/material en texto libre con sugerencias desde histórico.
- Calcular reparto proporcional del coste cuando proceda.
- Permitir coste 0 cuando no exista compra previa.
- Evitar un modelo de stock complejo en el MVP.

### Datos colaborativos

- Generar estadísticas agregadas y anónimas cuando la fase futura se active.
- Evitar exposición de datos personales o de explotación identificable.

### Analítica predictiva

- Cruzar datos de producción y meteorología para sugerir ventanas de trabajo.
- Presentar recomendaciones comprensibles para un usuario no técnico.

### Acceso multiusuario

- Evolucionar hacia control de acceso granular cuando haya varios usuarios reales.
- Aplicar roles y permisos por terreno o grupo de terrenos.
- Mantener en MVP permisos planos por Workspace, también para maestros e invitaciones.
- Permitir invitaciones por email y por enlace.

### Diario / actividades

- Registrar tareas por día y terreno.
- Relacionar trabajador y horas.
- Hacer obligatoria la tarea, desde catálogo o texto libre.
- Mantener el coste siempre manual/editable.
- Exponer un diario cronológico unificado como vista principal.

### Trabajadores

- Disponer de un maestro de personas para evitar inconsistencias al registrar actividades.
- Mantener nombres reutilizables para el diario.
- Crear automáticamente trabajador seleccionable para cada miembro del Workspace.

### Dashboard

- Resumen de temporada.
- Kg por destino.
- Kg por terreno.
- Evolución de rendimiento.
- Indicadores con datos incompletos cuando falte información de base.

### Dashboard Power BI actual

> Referencia del estado analítico actual. Esta sección no amplía por sí misma el alcance obligatorio del MVP cuando contradice o supera las decisiones ya cerradas arriba.

- Permitir filtrar el dashboard por temporada y propietario, manteniendo la navegación de análisis centrada en una sola vista.
- Mostrar un bloque superior de resumen con los indicadores principales de la temporada seleccionada: kg totales, kg por árbol, rendimiento medio y litros de aceite.
- Mostrar una tabla de detalle de registros con fecha, terreno, kilos, rendimiento, euros por kilo y litros de aceite.
- Mostrar el reparto de kilos por destino con las categorías visibles en la captura actual: aceite, venta y aceite personal.
- Mostrar la evolución temporal del rendimiento con línea de serie y tendencia de referencia.
- Mostrar un gráfico de kilos por terreno ordenado de mayor a menor, con los terrenos visibles en la temporada seleccionada.
- Mostrar un gráfico de rendimiento ponderado por terreno para comparar explotaciones dentro de la misma temporada.
- Mostrar un gráfico de litros de aceite por árbol y terreno cuando exista dato suficiente para el cálculo.
- Mantener la taxonomia de destino compatible con la hoja actual y con la captura, usando canon `desconocido` (alias visual permitido: "Sin destino") cuando proceda.
- Mostrar el dato de kg por árbol solo cuando el terreno tenga número de olivos informado; en caso contrario, excluirlo del cálculo agregado y señalar dato incompleto.
- Permitir que la tabla de detalle y los gráficos respondan al mismo contexto de filtros para que el usuario pueda cruzar resumen, detalle y evolución sin cambiar de pantalla.

### Requisitos de cálculo para el Power BI actual

- Calcular el rendimiento a partir de la unidad canónica L/100kg cuando esté disponible.
- Aceptar como origen del rendimiento un valor ya informado, un valor equivalente en kg/100kg o el cálculo derivado de kg entregados y litros obtenidos.
- Calcular el kg por árbol a partir de la producción total y del número de olivos del terreno.
- Calcular el valor ponderado de rendimiento por terreno cuando existan varios registros dentro de la temporada seleccionada.
- Mostrar litros de aceite totales en el resumen de temporada cuando exista información de molturación o conversión suficiente.
- Conservar el valor histórico de euros por kilo o equivalente si está disponible en el origen de datos, aunque no sea un KPI principal del MVP.
- Mantener la consistencia entre el resumen superior, la tabla detallada y los gráficos agregados para que no existan discrepancias visibles entre vistas.

### Requisitos de navegación y lectura

- La vista debe seguir siendo de lectura rápida, con scroll vertical, sin fragmentar el análisis en pantallas independientes.
- La tabla de detalle debe servir como fuente de verificación del resto de visualizaciones, no como sustituto del resumen.
- Los filtros aplicados deben reflejarse simultáneamente en todos los bloques visibles del dashboard.
- Cuando un dato no exista, el widget afectado debe mostrarse en estado vacío o incompleto, pero no debe romper el resto de la pantalla.

---

## Estado del cierre funcional previo al roadmap

No quedan decisiones funcionales bloqueantes para pasar al diseño del roadmap del MVP.

Quedan como evoluciones posteriores al MVP, no como gaps de definición:

1. Importación o migración desde la hoja actual.
2. Balance, molturación y precio en producción.
3. Permisos granulares por usuario, rol o terreno.
4. Analítica predictiva, datos colaborativos y offline.

---

## Historias de usuario priorizadas

> Priorización orientada a igualar primero la funcionalidad mínima del Sheet y del dashboard actual.

### Prioridad P0 - Núcleo operativo mínimo

| ID | Historia | Usuario | Resultado esperado |
|----|----------|---------|-------------------|
| HU-01 | Gestionar terrenos | Antonio | Puede crear, editar y consultar terrenos con propietario, alias, referencia catastral y número de olivos. |
| HU-02 | Definir temporadas | Antonio | Puede registrar temporadas con fechas de inicio y fin para filtrar datos y comparar histórico. |
| HU-03 | Registrar cosechas | Antonio | Puede registrar producción por terreno, fecha, producto, kilos, litros, rendimiento y destino. |
| HU-04 | Registrar trabajos diarios | Antonio / responsable | Puede registrar tareas por dia con terreno, trabajador, horas y coste manual editable. |
| HU-05 | Consultar dashboard base | Antonio | Puede ver resumen de temporada, kg por destino, kg por terreno y evolución de rendimiento. |

### Prioridad P1 - Consistencia y reutilización de datos

| ID | Historia | Usuario | Resultado esperado |
|----|----------|---------|-------------------|
| HU-06 | Seleccionar tareas desde catálogo | Antonio | Puede elegir tareas reutilizables para evitar incoherencias al registrar actividades. |
| HU-07 | Mantener maestro de trabajadores | Antonio | Puede reutilizar nombres de trabajadores/responsables en registros de diario. |
| HU-08 | Filtrar histórico por terreno y temporada | Antonio | Puede revisar registros y métricas por terreno, temporada, tarea y trabajador. |

### Prioridad P2 - Cálculos y depuración de datos

| ID | Historia | Usuario | Resultado esperado |
|----|----------|---------|-------------------|
| HU-09 | Calcular métricas derivadas | Antonio | La app calcula producción total, litros de aceite, kg por terreno, kg por árbol y rendimiento medio. |
| HU-10 | Gestionar datos incompletos | Antonio | La app muestra avisos y valores parciales cuando faltan datos base como número de olivos. |
| HU-11 | Ver detalle tabular del dashboard | Antonio | Puede consultar una tabla de registros con fecha, terreno, kg, rendimiento, euros por kilo y litros de aceite como evolución posterior al dashboard MVP base. |
| HU-12 | Filtrar por propietario | Antonio | Puede acotar la lectura del dashboard por propietario sin salir de la misma vista como mejora posterior al MVP base. |
| HU-13 | Comparar rendimiento ponderado | Antonio | Puede ver el rendimiento ponderado por terreno para comparar explotaciones dentro de la temporada. |
| HU-14 | Ver litros por árbol | Antonio | Puede consultar litros de aceite por árbol y terreno cuando exista información suficiente. |

### Prioridad P3 - Evolución futura

| ID | Historia | Usuario | Resultado esperado |
|----|----------|---------|-------------------|
| HU-15 | Importar o migrar desde el Sheet | Antonio | Los datos existentes pueden trasladarse a la app sin pérdida de información operativa. |
| HU-16 | Definir balance y molturación | Antonio | La app conserva o calcula campos heredados del Sheet como balance y molturación €/kg cuando aplique. |

---

## Siguiente paso recomendado

Convertir esta matriz en historias de usuario, empezando por:

1. Terrenos y temporadas.
2. Producción.
3. Diario de actividades.
4. Dashboard y cálculos derivados.

## Regla de cierre

Las decisiones funcionales base del MVP ya se consideran cerradas para poder diseñar el roadmap.
Los criterios de aceptación detallados se cerrarán al bajar estas decisiones a épicas e historias.

---

## Requisitos adicionales validados en sesión de Product Owner (2026-07-16)

> Resultado de entrevista estructurada con Antonio (PO). Los siguientes requisitos no estaban explícitos en la KB y han sido confirmados en esta sesión.

### Conectividad y sincronización

- **RU-14: Captura offline permitida con sincronización diferida**
  - Los usuarios pueden registrar actividades/cosechas sin conexión.
  - Los registros se sincronizan automáticamente al recuperar cobertura.
  - Estado: Backlog post-MVP

- **RU-15: Edición limitada en modo offline**
  - En offline solo se pueden crear nuevos registros.
  - Solo se pueden editar registros aún no sincronizados.
  - Registros ya sincronizados son de solo lectura en offline.
  - Estado: Backlog post-MVP

- **RU-16: Estrategia híbrida de reintento y cola de errores**
  - La app intenta resincronizar automáticamente registros con error.
  - Si persiste el error, el registro pasa a cola visible para que Antonio revise y corrija.
  - Estado: Backlog post-MVP

### Autenticación y acceso

- **RU-17: Acceso requiere sesión iniciada**
  - Todo registro (actividad, cosecha, compra, etc.) requiere que el usuario esté autenticado.
  - Modo invitado/anónimo para Carlos (trabajador) queda fuera del scope.
  - Estado: MVP

### Gestión de temporadas

- **RU-18: Temporadas solapables por cultivo**
  - Las temporadas pueden solaparse en fechas para soportar campañas por cultivo (ejemplo: campaña de oliva + campaña de almendra simultáneamente).
  - Estado: MVP

- **RU-19: Campaña activa por defecto en registros**
  - Para cada cultivo debe existir una temporada/campaña marcada como "activa".
  - Al crear un nuevo registro, se asigna automáticamente la campaña activa del cultivo.
  - El usuario puede editarla manualmente.
  - Estado: MVP

### Gestión de registros

- **RU-20: Cierre de temporada sin bloqueo**
  - Al cerrar una temporada, los registros siguen siendo editables.
  - No existe bloqueo "solo lectura" automático, pero la UX puede indicar que está cerrada.
  - Estado: MVP

- **RU-21: Metadato de última edición (sin histórico completo)**
  - Se guardan usuario y fecha de la última edición/creación de cada registro crítico.
  - No se mantiene histórico completo de cambios por simplicidad.
  - Estado: MVP

- **RU-22: Borrado lógico de registros sincronizados**
  - Se permite borrar registros ya guardados en sistema (cosechas, actividades, compras).
  - El impacto en KPIs se refleja en el siguiente refresco del dashboard.
  - Estado: MVP

- **RU-23: Todos los campos obligatorios en alta**
  - No se permiten borradores incompletos.
  - Todos los campos obligatorios deben completarse antes de guardar.
  - Estado: MVP

- **RU-24: Avisos de posibles duplicados**
  - Si se intenta crear una cosecha con mismo terreno, fecha, producto y unidad que uno existente, se muestra aviso.
  - Se permite guardar igual (sin bloqueo).
  - Estado: MVP — **entregado en `MVP-805` (2026-08-10)**, formalizado como `RN-044`.
  - **La «misma unidad» no se traduce literalmente** (decisión del PO, 2026-08-10): este requisito se
    escribió cuando la cosecha aún podía informar el rendimiento de varias formas, y hoy `RN-013` fija
    la unidad canónica, de modo que el modo de entrada es solo eso. La comparación es **terreno +
    fecha + producto**. Incluir el modo de entrada dejaría sin avisar el duplicado más probable —quien
    apunta dos veces lo mismo suele hacerlo de dos maneras—, y añadir los kilos se llevaría por delante
    el caso de teclear mal la cantidad al repetir, que es cuando el aviso más sirve.

### Compras y consumo de materiales

- **RU-25: Compras desacopladas de aplicaciones**
  - Las compras de productos NO están vinculadas a registros de trabajo/aplicación.
  - Esto evita complejidad con excedentes y productos de larga vida útil.
  - La compra solo define precio unitario para cálculos futuros.
  - Estado: MVP

- **RU-26: Coste manual obligatorio en registro operativo**
  - En el registro de trabajo se informa el coste manual total.
  - No se realiza cálculo automático de coste en MVP.
  - Estado: MVP

- **RU-27: Compras con trazabilidad sin recálculo de costes**
  - Las compras se registran para trazabilidad operativa (producto, cantidad, coste total).
  - Registrar una compra no recalcula costes históricos de actividades ya guardadas.
  - Estado: MVP

- **RU-28: Regla de consistencia de costes históricos**
  - Los costes históricos permanecen como fueron registrados manualmente.
  - Se evita modificación automática posterior para mantener confianza en el dato.
  - Estado: MVP

### Gestión de trabajadores

- **RU-29: Trabajadores con estado activo/inactivo**
  - Los trabajadores se marcan como inactivos, nunca se borran.
  - Los inactivos no se ofrecen en selectores pero aparecen en histórico.
  - Estado: MVP

- **RU-30: Trabajadores opcionalmente vinculados a cuenta**
  - Un trabajador puede estar vinculado a una cuenta de usuario (para notificaciones/asignaciones).
  - Un trabajador puede existir sin cuenta si no está registrado en plataforma.
  - Estado: MVP

- **RU-31: Notificaciones configurables**
  - La asignación de tareas a trabajadores vinculados puede generar notificaciones.
  - Canales (push, email, WhatsApp) y tipos de tarea son configurables por Antonio.
  - Estado: Fase posterior

### Sugerencia y planificación de tareas

> **Corrección de estado (2026-08-10, `MVP-809` a partir de `P-111`).** Los tres requisitos de este
> bloque figuraban como MVP y llegaron al final del roadmap sin construirse, sin épica y sin decisión.
> El PO los pasa a backlog post-MVP: no son un defecto de la entrega sino alcance nuevo del tamaño de
> una épica —entidad de plan, motor de recurrencia sobre el histórico, señal de omisión y superficie
> propia—, y el producto de hoy no tiene el concepto de tarea planificada: el catálogo de `MVP-205` es
> un maestro de nombres, no un plan. Se corrige el estado, no se retiran los requisitos.

- **RU-32: Sugerencias de tareas por época y recurrencia**
  - La app sugiere tareas según época del año y recurrencia histórica de temporadas anteriores.
  - Solo sugiere tareas aún no realizadas en la temporada actual.
  - Estado: Backlog post-MVP

- **RU-33: Registro de tareas omitidas con motivo**
  - Las tareas sugeridas pueden marcarse como "omitida" con motivo (clima, falta de tiempo, decisión agronómica, etc.).
  - Esa señal se usa para refinar futuras sugerencias.
  - Estado: Backlog post-MVP

- **RU-34: Conversión manual de sugerencias a tareas planificadas**
  - Las sugerencias son recomendaciones visibles en el dashboard.
  - Antonio puede convertir manualmente una sugerencia en tarea planificada con fecha límite editable.
  - Estado: Backlog post-MVP

### Datos y privacidad

- **RU-35: Baja de cuenta con anonimización en MVP**
  - La opción de borrar cuenta y anonimizar datos operativos está incluida desde MVP (cumplimiento RGPD).
  - Estado: MVP

- **RU-36: Confirmación explícita del borrado con frase tecleada**
  - Al solicitar borrar cuenta, se exige teclear la frase exacta `ELIMINAR MI CUENTA`.
  - La frase se comprueba **también en servidor**, no solo en el diálogo del cliente.
  - El borrado/anonimización se ejecuta inmediatamente tras la confirmación.
  - No hay período de gracia adicional.
  - Estado: MVP
  - Decisión (2026-08-10, `MVP-809` a partir de `P-112`): el requisito pedía **un código enviado
    por email** y `MVP-505` entregó la frase tecleada. Se mantiene la frase y se corrige el
    requisito. Motivo: la frase ya cumple lo que el requisito buscaba de verdad —una confirmación
    explícita, informada y verificada en servidor de una operación irreversible—, mientras que el
    código añadiría un sexto correo del producto y un punto de fallo de entrega a un flujo que la
    persona inicia estando ya autenticada con una cuenta que Google ha verificado. Lo que no se
    sostenía era que el requisito dijera una cosa y el producto hiciera otra sin que constase la
    divergencia.

- **RU-37: Recuperación de cuenta fuera del MVP**
  - No se implementa en MVP flujo de recuperación/migración de cuenta si se pierde acceso a Google.
  - Se anota como mejora de backlog para fases posteriores.
  - Estado: Backlog

### Análisis y comparativas

- **RU-38: Dashboard acotado a cultivo/campaña única**
  - El análisis en el dashboard siempre está filtrado a una sola temporada/campaña.
  - No se permite mezclar cultivos en la misma visualización.
  - Estado: MVP

### Unidades y formatos

- **RU-39: Estándar fijo de unidades en MVP**
  - Se usa estándar fijo de unidades y formatos (coma/punto, kg/litros, etc.).
  - No se implementa localización flexible de unidades.
  - Estado: MVP

- **RU-40: Ubicación heredada del terreno**
  - Los registros heredan ubicación/coordenadas del terreno.
  - No se exige introducir ubicación exacta en cada registro.
  - Estado: MVP

### Registros de trabajo/actividades

- **RU-41: Duración 0 permitida**
  - Un registro de actividad puede tener duración 0 (anotación rápida sin tiempo).
  - Estado: MVP

- **RU-42: Actividades cruzando medianoche como registro único**
  - Una tarea que empieza en un día y termina pasada medianoche se guarda como un único registro (no se parte automáticamente).
  - Estado: MVP

- **RU-43: Tarifa editable por registro**
  - La tarifa de coste de mano de obra puede sobrescribirse en cada registro individual.
  - No se requiere guardar motivo de cambio.
  - Estado: MVP

- **RU-44: Sin límite máximo de horas por registro**
  - No se impone límite máximo de horas en un registro (permite jornadas de cualquier duración).
  - Estado: MVP

- **RU-45: Recálculo de KPIs en siguiente refresco**
  - Las ediciones de registros (como los borrados) impactan en KPIs en el siguiente refresco.
  - No hay recálculo en tiempo real.
  - Estado: MVP

- **RU-46: Registro independiente por terreno**
  - Si una tarea afecta a varios terrenos el mismo día, se crea un registro independiente por terreno.
  - No existe registro multi-terreno con reparto de horas.
  - Estado: MVP

### Catálogos y taxonomías

- **RU-47: Catálogo fijo de destinos**
  - Las categorías de destino (`venta_aceituna`, `aceite_para_venta`, `aceite_personal`, `desconocido`) se mantienen como catálogo fijo.
  - No se permite renombrar o crear nuevas categorías en MVP.
  - Estado: MVP

### Funcionalidades no incluidas en MVP

- **Adjuntos (fotos, documentos)**: Se anota como mejora de backlog. No se implementa en MVP.
- **Exportación a Excel/CSV**: Se anota como mejora de backlog. La visualización interna es suficiente.
- **Validaciones de rango blando**: Se anota como mejora de backlog (ej: avisar si cosecha inusualmente alta sin bloquear).
- **Recuperación de cuenta**: Se anota como mejora de backlog (migración si se pierde acceso a Google).

---

## Impacto en la arquitectura

Los requisitos RU-14 a RU-47 impactan principalmente en:

1. **Modelo de datos**: Agregar campos de metadatos (última edición y trazabilidad operativa).
2. **Sincronización (post-MVP)**: Definir cola de cambios local, reintento con backoff y registro de errores para fases posteriores.
3. **Autenticación**: Confirmar sesión requerida en toda operación.
4. **Validación**: Alerta de duplicados sin bloqueo, campos obligatorios en alta.
5. **Dashboard**: Filtrado siempre por una sola campaña, recálculo periódico no real-time.
6. **Trabajadores**: Modelo opcional de vinculación a cuenta de usuario.
7. **Privacidad**: Implementar baja de cuenta con confirmación explícita del titular y anonimización inmediata.

---

## Matriz de trazabilidad RU -> destino

> Añadida en `MVP-809` (2026-08-10) a partir de `P-114`. **Es normativa, no informativa**: el gate de KB
> (`docs/00-meta/scripts/validar_kb.py`) la lee y falla si un requisito marcado «Estado: MVP» no tiene
> destino, o si su destino son historias ya `completado` y el requisito no consta como entregado.

### Por qué existe

De los 47 requisitos, **44 no se citaban en ningún documento fuera de este**. Las épicas trazan contra
`RN-xxx`, que es una capa más abajo, y nadie trazaba contra `RU-xxx`, así que el primer eslabón de la
cadena que el propio roadmap declara como criterio de priorización —«maximizar trazabilidad requisito
-> regla -> contrato -> validación»— no existía. La consecuencia no fue teórica: `RU-24` llegó al final
del roadmap marcado MVP sin construirse ni descartarse, `RU-32`/`RU-33`/`RU-34` sin épica y sin
decisión, y `RU-36` diciendo una cosa mientras el producto hacía otra.

### Cómo se lee

- **Estado declarado**: lo que dice el `Estado:` del propio requisito. Las dos declaraciones tienen que
  coincidir; el gate lo comprueba.
- **Destino**: dónde queda recogido. Sirve una regla de negocio (`RN-xxx`), una historia (`MVP-xxx`), un
  punto del registro de `MVP-999` (`P-xxx`) o un ADR. **Prosa sin identificador no cuenta como destino**
  para un requisito MVP.
- **Estado real**: `entregado` · `entregado con hueco` (obliga a citar el `P-xxx` que persigue lo que
  falta) · `en <historia>` · `backlog` · `descartado`.

### Matriz

| Requisito | Qué pide | Estado declarado | Destino | Estado real |
|---|---|---|---|---|
| RU-01 | Registrar terrenos con alta mínima | MVP | `RN-028`, `MVP-202` | entregado |
| RU-02 | Registrar temporadas con rango de fechas | MVP | `RN-021`, `RN-023`, `RN-024`, `MVP-203`, `MVP-209` | entregado |
| RU-03 | Registrar cosechas | MVP | `RN-004`, `RN-029`, `RN-030`, `MVP-401`, `MVP-402` | entregado |
| RU-04 | Registrar trabajos del día | MVP | `RN-002`, `RN-003`, `RN-025`, `MVP-301` | entregado |
| RU-05 | Usar un catálogo de tareas | MVP | `RN-026`, `MVP-205`, `MVP-302` | entregado |
| RU-06 | Filtrar el histórico por terreno, temporada, tarea y trabajador | MVP | `RN-033`, `MVP-305`, `MVP-405`, `MVP-506`; hueco en `P-119` | entregado con hueco |
| RU-07 | Calcular los indicadores del dashboard | MVP | `RN-009`, `RN-010`, `RN-011`, `MVP-403`, `MVP-404` | entregado |
| RU-08 | Acceder con identidad social simplificada | MVP | `RN-018`, `RN-035`, `RN-036`, `MVP-101`, `MVP-103` | entregado |
| RU-09 | Cumplir RGPD y LOPDGDD | MVP | `RN-017`, `RN-041`, `RN-042`, `MVP-502`, `MVP-503`, `MVP-505` | entregado |
| RU-10 | Registrar compras y consumo aproximado | MVP | `RN-031`, `RN-032`, `RN-043`, `MVP-303`, `MVP-304`; hueco en `P-121` | entregado con hueco |
| RU-11 | Estadísticas colaborativas anónimas | Fase posterior | Fase futura: sin regla, sin épica y sin base legal aprobada | backlog |
| RU-12 | Análisis predictivo meteo + IA | Fase posterior | Fase futura: sin regla y sin épica | backlog |
| RU-13 | Acceso multiusuario granular | Fase posterior | `RN-034` fija permisos planos en MVP; la evolución no tiene épica | backlog |
| RU-14 | Captura offline con sincronización diferida | Backlog post-MVP | `ADR-0002` (online-first), Hito I | backlog |
| RU-15 | Edición limitada en modo offline | Backlog post-MVP | `ADR-0002` (online-first), Hito I | backlog |
| RU-16 | Reintento híbrido y cola de errores | Backlog post-MVP | `ADR-0002` (online-first), Hito I | backlog |
| RU-17 | Todo registro exige sesión iniciada | MVP | `RN-034`, `MVP-105` | entregado |
| RU-18 | Temporadas solapables por cultivo | MVP | `RN-021`, `MVP-203`; la dimensión de cultivo en `P-059`, `P-060` | entregado |
| RU-19 | Campaña activa por defecto en los registros | MVP | `RN-021`, `RN-022`, `MVP-203`, `MVP-209`; la dimensión de cultivo en `P-060` | entregado |
| RU-20 | Cierre de temporada sin bloqueo | MVP | `RN-024`, `MVP-209` | entregado |
| RU-21 | Metadato de última creación y edición | MVP | `P-113`, `MVP-804` | en MVP-804 |
| RU-22 | Borrado lógico de registros | MVP | `RN-037`, `RN-041`, `MVP-305` | entregado |
| RU-23 | Todos los campos obligatorios en el alta | MVP | `RN-002`, `RN-004`, `MVP-301`, `MVP-401` | entregado |
| RU-24 | Aviso de posible cosecha duplicada | MVP | `RN-044`, `P-110`, `MVP-805` | entregado |
| RU-25 | Compras desacopladas de las aplicaciones | MVP | `RN-031`, `RN-032`, `MVP-303` | entregado |
| RU-26 | Coste manual obligatorio en el registro operativo | MVP | `RN-003`, `MVP-301` | entregado |
| RU-27 | Compras con trazabilidad y sin recálculo | MVP | `RN-032`, `MVP-304` | entregado |
| RU-28 | Consistencia de los costes históricos | MVP | `RN-032`, `MVP-304` | entregado |
| RU-29 | Trabajadores activos o inactivos, nunca borrados | MVP | `RN-027`, `MVP-204`; hueco en `P-120` | entregado con hueco |
| RU-30 | Trabajadores opcionalmente vinculados a una cuenta | MVP | `RN-027`, `MVP-204` | entregado |
| RU-31 | Notificaciones configurables por canal y tipo | Fase posterior | `P-011`, `P-029`, `MVP-808` entregan el mínimo in-app; la generalización sigue en fase posterior | backlog |
| RU-32 | Sugerencias de tareas por época y recurrencia | Backlog post-MVP | `P-111`: épica propia de planificación de tareas | backlog |
| RU-33 | Registro de tareas omitidas con motivo | Backlog post-MVP | `P-111`: épica propia de planificación de tareas | backlog |
| RU-34 | Conversión de sugerencia en tarea planificada | Backlog post-MVP | `P-111`: épica propia de planificación de tareas | backlog |
| RU-35 | Baja de cuenta con anonimización | MVP | `RN-041`, `MVP-505` | entregado |
| RU-36 | Confirmación explícita del borrado de cuenta | MVP | `MVP-505`, `P-112` (decisión: frase tecleada, no código por email) | entregado |
| RU-37 | Recuperación de cuenta fuera del MVP | Backlog | Backlog post-MVP: migración si se pierde el acceso a Google | backlog |
| RU-38 | Dashboard acotado a una sola campaña | MVP | `RN-005`, `RN-008`, `MVP-403`, `MVP-801` | entregado |
| RU-39 | Estándar fijo de unidades y formatos | MVP | `RN-013`, `RN-016`, `MVP-402`; hueco en `P-121` | entregado con hueco |
| RU-40 | Ubicación heredada del terreno | MVP | `RN-028`, `MVP-202` | entregado |
| RU-41 | Duración 0 permitida en una actividad | MVP | `RN-002` lo contradice; discrepancia abierta en `P-122` | backlog |
| RU-42 | Actividad que cruza medianoche como registro único | MVP | `MVP-301`: la actividad es fecha + horas, sin hora de inicio ni de fin | entregado |
| RU-43 | Tarifa editable por registro, sin motivo | MVP | `RN-003`, `MVP-301`, `MVP-208` | entregado |
| RU-44 | Sin límite máximo de horas por registro | MVP | `MVP-301`; tope técnico de 999,99 h en `P-122` | entregado con hueco |
| RU-45 | Recálculo de KPIs en el siguiente refresco | MVP | `RN-006`, `MVP-403` | entregado |
| RU-46 | Un registro independiente por terreno | MVP | `MVP-301`, `MVP-401` | entregado |
| RU-47 | Catálogo fijo de destinos | MVP | `RN-012`, `MVP-402` | entregado |

### Notas del repaso (2026-08-10)

- **`RU-01`**: la ubicación del terreno es un texto libre (`plots.location`), no un par de coordenadas.
  El requisito pide «ubicación **o** coordenadas» como dato opcional e informativo, así que queda
  cubierto; se anota para que nadie lo lea como que hay geolocalización.
- **`RU-18`, `RU-19`, `RU-38`**: los tres están escritos alrededor de una dimensión —el **cultivo**— que
  el modelo no tiene. Hoy son ciertos por construcción, porque el catálogo `harvest_product` tiene un
  único valor (`aceituna_olivar`) y no se pueden mezclar cultivos que no existen. Cuando se materialice
  el producto por Workspace (`P-060`) habrá que releerlos.
- **`RU-25`**: sigue vigente pese a `MVP-304`. La imputación de consumos vincula una compra con
  **terrenos**, no con registros de trabajo, que es lo que el requisito excluye.
- **`RU-29`**: `MVP-806` va a permitir borrar maestros **nunca usados**, lo que matiza el «nunca se
  borran» del requisito sin contradecirlo: los que tienen histórico se seguirán inactivando.
- **`RU-31`**: se declara «Fase posterior» y aun así ya tiene una rebanada construida. `MVP-808` entregó
  el mínimo que quita la dependencia del correo (`P-011`, `P-029`), no la generalización por canal
  —push, email, WhatsApp— ni por tipo de tarea, que es lo que el requisito pide. Por eso su estado real
  es `backlog` y no `entregado`: lo entregado no es el requisito, es lo que hacía falta para no depender
  de que llegue un correo.
- **`RU-44`**: el tope de 999,99 h no es una regla de negocio, sino la cota de la columna
  `decimal(5,2)`. Se anota como hueco porque el requisito dice «sin límite máximo» y hay uno.
