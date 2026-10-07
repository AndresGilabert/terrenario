---
id: "MKT-108"
tipo: tarea
titulo: "TDD: Bloques de media y clúster de guías en las landings"
estado: borrador
tickets: []
epica: "MKT-100--posicionamiento-organico-inicial"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["marketing", "frontend"]
  modulo_path: "03-modulos/plataforma-de-aplicacion/"
  componentes: ["landing-publica"]
  etiquetas: ["content", "seo", "media", "csp"]
  nivel_riesgo: medio
creado_en: "2026-09-28"
actualizado_en: "2026-10-07"
---

# TDD: MKT-108 — Bloques de media y clúster de guías en las landings

> **Referencia al spec**: [spec.md](./spec.md) · **Auditoría**: [inventario-landings.md](./inventario-landings.md)

## Resumen técnico

La propuesta de este diseño introdujo un **tercer clúster** con ruta propia (`/guias/{slug}`) y
media editorial. A fecha 2026-10-07, el clúster ya está en uso y
`como-empezar-en-terrenario` está implementada con cinco imágenes autoalojadas. El soporte de vídeo
descrito más abajo sigue siendo una propuesta; no existe en el modelo ni en el render actual.

La implementación actual extiende el modelo tipado de
[landings.ts](../../../../../src/frontend/terrenario-web/src/content/landings.ts) y el componente
[ContentLandingPage.tsx](../../../../../src/frontend/terrenario-web/src/components/marketing/ContentLandingPage.tsx),
sin tocar el mecanismo de pre-renderizado: el script ya deriva la ruta de salida, el `sitemap.xml` y
el `robots.txt` del propio array, así que un clúster nuevo se propaga solo.

La restricción que gobierna el diseño es que la página se sirve **sin JavaScript** (`ADR-0012`) y
**sin recursos de terceros** (`RN-042`): la media tiene que funcionar con HTML plano y estar
autoalojada.

### Estado comprobado a 2026-10-07

- `LandingCluster` incluye `guia`; el pre-render genera `/guias/{slug}/index.html` y el sitemap se
  construye desde `LANDING_CONTENTS`.
- Hay una guía implementada: `/guias/como-empezar-en-terrenario`, con pasos editoriales y cinco
  capturas locales.
- `LandingBullet` admite actualmente `image?: { src, alt, width, height }`. En guías se renderiza
  una lista ordenada sin tarjetas, con el texto y la imagen alternados.
- No hay tipo `LandingVideo`, componente `<video>` ni fuentes/captions; tampoco se añadió
  `media-src` a la CSP. No afirmar que se admiten vídeos hasta implementar y validar esa capacidad.
- Los PNG de la guía están en `public/landings/como-empezar-en-terrenario/`; el build los copia a
  `dist/landings/` y las dos capturas que exponían nombres fueron anonimizadas en las copias
  públicas, conservando los originales de origen.

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
| ---------- | -------------- | ----------- |
| `src/content/landings.ts` | implementado | Clúster `guia`; `LandingBullet.image` opcional; guía de inicio y 3 guías futuras con brief |
| `src/components/marketing/ContentLandingPage.tsx` | implementado | Render editorial para guías y soporte de imágenes; sin soporte de vídeo |
| `src/components/marketing/LandingPage.tsx` | implementado | Grupo de Ayuda y ancla en el hub de portada |
| `scripts/prerenderizar-landings.mjs` | verificado | Emite ruta `/guias/{slug}/` desde `content.path` y actualiza el sitemap |
| `vite.config.ts` | sin cambio necesario para imágenes | Imágenes servidas desde el mismo origen; no se permite media externa |
| `src/test/sin-recursos-externos.test.ts` | verificar | Debe seguir bloqueando media externa |
| `public/landings/` | nuevo | Capturas y vídeos autoalojados |
| `docs/04-ingenieria/estandares-codigo.md` | modificado | Reglas de media en landings |

## Diseño detallado

### Modelo de contenido

```ts
export type LandingCluster = 'funcionalidad' | 'perfil' | 'guia';

export interface LandingImage {
  kind: 'image';
  /** Ruta absoluta dentro de `public/`, nunca una URL externa. */
  src: string;
  alt: string;
  width: number;
  height: number;
  caption?: string;
}

export interface LandingVideo {
  kind: 'video';
  /** Fuentes autoalojadas; se declara webm y mp4 para cobertura de navegadores. */
  sources: { src: string; type: 'video/webm' | 'video/mp4' }[];
  poster: string;
  /** Descripción para quien no puede ver el vídeo; también es el contenido de respaldo. */
  description: string;
  width: number;
  height: number;
  captions?: string;
  caption?: string;
}

export type LandingMedia = LandingImage | LandingVideo;

export interface LandingStep {
  order: number;
  title: string;
  text: string;
  media?: LandingMedia;
}
```

**Implementación actual:** cada paso editorial es un elemento de `bullets`; `LandingBullet` tiene
un `image` opcional con ruta, texto alternativo y dimensiones. No existe todavía `LandingStep` ni
un tipo union de media. Mantener este modelo sencillo mientras solo se necesiten capturas.

**Propuesta futura:** introducir un tipo de vídeo solo si una guía real lo necesita y se aprueban
el presupuesto de hosting, formato, subtítulos, CSP y privacidad. No añadir una abstracción de vídeo
por anticipado.

### Render

- **Imagen**: `<img src loading="lazy" decoding="async" width height alt>` dentro de `<figure>`,
  con `<figcaption>` cuando hay `caption`. `width` y `height` son obligatorios para reservar espacio
  y no degradar CLS.
- **Vídeo**: no implementado. La propuesta de `<video controls preload="none">` de este diseño no
  es un contrato disponible y no debe usarse en contenido hasta completar su implementación.
- **Pasos**: lista ordenada `<ol>` para que la secuencia sea semántica y sea candidata a `HowTo` en
  datos estructurados (fuera del alcance de esta historia; se evalúa en una revisión posterior).

Todo es HTML estático: sobrevive a `renderToStaticMarkup` sin estado ni hooks.

### Seguridad de contenido (CSP)

La política actual, construida en
[vite.config.ts](../../../../../src/frontend/terrenario-web/vite.config.ts), declara imágenes del mismo origen.
Las capturas funcionan sin cambio de CSP. Como vídeo no está implementado, no se añadió
`media-src`; si se aprueba vídeo autoalojado, debe añadirse explícitamente antes de desplegarlo.

El backend sirve la política desde
[SecurityHeadersMiddleware.cs](../../../../../src/backend/Terrenario.Api/Common/Http/SecurityHeadersMiddleware.cs),
que la lee del artefacto del build: no hay que tocar el backend.

### Peso y presupuesto

[peso-primera-carga.mjs](../../../../../src/frontend/terrenario-web/scripts/peso-primera-carga.mjs) mide
`index.html`, `dist/assets` y las tipografías, y **excluye a propósito lo que se copia desde
`public/`**, que se informa aparte. Por tanto la media de las landings no rompe el build, pero
tampoco está vigilada. Se fija un presupuesto editorial, verificable en la revisión del brief:

| Recurso | Límite por landing |
| ------- | ------------------ |
| Imágenes | 6 como máximo, ≤ 150 kB cada una; usar WebP cuando no perjudique la legibilidad de la captura |
| Vídeo | Propuesta no implementada: límite a ratificar en el diseño técnico específico |
| Póster de vídeo | ≤ 100 kB |

El vídeo no penaliza la primera carga porque `preload="none"` impide que se descargue hasta que el
usuario lo pide. Las imágenes van con `loading="lazy"` salvo la primera visible.

### Privacidad de las capturas

Las capturas y los vídeos se graban sobre un Workspace de demostración con datos sintéticos. No
pueden aparecer nombres de personas o explotaciones reales, correos, referencias catastrales ni
cifras de producción de una explotación real
([privacidad-datos.md](../../../../07-seguridad/privacidad-datos.md)). La verificación
es un punto del checklist del brief y del code review.

### Convención de assets

```text
public/landings/{slug}/{nombre-descriptivo}.webp
public/landings/{slug}/{nombre-descriptivo}.png
```

Nombres en kebab-case y descriptivos del contenido, no del orden: si se reordena la guía, el
fichero no queda mintiendo.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
| ----------- | ------------------- |
| Incrustar YouTube o Vimeo | Prohibido por `RN-042` y por `ADR-0011`: introduce recursos y seguimiento de terceros |
| Servir la media desde un CDN externo | Mismo motivo, y lo bloquea el test `sin-recursos-externos` |
| Imágenes como data URL en el TypeScript | Infla el bundle y el HTML pre-renderizado, y entra en el presupuesto de primera carga |
| GIF animado en vez de vídeo | Peor compresión y sin control de reproducción |
| Reutilizar `/funcionalidades/` para las guías | Mezcla intención informacional y comercial en la misma ruta; canibaliza y confunde el clúster |
| Añadir un CMS para las guías | Rompe el pre-renderizado sin JavaScript y añade infraestructura para 4 páginas |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
| ------ | ------------ | ---------- |
| La media se desactualiza cuando cambia la interfaz | alta | Cada brief registra qué pantalla muestra; la revisión de una pantalla obliga a revisar sus capturas |
| Capturas con datos reales | media | Workspace de demostración obligatorio + punto de checklist en brief y code review |
| El peso de `public/` crece sin control | media | Presupuesto editorial por landing, verificado en el brief |
| La guía canibaliza a su landing comercial | media | Reparto explícito de intención y keyword en la sección 3 del brief |
| El script de pre-render no contempla el clúster nuevo | baja | Se verifica antes de redactar; si la ruta está fijada, se parametriza |

## Plan de testing

- [ ] Unitarios (`landings.test.ts`): las rutas del clúster `guia` cuelgan de `/guias/`; ningún
      `src` de media apunta fuera de `public/`; toda imagen tiene `alt`, `width` y `height`.
- [ ] Componente (`ContentLandingPage.test.tsx`): se renderiza `<figure>` por imagen, `<video>` con
      `preload="none"` y sin `autoplay`, y `<ol>` por bloque de pasos.
- [ ] Pre-render (`prerenderizar-landings.test.ts`): se emite `dist/guias/{slug}/index.html` y las
      guías entran en `sitemap.xml`.
- [ ] Recursos externos (`sin-recursos-externos.test.ts`): sigue en verde con la media añadida.
- [x] CSP: las capturas de mismo origen funcionan con la directiva de imágenes actual; no hay vídeo.
- [ ] Existencia de assets: todo `src` declarado en `landings.ts` existe en `public/` en tiempo de
      build.

## Checklist de implementación

- [ ] Tipos de media y de pasos añadidos a `landings.ts`
- [x] Clúster `guia` soportado en render, navegación y pre-render
- [x] Imágenes autoalojadas renderizadas con `alt`, `width` y `height`
- [ ] Vídeo autoalojado: pendiente de decisión e implementación; no usar todavía
- [ ] Convención `public/landings/{slug}/` documentada en `estandares-codigo.md`
- [ ] Presupuesto de media documentado y verificado
- [ ] Tests nuevos en verde y `npm run build` sin regresión de peso
