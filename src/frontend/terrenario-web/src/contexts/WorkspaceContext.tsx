import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import type { Workspace } from '../types/workspace.types';
import { workspaceService } from '../services/workspace.service';
import { useAuth } from './AuthContext';

interface WorkspaceContextValue {
  activeWorkspace: Workspace | null;
  isLoading: boolean;
  createWorkspace: (nombre: string) => Promise<Workspace>;
}

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

/**
 * Mantiene el Workspace activo de la sesión (MVP-102). Mientras sea `null` y la sesión
 * esté autenticada, el usuario debe pasar por el onboarding de creación.
 */
export function WorkspaceProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: isAuthLoading, getAccessToken, setAccessToken } = useAuth();
  const [activeWorkspace, setActiveWorkspace] = useState<Workspace | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // El token cambia al renovarse o al crear un Workspace; la referencia evita recargar
  // el contexto en cada rotación de token.
  const getAccessTokenRef = useRef(getAccessToken);
  getAccessTokenRef.current = getAccessToken;

  useEffect(() => {
    if (isAuthLoading) return;

    if (!isAuthenticated) {
      setActiveWorkspace(null);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);

    (async () => {
      const accessToken = await getAccessTokenRef.current();
      if (cancelled) return;

      try {
        const workspace = accessToken
          ? await workspaceService.getActiveWorkspace(accessToken)
          : null;
        if (!cancelled) setActiveWorkspace(workspace);
      } catch {
        if (!cancelled) setActiveWorkspace(null);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, isAuthLoading]);

  const createWorkspace = useCallback(
    async (nombre: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await workspaceService.createWorkspace(nombre, accessToken);

      // El backend reemite la sesión ya situada en el nuevo Workspace.
      setAccessToken(result.access_token, result.expires_in);
      setActiveWorkspace(result.workspace);

      return result.workspace;
    },
    [setAccessToken]
  );

  const value: WorkspaceContextValue = { activeWorkspace, isLoading, createWorkspace };

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);
  if (!context) throw new Error('useWorkspace must be used within a WorkspaceProvider');
  return context;
}
