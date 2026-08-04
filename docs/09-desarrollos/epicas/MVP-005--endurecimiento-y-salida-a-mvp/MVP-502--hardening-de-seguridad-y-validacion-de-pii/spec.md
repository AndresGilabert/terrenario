---
id: "MVP-502"
tipo: feature
titulo: "Hardening de seguridad y validación de PII"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-001", "MVP-003", "MVP-004"]
bloquea: ["MVP-503", "MVP-504"]
relacionado_con: ["MVP-105", "MVP-601"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seguridad", "privacidad"]
  modulo_path: "03-modulos/"
  componentes: ["auth", "authorization", "logging", "pii-controls"]
  etiquetas: ["mvp", "security", "privacy"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-31"
---

# MVP-502 — Hardening de seguridad y validación de PII

## Contexto

El MVP trata identidad, membresía y datos operativos con componentes que pueden contener PII. La KB exige no exponer PII en logs, errores o URLs, y reforzar validación, autorización y seguridad por defecto antes de producción.

## Objetivo

Reducir el riesgo operativo del MVP reforzando controles de autenticación, autorización, validación de entrada y tratamiento seguro de PII.

## Requisitos de usuario

### HU-1 — Operar con seguridad por defecto

**Como** responsable técnico,
**quiero** que el MVP falle de forma segura y limite exposición de datos,
**para** no validar usuarios reales sobre una base débil.

### HU-2 — Evitar fugas de datos personales

**Como** responsable de cumplimiento,
**quiero** que logs, errores y flujos críticos eviten PII sensible en claro,
**para** respetar privacidad por diseño en el MVP.

## Alcance (in-scope)

- Revisión y refuerzo de autorización por Workspace.
- Revisión de validación de entrada en bordes API.
- Revisión de logs, errores y trazabilidad para evitar PII sensible en claro.
- Revisión de manejo seguro de identidad social y tokens.

## Fuera de alcance (out-of-scope)

- Certificaciones formales o auditorías externas.
- Rediseño mayor de arquitectura de seguridad.
- Hardening avanzado post-MVP que no bloquee salida inicial.

## Criterios de aceptación

- [x] **CA-1**: Las operaciones críticas del MVP aplican controles de autorización y validación coherentes con la KB.
- [x] **CA-2**: Logs, errores y trazas del MVP evitan exposición de PII sensible en claro.
- [x] **CA-3**: El manejo de autenticación social y contexto de Workspace queda revisado antes de release.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| LoginPage | RN-017, RN-018, RN-036 | cubierto | Intercambio con Google sin volcar la respuesta ajena a logs; `id_token` nunca en traza |
| Ajustes/App | docs/07-seguridad/modelo-seguridad.md | cubierto | CSP en el documento del SPA, verificada sobre el build de producción |
| Bordes de la API | docs/02-arquitectura/contratos-api.md | cubierto | 14 tests de integración del borde de transporte (`P-027`, `P-043`) |

## Notas y decisiones

- Esta historia endurece el MVP ya construido; no debe introducir cambios funcionales de producto salvo los bloqueantes.
- **Puntos de `MVP-999` asignados aquí** (3ª pasada de `MVP-299`, 2026-07-28). Los dos viven en el
  **mismo borde de transporte** y deben resolverse en una sola pasada, con un helper común de lectura
  de cuerpo:
  - **`P-027`** — los `PATCH` de campos parciales de los maestros responden **500** ante un cuerpo
    JSON con bytes no UTF-8: el patrón `[FromBody] Dictionary<string, JsonElement>` acepta los bytes y
    revienta después en `GetString()`. Debe devolver `400`. Afecta a `PlotsController`,
    `WorkersController` y `TasksController`.
  - **`P-043`** — `InvalidModelStateResponseFactory` colapsa toda la validación de alta a
    `VALIDATION_REQUIRED`, así que un cliente no puede distinguir «falta» de «en blanco» ni de
    «demasiado largo» en el `POST` de los cuatro maestros, mientras el `PATCH` sí emite el código
    específico. Además filtra el mensaje por defecto de ASP.NET **en inglés** («The request field is
    required.»), que la UI muestra tal cual al usuario. La corrección **documental** ya se aplicó
    (`MVP-208` CA-9 y `MVP-299` CA-5): el contrato describe hoy lo que la API hace; lo que queda es
    unificar el comportamiento.
  - Estaban registrados con destino «`MVP-999` o `MVP-501`». Se asignan aquí porque «revisión de
    validación de entrada en bordes API» es literalmente el alcance de esta historia; `MVP-501` aporta
    los tests que lo verifican, no el arreglo.

## Resultado de la entrega (2026-07-31)

Diseño técnico completo en [tech-design.md](./tech-design.md).

Historia de endurecimiento: **no añade capacidades de producto**. Cierra los dos puntos asignados y
ejecuta la auditoría que piden los tres CA, corrigiendo lo que encuentra.

### `P-027` — Un cuerpo mal codificado ya no es un `500`

Un `PATCH` con bytes que no son UTF-8 válido respondía **500**: `JsonElement.GetString()` lanza
después del binding y nadie la capturaba. Ahora es **400**, con un lector común
(`PartialUpdateBody` + la primitiva `JsonText`) y un filtro que traduce en un único sitio.

**Se corrigen los ocho controladores, no los tres que el punto nombraba.** `P-027` citaba `Plots`,
`Workers` y `Tasks`, pero el mismo patrón estaba también en `Seasons`, `Activities`, `Harvests`,
`Purchases` y `Consumptions`, que llegaron después de registrarse. Cerrarlo solo en tres habría sido
cerrarlo de nombre.

### `P-043` — El alta y la edición responden lo mismo

`VALIDATION_REQUIRED` lo absorbía todo en el `POST`, así que un cliente no podía distinguir «falta»
de «demasiado largo» —el `PATCH` sí devolvía el código de dominio—. Y los fallos del enlace de modelo
salían con el texto por defecto de ASP.NET **en inglés**, que la UI mostraba tal cual.

Ahora cada anotación declara su código y las dos vías responden igual. Se añade
`VALIDATION_FORMAT_INVALID` para «el valor llegó, pero no se puede interpretar», que es un arreglo
distinto de «falta». El contrato (`contratos-api.md`) ya describe el comportamiento nuevo.

### Hallazgos de la auditoría

La mayor parte de los controles ya estaban puestos en épicas anteriores —emails enmascarados, tokens
solo como hash, sin PII en query params ni en consola del cliente, `X-Request-Id` acotado, cookie de
refresco bien configurada—. Lo que **sí** hubo que corregir:

- **`H-1` (CA-2) — El intercambio con Google volcaba a log la respuesta ajena entera.** Es una carga
  de un tercero que acompaña a una petición con el `code` y el `client_secret`, y
  `privacidad-datos.md` prohíbe expresamente registrar credenciales del proveedor. Ahora se conserva
  solo el campo `error` de OAuth 2.0, que es vocabulario cerrado. Lo mismo con `ex.Message` de
  `InvalidJwtException`, que puede arrastrar fragmentos del `id_token`: se registra solo el tipo.
- **`H-2` (CA-1) — La CSP estaba donde no protege.** Existía en las respuestas de la API, que son
  JSON y no ejecutan scripts; **el documento del SPA no tenía ninguna**, que es donde mitiga XSS y
  donde importa, porque el token de acceso vive en `sessionStorage`. Se inyecta en el build de
  producción y se ha verificado sobre el build servido de verdad: fuentes e iconos cargados, todas
  las llamadas en `200`, cero violaciones.
- **`H-3` (CA-1) — El sucesor del traspaso de Workspace no era determinista.** RN-038/CA-5 lo
  promete, pero dos copropietarios pueden compartir `joined_at` —la resolución del reloj es de
  milisegundos— y entonces quien heredaba lo decidía el orden físico de las filas. El orden en
  memoria lo aparentaba estable; al pasar a SQL en `MVP-501` quedó a la vista. Se cierra con un
  desempate por identificador **en los dos sitios**: el repositorio que traspasa y el handler que
  anuncia al sucesor. Sin el segundo, la pantalla podía nombrar a una persona y el traspaso acabar en
  otra.

### Verificación

- Backend: **631 tests** en verde (610 antes), con 14 de integración nuevos sobre el borde de
  transporte y una regresión de determinismo.
- Frontend: 72 en verde, `npm run build` y `npm run lint` sin errores nuevos.
- CSP verificada manualmente sobre el build de producción con la API y PostgreSQL levantados.
