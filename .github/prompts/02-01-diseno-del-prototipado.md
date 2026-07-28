Diseña un prototipo web de alta fidelidad para Terrenario, una plataforma de gestión agrícola personal orientada a usuarios no técnicos. El prototipo debe cubrir todas las pantallas, modales, formularios, estados vacíos, estados de error y estados de carga del MVP. Debe ser navegable end-to-end, con versiones desktop y mobile, y con una estética moderna, cálida, clara y agrícola-profesional. Idioma de toda la interfaz: español.

Objetivo del prototipo:

Representar visualmente todo el MVP de Terrenario.
Incluir ejemplos de todas las pantallas y estados relevantes.
Permitir navegar entre módulos como si fuera una app real.
Reflejar reglas de negocio clave en la UI.
Mantener una experiencia simple, rápida y comprensible para usuarios no técnicos.
Dirección de diseño:

Desktop first, pero completamente responsive.
Interfaz limpia, con personalidad, sin look genérico SaaS.
Formularios cortos, jerarquía clara y acciones primarias muy visibles.
Estados vacíos útiles, feedback inmediato y navegación evidente.
Componentes reutilizables consistentes en toda la app.
Microcopys en español, orientados a uso diario real.
Contexto funcional del MVP:

Terrenario es multiusuario desde el día uno.
La unidad de contexto es el Workspace.
El login principal es Google, sin contraseña local.
No hay acceso anónimo.
Todos los miembros del Workspace pueden ver y editar todo en MVP.
La temporada es obligatoria en registros operativos.
Solo puede existir una temporada activa por Workspace.
La vista principal de uso es un diario cronológico unificado.
El dashboard MVP vive en una sola pantalla con scroll vertical.
No incluir funcionalidades post-MVP.
Pantallas que deben diseñarse obligatoriamente

Acceso e identidad
1.1 Landing pública.
1.2 Pantalla de login con Google como acción principal.
1.3 Estado de éxito de autenticación.
1.4 Estado de error de autenticación.
1.5 Pantalla de invitación recibida.
1.6 Pantalla de aceptación de invitación.
1.7 Pantalla de primer acceso sin Workspace.
1.8 Pantalla de cuenta usuario con acceso a Workspaces.

Onboarding inicial
2.1 Crear primer Workspace.
2.2 Confirmación de Workspace creado.
2.3 Crear o sugerir primera temporada.
2.4 Crear primer terreno mínimo.
2.5 Pantalla de “todo listo para empezar”.
2.6 Estado vacío de Workspace recién creado.

Navegación global
3.1 App shell principal con navegación lateral o superior.
3.2 Selector visible de Workspace activo.
3.3 Selector de temporada visible o contextual.
3.4 Menú de usuario.
3.5 Estado de cambio de Workspace.
3.6 Estado de Workspace vacío o sin datos.
3.7 Estado de acceso denegado por contexto incorrecto.

Gestión de Workspaces
4.1 Lista de Workspaces del usuario.
4.2 Detalle de Workspace.
4.3 Crear Workspace.
4.4 Editar Workspace.
4.5 Miembros del Workspace.
4.6 Invitar miembro por email.
4.7 Invitar mediante enlace compartible.
4.8 Estado de invitación enviada.
4.9 Estado de invitación pendiente.
4.10 Estado de invitación revocada.
4.11 Confirmación para abandonar Workspace si aplica.
4.12 Estado de cambio de miembro activo o revocado.

Terrenos
5.1 Listado de terrenos.
5.2 Detalle de terreno.
5.3 Crear terreno.
5.4 Editar terreno.
5.5 Estado vacío sin terrenos.
5.6 Estado con varios terrenos y filtros.
5.7 Modal de borrar terreno.
5.8 Modal o drawer de alta rápida de terreno.
5.9 Formulario completo de terreno con campos opcionales.
5.10 Vista de terreno con indicadores básicos como número de árboles o metadatos.

Campos visibles en el formulario de terreno:

Nombre.

Tipo de propiedad.

Alias.

Propietario.

Referencia catastral.

Ubicación.

Número de árboles.

Metadatos de suelo.

Temporadas
6.1 Listado de temporadas.
6.2 Crear temporada.
6.3 Editar temporada.
6.4 Temporada activa resaltada.
6.5 Temporada cerrada.
6.6 Estado vacío sin temporadas.
6.7 Modal de cambiar temporada activa.
6.8 Modal de cerrar temporada.
6.9 Aviso si se intenta operar fuera del rango de fechas.
6.10 Formulario de temporada con validación de fechas.

Campos visibles en el formulario de temporada:

Nombre.

Fecha de inicio.

Fecha de fin.

Estado.

Indicador de temporada activa.

Trabajadores
7.1 Listado de trabajadores.
7.2 Crear trabajador.
7.3 Editar trabajador.
7.4 Trabajador vinculado a usuario.
7.5 Trabajador sin cuenta.
7.6 Estado activo.
7.7 Estado inactivo.
7.8 Estado vacío sin trabajadores.
7.9 Modal de inactivación o borrado lógico.
7.10 Selector de trabajador en actividad.

Campos visibles en el formulario de trabajador:

Nombre.

Cuenta vinculada o no.

Estado activo o inactivo.

Tarifa horaria como referencia si se muestra.

Tareas
8.1 Listado de tareas.
8.2 Crear tarea.
8.3 Editar tarea.
8.4 Inactivar tarea.
8.5 Estado vacío del catálogo.
8.6 Sugerencias de tarea desde histórico.
8.7 Modal para guardar una tarea libre en catálogo.
8.8 Vista de tareas activas e inactivas.

Campos visibles en el formulario de tarea:

Nombre.

Estado activa o inactiva.

Diario cronológico unificado
9.1 Pantalla principal del producto.
9.2 Timeline o feed cronológico con actividades, compras y consumos.
9.3 Filtros por fecha, terreno, temporada, trabajador y tarea.
9.4 Botón de crear nuevo registro rápido.
9.5 Estado sin resultados por filtros.
9.6 Estado con actividad reciente.
9.7 Estado de carga del diario.
9.8 Vista compacta para mobile.
9.9 Vista expandida para desktop.
9.10 Detalle de elemento del diario.
9.11 Acción de editar desde el diario.
9.12 Acción de borrar desde el diario.
9.13 Modal de confirmación de borrado.

Cada item del diario debe poder mostrar:

Fecha.

Tipo de registro.

Terreno.

Temporada.

Responsable o proveedor.

Descripción breve.

Coste o cantidad asociada.

Estado o aviso si falta compra previa.

Actividades
10.1 Pantalla de alta de actividad.
10.2 Pantalla de edición de actividad.
10.3 Modal de alta rápida de actividad.
10.4 Formulario completo de actividad.
10.5 Error de validación si falta responsable.
10.6 Error de validación si faltan horas.
10.7 Error de validación si falta temporada.
10.8 Opción de tarea desde catálogo.
10.9 Opción de tarea libre.
10.10 Confirmación para guardar tarea libre en catálogo.
10.11 Modal de borrado de actividad.
10.12 Vista de detalle de actividad.

Campos visibles en el formulario de actividad:

Fecha.

Terreno.

Temporada.

Responsable.

Tarea.

Horas.

Coste manual.

Descripción.

Compras
11.1 Listado de compras.
11.2 Crear compra.
11.3 Editar compra.
11.4 Vista de detalle de compra.
11.5 Estado vacío sin compras.
11.6 Modal de borrar compra.
11.7 Resumen de coste total y cantidad total.
11.8 Vistas de compra con imputaciones asociadas.

Campos visibles en el formulario de compra:

Fecha.

Producto o material.

Cantidad total.

Coste total.

Temporada.

Imputaciones y consumo
12.1 Modal o pantalla para imputar una compra a terrenos.
12.2 Pantalla de consumo aproximado por terreno.
12.3 Estado de consumo sin compra previa.
12.4 Aviso visible de coste 0 cuando no existe compra previa.
12.5 Confirmación de imputación parcial.
12.6 Vista de reparto de una compra entre varios terrenos.
12.7 Modal de eliminar imputación.
12.8 Estado de suma de imputaciones respecto al total comprado.

Campos visibles en la imputación:

Terreno.

Cantidad consumida.

Coste proporcional.

Observaciones.

Cosechas
13.1 Listado de cosechas.
13.2 Crear cosecha.
13.3 Editar cosecha.
13.4 Vista de detalle de cosecha.
13.5 Estado vacío sin cosechas.
13.6 Modal de borrar cosecha.
13.7 Estado de validación XOR entre rendimiento y litros.
13.8 Aviso si ambos campos se intentan completar.
13.9 Destino desconocido como opción válida con etiqueta visual “Sin destino”.
13.10 Catálogo cerrado de destinos.
13.11 Vista de cosechas por terreno y por temporada.

Campos visibles en el formulario de cosecha:

Fecha.

Terreno.

Temporada.

Producto.

Kgs.

Destino.

Rendimiento.

Litros obtenidos.

Dashboard MVP
14.1 Pantalla única principal del dashboard.
14.2 Filtros persistentes por temporada y terrenos.
14.3 Botón de recarga manual.
14.4 Estado de datos cargados.
14.5 Estado sin datos suficientes.
14.6 Estado de dato incompleto en kg por árbol.
14.7 Comparativa histórica básica.
14.8 Estado de dashboard con una sola métrica o datos parciales.
14.9 Vista desktop con cuatro widgets visibles.
14.10 Vista mobile con los widgets apilados verticalmente.

Widgets obligatorios del dashboard:

Resumen de temporada.
Kg por destino.
Kg por terreno.
Evolución de rendimiento.
El widget de resumen de temporada debe mostrar:

Kg totales.
Litros totales si aplica.
Rendimiento medio.
Kg por árbol.
El widget de kg por terreno debe:

Usar barras verticales.
Ordenarse por kg descendente.
Desempatar alfabéticamente.
No agrupar en “Otros”.
El widget de kg por destino debe:

Incluir la categoría desconocido.
Permitir mostrarla como “Sin destino”.
El widget de evolución de rendimiento debe:

Mostrar serie temporal.

Usar unidad canónica L/100kg.

Mostrar comparativa básica si hay histórico.

Estados transversales
15.1 Empty states.
15.2 Loading skeletons.
15.3 Error states.
15.4 Permission or scope errors.
15.5 Conflict de edición tipo versión desactualizada.
15.6 Toasts o alerts no intrusivos.
15.7 Confirmaciones de acciones destructivas.
15.8 Pantallas de éxito tras guardar.
15.9 Pantallas de validación fallida.

Componentes reutilizables que deben aparecer en el prototipo:

Header o app shell.
Sidebar o navegación superior.
Selector de Workspace.
Selector de temporada.
Tarjetas KPI.
Cards de resumen.
Timeline.
Tablas/listados.
Formularios completos.
Drawers o modales de creación rápida.
Dialog de confirmación.
Chips o badges de estado.
Gráficos de barras.
Gráficos de línea.
Empty states ilustrados.
Skeletons de carga.
Toasts.
Alertas de aviso.
Buscadores y filtros.
Paginación o scroll de listados si aplica.
Flujos que deben estar conectados con navegación real:

Login con Google -> onboarding -> crear Workspace -> primera temporada -> primer terreno -> dashboard.
Login -> selector de Workspace -> diario -> nueva actividad.
Diario -> nueva compra -> imputación a terreno.
Diario -> nueva cosecha -> validación de rendimiento/litros.
Dashboard -> filtros -> recarga manual.
Workspace -> invitar miembro -> aceptar invitación.
Terrenos -> detalle -> editar.
Temporadas -> activar una temporada.
Tareas -> crear desde texto libre en una actividad -> guardar al catálogo.
Cosechas -> detalle -> editar.
Compras -> detalle -> imputaciones.
Borrado con confirmación en cualquier entidad.
Datos de ejemplo que deben existir en el prototipo:

Al menos 2 Workspaces distintos.
Al menos 4 terrenos.
Al menos 2 temporadas.
Al menos 5 trabajadores.
Varias tareas reutilizables.
Varias actividades.
Varias compras.
Varias imputaciones.
Varias cosechas.
Casos con destino desconocido.
Casos con num_arboles ausente para disparar dato incompleto.
Casos con consumo sin compra previa.
Casos con tarea libre guardable en catálogo.
Casos de estado vacío para cada módulo.
Restricciones funcionales:

No diseñar roles granulares.
No diseñar analytics avanzada.
No diseñar predicción meteo o IA.
No diseñar offline o sincronización diferida.
No diseñar login distinto de Google.
No diseñar precio, molturación o balance económico-industrial del cultivo.
Requisitos de comportamiento visual:

Las pantallas deben parecer parte de una misma aplicación real.
Cada módulo debe tener su propio estado vacío, carga, detalle y edición.
Los formularios deben mostrar validaciones visibles.
Las acciones destructivas deben tener confirmación.
Los cambios de contexto de Workspace deben ser muy obvios.
El dashboard debe sentirse como la pantalla más importante del producto.
El diario debe sentirse como la vista principal de trabajo diario.
La experiencia debe funcionar tanto en escritorio como en móvil.
Entrega esperada del prototipo:

Un mapa de navegación completo.
Todas las pantallas listadas arriba.
Todos los modales y formularios asociados.
Ejemplos de estados vacíos, de error y de carga.
Flujos clicables end-to-end.
Consistencia visual completa.
Microcopy en español natural y práctico.