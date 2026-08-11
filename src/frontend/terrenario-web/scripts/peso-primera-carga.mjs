/**
 * MVP-810 — Presupuesto de peso de la primera carga.
 *
 * **Por qué existe.** El subconjunto de iconos quita 3,7 MB de golpe, pero eso solo dura hasta la
 * siguiente dependencia que entre sin que nadie mire la báscula: `P-115` no apareció porque alguien
 * decidiera servir 5,57 MB, sino porque nadie lo estaba midiendo. Esto lo mide en cada `build` y
 * **rompe el build** al pasarse.
 *
 * **Qué cuenta como «primera carga».** Lo que el navegador pide *antes* de que la aplicación sea
 * utilizable, y que sale del build:
 *
 *   - `index.html`.
 *   - Todo el JavaScript y el CSS de `dist/assets`. Hoy no hay rutas diferidas —dividir el bundle
 *     está explícitamente fuera del alcance de esta historia—, así que todos los trozos entran.
 *   - Las tipografías que el navegador **va a pedir de verdad**, deducidas de los `@font-face` que
 *     el propio build emite: la variante `woff2` de cada familia cuyo `unicode-range` cubra el
 *     latín básico, o que no declare rango (el caso de los iconos).
 *
 * **Qué no cuenta, y por qué.** Lo que se copia tal cual desde `public/` —favicon, manifest, la
 * fotografía de la portada y la imagen social—: no lo produce el build, no lo gobierna esta
 * historia y mezclarlo haría que el presupuesto subiera o bajara por motivos que no son de código.
 * Se informa aparte para que se vea, no para que se olvide.
 *
 * También quedan fuera los `.woff` de reserva: `@fontsource` declara cada tipografía dos veces,
 * `woff2` primero y `woff` después, y ningún navegador que pueda ejecutar esta aplicación pide el
 * segundo. Están en `dist` pero no se descargan; el presupuesto de primera carga mide descargas.
 */
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join } from 'node:path';

/**
 * Umbrales, en bytes **sin comprimir**.
 *
 * Sin comprimir a propósito: es la medida pesimista y no depende de si quien sirve el estático tiene
 * la compresión activada, así que no cambia de valor por un ajuste de infraestructura.
 *
 * Se fijan **con la medida delante** (2026-08-11, ver el `tech-design.md` de `MVP-810`) dejando en
 * torno a un 14 % de margen sobre lo medido: bastante para que un puñado de pantallas o una
 * dependencia mediana no lo conviertan en un trámite semanal, y muy poco para lo que de verdad
 * vigila —volver a servir la fuente de iconos entera son 3,70 MB, casi cuatro veces el umbral—.
 *
 * Subirlos es una decisión consciente que se argumenta en el PR: si el número sube, alguien tiene
 * que decir por qué.
 */
export const UMBRALES = {
  /** Lo que el navegador descarga para arrancar. Medido en `MVP-810`: 877,9 kB. */
  primeraCarga: 1_000_000,
  /** Todo lo que el build publica en `dist/assets`. Medido en `MVP-810`: 1.224,0 kB. */
  totalAssets: 1_400_000,
};

/** El latín básico y su suplemento: lo que necesita cualquier pantalla del producto, en español. */
const LATIN_BASICO = [0x20, 0xff];

function ficheros(directorio) {
  return readdirSync(directorio, { withFileTypes: true }).flatMap((entrada) => {
    const ruta = join(directorio, entrada.name);
    return entrada.isDirectory() ? ficheros(ruta) : [ruta];
  });
}

/**
 * `U+0000-00FF,U+0131,U+2000-206F` -> ¿toca el latín básico?
 *
 * Hay que contar con la forma con comodines (`U+??`, `U+4??`): el minificador de CSS reescribe
 * `U+0000-00FF` como `U+??`, y leerlo mal fue justo lo que dejó las tipografías de texto a cero en
 * la primera versión de esta medida.
 */
function cubreLatinBasico(rango) {
  return rango.split(',').some((tramo) => {
    const limpio = tramo.trim().replace(/^U\+/i, '');
    const [desde, hasta] = limpio.includes('-')
      ? limpio.split('-')
      : [limpio.replace(/\?/g, '0'), limpio.replace(/\?/g, 'F')];
    const inicio = parseInt(desde, 16);
    const fin = parseInt(hasta, 16);
    return !Number.isNaN(inicio) && !Number.isNaN(fin) && inicio <= LATIN_BASICO[1] && fin >= LATIN_BASICO[0];
  });
}

/**
 * Ficheros de tipografía que el navegador pedirá, leídos de los `@font-face` que emite el build.
 * Se deduce del CSS publicado y no de una lista escrita a mano: una tipografía nueva entra sola.
 */
function tipografiasDePrimeraCarga(css) {
  const pedidas = new Set();
  for (const bloque of css.matchAll(/@font-face\s*\{([^}]*)\}/g)) {
    const cuerpo = bloque[1];
    const rango = /unicode-range\s*:\s*([^;}]+)/i.exec(cuerpo);
    if (rango && !cubreLatinBasico(rango[1])) continue;
    for (const fuente of cuerpo.matchAll(/url\(\s*["']?([^"')]+\.woff2)["']?\s*\)/gi)) {
      pedidas.add(fuente[1].split('/').pop());
    }
  }
  return pedidas;
}

/**
 * Mide el `dist` indicado. Devuelve los dos totales y el desglose por tipo de recurso, que es lo
 * que piden `CA-1` y `CA-4` de `MVP-810`.
 */
export function medirPrimeraCarga(dist) {
  const directorioAssets = join(dist, 'assets');
  const assets = ficheros(directorioAssets).map((ruta) => ({
    nombre: ruta.split(/[\\/]/).pop(),
    extension: extname(ruta).slice(1),
    bytes: statSync(ruta).size,
  }));

  const css = assets
    .filter((a) => a.extension === 'css')
    .map((a) => readFileSync(join(directorioAssets, a.nombre), 'utf8'))
    .join('\n');
  const tipografiasPedidas = tipografiasDePrimeraCarga(css);

  const documento = statSync(join(dist, 'index.html')).size;
  const desglose = {
    'Documento (index.html)': documento,
    JavaScript: 0,
    CSS: 0,
    'Iconos (Material Symbols)': 0,
    'Tipografías de texto': 0,
  };
  const noDescargado = { 'Tipografías no pedidas (otros alfabetos y reserva .woff)': 0 };

  for (const asset of assets) {
    if (asset.extension === 'js') desglose.JavaScript += asset.bytes;
    else if (asset.extension === 'css') desglose.CSS += asset.bytes;
    else if (!tipografiasPedidas.has(asset.nombre)) {
      noDescargado['Tipografías no pedidas (otros alfabetos y reserva .woff)'] += asset.bytes;
    } else if (/material-symbols/.test(asset.nombre)) {
      desglose['Iconos (Material Symbols)'] += asset.bytes;
    } else {
      desglose['Tipografías de texto'] += asset.bytes;
    }
  }

  const primeraCarga = Object.values(desglose).reduce((a, b) => a + b, 0);
  const totalAssets = assets.reduce((a, b) => a + b.bytes, 0);

  // Lo que se copia tal cual desde `public/`: fuera del presupuesto, pero a la vista.
  const copiados = ficheros(dist)
    .filter((ruta) => !ruta.includes(`${join(dist, 'assets')}`) && !ruta.endsWith('index.html'))
    .reduce((total, ruta) => total + statSync(ruta).size, 0);

  return { desglose, noDescargado, primeraCarga, totalAssets, copiados, assets };
}

const kB = (bytes) => `${(bytes / 1000).toFixed(1)} kB`;

export function informe(medida) {
  const filas = [
    ...Object.entries(medida.desglose).map(([k, v]) => `  ${k.padEnd(48)} ${kB(v).padStart(11)}`),
    `  ${'PRIMERA CARGA'.padEnd(48)} ${kB(medida.primeraCarga).padStart(11)}`,
    '',
    ...Object.entries(medida.noDescargado).map(([k, v]) => `  ${k.padEnd(48)} ${kB(v).padStart(11)}`),
    `  ${'TOTAL dist/assets'.padEnd(48)} ${kB(medida.totalAssets).padStart(11)}`,
    `  ${'Copiado de public/ (fuera del presupuesto)'.padEnd(48)} ${kB(medida.copiados).padStart(11)}`,
  ];
  return `[MVP-810] Peso de la primera carga\n${filas.join('\n')}`;
}

/**
 * Comprueba los dos umbrales. Devuelve la lista de incumplimientos: vacía si todo cabe.
 */
export function comprobarUmbrales(medida, umbrales = UMBRALES) {
  const incumplimientos = [];
  const revisar = (etiqueta, valor, umbral, pista) => {
    if (valor > umbral) {
      incumplimientos.push(
        `${etiqueta}: ${kB(valor)} supera el umbral de ${kB(umbral)} ` +
          `(sobran ${kB(valor - umbral)}). ${pista}`
      );
    }
  };
  revisar(
    'Primera carga',
    medida.primeraCarga,
    umbrales.primeraCarga,
    'Es lo que descarga alguien que abre la aplicación por primera vez, y el usuario objetivo ' +
      'trabaja con mala cobertura (RT-01, MVP-709).'
  );
  revisar(
    'Total de dist/assets',
    medida.totalAssets,
    umbrales.totalAssets,
    'Aunque no todo se descargue en la primera visita, es lo que hay que publicar y mantener.'
  );
  return incumplimientos;
}
