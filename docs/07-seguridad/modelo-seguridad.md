---
bloque: 07-seguridad
documento: modelo-seguridad
actualizado_en: "2026-07-18"
---

# Modelo de Seguridad

> **IMPORTANTE para agentes de IA**: Este documento define los límites de seguridad del sistema.
> Todo código generado debe respetar estas restricciones. En caso de duda, consultar antes de implementar.

---

## Principios de seguridad

| Principio | Descripción |
|-----------|-------------|
| **Mínimo privilegio** | Cada servicio/usuario tiene solo los permisos necesarios |
| **Defensa en profundidad** | Múltiples capas de seguridad, sin punto único de fallo |
| **Zero trust** | No asumir confianza por estar en la red interna |
| **Fail secure** | Ante error, denegar acceso por defecto |
| **Cumplimiento normativo por defecto** | Todo diseno e implementacion debe cumplir RGPD + LOPDGDD, y normas condicionadas cuando apliquen |

## Cumplimiento legal de referencia

La definicion normativa de privacidad y proteccion de datos esta en `privacidad-datos.md`.

Regla de aplicacion:

1. Lo obligatorio (RGPD + LOPDGDD) se cumple siempre.
2. Lo obligatorio condicionado (LSSI-CE, ePrivacy, EIPD, brechas) se evalua por caso y se documenta.
3. Lo recomendado no puede contradecir ni sustituir obligaciones legales.

---

## Superficie de ataque

### Entradas del sistema (validar SIEMPRE)

| Entrada | Validación requerida |
|---------|---------------------|
| Requests HTTP | Schema validation en el controller |
| Webhooks externos | Validación de firma criptográfica |
| Mensajes del bus de eventos | Schema validation + origen verificado |
| Archivos subidos | Tipo MIME + tamaño + escaneo de malware |

---

## Control de acceso

**Autenticación**: ver `autenticacion-autorizacion.md`

**Autorización**:

- Modelo: RBAC (Role-Based Access Control)
- Los recursos se protegen a nivel de servicio
- Validar siempre que el usuario tiene acceso al recurso específico (no solo al endpoint)

---

## Gestión de secretos

| Tipo de secreto | Dónde se almacena | Rotación |
|----------------|-------------------|---------|
| API Keys de integraciones externas | Secret Manager del proveedor (`dev` y `prod`) | Cada 90 días |
| Credenciales de DB | Secret Manager del proveedor (`dev` y `prod`) | Cada 90 días |
| JWT signing keys | Secret Manager del proveedor (`dev` y `prod`) | Cada 30 días |
| Webhook secrets | Secret Manager del proveedor (`dev` y `prod`) | En caso de compromiso |

**NUNCA en**: código fuente, variables de CI/CD visibles, logs, documentación.

## Trazabilidad KB

1. Entornos y despliegue: `../05-infraestructura/entornos.md`
2. Autenticación y autorización: `autenticacion-autorizacion.md`

---

## OWASP Top 10 — controles implementados

| Riesgo | Control |
|--------|---------|
| A01: Broken Access Control | RBAC + validación de ownership en cada request |
| A02: Cryptographic Failures | TLS 1.3 en todas las comunicaciones, cifrado en reposo de datos PII |
| A03: Injection | Queries parametrizadas, ORM, validación de inputs |
| A05: Security Misconfiguration | Revisión de config en cada release, headers de seguridad |
| A09: Security Logging | Logs de auditoría para operaciones sensibles |

---

## Reporte de vulnerabilidades

Ver proceso completo en `politica-vulnerabilidades.md`.

Contacto de seguridad (temporal): @tech-lead (canal privado interno de seguridad)
