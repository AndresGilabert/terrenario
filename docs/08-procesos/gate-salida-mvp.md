---
bloque: 08-procesos
documento: gate-salida-mvp
actualizado_en: "2026-08-03"
---

# Gate de salida del MVP (MVP-504)

Punto de decisión único para responder a una sola pregunta: **¿puede el MVP exponerse a usuarios
reales?**

Existe porque, sin él, «el MVP está terminado» significaría «la funcionalidad está construida», que
es una respuesta distinta. Este documento separa lo que **el desarrollo** puede cerrar de lo que
**solo puede cerrar el negocio o la infraestructura**, para que ninguna de las dos cosas se dé por
hecha.

---

## 1. Veredicto

| Salida | Estado |
|---|---|
| **Despliegue a `staging`** | ✅ **AUTORIZADO** |
| **Despliegue a `producción` con usuarios reales** | ⛔ **BLOQUEADO** — 4 bloqueos abiertos, ninguno de desarrollo |

La distinción es el resultado principal de esta historia. **La construcción del MVP no tiene deuda
que impida desplegarlo**: los gates de calidad, seguridad y cumplimiento imputables al desarrollo
están cerrados y son verificables automáticamente. Lo que impide exponerlo a personas reales son
decisiones de negocio e infraestructura que no se pueden resolver escribiendo código.

---

## 2. Gate automático

Desde esta historia, el CI **ejecuta** el gate en cada PR a `develop` y a `main`
(`.github/workflows/ci.yml`). Antes solo validaba la KB: ni compilaba ni ejecutaba un test. Un gate
que no corre automáticamente no es un gate.

| Comprobación | Job | Bloquea |
|---|---|---|
| Compilación del backend | `backend` | Sí |
| Tests del backend (unitarios, repositorio, integración y smoke E2E) | `backend` | Sí |
| Lint del cliente | `frontend` | Sí |
| Build del cliente, incluida comprobación de tipos de los tests | `frontend` | Sí |
| Tests del cliente | `frontend` | Sí |
| `npm audit` a partir de severidad alta | `seguridad` | Sí |
| Estructura, índices y markdownlint de la KB | `validar-kb` | Sí |

Estado en el momento de escribir esto: **666 tests de backend y 87 de cliente en verde**, build y
lint limpios, **0 vulnerabilidades**.

### Lo que este gate **no** cubre

- **E2E de navegador** (`MVP-999`, `P-064`). El smoke E2E es **de servidor**: recorre la API de punta
  a punta pero no ejercita el cliente en un navegador. Al leer «smoke E2E en verde» hay que saber que
  eso es lo que significa aquí.
- **Rendimiento y carga**, fuera del alcance declarado del MVP.

---

## 3. Bloqueos para salir a producción

Ninguno es de desarrollo. Los cuatro necesitan una decisión o una acción externa.

### B-1 · Los datos del responsable del tratamiento son marcadores

**Qué falta**: razón social, NIF, domicilio, correo de contacto de privacidad, si hay DPO designado,
el fuero, si hay transferencias internacionales, y los nombres de los proveedores de correo y
alojamiento.

**Por qué bloquea**: sin ellos, la Política de Privacidad y los Términos **no son publicables** y no
existe una dirección real donde ejercer derechos. Exponer el servicio así es un incumplimiento
conocido, no un descuido.

**Quién lo cierra**: negocio. Las páginas ya existen y avisan de que están pendientes.

### B-2 · Contratos de encargo del tratamiento (RGPD art. 28)

| Proveedor | Estado |
|---|---|
| Google (OIDC) | Verificar que las condiciones de tratamiento aplicables están aceptadas |
| Proveedor de email | **Sin contratar** (ADR-0010) |
| Proveedor de alojamiento | **Sin decidir** |

**Por qué bloquea**: tratar datos personales a través de un encargado sin contrato de encargo es un
incumplimiento directo. Hay que verificar además dónde se alojan los datos y si hay transferencia
internacional con garantías.

**Quién lo cierra**: negocio e infraestructura.

### B-3 · La rutina de expurgo no está programada

**Qué hay**: `RN-041` fija 24 meses, `AccountRetentionPolicy` calcula la fecha de purga y la baja de
cuenta la devuelve.

**Qué falta**: que algo la ejecute. Hoy **nada se purga**.

**Por qué bloquea**: la política declarada y la realidad no coinciden, y el desfase crece con el
tiempo. Con el MVP recién desplegado el primer vencimiento está a 24 meses, así que **no es urgente,
pero sí es una promesa incumplida desde el día uno**.

**Quién lo cierra**: infraestructura, decidiendo dónde se programa (tarea del alojamiento, job del
contenedor o servicio en segundo plano de la propia API).

### B-4 · Sin exportación de datos (portabilidad, art. 20)

**Qué falta**: el derecho de portabilidad solo se puede atender por vía manual, contra un correo de
contacto que además es un marcador (B-1).

**Decisión pendiente**: si se acepta atenderlo manualmente mientras el MVP está en validación —lo que
es defendible con pocos usuarios y un plazo de un mes— o si se considera bloqueante. Registrado como
`P-070`.

**Quién lo cierra**: negocio.

---

## 4. Riesgos aceptados, no bloqueantes

Se listan para que la decisión de salir sea informada, no para frenarla.

| # | Riesgo | Por qué se acepta |
|---|---|---|
| `P-064` | Sin E2E de navegador | El smoke de servidor cubre la lógica de punta a punta; una regresión solo visible en el navegador la caza la QA manual. Coste de montarlo alto por el login con Google |
| `P-069` | La suite de backend exige Docker y una política de Application Control permisiva | El entorno de referencia es el CI sobre Linux, donde no aplica. En desarrollo local puede volver a bloquearse |
| `P-011`, `P-029` | Avisos in-app que solo se refrescan al montar la sesión | No hay pérdida de dato: el correo sigue llegando |
| `P-032` | Sin edición de perfil propia | La identidad la gobierna Google (RN-036) |

---

## 5. Criterios de promoción a producción

Cuando B-1 a B-4 estén resueltos:

1. **Gate automático en verde** en `main`.
2. **Migraciones aplicadas** y verificadas en staging.
3. **Variables y secretos** del entorno configurados: claves JWT (RS256), cadena de conexión,
   credenciales de Google OIDC, cuenta de envío de correo, `Cors:AllowedOrigins` y
   `VITE_API_BASE_URL` del cliente.
4. **CSP servida como cabecera** por quien sirva el estático, no solo como `meta` (`P-067`).
5. **HTTPS obligatorio**: la cookie de refresco solo es `Secure` sobre HTTPS.
6. **Smoke manual** del núcleo: acceso, alta de Workspace, captura en el diario y baja de cuenta.
7. **Notas de release** en `docs/10-releases/` y tag de versión.

---

## 6. Cómo se comprueba este gate

```bash
dotnet test src/backend/Terrenario.sln
```

```bash
npm ci --prefix src/frontend/terrenario-web && npm run lint --prefix src/frontend/terrenario-web && npm run build --prefix src/frontend/terrenario-web && npm test --prefix src/frontend/terrenario-web
```

```bash
PYTHONUTF8=1 python docs/00-meta/scripts/validar_pipeline_kb.py --solo-cambios --base-ref origin/develop --check-indices-clean
```

Requisitos del entorno: **Docker** para la suite de backend y **Node 22** para el cliente.

---

## Trazabilidad KB

1. Evidencia de cumplimiento: [`../07-seguridad/checklist-cumplimiento-mvp.md`](../07-seguridad/checklist-cumplimiento-mvp.md)
2. Estrategia y alcance de los tests: [`../04-ingenieria/estrategia-testing.md`](../04-ingenieria/estrategia-testing.md)
3. Proceso de release y rollback: [`proceso-release.md`](./proceso-release.md)
4. Pipeline y entornos: [`../05-infraestructura/ci-cd.md`](../05-infraestructura/ci-cd.md)
