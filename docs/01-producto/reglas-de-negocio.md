---
bloque: 01-producto
documento: reglas-de-negocio
actualizado_en: "2026-07-16"
---

# Reglas de Negocio Globales

> Este documento recoge las reglas de negocio que aplican de forma transversal al producto.
> Las reglas específicas de un módulo están en `../03-modulos/{modulo}/modelo-dominio.md`.
>
> **IMPORTANTE para agentes de IA**: Antes de generar código que implique lógica de negocio,
> verifica que no contradiga ninguna regla de este documento.

---

## Convenciones

Cada regla sigue el formato:

- **ID**: `RN-XXX` (identificador único, no reusar IDs aunque se elimine la regla)
- **Estado**: `activa` | `obsoleta` | `en-revisión`
- **Fuente**: de dónde viene esta regla (legal, producto, acuerdo contractual, etc.)

---

## Reglas activas

### RN-001 — Descripción de la regla 001

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: modulo-principal, registro-operativo

TODO (Ejempo, remplazar por regla real) Todo registro operativo debe estar asociado a una entidad principal del dominio.

---

### RN-002 — Descripción de la regla 002

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades

TODO (Ejempo, remplazar por regla real) No se permite registrar actividad sin responsable y tiempo dedicado.

---

### RN-00x — Descripción de la regla 00x

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades, compras-consumo

TODO (Ejempo, remplazar por regla real) El sistema calcula costes en base a compras y mano de obra. No se introduce importe final manual en el registro operativo.

---

## Reglas obsoletas

| ID | Nombre | Motivo de obsolescencia | Fecha |
|----|--------|------------------------|-------|
| RN-LEGACY-001 | Dashboard solo v2 | TODO, ejemplo, remplazar con reglas obsoletas del proyecto | 2026-06-30 |
