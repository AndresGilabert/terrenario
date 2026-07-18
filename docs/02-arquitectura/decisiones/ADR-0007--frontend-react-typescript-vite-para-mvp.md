---
id: "ADR-0007"
titulo: "Frontend MVP con React + TypeScript + Vite"
estado: aceptada
fecha: "2026-07-18"
decisores: ["@po", "@tech-lead"]
etiquetas: ["frontend", "spa", "mvp"]
---

# ADR-0007 - Frontend MVP con React + TypeScript + Vite

## Estado

`aceptada`

## Contexto

El MVP requiere una SPA con bajo riesgo de entrega, buen soporte para formularios y validaciones, y trazabilidad de calidad con pruebas E2E.

En la KB existia un hueco abierto en `tech-stack.md` para el stack frontend, lo que impedía cerrar la baseline técnica implementable.

## Decisión

Se adopta **React 19 + TypeScript 5.x + Vite 6.x** como stack frontend del MVP.

Alcance:

1. SPA web para flujos operativos del MVP.
2. Build y desarrollo local con Vite.
3. Tipado estático con TypeScript.
4. Integración de pruebas E2E con Playwright según estrategia de testing.

## Alternativas consideradas

### Opción A: React + TypeScript + Vite

**Pros**: ecosistema maduro, alta productividad en formularios y dashboards, integración natural con Playwright.
**Contras**: requiere gobernanza de librerías para evitar dispersión.

### Opción B: Blazor WebAssembly

**Pros**: coherencia máxima con stack .NET y unificación de lenguaje.
**Contras**: menor ecosistema de componentes web y menor disponibilidad de perfiles frontend.

### Opción C: Vue 3 + TypeScript + Vite

**Pros**: curva de adopción suave y buena productividad en UI de negocio.
**Contras**: menor alineación con experiencia principal .NET declarada para el equipo.

## Consecuencias

### Positivas

- Se cierra el hueco técnico del stack frontend en la baseline.
- Se mejora la velocidad de implementación para validación temprana con usuarios.
- Se mantiene alta capacidad de pruebas automatizadas en flujos críticos.

### Negativas / Trade-offs

- Será necesario fijar una guía de librerías de estado/UI para evitar deriva arquitectónica.

### Neutrales

- La decisión no altera los ADRs de backend, datos ni concurrencia ya aceptados.
