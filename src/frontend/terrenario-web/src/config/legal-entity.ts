/**
 * MVP-504 (B-1) — Identidad del responsable del tratamiento, en **un solo sitio**.
 *
 * Hasta ahora estos datos eran marcadores escritos a mano dentro de las páginas legales, repetidos
 * entre la Política de Privacidad y los Términos. Cambiar el NIF obligaba a tocar JSX en dos ficheros
 * y a confiar en no olvidarse de ninguno; esto lo convierte en dato, no en código.
 *
 * Los valores están **versionados** a propósito, no en un `.env`: la LSSI obliga a publicarlos, así
 * que no hay nada que proteger, y `.env` no está en el repositorio —el build de CI y cualquier
 * despliegue publicarían las páginas vacías—. Lo que sí se puede es sobreescribir cada campo por
 * variable de entorno (`VITE_LEGAL_*`) para un despliegue concreto sin tocar el código.
 *
 * Ojo: las `VITE_*` se incrustan en el bundle al compilar. Aquí da igual —son datos públicos por
 * obligación legal— pero por eso mismo este módulo no debe crecer con nada que no lo sea.
 *
 * MVP-715 — Los valores dejan de estar escritos aquí y pasan a `legal-entity.json`, **porque ahora
 * hay un segundo consumidor**: el pie legal de los correos transaccionales. La API incrusta ese
 * mismo fichero al compilar (`<EmbeddedResource>` en `Terrenario.Api.csproj`) en vez de reescribir
 * el NIF en C#, con el mismo criterio que ya se aplicó a la CSP en `vite.config.ts`: dos copias de
 * un dato legal divergen y nadie se entera hasta que la equivocada es la que se publica.
 *
 * El JSON vive dentro de `src/` y no en la raíz del repositorio por una razón concreta: el
 * `server.fs.allow` de Vite se calcula desde el `package-lock.json`, que está en
 * `src/frontend/terrenario-web`, así que un fichero por encima de esa carpeta quedaría bloqueado en
 * el servidor de desarrollo.
 */
import legalEntityDefaults from './legal-entity.json';

/** Campos que la normativa exige publicar. Si añades uno, `missingLegalFields` avisa si va vacío. */
export interface LegalEntity {
  /** Titular del servicio. LSSI art. 10 y RGPD art. 13. */
  legalName: string;
  /** NIF/CIF. LSSI art. 10. */
  taxId: string;
  /** Domicilio a efectos de notificaciones. LSSI art. 10. */
  address: string;
  /** Dirección donde se ejercen los derechos de los arts. 15-22. */
  privacyEmail: string;
  /** Delegado de Protección de Datos, o «No designado». No es obligatorio designarlo (art. 37). */
  dpo: string;
  /** Encargado del envío de invitaciones (art. 28). */
  emailProvider: string;
  /** Encargado del alojamiento (art. 28). */
  hostingProvider: string;
  /** Dónde se almacenan los datos. Determina si hay transferencia internacional (cap. V). */
  hostingRegion: string;
}

// El tipado no es decorativo: si el JSON pierde un campo o le cambia el tipo, esto no compila.
const DEFAULTS: LegalEntity = legalEntityDefaults;

/** Variable de entorno que sobreescribe cada campo. */
const ENV_KEYS: Record<keyof LegalEntity, string> = {
  legalName: 'VITE_LEGAL_NAME',
  taxId: 'VITE_LEGAL_TAX_ID',
  address: 'VITE_LEGAL_ADDRESS',
  privacyEmail: 'VITE_LEGAL_PRIVACY_EMAIL',
  dpo: 'VITE_LEGAL_DPO',
  emailProvider: 'VITE_LEGAL_EMAIL_PROVIDER',
  hostingProvider: 'VITE_LEGAL_HOSTING_PROVIDER',
  hostingRegion: 'VITE_LEGAL_HOSTING_REGION'
};

type EnvSource = Record<string, unknown>;

/**
 * Resuelve la identidad legal: variable de entorno si trae contenido, y si no el valor versionado.
 *
 * Es una función pura y no lee `import.meta.env` directamente para que se pueda probar en ambos
 * estados sin recargar módulos.
 */
export function resolveLegalEntity(env: EnvSource = {}): LegalEntity {
  const resolved = {} as LegalEntity;

  for (const key of Object.keys(DEFAULTS) as (keyof LegalEntity)[]) {
    const override = env[ENV_KEYS[key]];
    // Una variable definida pero vacía no debe dejar hueco en un documento legal: cae al versionado.
    resolved[key] = typeof override === 'string' && override.trim() !== ''
      ? override.trim()
      : DEFAULTS[key];
  }

  return resolved;
}

/** Campos sin valor. Debe estar siempre vacío: es la red de seguridad al añadir un campo nuevo. */
export function missingLegalFields(entity: LegalEntity): (keyof LegalEntity)[] {
  return (Object.keys(entity) as (keyof LegalEntity)[]).filter((key) => entity[key].trim() === '');
}

/** Identidad efectiva de este build. */
export const legalEntity: LegalEntity = resolveLegalEntity(
  import.meta.env as unknown as EnvSource
);
