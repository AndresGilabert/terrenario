import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  AUTH_UNAUTHENTICATED,
  AUTH_WORKSPACE_FORBIDDEN,
  AUTH_WORKSPACE_SCOPE_REQUIRED,
  createHttpClient,
  HttpError,
} from './http-client';

/**
 * MVP-501 — El cliente HTTP común (P-007/P-018) es el punto por el que pasa **toda** la operativa
 * con ámbito de Workspace: si su reacción a los errores de scope de MVP-105 se rompe, la aplicación
 * deja de cerrar sesión cuando debe o desvía al onboarding cuando no toca. Hasta ahora estaba
 * cubierto solo por tipado y QA manual (`MVP-999`, `P-012`/`P-023`).
 */
describe('createHttpClient', () => {
  const BASE = 'http://api.test';

  let fetchMock: ReturnType<typeof vi.fn>;

  const respondWith = (
    status: number,
    body: unknown,
    init: { json?: boolean } = { json: true }
  ): Response =>
    ({
      ok: status >= 200 && status < 300,
      status,
      json: init.json === false ? () => Promise.reject(new Error('sin cuerpo')) : () => Promise.resolve(body),
    }) as unknown as Response;

  const clientWith = (opts: {
    token?: string | null;
    onAuthError?: (code: string, status: number) => void;
  }) =>
    createHttpClient({
      getAccessToken: async () => (opts.token === undefined ? 'token-valido' : opts.token),
      onAuthError: opts.onAuthError,
      baseUrl: BASE,
    });

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  describe('petición', () => {
    it('Deberia_EnviarElTokenVigente_Cuando_LaSesionEsValida', async () => {
      fetchMock.mockResolvedValue(respondWith(200, { data: [] }));

      await clientWith({}).request('/api/v1/plots');

      const [url, init] = fetchMock.mock.calls[0];
      expect(url).toBe(`${BASE}/api/v1/plots`);
      expect(init.headers.Authorization).toBe('Bearer token-valido');
      expect(init.method).toBe('GET');
    });

    it('Deberia_SerializarElCuerpoComoJson_Cuando_SeEnviaBody', async () => {
      fetchMock.mockResolvedValue(respondWith(200, {}));

      await clientWith({}).request('/api/v1/plots', { method: 'POST', body: { name: 'La Vega' } });

      const [, init] = fetchMock.mock.calls[0];
      expect(init.headers['Content-Type']).toBe('application/json');
      expect(init.body).toBe(JSON.stringify({ name: 'La Vega' }));
    });

    it('Deberia_ConservarLaCabeceraDeSesion_Cuando_QuienLlamaPasaCabecerasPropias', async () => {
      fetchMock.mockResolvedValue(respondWith(200, {}));

      // `If-Match` es el caso real (ADR-0005), pero lo que se protege es que ninguna cabecera de
      // quien llama pueda pisar `Authorization`: la sesión la gobierna el cliente.
      await clientWith({}).request('/api/v1/activities/1', {
        method: 'PATCH',
        headers: { 'If-Match': '3', Authorization: 'Bearer suplantado' },
        body: {},
      });

      const [, init] = fetchMock.mock.calls[0];
      expect(init.headers['If-Match']).toBe('3');
      expect(init.headers.Authorization).toBe('Bearer token-valido');
    });

    it('Deberia_DevolverUndefined_Cuando_LaRespuestaEs204', async () => {
      fetchMock.mockResolvedValue(respondWith(204, undefined));

      await expect(clientWith({}).request('/api/v1/activities/1', { method: 'DELETE' })).resolves.toBeUndefined();
    });
  });

  describe('query params', () => {
    const queryOf = (): string => new URL(fetchMock.mock.calls[0][0]).search;

    it('Deberia_OmitirLosValoresVacios_Cuando_SonUndefinedONull', async () => {
      fetchMock.mockResolvedValue(respondWith(200, {}));

      await clientWith({}).request('/api/v1/diary', {
        query: { plot_id: undefined, season_id: null, type: 'labor' },
      });

      expect(queryOf()).toBe('?type=labor');
    });

    it('Deberia_RepetirElParametro_Cuando_ElValorEsUnArray', async () => {
      fetchMock.mockResolvedValue(respondWith(200, {}));

      await clientWith({}).request('/api/v1/dashboard', { query: { plot_ids: ['a', 'b'] } });

      expect(queryOf()).toBe('?plot_ids=a&plot_ids=b');
    });

    it('Deberia_OmitirElParametro_Cuando_ElArrayEstaVacio', async () => {
      fetchMock.mockResolvedValue(respondWith(200, {}));

      // «Sin filtro» y «filtro que no selecciona nada» no son lo mismo: el array vacío se omite.
      await clientWith({}).request('/api/v1/dashboard', { query: { plot_ids: [] } });

      expect(queryOf()).toBe('');
    });
  });

  describe('errores de ámbito de sesión (MVP-105)', () => {
    it('Deberia_AvisarYNoLlamarALaApi_Cuando_NoHayTokenDeAcceso', async () => {
      const onAuthError = vi.fn();

      await expect(clientWith({ token: null, onAuthError }).request('/api/v1/plots')).rejects.toBeInstanceOf(
        HttpError
      );

      expect(onAuthError).toHaveBeenCalledWith(AUTH_UNAUTHENTICATED, 401);
      // Sin sesión no se sale a la red: el 401 se decide en el cliente.
      expect(fetchMock).not.toHaveBeenCalled();
    });

    it.each([
      [401, AUTH_UNAUTHENTICATED],
      [403, AUTH_WORKSPACE_SCOPE_REQUIRED],
      [403, AUTH_WORKSPACE_FORBIDDEN],
    ])('Deberia_NotificarLaReaccionDeSesion_Cuando_LaApiDevuelve_%s_%s', async (status, code) => {
      const onAuthError = vi.fn();
      fetchMock.mockResolvedValue(respondWith(status, { error: { code, message: 'no' } }));

      await expect(clientWith({ onAuthError }).request('/api/v1/plots')).rejects.toBeInstanceOf(HttpError);

      expect(onAuthError).toHaveBeenCalledWith(code, status);
    });

    it('Deberia_NoNotificarNada_Cuando_ElErrorEsDeNegocio', async () => {
      const onAuthError = vi.fn();
      fetchMock.mockResolvedValue(
        respondWith(409, { error: { code: 'CONFLICT_VERSION_MISMATCH', message: 'Ha cambiado.' } })
      );

      // Un conflicto de versión lo resuelve la vista recargando; no debe tocar la sesión.
      await expect(clientWith({ onAuthError }).request('/api/v1/activities/1')).rejects.toMatchObject({
        status: 409,
        code: 'CONFLICT_VERSION_MISMATCH',
      });

      expect(onAuthError).not.toHaveBeenCalled();
    });
  });

  describe('contrato de error', () => {
    it('Deberia_PropagarCodigoYMensaje_Cuando_LaApiRespetaElContrato', async () => {
      fetchMock.mockResolvedValue(
        respondWith(422, { error: { code: 'VALIDATION_PLOT_NAME_LENGTH', message: 'Nombre demasiado largo.' } })
      );

      await expect(clientWith({}).request('/api/v1/plots')).rejects.toMatchObject({
        status: 422,
        code: 'VALIDATION_PLOT_NAME_LENGTH',
        message: 'Nombre demasiado largo.',
      });
    });

    it('Deberia_DarUnMensajeAccionable_Cuando_LaRespuestaDeErrorNoTraeCuerpo', async () => {
      fetchMock.mockResolvedValue(respondWith(500, null, { json: false }));

      // Un 502 de un proxy no trae el contrato de la API: el usuario no puede quedarse sin mensaje.
      await expect(clientWith({}).request('/api/v1/plots')).rejects.toMatchObject({
        status: 500,
        code: 'REQUEST_FAILED',
      });
    });
  });
});
