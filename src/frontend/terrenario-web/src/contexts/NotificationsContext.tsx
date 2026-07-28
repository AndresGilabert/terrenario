import React, { createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';
import type { Workspace } from '../types/workspace.types';
import type { ReceivedInvitation } from '../types/invitation.types';
import { invitationService } from '../services/invitation.service';
import { useAuth } from './AuthContext';
import { useWorkspace } from './WorkspaceContext';

interface NotificationsContextValue {
  /** Invitaciones recibidas y accionables por la cuenta autenticada (MVP-107, HU-3). */
  receivedInvitations: ReceivedInvitation[];
  /** Número de invitaciones pendientes: alimenta el contador de la campanita (CA-3). */
  pendingCount: number;
  isLoading: boolean;
  refresh: () => Promise<void>;
  /** Acepta y deja la sesión situada en ese Workspace (decisión de producto: cambiar de contexto). */
  accept: (id: string) => Promise<Workspace>;
  reject: (id: string) => Promise<void>;
  /**
   * Primera invitación aún no vista, para ofrecerla en el modal no bloqueante (HU-2). `null` si no
   * hay ninguna nueva. Cerrar el modal la marca como vista sin perderla: sigue en la bandeja.
   */
  newInvitation: ReceivedInvitation | null;
  dismissNew: () => void;
}

const NotificationsContext = createContext<NotificationsContextValue | null>(null);

const SEEN_STORAGE_KEY = 'terrenario:seen_invitations';

function readSeen(): Set<string> {
  try {
    const raw = localStorage.getItem(SEEN_STORAGE_KEY);
    return new Set<string>(raw ? (JSON.parse(raw) as string[]) : []);
  } catch {
    return new Set<string>();
  }
}

function persistSeen(seen: Set<string>): void {
  try {
    localStorage.setItem(SEEN_STORAGE_KEY, JSON.stringify([...seen]));
  } catch {
    // El tracking de "vistas" es una mejora de UX; si el almacenamiento falla, no rompe el flujo.
  }
}

/**
 * Centro de notificaciones del MVP: hoy solo invitaciones (MVP-107). Vive dentro de
 * `WorkspaceProvider` porque aceptar una invitación reemite la sesión y cambia el Workspace activo.
 */
export function NotificationsProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: isAuthLoading, getAccessToken } = useAuth();
  const { acceptInvitationById } = useWorkspace();

  const [receivedInvitations, setReceivedInvitations] = useState<ReceivedInvitation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [seen, setSeen] = useState<Set<string>>(() => readSeen());

  const getAccessTokenRef = useRef(getAccessToken);
  getAccessTokenRef.current = getAccessToken;

  const refresh = useCallback(async (): Promise<void> => {
    const accessToken = await getAccessTokenRef.current();
    if (!accessToken) {
      setReceivedInvitations([]);
      return;
    }

    try {
      const invitations = await invitationService.listReceivedInvitations(accessToken);
      setReceivedInvitations(invitations);

      // Poda de "vistas": el almacén solo conserva ids que siguen pendientes, para no crecer sin fin.
      setSeen((current) => {
        const alive = new Set([...current].filter((id) => invitations.some((i) => i.id === id)));
        persistSeen(alive);
        return alive;
      });
    } catch {
      // La bandeja es informativa: si la carga falla, la operativa continúa sin bloquearse.
      setReceivedInvitations([]);
    }
  }, []);

  useEffect(() => {
    if (isAuthLoading) return;

    if (!isAuthenticated) {
      setReceivedInvitations([]);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);

    (async () => {
      await refresh();
      if (!cancelled) setIsLoading(false);
    })();

    return () => {
      cancelled = true;
    };
  }, [isAuthenticated, isAuthLoading, refresh]);

  const accept = useCallback(
    async (id: string): Promise<Workspace> => {
      const workspace = await acceptInvitationById(id);
      // La aceptada deja de ser pendiente (ya se es miembro): sale de la bandeja de inmediato.
      setReceivedInvitations((current) => current.filter((invitation) => invitation.id !== id));
      return workspace;
    },
    [acceptInvitationById]
  );

  const reject = useCallback(
    async (id: string): Promise<void> => {
      const accessToken = await getAccessTokenRef.current();
      if (!accessToken) throw new Error('Sesión no válida.');

      await invitationService.rejectReceivedInvitation(id, accessToken);
      setReceivedInvitations((current) => current.filter((invitation) => invitation.id !== id));
    },
    []
  );

  // Primera invitación no vista: la que ofrece el modal. Marcarla como vista la retira del modal
  // pero la deja en la bandeja (sigue pendiente hasta aceptar o rechazar).
  const newInvitation = receivedInvitations.find((invitation) => !seen.has(invitation.id)) ?? null;

  const dismissNew = useCallback(() => {
    if (!newInvitation) return;
    setSeen((prev) => {
      const updated = new Set(prev).add(newInvitation.id);
      persistSeen(updated);
      return updated;
    });
  }, [newInvitation]);

  const value: NotificationsContextValue = {
    receivedInvitations,
    pendingCount: receivedInvitations.length,
    isLoading,
    refresh,
    accept,
    reject,
    newInvitation,
    dismissNew,
  };

  return <NotificationsContext.Provider value={value}>{children}</NotificationsContext.Provider>;
}

export function useNotifications(): NotificationsContextValue {
  const context = useContext(NotificationsContext);
  if (!context) throw new Error('useNotifications must be used within a NotificationsProvider');
  return context;
}
