---
integracion: stripe
documento: gestion-errores
actualizado_en: "2026-07-14"
---

# Stripe - Gestión de Errores

---

## Códigos de error y acciones

| Código Stripe | Tipo | Acción | Reintentable |
|--------------|------|--------|-------------|
| `card_declined` | `card_error` | Informar al usuario, no reintentar | No |
| `insufficient_funds` | `card_error` | Informar al usuario | No |
| `expired_card` | `card_error` | Solicitar nueva tarjeta | No |
| `incorrect_cvc` | `card_error` | Solicitar corrección | No |
| `rate_limit_error` | `api_error` | Backoff exponencial (máx. 3 intentos) | Sí |
| `api_connection_error` | `api_error` | Reintento con backoff, alerta si persiste | Sí |
| `authentication_error` | `api_error` | Alertar inmediatamente - problema de configuración | No |
| `invalid_request_error` | `invalid_request_error` | Log + alerta - bug en nuestra integración | No |

---

## Idempotencia

Usar `Idempotency-Key` en todas las mutaciones para evitar cobros duplicados ante reintentos:

```text
Idempotency-Key: {transaction_id}--{operation}--{attempt_number}
```
---

## Alertas específicas de Stripe

| Condición | Alerta | Severidad |
|-----------|--------|-----------|
| `authentication_error` en cualquier request | Inmediata | crítica |
| Tasa de `card_declined` > 15% en 10min | Dashboard + Slack | warning |
| Webhook sin procesar > 5min | Slack | warning |

---

## Fallback

Si Stripe no está disponible (confirmado con 3 reintentos fallidos):

1. Registrar la transacción en estado `pendiente-fallback`
2. Notificar al equipo en `#incidents`
3. Si hay PSP alternativo configurado (`PAYMENT_PSP_FALLBACK`), intentar con él
4. Si no hay fallback, devolver error `503` al cliente con mensaje claro
