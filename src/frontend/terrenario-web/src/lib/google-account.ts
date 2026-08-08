/**
 * MVP-712 — Lo que hay que decir sobre el acceso, en un solo sitio.
 *
 * `RN-018` y `RN-036` fijan Google OIDC como único proveedor del MVP: eso es una decisión, no un
 * defecto. El defecto era otro y sí era de producto: «accede con tu cuenta de Google» se lee como
 * «necesitas un Gmail», y quien tiene un correo de Hotmail, de Outlook o de su cooperativa se
 * descarta solo (`P-089`). Puede entrar: una Cuenta de Google se da de alta **con la dirección que
 * ya se tiene**, sin crear ningún buzón nuevo.
 *
 * El texto vive aquí y no en cada pantalla porque el login, la landing y la pantalla de invitación
 * tienen que decir **lo mismo**: si cada una lo redacta a su manera, la que se quede corta vuelve a
 * dejar fuera a quien esta historia intenta recuperar.
 *
 * Es un **enlace**, nunca un recurso: la landing es pública y su CSP es `default-src 'self'`
 * (`RN-042`). Navegar a Google lo decide la persona; cargar algo de Google, no.
 */

/** Alta de Cuenta de Google. El formulario ofrece usar una dirección de correo ya existente. */
export const GOOGLE_ACCOUNT_SIGNUP_URL = 'https://accounts.google.com/signup';

/** Rótulo del enlace. Dice qué se consigue al pulsarlo, no a dónde lleva. */
export const GOOGLE_ACCOUNT_SIGNUP_LABEL = 'Dar de alta mi dirección como Cuenta de Google';

/**
 * La frase compartida. Nombra los dominios concretos a propósito —«cualquier dirección» es
 * abstracto y quien tiene un Hotmail no se da por aludido— y **no promete que valga cualquier
 * correo sin más**: dar de alta la dirección en Google es un paso real que hay que dar.
 */
export const ANY_EMAIL_WORKS_HINT =
  'No hace falta que tu correo sea de Gmail: sirve el de Hotmail, Outlook o el de tu cooperativa, ' +
  'siempre que des de alta esa misma dirección como Cuenta de Google. Es gratis y no crea un buzón ' +
  'nuevo.';
