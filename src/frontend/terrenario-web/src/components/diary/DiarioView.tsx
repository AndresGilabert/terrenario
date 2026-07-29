import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { createActivityService } from '../../services/activity.service';
import { createConsumptionService } from '../../services/consumption.service';
import { createDiaryService } from '../../services/diary.service';
import { createPlotService } from '../../services/plot.service';
import { createPurchaseService } from '../../services/purchase.service';
import { createTaskService } from '../../services/task.service';
import { createWorkerService } from '../../services/worker.service';
import { HttpError } from '../../services/http-client';
import {
  CONFLICT_VERSION_MISMATCH,
  RESOURCE_NOT_FOUND,
  TASK_CATALOG_OUTCOME_MESSAGES,
  type Activity,
  type CreateActivityPayload,
} from '../../types/activity.types';
import {
  DIARY_ENTRY_NOUNS,
  DIARY_ENTRY_STYLES,
  type DiaryEntry,
  type DiaryEntryType,
  type DiaryListResponse,
} from '../../types/diary.types';
import type { Plot } from '../../types/plot.types';
import type { WorkTask } from '../../types/task.types';
import type { Worker } from '../../types/worker.types';
import { ConfirmDialog } from '../common/ConfirmDialog';
import { ActivityFormModal } from './ActivityFormModal';

const EMPTY_SUMMARY: DiaryListResponse['meta'] = {
  total: 0,
  total_cost: 0,
  activities: 0,
  purchases: 0,
  consumptions: 0,
  consumptions_without_purchase: 0,
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

const euros = (value: number) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * Diario de campo del Workspace (MVP-305): **la vista principal del MVP** (RN-033).
 *
 * Mezcla actividades, compras y consumos en un solo muro ordenado por **fecha de negocio** —no por
 * fecha de captura—, que es lo que convierte la aplicación en una experiencia de diario y no en tres
 * listados aislados (CA-1/CA-2). La mezcla y el orden los hace el servidor (`GET /api/v1/diary`).
 *
 * El **borrado exige confirmación explícita** (RN-037, CA-3) y es **lógico**: el registro desaparece
 * del diario y de los listados, pero no se pierde en base de datos. No hay papelera ni deshacer en el
 * MVP, así que la confirmación dice qué se elimina antes de hacerlo.
 *
 * La **cosecha** todavía no aparece porque `HARVEST` no existe hasta MVP-004: encenderla es alcance
 * de `MVP-401` (hallazgo `G-4`), y la tarjeta está construida para que sea un tipo más.
 */
export const DiarioView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const { seasons, activeSeason } = useSeason();

  const diaryService = useMemo(() => createDiaryService(http), [http]);
  const activityService = useMemo(() => createActivityService(http), [http]);
  const purchaseService = useMemo(() => createPurchaseService(http), [http]);
  const consumptionService = useMemo(() => createConsumptionService(http), [http]);
  const plotService = useMemo(() => createPlotService(http), [http]);
  const workerService = useMemo(() => createWorkerService(http), [http]);
  const taskService = useMemo(() => createTaskService(http), [http]);

  const [entries, setEntries] = useState<DiaryEntry[]>([]);
  const [summary, setSummary] = useState<DiaryListResponse['meta']>(EMPTY_SUMMARY);
  const [plots, setPlots] = useState<Plot[]>([]);
  const [workers, setWorkers] = useState<Worker[]>([]);
  const [tasks, setTasks] = useState<WorkTask[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const [plotFilter, setPlotFilter] = useState('todos');
  const [seasonFilter, setSeasonFilter] = useState('todas');
  const [typeFilter, setTypeFilter] = useState<DiaryEntryType | 'todos'>('todos');
  const [searchTerm, setSearchTerm] = useState('');

  const [isModalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Activity | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [busyEntryId, setBusyEntryId] = useState<string | null>(null);
  /** Registro pendiente de confirmación de borrado (RN-037, CA-3). */
  const [pendingDelete, setPendingDelete] = useState<DiaryEntry | null>(null);
  const [isDeleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const [diary, plotList, workerList, taskList] = await Promise.all([
        diaryService.listDiary({
          plotId: plotFilter === 'todos' ? undefined : plotFilter,
          seasonId: seasonFilter === 'todas' ? undefined : seasonFilter,
          types: typeFilter === 'todos' ? undefined : [typeFilter],
        }),
        // Los maestros se piden activos: es lo que se ofrece para registros nuevos. El catálogo de
        // tareas se trae entero por el aviso de duplicado de MVP-302.
        plotService.listPlots({ isActive: true }),
        workerService.listWorkers({ isActive: true }),
        taskService.listTasks(),
      ]);
      setEntries(diary.data);
      setSummary(diary.meta);
      setPlots(plotList);
      setWorkers(workerList);
      setTasks(taskList);
    } catch (error) {
      setLoadError(error instanceof HttpError ? error.message : 'No se pudo cargar el diario.');
    } finally {
      setIsLoading(false);
    }
  }, [diaryService, plotService, workerService, taskService, plotFilter, seasonFilter, typeFilter]);

  useEffect(() => {
    void reload();
  }, [reload]);

  /** La búsqueda es local sobre lo ya filtrado en servidor (ver `MVP-999`, `P-052`). */
  const visibleEntries = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    if (!term) return entries;
    return entries.filter((entry) =>
      [entry.title, entry.plot_name ?? '', entry.worker_name ?? '', entry.description ?? ''].some(
        (field) => field.toLowerCase().includes(term)
      )
    );
  }, [entries, searchTerm]);

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

  /**
   * La entrada del diario es una proyección común de los tres tipos, así que para corregir una
   * actividad se piden sus campos completos.
   */
  const openEdit = async (entry: DiaryEntry) => {
    setBusyEntryId(entry.id);
    setLoadError(null);
    try {
      const activity = await activityService.getActivity(entry.id);
      setEditing(activity);
      setFormError(null);
      setModalOpen(true);
    } catch (error) {
      await handleStaleEntry(error, 'No se pudo abrir la actividad.');
    } finally {
      setBusyEntryId(null);
    }
  };

  /**
   * El diario puede estar mostrando algo que otra persona ya cambió o eliminó. En vez de dejar un
   * error suelto, se recarga y se explica (ADR-0005, RN-037).
   */
  const handleStaleEntry = async (error: unknown, fallback: string) => {
    if (
      error instanceof HttpError &&
      (error.code === CONFLICT_VERSION_MISMATCH || error.code === RESOURCE_NOT_FOUND)
    ) {
      await reload();
      setLoadError(
        error.code === RESOURCE_NOT_FOUND
          ? 'Ese registro ya no existe: otra persona lo eliminó. Se ha recargado el diario.'
          : 'Otra persona modificó ese registro mientras lo mirabas. Se ha recargado el diario con la versión actual.'
      );
      return true;
    }
    setLoadError(error instanceof HttpError ? error.message : fallback);
    return false;
  };

  const handleSubmit = async (payload: CreateActivityPayload) => {
    setSubmitting(true);
    setFormError(null);
    setNotice(null);
    try {
      const saved = editing
        ? await activityService.updateActivity(editing.id, editing.version, payload)
        : await activityService.createActivity(payload);

      setModalOpen(false);
      setEditing(null);
      await reload();
      if (saved.task_catalog_outcome) {
        setNotice(TASK_CATALOG_OUTCOME_MESSAGES[saved.task_catalog_outcome](saved.task));
      }
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        setModalOpen(false);
        setEditing(null);
        await reload();
        setLoadError(
          'Otra persona modificó esa actividad mientras la editabas. Se ha recargado el diario con la versión actual; revisa el cambio y vuelve a aplicarlo si hace falta.'
        );
        return;
      }
      setFormError(error instanceof HttpError ? error.message : 'No se pudo guardar la actividad.');
    } finally {
      setSubmitting(false);
    }
  };

  /** MVP-302 (CA-3) — promociona al catálogo la tarea de una actividad ya registrada. */
  const handleSaveTaskToCatalog = async (entry: DiaryEntry) => {
    setBusyEntryId(entry.id);
    setNotice(null);
    setLoadError(null);
    try {
      const saved = await activityService.saveTaskToCatalog(entry.id, entry.version);
      await reload();
      if (saved.task_catalog_outcome) {
        setNotice(TASK_CATALOG_OUTCOME_MESSAGES[saved.task_catalog_outcome](saved.task));
      }
    } catch (error) {
      await handleStaleEntry(error, 'No se pudo guardar la tarea en el catálogo.');
    } finally {
      setBusyEntryId(null);
    }
  };

  /**
   * RN-037 (CA-3) — Borrado **lógico** tras confirmación explícita. Cada tipo se elimina por su
   * propio recurso, que es donde viven sus reglas: por eso una compra con imputaciones vivas
   * responde 422 y el diálogo lo muestra sin cerrarse (MVP-304).
   */
  const confirmDelete = async () => {
    if (!pendingDelete) return;
    const entry = pendingDelete;

    setDeleting(true);
    setDeleteError(null);
    try {
      if (entry.type === 'actividad') {
        await activityService.deleteActivity(entry.id, entry.version);
      } else if (entry.type === 'compra') {
        await purchaseService.deletePurchase(entry.id, entry.version);
      } else {
        await consumptionService.deleteConsumption(entry.id, entry.version);
      }
      setPendingDelete(null);
      await reload();
      setNotice(`Se ha eliminado ${DIARY_ENTRY_NOUNS[entry.type]} «${entry.title}».`);
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        setPendingDelete(null);
        await reload();
        setLoadError(
          'Otra persona modificó ese registro mientras lo mirabas. Se ha recargado el diario; revísalo antes de eliminarlo.'
        );
        return;
      }
      // 422 de regla de negocio (p. ej. compra con imputaciones) o cualquier otro fallo: se muestra
      // dentro del diálogo, que es donde se está tomando la decisión.
      setDeleteError(
        error instanceof HttpError ? error.message : 'No se pudo eliminar el registro.'
      );
    } finally {
      setDeleting(false);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Diario de campo</h2>
          <p className="text-xs text-[#76786b]">
            Labores, compras y consumos del Workspace en orden cronológico. Las cosechas se sumarán a
            este mismo muro.
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

      {/* Resumen de lo que se está viendo */}
      {!isLoading && summary.total > 0 && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <SummaryTile label="Registros" value={String(summary.total)} icon="event_note" />
          <SummaryTile label="Labores" value={String(summary.activities)} icon="content_cut" />
          <SummaryTile
            label="Compras y consumos"
            value={String(summary.purchases + summary.consumptions)}
            icon="shopping_bag"
          />
          <SummaryTile label="Coste" value={`${euros(summary.total_cost)} €`} icon="payments" highlight />
        </div>
      )}

      {/* CA-3 de la épica — el impacto en la calidad del dato queda visible */}
      {summary.consumptions_without_purchase > 0 && (
        <p className="text-xs text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-xl px-3 py-2 flex items-start gap-1.5">
          <span className="material-symbols-outlined text-base shrink-0" aria-hidden="true">info</span>
          <span>
            {summary.consumptions_without_purchase === 1
              ? 'Hay 1 consumo sin compra previa: su coste consta como 0 porque se desconoce, así que el total de arriba se queda corto.'
              : `Hay ${summary.consumptions_without_purchase} consumos sin compra previa: su coste consta como 0 porque se desconoce, así que el total de arriba se queda corto.`}
          </span>
        </p>
      )}

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
      {(entries.length > 0 || typeFilter !== 'todos' || plotFilter !== 'todos' || seasonFilter !== 'todas') && (
        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] grid grid-cols-1 sm:grid-cols-4 gap-3">
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
            <label htmlFor="diary-type" className="sr-only">Filtrar por tipo de registro</label>
            <select
              id="diary-type"
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value as DiaryEntryType | 'todos')}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los tipos</option>
              <option value="actividad">Labores</option>
              <option value="compra">Compras</option>
              <option value="consumo">Consumos</option>
            </select>
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

      {/* Filtrar por terreno deja fuera las compras por definición, no por error */}
      {plotFilter !== 'todos' && typeFilter !== 'consumo' && typeFilter !== 'actividad' && (
        <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
          <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
          Al filtrar por terreno no se muestran compras: una compra es del Workspace y solo se
          reparte por terrenos al imputarla.
        </p>
      )}

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {notice && (
        <div
          role="status"
          className="p-3 rounded-xl bg-[#eef2e0] border border-[#c9dba0] text-[#33450d] text-sm flex items-start justify-between gap-3"
        >
          <span className="flex items-start gap-2">
            <span className="material-symbols-outlined text-lg shrink-0" aria-hidden="true">check_circle</span>
            {notice}
          </span>
          <button
            type="button"
            onClick={() => setNotice(null)}
            aria-label="Cerrar aviso"
            className="p-0.5 rounded text-[#4a5d23] hover:bg-[#dfe7c6] shrink-0"
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">close</span>
          </button>
        </div>
      )}

      {/* Muro cronológico */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : entries.length === 0 ? (
        <EmptyDiary canRegister={missingMasters.length === 0} onRegister={openCreate} />
      ) : visibleEntries.length === 0 ? (
        <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
          No hay registros que coincidan con los filtros.
        </p>
      ) : (
        <ol className="relative pl-6 space-y-4 before:absolute before:left-3.5 before:top-3 before:bottom-3 before:w-0.5 before:bg-[#c6c8b8]">
          {visibleEntries.map((entry) => (
            <DiaryCard
              key={`${entry.type}-${entry.id}`}
              entry={entry}
              isBusy={busyEntryId === entry.id}
              onEdit={() => void openEdit(entry)}
              onSaveTaskToCatalog={() => void handleSaveTaskToCatalog(entry)}
              onDelete={() => {
                setDeleteError(null);
                setPendingDelete(entry);
              }}
              onOpenPurchases={() => navigate('/app/compras')}
            />
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

      {/* RN-037 (CA-3) — confirmación explícita antes de eliminar */}
      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title={`¿Eliminar ${pendingDelete ? DIARY_ENTRY_NOUNS[pendingDelete.type] : 'el registro'}?`}
        message={
          pendingDelete && (
            <>
              <p>
                Vas a eliminar <strong>«{pendingDelete.title}»</strong> del{' '}
                {formatDate(pendingDelete.date)}
                {pendingDelete.plot_name ? ` en ${pendingDelete.plot_name}` : ''}.
              </p>
              <p className="text-xs text-[#76786b]">
                Desaparecerá del diario y de los listados. No hay papelera: si te equivocas, tendrás
                que volver a registrarlo.
              </p>
            </>
          )
        }
        isBusy={isDeleting}
        errorMessage={deleteError}
        onCancel={() => {
          setPendingDelete(null);
          setDeleteError(null);
        }}
        onConfirm={() => void confirmDelete()}
      />
    </div>
  );
};

const SummaryTile: React.FC<{
  label: string;
  value: string;
  icon: string;
  highlight?: boolean;
}> = ({ label, value, icon, highlight = false }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] px-4 py-3">
    <p className="text-[10px] font-bold text-[#76786b] uppercase flex items-center gap-1">
      <span className="material-symbols-outlined text-sm" aria-hidden="true">{icon}</span>
      {label}
    </p>
    <p className={`font-headline font-extrabold text-lg ${highlight ? 'text-[#ba1a1a]' : 'text-[#1c1c19]'}`}>
      {value}
    </p>
  </div>
);

interface DiaryCardProps {
  entry: DiaryEntry;
  isBusy: boolean;
  onEdit: () => void;
  onSaveTaskToCatalog: () => void;
  onDelete: () => void;
  onOpenPurchases: () => void;
}

const DiaryCard: React.FC<DiaryCardProps> = ({
  entry,
  isBusy,
  onEdit,
  onSaveTaskToCatalog,
  onDelete,
  onOpenPurchases,
}) => {
  const style = DIARY_ENTRY_STYLES[entry.type];
  const isActivity = entry.type === 'actividad';
  const isConsumptionWithoutPurchase = entry.type === 'consumo' && entry.has_purchase === false;

  return (
    <li className="relative">
      {/* Nodo del timeline: el icono identifica el tipo. MVP-401 añadirá el de cosecha. */}
      <div
        className={`absolute -left-6 top-4 w-7 h-7 rounded-full ${style.badgeClass} text-white flex items-center justify-center shadow-md ring-4 ring-[#fcf9f4]`}
      >
        <span className="material-symbols-outlined text-base" aria-hidden="true">{style.icon}</span>
      </div>

      <div className="bg-white rounded-2xl border border-[#e5e2dd] p-5 ambient-shadow space-y-3 ml-3">
        <div className="flex items-start justify-between gap-3">
          <div className="space-y-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md ${style.badgeClass} text-white uppercase tracking-wider`}>
                {style.label}
              </span>
              <span className="text-xs font-bold text-[#33450d]">{formatDate(entry.date)}</span>
              <span className="text-[11px] text-[#76786b]">· {entry.season_name}</span>
              {entry.is_out_of_season_range && (
                <span
                  title="La fecha queda fuera del rango de la temporada"
                  className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8]"
                >
                  FUERA DE TEMPORADA
                </span>
              )}
              {isConsumptionWithoutPurchase && (
                <span
                  title="Registrado sin compra previa: el coste se desconoce"
                  className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8]"
                >
                  SIN COMPRA
                </span>
              )}
            </div>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19] tracking-tight truncate">
              {entry.title}
            </h3>
          </div>

          <div className="flex items-center gap-1 shrink-0">
            {/* MVP-302 — guardar en el catálogo la tarea escrita a mano */}
            {isActivity && entry.task_id === null && (
              <button
                type="button"
                onClick={onSaveTaskToCatalog}
                disabled={isBusy}
                title="Guardar esta tarea en el catálogo"
                aria-label={`Guardar «${entry.title}» en el catálogo de tareas`}
                className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors disabled:opacity-50"
              >
                <span className="material-symbols-outlined text-base" aria-hidden="true">playlist_add</span>
              </button>
            )}

            {isActivity ? (
              <button
                type="button"
                onClick={onEdit}
                disabled={isBusy}
                title="Corregir actividad"
                aria-label={`Corregir ${entry.title}`}
                className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors disabled:opacity-50"
              >
                <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
              </button>
            ) : (
              /* Compras y consumos se corrigen donde viven, con sus reglas (imputación, sugerencias) */
              <button
                type="button"
                onClick={onOpenPurchases}
                disabled={isBusy}
                title="Corregir en Compras e insumos"
                aria-label={`Corregir «${entry.title}» en Compras e insumos`}
                className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors disabled:opacity-50"
              >
                <span className="material-symbols-outlined text-base" aria-hidden="true">open_in_new</span>
              </button>
            )}

            {/* RN-037 — el borrado pide confirmación explícita antes de ejecutarse */}
            <button
              type="button"
              onClick={onDelete}
              disabled={isBusy}
              title="Eliminar registro"
              aria-label={`Eliminar «${entry.title}»`}
              className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#ffdad6]/60 hover:text-[#ba1a1a] transition-colors disabled:opacity-50"
            >
              <span className="material-symbols-outlined text-base" aria-hidden="true">delete</span>
            </button>
          </div>
        </div>

        {entry.description && (
          <p className="text-xs sm:text-sm text-[#45483c] leading-relaxed">{entry.description}</p>
        )}

        <div className="flex items-center gap-4 text-xs font-semibold text-[#1c1c19] flex-wrap pt-1 border-t border-[#f0ede8]">
          {entry.plot_name && (
            <span className="flex items-center gap-1 text-[#33450d]">
              <span className="material-symbols-outlined text-base" aria-hidden="true">location_on</span>
              {entry.plot_name}
            </span>
          )}
          {entry.worker_name && (
            <span className="flex items-center gap-1 text-[#45483c]">
              <span className="material-symbols-outlined text-base" aria-hidden="true">person</span>
              {entry.worker_name}
            </span>
          )}
          {entry.hours !== null && (
            <span className="flex items-center gap-1 text-[#76786b]">
              <span className="material-symbols-outlined text-base" aria-hidden="true">schedule</span>
              {entry.hours} h
            </span>
          )}
          {entry.quantity !== null && (
            <span className="flex items-center gap-1 text-[#76786b]">
              <span className="material-symbols-outlined text-base" aria-hidden="true">scale</span>
              {entry.quantity.toLocaleString('es-ES')}
            </span>
          )}
          <span
            className={`flex items-center gap-1 font-bold ${
              isConsumptionWithoutPurchase ? 'text-[#76786b]' : 'text-[#ba1a1a]'
            }`}
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">payments</span>
            {isConsumptionWithoutPurchase ? 'coste desconocido' : `${euros(entry.cost)} €`}
          </span>
        </div>
      </div>
    </li>
  );
};

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
        Apunta la primera labor: qué se hizo, quién la hizo, cuánto duró y cuánto costó. Las compras y
        los consumos que registres aparecerán también aquí.
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
