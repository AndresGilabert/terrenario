import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useSeason } from '../../contexts/SeasonContext';
import { useApiClient } from '../../contexts/ApiContext';
import { createPlotService } from '../../services/plot.service';
import { createWorkerService } from '../../services/worker.service';
import { createTaskService } from '../../services/task.service';
import { VisionGeneralView } from '../dashboard/VisionGeneralView';

/**
 * Home del área operativa (MVP-201 · MVP-207 · MVP-499).
 *
 * Tiene dos caras según la preparación del Workspace (P-040, decisión del PO en MVP-499):
 *
 * - **Mientras falten maestros por poblar**, es la pantalla de arranque: bienvenida + checklist de lo
 *   que queda para empezar a registrar (temporada, terrenos, trabajadores, tareas). Es lo que pedía
 *   HU-2 de MVP-201.
 * - **Cuando la explotación está preparada**, el Home **pasa a ser la Visión General**: quien ya lo
 *   tiene todo listo entra directo a sus métricas, no a un checklist completado. Así no hay dos
 *   pantallas de inicio compitiendo (cierra P-040).
 */

interface SetupStep {
  key: string;
  label: string;
  icon: string;
  /** Qué aporta el maestro, en una línea. */
  hint: string;
  to: string;
  cta: string;
  done: boolean;
  /** Cuántos registros tiene ya (para los maestros de lista). */
  count?: number;
}

export const HomeView: React.FC = () => {
  const { user } = useAuth();
  const { activeWorkspace } = useWorkspace();
  const { activeSeason, seasons } = useSeason();
  const navigate = useNavigate();
  const http = useApiClient();

  const plotService = useMemo(() => createPlotService(http), [http]);
  const workerService = useMemo(() => createWorkerService(http), [http]);
  const taskService = useMemo(() => createTaskService(http), [http]);

  const [counts, setCounts] = useState<{ plots: number; workers: number; tasks: number } | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const workspaceId = activeWorkspace?.id ?? null;

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const [plots, workers, tasks] = await Promise.all([
        plotService.listPlots(),
        workerService.listWorkers(),
        taskService.listTasks(),
      ]);
      setCounts({ plots: plots.length, workers: workers.length, tasks: tasks.length });
    } catch {
      // El estado de preparación es informativo: si no se puede calcular, la pantalla sigue
      // ofreciendo los accesos a los maestros en vez de mostrar un error que no aporta nada.
      setCounts(null);
    } finally {
      setIsLoading(false);
    }
  }, [plotService, workerService, taskService]);

  useEffect(() => {
    if (!workspaceId) return;
    void load();
  }, [workspaceId, load]);

  const steps: SetupStep[] = counts
    ? [
        {
          key: 'season',
          label: 'Temporada activa',
          icon: 'calendar_today',
          hint: 'Toda actividad, cosecha y compra se agrupa por campaña.',
          to: '/app/temporadas',
          // Con temporadas pero ninguna activa, lo que falta es elegir, no crear (MVP-208, CA-8/CA-10).
          cta: seasons.length > 0 ? 'Activar temporada' : 'Crear temporada',
          done: activeSeason !== null,
        },
        {
          key: 'plots',
          label: 'Terrenos',
          icon: 'map',
          hint: 'Las parcelas a las que se imputa cada registro.',
          to: '/app/terrenos',
          cta: 'Añadir terrenos',
          done: counts.plots > 0,
          count: counts.plots,
        },
        {
          // MVP-208 (CA-10) — El paso contaba solo la cuadrilla mientras su propia ayuda decía que
          // los miembros cuentan, así que aparecía pendiente en un Workspace que por RN-027 ya tiene
          // responsables. Ahora `GET /workers` devuelve el maestro completo y el recuento coincide
          // con lo que el usuario ve: el paso está hecho por construcción, y lo que queda es opcional.
          key: 'workers',
          label: 'Trabajadores',
          icon: 'group',
          hint: 'Quién hace cada labor. Los miembros del Workspace ya cuentan; añade a la cuadrilla sin cuenta.',
          to: '/app/trabajadores',
          cta: 'Añadir trabajadores',
          done: counts.workers > 0,
          count: counts.workers,
        },
        {
          key: 'tasks',
          label: 'Catálogo de tareas',
          icon: 'checklist',
          hint: 'Las labores habituales, para no reescribirlas cada vez.',
          to: '/app/tareas',
          cta: 'Añadir tareas',
          done: counts.tasks > 0,
          count: counts.tasks,
        },
      ]
    : [];

  const pending = steps.filter((step) => !step.done).length;
  const isReady = counts !== null && pending === 0;

  // Mientras se calcula la preparación no se decide qué cara mostrar: un parpadeo entre el checklist y
  // el dashboard sería peor que esperar un instante.
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  // P-040 — con la explotación preparada, el Home ES la Visión General (MVP-499): se reutiliza la misma
  // vista, no se duplica. El checklist ya no aporta nada porque no queda nada que preparar.
  if (isReady) {
    return <VisionGeneralView />;
  }

  return (
    <div className="space-y-6 pb-12">
      {/* Bienvenida. El selector, la campanita, la navegación y el cierre de sesión viven en el
          shell (MVP-107). */}
      <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 ambient-shadow space-y-4">
        <div className="w-14 h-14 rounded-2xl bg-[#33450d] text-white flex items-center justify-center">
          <span className="material-symbols-outlined fill text-3xl" aria-hidden="true">eco</span>
        </div>
        <div className="space-y-1">
          <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
            ¡Bienvenido, {user?.displayName ?? 'usuario'}!
          </h1>
          {activeWorkspace && (
            <p className="text-sm font-semibold text-[#33450d]">
              Estás trabajando en «{activeWorkspace.name}».
            </p>
          )}
        </div>
        <p className="text-[#45483c] text-sm max-w-lg">
          El <strong>diario de campo</strong> es donde se registra el día a día de la explotación:
          labores, compras, consumos y cosechas, en orden cronológico. Los maestros —temporadas,
          terrenos, trabajadores, tareas y accesos— y la <strong>Visión General</strong> están en el
          menú lateral. En cuanto termines de preparar los maestros, esta pantalla pasará a mostrarte
          el resumen de tu explotación.
        </p>
        <div className="flex flex-col sm:flex-row items-start gap-3 pt-1">
          {/* MVP-301 — el diario es la vista principal del MVP (RN-033): el Home conduce a él. */}
          <button
            onClick={() => navigate('/app/diario')}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">event_note</span>
            Ir al diario de campo
          </button>
          <button
            onClick={() => navigate('/app/invitations')}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-white border border-[#c6c8b8] hover:bg-[#f0ede8] text-[#45483c] text-sm font-semibold transition-colors"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
            Invitar a alguien
          </button>
        </div>
      </div>

      {/* Preparación de la explotación (CA-6): el camino explícito a los maestros pendientes. Aquí
          siempre queda algo por poblar —si no, el Home ya sería la Visión General (P-040)—. */}
      {counts !== null && (
          <section className="bg-white rounded-2xl border border-[#e5e2dd] p-6 ambient-shadow space-y-4">
            <div className="flex items-start justify-between gap-3 flex-wrap">
              <div>
                <h2 className="font-headline font-bold text-lg text-[#1c1c19]">Prepara tu explotación</h2>
                <p className="text-xs text-[#76786b]">
                  {`Te ${pending === 1 ? 'queda 1 maestro' : `quedan ${pending} maestros`} por poblar para empezar a registrar el día a día.`}
                </p>
              </div>
              <span className="text-[11px] font-bold px-2.5 py-1 rounded-full bg-[#eef2e0] text-[#33450d] shrink-0">
                {steps.length - pending}/{steps.length}
              </span>
            </div>

            <ul className="divide-y divide-[#f0ede8]">
              {steps.map((step) => (
                <li key={step.key} className="py-3 flex items-center justify-between gap-3 flex-wrap">
                  <div className="flex items-center gap-3 min-w-0">
                    <div
                      className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${
                        step.done ? 'bg-[#eef2e0] text-[#33450d]' : 'bg-[#f6f3ee] text-[#76786b]'
                      }`}
                    >
                      <span className="material-symbols-outlined text-lg" aria-hidden="true">
                        {step.done ? 'check' : step.icon}
                      </span>
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-semibold text-[#1c1c19]">
                        {step.label}
                        {step.done && step.count !== undefined && (
                          <span className="ml-1.5 text-xs font-normal text-[#76786b]">({step.count})</span>
                        )}
                      </p>
                      <p className="text-xs text-[#76786b]">{step.hint}</p>
                    </div>
                  </div>

                  <button
                    onClick={() => navigate(step.to)}
                    className={`px-3 py-1.5 rounded-lg text-xs font-semibold shrink-0 transition-colors ${
                      step.done
                        ? 'text-[#45483c] hover:bg-[#f0ede8]'
                        : 'bg-[#33450d] hover:bg-[#4a5d23] text-white'
                    }`}
                  >
                    {step.done ? 'Ver' : step.cta}
                  </button>
                </li>
              ))}
            </ul>
          </section>
      )}
    </div>
  );
};
