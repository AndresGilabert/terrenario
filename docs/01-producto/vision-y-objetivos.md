---
bloque: 01-producto
documento: vision-y-objetivos
actualizado_en: "2026-07-17"
---

# Visión y Objetivos del Producto

## Visión y Objetivos: Plataforma de Gestión Agrícola Personal

### Visión:

Crear una plataforma centralizada y sencilla que sirva como herramienta digital para el registro, seguimiento y análisis integral de pequeñas explotaciones agrícolas personales. Su objetivo es transformar la gestión manual y fragmentada del trabajo agrícola en un proceso estructurado y medible, permitiendo a los propietarios tener visibilidad completa sobre su rendimiento, costes y recursos sin requerir una actividad profesional dedicada exclusivamente al sector.

## Reglas de negocio que definen el producto

Estas reglas impactan directamente cómo se modela el sistema y sus validaciones.

| # | Regla | Definición |
|---|-------|-----------|
| R1 | Unidad base = Terreno | Todo registro (actividad, cosecha, consumo material, coste) DEBE estar asociado a un terreno. Los terrenos pueden ser propios o cedidos, gestionados simultáneamente desde el día 1. |
| R2 | Responsable: maestro de trabajadores con alcance por Workspace | Toda actividad requiere responsable + horas. El responsable es una entidad Trabajador del mismo Workspace que el registro: puede tener cuenta vinculada a usuario de la plataforma (auto-registrado automáticamente) o no tenerla (trabajador externo creado como ficha independiente). Todos los miembros de un Workspace aparecen en la lista de responsables; si alguien tiene una tarifa horaria configurada se recomienda usarla pero se permite coste manual. |
| R3 | Coste contextual: calculado para trabajadores internos con tarifa, manual para externos sin tarifa | Los costes de materiales se calculan automáticamente desde la compra vinculada (cantidad aplicada × precio unitario). El coste mano de obra del trabajador interno depende de su tipo: trabajador con cuenta (coste automático `horas * tarifa` si tiene tarifa configurada) o trabajador externo sin cuenta (coste manual obligatorio al crear el registro). El usuario puede sobrescribir cualquier importe en cualquier momento. |
| R4 | Compra + consumo aproximado | Se registra la compra (producto, cantidad total, coste total). Antonio asigna consumos aproximados por terreno. Coste proporcional si vinculado a compra específico. NO hay stock tracking ni saldo acumulativo. Los excedentes se pierden naturalmentc y desaparecen sin rastro. Cuando no existe una compra previa del producto el coste en los registros de aplicación queda a 0 con un aviso para rellenar. Si posteriormente se registra la compra NO se recalculan costes históricos retroactivamente. |
| R5 | Cosecha: peso bruto siempre + cálculo automático dimensiones (litros o rendimiento) | El registro de cosecha siempre incluye peso_bruto_kg (obligatorio). Además debe informarse al menos uno de estos datos netos: rendimiento_pct (L/100kg o kg/100kg) o litros_obtenidos. El sistema completa automáticamente el campo faltante (litros a partir del rendimiento, o viceversa). Un solo registro de cosecha lleva siempre los valores de kg Y litros (si es oliva / cultivo con molienda), ambos visibles en el dashboard. No se permite registrar una cosecha como unidad única excluyente porque las dimensiones complementarias son obligatorias cuando aplican. |
| R6 | Multi-usuario y Workspaces en MVP v1: la unidad organizativa de negocio es el Workspace (explotación) | El producto nace con soporte multi-usuario desde el día 1. La unidad orgamizativa es el Workspace (explotación). Cada usuario puede crear múltiplestrabajos o unirse a otros por invitación y alternar entre ellos mediante un selector visible en la UI. Todos los miembros de un mismo Workspace pueden ver y editar todos los registros del Workspace sin restricciones granulares. Roles y permisos granulares se definen en fases posteriores. |

## Corte del alcance MVP v1

De las funcionalidades definidas, esta es la separación confirmada:

### ✅ Incluidas en MVP v1

| # | Funcionalidad | Alcance v1 |
|---|--------------|------------|
| 1 | Gestión de Terrenos | Propios + cedidos. Campos mínimos: nombre, tipo_propiedad, ubicación, metadatos_suelo simple (JSONB). Todos vinculados al Workspace activo. |
| 2 | Registro de Actividades y Recursos con maestro de Trabajadores | Qué se hizo, quién, cuándo, en qué terreno, duración. Responsable = entidad Trabajador del Workspace (puede tener cuenta o no). Coste manual obligatorio para externos sin tarifa / calculado automáticamente `horas * tarifa` para internos con tarifa. |
| 3 | Gestión de Producción | Producto, peso bruto kg + datos netos: al menos uno de {rendimiento, litros_obtenidos}. El sistema completa automáticamente el segundo valor del par. Ambos valores (kg y litros si es oliva) se reflejan en el dashboard para el Workspace activo. Terreno fuente vinculado al Workspace. |
| 4 | Compras + consumo aprox por terreno | Registrar producto comprado y costo asignado a terrenos. Sin stock tracking ni saldo acumulado. Excedentes naturales no regulados (desaparecen sin rastro). Si no hay compra previa: coste 0 con aviso para rellenar después; sin recálculo retroactivo. |
| 5 | Dashboard operativo MVP por Workspace | Dashboard simple con 4 widgets iniciales (resumen de temporada, kg por destino, kg por terreno, evolución de rendimiento) filtrado al Workspace activo. Consulta rápida y comparación histórica básica. Al seleccionar un Workspace diferente el dashboard se recalcula completo desde los datos de ese Workspace. |
| 6 | Multi-usuario + Workspaces + invitaciones | Cada usuario puede crear múltiples WorKSpaces o unirse a otros por invitación mediante link/email válido. Selector de contexto visible en la UI para alternar entre WorkSpaces abiertos. Todos los miembros del mismo Workspace tienen permisos completos de lectura/escritura sobre todos los registros del Workspace (sin roles granulares en MVP). |
| 7 | Maestro de trabajadores con ámbito de Workspace | Crear, editar y listar trabajadores propios de cada Workspace, activos o inactivos. Trabajadores pueden tener cuenta vinculada o no. Los miembros del Workspace siempre aparecen como opcionales como responsables al crear una actividad. Sin límite de registros por Workspace. |
| 8 | Acceso por Google OIDC (menor fricción posible) + flujo de invitación | Registro con mínimos pasos. No se exige contraseña local en MVP. El login principal es vía Google. La autenticación es obligatoria para todos los usuarios del sistema desde el día uno, incluso aquellos que actúan como trabajadores internos sin cuenta propia. No hay acceso anónimo. |

### ❌ Excluidas de MVP v1

| # | Funcionalidad | Destino |
|---|--------------|---------|
| 1 | Inteligencia de Negocio/Analytics Avanzado | v2+: analítica profunda, exploración ad-hoc y comparativas avanzadas |
| 2 | Datos Colaborativos/anónimos | v3+ |
| 3 | Análisis Predictivo Meteo + IA | v2+ |
| 4 | Control acceso Multi-Usuario granular (roles/permisos por recurso) | fases posteriores, después de validar el producto MVP con usuarios reales |

## Problemas que resuelve:

1. **Gestión Fragmentada:** Los agricultores carecen de una única fuente de verdad para gestionar múltiples terrenos con diversas actividades, costes y cosechas.
2. **Trazabilidad Incompleta del Trabajo:** Es difícil registrar de forma sistemática cuándo, quién y cuánto tiempo se dedicó a cada tarea en diferentes terrenos.
3. **Control Financiero/Material:** Existe dificultad para llevar un control consolidado e inmediato de los costes asociados (materiales comprados, mano de obra) y su uso específico por terreno o actividad.
4. **Análisis Desconectado:** La información recolectada es puntual o no se puede correlacionar fácilmente con el rendimiento total, impidiendo la optimización basada en datos.
5. **Falta de conocimiento:** Los agricultores personales carecen de una formación y experiencia profesional para tomar decisiones colegiadas para optimizar las tareas y el rendimiento de las cosechas.

## Funcionalidades Clave:

1. **Gestión de Terrenos:** Capacidad para registrar y administrar múltiples terrenos gestionados por el usuario dentro de cada Workspace, distinguiendo entre propiedades propias o cedidas. Incluye registro completo de terrenos (propios o cedidos) con metadatos específicos del cultivo/suelo inicial. Todos los terrenos están vinculados a un Workspace.

2. **Registro de Actividades y Recursos (Mano de Obra/Costes):** Implementar un módulo que permita registrar trabajos específicos (qué se hizo), asociar fecha y responsable (entidad Trabajador del Workspace, puede tener cuenta o no), cuantificar el tiempo dedicado y los costes asociados, y mantener trazabilidad total de productos/materiales comprados (fertilizantes, fitosanitarios), obligando a especificar el terreno exacto de consumo.

3. **Gestión de Producción:** Módulo para registrar sistematizadamente las cosechas, especificando producto recolectado, peso bruto siempre y datos netos complementarios calculados automáticamente (litros o rendimiento). Cada registro de cosecha mantiene los campos dimensionales completos. Registros multidimensionales excluidos v1 → solo un campo por registro.

4. **Inventario y Consumo:** Registro detallado de productos o materiales adquiridos (fertilizantes, fitosanitarios). Es vital la capacidad de rastrear dónde y en qué terreno se ha consumido cada material. Sin stock tracking ni saldo acumulado. Excedentes naturales no regulados desaparcen sin rastro.

5. **Inteligencia de Negocio (Dashboard):** Un dashboard centralizado que proporcione análisis consolidado de producción general y desagregada por terreno, con capacidad de desglosar métricas a nivel de unidad biológica (por árbol/planta) dentro de un terreno. Filtrado por Workspace: los datos mostrados pertenecen exclusivamente al Workspace activo.

6. **Datos Colaborativos:** Capacidad de generar estadísticas agregadas y anónimas a partir de datos compartidos por la comunidad de usuarios en fases posteriores.

7. **Análisis Predictivo:** Un motor estadístico que cruce la producción con las condiciones meteorológicas (integración Meteo API) para sugerir ventanas temporales óptimas para realizar tareas o cosechar, maximizando el rendimiento biológico. No solo reporta el clima actual, sino que modela: "Si la semana próxima habrá X precipitación, debes realizar Y tarea en Z terreno para optimizar C cosechas" (Combinación de Meteo + IA).

8. **Control de Acceso Multi-Usuario:** El sistema soporta multi-usuario desde el día 1 mediante Workspaces y permisos iguales por miembro del Workspace. Roles granulares se definirán en fases posteriores.

9. **Simplicidad Nivel "Usuario Final":** Mantener la alta tecnología de IA y análisis, pero presentarla con la interfaz de un diario personal altamente potente, priorizando la acción sencilla sobre los gráficos complejos para el usuario non-profesional. La vista principal es cronológica (diario por fecha).

## Objetivos estratégicos

| Objetivo | Métrica de éxito | Horizonte |
|----------|-----------------|-----------|
| Lanzar MVP funcional con soporte multi-usuario + Workspaces | 100% de funcionalidades #1-#8 implementadas y validadas con Antonio | Q3 2026 |
| Alcanzar al menos 3 Workspaces activos en los primeros 90 días | 3 WorkSpaces | Q4 2026 |
| Mantener tiempo medio de registro de actividad/día inferior a 2 minutos | <= 2 min por registro | Continuo |

## KPIs principales

> Ver detalle en `kpis.md`.

| KPI | Valor actual | Objetivo |
|-----|-------------|---------|
| Workspaces activos con registros completos en temporada | 0 | >= 3 en Q4 2026 |
| Registros de cosecha con destino informado | Pendiente baseline | >= 80% |
| Conversión login (pantalla a éxito) | Pendiente baseline | >= 85% |

## Roadmap de alto nivel

> Ver detalle en `roadmap.md`.

| Horizonte | Foco |
|-----------|------|
| Corto plazo (0-3 meses) | MVP: multi-usuario, Workspaces, 8 funcionalidades #1-#8, maestro de trabajadores, catálogo fijo de destinos |
| Medio plazo (3-6 meses) | Catálogo de tareas configurable, sugerencias por época/recurrencia, permisos granulares por Workspace |
| Largo plazo (6-12 meses) | Análisis predictivo meteo + IA, estadísticas colaborativas anónimas, Passkeys, offline mejorado |

## Decisión de identidad de marca

- Nombre comercial adoptado: **Terrenario**.
- Dominio principal adoptado: **terrenario.com**.
- Fecha de decisión: **2026-07-13**.
- Criterio: nombre de marca claro, con presencia y no limitado a un cultivo específico.
- Impacto: se utilizará como referencia principal en comunicación de producto, web y materiales comerciales.

## Resumen funcional confirmado (pre-roadmap / pre-ADR)

Este producto es una plataforma de gestión agrícola personal orientada a pequeñas explotaciones de olivar y otros cultivos familiares. El MVP prioriza simplicidad y utilidad diaria para un perfil no técnico, con foco en registrar datos operativos y convertirlos en visibilidad accionable. La arquitectura nace multi-usuario con Workspaces desde el día uno.

### Dashboard MVP como pieza central

- El dashboard es parte del MVP y se consulta en una sola pantalla con scroll vertical.
- No hay actualización continua en segundo plano en MVP: los datos se refrescan al abrir o al recargar manualmente.
- Al recargar manualmente, se conservan los filtros activos.
- Filtro por defecto en primer acceso: todos los terrenos del Workspace activo + temporada activa.
- En MVP no se muestra marca de "última actualización" para evitar ruido visual.

### Widgets iniciales confirmados

1. Resumen de temporada (producción total en kg, litros si aplica, rendimiento medio, kg/árbol)
2. Kg por destino de venta/uso + litros por destino (si aplica)
3. Kg por terreno (ambas dimensiones cuando el producto lo requiere)
4. Evolución de rendimiento de fruto

### Reglas clave de lectura de datos

- Si faltan árboles en algún terreno, el KPI global de kg/árbol excluye esos terrenos y muestra "dato incompleto".
- El gráfico de kg por terreno usa barras verticales, sin agrupación en "Otros", con orden fijo por kg descendente y desempate alfabético.
- El widget de destinos incluye categoría "Sin destino" como categoría válida.
- La evolución de rendimiento usa unidad canónica L/100kg y permite entradas equivalentes (L/100kg, kg/100kg o cálculo desde kg entregados + litros obtenidos).
- En comparativas históricas se muestra promedio histórico disponible; los promedios 5y/10y solo aparecen cuando exista suficiente histórico.
