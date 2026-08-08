import React, { useEffect, useMemo, useState } from 'react';
import type { Plot } from '../../types/plot.types';
import type { Season } from '../../types/season.types';
import type { Worker } from '../../types/worker.types';
import type { WorkTask } from '../../types/task.types';
import {
  ACTIVITY_DESCRIPTION_MAX_LENGTH,
  ACTIVITY_TASK_TEXT_MAX_LENGTH,
  type Activity,
  type CreateActivityPayload,
} from '../../types/activity.types';
import { Modal } from '../common/Modal';

/** Valor centinela del selector de tarea para «escribirla a mano» (RN-025). */
const FREE_TEXT_TASK = '__free__';

interface ActivityFormModalProps {
  isOpen: boolean;
  /** Actividad a corregir; `null` para alta. */
  activity: Activity | null;
  plots: Plot[];
  workers: Worker[];
  /**
   * Catálogo **completo** del Workspace, activas e inactivas. El selector ofrece solo las activas
   * (MVP-205, CA-3), pero el aviso de «esta tarea ya está en tu catálogo» (MVP-302) necesita ver
   * también las inactivadas: siguen ocupando su nombre.
   */
  tasks: WorkTask[];
  seasons: Season[];
  activeSeason: Season | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (payload: CreateActivityPayload) => void;
}

function todayIso(): string {
  const now = new Date();
  const offset = now.getTimezoneOffset();
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10);
}

/**
 * Alta y corrección de una actividad (MVP-301, HU-1/HU-2).
 *
 * Decisiones de captura que sostienen el «registrar el día a día con la mínima fricción» de la épica:
 *
 * - **La temporada llega autoseleccionada** con la activa del Workspace (RN-021) y queda visible y
 *   cambiable, no oculta: registrar una labor de la campaña anterior es un caso real.
 * - **La fecha por defecto es hoy**, y si cae fuera del rango de la temporada elegida se muestra un
 *   aviso **no bloqueante** (RN-023, CA-2): se puede guardar igual.
 * - **La tarea sale del catálogo o se escribe** (RN-025). El selector ofrece las tareas activas y una
 *   opción explícita «Otra (escribirla)», que revela el campo libre.
 * - **El coste es siempre manual y editable** (RN-003, CA-3). Si el responsable tiene tarifa de
 *   referencia se ofrece un cálculo de un clic (tarifa × horas), pero **nunca** se aplica solo: el
 *   valor lo escribe siempre la persona.
 */
export const ActivityFormModal: React.FC<ActivityFormModalProps> = ({
  isOpen,
  activity,
  plots,
  workers,
  tasks,
  seasons,
  activeSeason,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const isEdit = activity !== null;

  const [date, setDate] = useState(todayIso());
  const [plotId, setPlotId] = useState('');
  const [seasonId, setSeasonId] = useState('');
  const [workerId, setWorkerId] = useState('');
  const [taskChoice, setTaskChoice] = useState(FREE_TEXT_TASK);
  const [taskText, setTaskText] = useState('');
  const [hours, setHours] = useState('4');
  const [manualCost, setManualCost] = useState('0');
  const [description, setDescription] = useState('');
  const [saveTaskToCatalog, setSaveTaskToCatalog] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  // Sincroniza el formulario al abrirse: alta con los valores por defecto, edición con los actuales.
  useEffect(() => {
    if (!isOpen) return;
    setDate(activity?.date ?? todayIso());
    setPlotId(activity?.plot_id ?? plots[0]?.id ?? '');
    setSeasonId(activity?.season_id ?? activeSeason?.id ?? seasons[0]?.id ?? '');
    setWorkerId(activity?.worker_id ?? workers[0]?.id ?? '');
    // En un alta se preselecciona la primera tarea del catálogo, que es para lo que existe (RN-026);
    // solo se cae al texto libre si el catálogo está vacío. Al corregir manda lo que tenga la
    // actividad: si se escribió a mano, se sigue viendo el texto y no una tarea que nadie eligió.
    setTaskChoice(activity ? (activity.task_id ?? FREE_TEXT_TASK) : (tasks[0]?.id ?? FREE_TEXT_TASK));
    setTaskText(activity?.task_text ?? '');
    setHours(activity ? String(activity.hours) : '4');
    setManualCost(activity ? String(activity.manual_cost) : '0');
    setDescription(activity?.description ?? '');
    // RN-026 lo plantea como una **oferta**: nunca se guarda en el catálogo sin pedirlo.
    setSaveTaskToCatalog(false);
    setLocalError(null);
  }, [isOpen, activity, plots, workers, tasks, seasons, activeSeason]);

  const selectedSeason = useMemo(
    () => seasons.find((s) => s.id === seasonId) ?? null,
    [seasons, seasonId]
  );

  const selectedWorker = useMemo(
    () => workers.find((w) => w.id === workerId) ?? null,
    [workers, workerId]
  );

  // RN-023 — aviso, no bloqueo. Se calcula en cliente para que aparezca mientras se escribe; el
  // servidor lo recalcula y lo devuelve en `is_out_of_season_range` para el diario.
  const isOutOfSeasonRange = useMemo(() => {
    if (!selectedSeason || !date) return false;
    if (date < selectedSeason.start_date) return true;
    return selectedSeason.end_date !== null && date > selectedSeason.end_date;
  }, [selectedSeason, date]);

  /**
   * Opciones del selector: las tareas **activas** del catálogo (MVP-205, CA-3) más, al corregir, la
   * que la actividad ya referencia aunque esté inactivada —si no, editar cualquier otro campo la
   * cambiaría sin querer—.
   */
  const selectableTasks = useMemo(() => {
    const active = tasks.filter((task) => task.is_active);
    const current = activity?.task_id ? tasks.find((task) => task.id === activity.task_id) : undefined;
    return current && !current.is_active ? [...active, current] : active;
  }, [tasks, activity]);

  /**
   * MVP-302 — La tarea escrita ya existe en el catálogo (ignorando mayúsculas y espacios, el mismo
   * criterio del índice único de MVP-205). Se avisa antes de guardar para que nadie crea que va a
   * crear una tarea nueva: el servidor reutilizará —o reactivará— la que ya hay.
   */
  const existingCatalogTask = useMemo(() => {
    const normalized = taskText.trim().toLowerCase();
    if (normalized.length === 0) return null;
    return tasks.find((task) => task.name.trim().toLowerCase() === normalized) ?? null;
  }, [tasks, taskText]);

  const suggestedCost = useMemo(() => {
    const rate = selectedWorker?.hourly_rate;
    const parsedHours = Number(hours);
    if (rate == null || !Number.isFinite(parsedHours) || parsedHours <= 0) return null;
    return Math.round(rate * parsedHours * 100) / 100;
  }, [selectedWorker, hours]);

  if (!isOpen) return null;

  const hasMasters = plots.length > 0 && workers.length > 0 && seasonId !== '';

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const parsedHours = Number(hours);
    if (!Number.isFinite(parsedHours) || parsedHours <= 0) {
      setLocalError('Las horas dedicadas deben ser mayores que 0.');
      return;
    }

    const parsedCost = Number(manualCost);
    if (!Number.isFinite(parsedCost) || parsedCost < 0) {
      setLocalError('El coste no puede ser negativo.');
      return;
    }

    const usesCatalog = taskChoice !== FREE_TEXT_TASK;
    if (!usesCatalog && taskText.trim().length === 0) {
      setLocalError('Indica la tarea: elígela del catálogo o escríbela.');
      return;
    }

    setLocalError(null);
    onSubmit({
      date,
      plot_id: plotId,
      season_id: seasonId,
      worker_id: workerId,
      // RN-025 — excluyentes: se envían siempre los dos para que la edición pueda pasar de catálogo
      // a texto libre y al revés sin dejar el par a medias.
      task_id: usesCatalog ? taskChoice : null,
      task_text: usesCatalog ? null : taskText.trim(),
      hours: parsedHours,
      manual_cost: parsedCost,
      description: description.trim() || null,
      // MVP-302 — solo tiene sentido sobre una tarea escrita a mano.
      save_task_to_catalog: !usesCatalog && saveTaskToCatalog,
    });
  };

  const shownError = localError ?? errorMessage;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEdit ? 'Corregir actividad' : 'Nueva actividad'}
      icon="post_add"
      closeDisabled={isSubmitting}
    >
      <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm overflow-y-auto" noValidate>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <label htmlFor="activity-date" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Fecha <span className="text-[#ba1a1a]">*</span>
            </label>
            <input
              id="activity-date"
              type="date"
              required
              value={date}
              onChange={(e) => setDate(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            />
          </div>

          <div className="space-y-1.5">
            <label htmlFor="activity-plot" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Terreno <span className="text-[#ba1a1a]">*</span>
            </label>
            <select
              id="activity-plot"
              value={plotId}
              onChange={(e) => setPlotId(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            >
              {plots.map((plot) => (
                <option key={plot.id} value={plot.id}>
                  {plot.alias ? `${plot.name} (${plot.alias})` : plot.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* RN-021 — la temporada se autoselecciona con la activa, pero queda visible y cambiable */}
        <div className="space-y-1.5">
          <label htmlFor="activity-season" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Temporada <span className="text-[#ba1a1a]">*</span>
          </label>
          <select
            id="activity-season"
            value={seasonId}
            onChange={(e) => setSeasonId(e.target.value)}
            disabled={isSubmitting}
            className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          >
            {seasons.map((season) => (
              <option key={season.id} value={season.id}>
                {season.name}
                {season.is_working ? ' · en curso' : ''}
              </option>
            ))}
          </select>
          {isOutOfSeasonRange && (
            <p role="status" className="text-[11px] text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-lg px-2.5 py-1.5 flex items-start gap-1.5">
              <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">warning</span>
              <span>
                La fecha queda fuera del rango de «{selectedSeason?.name}». Puedes guardarla igual;
                solo es un aviso.
              </span>
            </p>
          )}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <label htmlFor="activity-worker" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Responsable <span className="text-[#ba1a1a]">*</span>
            </label>
            <select
              id="activity-worker"
              value={workerId}
              onChange={(e) => setWorkerId(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            >
              {workers.map((worker) => (
                <option key={worker.id} value={worker.id}>{worker.name}</option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <label htmlFor="activity-hours" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Horas dedicadas <span className="text-[#ba1a1a]">*</span>
            </label>
            <input
              id="activity-hours"
              type="number"
              min="0.25"
              step="0.25"
              required
              value={hours}
              onChange={(e) => setHours(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            />
          </div>
        </div>

        {/* RN-025 — tarea obligatoria: del catálogo del Workspace o escrita al vuelo */}
        <div className="space-y-1.5">
          <label htmlFor="activity-task" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Tarea <span className="text-[#ba1a1a]">*</span>
          </label>
          <select
            id="activity-task"
            value={taskChoice}
            onChange={(e) => setTaskChoice(e.target.value)}
            disabled={isSubmitting}
            className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          >
            {selectableTasks.map((task) => (
              <option key={task.id} value={task.id}>
                {task.name}
                {task.is_active ? '' : ' (inactiva)'}
              </option>
            ))}
            <option value={FREE_TEXT_TASK}>Otra (escribirla)</option>
          </select>

          {taskChoice === FREE_TEXT_TASK && (
            <>
              <input
                type="text"
                aria-label="Escribe la tarea"
                maxLength={ACTIVITY_TASK_TEXT_MAX_LENGTH}
                value={taskText}
                onChange={(e) => setTaskText(e.target.value)}
                placeholder="ej. Poda de formación, tratamiento fitosanitario"
                disabled={isSubmitting}
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />

              {/* MVP-302 — la oferta de guardar la labor en el catálogo, para no reescribirla la
                  próxima vez (RN-026, HU-1). Va aquí, en el propio flujo de captura, para no
                  obligar a salir del contexto de trabajo (CA-1). */}
              <label className="flex items-start gap-2 pt-1 cursor-pointer">
                <input
                  type="checkbox"
                  checked={saveTaskToCatalog}
                  onChange={(e) => setSaveTaskToCatalog(e.target.checked)}
                  disabled={isSubmitting || taskText.trim().length === 0}
                  className="mt-0.5 accent-[#33450d] disabled:opacity-60"
                />
                <span className="text-xs text-[#45483c]">
                  Guardar esta tarea en el catálogo del Workspace
                  <span className="block text-[11px] text-[#76786b]">
                    {existingCatalogTask
                      ? existingCatalogTask.is_active
                        ? `Ya está en tu catálogo: se reutilizará «${existingCatalogTask.name}» en vez de crear otra.`
                        : `Está en tu catálogo pero inactivada: se reactivará «${existingCatalogTask.name}».`
                      : 'Así podrás elegirla del desplegable la próxima vez.'}
                  </span>
                </span>
              </label>
            </>
          )}
        </div>

        {/* RN-003 — el coste es siempre manual: la tarifa solo sugiere, nunca decide */}
        <div className="space-y-1.5">
          <label htmlFor="activity-cost" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Coste (€) <span className="text-[#ba1a1a]">*</span>
          </label>
          <input
            id="activity-cost"
            type="number"
            min="0"
            step="0.01"
            required
            value={manualCost}
            onChange={(e) => setManualCost(e.target.value)}
            disabled={isSubmitting}
            className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          />
          {suggestedCost !== null && (
            <button
              type="button"
              onClick={() => setManualCost(String(suggestedCost))}
              disabled={isSubmitting}
              className="text-[11px] text-[#33450d] underline underline-offset-2 hover:text-[#4a5d23] disabled:opacity-60"
            >
              Usar {suggestedCost.toLocaleString('es-ES', { minimumFractionDigits: 2 })} € (tarifa de
              {' '}{selectedWorker?.name} × {hours} h)
            </button>
          )}
        </div>

        <div className="space-y-1.5">
          <label htmlFor="activity-description" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
            Observaciones
          </label>
          <textarea
            id="activity-description"
            rows={3}
            maxLength={ACTIVITY_DESCRIPTION_MAX_LENGTH}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Dosis, sector, incidencias…"
            disabled={isSubmitting}
            className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          />
        </div>

        {shownError && (
          <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {shownError}
          </div>
        )}

        <div className="pt-3 flex items-center justify-end gap-3 border-t border-[#e5e2dd]">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl disabled:opacity-60"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={!hasMasters || isSubmitting}
            className="flex items-center gap-2 px-5 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            <span>{isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Registrar actividad'}</span>
            <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
          </button>
        </div>
      </form>
    </Modal>
  );
};
