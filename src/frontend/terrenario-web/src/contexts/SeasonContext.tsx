import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { CreateSeasonPayload, Season } from '../types/season.types';
import { createSeasonService } from '../services/season.service';
import { useApiClient } from './ApiContext';
import { useWorkspace } from './WorkspaceContext';

interface SeasonContextValue {
  /** Temporada activa del Workspace en curso; `null` si ninguna lo está (RN-022). */
  activeSeason: Season | null;
  /**
   * Todas las temporadas del Workspace en curso. Se necesita para no mentir cuando **hay**
   * temporadas pero ninguna activa —estado alcanzable desde MVP-203 al cerrar la activa— y para
   * poder ofrecer activar una existente en vez de solo crear otra (MVP-208, CA-8).
   */
  seasons: Season[];
  isLoading: boolean;
  /** El usuario ya rechazó la oferta de temporada para el Workspace activo en esta sesión. */
  offerDismissed: boolean;
  /** Crea la temporada activa del Workspace en curso (MVP-201). */
  createSeason: (payload: CreateSeasonPayload) => Promise<Season>;
  /** Activa una temporada existente del Workspace en curso (MVP-203, RN-022). */
  activateSeason: (seasonId: string) => Promise<Season>;
  /** Descarta la oferta de temporada para el Workspace activo (no crea ninguna). */
  dismissOffer: () => void;
  /**
   * Resincroniza las temporadas desde el servidor. Lo usa el maestro (MVP-203) tras activar, cerrar
   * o editar una temporada, para que la cabecera y la autoselección queden coherentes.
   */
  refresh: () => Promise<void>;
}

const SeasonContext = createContext<SeasonContextValue | null>(null);

/**
 * Mantiene las temporadas del Workspace en curso (MVP-201 · MVP-203). El resto de la app decide con
 * `activeSeason` si debe ofrecer temporada (oferta cancelable) y con `seasons` **qué** ofrecer: crear
 * la primera, o activar una de las que ya hay. No se crea nada por defecto: la temporada es siempre
 * un acto explícito del usuario.
 *
 * Se carga con un único `GET /seasons` en vez de `GET /seasons/active`: la lista ya trae cuál está
 * activa (`is_active`), así que informar de las dos cosas no cuesta una petición más.
 */
export function SeasonProvider({ children }: { children: React.ReactNode }) {
  const http = useApiClient();
  const { activeWorkspace } = useWorkspace();
  const seasonService = useMemo(() => createSeasonService(http), [http]);

  const [seasons, setSeasons] = useState<Season[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  // Workspaces cuya oferta de temporada se rechazó en esta sesión (en memoria; se reofrece al recargar).
  const [dismissedWorkspaces, setDismissedWorkspaces] = useState<ReadonlySet<string>>(new Set());

  const workspaceId = activeWorkspace?.id ?? null;
  const activeSeason = useMemo(() => seasons.find((s) => s.is_active) ?? null, [seasons]);

  useEffect(() => {
    if (!workspaceId) {
      setSeasons([]);
      setIsLoading(false);
      return;
    }

    let cancelled = false;
    setIsLoading(true);

    (async () => {
      try {
        const list = await seasonService.listSeasons();
        if (!cancelled) setSeasons(list);
      } catch {
        if (!cancelled) setSeasons([]);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [workspaceId, seasonService]);

  const refresh = useCallback(async () => {
    if (!workspaceId) return;
    try {
      setSeasons(await seasonService.listSeasons());
    } catch {
      setSeasons([]);
    }
  }, [workspaceId, seasonService]);

  const createSeason = useCallback(
    async (payload: CreateSeasonPayload): Promise<Season> => {
      const season = await seasonService.createSeason(payload);
      // La nueva nace activa y desbanca a la anterior (P-017): se recarga la lista entera para que
      // el estado de las demás quede al día, no solo el de la creada.
      await refresh();
      return season;
    },
    [seasonService, refresh]
  );

  const activateSeason = useCallback(
    async (seasonId: string): Promise<Season> => {
      const season = await seasonService.activateSeason(seasonId);
      await refresh();
      return season;
    },
    [seasonService, refresh]
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
    seasons,
    isLoading,
    offerDismissed: workspaceId ? dismissedWorkspaces.has(workspaceId) : false,
    createSeason,
    activateSeason,
    dismissOffer,
    refresh,
  };

  return <SeasonContext.Provider value={value}>{children}</SeasonContext.Provider>;
}

export function useSeason(): SeasonContextValue {
  const context = useContext(SeasonContext);
  if (!context) throw new Error('useSeason must be used within a SeasonProvider');
  return context;
}
