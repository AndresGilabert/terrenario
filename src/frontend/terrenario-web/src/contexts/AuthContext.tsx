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
}

const AuthContext = createContext<AuthContextValue | null>(null);

const REFRESH_BEFORE_EXPIRY_MS = 60_000;

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, initialState);
  const expiresAtRef = useRef<number | null>(null);
  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const scheduleRefresh = useCallback((expiresIn: number) => {
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    const delay = Math.max(0, expiresIn * 1000 - REFRESH_BEFORE_EXPIRY_MS);
    refreshTimerRef.current = setTimeout(async () => {
      try {
        const result = await authService.refreshToken();
        dispatch({ type: 'REFRESH_SUCCESS', payload: { accessToken: result.access_token } });
        sessionStorage.setItem(ACCESS_TOKEN_KEY, result.access_token);
        scheduleRefresh(result.expires_in);
      } catch {
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
        })
        .catch(() => {
          sessionStorage.removeItem(ACCESS_TOKEN_KEY);
          dispatch({ type: 'SET_LOADING', payload: false });
        });
    } else {
      dispatch({ type: 'SET_LOADING', payload: false });
    }

    return () => {
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    };
  }, []);

  const login = useCallback(
    (accessToken: string, user: AuthUser, expiresIn: number) => {
      sessionStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
      expiresAtRef.current = Date.now() + expiresIn * 1000;
      dispatch({ type: 'LOGIN_SUCCESS', payload: { accessToken, user } });
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
    } catch {
      return null;
    }
  }, [state.accessToken]);

  const value: AuthContextValue = {
    ...state,
    login,
    logout,
    getAccessToken,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
}
