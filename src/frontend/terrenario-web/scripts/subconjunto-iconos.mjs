/**
 * MVP-810 — Subconjunto de Material Symbols con los glifos que el producto usa.
 *
 * ---------------------------------------------------------------------------------------------
 * SI VIENES A AÑADIR UN ICONO NUEVO: no hay que tocar nada aquí. Lee la cabecera de
 * `scripts/inventario-iconos.mjs`, que explica cómo escribirlo para que el inventario lo vea.
 * ---------------------------------------------------------------------------------------------
 *
 * **Por qué existe.** La fuente completa pesa 3,78 MB —el 68 % de todo lo que se publica y el 82 %
 * de lo que descargaba la primera visita— y trae el catálogo entero de Material Symbols para pintar
 * setenta y tantos glifos (`P-115`). Autoalojarla es innegociable (`RN-042`: no se transfiere la IP
 * del visitante a Google), así que lo que se corrige no es *de dónde* viene sino *cuánta* viene.
 *
 * **Con qué.** HarfBuzz compilado a WebAssembly (`harfbuzzjs`), que es el mismo motor que usa el
 * navegador para componer y el mismo subsetter que usa Google Fonts. Sin binarios nativos ni Python:
 * `npm ci` en Linux basta, que es donde corre el CI. `fontverter` hace de traductor woff2 <-> ttf,
 * porque HarfBuzz no descomprime woff2.
 *
 * **Los dos detalles que no son obvios**, y que se explican aquí porque cada uno rompía el resultado
 * de una forma distinta y silenciosa:
 *
 * 1. **Las ligaduras.** Material Symbols pinta por ligadura: el texto `home` se sustituye por el
 *    glifo del icono. Si se subsetea «por texto», HarfBuzz hace el cierre de las tablas de
 *    composición y conserva *toda* ligadura formable con las letras retenidas — y entre los setenta
 *    nombres se usa casi el alfabeto entero, así que retiene casi el catálogo. Medido: **2,85 MB**,
 *    un recorte del 25 % que no sirve de nada. Por eso el cierre va **desactivado** y los glifos se
 *    piden uno a uno.
 *
 * 2. **La variante rellena.** `.material-symbols-outlined.fill` (el distintivo `eco` de la marca, en
 *    el lateral, el login, la portada y el alta) pide `FILL 1`. En 53 de los 75 iconos eso **no** es
 *    una interpolación del contorno: la fuente **sustituye el glifo por otro** mediante `rvrn`, y
 *    ese glifo alterno no tiene punto de código, así que un subconjunto pedido por puntos de código
 *    lo deja fuera. El resultado era un `eco` sin rellenar en cinco pantallas, y ni el peso ni la
 *    composición lo delataban. Se resuelve pidiendo los glifos **por índice**, y descubriéndolos
 *    como los descubre el navegador: componiendo cada nombre en cada instancia del espacio de
 *    variación que la fuente declara.
 *
 * El resultado se verifica antes de escribirlo: cada nombre se vuelve a componer contra la fuente ya
 * recortada, en las dos variantes que usa el CSS, y su contorno tiene que ser **idéntico** al de la
 * fuente completa. Cualquier diferencia rompe el build en vez de llegar a una pantalla.
 */
import { createHash } from 'node:crypto';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import * as fontverter from 'fontverter';
import cargarHarfbuzz from 'harfbuzzjs';
import { inventarioDeIconos } from './inventario-iconos.mjs';

const require = createRequire(import.meta.url);
const RAIZ = join(dirname(fileURLToPath(import.meta.url)), '..');
const FUENTE_COMPLETA = join(RAIZ, 'node_modules/material-symbols/material-symbols-outlined.woff2');
const DIRECTORIO_SALIDA = join(RAIZ, 'src/generado');
const SALIDA = join(DIRECTORIO_SALIDA, 'material-symbols-subconjunto.woff2');
const MANIFIESTO = join(DIRECTORIO_SALIDA, 'material-symbols-subconjunto.json');

/** Sube al cambiar el algoritmo, para invalidar la caché de quien ya tenga un subconjunto viejo. */
const VERSION = 2;

/** Las dos instancias que declara `src/index.css`: el icono normal y el `.fill`. */
const VARIANTES_DEL_CSS = [
  { FILL: 0, wght: 400, GRAD: 0, opsz: 24 },
  { FILL: 1, wght: 400, GRAD: 0, opsz: 24 },
];

// -------------------------------------------------------------------------------------------
// HarfBuzz: composición (hb.wasm, vía la envoltura `hbjs`) y recorte (hb-subset.wasm, a pelo).
// -------------------------------------------------------------------------------------------

/** Abre una fuente (woff2 o ttf) para componer. */
async function abrir(hb, bytes) {
  const ttf = await fontverter.convert(bytes, 'truetype');
  const face = hb.createFace(hb.createBlob(new Uint8Array(ttf)), 0);
  return { face, font: hb.createFont(face), ttf };
}

/** Compone un texto y devuelve los glifos resultantes, igual que hará el navegador. */
function componer(hb, font, texto) {
  const buffer = hb.createBuffer();
  buffer.addText(texto);
  buffer.guessSegmentProperties();
  hb.shape(font, buffer, '');
  const glifos = buffer.json();
  buffer.destroy();
  return glifos;
}

/**
 * Todas las instancias del espacio de variación que hay que barrer para no dejarse ningún glifo
 * alterno. Se construye desde los ejes que declara la fuente —no desde una lista escrita a mano—,
 * con los extremos, el valor por defecto y el punto medio de cada eje: las sustituciones por
 * variación (`rvrn`) se activan por tramos, y con los extremos y el centro se cubren todos.
 */
function instanciasABarrer(ejes) {
  let instancias = [{}];
  for (const [tag, { min, default: porDefecto, max }] of Object.entries(ejes)) {
    const valores = [...new Set([min, porDefecto, max, (min + max) / 2])];
    instancias = instancias.flatMap((base) => valores.map((valor) => ({ ...base, [tag]: valor })));
  }
  return [...VARIANTES_DEL_CSS, ...instancias];
}

/**
 * Índices de glifo que hacen falta para pintar `nombres`: el glifo de cada icono en cada instancia
 * del espacio de variación, más las letras con las que se escriben (la entrada de la ligadura).
 *
 * Un nombre que no componga en **un** glifo no existe en la fuente: en pantalla sería un hueco, así
 * que se devuelve como desconocido para que el generador rompa el build.
 */
function glifosNecesarios(hb, fuente, nombres) {
  const glifos = new Set();
  const desconocidos = new Set();
  const instancias = instanciasABarrer(fuente.face.getAxisInfos());

  for (const letra of new Set(nombres.join(''))) {
    const compuesto = componer(hb, fuente.font, letra);
    for (const glifo of compuesto) glifos.add(glifo.g);
  }

  for (const instancia of instancias) {
    fuente.font.setVariations(instancia);
    for (const nombre of nombres) {
      const compuesto = componer(hb, fuente.font, nombre);
      if (compuesto.length !== 1) desconocidos.add(nombre);
      else glifos.add(compuesto[0].g);
    }
  }
  fuente.font.setVariations(VARIANTES_DEL_CSS[0]);

  return { glifos: [...glifos].sort((a, b) => a - b), desconocidos: [...desconocidos] };
}

/**
 * Recorta la fuente a los glifos indicados.
 *
 * Se llama a `hb-subset` directamente en vez de usar una envoltura publicada porque las que hay
 * solo aceptan **texto**, y aquí hacen falta **índices de glifo**: la variante rellena de 53 de los 75 iconos
 * no tiene punto de código y no hay forma de pedirla por texto (ver la cabecera).
 */
async function recortar(bytesTtf, glifos) {
  const wasm = await WebAssembly.instantiate(readFileSync(require.resolve('harfbuzzjs/hb-subset.wasm')));
  const hbs = wasm.instance.exports;
  /** La memoria del módulo puede crecer al reservar, así que la vista se toma en cada uso. */
  const heap = () => new Uint8Array(hbs.memory.buffer);

  const entrada = hbs.hb_subset_input_create_or_fail();
  if (entrada === 0) throw new Error('hb_subset_input_create_or_fail ha fallado.');

  const puntero = hbs.malloc(bytesTtf.byteLength);
  heap().set(new Uint8Array(bytesTtf), puntero);
  const blob = hbs.hb_blob_create(puntero, bytesTtf.byteLength, 2 /* WRITABLE */, 0, 0);
  const face = hbs.hb_face_create(blob, 0);
  hbs.hb_blob_destroy(blob);

  // Conservar todas las características de composición (`liga`, `rvrn`…): equivale a
  // `--layout-features=*`. Sin esto, `hb-subset` se queda con un puñado por defecto.
  const caracteristicas = hbs.hb_subset_input_set(entrada, 6 /* LAYOUT_FEATURE_TAG */);
  hbs.hb_set_clear(caracteristicas);
  hbs.hb_set_invert(caracteristicas);

  // Sin cierre de composición: los glifos son exactamente los que se piden. Ver la cabecera.
  hbs.hb_subset_input_set_flags(
    entrada,
    hbs.hb_subset_input_get_flags(entrada) | 0x00000200 /* NO_LAYOUT_CLOSURE */
  );

  const conjunto = hbs.hb_subset_input_glyph_set(entrada);
  for (const glifo of glifos) hbs.hb_set_add(conjunto, glifo);

  const recortada = hbs.hb_subset_or_fail(face, entrada);
  hbs.hb_subset_input_destroy(entrada);
  if (recortada === 0) {
    hbs.hb_face_destroy(face);
    hbs.free(puntero);
    throw new Error('hb_subset_or_fail ha fallado: ¿fuente corrupta?');
  }

  const resultado = hbs.hb_face_reference_blob(recortada);
  const desplazamiento = hbs.hb_blob_get_data(resultado, 0);
  const longitud = hbs.hb_blob_get_length(resultado);
  const ttf = Buffer.from(heap().subarray(desplazamiento, desplazamiento + longitud));

  hbs.hb_blob_destroy(resultado);
  hbs.hb_face_destroy(recortada);
  hbs.hb_face_destroy(face);
  hbs.free(puntero);

  if (ttf.length === 0) throw new Error('El subconjunto ha salido vacío.');
  return fontverter.convert(ttf, 'woff2', 'truetype');
}

function huella(nombres, bytesFuente) {
  return createHash('sha256')
    .update(`v${VERSION}\n`)
    .update(nombres.join(','))
    .update('\n')
    .update(bytesFuente)
    .digest('hex');
}

/**
 * Genera el subconjunto si hace falta. Devuelve el resumen (nombres, bytes, si se regeneró).
 * Es idempotente: con el mismo inventario y la misma fuente de partida no reescribe nada, para que
 * arrancar el servidor de desarrollo no pague el recorte cada vez.
 */
export async function generarSubconjuntoDeIconos({ silencioso = false } = {}) {
  if (!existsSync(FUENTE_COMPLETA)) {
    throw new Error(
      `No encuentro ${FUENTE_COMPLETA}. ¿Falta un \`npm ci\` en src/frontend/terrenario-web?`
    );
  }

  const nombres = inventarioDeIconos(RAIZ);
  if (nombres.length === 0) {
    throw new Error('El inventario de iconos ha salido vacío: eso es un fallo del escaneo.');
  }

  const bytesFuente = readFileSync(FUENTE_COMPLETA);
  const esperada = huella(nombres, bytesFuente);

  if (existsSync(SALIDA) && existsSync(MANIFIESTO)) {
    const previo = JSON.parse(readFileSync(MANIFIESTO, 'utf8'));
    if (previo.huella === esperada) return { ...previo, regenerado: false };
  }

  const hb = await cargarHarfbuzz;
  const completa = await abrir(hb, bytesFuente);

  const { glifos, desconocidos } = glifosNecesarios(hb, completa, nombres);
  if (desconocidos.length > 0) {
    throw new Error(
      `Estos nombres no son iconos de Material Symbols Outlined: ${desconocidos.join(', ')}.\n` +
        'Compruébalos en https://fonts.google.com/icons (estilo Outlined). Si no es un icono, no ' +
        'lo llames `icon` ni lo pongas dentro de un `<span class="material-symbols-outlined">`.'
    );
  }

  const subconjunto = await recortar(completa.ttf, glifos);

  // Verificación: la fuente recortada tiene que pintar exactamente lo mismo que la completa en las
  // dos variantes que declara el CSS. Se comparan los **contornos**, no los tamaños: un glifo que
  // se quedara sin sus deltas de variación compondría igual y se vería distinto.
  const recortada = await abrir(hb, subconjunto);
  const diferencias = [];
  for (const variantes of VARIANTES_DEL_CSS) {
    completa.font.setVariations(variantes);
    recortada.font.setVariations(variantes);
    for (const nombre of nombres) {
      const original = componer(hb, completa.font, nombre);
      const copia = componer(hb, recortada.font, nombre);
      const contorno = copia.length === 1 ? recortada.font.glyphToPath(copia[0].g) : '';
      if (copia.length !== 1 || contorno !== completa.font.glyphToPath(original[0].g)) {
        diferencias.push(`${nombre} (FILL ${variantes.FILL})`);
      }
    }
  }
  if (diferencias.length > 0) {
    throw new Error(
      `El subconjunto no pinta igual que la fuente completa en: ${diferencias.join(', ')}.\n` +
        'No se escribe nada.'
    );
  }

  const ejes = recortada.face.getAxisInfos();
  for (const eje of ['wght', 'FILL', 'GRAD', 'opsz']) {
    if (!(eje in ejes)) {
      throw new Error(`El subconjunto ha perdido el eje variable ${eje}: cambiaría el aspecto.`);
    }
  }

  const resumen = {
    huella: esperada,
    generado_por: 'scripts/subconjunto-iconos.mjs (MVP-810)',
    iconos: nombres.length,
    glifos: glifos.length,
    bytes: subconjunto.length,
    bytes_fuente_completa: bytesFuente.length,
    ejes: Object.fromEntries(Object.entries(ejes).map(([t, e]) => [t, [e.min, e.default, e.max]])),
    nombres,
  };

  mkdirSync(DIRECTORIO_SALIDA, { recursive: true });
  writeFileSync(SALIDA, subconjunto);
  writeFileSync(MANIFIESTO, JSON.stringify(resumen, null, 2) + '\n');

  if (!silencioso) {
    const porcentaje = ((1 - subconjunto.length / bytesFuente.length) * 100).toFixed(1);
    console.log(
      `[MVP-810] Subconjunto de iconos: ${nombres.length} iconos en ${glifos.length} glifos, ` +
        `${(subconjunto.length / 1024).toFixed(1)} kB ` +
        `(desde ${(bytesFuente.length / 1024 / 1024).toFixed(2)} MB, −${porcentaje} %).`
    );
  }

  return { ...resumen, regenerado: true };
}

// Ejecutable a mano (`node scripts/subconjunto-iconos.mjs`) además de desde el plugin de Vite.
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await generarSubconjuntoDeIconos();
}
