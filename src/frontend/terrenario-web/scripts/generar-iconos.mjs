/**
 * MVP-710 — Generador de los recursos de marca rasterizados.
 *
 * NO forma parte de `npm run build`. Se ejecuta a mano cuando cambia el icono, y sus resultados
 * (`favicon.ico`, `apple-touch-icon.png`, `icon-*.png`, `og-image.png`) se versionan en `public/`.
 *
 * Sus dependencias (`sharp`, `@resvg/resvg-js`, `wawoff2`) **no estan en `package.json` a proposito**:
 * son ~50 MB de binarios nativos que cada `npm ci` del CI pagaria para producir unos ficheros que
 * cambian una vez al ano. El precio de esa decision es que el script no arranca solo; el porque queda
 * escrito aqui para que quien lo necesite sepa que le falta y no crea que esta roto:
 *
 *   npm i --no-save sharp @resvg/resvg-js wawoff2
 *   node scripts/generar-iconos.mjs
 *   npm i   # deja `package.json`/`package-lock.json` como estaban
 *
 * La fuente de verdad del dibujo es `public/favicon.svg`, no este fichero: aqui solo se rasteriza y
 * se componen las variantes. Asi el vector se puede editar a mano sin tocar codigo.
 */
import { Resvg } from '@resvg/resvg-js';
import { decompress } from 'wawoff2';
import sharp from 'sharp';
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const PUBLICO = path.join(RAIZ, 'public');

const OLIVA = '#33450d';
const CREMA = '#fcf9f4';
const TINTA = '#1c1c19';
const TEXTO_SUAVE = '#45483c';

/** Tinta del glifo `eco` en su rejilla original de 960: 678x683 con el centro en (458.5, -422). */
const GLIFO_ANCHO = 678;
const GLIFO_CENTRO_X = 458.5;
const GLIFO_CENTRO_Y = -422;

/**
 * Coloca el glifo `eco` centrado en (cx, cy) con `ancho` pixeles de tinta. El glifo no esta centrado
 * en su caja em, asi que centrarlo por la caja lo dejaria visiblemente alto y a la derecha.
 */
function transformarGlifo(cx, cy, ancho) {
  const escala = ancho / GLIFO_ANCHO;
  const tx = cx - GLIFO_CENTRO_X * escala;
  const ty = cy - GLIFO_CENTRO_Y * escala;
  return `translate(${tx.toFixed(3)} ${ty.toFixed(3)}) scale(${escala.toFixed(6)})`;
}

/** Rasteriza un SVG a PNG con resvg. `fuentes` solo hace falta cuando el SVG lleva texto. */
function rasterizar(svg, ancho, fuentes = []) {
  return new Resvg(svg, {
    fitTo: { mode: 'width', value: ancho },
    background: 'rgba(0,0,0,0)',
    font: { loadSystemFonts: false, fontFiles: fuentes },
  })
    .render()
    .asPng();
}

/**
 * Empaqueta varios PNG cuadrados en un `.ico`.
 *
 * Se escriben como DIB (BITMAPINFOHEADER + BGRA de abajo arriba + mascara AND) y no como PNG
 * embebido: el PNG dentro de un ICO solo lo entienden los consumidores modernos, y el unico motivo
 * para seguir publicando un `.ico` es precisamente el consumidor viejo que no lee el SVG.
 */
function empaquetarIco(imagenes) {
  const entradas = imagenes.map(({ tamano, bgra }) => {
    const filas = [];
    // El DIB va de abajo arriba; la mascara AND es obligatoria aunque el alfa la haga redundante.
    for (let y = tamano - 1; y >= 0; y--) {
      filas.push(bgra.subarray(y * tamano * 4, (y + 1) * tamano * 4));
    }
    const bytesMascaraPorFila = Math.ceil(tamano / 32) * 4;
    const mascara = Buffer.alloc(bytesMascaraPorFila * tamano, 0);

    const cabecera = Buffer.alloc(40);
    cabecera.writeUInt32LE(40, 0); // biSize
    cabecera.writeInt32LE(tamano, 4); // biWidth
    cabecera.writeInt32LE(tamano * 2, 8); // biHeight: imagen + mascara
    cabecera.writeUInt16LE(1, 12); // biPlanes
    cabecera.writeUInt16LE(32, 14); // biBitCount
    cabecera.writeUInt32LE(0, 16); // biCompression = BI_RGB

    return { tamano, datos: Buffer.concat([cabecera, ...filas, mascara]) };
  });

  const cabeceraIco = Buffer.alloc(6);
  cabeceraIco.writeUInt16LE(0, 0);
  cabeceraIco.writeUInt16LE(1, 2); // tipo 1 = icono
  cabeceraIco.writeUInt16LE(entradas.length, 4);

  let desplazamiento = 6 + entradas.length * 16;
  const directorio = entradas.map(({ tamano, datos }) => {
    const entrada = Buffer.alloc(16);
    entrada.writeUInt8(tamano === 256 ? 0 : tamano, 0);
    entrada.writeUInt8(tamano === 256 ? 0 : tamano, 1);
    entrada.writeUInt16LE(1, 4); // planos
    entrada.writeUInt16LE(32, 6); // bits por pixel
    entrada.writeUInt32LE(datos.length, 8);
    entrada.writeUInt32LE(desplazamiento, 12);
    desplazamiento += datos.length;
    return entrada;
  });

  return Buffer.concat([cabeceraIco, ...directorio, ...entradas.map((e) => e.datos)]);
}

/** Aplana sobre un fondo opaco: iOS no admite transparencia en `apple-touch-icon`. */
function aplanar(png, fondo) {
  return sharp(png).flatten({ background: fondo }).png({ compressionLevel: 9 }).toBuffer();
}

/**
 * Tarjeta social 1200x630. Repite el lenguaje de la landing —titular, subtitulo y distintivo— para
 * que quien recibe el enlace por WhatsApp vea lo mismo que va a encontrarse al abrirlo.
 */
function tarjetaSocial(glifo) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
  <rect width="1200" height="630" fill="${CREMA}"/>
  <circle cx="960" cy="315" r="215" fill="#eaf0dc"/>
  <path transform="${transformarGlifo(960, 315, 245)}" fill="${OLIVA}" d="${glifo}"/>
  <rect x="80" y="72" width="88" height="88" rx="20" fill="${OLIVA}"/>
  <path transform="${transformarGlifo(124, 116, 44)}" fill="#fff" d="${glifo}"/>
  <text x="192" y="130" font-family="Plus Jakarta Sans" font-size="44" fill="${OLIVA}">Terrenario</text>
  <text x="80" y="320" font-family="Plus Jakarta Sans" font-size="88" fill="${TINTA}">Tu tierra,</text>
  <text x="80" y="420" font-family="Plus Jakarta Sans" font-size="88" fill="${OLIVA}">bajo control.</text>
  <text x="80" y="490" font-family="Inter" font-size="29" fill="${TEXTO_SUAVE}">Terrenos, cosechas, compras y diario de campo,</text>
  <text x="80" y="530" font-family="Inter" font-size="29" fill="${TEXTO_SUAVE}">en un solo sitio.</text>
  <rect x="0" y="614" width="1200" height="16" fill="${OLIVA}"/>
</svg>`;
}

/** Convierte los woff2 autoalojados a TTF: resvg no descomprime woff2. */
async function cargarFuentes() {
  const rutas = [
    'node_modules/@fontsource/plus-jakarta-sans/files/plus-jakarta-sans-latin-800-normal.woff2',
    'node_modules/@fontsource/inter/files/inter-latin-500-normal.woff2',
  ];
  const ttf = [];
  for (const relativa of rutas) {
    const destino = path.join(RAIZ, 'node_modules', '.cache', path.basename(relativa) + '.ttf');
    await writeFile(destino, Buffer.from(await decompress(await readFile(path.join(RAIZ, relativa)))));
    ttf.push(destino);
  }
  return ttf;
}

async function main() {
  const vector = await readFile(path.join(PUBLICO, 'favicon.svg'), 'utf8');
  const glifo = /<path transform="[^"]*" fill="#fff" d="([^"]+)"/.exec(vector)[1];

  // A pantalla completa: el sistema operativo aplica su propia mascara (el squircle de iOS, el
  // circulo de Android), asi que redondear tambien aqui recortaria dos veces.
  const aSangre = vector.replace('rx="112"', 'rx="0"');

  await writeFile(path.join(PUBLICO, 'apple-touch-icon.png'), await aplanar(rasterizar(aSangre, 180), OLIVA));
  await writeFile(path.join(PUBLICO, 'icon-192.png'), rasterizar(vector, 192));
  await writeFile(path.join(PUBLICO, 'icon-512.png'), rasterizar(vector, 512));
  await writeFile(
    path.join(PUBLICO, 'icon-maskable-512.png'),
    await aplanar(rasterizar(aSangre, 512), OLIVA)
  );

  // 16/32/48: los tres tamanos que Windows y los navegadores viejos piden de un `.ico`.
  const imagenes = [];
  for (const tamano of [16, 32, 48]) {
    const { data } = await sharp(rasterizar(vector, tamano))
      .ensureAlpha()
      .toColorspace('srgb')
      .raw()
      .toBuffer({ resolveWithObject: true });
    const bgra = Buffer.alloc(tamano * tamano * 4);
    for (let i = 0; i < tamano * tamano; i++) {
      bgra[i * 4] = data[i * 4 + 2];
      bgra[i * 4 + 1] = data[i * 4 + 1];
      bgra[i * 4 + 2] = data[i * 4];
      bgra[i * 4 + 3] = data[i * 4 + 3];
    }
    imagenes.push({ tamano, bgra });
  }
  await writeFile(path.join(PUBLICO, 'favicon.ico'), empaquetarIco(imagenes));

  await writeFile(
    path.join(PUBLICO, 'og-image.png'),
    rasterizar(tarjetaSocial(glifo), 1200, await cargarFuentes())
  );

  console.log('Recursos de marca regenerados en public/.');
}

await main();
