---
bloque: 05-infraestructura
documento: entornos
actualizado_en: "2026-07-13"
---

# Entornos

---

## Resumen de entornos

| Entorno | Propósito | Rama | URL | Deploy |
|---------|-----------|------|-----|--------|
| **development** | Desarrollo local | cualquier rama | `localhost` | manual |
| **staging** | QA y validación pre-release | `develop` | TODO | automático |
| **production** | Usuarios reales | `main` (solo merges desde `develop`) | TODO | manual con aprobación |

---

## Development (local)

**Requisitos**:

- TODO (Docker, Node.js vX, Python vX, etc.)

**Arrancar el entorno**:

```bash
# TODO: comandos para levantar el entorno local
```

**Variables de entorno**: copiar `.env.example` a `.env` y rellenar los valores.
Nunca commitear el `.env` real.

---

## Staging

**Propósito**: validación final antes de ir a producción. QA ejecuta sus tests aquí.

**Acceso**: TODO (VPN / IP allowlist / autenticación)

**Deploy**: automático al mergear a `develop`. Ver pipeline en `ci-cd.md`.

**Base de datos**: copia anonimizada de producción, renovada TODO (diariamente / semanalmente).

---

## Production

**Acceso**: restringido. Solo el equipo de infraestructura y tech leads tienen acceso directo.

**Deploy**: manual, requiere aprobación de TODO en el pipeline de CI/CD.

**Backup**: TODO (frecuencia, retención, proceso de restauración en `disaster-recovery.md`).

---

## Variables de entorno por entorno

| Variable | Development | Staging | Production | Descripción |
|----------|-------------|---------|------------|-------------|
| `APP_ENV` | `development` | `staging` | `production` | Entorno activo |
| `LOG_LEVEL` | `debug` | `info` | `warning` | Nivel de logs |
| `DATABASE_URL` | local | gestionado en CI | secreto | Cadena de conexión DB |
| TODO | | | | |

---

## Gestión de secretos

Los secretos de producción se gestionan en TODO (AWS Secrets Manager / Vault / etc.).
**Nunca** incluir secretos en el código, en variables de CI/CD visibles ni en este documento.
Ver `../07-seguridad/modelo-seguridad.md`.
