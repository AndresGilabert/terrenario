---
id: "MVP-811"
tipo: bugfix
titulo: "Deuda menor de la revision"
estado: aprobado
prioridad: media
sprint: ""
hito: "Hito H — Ajustes de la segunda revision"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-008--ajustes-mvp-02"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["frontend", "backend", "ux"]
  modulo_path: "03-modulos/"
  componentes: ["plataforma-de-aplicacion", "identidad"]
  etiquetas: ["mvp", "ajustes", "deuda", "contrato"]
  nivel_riesgo: bajo
creado_en: "2026-08-10"
actualizado_en: "2026-08-10"
---

# MVP-811 — Deuda menor de la revision

> **Origen**: `P-116`, `P-117` y `P-118` del registro de `MVP-999`, detectados en la segunda revision
> completa del MVP (2026-08-10).

## Contexto

Tres defectos pequenos, acotados y de causa conocida. Van juntos porque ninguno justifica una historia
propia y los tres se cierran con la misma pasada de verificacion.

**`P-116` — Aviso de React en cada carga de la aplicacion.** La consola del navegador dice, en
cualquier entrada a `/app`: «Cannot update a component (`DataScopeProvider`) while rendering a
different component (`WorkspaceProvider`)». La causa exacta esta en `applyActiveWorkspace`
(`WorkspaceContext.tsx`): `invalidateScope()` se llama **dentro del updater** de `setActiveWorkspace`,
y ese updater lo ejecuta React en fase de render, de modo que resulta un `setState` sobre otro
componente durante el render. Hoy no rompe nada visible —`scopeVersion` es solo una clave de
remontaje—, pero en `StrictMode` el updater corre dos veces y en versiones posteriores de React esto
escala de aviso a fallo. Y lo que sostiene es el mecanismo que corrigio `P-081`, los datos cruzados
entre Workspaces. Los **256 tests del cliente pasan** sin detectarlo, porque ninguno mira la consola.

**`P-117` — Un 404 de enrutado responde con el cuerpo vacio.** `contratos-api.md` dice que las
respuestas de error son «**siempre** JSON con `{ error: { code, message, details } }`». Verificado:
`GET /api/v1/noexiste`, `DELETE /api/v1/seasons` y `GET /api/v1/plots/no-es-un-guid` devuelven `404`
sin cuerpo y sin `Content-Type`. Los 404 de dominio si cumplen (`RESOURCE_NOT_FOUND`,
`SEASON_NOT_FOUND`, comprobados). Es el mismo borde de transporte que `MVP-502` cerro para `P-027` y
`P-043`.

**`P-118` — La pantalla de baja de cuenta describe mal la situacion.** En `DeleteAccountPanel.tsx` el
texto es «Sales de {n} Workspace(s) **compartidos**» con el adjetivo fijo: con uno solo sale «Sales de
1 Workspace compartidos», y cuando la persona es la unica del Workspace afirma que se comparte mientras
la misma pantalla dice mas arriba «Eres la unica persona en este Workspace». En un flujo irreversible,
un texto que describe mal la situacion resta confianza justo donde hace falta.

## Objetivo

Cerrar los tres sin dejar ninguno «para cuando toque la zona», que es como se pierden.

## Requisitos de usuario

### HU-1 — Que el texto de una decision irreversible sea exacto

**Como** persona que esta a punto de eliminar su cuenta,
**quiero** que la pantalla describa con exactitud de que Workspaces salgo,
**para** poder decidir con lo que de verdad va a pasar.

## Alcance (in-scope)

- **`P-116`**: sacar la comparacion fuera del updater de `setState`, o invalidar en un efecto. Con una
  prueba que **falle** ante el aviso: la cobertura que faltaba es la que mira la consola, no una mas de
  las que ya hay.
- **`P-117`**: manejador de rutas no encontradas que devuelva el envoltorio de error canonico,
  reutilizando el formateador existente y sin duplicar el contrato. Cubre tambien el metodo no
  permitido y el tipo de contenido no soportado si comparten el mismo borde.
- **`P-118`**: texto condicional que concuerde en numero y que solo diga «compartido» cuando de verdad
  haya mas gente. Con cobertura de los dos casos.

## Fuera de alcance (out-of-scope)

- Revisar el resto de contextos del cliente en busca del mismo patron de `P-116`: si aparece otro, se
  registra como punto, no se amplia esta historia.
- Cambiar los codigos de error existentes o el envoltorio.
- Reescribir el resto de textos de la baja de cuenta, que se verificaron y son correctos.

## Criterios de aceptación

- [ ] **CA-1**: Entrar en `/app` no produce ningun aviso de React en la consola. Verificado sobre la
  aplicacion en marcha, y fijado con una prueba que falla si el aviso vuelve.
- [ ] **CA-2**: El mecanismo de invalidacion sigue funcionando: cambiar de Workspace remonta el area
  operativa y ninguna vista muestra datos del anterior. Es la garantia de `P-081` y no puede
  degradarse al arreglar el aviso.
- [ ] **CA-3**: `GET /api/v1/noexiste` responde `404` con el envoltorio canonico y su
  `Content-Type: application/json`, y los 404 de dominio siguen respondiendo exactamente igual que
  ahora.
- [ ] **CA-4**: La pantalla de baja de cuenta concuerda en numero y solo habla de Workspaces
  «compartidos» cuando hay mas de una persona. Comprobados los dos casos.

## Notas y decisiones

- **`P-116` no es cosmetico aunque hoy no tenga sintoma.** Es el mismo criterio que ya rige para los
  avisos del compilador: se corrigen siempre, salvo que el arreglo tenga consecuencias peores. Y `CA-2`
  esta para asegurar que el arreglo no las tiene.
- **La prueba de `CA-1` es la parte que aporta.** Que 256 tests pasaran mientras la consola avisaba en
  cada carga es el hallazgo real: sin una prueba que mire ahi, el siguiente aviso tampoco lo vera
  nadie.
