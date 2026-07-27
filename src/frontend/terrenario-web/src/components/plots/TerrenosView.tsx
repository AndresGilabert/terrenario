import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useApiClient } from '../../contexts/ApiContext';
import { createPlotService } from '../../services/plot.service';
import { HttpError } from '../../services/http-client';
import { PLOT_OWNERSHIP_LABELS, type CreatePlotPayload, type Plot } from '../../types/plot.types';
import { PlotFormModal } from './PlotFormModal';

/**
 * Maestro de terrenos del Workspace (MVP-202). Lista, alta, edición e inactivación con la mínima
 * fricción (RN-028): el alta solo exige nombre y tipo de propiedad. Reutiliza el shell y la paleta
 * del prototipo (`TerrenosView`), pero los campos son los de la KB, no los inventados por el
 * prototipo (olivos/riego/poda). La ausencia de nº de árboles se muestra como aviso informativo, sin
 * bloquear (RN-010), anticipando el "dato incompleto" del dashboard.
 */
export const TerrenosView: React.FC = () => {
  const http = useApiClient();
  const plotService = useMemo(() => createPlotService(http), [http]);

  const [plots, setPlots] = useState<Plot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showInactive, setShowInactive] = useState(false);

  const [isModalOpen, setModalOpen] = useState(false);
  const [editingPlot, setEditingPlot] = useState<Plot | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [busyPlotId, setBusyPlotId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      // Traemos activos + inactivos y filtramos en cliente para una respuesta ágil; el filtro de
      // servidor (`is_active`) queda disponible y probado para otros consumidores.
      const data = await plotService.listPlots();
      setPlots(data);
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudieron cargar los terrenos.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [plotService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const visiblePlots = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    return plots.filter((p) => {
      if (!showInactive && !p.is_active) return false;
      if (!term) return true;
      return (
        p.name.toLowerCase().includes(term) ||
        (p.alias?.toLowerCase().includes(term) ?? false) ||
        (p.location?.toLowerCase().includes(term) ?? false)
      );
    });
  }, [plots, searchTerm, showInactive]);

  const activeCount = useMemo(() => plots.filter((p) => p.is_active).length, [plots]);
  const inactiveCount = plots.length - activeCount;

  const openCreate = () => {
    setEditingPlot(null);
    setSubmitError(null);
    setModalOpen(true);
  };

  const openEdit = (plot: Plot) => {
    setEditingPlot(plot);
    setSubmitError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (payload: CreatePlotPayload) => {
    setSubmitting(true);
    setSubmitError(null);
    try {
      if (editingPlot) {
        await plotService.updatePlot(editingPlot.id, { ...payload, is_active: editingPlot.is_active });
      } else {
        await plotService.createPlot(payload);
      }
      setModalOpen(false);
      setEditingPlot(null);
      await reload();
    } catch (error) {
      setSubmitError(
        error instanceof HttpError ? error.message : 'No se pudo guardar el terreno. Inténtalo de nuevo.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  const toggleActive = async (plot: Plot) => {
    setBusyPlotId(plot.id);
    try {
      await plotService.updatePlot(plot.id, {
        name: plot.name,
        ownership_type: plot.ownership_type,
        alias: plot.alias,
        owner_name: plot.owner_name,
        cadastral_reference: plot.cadastral_reference,
        location: plot.location,
        tree_count: plot.tree_count,
        is_active: !plot.is_active,
      });
      await reload();
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudo cambiar el estado del terreno.'
      );
    } finally {
      setBusyPlotId(null);
    }
  };

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Cabecera */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Terrenos y parcelas</h2>
          <p className="text-xs text-[#76786b]">
            Da de alta tus parcelas para empezar a registrar labores y cosechas. Solo el nombre y el
            tipo de propiedad son obligatorios.
          </p>
        </div>

        <button
          onClick={openCreate}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
          <span>Añadir terreno</span>
        </button>
      </div>

      {/* Búsqueda y filtros */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="flex-1 bg-white p-3 rounded-2xl border border-[#e5e2dd] flex items-center gap-3">
          <span className="material-symbols-outlined text-[#76786b] pl-2" aria-hidden="true">search</span>
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Buscar por nombre, alias o ubicación…"
            className="w-full bg-transparent text-xs font-medium text-[#1c1c19] focus:outline-none"
          />
          {searchTerm && (
            <button onClick={() => setSearchTerm('')} className="text-xs text-[#76786b] pr-2">
              Limpiar
            </button>
          )}
        </div>

        {inactiveCount > 0 && (
          <button
            type="button"
            onClick={() => setShowInactive((v) => !v)}
            aria-pressed={showInactive}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-2xl border text-xs font-semibold transition-colors ${
              showInactive
                ? 'bg-[#33450d] text-white border-[#33450d]'
                : 'bg-white text-[#45483c] border-[#e5e2dd] hover:bg-[#f0ede8]'
            }`}
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">
              {showInactive ? 'visibility' : 'visibility_off'}
            </span>
            <span>Inactivos ({inactiveCount})</span>
          </button>
        )}
      </div>

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {/* Contenido */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : plots.length === 0 ? (
        <EmptyState onAdd={openCreate} />
      ) : visiblePlots.length === 0 ? (
        <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
          No hay terrenos que coincidan con la búsqueda.
        </p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {visiblePlots.map((plot) => (
            <PlotCard
              key={plot.id}
              plot={plot}
              isBusy={busyPlotId === plot.id}
              onEdit={() => openEdit(plot)}
              onToggleActive={() => void toggleActive(plot)}
            />
          ))}

          <button
            onClick={openCreate}
            className="min-h-[210px] bg-[#f6f3ee] hover:bg-[#f0ede8] rounded-2xl border-2 border-dashed border-[#c6c8b8] p-6 flex flex-col items-center justify-center text-center gap-3 transition-colors group"
          >
            <div className="w-12 h-12 rounded-full bg-white text-[#33450d] flex items-center justify-center shadow-md group-hover:scale-110 transition-transform">
              <span className="material-symbols-outlined text-2xl" aria-hidden="true">add_location_alt</span>
            </div>
            <div>
              <h3 className="font-headline font-bold text-base text-[#1c1c19]">Añadir parcela</h3>
              <p className="text-xs text-[#76786b] max-w-xs mt-1">
                Registra un nuevo terreno con el nombre y el tipo de propiedad.
              </p>
            </div>
          </button>
        </div>
      )}

      <PlotFormModal
        isOpen={isModalOpen}
        plot={editingPlot}
        isSubmitting={isSubmitting}
        errorMessage={submitError}
        onClose={() => {
          if (!isSubmitting) {
            setModalOpen(false);
            setEditingPlot(null);
          }
        }}
        onSubmit={(payload) => void handleSubmit(payload)}
      />
    </div>
  );
};

const EmptyState: React.FC<{ onAdd: () => void }> = ({ onAdd }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-10 text-center ambient-shadow space-y-4">
    <div className="w-16 h-16 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-4xl" aria-hidden="true">map</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Aún no tienes terrenos</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">
        Los terrenos son la base de tus registros: toda labor, compra y cosecha se asocia a uno.
        Empieza con lo mínimo, el nombre y el tipo de propiedad.
      </p>
    </div>
    <button
      onClick={onAdd}
      className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
    >
      <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
      Añadir mi primer terreno
    </button>
  </div>
);

interface PlotCardProps {
  plot: Plot;
  isBusy: boolean;
  onEdit: () => void;
  onToggleActive: () => void;
}

const PlotCard: React.FC<PlotCardProps> = ({ plot, isBusy, onEdit, onToggleActive }) => {
  const incomplete = !plot.has_tree_count;

  return (
    <div
      className={`bg-white rounded-2xl border p-5 flex flex-col justify-between gap-4 shadow-2xs transition-all ${
        plot.is_active ? 'border-[#e5e2dd]' : 'border-[#dcd9d2] bg-[#faf8f4] opacity-90'
      }`}
    >
      <div className="space-y-3">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              {plot.alias && (
                <span className="text-[11px] font-bold px-2 py-0.5 rounded-md bg-[#f0ede8] text-[#33450d]">
                  {plot.alias}
                </span>
              )}
              <span className="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-[#eef2e0] text-[#33450d]">
                {PLOT_OWNERSHIP_LABELS[plot.ownership_type]}
              </span>
              {!plot.is_active && (
                <span className="text-[11px] font-bold px-2 py-0.5 rounded-md bg-[#e5e2dd] text-[#76786b]">
                  Inactivo
                </span>
              )}
            </div>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19] tracking-tight mt-1.5 truncate">
              {plot.name}
            </h3>
            {plot.location && (
              <p className="text-xs text-[#76786b] flex items-center gap-1 mt-0.5">
                <span className="material-symbols-outlined text-sm" aria-hidden="true">location_on</span>
                <span className="truncate">{plot.location}</span>
              </p>
            )}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2 text-xs bg-[#f6f3ee] p-3 rounded-xl border border-[#e5e2dd]">
          <div>
            <p className="text-[10px] font-bold text-[#76786b] uppercase">Nº de árboles</p>
            {plot.tree_count != null ? (
              <p className="font-extrabold text-[#1c1c19]">{plot.tree_count.toLocaleString('es-ES')}</p>
            ) : (
              <p className="font-semibold text-[#8a6d1a] flex items-center gap-1">
                <span className="material-symbols-outlined text-sm" aria-hidden="true">error</span>
                Sin registrar
              </p>
            )}
          </div>
          <div className="min-w-0">
            <p className="text-[10px] font-bold text-[#76786b] uppercase">Propietario</p>
            <p className="font-semibold text-[#45483c] truncate">{plot.owner_name ?? '—'}</p>
          </div>
        </div>

        {incomplete && (
          <p className="text-[11px] text-[#8a6d1a] bg-[#fdf6e3] border border-[#f0e2b8] rounded-lg px-2.5 py-1.5 flex items-center gap-1.5">
            <span className="material-symbols-outlined text-sm" aria-hidden="true">info</span>
            Añade el nº de árboles para habilitar los KPIs por árbol del dashboard.
          </p>
        )}
      </div>

      <div className="pt-3 border-t border-[#f0ede8] flex items-center justify-between gap-2">
        <button
          onClick={onToggleActive}
          disabled={isBusy}
          className="text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-50 flex items-center gap-1"
        >
          <span className="material-symbols-outlined text-base" aria-hidden="true">
            {plot.is_active ? 'toggle_off' : 'toggle_on'}
          </span>
          {plot.is_active ? 'Inactivar' : 'Reactivar'}
        </button>

        <button
          onClick={onEdit}
          className="px-3.5 py-1.5 rounded-xl text-xs font-bold bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] flex items-center gap-1"
        >
          <span className="material-symbols-outlined text-sm" aria-hidden="true">edit</span>
          Editar
        </button>
      </div>
    </div>
  );
};
