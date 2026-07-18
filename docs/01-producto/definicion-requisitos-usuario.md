---
bloque: 01-producto
documento: definicion-requisitos-usuario
actualizado_en: "2026-07-18"
---

# Definición de requisitos de usuario

> Documento de trabajo para traducir la base actual del Google Sheet/BANCALS.xlsx a requisitos de usuario de la app.
> Objetivo: que la app pueda hacer, como mínimo, todo lo que hoy soporta la hoja actual.

---

## Objetivo

Consolidar la funcionalidad real del negocio a partir de la hoja actual y convertirla en una base de requisitos de usuario verificables, priorizados y alineados con el MVP.

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
| [reglas-de-negocio](reglas-de-negocio.md) | Reglas globales | Toda la sección RN-001..RN-020 está referenciada por dominios y journeys | Cubierto |

---

## Matriz hoja -> app

| Hoja | Qué representa hoy | Capacidad mínima que la app debe cubrir | Gap respecto a la KB actual |
|------|--------------------|------------------------------------------|-----------------------------|
| `PRODUCCIO` | Registro de cosechas y rendimiento por fecha, terreno, producto, kilos, litros, destino, molturación y balance | Alta y consulta de cosechas por terreno y temporada, con unidad principal única, destino, rendimiento y cálculos derivados | Falta especificar con más detalle el modelo de producción para cubrir litros, molturación, precio y balance |
| `DIARI` | Diario de trabajos por fecha, terreno, trabajador, tarea, horas e importe | Alta de actividades/recursos con terreno, responsable, tarea, horas y coste | Falta definir catálogo de tareas y si el importe es calculado o editable en cada caso |
| `TASQUES` | Catálogo de tareas reutilizables | Catálogo maestro de tareas seleccionables al registrar actividades | No está definido como entidad funcional explícita en la KB |
| `TERRENYS` | Maestro de parcelas con propietario, alias, referencia catastral, URL, coordenadas y número de olivos | Maestro de terrenos con identificación, ubicación y nº de olivos para KPIs | Falta concretar qué campos son obligatorios, cuáles opcionales y cómo se validan |
| `TEMPORADES` | Maestro de temporadas con rango de fechas y precio estimado de aceite | Gestión de temporadas como eje de filtrado, histórico y comparativas | Falta bajar la temporada a requisito funcional formal |
| `TREBALLADORS` | Maestro de personas trabajadoras | Maestro de responsables / trabajadores para actividades y trazabilidad | La KB solo define responsable como texto; falta decidir si se modela como maestro o se mantiene libre |

---

## Requisitos de usuario mínimos que ya se desprenden del Sheet

### RU-01 - Registrar terrenos

El usuario debe poder crear y consultar terrenos con al menos:

- propietario
- alias o nombre corto
- referencia catastral o identificador único
- ubicación o coordenadas si existen
- número de olivos

### RU-02 - Registrar temporadas

El usuario debe poder definir temporadas con rango de fechas y usarlas como filtro de consulta y análisis.

### RU-03 - Registrar cosechas

El usuario debe poder registrar cosechas con:

- fecha
- terreno
- producto
- kilos
- litros de aceite cuando aplique
- rendimiento
- destino
- coste manual operativo (sin modelo de balance en MVP)

### RU-04 - Registrar trabajos del día

El usuario debe poder registrar actividades con:

- fecha
- terreno
- trabajador o responsable
- tarea
- horas
- coste manual editable

### RU-05 - Usar un catálogo de tareas

El usuario debe poder seleccionar tareas desde un catálogo reutilizable para evitar introducir nombres inconsistentes.

### RU-06 - Consultar el histórico

El usuario debe poder filtrar y consultar por terreno, temporada, tarea y trabajador para reproducir la lógica actual de la hoja y del PowerBI.

### RU-07 - Calcular indicadores del dashboard

La app debe generar como mínimo los mismos indicadores que hoy alimenta el dashboard:

- producción total
- litros de aceite
- rendimiento medio
- kg por terreno
- kg por destino
- histórico de rendimiento
- kg por árbol cuando exista dato de olivos

### RU-08 - Acceder con identidad social simplificada

El usuario debe poder registrarse e iniciar sesión con el menor esfuerzo posible, usando una cuenta externa de identidad social.

Requisitos derivados:

- No exigir contraseña local en el MVP.
- Priorizar un flujo apto para usuarios con baja familiaridad técnica.
- Permitir autenticación con Google como camino principal.
- Mantener abierta la posibilidad de incorporar otros proveedores sociales compatibles con OIDC/OAuth 2.0 si el negocio lo necesita.
- Minimizar pasos, campos y pantallas en el alta y el acceso.

### RU-09 - Cumplir la normativa europea de protección de datos

El sistema debe cumplir la normativa aplicable de protección de datos de la Unión Europea y su transposición en España desde el diseño y por defecto.

Requisitos derivados:

- Cumplir RGPD y LOPDGDD en todo tratamiento de datos personales.
- Aplicar minimización, limitación de finalidad, control de acceso y retención definida.
- Registrar y documentar la base jurídica de cualquier dato personal tratado.
- Evitar el almacenamiento innecesario de datos personales en texto plano, logs o URLs.
- No condicionar el uso del sistema a tratamientos que no tengan base legal válida.

### RU-10 - Registrar compras y consumo aproximado

El usuario debe poder registrar compras de materiales y asignar su consumo aproximado a uno o varios terrenos.

Requisitos derivados:

- Registrar producto o material comprado, cantidad total y coste total.
- Asignar consumos aproximados por terreno cuando el material se utilice en varios lugares.
- Calcular la parte proporcional del coste si la compra se vincula a un consumo concreto.
- No exigir stock tracking ni saldo acumulado en el MVP.
- Mantener trazabilidad suficiente para entender qué se compró, dónde se consumió y cuánto costó.

### RU-11 - Consultar estadísticas colaborativas anónimas

El sistema debe poder generar estadísticas agregadas y anónimas a partir de datos compartidos por la comunidad de usuarios en fases posteriores.

Requisitos derivados:

- Trabajar siempre sobre datos agregados o anonimizados.
- No exponer información personal ni datos sensibles de explotaciones concretas.
- Permitir activar esta capacidad solo cuando exista una base legal y de producto aprobada.

### RU-12 - Recibir análisis predictivo meteo + IA

El sistema debe poder cruzar producción y meteorología para sugerir ventanas óptimas de trabajo o cosecha en una fase futura.

Requisitos derivados:

- Integrar datos meteorológicos como fuente de análisis.
- Generar recomendaciones interpretables para el usuario.
- Mantener esta capacidad como evolución posterior al MVP, sin bloquear el uso básico del producto.

### RU-13 - Gestionar acceso multiusuario granular

El sistema debe poder evolucionar hacia permisos por usuario, rol y terreno cuando el producto tenga varios usuarios reales.

Requisitos derivados:

- Soportar roles definidos a nivel de sistema.
- Permitir aplicar permisos por terreno o grupo de terrenos.
- Mantener el MVP actual con acceso simple, pero sin cerrar el diseño a la evolución multiusuario.

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
- Guardar propietario, alias y referencia catastral.
- Soportar coordenadas o enlace externo de ubicación.
- Guardar número de olivos para KPIs.

### Temporadas

- Definir temporadas con fechas de inicio y fin.
- Usarlas como base de filtrado del dashboard y de los registros.

### Producción

- Registrar cosechas por terreno y fecha.
- Manejar una unidad principal por cosecha.
- Calcular rendimiento.
- Guardar destino.
- Soportar litros de aceite y molturación cuando aplique.
- Mantener un campo de balance si el negocio sigue usando esa lógica.

### Compras y consumo

- Registrar compras de materiales y consumos aproximados por terreno.
- Mantener trazabilidad del material comprado y usado.
- Calcular reparto proporcional del coste cuando proceda.
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

### Diario / actividades

- Registrar tareas por día y terreno.
- Relacionar trabajador y horas.
- Capturar importes o calcularlos según reglas.

### Trabajadores

- Disponer de un maestro de personas para evitar inconsistencias al registrar actividades.
- Mantener nombres reutilizables para el diario.

### Dashboard

- Resumen de temporada.
- Kg por destino.
- Kg por terreno.
- Evolución de rendimiento.
- Indicadores con datos incompletos cuando falte información de base.

### Dashboard Power BI actual

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

## Gaps a resolver en la siguiente iteración de requisitos

1. Decidir si `TREBALLADORS` se modela como entidad formal o como texto libre con autocompletado.
2. Definir regla de sugerencia de coste por tarifa_hora sin romper el principio de coste manual obligatorio por registro.
3. Definir el modelo exacto de `balance` en producción.
4. Formalizar si `molturación €/Kg` es un dato de entrada, un cálculo o un dato histórico heredado.
5. Definir si el filtro por propietario es un filtro funcional del dashboard o solo un atributo de consulta interna.
6. Formalizar cómo se importan o migran datos desde la hoja actual.
7. Decidir qué datos son obligatorios y cuáles opcionales en creación/edición.

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
| HU-11 | Importar o migrar desde el Sheet | Antonio | Los datos existentes pueden trasladarse a la app sin pérdida de información operativa. |
| HU-12 | Ver detalle tabular del dashboard | Antonio | Puede consultar una tabla de registros con fecha, terreno, kg, rendimiento, euros por kilo y litros de aceite. |
| HU-13 | Filtrar por propietario | Antonio | Puede acotar la lectura del dashboard por propietario sin salir de la misma vista. |
| HU-14 | Comparar rendimiento ponderado | Antonio | Puede ver el rendimiento ponderado por terreno para comparar explotaciones dentro de la temporada. |
| HU-15 | Ver litros por árbol | Antonio | Puede consultar litros de aceite por árbol y terreno cuando exista información suficiente. |

### Prioridad P3 - Evolución futura

| ID | Historia | Usuario | Resultado esperado |
|----|----------|---------|-------------------|
| HU-16 | Normalizar categorias de destino | Antonio | La app permite clasificar destinos con taxonomia fija: `venta_aceituna`, `aceite_para_venta`, `aceite_personal` y `desconocido` (alias UI: "Sin destino"). |
| HU-17 | Definir balance y molturación | Antonio | La app conserva o calcula campos heredados del Sheet como balance y molturación €/kg cuando aplique. |

---

## Siguiente paso recomendado

Convertir esta matriz en historias de usuario, empezando por:

1. Terrenos y temporadas.
2. Producción.
3. Diario de actividades.
4. Dashboard y cálculos derivados.

## Regla de cierre

Los criterios de aceptación detallados no deben considerarse cerrados hasta que termine esta fase de definición de requisitos de usuario.
En esta etapa solo se mantienen como referencias provisionales de validación, para no congelar demasiado pronto el alcance de las futuras historias.

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
  - Estado: MVP

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

- **RU-32: Sugerencias de tareas por época y recurrencia**
  - La app sugiere tareas según época del año y recurrencia histórica de temporadas anteriores.
  - Solo sugiere tareas aún no realizadas en la temporada actual.
  - Estado: MVP (básico)

- **RU-33: Registro de tareas omitidas con motivo**
  - Las tareas sugeridas pueden marcarse como "omitida" con motivo (clima, falta de tiempo, decisión agronómica, etc.).
  - Esa señal se usa para refinar futuras sugerencias.
  - Estado: MVP

- **RU-34: Conversión manual de sugerencias a tareas planificadas**
  - Las sugerencias son recomendaciones visibles en el dashboard.
  - Antonio puede convertir manualmente una sugerencia en tarea planificada con fecha límite editable.
  - Estado: MVP

### Datos y privacidad

- **RU-35: Baja de cuenta con anonimización en MVP**
  - La opción de borrar cuenta y anonimizar datos operativos está incluida desde MVP (cumplimiento RGPD).
  - Estado: MVP

- **RU-36: Confirmación por código de email para borrado**
  - Al solicitar borrar cuenta, se requiere validar código enviado por email.
  - El borrado/anonimización se ejecuta inmediatamente tras validar el código.
  - No hay período de gracia adicional.
  - Estado: MVP

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
7. **Privacidad**: Implementar baja de cuenta con confirmación por email y anonimización inmediata.
