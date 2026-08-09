import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useReducer,
  useRef,
} from 'react';
import type { AuthAction, AuthState, AuthUser } from '../types/auth.types';
import { authService } from '../services/auth.service';
import { esFalloDeRed } from '../services/http-client';

const ACCESS_TOKEN_KEY = 'terrenario_at';

const initialState: AuthState = {
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isLoading: true,
};

function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'LOGIN_SUCCESS':
      return {
        ...state,
        user: action.payload.user,
        accessToken: action.payload.accessToken,
        isAuthenticated: true,
        isLoading: false,
      };
    case 'LOGOUT':
      return { ...initialState, isLoading: false };
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    case 'REFRESH_SUCCESS':
      return { ...state, accessToken: action.payload.accessToken };
    default:
      return state;
  }
}

interface AuthContextValue extends AuthState {
  login: (accessToken: string, user: AuthUser, expiresIn: number) => void;
  logout: () => Promise<void>;
  getAccessToken: () => Promise<string | null>;
  /** Reemplaza el access token de la sesión activa (p. ej. tras crear un Workspace). */
  setAccessToken: (accessToken: string, expiresIn: number) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const REFRESH_BEFORE_EXPIRY_MS = 60_000;

/**
 * MVP-709 — Espera antes de reintentar el refresco cuando lo que falló fue la red.
 *
 * No se reutiliza `scheduleRefresh` con una caducidad corta: su retardo es
 * `caducidad - REFRESH_BEFORE_EXPIRY_MS`, así que cualquier valor por debajo del minuto da cero y el
 * reintento se convierte en un bucle que machaca la red mientras no la haya.
 */
const REINTENTO_SIN_CONEXION_MS = 30_000;

/**
 * MVP-999 (`P-099`) — Segundos de vida que le quedan al token guardado, leídos de su propio `exp`.
 *
 * <b>Por qué se lee el token en vez de preguntar.</b> Al recuperar una pestaña no sabemos cuánto le
 * queda: `GET /auth/me` confirma que vale, pero no dice hasta cuándo. La alternativa era refrescar en
 * cada arranque para obtener un `expires_in` fresco, y eso añade una fila a `refresh_tokens` por cada
 * carga de página —la rotación ya genera miles al año por usuario activo (`RN-041`)—.
 *
 * <b>No se valida nada aquí, y no hace falta.</b> El `exp` solo se usa para decidir *cuándo*
 * programar el refresco; quien decide si el token vale es el servidor en cada petición. Un `exp`
 * manipulado adelanta o atrasa un temporizador propio, nada más.
 *
 * Devuelve `null` si el token no es un JWT legible o si ya caducó: en ambos casos quien llama debe
 * refrescar de inmediato en vez de programar nada.
 */
function segundosDeVidaRestante(token: string): number | null {
  try {
    const carga = token.split('.')[1];
    if (!carga) return null;
    // base64url → base64, y `atob` no admite el relleno implícito.
    const base64 = carga.replace(/-/g, '+').replace(/_/g, '/');
    const { exp } = JSON.parse(atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')));
    if (typeof exp !== 'number') return null;
    const restante = exp - Math.floor(Date.now() / 1000);
    return restante > 0 ? restante : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, initialState);
  const expiresAtRef = useRef<number | null>(null);
  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const scheduleRefresh = useCallback((expiresIn: number, delayMs?: number) => {
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    const delay = delayMs ?? Math.max(0, expiresIn * 1000 - REFRESH_BEFORE_EXPIRY_MS);
    refreshTimerRef.current = setTimeout(async () => {
      try {
        const result = await authService.refreshToken();
        dispatch({ type: 'REFRESH_SUCCESS', payload: { accessToken: result.access_token } });
        sessionStorage.setItem(ACCESS_TOKEN_KEY, result.access_token);
        scheduleRefresh(result.expires_in);
      } catch (error) {
        // MVP-709 (`P-091`) — **Un corte de cobertura no cierra la sesión.**
        //
        // Este temporizador se dispara solo, cada cuarto de hora largo. Sin esta distinción, saltar
        // justo mientras el móvil está sin cobertura echaba fuera al usuario y se llevaba por delante
        // el formulario que estuviera escribiendo, que es exactamente lo que la historia evita. Y el
        // motivo del cierre era una red caída, no un rechazo del servidor.
        //
        // Solo se cierra cuando el servidor **responde** que la sesión ya no vale. Si no hubo
        // respuesta, se conserva y se vuelve a intentar en un minuto: el `refresh_token` es una cookie
        // de larga duración y sigue siendo válido cuando la cobertura vuelva.
        if (esFalloDeRed(error)) {
          scheduleRefresh(expiresIn, REINTENTO_SIN_CONEXION_MS);
          return;
        }
        dispatch({ type: 'LOGOUT' });
        sessionStorage.removeItem(ACCESS_TOKEN_KEY);
      }
    }, delay);
  }, []);

  useEffect(() => {
    const storedToken = sessionStorage.getItem(ACCESS_TOKEN_KEY);
    if (storedToken) {
      authService
        .getMe(storedToken)
        .then((userData) => {
          dispatch({
            type: 'LOGIN_SUCCESS',
            payload: {
              accessToken: storedToken,
              user: { id: userData.id, displayName: userData.display_name },
            },
          });
          // MVP-999 (`P-099`) — **Este camino no programaba el refresco.** Solo lo hacía `login()`,
          // el retorno de Google, así que una pestaña recuperada se quedaba con el token que tuviera
          // y no lo renovaba: al caducar, la primera petición se iba en 401. Quedaba tapado porque
          // `getAccessToken` refresca cuando falta el token en memoria, pero eso es un camino de
          // rescate y no el ciclo previsto.
          //
          // Sin `exp` legible o ya caducado se refresca de inmediato (`0`), que es lo que hacía el
          // rescate pero de forma explícita y sin esperar a que falle una petición del usuario.
          scheduleRefresh(segundosDeVidaRestante(storedToken) ?? 0);
        })
        .catch((error: unknown) => {
          // MVP-709 — Arrancar sin cobertura no debe borrar el token guardado: no se ha podido
          // comprobar si vale, que no es lo mismo que saber que no vale. Se deja la sesión en estado
          // de carga resuelto y sin usuario, y la primera petición con cobertura la recupera. Borrarlo
          // obligaba a volver a pasar por Google para algo que era un problema de red.
          if (!esFalloDeRed(error)) sessionStorage.removeItem(ACCESS_TOKEN_KEY);
          dispatch({ type: 'SET_LOADING', payload: false });
        });
    } else {
      dispatch({ type: 'SET_LOADING', payload: false });
    }

    return () => {
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    };
    // `scheduleRefresh` es estable (`useCallback` sin dependencias), así que declararla no reejecuta
    // el arranque; se declara igual para que el aviso de dependencias siga sirviendo de algo.
  }, [scheduleRefresh]);

  const login = useCallback(
    (accessToken: string, user: AuthUser, expiresIn: number) => {
      sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
      expiresAtRef.current = Date.now() + expiresIn * 1000;
      dispatch({ type: 'LOGIN_SUCCESS', payload: { accessToken, user } });
      scheduleRefresh(expiresIn);
    },
    [scheduleRefresh]
  );

  const setAccessToken = useCallback(
    (accessToken: string, expiresIn: number) => {
      sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
      expiresAtRef.current = Date.now() + expiresIn * 1000;
      dispatch({ type: 'REFRESH_SUCCESS', payload: { accessToken } });
      scheduleRefresh(expiresIn);
    },
    [scheduleRefresh]
  );

  const logout = useCallback(async () => {
    try {
      await authService.logout();
    } finally {
      sessionStorage.removeItem(ACCESS_TOKEN_KEY);
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
      dispatch({ type: 'LOGOUT' });
    }
  }, []);

  const getAccessToken = useCallback(async (): Promise<string | null> => {
    if (state.accessToken) return state.accessToken;
    try {
      const result = await authService.refreshToken();
      dispatch({ type: 'REFRESH_SUCCESS', payload: { accessToken: result.access_token } });
      sessionStorage.setItem(ACCESS_TOKEN_KEY, result.access_token);
      return result.access_token;
    } catch (error) {
      // MVP-709 — Devolver `null` aquí significa «no hay sesión», y el cliente HTTP lo traduce en
      // cerrarla. Sin cobertura eso es mentira: no se sabe si la sesión vale, solo que no se ha
      // podido preguntar. Se propaga el fallo de red para que la pantalla diga «sin conexión» y la
      // sesión siga en pie.
      if (esFalloDeRed(error)) throw error;
      return null;
    }
  }, [state.accessToken]);

  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    getAccessToken,
    setAccessToken,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
}
