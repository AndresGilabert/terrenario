---
bloque: 08-procesos
documento: proceso-release
actualizado_en: "2026-07-13"
---

# Proceso de Release

---

## Versionado

Seguimos **Semantic Versioning** (semver):

```text
v{MAJOR}.{MINOR}.{PATCH}

MAJOR: cambio incompatible en la API o comportamiento (breaking change)
MINOR: nueva funcionalidad compatible hacia atrás
PATCH: corrección de bugs compatible hacia atrás
```

---

## Flujo de release

```mermaid
flowchart LR
    dev["Desarrollo en\nfeature branches"] --> develop["Merge a develop"]
    develop --> staging["Deploy automático\na Staging"]
    staging --> qa["QA + smoke tests"]
    qa --> pr["PR revisado develop → main"]
    pr --> approval["Revisión\nmanual"]
    approval --> main["Merge a main"]
    main --> tag["Tag semántico\ngit tag v2.1.0"]
    tag --> prod["Deploy a\nProducción"]
    prod --> verify["Verificación\npost-deploy"]
```

---

## Checklist pre-release

- [ ] Todos los tickets del hito están en estado `completado`
- [ ] Tests de staging pasando (incluyendo E2E)
- [ ] `docs/10-releases/v{version}.md` creado con el changelog
- [ ] `docs/00-meta/changelog.md` actualizado si hay cambios estructurales en la KB
- [ ] Módulos afectados tienen documentación actualizada en `docs/03-modulos/`
- [ ] No hay CVEs críticas o altas sin parchear en las dependencias
- [ ] SLOs de staging estables durante las últimas 24h

---

## Procedimiento de release

```bash
# 1. Asegurarse de estar en develop y actualizado
git checkout develop && git pull

# 2. Crear el PR revisado desde develop hacia main

# 3. Hacer el merge a main solo si develop ya está validado

# 4. Tras el merge a main, crear el tag de release
git tag -a v2.1.0 -m "Release v2.1.0 — [descripción breve]"

# 5. Push del tag (activa el pipeline de producción)
git push origin v2.1.0
```

---

## Rollback

Si los smoke tests de producción fallan tras el deploy:

1. El pipeline hace rollback automático al deployment anterior
2. Si el rollback automático falla: usar el runbook `../05-infraestructura/runbooks/`
3. Notificar en `#incidents` con severidad apropiada

---

## Hotfix

Para bugs críticos en producción que no pueden esperar al próximo release:

```text
main → hotfix/PROJ-XXX--descripcion → develop → main → tag v2.1.1
```

El hotfix sigue el mismo proceso de PR y aprobación, pero con prioridad máxima.

---

## Comunicación del release

- **Interno**: update en el canal del equipo con enlace al changelog
- **Usuarios** (si hay cambios visibles): TODO (email / in-app / status page)
