import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  AUTH_UNAUTHENTICATED,
  AUTH_WORKSPACE_FORBIDDEN,
  AUTH_WORKSPACE_SCOPE_REQUIRED,
  createHttpClient,
  esFalloDeRed,
  HttpError,
  NETWORK_UNREACHABLE,
} from './http-client';
import { getReportContext, resetReportContext } from '../lib/report-context';
import { estadoDeConexion, marcarConConexion } from '../lib/connectivity';

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
    init: { json?: boolean; requestId?: string } = { json: true }
  ): Response =>
    ({
      ok: status >= 200 && status < 300,
      status,
      // MVP-711 — Las respuestas de la API traen `X-Request-Id` desde MVP-105 (`P-006`), y el cliente
      // lo lee para el canal de feedback. El doble tiene que traerlo o estaría probando otra cosa.
      headers: new Headers(init.requestId ? { 'X-Request-Id': init.requestId } : {}),
      json: init.json === false ? () => Promise.reject(new Error('sin cuerpo')) : () => Promise.resolve(body),
      // Un `Response` de verdad **siempre** tiene `text()`, y este doble no lo tenía. No es un detalle
      // de fontanería: por eso la suite no vio que el cliente reventaba con una respuesta correcta sin
      // cuerpo (`202` del canal de feedback). Un doble que ofrece menos que el original no prueba el
      // original, prueba otra cosa.
      // `JSON.stringify(undefined)` devuelve `undefined`, no una cadena: un cuerpo ausente se
      // representa como cadena vacía, que es lo que da un `Response` real sin contenido.
      text:
        init.json === false
          ? () => Promise.resolve('')
          : () => Promise.resolve(JSON.stringify(body) ?? ''),
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

  describe('correlación para el canal de feedback (MVP-711)', () => {
    beforeEach(() => resetReportContext());

    it('Deberia_RetenerLaCorrelacion_Cuando_UnaPeticionFalla', async () => {
      fetchMock.mockResolvedValue(
        respondWith(500, null, { json: false, requestId: 'a1b2c3d4e5f6' })
      );

      await expect(clientWith({}).request('/api/v1/plots')).rejects.toBeInstanceOf(HttpError);

      // Es lo que convierte «me ha dado un error» en una línea concreta de la traza del servidor.
      expect(getReportContext().lastFailedRequestId).toBe('a1b2c3d4e5f6');
    });

    it('Deberia_NoRetenerNada_Cuando_LaPeticionVaBien', async () => {
      fetchMock.mockResolvedValue(respondWith(200, {}, { requestId: 'no-deberia-guardarse' }));

      await clientWith({}).request('/api/v1/plots');

      expect(getReportContext().lastFailedRequestId).toBeNull();
    });

    it('Deberia_ConservarElUltimoFallo_Cuando_ElSiguienteNoTraeCabecera', async () => {
      fetchMock.mockResolvedValueOnce(respondWith(500, null, { json: false, requestId: 'el-bueno' }));
      await expect(clientWith({}).request('/api/v1/plots')).rejects.toBeInstanceOf(HttpError);

      // Sin cabecera —un proxy por medio, o CORS sin exponerla— es mejor el último identificador
      // conocido que ninguno.
      fetchMock.mockResolvedValueOnce(respondWith(500, null, { json: false }));
      await expect(clientWith({}).request('/api/v1/plots')).rejects.toBeInstanceOf(HttpError);

      expect(getReportContext().lastFailedRequestId).toBe('el-bueno');
    });
  });

  /**
   * MVP-709 (`P-091`) — La caída de red, distinguida del error del servidor.
   *
   * `fetch` solo rechaza cuando la petición **no llega a tener respuesta**. Esa frontera es la que
   * separa «no hay cobertura» de «el servidor ha contestado que no», y es lo que pide el `CA-1`.
   */
  describe('MVP-709 — caida de red', () => {
    beforeEach(() => {
      marcarConConexion();
    });

    it('Deberia_DecirQueNoHayConexion_Cuando_LaPeticionMuereSinRespuesta', async () => {
      // Así falla `fetch` de verdad sin red: un `TypeError`, no una respuesta con estado.
      fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

      const error = await clientWith({}).request('/api/v1/plots').catch((e: unknown) => e);

      expect(esFalloDeRed(error)).toBe(true);
      expect((error as HttpError).status).toBe(0);
      expect((error as HttpError).code).toBe(NETWORK_UNREACHABLE);
      // El texto no puede ser el generico: es lo que el CA-1 prohibe.
      expect((error as HttpError).message).toMatch(/conexión|servidor/i);
    });

    it('Deberia_HeredarDeHttpError_Para_QueTodaLaAplicacionLoDigaBien', async () => {
      // Media aplicacion esta escrita como `error instanceof HttpError ? error.message : generico`.
      // Si esto no heredara, todas esas pantallas volverian al texto que el CA-1 prohibe.
      fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

      const error = await clientWith({}).request('/api/v1/plots').catch((e: unknown) => e);

      expect(error).toBeInstanceOf(HttpError);
    });

    it('Deberia_MarcarSinConexion_Cuando_FallaLaRed', async () => {
      fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

      await clientWith({}).request('/api/v1/plots').catch(() => null);

      expect(estadoDeConexion()).toBe('sin-conexion');
    });

    it('Deberia_VolverAMarcarConexion_Cuando_UnaPeticionTraeRespuesta', async () => {
      fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
      await clientWith({}).request('/api/v1/plots').catch(() => null);

      // Incluso un 500 demuestra que hay conexion: es lo unico que este estado mide.
      fetchMock.mockResolvedValue(respondWith(500, null, { json: false }));
      await clientWith({}).request('/api/v1/plots').catch(() => null);

      expect(estadoDeConexion()).toBe('en-linea');
    });

    it('Deberia_NoCerrarSesion_Cuando_ElFalloEsDeRed', async () => {
      // Lo peor que podia pasar: perder la cobertura y que la aplicacion lo lea como sesion invalida.
      const onAuthError = vi.fn();
      fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

      await clientWith({ onAuthError }).request('/api/v1/plots').catch(() => null);

      expect(onAuthError).not.toHaveBeenCalled();
    });

    it('Deberia_DejarPasarLaCancelacion_SinConfundirlaConUnCorte', async () => {
      // Abortar al cambiar de pantalla es lo normal; pintarlo como «sin conexion» seria mentir.
      fetchMock.mockRejectedValue(new DOMException('cancelada', 'AbortError'));

      const error = await clientWith({}).request('/api/v1/plots').catch((e: unknown) => e);

      expect(esFalloDeRed(error)).toBe(false);
      expect((error as DOMException).name).toBe('AbortError');
      expect(estadoDeConexion()).toBe('en-linea');
    });
  });

  describe('respuestas correctas sin cuerpo', () => {
    /** Una respuesta real sin cuerpo: `json()` falla igual que en el navegador. */
    const sinCuerpo = (status: number): Response =>
      ({
        ok: true,
        status,
        headers: new Headers(),
        text: () => Promise.resolve(''),
        json: () => Promise.reject(new SyntaxError('Unexpected end of JSON input')),
      }) as unknown as Response;

    it('Deberia_ResolverSinError_Cuando_LaRespuestaEs202SinCuerpo', async () => {
      // El canal de feedback (MVP-711) responde `202 Accepted` sin cuerpo. El cliente solo trataba el
      // `204`, asi que el `json()` de un cuerpo vacio lanzaba un `SyntaxError` —que **no** es un
      // `HttpError`— y la pantalla decia «no hemos podido enviar tu mensaje» **con el correo ya
      // entregado**. Es el peor fallo posible en un canal de incidencias: el usuario lo reintenta.
      fetchMock.mockResolvedValue(sinCuerpo(202));

      await expect(clientWith({}).request('/api/v1/feedback', { method: 'POST' })).resolves.toBeUndefined();
    });

    it('Deberia_ResolverSinError_Cuando_LaRespuestaEs204', async () => {
      fetchMock.mockResolvedValue(sinCuerpo(204));

      await expect(clientWith({}).request('/api/v1/plots', { method: 'DELETE' })).resolves.toBeUndefined();
    });

    it('Deberia_SeguirLeyendoElCuerpo_Cuando_LaRespuestaLoTrae', async () => {
      fetchMock.mockResolvedValue(respondWith(200, { data: [1, 2] }));

      await expect(clientWith({}).request('/api/v1/plots')).resolves.toEqual({ data: [1, 2] });
    });
  });
});
