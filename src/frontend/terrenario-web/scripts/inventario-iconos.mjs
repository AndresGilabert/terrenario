/**
 * MVP-810 — Inventario de los glifos de Material Symbols que el producto usa de verdad.
 *
 * ---------------------------------------------------------------------------------------------
 * SI VIENES A AÑADIR UN ICONO NUEVO, LEE ESTO
 * ---------------------------------------------------------------------------------------------
 * La fuente de iconos **no se sirve entera**: el build genera un subconjunto con exactamente los
 * glifos que este fichero encuentra en el código (`scripts/subconjunto-iconos.mjs`). Un icono que
 * el inventario no vea no se descarga, y el síntoma sería un hueco en blanco que no apunta a la
 * causa. Para que eso no pase, el inventario **no es una lista que haya que mantener a mano**: se
 * deduce del código, y lo que no se puede deducir **rompe el build o el test**, nunca se pinta mal.
 *
 * Escribe el nombre del icono como una **cadena literal** en una de estas tres formas:
 *
 *   1. Contenido del propio `<span>`:
 *        <span className="material-symbols-outlined">agriculture</span>
 *   2. Cadena dentro de la expresión del `<span>`:
 *        <span className="material-symbols-outlined">{activo ? 'toggle_on' : 'toggle_off'}</span>
 *   3. Valor de una propiedad o atributo llamado `icon`:
 *        icon="agriculture"      { label: 'Cosechas', icon: 'agriculture' }
 *
 * Si necesitas una cuarta forma, añádela aquí y al test; no la uses sin más, porque el test
 * `iconos-inventario.test.ts` falla en cuanto un `<span>` de iconos contiene algo que este
 * inventario no sabe resolver.
 * ---------------------------------------------------------------------------------------------
 *
 * Se comprueba sobre el **código fuente** y no en el navegador, por el mismo motivo que
 * `sin-recursos-externos.test.ts`: así falla al escribirlo y no al desplegarlo.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), '..');

/** La clase que marca un `<span>` como icono. Es la misma que define `src/index.css`. */
export const CLASE_ICONO = 'material-symbols-outlined';

/**
 * Un `<span>` de iconos, con su contenido tal cual aparece en el código.
 * `contenido` es lo que va entre `>` y `</span>`, sin recortar la expresión.
 */
const PATRON_SPAN = new RegExp(`<span[^>]*${CLASE_ICONO}[^>]*>([\\s\\S]*?)</span>`, 'g');

/** `icon="x"`, `icon: 'x'`, `icon={cond ? 'x' : 'y'}` — el nombre siempre es una cadena literal. */
const PATRON_PROP_ICONO = /\bicon\s*[:=]\s*(\{[^}]*\}|"[^"]*"|'[^']*')/g;

/** Cadenas literales dentro de un fragmento de código. */
const PATRON_CADENA = /'([^']*)'|"([^"]*)"|`([^`$]*)`/g;

/** Los nombres de Material Symbols son minúsculas, dígitos y guion bajo. */
const PATRON_NOMBRE = /^[a-z0-9_]+$/;

function ficherosDeCodigo(directorio) {
  return readdirSync(directorio).flatMap((entrada) => {
    const ruta = join(directorio, entrada);
    if (statSync(ruta).isDirectory()) return ficherosDeCodigo(ruta);
    return /\.tsx?$/.test(entrada) && !/\.test\.tsx?$/.test(entrada) ? [ruta] : [];
  });
}

function* cadenasLiterales(fragmento) {
  for (const coincidencia of fragmento.matchAll(PATRON_CADENA)) {
    yield coincidencia[1] ?? coincidencia[2] ?? coincidencia[3];
  }
}

/**
 * Recorre el código y devuelve, por fichero, cada `<span>` de iconos con los nombres que se pueden
 * deducir de él. `resoluble` distingue «sé qué glifos puede pintar» de «aquí no puedo saberlo»,
 * que es justo lo que el test convierte en fallo.
 */
export function spansDeIconos(raiz = RAIZ) {
  const spans = [];
  for (const ruta of ficherosDeCodigo(join(raiz, 'src'))) {
    const texto = readFileSync(ruta, 'utf8');
    for (const coincidencia of texto.matchAll(PATRON_SPAN)) {
      const contenido = coincidencia[1].trim();
      const linea = texto.slice(0, coincidencia.index).split('\n').length;
      const nombres = [];
      let resoluble;

      if (PATRON_NOMBRE.test(contenido)) {
        // Forma 1: el nombre está escrito tal cual.
        nombres.push(contenido);
        resoluble = true;
      } else {
        // Forma 2: cadenas dentro de la expresión. Si además la expresión lee una propiedad
        // `icon`, los nombres los aporta la forma 3 desde donde se rellena esa propiedad.
        nombres.push(...[...cadenasLiterales(contenido)].filter((c) => PATRON_NOMBRE.test(c)));
        resoluble = nombres.length > 0 || /\bicon\b/i.test(contenido);
      }

      spans.push({
        ruta: ruta.slice(raiz.length + 1).replace(/\\/g, '/'),
        linea,
        contenido: contenido.replace(/\s+/g, ' '),
        nombres,
        resoluble,
      });
    }
  }
  return spans;
}

/**
 * Nombres de glifo que el producto usa, ordenados. Es la entrada del subconjunto.
 *
 * Nota sobre el sobre-inventario: una propiedad `icon` que no sea un icono metería aquí un nombre
 * inexistente, y el generador **falla** en vez de ignorarlo (`subconjunto-iconos.mjs`). Es
 * deliberado: un nombre que no está en la fuente tampoco se pintaría en pantalla, así que es
 * siempre un error, y descubrirlo en el build es mejor que descubrirlo en producción.
 */
export function inventarioDeIconos(raiz = RAIZ) {
  const nombres = new Set();

  for (const span of spansDeIconos(raiz)) {
    for (const nombre of span.nombres) nombres.add(nombre);
  }

  for (const ruta of ficherosDeCodigo(join(raiz, 'src'))) {
    const texto = readFileSync(ruta, 'utf8');
    for (const coincidencia of texto.matchAll(PATRON_PROP_ICONO)) {
      for (const cadena of cadenasLiterales(coincidencia[1])) {
        if (PATRON_NOMBRE.test(cadena)) nombres.add(cadena);
      }
    }
  }

  return [...nombres].sort();
}
