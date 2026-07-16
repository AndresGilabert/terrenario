---
integracion: stripe
tipo: psp
estado: activo
owner: "@equipo-pagos"
actualizado_en: "2026-06-09"
---

# Integración: Stripe

**Propósito**: Procesamiento de pagos con tarjeta para mercados internacionales.
**Documentación oficial**: <https://stripe.com/docs/api>
**Dashboard**: <https://dashboard.stripe.com>

---

## Autenticación

- **Tipo**: API Key (Bearer token)
- **Variable de entorno**: `STRIPE_SECRET_KEY`
- **Clave pública (frontend)**: `STRIPE_PUBLISHABLE_KEY`
- Las claves son diferentes por entorno (test / live)

---

## Endpoints utilizados

| Operación | Método | Endpoint Stripe |
|-----------|--------|----------------|
| Crear PaymentIntent | POST | `/v1/payment_intents` |
| Confirmar pago | POST | `/v1/payment_intents/{id}/confirm` |
| Capturar | POST | `/v1/payment_intents/{id}/capture` |
| Cancelar | POST | `/v1/payment_intents/{id}/cancel` |
| Reembolsar | POST | `/v1/refunds` |
| Consultar estado | GET | `/v1/payment_intents/{id}` |

---

## Webhooks

Stripe notifica eventos de forma asíncrona a:
`POST /api/v1/payments/webhook`

| Evento Stripe | Acción interna |
|--------------|----------------|
| `payment_intent.succeeded` | Marcar transacción como `capturada` |
| `payment_intent.payment_failed` | Marcar transacción como `fallida` |
| `charge.refunded` | Marcar transacción como `reembolsada` |

**Validación de firma**: `Stripe-Signature` header + `PAYMENT_WEBHOOK_SECRET`

---

## Rate limits

- **Límite**: 100 req/s en live, 25 req/s en test
- **Estrategia ante rate limit**: backoff exponencial (1s, 2s, 4s), máximo 3 reintentos
