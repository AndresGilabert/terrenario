import { render, screen, waitFor } from '@testing-library/react';
import { useEffect, useRef } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider, useAuth } from './AuthContext';
import { authService } from '../services/auth.service';
import { NetworkError } from '../services/http-client';

const ACCESS_TOKEN_KEY = 'terrenario_at';

function Sonda() {
  const { isAuthenticated, isLoading, user } = useAuth();
  return (
    <div>
      <span data-testid="autenticado">{String(isAuthenticated)}</span>
      <span data-testid="cargando">{String(isLoading)}</span>
      <span data-testid="usuario">{user?.displayName ?? '—'}</span>
    </div>
  );
}

/**
 * MVP-709 (`P-091`) — **Un corte de cobertura no puede cerrar la sesión.**
 *
 * Es el peor caso de la historia y el que más daño hacía: el refresco programado se dispara solo cada
 * cuarto de hora largo, y si saltaba mientras el móvil estaba sin cobertura echaba fuera al usuario
 * —llevándose por delante el formulario que estuviera escribiendo—. El motivo del cierre era una red
 * caída, no un rechazo del servidor.
 */
describe('AuthContext — pérdida de conexión', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
    sessionStorage.clear();
  });

  it('conserva el token guardado cuando el arranque falla por red', async () => {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, 'token-de-antes');
    vi.spyOn(authService, 'getMe').mockRejectedValue(new NetworkError('Sin conexión.'));

    render(
      <AuthProvider>
        <Sonda />
      </AuthProvider>
    );

    await waitFor(() => expect(screen.getByTestId('cargando')).toHaveTextContent('false'));
    // No se ha podido comprobar si el token vale, que no es lo mismo que saber que no vale. Borrarlo
    // obligaba a volver a pasar por Google por un problema de red.
    expect(sessionStorage.getItem(ACCESS_TOKEN_KEY)).toBe('token-de-antes');
  });

  it('descarta el token guardado cuando el servidor responde que no vale', async () => {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, 'token-caducado');
    vi.spyOn(authService, 'getMe').mockRejectedValue(new Error('401'));

    render(
      <AuthProvider>
        <Sonda />
      </AuthProvider>
    );

    await waitFor(() => expect(screen.getByTestId('cargando')).toHaveTextContent('false'));
    expect(sessionStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull();
  });

  it('no cierra la sesión cuando el refresco programado falla por red', async () => {
    const refresh = vi
      .spyOn(authService, 'refreshToken')
      .mockRejectedValue(new NetworkError('Sin conexión.'));

    render(
      <AuthProvider>
        <IniciaSesion expiraEn={90} />
      </AuthProvider>
    );
    await waitFor(() => expect(screen.getByTestId('autenticado')).toHaveTextContent('true'));

    // El temporizador salta 60 s antes de caducar; con 90 s de vida, a los 30 s.
    await vi.advanceTimersByTimeAsync(31_000);
    expect(refresh).toHaveBeenCalledTimes(1);

    // Aquí estaba el daño: antes, este fallo hacía LOGOUT y se llevaba lo que estuviera escrito.
    expect(screen.getByTestId('autenticado')).toHaveTextContent('true');
    expect(sessionStorage.getItem(ACCESS_TOKEN_KEY)).toBe('token-vivo');

    // Y sigue reintentando en vez de rendirse: el `refresh_token` es una cookie de larga duración y
    // seguirá valiendo cuando vuelva la cobertura.
    await vi.advanceTimersByTimeAsync(31_000);
    expect(refresh).toHaveBeenCalledTimes(2);
    expect(screen.getByTestId('autenticado')).toHaveTextContent('true');
  });

  it('cierra la sesión cuando el refresco programado lo rechaza el servidor', async () => {
    // La contrapartida: si el servidor **responde** que la sesión no vale, se cierra como siempre.
    vi.spyOn(authService, 'refreshToken').mockRejectedValue(new Error('AUTH_REFRESH_TOKEN_INVALID'));

    render(
      <AuthProvider>
        <IniciaSesion expiraEn={90} />
      </AuthProvider>
    );
    await waitFor(() => expect(screen.getByTestId('autenticado')).toHaveTextContent('true'));

    await vi.advanceTimersByTimeAsync(31_000);

    await waitFor(() => expect(screen.getByTestId('autenticado')).toHaveTextContent('false'));
    expect(sessionStorage.getItem(ACCESS_TOKEN_KEY)).toBeNull();
  });
});

/**
 * Establece sesión como lo hace el retorno de Google.
 *
 * Hasta `P-099` era el **único** camino que programaba el refresco, y por eso los escenarios de
 * temporizador se montan por aquí: arrancar con un token guardado no lo programaba y la prueba no
 * habría llegado a disparar nada. Ese defecto está corregido y tiene su propia cobertura abajo.
 */
function IniciaSesion({ expiraEn }: { expiraEn: number }) {
  const { login } = useAuth();
  const hecho = useRef(false);
  useEffect(() => {
    if (hecho.current) return;
    hecho.current = true;
    login('token-vivo', { id: 'u1', displayName: 'Andrés' }, expiraEn);
  }, [login, expiraEn]);
  return <Sonda />;
}

/** Construye un JWT sin firmar cuyo `exp` está dentro de `segundos`. Solo se lee la carga. */
function tokenQueCaducaEn(segundos: number): string {
  const carga = { exp: Math.floor(Date.now() / 1000) + segundos };
  const b64 = btoa(JSON.stringify(carga)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `cabecera.${b64}.firma`;
}

/**
 * MVP-999 (`P-099`) — **Arrancar con un token guardado tiene que programar el refresco.**
 *
 * No lo hacía: solo lo programaba `login()`, el retorno de Google. Una pestaña recuperada se quedaba
 * con el token que tuviera y no lo renovaba; al caducar, la primera petición se iba en 401. Quedaba
 * tapado porque `getAccessToken` refresca cuando falta el token en memoria, pero eso es un camino de
 * rescate y no el ciclo previsto, y lo tapado no se ve cuando se rompe.
 */
describe('AuthContext — refresco al arrancar con token guardado', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
    sessionStorage.clear();
  });

  const arrancarCon = async (token: string) => {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, token);
    vi.spyOn(authService, 'getMe').mockResolvedValue({ id: 'u1', display_name: 'Andrés' });
    const refresh = vi
      .spyOn(authService, 'refreshToken')
      .mockResolvedValue({ access_token: 'token-renovado', expires_in: 900 });

    render(
      <AuthProvider>
        <Sonda />
      </AuthProvider>
    );
    await waitFor(() => expect(screen.getByTestId('autenticado')).toHaveTextContent('true'));
    return refresh;
  };

  it('programa el refresco según lo que le quede al token', async () => {
    // Caduca en 300 s y el refresco salta 60 s antes: a los 240 s.
    const refresh = await arrancarCon(tokenQueCaducaEn(300));

    await vi.advanceTimersByTimeAsync(239_000);
    expect(refresh).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(2_000);
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('refresca de inmediato si el token guardado ya caducó', async () => {
    const refresh = await arrancarCon(tokenQueCaducaEn(-60));

    await vi.advanceTimersByTimeAsync(100);

    // Sin esperar a que una petición del usuario se vaya en 401, que es lo que pasaba antes.
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('refresca de inmediato si el token no es un JWT legible', async () => {
    // Nunca debería pasar, pero quedarse sin programar nada es el fallo que se está corrigiendo.
    const refresh = await arrancarCon('esto-no-es-un-jwt');

    await vi.advanceTimersByTimeAsync(100);

    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('encadena el siguiente refresco con la caducidad que devuelve el servidor', async () => {
    const refresh = await arrancarCon(tokenQueCaducaEn(-1));
    await vi.advanceTimersByTimeAsync(100);
    expect(refresh).toHaveBeenCalledTimes(1);

    // 900 s de vida => el siguiente a los 840. Si no encadenara, la sesión volvería a quedar suelta.
    await vi.advanceTimersByTimeAsync(841_000);
    expect(refresh).toHaveBeenCalledTimes(2);
  });
});
