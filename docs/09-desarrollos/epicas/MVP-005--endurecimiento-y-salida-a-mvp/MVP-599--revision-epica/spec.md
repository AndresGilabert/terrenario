---
id: "MVP-599"
tipo: feature
titulo: "Revision epica"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-501", "MVP-502", "MVP-503", "MVP-504"]
bloquea: []
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["gobernanza", "calidad", "scope-control"]
  modulo_path: "03-modulos/"
  componentes: ["backlog", "qa", "stabilization"]
  etiquetas: ["mvp", "revision-epica", "cierre"]
  nivel_riesgo: medio
creado_en: "2026-07-24"
actualizado_en: "2026-08-04"
---

# MVP-599 — Revision epica

## Contexto

Durante la ejecucion de una epica aparecen ajustes, puntos ciegos y necesidades no previstas en las historias originales. Si no se centralizan antes del cierre, se dispersan y se pierde trazabilidad para decidir el trabajo posterior.

## Objetivo

Ejecutar una revision final de la epica para validar el funcionamiento global, consolidar los pendientes detectados y convertirlos en nuevas historias planificables.

## Requisitos de usuario

### HU-1 — Consolidar pendientes de la epica

**Como** Product Owner,
**quiero** reunir en un solo punto los ajustes y requisitos detectados durante la epica,
**para** evitar omisiones y cerrar el alcance con trazabilidad.

### HU-2 — Verificar calidad funcional final

**Como** equipo de producto y desarrollo,
**quiero** revisar el estado final de la epica sobre el flujo integrado,
**para** abrir nuevas historias concretas con evidencias de error o falta.

## Alcance (in-scope)

- Revision integral del comportamiento entregado por la epica.
- Consolidacion de puntos ciegos y requisitos pendientes detectados durante las historias previas.
- Creacion de nuevas historias para cubrir errores, faltas o ajustes detectados.
- Priorizacion inicial de los nuevos items segun impacto funcional y de negocio.

## Fuera de alcance (out-of-scope)

- Implementar en esta historia los nuevos cambios detectados.
- Redefinir objetivos de negocio ya aprobados para la epica.
- Sustituir actividades de QA o validacion tecnica de historias previas.

## Criterios de aceptación

- [x] **CA-1**: Existe una revision funcional final de la epica con evidencias de lo validado.
- [x] **CA-2**: Todos los puntos ciegos o requisitos pendientes detectados quedan documentados.
- [x] **CA-3**: Cada punto pendiente se transforma en una nueva historia en la epica correspondiente o en MVP-999 cuando aplique.

## Maquetas y referencias visuales

- No aplica para esta historia de gobierno de alcance.

## Notas y decisiones

- Esta historia debe ejecutarse siempre como cierre de la epica.
- No se marca la epica como completada hasta cerrar esta historia.

---

## Resultado de la revisión (2026-08-04)

### Cómo se hizo

**No es una relectura de las historias.** Se levantó el sistema entero —API sobre PostgreSQL real y
cliente— y se recorrió el flujo que entrega la épica con una sesión autenticada, sembrando antes
volumen suficiente para que la paginación de `MVP-506` se ejercitara de verdad: con los 8 registros
que había en local no se prueba nada, así que se crearon 28 labores hasta llegar a **36 registros**.

Cuatro hallazgos, **tres corregidos en esta misma rama** y uno derivado.

### Qué se verificó, y con qué evidencia

| Historia | Qué se comprobó | Resultado |
|---|---|---|
| `MVP-501` | Las dos suites completas sobre `develop` integrado | ✅ **674 backend · 98 cliente** en verde |
| `MVP-502` | Cabeceras de seguridad de la API contra las cuatro documentadas | ✅ Coinciden exactamente |
| `MVP-502` | Aislamiento por Workspace: `X-Workspace-Id` con un identificador ajeno | ✅ **La cabecera se ignora**, manda el token. Devolvió los datos del Workspace del JWT |
| `MVP-502` | Contrato de error en `PATCH` con tipo incorrecto y con valor vacío | ⚠️ Código correcto, **mensaje engañoso** → `R-04` |
| `MVP-503`/`MVP-505` | Panel de privacidad en Ajustes contra el inventario corregido | ❌ **Desfasado e incompleto** → `R-02`, `R-03` |
| `MVP-505` | Páginas legales públicas y enlaces desde el acceso | ✅ Ambos enlaces resuelven; **cero peticiones externas** al cargar |
| `MVP-505` | Baja de cuenta: obligaciones, sesiones, Workspaces y plazo | ✅ Enumera los 3 Workspaces de propiedad única, 15 sesiones y los 24 meses |
| `MVP-504` | Rutina de expurgo arrancando la API de verdad | ✅ `Expurgo completado (RN-041)` en el primer arranque |
| `MVP-506` | Paginación, búsqueda, filtro por responsable y tope de límite | ✅ 20+16 de 36; búsqueda 5/5 contra API y UI; `limit=500` topado a **100** |
| `MVP-506` | Totales de cabecera bajo filtro | ✅ Se recalculan con el filtro, no muestran el total global |
| `MVP-506` | Etiquetas accesibles de los cinco filtros | ✅ `sr-only` con `htmlFor` en todos |

### Hallazgos

#### `R-01` · Los filtros del diario no persisten en la URL, y los del dashboard sí — **derivado**

Cambiar de página, buscar o filtrar por responsable en el diario deja la URL en `/app/diario`, sin
rastro. En Visión General la misma acción produce
`/app/vision-general?season_id=5ff659f0-…`, porque `VisionGeneralView` usa `useSearchParams` como
fuente única y así se decidió expresamente.

No es un defecto de `MVP-506` —sus criterios no lo pedían— pero **es una decisión ya tomada que no se
aplicó a la vista que más la necesita**: el diario tiene cinco filtros y paginación, así que es donde
más duele no poder compartir un enlace ni volver a la página 3 después de abrir un registro.

Arreglarlo no es menor: hay que meter la URL como fuente única conviviendo con el rebote de 350 ms de
la búsqueda y con la guarda de respuestas obsoletas. Derivado a `MVP-999` (`P-072`).

#### `R-02` · El panel de privacidad seguía negando la analítica — **corregido aquí**

Ajustes → Privacidad afirmaba *«No usamos analítica, publicidad ni perfilado»*: exactamente la
afirmación absoluta que `MVP-503` declaró inexacta (`R-05`) y que `MVP-504` corrigió **en la Política
de Privacidad**. Nadie tocó el panel, así que los dos documentos del producto se contradecían entre
sí.

Ahora dice *«No hay analítica de terceros, publicidad ni perfilado… más la medición del acceso que ves
abajo, que es propia y no te identifica»*, alineado con la Política.

#### `R-03` · El «inventario completo» listaba cuatro de siete — **corregido aquí**

La Política remite a este panel llamándolo *el inventario completo*. Listaba cuatro tecnologías y
faltaban **`pkce_code_verifier`, `oauth_state`, `terrenario_post_login_redirect` y las dos claves de
medición del embudo** — justo las cinco que `R-03` de `MVP-503` había añadido al inventario de la KB.
La corrección se quedó en la documentación y no llegó al producto.

Ahora lista las siete. Y como se desfasó **precisamente por no tener test**, se añade
`PrivacyPanel.test.tsx`, que ancla el panel al inventario: si desaparece una tecnología declarada o
vuelve la afirmación absoluta, falla el build.

#### `R-04` · Un error de tipo se reportaba como error de codificación — **corregido aquí**

`PATCH /api/v1/plots/{id}` con `{"name": 12345}` respondía:

> `VALIDATION_FORMAT_INVALID` — «El campo 'name' no se puede leer: el cuerpo de la petición debe estar
> codificado en UTF-8.»

El cuerpo era UTF-8 perfectamente válido. `JsonElement.GetString()` lanza `InvalidOperationException`
por **dos** motivos y `MVP-502` solo contempló uno: además del transcodificado inválido, falla cuando
el valor no es texto. El código de error y el 400 eran correctos; el mensaje mandaba a quien integra a
revisar su codificación en lugar de su tipo.

Ahora responde «El campo 'name' debe ser un texto», con cuatro casos cubiertos —número, booleano,
array y objeto— y una aserción que impide que el mensaje del UTF-8 vuelva a colarse ahí.

### Veredicto por criterio de la épica

| CA de `MVP-005` | Veredicto |
|---|---|
| **CA-1** — Todas las historias en `completado` | ✅ Las siete, con esta |
| **CA-2** — Gates mínimos de tests en verde | ✅ **678 backend · 101 cliente**, ejecutados automáticamente en cada PR desde `MVP-504`. Antes de esa historia el CI no compilaba siquiera |
| **CA-3** — Evidencia documental de salida controlada | ✅ Checklist de cumplimiento verificado contra el sistema (`MVP-503`) y gate con los cuatro bloqueos cerrados (`MVP-504`) |

**La épica se cierra.** El MVP queda desplegable a `staging` sin reservas y a producción con los
criterios de promoción del gate por cumplir.

### Lo que esta revisión deja dicho para la siguiente

Los tres hallazgos corregidos comparten patrón, y conviene verlo: **una corrección se aplicó a la
documentación y no al producto** (`R-02`, `R-03`), o **se resolvió el caso que se tenía en mente y no
el hermano** (`R-04`). Ninguno lo habría cazado un test de los que había, porque nadie probaba esos
artefactos. Los tres se cierran con test, no solo con el arreglo.
