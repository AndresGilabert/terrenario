---
bloque: 05-infraestructura
documento: disaster-recovery
actualizado_en: ""
---

# Disaster Recovery

---

## Objetivos de recuperación

| Servicio | RTO (tiempo máx. de recuperación) | RPO (pérdida de datos aceptable) |
|---------|----------------------------------|----------------------------------|
| API principal | 1 hora | 15 minutos |
| Base de datos | 4 horas | 5 minutos |
| Módulo de Payments | 30 minutos | 0 (sin pérdida) |

---

## Estrategia de backup

| Dato | Frecuencia | Retención | Almacenamiento |
|------|-----------|-----------|---------------|
| Base de datos completa | Diaria | 30 días | TODO |
| WAL / binlog (incremental) | Cada 5 minutos | 7 días | TODO |
| Archivos estáticos | En cada cambio | 90 días | TODO |

---

## Procedimiento de recuperación

### Escenario 1: Fallo de instancia de aplicación

1. El load balancer detecta la instancia caída y deja de enviarle tráfico
2. El autoscaling lanza una nueva instancia automáticamente
3. Verificar que la nueva instancia está sana en los dashboards

**Tiempo estimado**: < 5 minutos (automático)

### Escenario 2: Corrupción o pérdida de base de datos

1. Notificar al equipo y activar el protocolo de incidente
2. Identificar el punto de restauración más reciente viable
3. Restaurar el backup: `TODO: comando de restauración`
4. Aplicar los WAL incrementales hasta el RPO
5. Ejecutar smoke tests de integridad de datos
6. Reconectar la aplicación a la DB restaurada

**Tiempo estimado**: < 4 horas

---

## Contactos durante un DR

| Rol | Persona | Teléfono |
|-----|---------|---------|
| TODO | | |

---

## Tests de DR

Los procedimientos de DR se prueban **TODO** (trimestralmente / semestralmente).
El resultado se documenta en `../../05-infraestructura/runbooks/`.
