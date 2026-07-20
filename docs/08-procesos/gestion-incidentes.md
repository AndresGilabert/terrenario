---
bloque: 08-procesos
documento: gestion-incidentes
actualizado_en: "2026-07-13"
---

# Gestión de Incidentes

---

## Severidades

| Nivel | Criterio | Tiempo de respuesta | Comunicación |
|-------|---------|--------------------|-|
| **SEV-1** | Producción caída para todos los usuarios | < 15 min | Inmediata (PagerDuty + canal #incidents) |
| **SEV-2** | Funcionalidad crítica degradada (ej: pagos fallando) | < 30 min | Canal #incidents |
| **SEV-3** | Funcionalidad no crítica afectada | < 2 horas | Ticket en sistema de trabajo |
| **SEV-4** | Problema menor, workaround disponible | Siguiente día hábil | Ticket en sistema de trabajo |

---

## Proceso de respuesta

```text
1. DETECCIÓN
   └── Alerta automática o reporte de usuario

2. TRIAGE (< 15 min para SEV-1/2)
   ├── Confirmar el incidente
   ├── Asignar severidad
   └── Designar Incident Commander (IC)

3. COMUNICACIÓN
   ├── Notificar en #incidents: "IC: @user | SEV-X | Síntomas: ..."
   └── Actualizar página de status si aplica

4. RESOLUCIÓN
   ├── Diagnosticar (usar runbooks de 05-infraestructura/runbooks/)
   ├── Aplicar fix o rollback
   └── Verificar con smoke tests

5. CIERRE
   ├── Confirmar resolución en #incidents
   └── Programar postmortem (SEV-1/2 obligatorio)
```text
---

## On-call

| Equipo | Herramienta | Rotación |
|--------|------------|---------|
| TODO | PagerDuty / OpsGenie | Semanal |

---

## Postmortem

Obligatorio para SEV-1 y SEV-2. Opcional pero recomendado para SEV-3.

**Template**: `../00-meta/plantillas/runbook.md` (adaptar para postmortem)

**Plazo**: 5 días hábiles tras el cierre del incidente

**Formato**:

```markdown
## Resumen
## Timeline
## Causa raíz
## Impacto (usuarios, duración, SLO)
## Qué salió bien
## Qué mejorar
## Acciones de seguimiento (con owner y fecha)
```text
**Principio**: los postmortems son **blameless** — el objetivo es mejorar el sistema, no señalar personas.

---

## Runbooks disponibles

Ver `../05-infraestructura/runbooks/` para los procedimientos operacionales.
