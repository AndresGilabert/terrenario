---
id: "MVP-503"
tipo: feature
titulo: "Checklist de cumplimiento del MVP"
estado: borrador
prioridad: alta
sprint: ""
hito: "Hito E — Salida controlada a MVP"
esfuerzo_estimado: "2d"
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
depende_de: ["MVP-502"]
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
actualizado_en: "2026-07-20"
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

- [ ] **CA-1**: Existe evidencia documental mínima de cumplimiento RGPD/LOPDGDD para los flujos del MVP.
- [ ] **CA-2**: Queda documentado si LSSI/ePrivacy y EIPD aplican o no al alcance MVP.
- [ ] **CA-3**: La documentación resultante permite sostener la salida controlada definida en la épica.

## Maquetas y referencias visuales

- Referencia funcional: `definition-of-ready.md`, `definition-of-done.md` y `privacidad-datos.md`.

## Notas y decisiones

- Esta historia es de gobernanza mínima, no de burocracia adicional fuera del MVP.
