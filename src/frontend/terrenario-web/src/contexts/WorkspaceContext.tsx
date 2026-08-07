import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import type { Workspace, WorkspaceMembership } from '../types/workspace.types';
import { workspaceService } from '../services/workspace.service';
import { invitationService } from '../services/invitation.service';
import { useAuth } from './AuthContext';
import { useDataScope } from './DataScopeContext';

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
  /**
   * Resincroniza el contexto completo (Workspace activo + lista) sin recrear la sesión. Lo usa el
   * ciclo de vida del Workspace (MVP-206): renombrar refresca el nombre en selector y cabecera
   * (CA-1), y dar de baja o traspasar hace que el activo pase a resolverse de nuevo (CA-8).
   */
  refreshContext: () => Promise<void>;
  /** Acepta una invitación por enlace y deja la sesión situada en ese Workspace (MVP-103). */
  acceptInvitation: (token: string) => Promise<Workspace>;
  /** Acepta una invitación recibida por id (bandeja/notificaciones) y sitúa la sesión (MVP-107). */
  acceptInvitationById: (id: string) => Promise<Workspace>;
}

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

/**
 * Mantiene el Workspace activo de la sesión y la lista de Workspaces disponibles (MVP-102/104).
 * Mientras el activo sea `null` y la sesión esté autenticada, el usuario debe pasar por el
 * onboarding de creación o aceptar una invitación.
 */
export function WorkspaceProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: isAuthLoading, getAccessToken, setAccessToken } = useAuth();
  const { invalidateScope } = useDataScope();
  const [activeWorkspace, setActiveWorkspace] = useState<Workspace | null>(null);
  const [workspaces, setWorkspaces] = useState<WorkspaceMembership[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // El token cambia al renovarse o al cambiar de Workspace; la referencia evita recargar
  // el contexto en cada rotación de token.
  const getAccessTokenRef = useRef(getAccessToken);
  getAccessTokenRef.current = getAccessToken;

  const loadWorkspaces = useCallback(async (tokenOverride?: string): Promise<void> => {
    // Tras crear/cambiar/aceptar, la sesión se reemite: se pasa el token nuevo explícitamente
    // porque el de estado aún no se ha propagado (evita recargar con un token obsoleto).
    const accessToken = tokenOverride ?? (await getAccessTokenRef.current());
    if (!accessToken) {
      setWorkspaces([]);
      return;
    }

    try {
      setWorkspaces(await workspaceService.listWorkspaces(accessToken));
    } catch {
      // Un fallo transitorio no debe borrar la lista buena: se conserva lo último cargado para
      // no hacer "desaparecer" del selector Workspaces a los que el usuario sí pertenece.
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

  /**
   * MVP-701 — Fija el Workspace activo e invalida los datos cargados **solo si de verdad ha
   * cambiado**. Renombrar (MVP-206) resincroniza el contexto sin cambiar de Workspace: remontar el
   * área operativa por un cambio de nombre sería recargarlo todo para nada.
   */
  const applyActiveWorkspace = useCallback(
    (next: Workspace | null) => {
      setActiveWorkspace((previous) => {
        if (previous?.id !== next?.id) invalidateScope();
        return next;
      });
    },
    [invalidateScope]
  );

  const refreshContext = useCallback(async (): Promise<void> => {
    const accessToken = await getAccessTokenRef.current();
    if (!accessToken) return;

    try {
      // El activo se resuelve siempre en servidor: si el Workspace en el que estábamos se ha dado
      // de baja, la respuesta ya trae el que pasa a serlo (o `null` si no queda ninguno).
      applyActiveWorkspace(await workspaceService.getActiveWorkspace(accessToken));
    } catch {
      applyActiveWorkspace(null);
    }

    await loadWorkspaces(accessToken);
  }, [loadWorkspaces, applyActiveWorkspace]);

  const createWorkspace = useCallback(
    async (name: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await workspaceService.createWorkspace(name, accessToken);

      // El backend reemite la sesión ya situada en el nuevo Workspace.
      setAccessToken(result.access_token, result.expires_in);
      applyActiveWorkspace(result.workspace);
      await loadWorkspaces(result.access_token);

      return result.workspace;
    },
    [setAccessToken, loadWorkspaces, applyActiveWorkspace]
  );

  const switchWorkspace = useCallback(
    async (workspaceId: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await workspaceService.switchWorkspace(workspaceId, accessToken);

      // La sesión reemitida lleva el nuevo Workspace en el claim: al fijarla, cualquier
      // operación posterior queda acotada al contexto elegido (CA-2, sin datos cruzados).
      setAccessToken(result.access_token, result.expires_in);
      // MVP-701 (CA-1, CA-2) — y además se invalida lo cargado: reemitir la sesión no bastaba, las
      // vistas seguían pintando el Workspace anterior porque nada volvía a dispararlas (`P-081`).
      applyActiveWorkspace(result.workspace);
      await loadWorkspaces(result.access_token);

      return result.workspace;
    },
    [setAccessToken, loadWorkspaces, applyActiveWorkspace]
  );

  const adoptAcceptedSession = useCallback(
    (result: { access_token: string; expires_in: number; workspace: Workspace }): Workspace => {
      // Igual que al crear un Workspace, el backend reemite la sesión ya situada en el destino.
      setAccessToken(result.access_token, result.expires_in);
      applyActiveWorkspace(result.workspace);
      return result.workspace;
    },
    [setAccessToken, applyActiveWorkspace]
  );

  const acceptInvitation = useCallback(
    async (token: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await invitationService.acceptInvitation(token, accessToken);
      const workspace = adoptAcceptedSession(result);
      await loadWorkspaces(result.access_token);

      return workspace;
    },
    [adoptAcceptedSession, loadWorkspaces]
  );

  const acceptInvitationById = useCallback(
    async (id: string): Promise<Workspace> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      const result = await invitationService.acceptReceivedInvitation(id, accessToken);
      const workspace = adoptAcceptedSession(result);
      await loadWorkspaces(result.access_token);

      return workspace;
    },
    [adoptAcceptedSession, loadWorkspaces]
  );

  const value: WorkspaceContextValue = {
    activeWorkspace,
    workspaces,
    isLoading,
    createWorkspace,
    switchWorkspace,
    refreshWorkspaces: loadWorkspaces,
    refreshContext,
    acceptInvitation,
    acceptInvitationById,
  };

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);
  if (!context) throw new Error('useWorkspace must be used within a WorkspaceProvider');
  return context;
}
