---
bloque: 02-arquitectura
documento: vision-general
actualizado_en: ""
---

# Visión General de la Arquitectura

> Diagrama C4 Nivel 1 (Contexto) y Nivel 2 (Contenedores).
> Para el detalle de componentes internos, ver `componentes.md`.
> Para decisiones técnicas, ver `decisiones/`.

---

## Nivel 1 — Diagrama de Contexto (C4)

> Muestra el sistema en relación con sus usuarios y sistemas externos.

```mermaid
C4Context
    title Diagrama de Contexto — [Nombre del Sistema]

    Person(usuario, "Usuario", "TODO: descripción del usuario principal")
    System(sistema, "[Nombre del Sistema]", "TODO: qué hace el sistema")
    System_Ext(sistemaExterno, "[Sistema Externo]", "TODO")

    Rel(usuario, sistema, "Usa")
    Rel(sistema, sistemaExterno, "Integra con")
```

---

## Nivel 2 — Diagrama de Contenedores (C4)

> Muestra los principales bloques tecnológicos del sistema.

```mermaid
C4Container
    title Diagrama de Contenedores — [Nombre del Sistema]

    Person(usuario, "Usuario")

    Container_Boundary(c1, "[Nombre del Sistema]") {
        Container(webapp, "Web App", "React / Vue / etc.", "TODO")
        Container(api, "API Backend", "Node.js / Python / etc.", "TODO")
        ContainerDb(db, "Base de Datos", "PostgreSQL / etc.", "TODO")
    }

    System_Ext(externo, "Sistema Externo", "TODO")

    Rel(usuario, webapp, "Usa", "HTTPS")
    Rel(webapp, api, "Llama a", "HTTPS/JSON")
    Rel(api, db, "Lee/Escribe", "SQL")
    Rel(api, externo, "Integra con", "HTTPS")
```

---

## Principios arquitecturales

> Las decisiones que guían cómo se construye el sistema.
> Para el "por qué" de cada decisión, ver `decisiones/`.

| Principio | Descripción |
|-----------|-------------|
| TODO | |

## Restricciones

> Limitaciones técnicas, de negocio o de equipo que condicionan la arquitectura.

- TODO

## Stack tecnológico

> Ver detalle completo en `tech-stack.md`.

| Capa | Tecnología | Versión |
|------|-----------|---------|
| TODO | | |
