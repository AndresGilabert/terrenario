import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import type { Workspace } from '../types/workspace.types';
import { workspaceService } from '../services/workspace.service';
import { invitationService } from '../services/invitation.service';
import { useAuth } from './AuthContext';

interface WorkspaceContextValue {
  activeWorkspace: Workspace | null;
  isLoading: boolean;
  createWorkspace: (name: string) => Promise<Workspace>;
  /** Acepta una invitación y deja la sesión situada en ese Workspace (MVP-103). */
  acceptInvitation: (token: string) => Promise<Workspace>;
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
    async (name: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await workspaceService.createWorkspace(name, accessToken);

      // El backend reemite la sesión ya situada en el nuevo Workspace.
      setAccessToken(result.access_token, result.expires_in);
      setActiveWorkspace(result.workspace);

      return result.workspace;
    },
    [setAccessToken]
  );

  const acceptInvitation = useCallback(
    async (token: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await invitationService.acceptInvitation(token, accessToken);

      // Igual que al crear un Workspace, el backend reemite la sesión ya situada en el destino.
      setAccessToken(result.access_token, result.expires_in);
      setActiveWorkspace(result.workspace);

      return result.workspace;
    },
    [setAccessToken]
  );

  const value: WorkspaceContextValue = {
    activeWorkspace,
    isLoading,
    createWorkspace,
    acceptInvitation,
  };

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);
  if (!context) throw new Error('useWorkspace must be used within a WorkspaceProvider');
  return context;
}
