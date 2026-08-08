import { marcarConConexion, marcarSinConexion } from '../lib/connectivity';
import { API_BASE, readErrorBody } from './api.config';

/**
 * Error de una llamada HTTP con el contrato `{ error: { code, message } }` de la API
 * (`docs/02-arquitectura/contratos-api.md`). Lleva el `status` para que cada servicio decida
 * (p. ej. tratar un 404 como "todavía no hay recurso" en vez de como fallo).
 */
export class HttpError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = 'HttpError';
    this.status = status;
    this.code = code;
  }
}

/** Código propio: no viene del contrato de la API, porque no hay respuesta de la API que lo traiga. */
export const NETWORK_UNREACHABLE = 'NETWORK_UNREACHABLE';

/**
 * MVP-709 (`P-091`) — La petición murió sin respuesta: no hay cobertura, o el servidor no se alcanza.
 *
 * <b>Hereda de `HttpError` a propósito.</b> Toda la aplicación está escrita como
 * `error instanceof HttpError ? error.message : 'No se pudieron cargar…'`. Si esto fuera una clase
 * aparte, cada una de esas decenas de pantallas caería en su texto genérico —justo el «no se pudieron
 * cargar los datos» indistinguible de un fallo del servidor que el `CA-1` prohíbe— y habría que
 * repasarlas una a una. Heredando, el mensaje correcto aparece en todas a la vez, y las que quieran
 * distinguirlo pueden mirar el código.
 *
 * El `status` es `0`, que es lo que vale «no hubo respuesta»: ningún código HTTP lo representa sin
 * mentir.
 */
export class NetworkError extends HttpError {
  constructor(message: string) {
    super(0, NETWORK_UNREACHABLE, message);
    this.name = 'NetworkError';
  }
}

export function esFalloDeRed(error: unknown): error is NetworkError {
  return error instanceof HttpError && error.code === NETWORK_UNREACHABLE;
}

/**
 * Se distinguen los dos casos porque la acción del usuario es distinta: si el móvil dice que no hay
 * red, toca buscar cobertura; si dice que sí y aun así no se llega, el problema puede estar al otro
 * lado y volver a intentarlo tiene sentido.
 */
function mensajeDeRed(): string {
  const sinInterfaz = typeof navigator !== 'undefined' && navigator.onLine === false;
  return sinInterfaz
    ? 'Sin conexión. Comprueba la cobertura y vuelve a intentarlo.'
    : 'No se ha podido contactar con el servidor. Comprueba la cobertura y vuelve a intentarlo.';
}

/**
 * MVP-709 — `fetch` con la caída de red traducida. Lo usan este cliente y `auth.service`, que es el
 * otro sitio por el que salen peticiones.
 *
 * `fetch` solo rechaza cuando la petición **no llega a tener respuesta**: sin red, DNS que no
 * resuelve, TLS que falla o CORS que la bloquea. Cualquier respuesta del servidor —incluido un 500—
 * resuelve. Esa frontera es exactamente la que pide el `CA-1`.
 *
 * La cancelación se deja pasar tal cual: abortar una petición al cambiar de pantalla es lo normal y
 * no es un fallo de conexión. Confundirlas pintaría «sin conexión» cada vez que alguien navega rápido.
 */
export async function fetchVigilado(input: string, init?: RequestInit): Promise<Response> {
  let response: Response;
  try {
    response = await fetch(input, init);
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;
    marcarSinConexion();
    throw new NetworkError(mensajeDeRed());
  }

  // Hubo respuesta: hay conexión, diga lo que diga `navigator.onLine`.
  marcarConConexion();
  return response;
}

/** Códigos de ámbito de Workspace/autenticación que exigen una reacción global de sesión (MVP-105). */
export const AUTH_UNAUTHENTICATED = 'AUTH_UNAUTHENTICATED';
export const AUTH_WORKSPACE_SCOPE_REQUIRED = 'AUTH_WORKSPACE_SCOPE_REQUIRED';
export const AUTH_WORKSPACE_FORBIDDEN = 'AUTH_WORKSPACE_FORBIDDEN';

export type AuthErrorHandler = (code: string, status: number) => void;

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  /** Cuerpo JSON serializable. Se serializa y se fija `Content-Type: application/json`. */
  body?: unknown;
  /**
   * Query params; los valores `undefined`/`null` se omiten.
   *
   * Un **array** se serializa como parámetro **repetible** (`?plot_ids=a&plot_ids=b`), que es la forma
   * que espera la API para los filtros multivalor —`plot_ids` del dashboard (MVP-403), `type` del
   * diario—. Un array vacío se omite: «sin filtro» y «filtro que no selecciona nada» no son lo mismo,
   * y quien quiera el segundo debe decirlo explícitamente.
   */
  query?: Record<
    string,
    string | number | boolean | undefined | null | readonly (string | number | boolean)[]
  >;
  /**
   * Cabeceras adicionales de la petición. Lo estrenan los registros operativos (MVP-301), que exigen
   * `If-Match` con la versión vigente en `PATCH`/`DELETE` (ADR-0005). No puede sobrescribir
   * `Authorization`: la sesión la gobierna el cliente, no quien lo llama.
   */
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

export interface HttpClient {
  request<T>(path: string, options?: RequestOptions): Promise<T>;
}

/**
 * Cliente HTTP común del frontend (P-007/P-018). Centraliza en un único punto lo que antes repetía
 * cada `*.service.ts`: base URL, cabecera `Authorization` (con el token vigente y su refresco), el
 * parseo del cuerpo de error del contrato y, sobre todo, la reacción a los errores de ámbito de
 * Workspace introducidos con el enforcement de MVP-105:
 *
 * - `AUTH_UNAUTHENTICATED` (401): la sesión ya no es válida → cerrar sesión.
 * - `AUTH_WORKSPACE_SCOPE_REQUIRED` (403): la sesión no tiene Workspace activo → volver al onboarding.
 * - `AUTH_WORKSPACE_FORBIDDEN` (403): el recurso no es del Workspace activo → resincronizar contexto.
 *
 * `plots` (MVP-202) es el primer maestro con recurso *scoped* consumido por la UI y estrena este
 * cliente; `seasons` (también *scoped*, P-018) se migra a la vez. Los servicios de auth/workspace se
 * mantienen como estaban (ruta crítica de login).
 */
export function createHttpClient(opts: {
  getAccessToken: () => Promise<string | null>;
  onAuthError?: AuthErrorHandler;
  baseUrl?: string;
}): HttpClient {
  const baseUrl = opts.baseUrl ?? API_BASE;

  return {
    async request<T>(path: string, options: RequestOptions = {}): Promise<T> {
      const accessToken = await opts.getAccessToken();
      if (!accessToken) {
        opts.onAuthError?.(AUTH_UNAUTHENTICATED, 401);
        throw new HttpError(401, AUTH_UNAUTHENTICATED, 'Tu sesión ha expirado. Vuelve a iniciar sesión.');
      }

      const url = new URL(`${baseUrl}${path}`);
      if (options.query) {
        for (const [key, value] of Object.entries(options.query)) {
          if (value === undefined || value === null) continue;
          if (Array.isArray(value)) {
            for (const item of value) url.searchParams.append(key, String(item));
            continue;
          }
          url.searchParams.set(key, String(value));
        }
      }

      const headers: Record<string, string> = {
        ...options.headers,
        Authorization: `Bearer ${accessToken}`,
      };
      const hasBody = options.body !== undefined;
      if (hasBody) headers['Content-Type'] = 'application/json';

      // MVP-709: `fetchVigilado` traduce la caída de red a `NetworkError` y mantiene al día el estado
      // de conexión. Lo que sale de aquí es siempre una respuesta del servidor.
      const response = await fetchVigilado(url.toString(), {
        method: options.method ?? 'GET',
        credentials: 'include',
        headers,
        body: hasBody ? JSON.stringify(options.body) : undefined,
        signal: options.signal,
      });

      if (response.ok) {
        if (response.status === 204) return undefined as T;
        return (await response.json()) as T;
      }

      const errorBody = await readErrorBody(response);
      const code = errorBody?.error?.code ?? 'REQUEST_FAILED';
      const message = errorBody?.error?.message ?? 'No se pudo completar la operación. Inténtalo de nuevo.';

      if (
        response.status === 401 ||
        code === AUTH_WORKSPACE_SCOPE_REQUIRED ||
        code === AUTH_WORKSPACE_FORBIDDEN
      ) {
        opts.onAuthError?.(code, response.status);
      }

      throw new HttpError(response.status, code, message);
    },
  };
}
