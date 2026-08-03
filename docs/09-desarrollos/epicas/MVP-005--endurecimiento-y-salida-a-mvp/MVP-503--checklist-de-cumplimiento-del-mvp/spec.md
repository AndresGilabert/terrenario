---
id: "MVP-503"
tipo: feature
titulo: "Checklist de cumplimiento del MVP"
estado: completado
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-505", "MVP-502"]
bloquea: ["MVP-504"]
relacionado_con: []
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["cumplimiento", "documentacion"]
  modulo_path: "03-modulos/"
  componentes: ["rgpd-lopdgdd", "dod", "release-readiness"]
  etiquetas: ["mvp", "compliance", "release"]
  nivel_riesgo: medio
creado_en: "2026-07-20"
actualizado_en: "2026-08-03"
---

# MVP-503 — Checklist de cumplimiento del MVP

## Contexto

La KB exige evidencia documental mínima de RGPD/LOPDGDD y, cuando aplique, de LSSI/ePrivacy o EIPD. Sin este bloque, el MVP podría desplegarse con decisiones técnicas correctas pero con gobernanza incompleta.

## Objetivo

Dejar verificada y documentada la evidencia mínima de cumplimiento necesaria para considerar el MVP listo para salida controlada.

## Requisitos de usuario

### HU-1 — Verificar cumplimiento antes de producción

**Como** responsable del producto,
**quiero** un checklist claro de cumplimiento del MVP,
**para** no pasar a producción sin revisar las obligaciones mínimas aplicables.

## Alcance (in-scope)

- Revisión documental de base jurídica y minimización para los flujos del MVP.
- Verificación de retención y tratamiento de PII en los bloques relevantes.
- Registro de si aplica o no LSSI/ePrivacy y si aplica o no EIPD.
- Evidencia suficiente para cumplir DoR/DoD y release gate del MVP.

## Fuera de alcance (out-of-scope)

- Formalización legal externa completa.
- Nuevas políticas fuera del alcance ya definido en la KB.
- Análisis regulatorio de funcionalidades post-MVP.

## Criterios de aceptación

- [x] **CA-1**: Existe evidencia documental mínima de cumplimiento RGPD/LOPDGDD para los flujos del MVP.
- [x] **CA-2**: Queda documentado si LSSI/ePrivacy y EIPD aplican o no al alcance MVP.
- [x] **CA-3**: La documentación resultante permite sostener la salida controlada definida en la épica,
  **con las condiciones que se listan**: la salida es sostenible una vez `MVP-504` cierre los contratos
  de encargo y los dos bloqueos de negocio. Ver Resultado.

## Maquetas y referencias visuales

- Prototipo base ejecutable: [prototype/terrenario-mvp/README.md](../../../../../prototype/terrenario-mvp/README.md)
- Referencia UI: [prototype/terrenario-mvp/src/components/AjustesView.tsx](../../../../../prototype/terrenario-mvp/src/components/AjustesView.tsx)
- Referencia UI: [prototype/terrenario-mvp/src/components/LoginPage.tsx](../../../../../prototype/terrenario-mvp/src/components/LoginPage.tsx)

> El prototipo se usa solo como referencia visual y de flujo. La fuente de verdad funcional y de requisitos es la KB.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado (cubierto/parcial/falta) | Evidencia de prueba |
|---|---|---|---|
| AjustesView | RN-017 | cubierto | Panel de privacidad con el inventario y baja de cuenta (MVP-505), verificados |
| Flujo autenticacion | docs/07-seguridad/privacidad-datos.md | cubierto | Registro de tratamientos T1-T8 con base jurídica, contrastado con el esquema real |
| Páginas legales | docs/07-seguridad/checklist-cumplimiento-mvp.md | parcial | Existen y son alcanzables; **no publicables** hasta rellenar los marcadores (`R-02`) |

## Notas y decisiones

- Esta historia es de gobernanza mínima, no de burocracia adicional fuera del MVP.
- **Depende de `MVP-505`** (3ª pasada de `MVP-299`, 2026-07-28). Esta historia **verifica**
  cumplimiento; no lo construye —«nuevas políticas» está en su fuera de alcance—. Las tres piezas que
  necesita verificar (páginas legales y consentimiento, baja de cuenta y política de retención) no
  existían en ninguna historia del roadmap: se crean en `MVP-505`, que debe entregarse antes. Sin ese
  orden, el checklist saldría en rojo sin nadie a quien devolvérselo.

## Resultado de la entrega (2026-08-03)

Entregable: [`docs/07-seguridad/checklist-cumplimiento-mvp.md`](../../../../07-seguridad/checklist-cumplimiento-mvp.md).

**Esta revisión no ha sido una relectura de la KB.** Cada afirmación se contrastó contra el sistema
real —esquema de base de datos, código del cliente y comportamiento de la API— y eso destapó **tres
discrepancias entre lo documentado y lo que el sistema hace**, todas corregidas aquí. Una revisión que
solo hubiera confirmado lo ya escrito no habría servido de nada.

### Lo que se entrega

- **Registro de actividades de tratamiento** (art. 30) con ocho tratamientos, su base jurídica y su
  plazo, obtenido del esquema real y no de la documentación.
- **Revisión de los siete principios** del art. 5, con el estado de cada uno.
- **LSSI/ePrivacy: aplican**, y se cumplen **sin banner**, con la exención justificada tecnología por
  tecnología.
- **EIPD: no procede**, evaluada contra los nueve criterios del EDPB (cumple uno; se exige a partir de dos).
- **Derechos**: la supresión se ejerce desde la aplicación; el resto es procedimiento manual.
- **Veredicto por CA** y los bloqueos que hereda `MVP-504`.

### Discrepancias encontradas y corregidas

- **`R-03`** — El **inventario de tecnologías de `MVP-505` no coincidía con el código**: declaraba una
  clave que no existe (`terrenario:privacy_ack`) y omitía **cinco** que sí (`pkce_code_verifier`,
  `oauth_state`, `terrenario_post_login_redirect`, `terrenario_login_flow`,
  `terrenario_login_started`). Un inventario que no coincide con el sistema no sirve de evidencia.
- **`R-04`** — **`plots.owner_name` no estaba declarado como dato personal**, ni en la clasificación de
  la KB ni en la Política de Privacidad, y está en uso. Es el nombre del propietario de un terreno
  cedido: **un tercero sin cuenta**. Se añade, junto con un bloque nuevo sobre los datos de terceros
  que introduce el usuario —quién responde de ellos, por qué esas personas no pueden ejercer sus
  derechos desde el producto y por qué la baja de cuenta no los borra—.
- **`R-05`** — **«No hay analítica» era inexacto**: existe medición propia del embudo de login (RN-020,
  `MVP-105`). Analizada, **no requiere consentimiento** —primera parte, sin PII, sin seguimiento entre
  sitios, vida de sesión—, pero la afirmación absoluta no era defendible ante una inspección. Ahora
  está declarada y motivada.

### Veredicto

**El MVP no tiene deuda de cumplimiento imputable al desarrollo.** Lo que falta para publicar son
**decisiones de negocio e infraestructura**, que hereda `MVP-504`:

1. `R-01` — La **rutina de expurgo no está programada**: hoy nada se purga a los 24 meses.
2. `R-02` — Los **datos del responsable del tratamiento son marcadores**: sin ellos no hay documento
   publicable ni dirección donde ejercer derechos.
3. Los **contratos de encargo** con Google, el proveedor de email y el de alojamiento.
4. `R-06` — Sin **exportación de datos** (portabilidad, art. 20), que estaba fuera de alcance de
   `MVP-505`. Derivado a `MVP-999` para que el gate decida si bloquea.
