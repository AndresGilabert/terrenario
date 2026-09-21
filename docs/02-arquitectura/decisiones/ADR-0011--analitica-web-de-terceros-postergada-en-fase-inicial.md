---
id: "ADR-0011"
titulo: "Analítica web de terceros postergada en la fase inicial de posicionamiento orgánico"
estado: aceptada
fecha: "2026-08-31"
decisores: ["@andres"]
etiquetas: ["marketing", "observabilidad", "privacidad", "cumplimiento"]
---

# ADR-0011 — Analítica web de terceros postergada en la fase inicial de posicionamiento orgánico

## Estado

`aceptada`

## Contexto

Tras cerrar el MVP se abre la horizontal de marketing para mejorar adquisición orgánica. En este punto
se plantea incorporar Google Analytics como vía rápida de medición de tráfico.

El sistema ya tiene medición propia de primera parte para embudo de login y uso del producto, con
señales agregadas en `GET /api/v1/ops/signals` (`MVP-601`/`602`/`603`).

Además, las reglas activas de cumplimiento fijan que ninguna tecnología no esencial se activa sin
consentimiento previo (`RN-042`), y hoy el producto no muestra banner de cookies porque opera con
tecnologías exentas y sin analítica de terceros.

## Decisión

Se **pospone Google Analytics y cualquier analítica web de terceros** durante la fase inicial de
posicionamiento orgánico.

En esta fase se usará:

1. Medición propia existente (telemetría de primera parte y señales operativas).
2. Métricas de indexación y rendimiento de buscadores en herramientas gratuitas de webmaster.
3. Instrumentación propia adicional (UTM y eventos de aterrizaje) siempre bajo el mismo modelo de
   primera parte y agregación sin perfilado.

La decisión se reabre cuando exista necesidad real de capacidades que la medición propia no cubra.

## Alternativas consideradas

### Opción A: Incorporar Google Analytics desde el inicio

**Pros**: despliegue rápido, paneles listos y comparativas estándar de mercado.
**Contras**: exige consentimiento previo, banner/CMP, actualización legal y cambios de CSP; añade
dependencia de un tercero y posible sesgo por rechazos de consentimiento.

### Opción B: Mantener solo medición propia en fase inicial (elegida)

**Pros**: coherencia con `RN-042`, sin banner en esta fase, menor complejidad legal/técnica y coste
cero real.
**Contras**: menor ergonomía de consulta y más trabajo propio de reporting.

## Consecuencias

### Positivas

- Se mantiene la coherencia entre producto, política de privacidad y modelo de seguridad.
- Se evita abrir una capa de cumplimiento (CMP/consent mode) en la primera entrega de growth.
- Se prioriza ejecución en contenido indexable y SEO técnico, que hoy es el cuello de botella.

### Negativas / Trade-offs

- La lectura de tráfico depende de señales propias y consultas operativas, con peor UX analítica.
- Algunas métricas de adquisición requerirán construir vistas/resúmenes adicionales.

### Neutrales

- Esta decisión no prohíbe analítica de terceros; la difiere hasta que el coste de oportunidad lo
  justifique y exista diseño legal/técnico completo.

## Criterios para reabrir la decisión

Reabrir cuando se cumpla al menos una condición:

1. Dos ciclos mensuales consecutivos con necesidad no cubierta por la medición propia.
2. Necesidad de atribución/canal que no pueda resolverse con UTM y agregación de primera parte.
3. Decisión explícita de producto para asumir CMP y consentimiento previo como alcance.

## Referencias

- `../../01-producto/reglas-de-negocio.md` (`RN-042`)
- `../../07-seguridad/privacidad-datos.md`
- `../../05-infraestructura/observabilidad.md`
- `ADR-0010--envio-de-email-transaccional-por-smtp.md`
