import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import type { Workspace, WorkspaceMembership } from '../types/workspace.types';
import { workspaceService } from '../services/workspace.service';
import { invitationService } from '../services/invitation.service';
import { useAuth } from './AuthContext';

interface WorkspaceContextValue {
  activeWorkspace: Workspace | null;
  /** Workspaces a los que el usuario puede alternar (MVP-104, HU-1). */
  workspaces: WorkspaceMembership[];
  isLoading: boolean;
  createWorkspace: (name: string) => Promise<Workspace>;
  /** Cambia el Workspace activo y reemite la sesión situada en él (MVP-104, HU-2). */
  switchWorkspace: (workspaceId: string) => Promise<Workspace>;
  /** Vuelve a cargar la lista de membresías (p. ej. tras aceptar una invitación). */
  refreshWorkspaces: () => Promise<void>;
  /** Acepta una invitación y deja la sesión situada en ese Workspace (MVP-103). */
  acceptInvitation: (token: string) => Promise<Workspace>;
}

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

/**
 * Mantiene el Workspace activo de la sesión y la lista de Workspaces disponibles (MVP-102/104).
 * Mientras el activo sea `null` y la sesión esté autenticada, el usuario debe pasar por el
 * onboarding de creación o aceptar una invitación.
 */
export function WorkspaceProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: isAuthLoading, getAccessToken, setAccessToken } = useAuth();
  const [activeWorkspace, setActiveWorkspace] = useState<Workspace | null>(null);
  const [workspaces, setWorkspaces] = useState<WorkspaceMembership[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // El token cambia al renovarse o al cambiar de Workspace; la referencia evita recargar
  // el contexto en cada rotación de token.
  const getAccessTokenRef = useRef(getAccessToken);
  getAccessTokenRef.current = getAccessToken;

  const loadWorkspaces = useCallback(async (): Promise<void> => {
    const accessToken = await getAccessTokenRef.current();
    if (!accessToken) {
      setWorkspaces([]);
      return;
    }

    try {
      setWorkspaces(await workspaceService.listWorkspaces(accessToken));
    } catch {
      // El selector es informativo: si la lista falla, la operativa sigue con el activo.
      setWorkspaces([]);
    }
  }, []);

  useEffect(() => {
    if (isAuthLoading) return;

    if (!isAuthenticated) {
      setActiveWorkspace(null);
      setWorkspaces([]);
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
        if (!cancelled && workspace) await loadWorkspaces();
      } catch {
        if (!cancelled) setActiveWorkspace(null);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, isAuthLoading, loadWorkspaces]);

  const createWorkspace = useCallback(
    async (name: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await workspaceService.createWorkspace(name, accessToken);

      // El backend reemite la sesión ya situada en el nuevo Workspace.
      setAccessToken(result.access_token, result.expires_in);
      setActiveWorkspace(result.workspace);
      await loadWorkspaces();

      return result.workspace;
    },
    [setAccessToken, loadWorkspaces]
  );

  const switchWorkspace = useCallback(
    async (workspaceId: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await workspaceService.switchWorkspace(workspaceId, accessToken);

      // La sesión reemitida lleva el nuevo Workspace en el claim: al fijarla, cualquier
      // operación posterior queda acotada al contexto elegido (CA-2, sin datos cruzados).
      setAccessToken(result.access_token, result.expires_in);
      setActiveWorkspace(result.workspace);
      await loadWorkspaces();

      return result.workspace;
    },
    [setAccessToken, loadWorkspaces]
  );

  const acceptInvitation = useCallback(
    async (token: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await invitationService.acceptInvitation(token, accessToken);

      // Igual que al crear un Workspace, el backend reemite la sesión ya situada en el destino.
      setAccessToken(result.access_token, result.expires_in);
      setActiveWorkspace(result.workspace);
      await loadWorkspaces();

      return result.workspace;
    },
    [setAccessToken, loadWorkspaces]
  );

  const value: WorkspaceContextValue = {
    activeWorkspace,
    workspaces,
    isLoading,
    createWorkspace,
    switchWorkspace,
    refreshWorkspaces: loadWorkspaces,
    acceptInvitation,
  };

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);
  if (!context) throw new Error('useWorkspace must be used within a WorkspaceProvider');
  return context;
}
