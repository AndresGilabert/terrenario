---
bloque: 04-ingenieria
documento: onboarding
actualizado_en: ""
---

# Guía de Onboarding Técnico

> Bienvenido al equipo. Esta guía te llevará desde cero hasta hacer tu primer commit.

---

## Semana 1 — Contexto y entorno

### Día 1-2: Lectura de contexto

- [ ] Leer `AGENTS.md` — entender la estructura de la KB
- [ ] Leer `../../01-producto/vision-y-objetivos.md` — entender el producto
- [ ] Leer `../../02-arquitectura/vision-general.md` — entender el sistema
- [ ] Leer los `README.md` de los módulos de tu equipo en `../../03-modulos/`

### Día 3-5: Entorno de desarrollo

- [ ] Clonar el repositorio
- [ ] Seguir el README del repo para levantar el entorno local
- [ ] Ejecutar los tests: todos deben pasar
- [ ] Instalar los pre-commit hooks: `pip install pre-commit && pre-commit install`

---

## Semana 2 — Primer ticket

- [ ] Elegir un ticket de tipo `spike` o baja complejidad con el tech lead
- [ ] Leer el `spec.md` del ticket
- [ ] Crear la rama siguiendo `flujo-git.md`
- [ ] Implementar con tests
- [ ] Abrir el PR siguiendo la guía de `guia-code-review.md`

---

## Accesos necesarios

| Sistema | Quién lo gestiona | Tiempo estimado |
|---------|------------------|----------------|
| Repositorio de código | TODO | Inmediato |
| Entornos de staging | TODO | 1 día |
| Sistema de tickets (Jira / etc.) | TODO | Inmediato |
| Herramientas de monitoring | TODO | 1 día |
| Canal on-call (si aplica) | TODO | Cuando estés listo |

---

## Recursos de referencia

| Recurso | Dónde encontrarlo |
|---------|------------------|
| Estándares de código | `estandares-codigo.md` |
| Flujo de Git | `flujo-git.md` |
| Estrategia de testing | `estrategia-testing.md` |
| Arquitectura del sistema | `../../02-arquitectura/` |
| Módulos del negocio | `../../03-modulos/` |
| Glosario del dominio | `../../99-glosario/glosario.md` |

---

## Contactos de referencia

| Rol | Persona | Para qué |
|-----|---------|---------|
| Tech Lead | TODO | Dudas técnicas, diseño |
| PM del equipo | TODO | Dudas de producto |
| DevOps / SRE | TODO | Entornos, CI/CD, accesos |
