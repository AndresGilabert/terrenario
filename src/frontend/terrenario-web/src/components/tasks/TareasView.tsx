import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useApiClient } from '../../contexts/ApiContext';
import { createTaskService } from '../../services/task.service';
import { HttpError } from '../../services/http-client';
import { TASK_NAME_MAX_LENGTH, type WorkTask } from '../../types/task.types';
import { isDeletable } from '../../types/master.types';
import { useMasterDepuration } from '../../lib/use-master-depuration';
import { MasterDepurationLayer } from '../common/MasterDepurationLayer';

/**
 * Catálogo de tareas del Workspace (MVP-205, RN-026). Es el maestro que da consistencia al registro
 * diario: las tareas que se creen aquí se reutilizan después al apuntar una labor (RN-025).
 *
 * A diferencia del resto de maestros (terrenos, temporadas, trabajadores), una tarea es **un solo
 * campo**, así que el alta y el renombrado son **en línea** en vez de en modal: poblar un catálogo
 * consiste en escribir varias tareas seguidas y abrir/cerrar un modal por cada una sería fricción
 * pura. El resto de la mecánica (filtro de inactivas, inactivación reversible, paleta y tipografía)
 * es la misma que en los otros maestros.
 *
 * El catálogo **arranca vacío** (CA-2) y las tareas con histórico se **inactivan**, no se borran
 * (CA-3): dejan de ofrecerse para registros nuevos sin invalidar los que ya las usan. Desde MVP-806
 * la que **nunca** se usó sí se elimina, y dos que son la misma labor se fusionan: el listado trae
 * `usage_count` y el botón de eliminar solo aparece cuando el servidor confirma que está a cero.
 */
export const TareasView: React.FC = () => {
  const http = useApiClient();
  const taskService = useMemo(() => createTaskService(http), [http]);

  const [tasks, setTasks] = useState<WorkTask[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showInactive, setShowInactive] = useState(false);

  const [newName, setNewName] = useState('');
  const [isCreating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const newNameInput = useRef<HTMLInputElement>(null);
  /**
   * Contador de altas correctas. Existe solo para devolver el foco al campo **después** de que React
   * haya vuelto a renderizar: llamarlo dentro del propio manejador no funcionaba, porque en ese
   * momento el input sigue `disabled` y enfocar un elemento deshabilitado no hace nada
   * (`MVP-999`, `P-053`, corregido en la revisión de cierre `MVP-399`).
   */
  const [createdCount, setCreatedCount] = useState(0);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState('');
  const [rowError, setRowError] = useState<string | null>(null);
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      // Traemos activas + inactivas y filtramos en cliente; el filtro de servidor (`is_active`)
      // queda disponible para la operativa diaria, que solo querrá las activas.
      setTasks(await taskService.listTasks());
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudo cargar el catálogo de tareas.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [taskService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  // MVP-806 — Borrado de lo nunca usado y fusión de duplicados, con la misma mecánica que el resto
  // de maestros: el hook lleva el estado y los diálogos, la vista pone los botones donde encajan.
  const depuration = useMasterDepuration('tasks', { onChanged: reload });

  useEffect(() => {
    if (createdCount > 0) newNameInput.current?.focus();
  }, [createdCount]);

  const activeCount = useMemo(() => tasks.filter((t) => t.is_active).length, [tasks]);
  const inactiveCount = tasks.length - activeCount;

  const visibleTasks = useMemo(() => {
    const term = searchTerm.trim().toLowerCase();
    return tasks.filter((t) => {
      if (!showInactive && !t.is_active) return false;
      return !term || t.name.toLowerCase().includes(term);
    });
  }, [tasks, searchTerm, showInactive]);

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    const name = newName.trim();
    if (!name || isCreating) return;

    setCreating(true);
    setCreateError(null);
    try {
      await taskService.createTask({ name });
      setNewName('');
      await reload();
      // El foco vuelve al campo (poblar el catálogo es escribir varias tareas seguidas), pero se
      // pide por efecto, no aquí: ver `createdCount`.
      setCreatedCount((count) => count + 1);
    } catch (error) {
      // El 409 de nombre duplicado llega con su mensaje del contrato; se muestra tal cual.
      setCreateError(
        error instanceof HttpError ? error.message : 'No se pudo añadir la tarea. Inténtalo de nuevo.'
      );
    } finally {
      setCreating(false);
    }
  };

  const startEdit = (task: WorkTask) => {
    setEditingId(task.id);
    setEditingName(task.name);
    setRowError(null);
  };

  const cancelEdit = () => {
    setEditingId(null);
    setEditingName('');
    setRowError(null);
  };

  const handleRename = async (task: WorkTask) => {
    const name = editingName.trim();
    if (!name) {
      setRowError('Escribe un nombre para la tarea.');
      return;
    }
    if (name === task.name) {
      cancelEdit();
      return;
    }

    setBusyTaskId(task.id);
    setRowError(null);
    try {
      await taskService.updateTask(task.id, { name });
      cancelEdit();
      await reload();
    } catch (error) {
      setRowError(
        error instanceof HttpError ? error.message : 'No se pudo renombrar la tarea.'
      );
    } finally {
      setBusyTaskId(null);
    }
  };

  const toggleActive = async (task: WorkTask) => {
    setBusyTaskId(task.id);
    setRowError(null);
    try {
      await taskService.updateTask(task.id, { is_active: !task.is_active });
      await reload();
    } catch (error) {
      setRowError(
        error instanceof HttpError ? error.message : 'No se pudo cambiar el estado de la tarea.'
      );
    } finally {
      setBusyTaskId(null);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Catálogo de tareas</h2>
          <p className="text-xs text-[#76786b]">
            Las labores que repites en tu explotación (poda, riego, abonado…). Al registrar una
            actividad podrás elegirlas del catálogo en vez de volver a escribirlas.
          </p>
        </div>

        {/* Alta en línea: una tarea es solo un nombre, así que no hace falta un modal */}
        <form onSubmit={(e) => void handleCreate(e)} className="flex flex-col sm:flex-row gap-2">
          <label htmlFor="new-task-name" className="sr-only">
            Nombre de la tarea
          </label>
          <input
            id="new-task-name"
            ref={newNameInput}
            type="text"
            value={newName}
            maxLength={TASK_NAME_MAX_LENGTH}
            onChange={(e) => {
              setNewName(e.target.value);
              // El aviso (p. ej. nombre duplicado) deja de aplicar en cuanto se corrige el texto.
              if (createError) setCreateError(null);
            }}
            placeholder="ej. Poda de mantenimiento"
            disabled={isCreating}
            className="flex-1 px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
          />
          <button
            type="submit"
            disabled={newName.trim().length === 0 || isCreating}
            className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed shrink-0"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
            <span>{isCreating ? 'Añadiendo…' : 'Añadir tarea'}</span>
          </button>
        </form>

        {createError && (
          <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {createError}
          </div>
        )}
      </div>

      {/* Búsqueda y filtro de inactivas */}
      {tasks.length > 0 && (
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="flex-1 bg-white p-3 rounded-2xl border border-[#e5e2dd] flex items-center gap-3">
            <span className="material-symbols-outlined text-[#76786b] pl-2" aria-hidden="true">search</span>
            <label htmlFor="task-search" className="sr-only">
              Buscar tarea
            </label>
            <input
              id="task-search"
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar en el catálogo…"
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
              <span>Inactivas ({inactiveCount})</span>
            </button>
          )}
        </div>
      )}

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}
      {rowError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {rowError}
        </div>
      )}

      <MasterDepurationLayer
        kindLabel="la tarea"
        kindPlural="tareas"
        depuration={depuration}
        candidates={tasks.filter((t) => t.id !== depuration.merging?.id)}
      />
      {/* El 422 de «tiene histórico» llega cuando ya no hay diálogo abierto solo si el listado iba
          desfasado; el mensaje del servidor trae la cifra, así que se muestra tal cual. */}
      {depuration.error && !depuration.deleting && !depuration.merging && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {depuration.error}
        </div>
      )}

      {/* Catálogo */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : tasks.length === 0 ? (
        <EmptyTasks onSuggest={(name) => {
          setNewName(name);
          newNameInput.current?.focus();
        }} />
      ) : visibleTasks.length === 0 ? (
        <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
          No hay tareas que coincidan con la búsqueda.
        </p>
      ) : (
        <ul className="bg-white rounded-2xl border border-[#e5e2dd] divide-y divide-[#f0ede8] overflow-hidden">
          {visibleTasks.map((task) => (
            <li key={task.id} className={`p-4 ${task.is_active ? '' : 'bg-[#faf8f4]'}`}>
              {editingId === task.id ? (
                <form
                  onSubmit={(e) => {
                    e.preventDefault();
                    void handleRename(task);
                  }}
                  className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2"
                >
                  <label htmlFor={`task-name-${task.id}`} className="sr-only">
                    Nuevo nombre de la tarea
                  </label>
                  <input
                    id={`task-name-${task.id}`}
                    type="text"
                    autoFocus
                    value={editingName}
                    maxLength={TASK_NAME_MAX_LENGTH}
                    onChange={(e) => setEditingName(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Escape') cancelEdit();
                    }}
                    disabled={busyTaskId === task.id}
                    className="flex-1 px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                  />
                  <div className="flex items-center gap-2 shrink-0">
                    <button
                      type="button"
                      onClick={cancelEdit}
                      disabled={busyTaskId === task.id}
                      className="px-3 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl disabled:opacity-60"
                    >
                      Cancelar
                    </button>
                    <button
                      type="submit"
                      disabled={editingName.trim().length === 0 || busyTaskId === task.id}
                      className="px-4 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold disabled:opacity-60 disabled:cursor-not-allowed"
                    >
                      Guardar
                    </button>
                  </div>
                </form>
              ) : (
                <div className="flex items-center justify-between gap-3">
                  <div className="flex items-center gap-3 min-w-0">
                    <span
                      className={`material-symbols-outlined text-xl shrink-0 ${
                        task.is_active ? 'text-[#33450d]' : 'text-[#a2a496]'
                      }`}
                      aria-hidden="true"
                    >
                      checklist
                    </span>
                    <span
                      className={`text-sm font-semibold truncate ${
                        task.is_active ? 'text-[#1c1c19]' : 'text-[#76786b]'
                      }`}
                    >
                      {task.name}
                    </span>
                    {!task.is_active && (
                      <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#e5e2dd] text-[#76786b] shrink-0">
                        INACTIVA
                      </span>
                    )}
                  </div>

                  <div className="flex items-center gap-1 shrink-0">
                    <button
                      type="button"
                      onClick={() => startEdit(task)}
                      disabled={busyTaskId === task.id}
                      title="Renombrar"
                      aria-label={`Renombrar ${task.name}`}
                      className="p-2 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] disabled:opacity-50"
                    >
                      <span className="material-symbols-outlined text-lg" aria-hidden="true">edit</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => void toggleActive(task)}
                      disabled={busyTaskId === task.id}
                      title={task.is_active ? 'Inactivar' : 'Reactivar'}
                      aria-label={`${task.is_active ? 'Inactivar' : 'Reactivar'} ${task.name}`}
                      className="p-2 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] disabled:opacity-50"
                    >
                      <span className="material-symbols-outlined text-lg" aria-hidden="true">
                        {task.is_active ? 'toggle_off' : 'toggle_on'}
                      </span>
                    </button>
                    {/* MVP-806 — Fusionar solo tiene sentido si hay con qué. */}
                    {tasks.length > 1 && (
                      <button
                        type="button"
                        onClick={() => depuration.askMerge(task)}
                        disabled={busyTaskId === task.id}
                        title="Fusionar con otra tarea"
                        aria-label={`Fusionar ${task.name} con otra tarea`}
                        className="p-2 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] disabled:opacity-50"
                      >
                        <span className="material-symbols-outlined text-lg" aria-hidden="true">merge</span>
                      </button>
                    )}
                    {/* CA-2 — La acción no se ofrece si hay histórico; la palabra definitiva la tiene
                        el servidor, pero enseñar un botón que siempre va a fallar no es una opción. */}
                    {isDeletable(task) && (
                      <button
                        type="button"
                        onClick={() => depuration.askDelete(task)}
                        disabled={busyTaskId === task.id}
                        title="Eliminar"
                        aria-label={`Eliminar ${task.name}`}
                        className="p-2 rounded-lg text-[#76786b] hover:bg-red-50 hover:text-[#ba1a1a] disabled:opacity-50"
                      >
                        <span className="material-symbols-outlined text-lg" aria-hidden="true">delete</span>
                      </button>
                    )}
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      {tasks.length > 0 && (
        <p className="text-[11px] text-[#76786b] flex items-center gap-1.5">
          <span className="material-symbols-outlined text-sm" aria-hidden="true">info</span>
          Las tareas que ya se han usado se inactivan, no se borran: los registros que las utilizan
          siguen siendo válidos. Las que nunca se usaron sí se pueden eliminar, y dos que son la
          misma labor se pueden fusionar.
        </p>
      )}
    </div>
  );
};

/** Sugerencias de arranque: el catálogo nace vacío (CA-2) y conviene que la primera alta sea trivial. */
const SUGGESTIONS = ['Poda', 'Riego', 'Abonado', 'Tratamiento fitosanitario', 'Recolección'];

const EmptyTasks: React.FC<{ onSuggest: (name: string) => void }> = ({ onSuggest }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-4">
    <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-3xl" aria-hidden="true">checklist</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Tu catálogo está vacío</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">
        Añade arriba las labores que repites en tu explotación. También podrás guardar una tarea
        escrita al vuelo mientras registras una actividad.
      </p>
    </div>
    <div className="flex flex-wrap items-center justify-center gap-2 pt-1">
      <span className="text-xs text-[#76786b]">Empezar con:</span>
      {SUGGESTIONS.map((suggestion) => (
        <button
          key={suggestion}
          type="button"
          onClick={() => onSuggest(suggestion)}
          className="px-3 py-1.5 rounded-full bg-[#f6f3ee] border border-[#e5e2dd] text-xs font-semibold text-[#33450d] hover:bg-[#f0ede8] transition-colors"
        >
          {suggestion}
        </button>
      ))}
    </div>
  </div>
);
