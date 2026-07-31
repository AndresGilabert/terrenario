import type { HttpClient, RequestOptions } from '../services/http-client';

/** Una llamada capturada por el doble de cliente HTTP, para poder afirmar sobre método y query. */
export interface RecordedCall {
  path: string;
  options: RequestOptions;
}

export interface FakeHttpClient extends HttpClient {
  readonly calls: RecordedCall[];
  /** Llamadas a un path (comparación por prefijo, para no acoplarse a los ids de la URL). */
  callsTo(prefix: string): RecordedCall[];
}

type Route = (options: RequestOptions, path: string) => unknown;

/**
 * MVP-501 — Doble del cliente HTTP común para los tests de vista.
 *
 * Se sustituye el cliente y no `fetch` a propósito: lo que estos tests cubren es la **lógica de
 * decisión de la vista** (qué pide, qué acciones ofrece según lo que recibe), no el transporte, que
 * ya tiene sus propios tests en `http-client.test.ts`.
 *
 * Las rutas se declaran por prefijo de path y gana **la más específica** (el prefijo más largo), no
 * el orden de declaración: `/api/v1/workspace-members` no puede quedarse con las llamadas a
 * `/api/v1/workspace-members/{id}/revoke` solo por estar escrita antes.
 */
export function createFakeHttpClient(routes: Record<string, Route | unknown>): FakeHttpClient {
  const calls: RecordedCall[] = [];

  const client: FakeHttpClient = {
    calls,
    callsTo: (prefix) => calls.filter((call) => call.path.startsWith(prefix)),
    // Sin `vi.fn`: el registro de llamadas ya lo lleva `calls`, y envolver una función genérica en un
    // espía le hace perder el parámetro de tipo (el `Promise<T>` degrada a `Promise<unknown>` y el
    // build de tipos falla).
    async request<T>(path: string, options: RequestOptions = {}): Promise<T> {
      calls.push({ path, options });

      const entry = Object.entries(routes)
        .filter(([prefix]) => path.startsWith(prefix))
        .sort(([a], [b]) => b.length - a.length)[0];
      if (!entry) throw new Error(`Ruta no simulada en el test: ${options.method ?? 'GET'} ${path}`);

      const [, handler] = entry;
      const value = typeof handler === 'function' ? (handler as Route)(options, path) : handler;
      return (await value) as T;
    },
  };

  return client;
}
