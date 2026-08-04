---
bloque: 08-procesos
documento: gate-salida-mvp
actualizado_en: "2026-08-04"
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
| **Despliegue a `producción` con usuarios reales** | ⛔ **BLOQUEADO** — 3 bloqueos abiertos, ninguno de desarrollo (`B-1` cerrado el 2026-08-04) |

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

Ninguno es de desarrollo. Los que quedan abiertos necesitan una decisión o una acción externa.

### B-1 · Datos del responsable del tratamiento — ✅ **CERRADO** (2026-08-04)

Aportados por el negocio y publicados. Las páginas legales ya no tienen ni un marcador.

| Dato | Valor |
|---|---|
| Titular | Andrés Gilabert Sánchez |
| NIF | 21.679.361-K |
| Domicilio | Dr. Fleming, 39A, 03830 Muro de Alcoi (Alicante), España |
| Contacto de privacidad | `hola@andresgilabert.dev` |
| Delegado de Protección de Datos | No designado |
| Proveedor de correo | Arsys |
| Proveedor de alojamiento | Microsoft Azure, región **España** |

**Tres decisiones que van con esto:**

- **No se designa DPO.** No es obligatorio: el tratamiento no encaja en ninguno de los tres
  supuestos del art. 37 ni en el listado sectorial del art. 34 LOPDGDD. «No designado» es una
  respuesta completa, no un hueco.
- **No se impone fuero.** A un consumidor no se le puede imponer —sería cláusula abusiva
  (TRLGDCU art. 90.2)— y los usuarios serán mezcla de profesionales y particulares. Los Términos
  dicen que se aplica la legislación española y que la competencia es la que determine la ley.
- **Transferencias internacionales declaradas.** Alojamiento en la región de España y correo con
  proveedor español: sin transferencia. La única salida del EEE es el inicio de sesión con Google,
  amparada en cláusulas contractuales tipo y en la decisión de adecuación UE–EE. UU. Es inevitable
  mientras el acceso sea con Google (`RN-036`), así que se declara en vez de omitirse.

**Dónde vive**: `src/frontend/terrenario-web/src/config/legal-entity.ts`, con override por variable
de entorno `VITE_LEGAL_*`. Versionado a propósito —la LSSI obliga a publicar estos datos, así que no
hay nada que proteger, y un `.env` no llega al despliegue—. Si algún campo queda vacío, las páginas
vuelven a mostrar el aviso de documento pendiente en lugar de publicar un hueco, y un test lo impide
antes de llegar ahí.

**Lo que no cierra**: la **revisión por asesoría legal** del texto. Estaba fuera del alcance
declarado de `MVP-505` y sigue siendo una decisión de negocio; ver §4.

### B-2 · Contratos de encargo del tratamiento (RGPD art. 28) — **reducido** (2026-08-04)

La asesoría del negocio aportó el encuadre y cambia el tamaño del bloqueo.

**Google sale**: quien accede lo hace con **su** cuenta de Google, así que Google trata esos datos
bajo su propia política y no por cuenta del proyecto. Es **responsable independiente**, no encargado,
y no procede contrato del art. 28 con él. Lo que procede es informarlo, y ya se informa en la
Política de Privacidad. Esto corrige la clasificación de `MVP-503`, que lo listaba como encargado.

| Proveedor | Estado |
|---|---|
| Microsoft Azure (alojamiento) | **Servicio sin contratar** |
| Arsys (correo) | **Servicio sin contratar** (ADR-0010) |

**Qué queda, entonces**: con estos dos no hay contrato que negociar ni redactar —el anexo de
tratamiento de datos va incorporado al contratar el servicio—. El bloqueo es **confirmar que está en
vigor**, y eso solo puede hacerse al contratarlos, cuando exista infraestructura. Es el mismo momento
que desbloquea `B-3`.

**Por qué sigue bloqueando**: la Política de Privacidad declara que Azure y Arsys tratan datos como
encargados. Mientras no estén contratados, esa declaración describe una intención, no un hecho.

**Ya no aplica** el aviso que este gate traía sobre el orden de publicación: la sección 4 de la
política se reescribió y ya no afirma que cada proveedor tenga contrato firmado.

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
| — | **Las páginas legales no las ha revisado una asesoría jurídica** | Fuera del alcance declarado de `MVP-505`. El contenido describe el sistema real y se ha contrastado contra él (`MVP-503`), pero eso es verificación técnica, no validación jurídica. Decisión de negocio: revisarlas antes de abrir el servicio o asumirlo durante la validación |

---

## 5. Criterios de promoción a producción

Cuando `B-2` a `B-4` estén resueltos:

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
