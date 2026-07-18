---
bloque: 01-producto
documento: reglas-de-negocio
actualizado_en: "2026-07-17"
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

### RN-001 — Unidad base por registro operativo

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: terrenos, actividades, produccion

Todo registro operativo debe estar asociado a un terreno. No se admiten actividades o cosechas sin terreno.

---

### RN-002 — Responsable y tiempo obligatorios en actividad

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades

No se permite registrar actividad sin responsable y tiempo dedicado.

---

### RN-003 — Coste manual obligatorio en MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: actividades, compras-consumo

En MVP el coste operativo se registra manualmente y no se recalcula automáticamente.

---

### RN-004 — Regla de campos de cosecha en MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

Cada registro de cosecha requiere `kgs` obligatorio. Los campos `rendimiento` y `litros` son opcionales, pero no pueden coexistir en el mismo registro.

---

### RN-005 — Dashboard MVP en pantalla unica

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

El dashboard MVP se presenta en una sola pantalla con scroll vertical, sin navegacion por pestanas de bloques.

---

### RN-006 — Estrategia de refresco del dashboard

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

En MVP no existe actualizacion continua en segundo plano. Los datos se actualizan al entrar al dashboard o mediante recarga manual.

**Excepción**: Ninguna en MVP.

---

### RN-007 — Conservacion de filtros en recarga

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

La recarga manual del dashboard mantiene los filtros activos del usuario.

---

### RN-008 — Filtro por defecto inicial

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

Al primer acceso se aplican por defecto todos los terrenos y la temporada actual.

---

### RN-009 — Widgets minimos obligatorios de MVP

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

El dashboard MVP debe mostrar estos cuatro widgets:

1. Resumen de temporada
2. Kg por destino
3. Kg por terreno
4. Evolucion de rendimiento

---

### RN-010 — Tratamiento de datos incompletos en kg/arbol

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard, terrenos

Si faltan arboles en uno o mas terrenos, el KPI global kg/arbol excluye esos terrenos y el widget muestra aviso de "dato incompleto".

---

### RN-011 — Orden y visualizacion de kg por terreno

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

El grafico de kg por terreno se renderiza en barras verticales. El orden es fijo por kg descendente, con desempate alfabetico por nombre de terreno. No hay orden manual.

---

### RN-012 — Categoria de destino no clasificado

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion, dashboard

La categoria canónica es `desconocido` y forma parte de la visualización de kg por destino. La UI puede mostrar el alias legible "Sin destino".

---

### RN-013 — Unidad canonica de rendimiento

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion, dashboard

La unidad canonica de rendimiento es litros por cada 100 kg de aceituna (L/100kg).

---

### RN-014 — Entradas equivalentes para rendimiento

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

El sistema acepta para rendimiento:

1. L/100kg informado directamente.
2. kg aceite/100kg informado directamente.
3. Calculo automatico desde kg entregados y litros obtenidos.

---

### RN-015 — Reglas de historico para comparativas

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: dashboard

Se muestra promedio historico desde el primer anio disponible. Promedios 5 anios y 10 anios solo se muestran cuando exista historico suficiente.

---

### RN-016 — Conversion kg-L con densidad por defecto

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: produccion

La densidad por defecto para conversion entre kg y litros de aceite es 0.92 kg/L. Se permite override por almazara, aplicable por defecto a futuros registros y editable por registro.

---

### RN-017 — Cumplimiento obligatorio de proteccion de datos UE y Espana

**Estado**: activa
**Fuente**: legal
**Módulos afectados**: todos

Todo el proyecto debe cumplir de forma obligatoria y continua con RGPD (Reglamento UE 2016/679) y LOPDGDD (LO 3/2018). Cuando aplique por tipo de funcionalidad o canal, tambien deben cumplirse LSSI-CE y ePrivacy.

Ningun requisito funcional, tecnico o de negocio puede aprobarse si entra en conflicto con obligaciones legales de proteccion de datos.

---

### RN-018 — Metodo de login MVP orientado a simplicidad

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion

Para el usuario final Antonio, el metodo de acceso principal del MVP es Google Login. El flujo debe minimizar friccion y evitar gestion de contrasenas locales.

---

### RN-019 — Passkeys planificadas para fase futura

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion

Passkeys se documentan como evolucion posterior al MVP y no bloquean la salida inicial con Google Login.

---

### RN-020 — Trazabilidad obligatoria de abandono en login

**Estado**: activa
**Fuente**: producto
**Módulos afectados**: autenticacion, observabilidad

Toda sesion que alcance pantalla de login debe ser trazable hasta exito o abandono para medir conversion del embudo y detectar barreras de acceso.

La trazabilidad debe cumplir privacidad por diseno y no registrar PII sensible en claro.

## Reglas obsoletas

| ID | Nombre | Motivo de obsolescencia | Fecha |
|----|--------|------------------------|-------|
| RN-LEGACY-001 | Dashboard solo v2 | Se redefine alcance y se incorpora dashboard operativo simple en MVP | 2026-06-30 |
