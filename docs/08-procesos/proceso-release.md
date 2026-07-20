---
bloque: 08-procesos
documento: proceso-release
actualizado_en: "2026-07-18"
---

# Proceso de Release

---

## Versionado

Seguimos Semantic Versioning (semver) con bump automático según tipo de commit (Conventional Commits):

```text
v{MAJOR}.{MINOR}.{PATCH}

MAJOR: cambio incompatible en la API o comportamiento (breaking change)
MINOR: nueva funcionalidad compatible hacia atrás
PATCH: corrección de bugs compatible hacia atrás
```

---

## Flujo de release (fase A)

```mermaid
flowchart LR
    dev["Desarrollo en\nfeature branches"] --> develop["Merge a develop"]
    develop --> devenv["Deploy automático\na dev"]
    devenv --> qa["QA + smoke tests"]
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
- [ ] Tests de dev pasando (incluyendo smoke E2E)
- [ ] `docs/10-releases/v{version}.md` creado con el changelog
- [ ] `docs/00-meta/changelog.md` actualizado si hay cambios estructurales en la KB
- [ ] Módulos afectados tienen documentación actualizada en `docs/03-modulos/`
- [ ] No hay CVEs críticas o altas sin parchear en las dependencias
- [ ] SLOs de dev estables durante las últimas 24h (o de staging cuando fase B esté activa)

---

## Procedimiento de release

```bash
# 1. Asegurarse de estar en develop y actualizado
git checkout develop && git pull

# 2. Crear el PR revisado desde develop hacia main

# 3. Hacer el merge a main solo si develop ya está validado

# 4. Tras el merge a main, validar bump semántico y crear tag
git tag -a v2.1.0 -m "Release v2.1.0 — [descripción breve]"

# 5. Push del tag (activa el pipeline de producción)
git push origin v2.1.0
```

---

## Rollback

Si los smoke tests de producción fallan tras el deploy:

1. Ejecutar rollback semiautomático via script/pipeline con aprobación manual.
2. Si el rollback semiautomático falla: usar el runbook `../05-infraestructura/runbooks/`.
3. Notificar en `#incidents` con severidad apropiada

---

## Hotfix

Para bugs críticos en producción que no pueden esperar al próximo release:

```text
main -> hotfix/PROJ-XXX--descripcion -> main -> back-merge a develop -> tag vX.Y.Z
```

El hotfix sigue el mismo proceso de PR y aprobación, pero con prioridad máxima.

---

## Comunicación del release

- **Interno**: update en el canal del equipo con enlace al changelog
- **Usuarios** (si hay cambios visibles): TODO (email / in-app / status page)

## Cadencia de release en fase A

Releases bajo demanda con ventana mínima predefinida (objetivo operativo: hasta 2 ventanas por semana).

## Trazabilidad KB

1. Flujo de ramas y hotfix: `../04-ingenieria/flujo-git.md`
2. Pipeline técnico: `../05-infraestructura/ci-cd.md`
3. Notas de versión: `../00-meta/plantillas/release-notes.md`
