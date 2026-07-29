import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { createActivityService } from '../../services/activity.service';
import { createPlotService } from '../../services/plot.service';
import { createTaskService } from '../../services/task.service';
import { createWorkerService } from '../../services/worker.service';
import { HttpError } from '../../services/http-client';
import {
  CONFLICT_VERSION_MISMATCH,
  type Activity,
  type CreateActivityPayload,
} from '../../types/activity.types';
import type { Plot } from '../../types/plot.types';
import type { WorkTask } from '../../types/task.types';
import type { Worker } from '../../types/worker.types';
import { ActivityFormModal } from './ActivityFormModal';

/**
 * Diario de campo del Workspace (MVP-301).
 *
 * Esta historia entrega el diario **de actividades**: el muro cronológico por fecha de negocio
 * (RN-033) y el alta y la corrección de labores. La mezcla con compras y consumos, el borrado con
 * confirmación y el filtro por tipo de registro son alcance de **MVP-305**, y la cosecha llega en
 * MVP-401: por eso la tarjeta de la entrada se construye ya con un «tipo» explícito, para que añadir
 * los otros tres no obligue a rehacerla.
 *
 * El registro exige tres maestros poblados (terreno, responsable y temporada). Si falta alguno, la
 * pantalla lo dice y enlaza a donde se resuelve, en vez de ofrecer un formulario que fallaría.
 */
export const DiarioView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const { seasons, activeSeason } = useSeason();

  const activityService = useMemo(() => createActivityService(http), [http]);
  const plotService = useMemo(() => createPlotService(http), [http]);
  const workerService = useMemo(() => createWorkerService(http), [http]);
  const taskService = useMemo(() => createTaskService(http), [http]);

  const [activities, setActivities] = useState<Activity[]>([]);
  const [plots, setPlots] = useState<Plot[]>([]);
  const [workers, setWorkers] = useState<Worker[]>([]);
  const [tasks, setTasks] = useState<WorkTask[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [plotFilter, setPlotFilter] = useState('todos');
  const [seasonFilter, setSeasonFilter] = useState('todas');
  const [searchTerm, setSearchTerm] = useState('');

  const [isModalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Activity | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      // Los maestros se piden **activos**: es lo que se ofrece para registros nuevos (MVP-202/204/205,
      // CA-3). Una actividad antigua que referencie uno inactivo se sigue leyendo sin problema, porque
      // el nombre llega resuelto desde la API.
      const [activityList, plotList, workerList, taskList] = await Promise.all([
        activityService.listActivities(),
        plotService.listPlots({ isActive: true }),
        workerService.listWorkers({ isActive: true }),
        taskService.listTasks({ isActive: true }),
      ]);
      setActivities(activityList);
      setPlots(plotList);
      setWorkers(workerList);
      setTasks(taskList);
    } catch (error) {
      setLoadError(error instanceof HttpError ? error.message : 'No se pudo cargar el diario.');
    } finally {
      setIsLoading(false);
    }
  }, [activityService, plotService, workerService, taskService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const visibleActivities = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    return activities.filter((activity) => {
      if (plotFilter !== 'todos' && activity.plot_id !== plotFilter) return false;
      if (seasonFilter !== 'todas' && activity.season_id !== seasonFilter) return false;
      if (!term) return true;
      return [activity.task, activity.plot_name, activity.worker_name, activity.description ?? '']
        .some((field) => field.toLowerCase().includes(term));
    });
  }, [activities, plotFilter, seasonFilter, searchTerm]);

  const missingMasters = useMemo(() => {
    const missing: { label: string; to: string }[] = [];
    if (plots.length === 0) missing.push({ label: 'un terreno', to: '/app/terrenos' });
    if (workers.length === 0) missing.push({ label: 'un responsable', to: '/app/trabajadores' });
    if (seasons.length === 0) missing.push({ label: 'una temporada', to: '/app/temporadas' });
    return missing;
  }, [plots, workers, seasons]);

  const openCreate = () => {
    setEditing(null);
    setFormError(null);
    setModalOpen(true);
  };

  const openEdit = (activity: Activity) => {
    setEditing(activity);
    setFormError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (payload: CreateActivityPayload) => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editing) {
        await activityService.updateActivity(editing.id, editing.version, payload);
      } else {
        await activityService.createActivity(payload);
      }
      setModalOpen(false);
      setEditing(null);
      await reload();
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        // ADR-0005 — otra persona tocó el registro: se refresca el diario y se explica qué pasó,
        // en vez de dejar al usuario con un formulario que ya no puede guardar (CA-4).
        setModalOpen(false);
        setEditing(null);
        await reload();
        setLoadError(
          'Otra persona modificó esa actividad mientras la editabas. Se ha recargado el diario con la versión actual; revisa el cambio y vuelve a aplicarlo si hace falta.'
        );
        return;
      }
      setFormError(
        error instanceof HttpError ? error.message : 'No se pudo guardar la actividad.'
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Diario de campo</h2>
          <p className="text-xs text-[#76786b]">
            Lo que se ha hecho en la explotación, en orden cronológico. Las compras y los consumos se
            incorporarán a este mismo muro.
          </p>
        </div>

        <button
          type="button"
          onClick={openCreate}
          disabled={missingMasters.length > 0}
          title={missingMasters.length > 0 ? 'Faltan maestros por poblar' : undefined}
          className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed shrink-0"
        >
          <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
          <span>Nueva actividad</span>
        </button>
      </div>

      {/* Registrar exige maestros: se dice qué falta y se enlaza, en vez de fallar al guardar */}
      {!isLoading && missingMasters.length > 0 && (
        <div className="bg-[#fff6e5] border border-[#f0d9a8] rounded-2xl p-4 space-y-2">
          <p className="text-sm font-semibold text-[#8a5a00] flex items-center gap-1.5">
            <span className="material-symbols-outlined text-lg" aria-hidden="true">info</span>
            Antes de registrar necesitas {missingMasters.map((m) => m.label).join(' y ')}.
          </p>
          <div className="flex flex-wrap gap-2">
            {missingMasters.map((missing) => (
              <button
                key={missing.to}
                type="button"
                onClick={() => navigate(missing.to)}
                className="px-3 py-1.5 rounded-lg bg-white border border-[#f0d9a8] text-xs font-semibold text-[#8a5a00] hover:bg-[#fdf0d8]"
              >
                Añadir {missing.label}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Filtros */}
      {activities.length > 0 && (
        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="relative">
            <span className="material-symbols-outlined absolute left-3 top-2.5 text-[#76786b] text-lg" aria-hidden="true">search</span>
            <label htmlFor="diary-search" className="sr-only">Buscar en el diario</label>
            <input
              id="diary-search"
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar en el diario…"
              className="w-full pl-9 pr-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            />
          </div>

          <div>
            <label htmlFor="diary-plot" className="sr-only">Filtrar por terreno</label>
            <select
              id="diary-plot"
              value={plotFilter}
              onChange={(e) => setPlotFilter(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los terrenos</option>
              {plots.map((plot) => (
                <option key={plot.id} value={plot.id}>{plot.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="diary-season" className="sr-only">Filtrar por temporada</label>
            <select
              id="diary-season"
              value={seasonFilter}
              onChange={(e) => setSeasonFilter(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todas">Todas las temporadas</option>
              {seasons.map((season) => (
                <option key={season.id} value={season.id}>{season.name}</option>
              ))}
            </select>
          </div>
        </div>
      )}

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {/* Muro cronológico */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : activities.length === 0 ? (
        <EmptyDiary canRegister={missingMasters.length === 0} onRegister={openCreate} />
      ) : visibleActivities.length === 0 ? (
        <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
          No hay registros que coincidan con los filtros.
        </p>
      ) : (
        <ol className="relative pl-6 space-y-4 before:absolute before:left-3.5 before:top-3 before:bottom-3 before:w-0.5 before:bg-[#c6c8b8]">
          {visibleActivities.map((activity) => (
            <ActivityCard key={activity.id} activity={activity} onEdit={() => openEdit(activity)} />
          ))}
        </ol>
      )}

      <ActivityFormModal
        isOpen={isModalOpen}
        activity={editing}
        plots={plots}
        workers={workers}
        tasks={tasks}
        seasons={seasons}
        activeSeason={activeSeason}
        isSubmitting={isSubmitting}
        errorMessage={formError}
        onClose={() => {
          setModalOpen(false);
          setEditing(null);
        }}
        onSubmit={(payload) => void handleSubmit(payload)}
      />
    </div>
  );
};

/** Formato de fecha del muro: corto y legible, sin depender del locale del navegador. */
function formatDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

const ActivityCard: React.FC<{ activity: Activity; onEdit: () => void }> = ({ activity, onEdit }) => (
  <li className="relative">
    {/* Nodo del timeline. El icono identifica el tipo de entrada: MVP-305 añadirá compra y consumo,
        y MVP-401 la cosecha, sobre esta misma tarjeta. */}
    <div className="absolute -left-6 top-4 w-7 h-7 rounded-full bg-[#4a5d23] text-white flex items-center justify-center shadow-md ring-4 ring-[#fcf9f4]">
      <span className="material-symbols-outlined text-base" aria-hidden="true">content_cut</span>
    </div>

    <div className="bg-white rounded-2xl border border-[#e5e2dd] p-5 ambient-shadow space-y-3 ml-3">
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-[#4a5d23] text-white uppercase tracking-wider">
              Labor
            </span>
            <span className="text-xs font-bold text-[#33450d]">{formatDate(activity.date)}</span>
            <span className="text-[11px] text-[#76786b]">· {activity.season_name}</span>
            {activity.is_out_of_season_range && (
              <span
                title="La fecha queda fuera del rango de la temporada"
                className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8]"
              >
                FUERA DE TEMPORADA
              </span>
            )}
          </div>
          <h3 className="font-headline font-bold text-lg text-[#1c1c19] tracking-tight truncate">
            {activity.task}
          </h3>
        </div>

        <button
          type="button"
          onClick={onEdit}
          title="Corregir actividad"
          aria-label={`Corregir ${activity.task}`}
          className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
        </button>
      </div>

      {activity.description && (
        <p className="text-xs sm:text-sm text-[#45483c] leading-relaxed">{activity.description}</p>
      )}

      <div className="flex items-center gap-4 text-xs font-semibold text-[#1c1c19] flex-wrap pt-1 border-t border-[#f0ede8]">
        <span className="flex items-center gap-1 text-[#33450d]">
          <span className="material-symbols-outlined text-base" aria-hidden="true">location_on</span>
          {activity.plot_name}
        </span>
        <span className="flex items-center gap-1 text-[#45483c]">
          <span className="material-symbols-outlined text-base" aria-hidden="true">person</span>
          {activity.worker_name}
        </span>
        <span className="flex items-center gap-1 text-[#76786b]">
          <span className="material-symbols-outlined text-base" aria-hidden="true">schedule</span>
          {activity.hours} h
        </span>
        <span className="flex items-center gap-1 text-[#ba1a1a] font-bold">
          <span className="material-symbols-outlined text-base" aria-hidden="true">payments</span>
          {activity.manual_cost.toLocaleString('es-ES', { minimumFractionDigits: 2 })} €
        </span>
      </div>
    </div>
  </li>
);

const EmptyDiary: React.FC<{ canRegister: boolean; onRegister: () => void }> = ({
  canRegister,
  onRegister,
}) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-4">
    <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-3xl" aria-hidden="true">event_note</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Tu diario está vacío</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">
        Apunta la primera labor: qué se hizo, quién la hizo, cuánto duró y cuánto costó. Con eso ya
        tienes trazabilidad de la campaña.
      </p>
    </div>
    {canRegister && (
      <button
        type="button"
        onClick={onRegister}
        className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
      >
        <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
        Registrar actividad
      </button>
    )}
  </div>
);
