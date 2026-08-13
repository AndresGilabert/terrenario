---
id: "ADR-0003"
titulo: "Backend MVP con .NET 9 y ASP.NET Core Controllers"
estado: aceptada
fecha: "2026-07-17"
decisores: ["@po", "@tech-lead"]
etiquetas: ["backend", "dotnet", "api"]
---

# ADR-0003 - Backend MVP con .NET 9 y ASP.NET Core Controllers

## Estado

`aceptada`

## Contexto

El equipo confirmó experiencia fuerte en ecosistema .NET y se prioriza productividad con bajo riesgo de implementación en MVP.

## Decisión

Se adopta backend con **.NET 9** y **ASP.NET Core Web API con Controllers** para los endpoints del MVP.

> **Corrección de versión (2026-08-12, `P-129`).** Este ADR y los documentos derivados decían
> **.NET 10** desde su redacción. Nunca fue cierto: `Terrenario.Api.csproj` declara `net9.0` **desde
> el primer commit** (`MVP-101`, 2026-07-24) y el CI instala `9.0.x`. No hubo migración ni cambio de
> criterio que registrar —hubo una versión mal escrita en la KB que se propagó a `tech-stack.md`,
> `vision-general.md`, `entornos.md`, `ADR-0001` y al nombre de este fichero—. Se corrige el número
> en todos ellos en vez de dejar constancia de una decisión que no se tomó. `componentes.md` ya se
> había corregido antes, en `P-094`.
>
> Lo que este ADR decide de verdad —**Controllers frente a Minimal APIs**— no cambia: es lo que se
> comparó en las alternativas y lo que está implementado.

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
