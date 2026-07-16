---
id: "RB-001"
titulo: "Reiniciar el servicio de pagos"
servicio: "payment-service"
owner: "@equipo-pagos"
ultima_revision: "2026-06-09"
tiempo_estimado: "10 minutos"
---

# Runbook RB-001 — Reiniciar el servicio de pagos

> **Servicio**: payment-service
> **Tiempo estimado**: 10 minutos
> **Owner**: @equipo-pagos

---

## Cuándo usar este runbook

Cuando se active la alerta `ServiceDown` para `payment-service`, o cuando el servicio
no responda al health check tras un deploy.

**Alerta**: `payment-service.health_check.failed`

---

## Diagnóstico previo

- [ ] Verificar logs recientes: buscar errores en los últimos 5 minutos
- [ ] Comprobar si el problema coincide con un deploy reciente
- [ ] Verificar que la base de datos está accesible

```bash
# Revisar logs del servicio
TODO: comando para ver logs

# Comprobar estado del servicio
TODO: comando de health check
```

---

## Pasos de resolución

### Paso 1: Reinicio gradual (preferido)

```bash
# Reiniciar sin downtime (rolling restart)
TODO: comando de rolling restart
```

**Resultado esperado**: el servicio responde al health check en < 2 minutos.

---

### Paso 2: Si el rolling restart falla — reinicio forzado

```bash
# Detener el servicio
TODO: comando stop

# Esperar 30 segundos y arrancar
TODO: comando start
```

**Resultado esperado**: el servicio arranca y responde al health check.

---

## Verificación

```bash
# Comprobar que el health check responde 200
TODO: curl o comando equivalente

# Verificar que los pagos se están procesando
TODO: comando de verificación
```

---

## Escalación

Si tras los pasos anteriores el servicio sigue caído:

1. Escala a: @equipo-infraestructura
2. Canal: `#incidents`
3. Información a proporcionar: logs del último reinicio, métricas de la última hora

---

## Postmortem

Si el incidente duró > 15 minutos o afectó a transacciones de pago,
sigue el proceso en `../../08-procesos/gestion-incidentes.md`.
