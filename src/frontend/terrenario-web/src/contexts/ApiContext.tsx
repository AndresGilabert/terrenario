import React, { createContext, useContext, useMemo, useRef } from 'react';
import { useNavigate } from 'react-router';
import {
  AUTH_UNAUTHENTICATED,
  AUTH_WORKSPACE_FORBIDDEN,
  AUTH_WORKSPACE_SCOPE_REQUIRED,
  createHttpClient,
  type HttpClient,
} from '../services/http-client';
import { useAuth } from './AuthContext';
import { useWorkspace } from './WorkspaceContext';

const ApiContext = createContext<HttpClient | null>(null);

/**
 * Provee el cliente HTTP común (P-007/P-018) ya cableado con las reacciones globales a los errores de
 * ámbito de Workspace. Vive bajo `AuthProvider` y `WorkspaceProvider` para poder cerrar sesión y
 * resincronizar el contexto; y bajo `BrowserRouter` para poder redirigir al onboarding.
 */
export function ApiProvider({ children }: { children: React.ReactNode }) {
  const { getAccessToken, logout } = useAuth();
  const { refreshWorkspaces } = useWorkspace();
  const navigate = useNavigate();

  // Los handlers cambian de identidad entre renders; una ref evita recrear el cliente (y con él las
  // dependencias de los efectos que lo consumen) en cada render.
  const handlersRef = useRef({ getAccessToken, logout, refreshWorkspaces, navigate });
  handlersRef.current = { getAccessToken, logout, refreshWorkspaces, navigate };

  const client = useMemo(
    () =>
      createHttpClient({
        getAccessToken: () => handlersRef.current.getAccessToken(),
        onAuthError: (code) => {
          const h = handlersRef.current;
          if (code === AUTH_UNAUTHENTICATED) {
            void h.logout();
          } else if (code === AUTH_WORKSPACE_SCOPE_REQUIRED) {
            // La sesión perdió el Workspace activo: volver a resolverlo desde el onboarding.
            h.navigate('/onboarding', { replace: true });
          } else if (code === AUTH_WORKSPACE_FORBIDDEN) {
            // El recurso no era del Workspace activo: resincronizar la lista/estado de Workspaces.
            void h.refreshWorkspaces();
          }
        },
      }),
    []
  );

  return <ApiContext.Provider value={client}>{children}</ApiContext.Provider>;
}

export function useApiClient(): HttpClient {
  const client = useContext(ApiContext);
  if (!client) throw new Error('useApiClient must be used within an ApiProvider');
  return client;
}
