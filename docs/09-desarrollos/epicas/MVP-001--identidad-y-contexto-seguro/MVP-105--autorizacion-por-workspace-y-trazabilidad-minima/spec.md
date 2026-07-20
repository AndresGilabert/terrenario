---
id: "MVP-105"
tipo: feature
titulo: "Autorización por Workspace y trazabilidad mínima de login"
estado: borrador
prioridad: critica
sprint: ""
hito: "Hito A — Base segura y multiusuario"
esfuerzo_estimado: "3d"
tickets: []
epica: "MVP-001--identidad-y-contexto-seguro"
depende_de: ["MVP-101", "MVP-104"]
bloquea: ["MVP-003", "MVP-004", "MVP-006"]
relacionado_con: ["MVP-006"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["seguridad", "autorizacion", "observabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["workspace-scope", "authorization", "login-tracing"]
  etiquetas: ["mvp", "security", "telemetry"]
  nivel_riesgo: alto
creado_en: "2026-07-20"
actualizado_en: "2026-07-20"
---

# MVP-105 — Autorización por Workspace y trazabilidad mínima de login

## Contexto

La autenticación por sí sola no basta: el MVP debe rechazar operaciones fuera del Workspace activo y, además, medir si el flujo de login termina en éxito o abandono. La KB exige ambas cosas como parte del perímetro mínimo de seguridad y aprendizaje del producto.

## Objetivo

Garantizar que toda operación del MVP queda acotada al Workspace activo y que el flujo de login genera la traza mínima necesaria para medir conversión y abandono sin exponer PII sensible.

## Requisitos de usuario

### HU-1 — Operar solo dentro de mi Workspace

**Como** miembro de un Workspace,
**quiero** que la aplicación me permita acceder solo a los datos de mis Workspaces,
**para** evitar cruces o fugas de información entre explotaciones.

### HU-2 — Medir el embudo de login

**Como** responsable del producto,
**quiero** conocer si el login termina en éxito o abandono,
**para** detectar barreras reales de acceso desde el MVP.

## Alcance (in-scope)

- Enforcement de ámbito de Workspace en operaciones protegidas del MVP.
- Respuestas de acceso denegado cuando el recurso no pertenece al Workspace activo.
- Señal mínima de login iniciado, login completado o login abandonado.
- Trazabilidad segura sin PII sensible en claro.
- Preparación base para ampliar esta telemetría en la épica de observabilidad.

## Fuera de alcance (out-of-scope)

- Matriz detallada de permisos por rol o recurso.
- Cuadros analíticos avanzados de producto.
- Alertado operativo completo más allá de la señal mínima exigida.

## Criterios de aceptación

- [ ] **CA-1**: Las operaciones protegidas del MVP rechazan accesos fuera del Workspace activo.
- [ ] **CA-2**: El sistema genera trazas mínimas que permiten diferenciar login iniciado, completado y abandonado.
- [ ] **CA-3**: La trazabilidad cumple las restricciones de privacidad y no expone PII sensible en logs ni errores.

## Maquetas y referencias visuales

- Referencia funcional: RN-017, RN-020 y alcance de `MVP-006`.

## Notas y decisiones

- Aunque parte de la explotación posterior viva en `MVP-006`, la señal mínima de login se considera base del bloque de identidad.
- Esta historia es la que realmente cierra el perímetro de seguridad del Hito A.
