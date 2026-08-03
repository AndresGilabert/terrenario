---
id: "MVP-504"
tipo: feature
titulo: "Gate final de release del MVP"
estado: completado
prioridad: critica
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-501", "MVP-502", "MVP-503"]
bloquea: ["MVP-006"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["release", "calidad", "cumplimiento"]
  modulo_path: "03-modulos/"
  componentes: ["release-gate", "staging", "deploy-readiness"]
  etiquetas: ["mvp", "release", "readiness"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-08-03"
---

# MVP-504 — Gate final de release del MVP

## Contexto

Una vez cubiertos tests, seguridad y cumplimiento, hace falta un último punto de decisión que consolide el estado del MVP antes de staging/producción. Sin ese gate, el bloque de endurecimiento queda difuso y sin cierre operativo claro.

## Objetivo

Definir y ejecutar el gate final que permite considerar el MVP listo para salida controlada a staging y posterior promoción a producción.

## Requisitos de usuario

### HU-1 — Saber si el MVP está listo para salir

**Como** responsable del despliegue,
**quiero** un gate final de release,
**para** decidir con claridad si el MVP puede pasar a staging/producción.

## Alcance (in-scope)

- Checklist final de release del MVP.
- Verificación consolidada de tests, seguridad y cumplimiento.
- Preparación de salida a staging y criterio de promoción posterior.
- Cierre de deuda bloqueante abierta al final del núcleo funcional.

## Fuera de alcance (out-of-scope)

- Automatización completa del proceso de release si no existe ya.
- Mejora de funcionalidades no bloqueantes detectadas en la revisión final.
- Operación posterior continua del sistema una vez desplegado.

## Criterios de aceptación

- [x] **CA-1**: Existe un gate final de release explícito y verificable para el MVP.
- [x] **CA-2**: El MVP puede desplegarse a staging con criterios mínimos de calidad, seguridad y cumplimiento ya comprobados.
- [x] **CA-3**: No quedan bloqueos críticos abiertos **imputables al desarrollo**. Los cuatro que
  quedan son de negocio e infraestructura, están identificados y con dueño. Ver Resultado.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/App.tsx](../../../../../prototype/terrenario-mvp/src/App.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| README run local | docs/08-procesos/proceso-release.md | parcial | Arranque local documentado |
| Navegacion MVP | docs/08-procesos/definition-of-done.md | parcial | Base para gate visual, faltan gates formales |

## Notas y decisiones

- Esta historia cierra formalmente el Hito E.

## Resultado de la entrega (2026-08-03)

Entregable: [`docs/08-procesos/gate-salida-mvp.md`](../../../../08-procesos/gate-salida-mvp.md).

### El veredicto

| Salida | Estado |
|---|---|
| **Despliegue a `staging`** | ✅ **AUTORIZADO** |
| **Producción con usuarios reales** | ⛔ **BLOQUEADO** — 4 bloqueos, **ninguno de desarrollo** |

Esa distinción es el resultado principal. **La construcción del MVP no tiene deuda que impida
desplegarlo**; lo que impide exponerlo a personas reales son decisiones que no se resuelven
escribiendo código.

### La pieza funcional: el gate ahora se ejecuta

Hasta esta historia el CI **solo validaba la KB**: ni compilaba el código ni ejecutaba un test. La
estrategia de testing exige unitarios, integración crítica y smoke E2E en verde para permitir
despliegue, pero nada lo comprobaba: era una intención, no un gate.

`.github/workflows/ci.yml` lo hace ejecutable en cada PR a `develop` y `main`: build y suite completa
del backend con PostgreSQL vía Testcontainers, lint, build y tests del cliente, y `npm audit`
bloqueante a partir de severidad alta —esto último por la lección de `MVP-501`, donde un aviso *high*
en `react-router` no lo habría visto nadie hasta el gate final—.

Corre en **Linux**, que es el entorno de referencia: la suite necesita Docker y en Windows depende de
la política de Application Control de la máquina (`P-069`).

### Los cuatro bloqueos, con dueño

| # | Bloqueo | Quién lo cierra |
|---|---|---|
| `B-1` | Los **datos del responsable del tratamiento son marcadores**: sin ellos las páginas legales no son publicables ni hay dirección donde ejercer derechos | Negocio |
| `B-2` | **Contratos de encargo** (art. 28) con Google, proveedor de correo y de alojamiento | Negocio e infraestructura |
| `B-3` | La **rutina de expurgo no está programada**: `RN-041` promete 24 meses y hoy no se purga nada | Infraestructura |
| `B-4` | Sin **exportación de datos** (portabilidad, art. 20): decidir si se acepta atenderlo manualmente durante la validación | Negocio |

### Deuda cerrada en esta historia

- **Incoherencia en la Política de Privacidad**: la sección de cookies seguía afirmando «no usamos
  analítica» después de que `MVP-503` documentara la medición del embudo de login (`R-05`). Un
  documento legal que se contradice a sí mismo es peor que uno incompleto.
- **`proceso-release.md` y `ci-cd.md`** describían un pipeline que no existía. Ahora distinguen lo
  implementado de lo objetivo y enlazan el gate.

### Riesgos aceptados, no bloqueantes

`P-064` (sin E2E de navegador), `P-069` (la suite exige Docker y política permisiva), `P-011`/`P-029`
(avisos in-app sin refresco) y `P-032` (sin edición de perfil). Listados en el gate para que la
decisión de salir sea informada.
