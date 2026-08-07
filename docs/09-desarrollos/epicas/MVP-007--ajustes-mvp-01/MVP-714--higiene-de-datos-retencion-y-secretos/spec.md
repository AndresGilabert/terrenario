---
id: "MVP-714"
tipo: feature
titulo: "Higiene de datos: retencion de sesiones y secretos en el repositorio"
estado: borrador
prioridad: baja
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["privacidad", "cumplimiento", "seguridad"]
  modulo_path: "03-modulos/"
  componentes: ["retencion", "refresh-tokens", "repositorio"]
  etiquetas: ["mvp", "ajustes", "cumplimiento"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-714 — Higiene de datos: retencion de sesiones y secretos en el repositorio

> **Origen**: `P-071` y `P-076` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

**`P-071`** — Los refresh tokens revocados o caducados no tienen plazo de expurgo. La tabla de
retencion de `privacidad-datos.md` dice «hasta caducidad o revocacion», pero una vez caducado o
revocado el hash se queda en `refresh_tokens` indefinidamente: solo se limpia si se purga la cuenta
entera. No se incluyo en la rutina de `RN-041` **a proposito**, porque esa regla enumera cinco
categorias y esta no es una: anadirla es una decision de producto.

**Decision del PO (2026-08-06)**: se anade con **plazo corto** —dias o semanas tras la caducidad—, no
los 24 meses del resto, que para un dato de sesion es conservador de mas.

**`P-076`** — Una direccion de correo personal del titular esta versionada en
`prototype/terrenario-mvp/src/data/initialData.ts`, en un repositorio **publico** y en el historial de
git. **Decision del PO**: sustituirla por una direccion de ejemplo y aceptar que el historial la
conserva. Reescribir el historial invalidaria clones, tags y referencias existentes —incluidos los tags
de release desde los que se despliega— y no recuperaria lo ya copiado.

## Objetivo

Cerrar los dos restos de higiene que quedaron abiertos tras el gate de salida, sin abrir ninguno nuevo.

## Requisitos de usuario

### HU-1 — Que todo lo que se conserva tenga plazo

**Como** responsable de cumplimiento,
**quiero** que ninguna categoria de dato quede sin plazo de expurgo,
**para** que `RN-041` signifique lo que dice.

## Alcance (in-scope)

- Nueva categoria en `RN-041` para tokens de refresco revocados o caducados, con plazo corto y motivo
  escrito.
- Linea correspondiente en `RetentionPurgeService`.
- Actualizacion de la tabla de retencion de `docs/07-seguridad/privacidad-datos.md`.
- Sustitucion de la direccion personal en `initialData.ts` por una de ejemplo, con nota de que el
  historial la conserva.

## Fuera de alcance (out-of-scope)

- Reescritura del historial de git: descartada por el PO.
- Revisar el resto de plazos de `RN-041`, ya cerrados en `MVP-505`.
- Barrido general de secretos en el repositorio.

## Criterios de aceptación

- [ ] **CA-1**: `RN-041` incluye la categoria de tokens de refresco con su plazo y su motivo.
- [ ] **CA-2**: La rutina de expurgo la aplica, verificado con datos sembrados y no solo por lectura del
  codigo.
- [ ] **CA-3**: `privacidad-datos.md` refleja el plazo nuevo.
- [ ] **CA-4**: `initialData.ts` no contiene ninguna direccion personal real, y queda anotado que el
  historial de git si la conserva.
- [ ] **CA-5**: Ninguna otra direccion personal real aparece en la copia de trabajo del repositorio.

## Maquetas y referencias visuales

No aplica: cumplimiento y saneamiento de datos.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| initialData del prototipo | RN-017, RN-041 | falta | Correo personal versionado en repositorio publico |

## Notas y decisiones

- La eleccion conservadora aqui es **la sustitucion**, no la reescritura: el dato lleva meses expuesto,
  reescribir no lo recupera y si rompe las referencias desde las que se despliega.
