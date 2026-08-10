import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import type { Workspace } from '../types/workspace.types';
import type { ReceivedInvitation } from '../types/invitation.types';
import type { ReactivationRequest } from '../types/workspace-lifecycle.types';
import { invitationService } from '../services/invitation.service';
import { createReactivationService } from '../services/workspace-lifecycle.service';
import { useApiClient } from './ApiContext';
import { useAuth } from './AuthContext';
import { useWorkspace } from './WorkspaceContext';

interface NotificationsContextValue {
  /** Invitaciones recibidas y accionables por la cuenta autenticada (MVP-107, HU-3). */
  receivedInvitations: ReceivedInvitation[];
  /**
   * MVP-808 (HU-2, CA-3) — Solicitudes de reactivación de Workspace que esperan la decisión de la
   * cuenta autenticada (`RN-040`). Son el segundo tipo de aviso de la bandeja: hasta ahora solo se
   * avisaban por correo, así que una decisión irreversible dependía de que ese correo llegara.
   */
  pendingReactivations: ReactivationRequest[];
  /** Avisos pendientes de los dos tipos: alimenta el contador de la campanita (CA-3). */
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

/**
 * MVP-808 (CA-2) — Intervalo mínimo entre refrescos disparados por el foco de la ventana.
 *
 * Volver a la pestaña de Terrenario dispara **dos** eventos (`visibilitychange` y `focus`), y quien
 * trabaja con varias pestañas abiertas vuelve muchas veces por minuto. Sin esta salvaguarda, cada
 * ida y vuelta sería una petición: el aviso in-app acabaría costando más tráfico que el polling que
 * el alcance descarta explícitamente.
 *
 * Treinta segundos es el orden de magnitud de lo que se quiere resolver —enterarse de algo que otra
 * persona acaba de mandar— sin acercarse a un sondeo: una sesión de una hora saltando de pestaña sin
 * parar hace como mucho 120 peticiones, frente a las miles que haría una por evento.
 */
const MIN_REFRESH_INTERVAL_MS = 30_000;

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
 * Centro de notificaciones del MVP: invitaciones recibidas (MVP-107) y solicitudes de reactivación
 * pendientes de decidir (MVP-808). Vive dentro de `WorkspaceProvider` porque aceptar una invitación
 * reemite la sesión y cambia el Workspace activo, y dentro de `ApiProvider` porque las solicitudes
 * de reactivación se leen con el cliente HTTP común.
 */
export function NotificationsProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: isAuthLoading, getAccessToken } = useAuth();
  const { acceptInvitationById } = useWorkspace();
  const http = useApiClient();

  const [receivedInvitations, setReceivedInvitations] = useState<ReceivedInvitation[]>([]);
  const [pendingReactivations, setPendingReactivations] = useState<ReactivationRequest[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [seen, setSeen] = useState<Set<string>>(() => readSeen());

  const getAccessTokenRef = useRef(getAccessToken);
  getAccessTokenRef.current = getAccessToken;

  // Mismo motivo que el token: la referencia se guarda en una ref para que `refresh` siga siendo
  // estable y no reinicie los efectos que dependen de ella en cada render.
  const reactivationService = useMemo(() => createReactivationService(http), [http]);
  const reactivationServiceRef = useRef(reactivationService);
  reactivationServiceRef.current = reactivationService;

  /** Momento del último refresco lanzado, para el intervalo mínimo del refresco por foco (CA-2). */
  const lastRefreshAtRef = useRef(0);

  const refresh = useCallback(async (): Promise<void> => {
    lastRefreshAtRef.current = Date.now();

    const accessToken = await getAccessTokenRef.current();
    if (!accessToken) {
      setReceivedInvitations([]);
      setPendingReactivations([]);
      return;
    }

    // Las dos fuentes se piden a la vez y **fallan por separado**: que no se puedan leer las
    // invitaciones no puede esconder una decisión de reactivación pendiente, ni al revés. La bandeja
    // es informativa: si una carga falla, la operativa continúa sin bloquearse.
    const [invitations, reactivations] = await Promise.all([
      invitationService
        .listReceivedInvitations(accessToken)
        .catch((): ReceivedInvitation[] => []),
      reactivationServiceRef.current
        .listPendingAuthorizations()
        .then((response) => response.data)
        .catch((): ReactivationRequest[] => []),
    ]);

    setReceivedInvitations(invitations);
    setPendingReactivations(reactivations);

    // Poda de "vistas": el almacén solo conserva ids que siguen pendientes, para no crecer sin fin.
    setSeen((current) => {
      const alive = new Set([...current].filter((id) => invitations.some((i) => i.id === id)));
      persistSeen(alive);
      return alive;
    });
  }, []);

  useEffect(() => {
    if (isAuthLoading) return;

    if (!isAuthenticated) {
      setReceivedInvitations([]);
      setPendingReactivations([]);
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

  /**
   * MVP-808 (HU-1, CA-1/CA-2) — Refresco al recuperar el foco de la ventana, con intervalo mínimo.
   *
   * `RN-006` descartó el refresco al recuperar el foco para el dashboard y aquí se acepta, y no es
   * contradictorio: allí se trata de recalcular cifras, donde el usuario decide cuándo mirar; aquí,
   * de enterarse de algo que otra persona ha mandado, que por definición no depende de cuándo mires.
   *
   * Se escuchan los dos eventos porque ninguno cubre solo el caso: `visibilitychange` no salta al
   * volver desde otra ventana de la misma pantalla y `focus` no salta en móvil al recuperar la
   * pestaña. El intervalo mínimo hace que el solape no cueste nada.
   */
  useEffect(() => {
    if (isAuthLoading || !isAuthenticated) return;

    const refreshIfStale = () => {
      if (Date.now() - lastRefreshAtRef.current < MIN_REFRESH_INTERVAL_MS) return;
      void refresh();
    };

    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') refreshIfStale();
    };

    window.addEventListener('focus', refreshIfStale);
    document.addEventListener('visibilitychange', onVisibilityChange);

    return () => {
      window.removeEventListener('focus', refreshIfStale);
      document.removeEventListener('visibilitychange', onVisibilityChange);
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
    pendingReactivations,
    pendingCount: receivedInvitations.length + pendingReactivations.length,
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
