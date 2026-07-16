---
bloque: 07-seguridad
documento: politica-vulnerabilidades
actualizado_en: ""
---

# PolÃ­tica de GestiÃ³n de Vulnerabilidades

---

## Reporte de vulnerabilidades

**Canal de reporte**: TODO (<security@empresa.com> / HackerOne / canal privado)

**SLA de respuesta por severidad**:

| Severidad | Criterio | SLA de respuesta | SLA de fix |
|-----------|---------|-----------------|-----------|
| CrÃ­tica | CVSS >= 9.0 | 4 horas | 24 horas |
| Alta | CVSS 7.0-8.9 | 24 horas | 7 dÃ­as |
| Media | CVSS 4.0-6.9 | 72 horas | 30 dÃ­as |
| Baja | CVSS < 4.0 | 1 semana | PrÃ³xima release |

---

## Proceso de gestiÃ³n

```text
DetecciÃ³n â†’ Triaje â†’ ConfirmaciÃ³n â†’ Parcheo â†’ VerificaciÃ³n â†’ ComunicaciÃ³n
```

1. **DetecciÃ³n**: reporte externo, scanner automÃ¡tico o incidente
2. **Triaje**: evaluar severidad (CVSS), impacto y superficie afectada
3. **ConfirmaciÃ³n**: reproducir la vulnerabilidad en entorno controlado
4. **Parcheo**: desarrollar y testear el fix en rama aislada
5. **VerificaciÃ³n**: QA y pruebas de regresiÃ³n
6. **ComunicaciÃ³n**: notificar a usuarios afectados si procede (GDPR: 72h para brechas)

---

## Herramientas de detecciÃ³n automÃ¡tica

| Herramienta | QuÃ© detecta | Frecuencia |
|------------|------------|-----------|
| TODO (Dependabot / Snyk) | Vulnerabilidades en dependencias | En cada PR |
| TODO (SAST) | Vulnerabilidades en cÃ³digo fuente | En cada PR |
| TODO (DAST) | Vulnerabilidades en runtime | Semanal |

---

## Dependencias con vulnerabilidades conocidas

- Las dependencias con CVE crÃ­tica o alta se bloquean en el CI
- Se revisa el estado de dependencias TODO (semanalmente)
- No se puede mergear un PR con dependencias vulnerables sin aprobaciÃ³n del responsable de seguridad
