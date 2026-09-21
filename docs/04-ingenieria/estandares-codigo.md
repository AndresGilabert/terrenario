---
bloque: 04-ingenieria
documento: estandares-codigo
actualizado_en: "2026-09-20"
---

# Estándares de Código

> Estas convenciones son obligatorias para todo el código del proyecto.
> Los agentes de IA deben leer este documento antes de generar código.

---

## Principios generales

1. **Legibilidad sobre brevedad**: el código se lee más veces de las que se escribe
2. **Nombres descriptivos**: variables, funciones y clases deben revelar su intenciÃ³n
3. **Funciones pequeñas**: una función, una responsabilidad
4. **Sin comentarios de "qué"**: el código debe ser autoexplicativo; los comentarios explican el "por qué"

---

## Idioma de los identificadores

> Decisión completa y alternativas descartadas: [ADR-0009](../02-arquitectura/decisiones/ADR-0009--idioma-de-identificadores-en-codigo.md).

**Todo identificador del sistema se escribe en inglés.** La documentación se redacta en español,
pero **nunca traduce identificadores**: los cita literalmente en el idioma del código.

Redacción correcta en documentación:

> El login del usuario guarda el nombre visible en la columna `display_name` de la tabla `users`.

Aplica a: clases, interfaces, propiedades, variables, funciones, nombres de archivo, tablas y
columnas de base de datos, rutas de API, campos de request/response, códigos de error y nombres
de eventos.

**No aplica a**:

- Textos de interfaz y mensajes dirigidos al usuario final, que van en español por ser contenido.
- Los **valores** de los catálogos cerrados del dominio (`desconocido`, `venta_aceituna`,
  `planificada`, `invitado`...), que son vocabulario de negocio. El **nombre** del catálogo sí es
  un identificador y va en inglés (`harvest_destination`).

---

## Convenciones de naming

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Variables y funciones | camelCase inglés | `getPlotById` |
| Clases e interfaces | PascalCase inglés | `HarvestRecord` |
| Constantes | SCREAMING_SNAKE_CASE inglés | `MAX_RETRY_ATTEMPTS` |
| Archivos | kebab-case inglés | `harvest-record.service.cs` |
| Tablas de DB | snake_case plural inglés | `harvest_records` |
| Columnas de DB | snake_case inglés | `created_at`, `is_active` |
| Rutas de API | kebab-case plural inglés | `/api/v1/workspaces/active` |
| Códigos de error | SCREAMING_SNAKE_CASE inglés | `VALIDATION_REQUIRED_WORKSPACE_NAME` |

Los booleanos persistidos usan prefijo `is_` (`is_active`, `is_closed`).

---

## Linting y formateo

| Herramienta | Propósito | Configuración |
|------------|-----------|--------------|
| Roslyn Analyzers | Linting/analizador estático .NET | `.editorconfig` + reglas del proyecto |
| dotnet format | Formateo automático | `.editorconfig` |

**El CI falla si hay errores de linting o formateo.**

---

## Estructura de un módulo / servicio

```text
src/
├── {modulo}/
│   ├── domain/           # Entidades, value objects, reglas
│   ├── application/      # Casos de uso, comandos, queries
│   ├── infrastructure/   # Adaptadores, repositorios, migraciones
│   └── interfaces/       # Controllers, DTOs, mappers
```

---

## Manejo de errores

- Usar errores tipados, nunca `throw new Exception("mensaje genérico")` sin clasificar
- Nunca capturar y silenciar excepciones
- Los errores de dominio se propagan como excepciones de dominio
- Los errores de infraestructura se loguean y se transforman en errores de aplicación

---

## Seguridad en el código

- **Nunca** incluir credenciales, tokens o secrets en el cÃ³digo o en los tests
- Validar todas las entradas en los límites del sistema (controllers / API handlers)
- Usar consultas parametrizadas; **nunca** concatenar SQL
- Ver modelo de seguridad completo en `../07-seguridad/modelo-seguridad.md`

---

## Code smells a evitar

- Números mágicos sin nombre de constante
- Clases con más de 300 líneas
- Funciones con más de 3 parámetros (usar objetos)
- Condicionales anidados de más de 2 niveles
- Lógica de negocio en controllers o repositorios

---

## Iconos del cliente web (Material Symbols)

> Introducido en `MVP-810`. Detalle y alternativas descartadas:
> [tech-design de MVP-810](../09-desarrollos/epicas/MVP-008--ajustes-mvp-02/MVP-810--peso-de-la-primera-carga/tech-design.md).

**La fuente de iconos no se sirve entera.** El `build` genera un subconjunto con exactamente los
glifos que encuentra en el código: 75 iconos y 74 kB, frente a los 3,78 MB del catálogo completo,
que era el 82 % de lo que descargaba la primera visita.

La consecuencia práctica: **un icono que el inventario no vea no se descarga**, y en pantalla
quedaría un hueco. Para que eso no pase, el nombre del icono se escribe **siempre como una cadena
literal** en una de estas tres formas:

```tsx
<span className="material-symbols-outlined">agriculture</span>
<span className="material-symbols-outlined">{activo ? 'toggle_on' : 'toggle_off'}</span>
{ label: 'Cosechas', icon: 'agriculture' }   // o icon="agriculture" como atributo
```

Lo que **no** vale es que el nombre llegue por una vía que no se pueda leer en el código
(`<span className="material-symbols-outlined">{nombre}</span>` con `nombre` viniendo de una
variable que no se llame `icon`). No hace falta recordarlo: hay dos guardas y ninguna deja el fallo
para producción.

| Qué pasa | Quién lo detecta | Cuándo |
|---|---|---|
| Un `<span>` de iconos cuyo nombre no se puede deducir del código | `src/test/inventario-iconos.test.ts` | `npm test` y CI |
| Un nombre que no existe en Material Symbols Outlined (errata incluida) | `scripts/subconjunto-iconos.mjs` | `npm run build` |
| Un icono que el subconjunto no pinte igual que la fuente completa | `scripts/subconjunto-iconos.mjs` | `npm run build` |

**Añadir un icono nuevo no requiere ningún paso extra**: se escribe como arriba y el siguiente
`build` lo incluye. Con el servidor de desarrollo ya arrancado hay que **reiniciarlo**, porque el
inventario se lee al arrancar.

El proceso vive en `src/frontend/terrenario-web/scripts/`: `inventario-iconos.mjs` (qué iconos usa
el producto) y `subconjunto-iconos.mjs` (el recorte). Los dos están comentados con el porqué.

---

## Landings públicas (contenido de marketing)

> `MKT-102`/`103`/`104`/`105`. Detalle de arquitectura en
> [ADR-0012](../02-arquitectura/decisiones/ADR-0012--prerenderizado-estatico-landings-publicas-mkt-102.md)
> y en el tech-design de
> [MKT-102](../09-desarrollos/epicas/MKT-100--posicionamiento-organico-inicial/MKT-102--landings-publicas-p0-de-funcionalidades-y-casos/tech-design.md).
> Esto es el «cómo, en la práctica»; para verlas renderizadas en local ver
> [`desarrollo-local.md`](../05-infraestructura/desarrollo-local.md).

**Fuente única**: `src/frontend/terrenario-web/src/content/landings.ts`. El array
`LANDING_CONTENTS` es de donde salen, sin ningún otro sitio que tocar, las rutas servidas
(`prerenderizar-landings.mjs`), el `sitemap.xml`, y las exclusiones de `robots.txt` de todo lo que
no sea público. No hay YAML, CMS ni fichero por landing: es contenido en código, a propósito (sin
base de datos ni panel de edición para diez páginas fijas).

### Añadir una landing nueva

1. Añade una entrada a `LANDING_CONTENTS` con todos los campos de `LandingContent`: `slug`, `path`
   (sin barra final — es la forma que declara el propio `canonical`), `cluster`
   (`'funcionalidad'` → `/funcionalidades/{slug}`, `'perfil'` → `/para/{slug}`), `navLabel`,
   `title`, `metaDescription`, `eyebrow`, `h1`, `intro`, `bullets`, `faqs` y `relatedSlugs`.
   `sections` y `finalCta` son opcionales, pero obligatorios cuando la intención necesite desarrollar
   problema, solución y beneficios más allá del resumen inicial.
2. `relatedSlugs` tiene que apuntar a slugs que existan y **no puede incluirse a sí misma**
   (`landings.test.ts` lo comprueba); toda landing necesita al menos una relacionada (CA-2 de
   `MKT-102`).
3. Los iconos de `bullets` son nombres de Material Symbols Outlined, con las mismas reglas que
   [«Iconos del cliente web»](#iconos-del-cliente-web-material-symbols) de más arriba.
4. **Actualiza `src/content/landings.test.ts`**: el test `publica exactamente las 10 URLs del
   plan P0` fija la lista cerrada de rutas del plan inicial y **falla a propósito** si añades una
   landing sin tocarlo — es la barrera contra publicar una página sin que nadie se entere.
5. `title` y `metaDescription` no pueden repetirse entre landings (ni con la home): otro test lo
   comprueba.
6. Construye para verla en su forma final:

   ```bash
   cd src/frontend/terrenario-web
   npm run build   # genera dist/, incluida la landing nueva, el sitemap y robots.txt
   npm test        # valida lo del punto 2, 4 y 5
   ```

   Con `wwwroot` ya enlazado (`desarrollo-local.md`), `dotnet run` la sirve tal cual en
   `http://localhost:5127{path}`.

### Requisitos de contenido y captación

Antes de redactar se define **una intención de búsqueda principal** y la persona a la que responde.
`title`, `metaDescription`, `eyebrow`, `h1`, introducción, secciones y FAQ deben sostener esa misma
intención; repetir palabras clave sin aportar información no cuenta como contenido útil.

Una landing orientada a captación incluye, como mínimo:

1. Hero con un único `h1`, propuesta concreta, introducción y CTA a `/login`.
2. Explicación del problema con situaciones verificables de la persona objetivo.
3. Capacidades reales del producto, descritas con el vocabulario y las unidades del dominio.
4. Beneficios derivados directamente de esas capacidades, sin prometer resultados no medidos.
5. FAQ visible que responda dudas reales y sea la misma fuente del `FAQPage` JSON-LD.
6. Enlaces a funcionalidades relacionadas y un CTA final contextual.

Las afirmaciones se contrastan con las reglas de negocio, las fichas de módulo y el código
entregado. No se publican como hechos:

- precios, gratuidad, periodos de prueba o un `Offer` JSON-LD sin una decisión comercial vigente;
- plataformas no existentes (por ejemplo, Android/iOS si el producto es una aplicación web);
- resultados garantizados («aumenta la rentabilidad», «maximiza la producción») sin evidencia;
- testimonios, cifras de adopción, premios o autoría («diseñado por agricultores») no documentados;
- capacidades futuras o fuera de alcance presentadas como disponibles.

Cuando marketing use un término próximo pero no idéntico al dominio, el texto debe explicar la
equivalencia. Ejemplo: Terrenario usa «rendimiento de aceite» en `L/100kg`; «rendimiento graso» puede
ser una entrada en `kg/100kg`, pero no debe sugerir que la aplicación realiza un análisis químico.

### Requisitos técnicos de SEO

- El HTML se pre-renderiza y contiene todo el contenido principal sin ejecutar JavaScript.
- Cada URL publica `title`, meta description, canonical, `hreflang="es-ES"` y un único `h1`.
- Open Graph y Twitter reutilizan título, descripción y URL de la misma fuente editorial.
- Las FAQ visibles y el `FAQPage` estructurado no pueden divergir.
- `SoftwareApplication` declara únicamente `operatingSystem: Web`; no incluye `Offer` mientras no
   exista precio documentado.
- No se añade `meta keywords`: los buscadores principales no la usan para ranking.
- No hace falta `meta robots="index, follow"` en páginas indexables: es el comportamiento por
   defecto. Una futura página `noindex` debe declararlo explícitamente y quedar fuera del sitemap.
- Los activos deben ser relevantes para la intención, autoalojados y compatibles con la CSP. Una
   fotografía agrícola genérica no se reutiliza como imagen principal de una landing especializada
   si no representa el cultivo descrito.
- `robots.txt`, `sitemap.xml`, canonical, `hreflang` y JSON-LD se generan en
   `scripts/prerenderizar-landings.mjs`; no se duplican a mano en el componente.

### Lista de revisión de una landing

- [ ] La intención principal y la persona objetivo están escritas antes del copy.
- [ ] Cada afirmación tiene respaldo en KB/código o está formulada como posibilidad, no garantía.
- [ ] Hero, problema, capacidades, beneficios, FAQ, relacionados y CTA forman un recorrido coherente.
- [ ] Hay un solo `h1`; los bloques usan `h2` y sus ítems `h3`.
- [ ] El CTA apunta a `/login` y no promete precio o prueba no documentados.
- [ ] Título, description, canonical, `hreflang`, Open Graph, Twitter y JSON-LD se verifican en el HTML generado.
- [ ] El contenido visible de FAQ coincide con el `FAQPage` estructurado.
- [ ] Los activos son pertinentes, autoalojados, accesibles y no rompen la CSP ni el presupuesto de peso.
- [ ] `npm test` y `npm run build` pasan; se inspecciona el HTML de `dist/{path}/index.html`.

### Editar una landing existente

Cambia el campo que corresponda en su entrada de `LANDING_CONTENTS` (texto, `bullets`, `faqs`,
`relatedSlugs`…) y repite el paso 6. No hay que tocar `ContentLandingPage.tsx` ni el script de
pre-renderizado para un cambio de contenido: ese componente y ese script son genéricos y leen
`LandingContent` tal cual se declare.

### Qué no cambiar sin revisar el ADR

- El **layout** (`ContentLandingPage.tsx` para `/funcionalidades/*` y `/para/*`,
  `LandingPage.tsx` para la home) no lleva JavaScript de la SPA a propósito
  (`ADR-0012`): nada de `react-router`, nada de estado. Un enlace con `<Link>` en vez de `<a>` rompe
  el pre-renderizado.
- El pre-renderizado (`scripts/prerenderizar-landings.mjs`) no es una plantilla paralela: reescribe
  `dist/index.html` ya construido. No dupliques la cabecera (iconos, manifest, CSP) a mano en el
  script.

---

## Presupuesto de peso de la primera carga

> Introducido en `MVP-810`, a raíz de `P-115`.

El `build` del cliente **falla** si el peso de la primera carga o el total de `dist/assets` superan
el umbral fijado en `src/frontend/terrenario-web/scripts/peso-primera-carga.mjs`. Cada `build`
imprime el desglose por tipo de recurso, y `npm run peso` lo vuelve a sacar sin reconstruir.

Existe porque `P-115` no apareció por una decisión de servir 5,57 MB, sino porque nadie estaba
midiendo. Si un cambio justificado sube el peso, **se sube el umbral y se explica en el PR**; lo que
no vale es que suba sin que nadie lo diga.
