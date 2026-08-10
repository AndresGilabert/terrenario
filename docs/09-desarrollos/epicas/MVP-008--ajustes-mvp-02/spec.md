---
id: "MVP-008"
tipo: epica
titulo: "Ajustes MVP 02"
estado: aprobado
prioridad: alta
hito: "Hito H — Ajustes de la segunda revision"
tickets: []
historias: ['MVP-801', 'MVP-802', 'MVP-803', 'MVP-804', 'MVP-805', 'MVP-806', 'MVP-807', 'MVP-808', 'MVP-809', 'MVP-810', 'MVP-811', 'MVP-899']
depende_de: ["MVP-007"]
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["ux", "producto", "calidad", "gobernanza"]
  modulo_path: "03-modulos/"
  componentes: ["dashboard", "diario", "produccion", "maestros", "identidad", "plataforma"]
  etiquetas: ["mvp", "ajustes", "segunda-revision"]
  nivel_riesgo: medio
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# EPICA MVP-008 — Ajustes MVP 02

## Contexto

`MVP-007` cerro el 2026-08-09 y su release `v0.7.1` esta publicada. La segunda revision completa del
MVP (2026-08-10, registrada en `MVP-999`) se hizo **contra el sistema en marcha** —API y base de datos
reales mas navegador conducido— y dio doce puntos nuevos, `P-107` a `P-118`, mas el triaje de `P-095`,
que llevaba desde `MVP-702` sin destino.

El contraste numerico de partida **cuadro entero**: 4.460,50 kg, 930,65 L, 20,86 L/100kg, 17,16
kg/arbol y 390 € de gasto, todos verificados contra la base de datos. Los hallazgos no salieron de ahi,
sino de **provocar los escenarios que la propia KB anticipa** y comprobar si el producto se comporta
como dice que se comporta. Tres ejemplos de lo que aparecio:

- `RN-008` existe porque «desde `MVP-705` el filtro viaja en la URL y al cambiar de Workspace puede
  quedar el de otro». Tres de las cuatro vistas operativas cumplen la regla. La cuarta —la Vision
  General— no, y el resultado es que **le dice al usuario que cree una temporada mientras su propio
  selector lista las tres que ya tiene** (`P-107`).
- De los 47 requisitos de usuario, **solo tres se citan fuera del documento que los define**. Las
  epicas trazan contra `RN-xxx` y nadie traza contra `RU-xxx`, asi que un requisito puede quedarse
  marcado «Estado: MVP» durante todo el roadmap sin que nada lo delate (`P-114`). Tres de los hallazgos
  de esta revision son consecuencias de ese mismo agujero, no despistes independientes.
- La fuente de iconos pesa **3,78 MB**, el 68 % de la primera carga, para usar unas decenas de glifos,
  en un producto cuyo usuario objetivo trabaja con mala cobertura (`P-115`).

Con el MVP ya en uso se repasaron ademas **los 24 puntos heredados en `backlog-post-mvp`**, decididos
casi todos antes de publicarlo. Seis pasan aqui: son los que tienen la premisa cumplida o un hueco sin
alternativa.

## Objetivo

Cerrar los defectos y los huecos que la segunda revision dejo con destino, y **quitarle al proceso la
dependencia de que alguien se acuerde**: que un requisito marcado MVP no pueda quedarse sin destino sin
que el gate lo diga, igual que `P-096` hizo con el registro de puntos.

## Requisitos de usuario de alto nivel

- **Como** Antonio (titular de la explotacion), **quiero** que la pantalla de resumen me ensene lo que
  tengo y no me pida crear lo que ya existe, **para** poder fiarme de lo que leo.
- **Como** Antonio, **quiero** que un enlace a una lista filtrada reproduzca lo que yo estaba viendo,
  **para** poder volver a ello y compartirlo.
- **Como** Antonio, **quiero** poder saber quien apunto o cambio cada registro, **para** aclarar una
  discrepancia sin tener que preguntar uno por uno.
- **Como** miembro de un Workspace ajeno, **quiero** poder salir de el, **para** no arrastrar
  indefinidamente una explotacion en la que ya no colaboro.
- **Como** responsable del producto, **quiero** que la KB delate por si sola un requisito sin destino,
  **para** no descubrirlo dos revisiones tarde.

## Alcance

- Coherencia del ambito de temporada en la Vision General y en el diario, y filtros de cosechas y
  compras en la URL.
- Maqueta adaptada de Cosechas en movil y tableta.
- Autoria visible de los registros operativos y aviso de cosecha duplicada.
- Depuracion de maestros: borrado de lo nunca usado y fusion de duplicados.
- Ciclo de vida de la membresia: abandonar un Workspace y coherencia de la revocacion.
- Avisos in-app de lo que hoy depende de que llegue un correo, en alcance minimo.
- Trazabilidad de los requisitos de usuario, con comprobacion en el gate de KB.
- Peso de la primera carga.
- Deuda menor de la revision: aviso de React, envoltorio de error en 404 de enrutado y un texto que
  describe mal una situacion irreversible.

## Fuera de alcance

- **Planificacion y sugerencia de tareas** (`RU-32`/`RU-33`/`RU-34`, `P-111`): es alcance de epica
  propia —entidad de plan, motor de recurrencia sobre el historico, senal de omision y superficie
  propia— y meterlo aqui convertiria la epica en otra cosa. Lo que si entra es **corregir su estado** en
  el documento de requisitos.
- **Codigo por email en la baja de cuenta** (`P-112`): decision del PO de mantener la frase tecleada.
  Entra solo la correccion documental de `RU-36`.
- **Modelo de produccion ampliado** (`P-059` a `P-063`): sigue en backlog, Hito I.
- **Centro de notificaciones completo** (`RU-31`): `MVP-808` entrega el minimo que quita la dependencia
  del correo, no la generalizacion.
- **Discriminador de homonimos** (`P-044`, `P-047`) y **exportacion de datos** (`P-070`): los dos
  exigen una decision de diseno previa que a la escala actual del producto no se puede tomar con
  informacion.
- **Cobertura E2E de navegador** (`P-064`): sigue descartada por el mismo motivo, y se comprobo que no
  habria cazado `P-107` ni `P-108`.
- **Roles y permisos granulares** (`RU-13`): `P-074` sigue en backlog justamente por depender de eso.

## Criterios de aceptación de la épica

- [ ] **CA-1**: Todas las historias de la epica estan en estado `completado`.
- [ ] **CA-2**: Cambiar de Workspace desde cualquier vista operativa deja las cuatro —diario, cosechas,
  compras y Vision General— mostrando el ambito del Workspace elegido, y ninguna afirma en pantalla un
  ambito distinto del que aplica. Verificado sobre el sistema en marcha con el escenario que produjo
  `P-107` y `P-108`: un `season_id` de otro Workspace en la URL.
- [ ] **CA-3**: Un enlace a cualquiera de las cuatro vistas operativas reproduce los filtros que veia
  quien lo comparte, y recargar no los pierde.
- [ ] **CA-4**: El gate de KB **falla** si un requisito de usuario marcado «Estado: MVP» no tiene
  destino declarado. Comprobado provocando el fallo, no solo leyendo la regla.
- [ ] **CA-5**: La primera carga de la aplicacion baja de los 5,57 MB medidos, con el desglose antes y
  despues.
- [ ] **CA-6**: Ningun punto con destino `MVP-008` en el registro de `MVP-999` queda sin historia que lo
  construya, y ninguna fila del registro sigue diciendo `triado` con el trabajo hecho.

## Historias de esta épica

> Ver `_indice.md` para el estado actualizado.

| Historia | Puntos que cierra |
|---|---|
| `MVP-801` — Coherencia del ambito de temporada | `P-107`, `P-108` |
| `MVP-802` — Filtros de cosechas y compras en la URL | `P-109` |
| `MVP-803` — Cosechas en movil y tableta | `P-095` |
| `MVP-804` — Autoria visible de los registros operativos | `P-113` |
| `MVP-805` — Aviso de cosecha duplicada | `P-110` |
| `MVP-806` — Depuracion de maestros: borrado y fusion | `P-036`, `P-041` |
| `MVP-807` — Ciclo de vida de la membresia | `P-048`, `P-049` |
| `MVP-808` — Avisos in-app que no dependan del correo | `P-011`, `P-029` |
| `MVP-809` — Trazabilidad de los requisitos de usuario | `P-114`, y las correcciones de `P-111` y `P-112` |
| `MVP-810` — Peso de la primera carga | `P-115` |
| `MVP-811` — Deuda menor de la revision | `P-116`, `P-117`, `P-118`, nota de entorno de `P-069` |
| `MVP-899` — Revision epica | — |

## Secuenciacion recomendada

1. **`MVP-801` primero.** Es el unico que corrige algo que el usuario lee como falso, y `MVP-802`
   construye sobre el ambito que deja fijado.
2. **`MVP-802` despues de `MVP-801`, nunca antes.** Llevar los filtros a la URL es justo lo que expone
   el defecto de `P-107`/`P-108`: hacerlo primero lo propagaria a dos vistas mas.
3. **`MVP-805` despues de `MVP-803`**, porque las dos tocan la superficie de Cosechas.
4. **`MVP-806` antes que `MVP-807`**: la fusion de maestros deja el terreno limpio para el ciclo de
   vida de la membresia, y `P-022` pierde casi todo su motivo en cuanto exista.
5. **`MVP-809`, `MVP-810` y `MVP-811`** son independientes de todo lo anterior y pueden ir en paralelo.

## Vinculacion con prototipo (fuente visual)

Regla de precedencia, igual que en el resto de epicas:

- La fuente de verdad funcional y de requisitos es la KB.
- El prototipo (`prototype/terrenario-mvp`) aporta referencia visual y de flujo.
- Si hay contradiccion, prevalece la KB.

## Reglas de negocio que esta epica modifica

| Regla | Cambio | Historia |
|---|---|---|
| `RN-007` — Conservacion de filtros en recarga | Deja de regir solo en dashboard y diario: los filtros de cosechas y compras pasan tambien a la URL | `MVP-802` |
| `RN-008` — Filtro por defecto inicial | Se precisa que la caida al defecto ante un `season_id` desconocido aplica **tambien al dashboard**, y que el ambito devuelto por el servidor manda sobre la seleccion que traiga la URL | `MVP-801` |
| `RN-034` — Permisos planos por Workspace | Se matiza con la salida voluntaria de un miembro y con la revocacion entre copropietarios | `MVP-807` |
| `RN-037` — Borrado de registro operativo con confirmacion | Se extiende el criterio a los **maestros sin uso historico**, que hoy solo se pueden inactivar | `MVP-806` |

## Notas y decisiones

- **El hito reordena el roadmap**, con el mismo argumento con el que `MVP-007` lo reordeno: los ajustes
  salen del uso real y son previos a cualquier evolucion. `Hito H` pasa a ser esta epica; `Resiliencia
  offline` se desplaza a `Hito I` y `Escalado funcional` a `Hito J`.
- **El hallazgo de fondo de la revision es de proceso, no de producto.** `P-114` es a los requisitos lo
  que `P-096` fue al registro de puntos: una cadena que solo se sostiene si alguien se acuerda. Por eso
  `CA-4` exige **provocar el fallo del gate**, no leer la regla.
- **Seis puntos heredados entran por premisa cumplida, no por severidad.** El «sin uso historico» de
  los maestros ya es comprobable (`P-036`/`P-041`), no existe ninguna via para abandonar un Workspace
  (`P-048`) y una decision irreversible depende hoy de que llegue un correo (`P-029`). Los otros
  dieciocho siguen en backlog, y siete de ellos porque exigen una decision de diseno que a la escala
  actual del producto no se puede tomar con informacion.
- **Ningun punto de esta epica se apoya en «lo hara la historia de al lado»**, que es la leccion que
  `MVP-007` dejo escrita en su `CA-6` a partir de `P-055`.
