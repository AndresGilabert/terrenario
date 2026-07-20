---
bloque: 07-seguridad
documento: politica-vulnerabilidades
actualizado_en: "2026-07-14"
---

# Política de Gestión de Vulnerabilidades

---

## Reporte de vulnerabilidades

**Canal de reporte**: TODO (<security@empresa.com> / HackerOne / canal privado)

**SLA de respuesta por severidad**:

| Severidad | Criterio | SLA de respuesta | SLA de fix |
|-----------|---------|-----------------|-----------|
| Crítica | CVSS >= 9.0 | 4 horas | 24 horas |
| Alta | CVSS 7.0-8.9 | 24 horas | 7 días |
| Media | CVSS 4.0-6.9 | 72 horas | 30 días |
| Baja | CVSS < 4.0 | 1 semana | Próxima release |

---

## Proceso de gestión

```text
Detección → Triaje → Confirmación → Parcheo → Verificación → Comunicación
```

1. **Detección**: reporte externo, scanner automático o incidente
2. **Triaje**: evaluar severidad (CVSS), impacto y superficie afectada
3. **Confirmación**: reproducir la vulnerabilidad en entorno controlado
4. **Parcheo**: desarrollar y testear el fix en rama aislada
5. **Verificación**: QA y pruebas de regresión
6. **Comunicación**: notificar a usuarios afectados si procede (GDPR: 72h para brechas)

---

## Herramientas de detección automática

| Herramienta | Qué detecta | Frecuencia |
|------------|------------|-----------|
| TODO (Dependabot / Snyk) | Vulnerabilidades en dependencias | En cada PR |
| TODO (SAST) | Vulnerabilidades en código fuente | En cada PR |
| TODO (DAST) | Vulnerabilidades en runtime | Semanal |

---

## Dependencias con vulnerabilidades conocidas

- Las dependencias con CVE crítica o alta se bloquean en el CI
- Se revisa el estado de dependencias TODO (semanalmente)
- No se puede mergear un PR con dependencias vulnerables sin aprobación del responsable de seguridad
