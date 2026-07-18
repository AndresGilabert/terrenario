---
id: "ADR-0003"
titulo: "Backend MVP con .NET 10 y ASP.NET Core Controllers"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["backend", "dotnet", "api"]
---

# ADR-0003 - Backend MVP con .NET 10 y ASP.NET Core Controllers

## Estado

`aceptada`

## Contexto

El equipo confirmó experiencia fuerte en ecosistema .NET y se prioriza productividad con bajo riesgo de implementación en MVP.

## Decisión

Se adopta backend con **.NET 10** y **ASP.NET Core Web API con Controllers** para los endpoints del MVP.

## Alternativas consideradas

### Opción A: Minimal APIs

**Pros**: menor boilerplate y rapidez para endpoints simples.
**Contras**: puede degradar estructura en dominios amplios si no se disciplina estrictamente.

### Opción B: Controllers

**Pros**: estructura clara para APIs de negocio, convenciones maduras, mejor estandarización de equipo.
**Contras**: más boilerplate inicial.

### Opción C: Híbrido Controllers + Minimal APIs

**Pros**: combina estructura y agilidad.
**Contras**: aumenta carga de gobernanza técnica por coexistencia de estilos.

## Consecuencias

### Positivas

- Productividad alta con stack familiar.
- Estandarización clara para equipo y revisiones.
- Integración directa con validación, seguridad y OpenAPI.

### Negativas / Trade-offs

- Mayor acoplamiento al ecosistema Microsoft/.NET.

### Neutrales

- Se podrá introducir Minimal APIs en fases posteriores si surge un caso justificado y documentado.
