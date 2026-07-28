import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useSeason } from '../../contexts/SeasonContext';
import { useApiClient } from '../../contexts/ApiContext';
import { createPlotService } from '../../services/plot.service';
import { createWorkerService } from '../../services/worker.service';
import { createTaskService } from '../../services/task.service';

/**
 * Home del área operativa (MVP-201 · corregido en MVP-207, HU-4/CA-6).
 *
 * Antes era una pantalla muerta: su único CTA era «Invitar a alguien» y su copy seguía anunciando
 * como «por habilitar» módulos que ya están encendidos en el menú. Ahora conduce a los maestros que
 * faltan por poblar, que es lo que HU-2 de MVP-201 pedía («entrar a una aplicación preparada para
 * completar los maestros básicos»).
 *
 * No es el dashboard: la Visión General con métricas reales es alcance de MVP-004. Aquí solo se
 * responde a «¿qué me falta para empezar a registrar?», y el bloque desaparece en cuanto está todo
 * preparado.
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
  const { activeSeason } = useSeason();
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
          cta: 'Crear temporada',
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
          key: 'workers',
          label: 'Trabajadores',
          icon: 'group',
          hint: 'Quién hace cada labor. Los miembros del Workspace ya cuentan.',
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
          Temporadas, terrenos, trabajadores, tareas y accesos ya están disponibles en el menú
          lateral. El diario de campo, las cosechas y las compras llegarán después: en el menú se
          distinguen con la etiqueta «Pronto».
        </p>
        <div className="flex flex-col sm:flex-row items-start gap-3 pt-1">
          <button
            onClick={() => navigate('/app/invitations')}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
            Invitar a alguien
          </button>
        </div>
      </div>

      {/* Preparación de la explotación (CA-6): el camino explícito a los maestros pendientes. */}
      {isLoading ? (
        <div className="flex items-center justify-center py-10">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : (
        counts !== null && (
          <section className="bg-white rounded-2xl border border-[#e5e2dd] p-6 ambient-shadow space-y-4">
            <div className="flex items-start justify-between gap-3 flex-wrap">
              <div>
                <h2 className="font-headline font-bold text-lg text-[#1c1c19]">
                  {isReady ? 'Tu explotación está preparada' : 'Prepara tu explotación'}
                </h2>
                <p className="text-xs text-[#76786b]">
                  {isReady
                    ? 'Ya puedes seguir completando los maestros cuando lo necesites.'
                    : `Te ${pending === 1 ? 'queda 1 maestro' : `quedan ${pending} maestros`} por poblar para empezar a registrar el día a día.`}
                </p>
              </div>
              {!isReady && (
                <span className="text-[11px] font-bold px-2.5 py-1 rounded-full bg-[#eef2e0] text-[#33450d] shrink-0">
                  {steps.length - pending}/{steps.length}
                </span>
              )}
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
        )
      )}
    </div>
  );
};
