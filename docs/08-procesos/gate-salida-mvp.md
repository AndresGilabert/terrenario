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
| **Despliegue a `producción` con usuarios reales** | ✅ **AUTORIZADO con condiciones** — sin bloqueos abiertos; queda cumplir los criterios de promoción de §5 |

**Los cuatro bloqueos que abrió este gate quedaron cerrados el 2026-08-04.** Este documento conserva
cada uno con lo que costó cerrarlo, porque el rastro de por qué se decidió algo vale más que la marca
de que se decidió.

La distinción entre construcción y salida fue el resultado principal de la historia: **el MVP nunca
tuvo deuda de construcción que impidiera desplegarlo**, y lo que lo retenía eran decisiones de negocio
e infraestructura que no se resuelven escribiendo código. Tres de los cuatro bloqueos se cerraron sin
tocar el producto; el cuarto, la rutina de expurgo, era el único que necesitaba código.

**«Con condiciones» no es un matiz de cortesía**: §5 lista lo que hay que tener hecho antes de
promocionar —secretos configurados, migraciones verificadas en staging, CSP como cabecera, HTTPS,
smoke manual—. Nada de eso es un bloqueo del gate, pero desplegar sin ello sí sería un error.

Queda además un riesgo declarado que conviene decidir antes de abrir a usuarios reales, no después:
**las páginas legales no las ha revisado una asesoría jurídica** (§4).

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

## 3. Bloqueos para salir a producción — todos cerrados

Se conservan con el detalle de cómo se cerró cada uno. Ninguno era de desarrollo salvo `B-3`.

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

### B-2 · Contratos de encargo del tratamiento (RGPD art. 28) — ✅ **CERRADO** (2026-08-04)

Se cerró en dos movimientos.

**Google salió del alcance.** Quien accede lo hace con **su** cuenta de Google, así que Google trata
esos datos bajo su propia política y no por cuenta del proyecto: es **responsable independiente**, no
encargado, y no procede contrato del art. 28 con él. Lo que procede es informarlo, y se informa en la
Política de Privacidad. Esto corrige la clasificación de `MVP-503`, que lo listaba como encargado.

**Azure y Arsys están contratados y su anexo de tratamiento de datos está en vigor** (confirmado por
el negocio). Con estos proveedores no hay contrato que negociar: el anexo va incorporado al contratar
el servicio.

| Encargado | Datos | Estado |
|---|---|---|
| Microsoft Azure (alojamiento, región España) | Todo lo almacenado | ✅ En vigor |
| Arsys (correo) | Dirección de la persona invitada y nombre de quien invita | ✅ En vigor |

**Quién lo cerró**: negocio.

### B-3 · Rutina de expurgo — ✅ **CERRADA** (2026-08-04)

Era el único bloqueo que necesitaba código, y el que peor pinta tenía: `RN-041` prometía 24 meses,
`AccountRetentionPolicy` calculaba la fecha de purga y la baja de cuenta la devolvía al usuario…
pero **no la ejecutaba nadie**. Una política declarada que no corre es peor que no tenerla, porque
documenta un compromiso que se incumple desde el primer día.

**Qué se entrega**: `RetentionPurgeService` aplica las cinco categorías de `RN-041` —invitaciones
terminales o caducadas, solicitudes de reactivación resueltas o caducadas, registros operativos
eliminados lógicamente (RN-037), Workspaces dados de baja (RN-039) y cuentas anonimizadas—, y
`RetentionPurgeWorker` la ejecuta a diario.

**Dónde corre y por qué**: dentro de la propia API, como servicio en segundo plano. Las otras dos
opciones —tarea programada del alojamiento o job de contenedor— exigían infraestructura que no
existía cuando se escribió esto, y habrían dejado el expurgo esperando a que la hubiera. El precio es
que solo corre con la aplicación viva; con un plazo de 24 meses, perder algún día no tiene
consecuencia.

**Qué se cuidó**:

- **Orden de hijo a padre.** Las FK hacia `users` son `Restrict` a propósito, para que nada borre por
  accidente el rastro de quién hizo qué. La cuenta va la última, y **puede quedarse** si todavía la
  referencia algo vivo: se cuenta en el informe en vez de reventar la pasada. Que se quede no es una
  fuga —la fila dejó de identificar a nadie en el momento de la baja—, es limpieza pendiente.
- **Una sola transacción** con *advisory lock* de PostgreSQL: si la API escala, dos réplicas no
  purgan a la vez. No espera, y al ser de ámbito de transacción se libera solo.
- **Idempotente**, y probado como tal: corre a diario sin supervisión, así que la segunda pasada
  tiene que ser inocua.
- **Un fallo no tumba la aplicación**: se registra y se reintenta en la siguiente pasada.

**Lo que este expurgo no borra**: datos personales. Esos ya desaparecen en el acto al darse de baja
(`MVP-505`). Esto es el principio de limitación del plazo de conservación, no el derecho de supresión.

### B-4 · Portabilidad (art. 20) — ✅ **CERRADO** (2026-08-04)

**Decisión**: se acepta atenderla **por vía manual** mientras el MVP esté en validación. Con pocos
usuarios y un plazo legal de un mes, consultar la base y entregar el resultado es conforme: no es un
incumplimiento, es un procedimiento.

Para que sea un procedimiento y no una promesa, queda escrito en
[`../07-seguridad/privacidad-datos.md`](../07-seguridad/privacidad-datos.md): qué se entrega, en qué
formato —JSON o CSV, que el art. 20 exige legible por máquina— y en qué plazo.

**Lo que se aclaró al decidirlo**, y es lo que hacía que el bloqueo pareciera más grande de lo que
era: el art. 20 es **más estrecho** que «exportar todo». Cubre los datos personales que la persona
aportó, no los **derivados** —los agregados del dashboard quedan fuera— y **no puede perjudicar
derechos de terceros**. En este producto eso choca con dos realidades: los nombres de la cuadrilla y
de los propietarios de terrenos cedidos son de otras personas, y un Workspace compartido contiene lo
que registraron otros.

Por eso automatizarlo **no es programar un botón**: exige decidir antes cuál es la unidad de
exportación —la persona o la explotación— y qué se hace con los datos de terceros que van dentro. Esa
decisión no la necesita el cumplimiento, la necesita el producto.

`P-070` queda replanteado en consecuencia: la obligación legal se cubre a mano, y lo que sigue
pendiente es la **función de producto** —«llévate los datos de tu explotación»—, más amplia y más
valiosa que lo que exige la norma.

**Quién lo cerró**: negocio.

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

Sin bloqueos abiertos, queda:

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
