import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { createDashboardService } from '../../services/dashboard.service';
import { createSeasonService } from '../../services/season.service';
import { HttpError } from '../../services/http-client';
import { SEASON_STATUS_LABELS, type Season } from '../../types/season.types';
import { SeasonFormModal, type SeasonFormValues } from './SeasonFormModal';

/**
 * Maestro de temporadas del Workspace (MVP-203). Lista las campañas, permite crear (la nueva pasa a
 * ser la activa), editar, cambiar de activa (RN-022) y cerrar/reabrir (RN-024, informativo). Reutiliza
 * el shell y la paleta del prototipo (`TemporadasView`).
 *
 * **MVP-403 cierra `P-021`**: cada tarjeta muestra ya la producción agregada de su campaña, que
 * `MVP-203` omitió deliberadamente porque `HARVEST` todavía no existía y no quiso inventar métricas.
 * Llega en **una sola petición** (`GET /dashboard/kg-by-season`), no una por temporada, y su fallo no
 * tumba el maestro: si no se puede calcular, las tarjetas se pintan sin el dato.
 *
 * Tras cada acción resincroniza la temporada activa del contexto para que la cabecera y la
 * autoselección operativa queden coherentes. Esta pantalla vive fuera de la guarda de oferta de
 * temporada, así que es siempre accesible aunque el Workspace no tenga ninguna activa.
 */
export const TemporadasView: React.FC = () => {
  const http = useApiClient();
  const seasonService = useMemo(() => createSeasonService(http), [http]);
  const dashboardService = useMemo(() => createDashboardService(http), [http]);
  const { refresh: refreshActiveSeason } = useSeason();

  const [seasons, setSeasons] = useState<Season[]>([]);
  /** P-021 — kilos recolectados por temporada, indexados por id. Vacío si no se pudo calcular. */
  const [production, setProduction] = useState<Record<string, { kg: number; harvests: number }>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const [isModalOpen, setModalOpen] = useState(false);
  const [editingSeason, setEditingSeason] = useState<Season | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [busySeasonId, setBusySeasonId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const data = await seasonService.listSeasons();
      setSeasons(data);
    } catch (error) {
      setLoadError(error instanceof HttpError ? error.message : 'No se pudieron cargar las temporadas.');
      setIsLoading(false);
      return;
    }

    // P-021 — la producción es un enriquecimiento, no el maestro: si falla, el maestro sigue en pie.
    try {
      const { data } = await dashboardService.getKgBySeason();
      setProduction(
        Object.fromEntries(
          data.map((row) => [row.season_id, { kg: row.total_kg, harvests: row.harvests }])
        )
      );
    } catch {
      setProduction({});
    } finally {
      setIsLoading(false);
    }
  }, [seasonService, dashboardService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const hasActiveSeason = useMemo(() => seasons.some((s) => s.is_active), [seasons]);

  const openCreate = () => {
    setEditingSeason(null);
    setSubmitError(null);
    setModalOpen(true);
  };

  const openEdit = (season: Season) => {
    setEditingSeason(season);
    setSubmitError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (values: SeasonFormValues) => {
    setSubmitting(true);
    setSubmitError(null);
    try {
      if (editingSeason) {
        await seasonService.updateSeason(editingSeason.id, values);
      } else {
        await seasonService.createSeason(values);
      }
      setModalOpen(false);
      setEditingSeason(null);
      await reload();
      await refreshActiveSeason();
    } catch (error) {
      setSubmitError(
        error instanceof HttpError ? error.message : 'No se pudo guardar la temporada. Inténtalo de nuevo.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  /** Ejecuta una acción de estado (activar/cerrar/reabrir) y resincroniza lista + contexto. */
  const runAction = async (season: Season, action: () => Promise<unknown>) => {
    setBusySeasonId(season.id);
    setActionError(null);
    try {
      await action();
      await reload();
      await refreshActiveSeason();
    } catch (error) {
      setActionError(error instanceof HttpError ? error.message : 'No se pudo completar la acción.');
    } finally {
      setBusySeasonId(null);
    }
  };

  const activate = (season: Season) => runAction(season, () => seasonService.activateSeason(season.id));
  const close = (season: Season) => runAction(season, () => seasonService.updateSeason(season.id, { is_closed: true }));
  const reopen = (season: Season) => runAction(season, () => seasonService.updateSeason(season.id, { is_closed: false }));

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      {/* Cabecera */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Temporadas y campañas</h2>
          <p className="text-xs text-[#76786b]">
            Agrupa cosechas, labores y compras por campaña. Solo puede haber una temporada activa a la vez.
          </p>
        </div>

        <button
          onClick={openCreate}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
          <span>Nueva temporada</span>
        </button>
      </div>

      {(loadError || actionError) && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError ?? actionError}
        </div>
      )}

      {/* Contenido */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : seasons.length === 0 ? (
        <EmptyState onAdd={openCreate} />
      ) : (
        <div className="space-y-3">
          {seasons.map((season) => (
            <SeasonCard
              key={season.id}
              season={season}
              production={production[season.id]}
              isBusy={busySeasonId === season.id}
              onEdit={() => openEdit(season)}
              onActivate={() => void activate(season)}
              onClose={() => void close(season)}
              onReopen={() => void reopen(season)}
            />
          ))}
        </div>
      )}

      <SeasonFormModal
        isOpen={isModalOpen}
        season={editingSeason}
        hasActiveSeason={hasActiveSeason}
        isSubmitting={isSubmitting}
        errorMessage={submitError}
        onClose={() => {
          if (!isSubmitting) {
            setModalOpen(false);
            setEditingSeason(null);
          }
        }}
        onSubmit={(values) => void handleSubmit(values)}
      />
    </div>
  );
};

const EmptyState: React.FC<{ onAdd: () => void }> = ({ onAdd }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-10 text-center ambient-shadow space-y-4">
    <div className="w-16 h-16 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-4xl" aria-hidden="true">calendar_today</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Aún no tienes temporadas</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">
        La temporada es el eje temporal del que cuelgan cosechas, labores y compras. Crea la primera
        para empezar a organizar tu campaña.
      </p>
    </div>
    <button
      onClick={onAdd}
      className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
    >
      <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
      Crear mi primera temporada
    </button>
  </div>
);

interface SeasonCardProps {
  season: Season;
  /** P-021 — producción agregada de la campaña. `undefined` si no se pudo calcular. */
  production?: { kg: number; harvests: number };
  isBusy: boolean;
  onEdit: () => void;
  onActivate: () => void;
  onClose: () => void;
  onReopen: () => void;
}

const STATUS_BADGE: Record<Season['status'], string> = {
  activa: 'bg-[#c9f16f] text-[#33450d]',
  planificada: 'bg-[#eef2e0] text-[#33450d]',
  cerrada: 'bg-[#e5e2dd] text-[#76786b]',
};

function formatDate(iso: string): string {
  const parsed = new Date(`${iso}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) return iso;
  return parsed.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
}

const SeasonCard: React.FC<SeasonCardProps> = ({
  season,
  production,
  isBusy,
  onEdit,
  onActivate,
  onClose,
  onReopen,
}) => {
  const range = season.end_date
    ? `${formatDate(season.start_date)} — ${formatDate(season.end_date)}`
    : `Desde ${formatDate(season.start_date)}`;

  return (
    <div
      className={`bg-white p-5 rounded-2xl border flex flex-col sm:flex-row sm:items-center justify-between gap-4 transition-all ${
        season.is_active ? 'border-[#33450d] ring-2 ring-[#33450d]/20' : 'border-[#e5e2dd]'
      }`}
    >
      <div className="flex items-center gap-4 min-w-0">
        <div
          className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 ${
            season.is_active ? 'bg-[#33450d] text-white' : 'bg-[#f0ede8] text-[#76786b]'
          }`}
        >
          <span className="material-symbols-outlined text-xl" aria-hidden="true">calendar_today</span>
        </div>
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h3 className="font-headline font-bold text-base text-[#1c1c19] truncate">{season.name}</h3>
            <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${STATUS_BADGE[season.status]}`}>
              {SEASON_STATUS_LABELS[season.status].toUpperCase()}
            </span>
          </div>
          <p className="text-xs text-[#76786b]">{range}</p>
          {/* P-021 — producción agregada de la campaña. `0 kg` es información («no se recolectó
              nada»), no ausencia de dato; ausencia de dato es que no se pudo calcular y entonces no
              se enseña nada. */}
          {production && (
            <p className="text-xs font-semibold text-[#33450d] flex items-center gap-1 mt-0.5">
              <span className="material-symbols-outlined text-sm" aria-hidden="true">scale</span>
              {production.kg.toLocaleString('es-ES')} kg
              <span className="font-normal text-[#76786b]">
                · {production.harvests === 0
                  ? 'sin cosechas'
                  : `${production.harvests} ${production.harvests === 1 ? 'partida' : 'partidas'}`}
              </span>
            </p>
          )}
        </div>
      </div>

      <div className="flex items-center gap-2 shrink-0 flex-wrap">
        {!season.is_active && (
          <button
            onClick={onActivate}
            disabled={isBusy}
            className="px-3.5 py-1.5 rounded-xl bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] text-xs font-bold disabled:opacity-50 flex items-center gap-1"
          >
            <span className="material-symbols-outlined text-sm" aria-hidden="true">bolt</span>
            Activar
          </button>
        )}

        {season.is_active && (
          <button
            onClick={onClose}
            disabled={isBusy}
            className="px-3.5 py-1.5 rounded-xl bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#45483c] text-xs font-bold disabled:opacity-50 flex items-center gap-1"
            title="Marcar como cerrada (informativo). El Workspace quedará sin temporada activa."
          >
            <span className="material-symbols-outlined text-sm" aria-hidden="true">lock</span>
            Cerrar
          </button>
        )}

        {season.is_closed && (
          <button
            onClick={onReopen}
            disabled={isBusy}
            className="px-3.5 py-1.5 rounded-xl bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#45483c] text-xs font-bold disabled:opacity-50 flex items-center gap-1"
            title="Reabrir: vuelve a planificada"
          >
            <span className="material-symbols-outlined text-sm" aria-hidden="true">lock_open</span>
            Reabrir
          </button>
        )}

        <button
          onClick={onEdit}
          disabled={isBusy}
          className="px-3.5 py-1.5 rounded-xl text-xs font-bold bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] disabled:opacity-50 flex items-center gap-1"
        >
          <span className="material-symbols-outlined text-sm" aria-hidden="true">edit</span>
          Editar
        </button>
      </div>
    </div>
  );
};
