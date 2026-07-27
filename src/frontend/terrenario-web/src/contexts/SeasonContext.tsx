import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { CreateSeasonPayload, Season } from '../types/season.types';
import { createSeasonService } from '../services/season.service';
import { useApiClient } from './ApiContext';
import { useWorkspace } from './WorkspaceContext';

interface SeasonContextValue {
  /** Temporada activa del Workspace en curso; `null` si aún no tiene ninguna. */
  activeSeason: Season | null;
  isLoading: boolean;
  /** El usuario ya rechazó la oferta de crear temporada para el Workspace activo en esta sesión. */
  offerDismissed: boolean;
  /** Crea la temporada activa del Workspace en curso (MVP-201). */
  createSeason: (payload: CreateSeasonPayload) => Promise<Season>;
  /** Descarta la oferta de temporada para el Workspace activo (no crea ninguna). */
  dismissOffer: () => void;
}

const SeasonContext = createContext<SeasonContextValue | null>(null);

/**
 * Mantiene la temporada activa del Workspace en curso (MVP-201). El resto de la app decide con
 * `activeSeason` si debe ofrecer crear una temporada (cancelable). No se crea nada por defecto: la
 * temporada es siempre un acto explícito del usuario.
 */
export function SeasonProvider({ children }: { children: React.ReactNode }) {
  const http = useApiClient();
  const { activeWorkspace } = useWorkspace();
  const seasonService = useMemo(() => createSeasonService(http), [http]);

  const [activeSeason, setActiveSeason] = useState<Season | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  // Workspaces cuya oferta de temporada se rechazó en esta sesión (en memoria; se reofrece al recargar).
  const [dismissedWorkspaces, setDismissedWorkspaces] = useState<ReadonlySet<string>>(new Set());

  const workspaceId = activeWorkspace?.id ?? null;

  useEffect(() => {
    if (!workspaceId) {
      setActiveSeason(null);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);

    (async () => {
      try {
        const season = await seasonService.getActiveSeason();
        if (!cancelled) setActiveSeason(season);
      } catch {
        if (!cancelled) setActiveSeason(null);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [workspaceId, seasonService]);

  const createSeason = useCallback(
    async (payload: CreateSeasonPayload): Promise<Season> => {
      const season = await seasonService.createSeason(payload);
      setActiveSeason(season);
      return season;
    },
    [seasonService]
  );

  const dismissOffer = useCallback(() => {
    if (!workspaceId) return;
    setDismissedWorkspaces((prev) => {
      const next = new Set(prev);
      next.add(workspaceId);
      return next;
    });
  }, [workspaceId]);

  const value: SeasonContextValue = {
    activeSeason,
    isLoading,
    offerDismissed: workspaceId ? dismissedWorkspaces.has(workspaceId) : false,
    createSeason,
    dismissOffer,
  };

  return <SeasonContext.Provider value={value}>{children}</SeasonContext.Provider>;
}

export function useSeason(): SeasonContextValue {
  const context = useContext(SeasonContext);
  if (!context) throw new Error('useSeason must be used within a SeasonProvider');
  return context;
}
