import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { createActivityService } from '../../services/activity.service';
import { createConsumptionService } from '../../services/consumption.service';
import { createDiaryService } from '../../services/diary.service';
import { createHarvestService } from '../../services/harvest.service';
import { createPlotService } from '../../services/plot.service';
import { createPurchaseService } from '../../services/purchase.service';
import { createTaskService } from '../../services/task.service';
import { createWorkerService } from '../../services/worker.service';
import { HttpError } from '../../services/http-client';
import { useDiaryUrlState } from '../../lib/diary-url-state';
import { useSeasonScope } from '../../lib/season-scope';
import { ALL_SEASONS } from '../../types/season.types';
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
  DIARY_PAGE_SIZE,
  type DiaryEntry,
  type DiaryListResponse,
} from '../../types/diary.types';
import {
  harvestDestinationLabel,
  harvestProductLabel,
  type CreateHarvestPayload,
  type Harvest,
} from '../../types/harvest.types';
import type { Plot } from '../../types/plot.types';
import type { WorkTask } from '../../types/task.types';
import type { Worker } from '../../types/worker.types';
import { ConfirmDialog } from '../common/ConfirmDialog';
import { MobileDisclosure } from '../common/MobileDisclosure';
import { SummaryStrip } from '../common/SummaryStrip';
import { HarvestFormModal } from '../harvests/HarvestFormModal';
import { ActivityFormModal } from './ActivityFormModal';

const EMPTY_SUMMARY: DiaryListResponse['meta'] = {
  // MVP-701 — Ámbito todavía sin resolver: la primera respuesta lo sustituye.
  scope: { season: null, all_seasons: false },
  total: 0,
  // MVP-707 — `null` es «ninguna partida tiene precio», que no es lo mismo que 0 €.
  total_income: null,
  harvests_with_price: 0,
  page: 1,
  limit: DIARY_PAGE_SIZE,
  total_cost: 0,
  imputed_cost: 0,
  activities: 0,
  purchases: 0,
  consumptions: 0,
  consumptions_without_purchase: 0,
  harvests: 0,
  total_kg: 0,
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
 * Titular de la tarjeta. Todos los tipos traen del servidor un texto ya legible salvo la cosecha, cuyo
 * `title` es el **código** del catálogo de producto (RN-030): el vocabulario cerrado se rotula en
 * cliente, como el destino.
 */
const entryTitle = (entry: DiaryEntry): string =>
  entry.type === 'cosecha' ? harvestProductLabel(entry.title) : entry.title;

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
 * **MVP-401 enciende la cosecha** (hallazgo `G-4`), que era el tipo que faltaba para cumplir RN-033
 * entera. Se corrige **en línea**, como la actividad y a diferencia de la compra: la compra se abre en
 * su sección porque allí viven la imputación, las sugerencias de material y la cantidad pendiente,
 * mientras que el formulario de cosecha no necesita nada que el diario no tenga ya cargado. Mandar al
 * usuario a otra pantalla sin ganar nada a cambio sería peor experiencia.
 */
export const DiarioView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const { seasons, activeSeason } = useSeason();

  const diaryService = useMemo(() => createDiaryService(http), [http]);
  const activityService = useMemo(() => createActivityService(http), [http]);
  const purchaseService = useMemo(() => createPurchaseService(http), [http]);
  const consumptionService = useMemo(() => createConsumptionService(http), [http]);
  const harvestService = useMemo(() => createHarvestService(http), [http]);
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

  /**
   * MVP-705 (`P-072`) — Los seis filtros y la página viven en la **URL** (RN-007), no en el estado del
   * componente. `RN-007` ya lo exigía y `MVP-405` lo materializó en el dashboard; el diario, que es
   * donde más duele —cinco filtros y paginación—, se había quedado fuera.
   */
  const url = useDiaryUrlState();
  const {
    type: typeFilter,
    plotId: plotFilter,
    workerId: workerFilter,
    search: appliedSearch,
    page,
    setFilter,
    setPage,
    setSearch,
  } = url;

  // MVP-701 (`P-082`) — El defecto de temporada lo resuelve el servidor (RN-008): el diario ya no
  // arranca en «todas» mientras el dashboard arrancaba en la campaña de trabajo. Desde MVP-705 la
  // elección vive en la URL, así que el hook va en modo controlado: si la guardara también él,
  // habría dos copias de lo mismo y podrían divergir.
  const seasonScope = useSeasonScope({
    selection: url.seasonSelection,
    onSelect: useCallback((value: string) => setFilter({ seasonSelection: value }), [setFilter]),
  });
  // Desestructurado para que las dependencias de `reload` sean identificadores estables y la regla de
  // exhaustividad de los hooks pueda comprobarlas.
  const { requested: seasonRequested, applyFromResponse: applySeasonScope } = seasonScope;

  /**
   * Lo que se está tecleando. Es lo **único** de la navegación que no vive en la URL: escribirlo allí
   * en cada pulsación llenaría el historial y dispararía una petición por letra (`MVP-506`). El
   * término ya rebotado sí viaja a la URL, y de ahí sale `appliedSearch`.
   */
  const [searchTerm, setSearchTerm] = useState(appliedSearch);

  const [isModalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Activity | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  /** MVP-401 — la cosecha se registra y se corrige sin salir del diario. */
  const [isHarvestModalOpen, setHarvestModalOpen] = useState(false);
  const [editingHarvest, setEditingHarvest] = useState<Harvest | null>(null);
  const [harvestFormError, setHarvestFormError] = useState<string | null>(null);

  const [busyEntryId, setBusyEntryId] = useState<string | null>(null);
  /** Registro pendiente de confirmación de borrado (RN-037, CA-3). */
  const [pendingDelete, setPendingDelete] = useState<DiaryEntry | null>(null);
  const [isDeleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  /**
   * MVP-506 — Secuencia de la última carga pedida. Con filtros y paginación en servidor pueden
   * quedar dos peticiones en vuelo, y la lenta puede contestar después de la rápida: sin esta guarda,
   * una respuesta vieja pisa a la nueva y el muro acaba mostrando algo que ya no corresponde a los
   * filtros de la pantalla. Detectado al probar la búsqueda en el navegador.
   */
  const requestSeq = useRef(0);

  const reload = useCallback(async () => {
    const seq = ++requestSeq.current;
    const isStale = () => seq !== requestSeq.current;

    setIsLoading(true);
    setLoadError(null);
    try {
      const [diary, plotList, workerList, taskList] = await Promise.all([
        diaryService.listDiary({
          plotId: plotFilter === 'todos' ? undefined : plotFilter,
          seasonId: seasonRequested,
          types: typeFilter === 'todos' ? undefined : [typeFilter],
          workerId: workerFilter === 'todos' ? undefined : workerFilter,
          search: appliedSearch.trim() === '' ? undefined : appliedSearch.trim(),
          page,
          limit: DIARY_PAGE_SIZE,
        }),
        // Los maestros se piden activos: es lo que se ofrece para registros nuevos. El catálogo de
        // tareas se trae entero por el aviso de duplicado de MVP-302.
        plotService.listPlots({ isActive: true }),
        workerService.listWorkers({ isActive: true }),
        taskService.listTasks(),
      ]);

      // Mientras se esperaba, la persona pudo cambiar de filtro o de página: esta respuesta ya no
      // es la que la pantalla está pidiendo y pintarla sería mostrar datos de otra consulta.
      if (isStale()) return;

      setEntries(diary.data);
      setSummary(diary.meta);
      applySeasonScope(diary.meta.scope);
      setPlots(plotList);
      setWorkers(workerList);
      setTasks(taskList);
    } catch (error) {
      if (isStale()) return;
      setLoadError(error instanceof HttpError ? error.message : 'No se pudo cargar el diario.');
    } finally {
      if (!isStale()) setIsLoading(false);
    }
  }, [
    diaryService,
    plotService,
    workerService,
    taskService,
    plotFilter,
    seasonRequested,
    applySeasonScope,
    typeFilter,
    workerFilter,
    appliedSearch,
    page,
  ]);

  useEffect(() => {
    void reload();
  }, [reload]);

  /**
   * MVP-506 — La búsqueda viaja al servidor, pero no en cada pulsación: se espera a que la persona
   * pare de teclear. Sin esta espera, escribir «sulfatado» serían nueve peticiones y nueve repintados
   * del muro; con ella, una.
   *
   * MVP-705 (CA-3/CA-4) — El rebote se conserva **tal cual** y lo que cambia es a dónde escribe: a la
   * URL, y **sustituyendo** la entrada de historial. Escribir en la URL por pulsación dejaría una
   * entrada por carácter y el botón «atrás» quedaría inservible.
   */
  useEffect(() => {
    if (searchTerm.trim() === appliedSearch) return;

    const timer = setTimeout(() => setSearch(searchTerm.trim()), 350);
    return () => clearTimeout(timer);
  }, [searchTerm, appliedSearch, setSearch]);

  /**
   * MVP-705 — Cuando la URL cambia **por fuera** —«atrás», «adelante», o un enlace pegado— el cuadro
   * de búsqueda tiene que seguirla. La comparación con el término tecleado evita que este efecto pise
   * lo que se está escribiendo: mientras se teclea, la URL todavía no ha cambiado.
   */
  useEffect(() => {
    setSearchTerm((current) => (current.trim() === appliedSearch ? current : appliedSearch));
  }, [appliedSearch]);

  const totalPages = Math.max(1, Math.ceil(summary.total / DIARY_PAGE_SIZE));
  // MVP-705 — Lo decide la URL: si hay parámetro, hay filtro. Los defectos no llegan a escribirse.
  const hasFilters = url.hasFilters;
  // MVP-702 — Con los filtros plegados en móvil, el número es lo que evita no saber que hay puestos.
  const activeFilterCount =
    (typeFilter !== 'todos' ? 1 : 0) +
    (plotFilter !== 'todos' ? 1 : 0) +
    (seasonScope.isExplicit ? 1 : 0) +
    (workerFilter !== 'todos' ? 1 : 0) +
    (appliedSearch !== '' ? 1 : 0);

  const missingMasters = useMemo(() => {
    const missing: { label: string; to: string }[] = [];
    if (plots.length === 0) missing.push({ label: 'un terreno', to: '/app/terrenos' });
    if (workers.length === 0) missing.push({ label: 'un responsable', to: '/app/trabajadores' });
    if (seasons.length === 0) missing.push({ label: 'una temporada', to: '/app/temporadas' });
    return missing;
  }, [plots, workers, seasons]);

  /**
   * La cosecha no necesita responsable (RN-001/RN-021 piden terreno y temporada, RN-002 es de la
   * actividad), así que puede registrarse en Workspaces donde la labor todavía no.
   */
  const canRegisterHarvest = plots.length > 0 && seasons.length > 0;

  const openCreate = () => {
    setEditing(null);
    setFormError(null);
    setModalOpen(true);
  };

  const openCreateHarvest = () => {
    setEditingHarvest(null);
    setHarvestFormError(null);
    setHarvestModalOpen(true);
  };

  /**
   * La entrada del diario es una proyección común de los cuatro tipos, así que para corregir una
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

  /** MVP-401 — Igual que la actividad: la entrada no lleva todos los campos de la cosecha. */
  const openEditHarvest = async (entry: DiaryEntry) => {
    setBusyEntryId(entry.id);
    setLoadError(null);
    try {
      const harvest = await harvestService.getHarvest(entry.id);
      setEditingHarvest(harvest);
      setHarvestFormError(null);
      setHarvestModalOpen(true);
    } catch (error) {
      await handleStaleEntry(error, 'No se pudo abrir la cosecha.');
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

  /** MVP-401 — Alta y corrección de cosecha desde el propio diario. */
  const handleHarvestSubmit = async (payload: CreateHarvestPayload) => {
    setSubmitting(true);
    setHarvestFormError(null);
    setNotice(null);
    try {
      if (editingHarvest) {
        await harvestService.updateHarvest(editingHarvest.id, editingHarvest.version, payload);
      } else {
        await harvestService.createHarvest(payload);
      }
      setHarvestModalOpen(false);
      setEditingHarvest(null);
      await reload();
      setNotice(editingHarvest ? 'Cosecha corregida.' : 'Cosecha registrada.');
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        setHarvestModalOpen(false);
        setEditingHarvest(null);
        await reload();
        setLoadError(
          'Otra persona modificó esa cosecha mientras la editabas. Se ha recargado el diario con la versión actual; revisa el cambio y vuelve a aplicarlo si hace falta.'
        );
        return;
      }
      setHarvestFormError(
        error instanceof HttpError ? error.message : 'No se pudo guardar la cosecha.'
      );
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
      } else if (entry.type === 'cosecha') {
        await harvestService.deleteHarvest(entry.id, entry.version);
      } else {
        await consumptionService.deleteConsumption(entry.id, entry.version);
      }
      setPendingDelete(null);
      await reload();
      setNotice(`Se ha eliminado ${DIARY_ENTRY_NOUNS[entry.type]} «${entryTitle(entry)}».`);
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
            Labores, cosechas, compras y consumos del Workspace en orden cronológico.
          </p>
        </div>

        <div className="flex items-center gap-2 shrink-0">
          {/* MVP-401 — registrar cosecha desde la vista principal, sin pasar por su listado */}
          <button
            type="button"
            onClick={openCreateHarvest}
            disabled={!canRegisterHarvest}
            title={!canRegisterHarvest ? 'Necesitas un terreno y una temporada' : undefined}
            className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-white border border-[#c6c8b8] hover:bg-[#f0ede8] text-[#33450d] text-xs font-semibold transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">agriculture</span>
            <span>Nueva cosecha</span>
          </button>

          <button
            type="button"
            onClick={openCreate}
            disabled={missingMasters.length > 0}
            title={missingMasters.length > 0 ? 'Faltan maestros por poblar' : undefined}
            className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
            <span>Nueva actividad</span>
          </button>
        </div>
      </div>

      {/* Resumen de lo que se está viendo */}
      {!isLoading && summary.total > 0 && (
        <SummaryStrip desktopClassName="grid-cols-3 lg:grid-cols-6 gap-3">
          <SummaryTile label="Registros" value={String(summary.total)} icon="event_note" />
          <SummaryTile label="Labores" value={String(summary.activities)} icon="content_cut" />
          {/* MVP-401 — la cosecha se resume por kilos: no aporta gasto (RN-029) */}
          <SummaryTile
            label="Cosechas"
            value={summary.harvests === 0 ? '0' : `${summary.total_kg.toLocaleString('es-ES')} kg`}
            icon="agriculture"
            hint={
              summary.harvests > 0
                ? `${summary.harvests} ${summary.harvests === 1 ? 'partida' : 'partidas'}`
                : undefined
            }
          />
          <SummaryTile
            label="Compras y consumos"
            value={String(summary.purchases + summary.consumptions)}
            icon="shopping_bag"
          />
          <SummaryTile
            label="Gasto"
            value={`${euros(summary.total_cost)} €`}
            icon="payments"
            highlight
            /* R-01 (MVP-399) — el total NO suma las imputaciones: reparten dinero que la compra ya
               aportó, así que contarlas sería contar el mismo gasto dos veces. */
            hint={
              summary.imputed_cost > 0
                ? `De ese gasto, ${euros(summary.imputed_cost)} € ya están repartidos por terrenos.`
                : undefined
            }
          />
          {/* MVP-707 — El ingreso va **al lado** del gasto y no dentro de él: son magnitudes distintas
              y mezclarlas obligaría a un signo, que cada consumidor puede leer al revés. */}
          <SummaryTile
            label="Ingreso"
            /* CA-5 — sin ninguna partida con precio, «sin dato»: no se ha ingresado cero, no se sabe. */
            value={summary.total_income === null ? 'Sin dato' : `${euros(summary.total_income)} €`}
            icon="sell"
            highlight
            hint={
              summary.total_income === null
                ? 'Ninguna cosecha tiene precio por kilo'
                : summary.harvests_with_price < summary.harvests
                  ? `Sobre ${summary.harvests_with_price} de ${summary.harvests} partidas con precio.`
                  : undefined
            }
          />
        </SummaryStrip>
      )}

      {/* CA-3 de la épica — el impacto en la calidad del dato queda visible */}
      {summary.consumptions_without_purchase > 0 && (
        <p className="text-xs text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-xl px-3 py-2 flex items-start gap-1.5">
          <span className="material-symbols-outlined text-base shrink-0" aria-hidden="true">info</span>
          <span>
            {summary.consumptions_without_purchase === 1
              ? 'Hay 1 consumo sin compra previa: su coste consta como 0 porque se desconoce, así que el gasto real fue algo mayor.'
              : `Hay ${summary.consumptions_without_purchase} consumos sin compra previa: su coste consta como 0 porque se desconoce, así que el gasto real fue algo mayor.`}
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

      {/* Filtros. Todos viajan al servidor desde MVP-506, también la búsqueda por texto.
          MVP-702 — plegados en móvil: cinco controles a ancho completo empujaban los datos por
          debajo del pliegue. */}
      {(entries.length > 0 || hasFilters) && (
        <MobileDisclosure label="Filtros" icon="tune" activeCount={activeFilterCount}>
        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
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
              onChange={(e) => setFilter({ type: e.target.value })}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los tipos</option>
              <option value="actividad">Labores</option>
              <option value="cosecha">Cosechas</option>
              <option value="compra">Compras</option>
              <option value="consumo">Consumos</option>
            </select>
          </div>

          <div>
            <label htmlFor="diary-plot" className="sr-only">Filtrar por terreno</label>
            <select
              id="diary-plot"
              value={plotFilter}
              onChange={(e) => setFilter({ plotId: e.target.value })}
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
              value={seasonScope.value}
              onChange={(e) => seasonScope.select(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              {/* Hasta la primera respuesta no se sabe qué campaña aplica el servidor. */}
              {seasonScope.value === '' && <option value="">Campaña de trabajo…</option>}
              <option value={ALL_SEASONS}>Todas las temporadas</option>
              {seasons.map((season) => (
                <option key={season.id} value={season.id}>{season.name}</option>
              ))}
            </select>
          </div>

          {/* MVP-506 (`P-056`) — «qué hizo Antonio esta semana» es una pregunta natural con cuadrilla
              y hasta ahora solo se podía responder desde la API. */}
          <div>
            <label htmlFor="diary-worker" className="sr-only">Filtrar por responsable</label>
            <select
              id="diary-worker"
              value={workerFilter}
              onChange={(e) => setFilter({ workerId: e.target.value })}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los responsables</option>
              {workers.map((worker) => (
                <option key={worker.id} value={worker.id}>{worker.name}</option>
              ))}
            </select>
          </div>
        </div>
        </MobileDisclosure>
      )}

      {/* Filtrar por terreno deja fuera las compras por definición, no por error */}
      {plotFilter !== 'todos' && workerFilter === 'todos' && (typeFilter === 'todos' || typeFilter === 'compra') && (
        <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
          <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
          Al filtrar por terreno no se muestran compras: una compra es del Workspace y solo se
          reparte por terrenos al imputarla.
        </p>
      )}

      {/* MVP-506 — Filtrar por responsable deja fuera los otros tres tipos, por el mismo motivo que
          el terreno deja fuera las compras: no tienen responsable. Se dice, en vez de que el muro
          parezca vacío sin explicación. */}
      {workerFilter !== 'todos' && (
        <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
          <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
          Al filtrar por responsable solo se muestran labores: cosechas, compras y consumos no tienen
          persona asignada.
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
        // MVP-506 — con filtrado en servidor, «no hay nada» y «no hay nada que coincida» llegan igual:
        // una lista vacía. Los distingue si hay filtros puestos, porque son dos mensajes distintos.
        hasFilters ? (
          <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
            No hay registros que coincidan con los filtros.
          </p>
        ) : (
          <EmptyDiary canRegister={missingMasters.length === 0} onRegister={openCreate} />
        )
      ) : (
        <ol className="relative pl-6 space-y-4 before:absolute before:left-3.5 before:top-3 before:bottom-3 before:w-0.5 before:bg-[#c6c8b8]">
          {entries.map((entry) => (
            <DiaryCard
              key={`${entry.type}-${entry.id}`}
              entry={entry}
              isBusy={busyEntryId === entry.id}
              onEdit={() =>
                entry.type === 'cosecha' ? void openEditHarvest(entry) : void openEdit(entry)
              }
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

      {/* Paginación (MVP-506). Solo aparece cuando hay más de una página: en un Workspace recién
          estrenado sería un control que no lleva a ninguna parte. */}
      {!isLoading && totalPages > 1 && (
        <nav
          aria-label="Paginación del diario"
          className="flex items-center justify-between gap-3 bg-white p-3 rounded-2xl border border-[#e5e2dd]"
        >
          <button
            type="button"
            onClick={() => setPage(Math.max(1, page - 1))}
            disabled={page <= 1}
            className="px-3 py-1.5 rounded-lg text-xs font-semibold text-[#33450d] hover:bg-[#f0ede8] disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1"
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">chevron_left</span>
            Anteriores
          </button>

          {/* Se dice el total, no solo la página: es lo que permite saber si merece la pena seguir. */}
          <p className="text-xs text-[#76786b]" aria-live="polite">
            Página {page} de {totalPages}
            <span className="hidden sm:inline">
              {' '}· {summary.total} {summary.total === 1 ? 'registro' : 'registros'}
            </span>
          </p>

          <button
            type="button"
            onClick={() => setPage(Math.min(totalPages, page + 1))}
            disabled={page >= totalPages}
            className="px-3 py-1.5 rounded-lg text-xs font-semibold text-[#33450d] hover:bg-[#f0ede8] disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1"
          >
            Siguientes
            <span className="material-symbols-outlined text-base" aria-hidden="true">chevron_right</span>
          </button>
        </nav>
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

      {/* MVP-401 — la cosecha se registra y se corrige sin salir del diario */}
      <HarvestFormModal
        isOpen={isHarvestModalOpen}
        harvest={editingHarvest}
        plots={plots}
        seasons={seasons}
        activeSeason={activeSeason}
        isSubmitting={isSubmitting}
        errorMessage={harvestFormError}
        onClose={() => {
          setHarvestModalOpen(false);
          setEditingHarvest(null);
        }}
        onSubmit={(payload) => void handleHarvestSubmit(payload)}
      />

      {/* RN-037 (CA-3) — confirmación explícita antes de eliminar */}
      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title={`¿Eliminar ${pendingDelete ? DIARY_ENTRY_NOUNS[pendingDelete.type] : 'el registro'}?`}
        message={
          pendingDelete && (
            <>
              <p>
                Vas a eliminar <strong>«{entryTitle(pendingDelete)}»</strong> del{' '}
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
  /** Matiz que evita leer mal la cifra (p. ej. cuánto del gasto ya está repartido). */
  hint?: string;
}> = ({ label, value, icon, highlight = false, hint }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] px-4 py-3">
    <p className="text-[10px] font-bold text-[#76786b] uppercase flex items-center gap-1">
      <span className="material-symbols-outlined text-sm" aria-hidden="true">{icon}</span>
      {label}
    </p>
    <p className={`font-headline font-extrabold text-lg ${highlight ? 'text-[#ba1a1a]' : 'text-[#1c1c19]'}`}>
      {value}
    </p>
    {hint && <p className="text-[10px] text-[#76786b] leading-tight">{hint}</p>}
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
  const isHarvest = entry.type === 'cosecha';
  const isConsumptionWithoutPurchase = entry.type === 'consumo' && entry.has_purchase === false;
  // La actividad y la cosecha se corrigen aquí mismo; la compra y el consumo, en su sección.
  const editsInline = isActivity || isHarvest;

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
              {/* RN-012 — el destino sin clasificar se rotula «Sin destino», sin bloquear nada */}
              {isHarvest && entry.destination === 'desconocido' && (
                <span
                  title="La cosecha todavía no tiene destino asignado"
                  className="text-[10px] font-bold px-2 py-0.5 rounded-md bg-[#f0ede8] text-[#76786b] border border-[#dcd9d2]"
                >
                  SIN DESTINO
                </span>
              )}
            </div>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19] tracking-tight truncate">
              {entryTitle(entry)}
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

            {editsInline ? (
              <button
                type="button"
                onClick={onEdit}
                disabled={isBusy}
                title={isHarvest ? 'Corregir cosecha' : 'Corregir actividad'}
                aria-label={`Corregir ${entryTitle(entry)}`}
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
                aria-label={`Corregir «${entryTitle(entry)}» en Compras e insumos`}
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
              aria-label={`Eliminar «${entryTitle(entry)}»`}
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
          {/* MVP-401 — la cosecha muestra kilos y destino donde las demás muestran dinero: no tiene
              coste (RN-029), y enseñar «0,00 €» haría creer que salió gratis. */}
          {isHarvest ? (
            <>
              <span className="flex items-center gap-1 font-bold text-[#33450d]">
                <span className="material-symbols-outlined text-base" aria-hidden="true">scale</span>
                {(entry.kgs ?? 0).toLocaleString('es-ES')} kg
              </span>
              {/* RN-013 — rendimiento en la unidad canónica, declarado o derivado (RN-014) */}
              {entry.yield !== null && (
                <span className="flex items-center gap-1 text-[#45483c]">
                  <span className="material-symbols-outlined text-base" aria-hidden="true">water_drop</span>
                  {entry.yield.toLocaleString('es-ES', { maximumFractionDigits: 1 })} L/100kg
                </span>
              )}
              {entry.destination && entry.destination !== 'desconocido' && (
                <span className="flex items-center gap-1 text-[#45483c]">
                  <span className="material-symbols-outlined text-base" aria-hidden="true">local_shipping</span>
                  {harvestDestinationLabel(entry.destination)}
                </span>
              )}
              {/* MVP-707 — Importe ingresado (kilos × precio). Solo aparece cuando hay precio: sin él
                  no se sabe, y un «0,00 €» afirmaría algo falso. */}
              {entry.amount !== null && (
                <span className="flex items-center gap-1 font-bold text-[#33450d]">
                  <span className="material-symbols-outlined text-base" aria-hidden="true">sell</span>
                  {euros(entry.amount)} €
                </span>
              )}
            </>
          ) : (
            <span
              className={`flex items-center gap-1 font-bold ${
                isConsumptionWithoutPurchase ? 'text-[#76786b]' : 'text-[#ba1a1a]'
              }`}
            >
              <span className="material-symbols-outlined text-base" aria-hidden="true">payments</span>
              {isConsumptionWithoutPurchase ? 'coste desconocido' : `${euros(entry.cost)} €`}
            </span>
          )}
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
        Apunta la primera labor: qué se hizo, quién la hizo, cuánto duró y cuánto costó. Las cosechas,
        las compras y los consumos que registres aparecerán también aquí.
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
