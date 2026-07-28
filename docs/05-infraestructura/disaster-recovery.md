---
bloque: 05-infraestructura
documento: disaster-recovery
actualizado_en: "2026-07-18"
---

# Disaster Recovery

---

## Objetivos de recuperación

| Servicio | RTO objetivo | RPO objetivo |
|---------|--------------|--------------|
| API principal | <= 1 hora | <= 24 horas en fase C; <= 7 días en fase A con backup semanal |
| Base de datos | <= 4 horas | Según política de backup de fase |

---

## Estrategia de backup

| Fase | Backup | Retención | Notas |
|------|--------|-----------|-------|
| C (actual) | Snapshot puntual/manual | Sin política fija | Riesgo aceptado por coste mínimo |
| A | Snapshot automático semanal | 2 semanas | Política mínima acordada |
| B | Snapshot diario | 7 días | Complementado con snapshot semanal |
| B | Snapshot semanal | 8 semanas | Soporte a recuperación de incidente tardío |

---

## Procedimiento de recuperación

### Escenario 1: fallo de instancia de aplicación

1. El load balancer detecta la instancia caída y deja de enviarle tráfico
2. El autoscaling lanza una nueva instancia automáticamente
3. Verificar que la nueva instancia está sana en los dashboards

**Tiempo estimado**: < 5 minutos (automático)

### Escenario 2: corrupción o pérdida de base de datos

1. Notificar al equipo y activar el protocolo de incidente
2. Identificar el punto de restauración más reciente viable
3. Restaurar el último snapshot válido
4. Validar integridad lógica mínima de entidades operativas
5. Ejecutar smoke tests de integridad de datos
6. Reconectar la aplicación a la DB restaurada

**Tiempo estimado**: < 4 horas

---

## Contactos durante un DR

En fase actual (equipo de 1 persona) el responsable técnico/fundador asume activación y cierre de DR.

---

## Tests de DR

1. Fase A: prueba manual de restore trimestral en `dev` con checklist mínima y evidencia de resultado.
2. Fase B: se mantiene restore trimestral manual en `dev`.
3. Registrar resultado en `../05-infraestructura/runbooks/`.

## Trazabilidad KB

1. Política de entornos y fase activa: `entornos.md`
2. Reglas de release y rollback: `../08-procesos/proceso-release.md`
3. Seguridad y secretos: `../07-seguridad/modelo-seguridad.md`
