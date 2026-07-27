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

/** Códigos de ámbito de Workspace/autenticación que exigen una reacción global de sesión (MVP-105). */
export const AUTH_UNAUTHENTICATED = 'AUTH_UNAUTHENTICATED';
export const AUTH_WORKSPACE_SCOPE_REQUIRED = 'AUTH_WORKSPACE_SCOPE_REQUIRED';
export const AUTH_WORKSPACE_FORBIDDEN = 'AUTH_WORKSPACE_FORBIDDEN';

export type AuthErrorHandler = (code: string, status: number) => void;

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  /** Cuerpo JSON serializable. Se serializa y se fija `Content-Type: application/json`. */
  body?: unknown;
  /** Query params; los valores `undefined`/`null` se omiten. */
  query?: Record<string, string | number | boolean | undefined | null>;
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
          if (value !== undefined && value !== null) url.searchParams.set(key, String(value));
        }
      }

      const headers: Record<string, string> = { Authorization: `Bearer ${accessToken}` };
      const hasBody = options.body !== undefined;
      if (hasBody) headers['Content-Type'] = 'application/json';

      const response = await fetch(url.toString(), {
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
