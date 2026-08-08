import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { useSeasonScope } from '../../lib/season-scope';
import { ALL_SEASONS } from '../../types/season.types';
import { createHarvestService } from '../../services/harvest.service';
import { createPlotService } from '../../services/plot.service';
import { HttpError } from '../../services/http-client';
import { CONFLICT_VERSION_MISMATCH, RESOURCE_NOT_FOUND } from '../../types/activity.types';
import {
  HARVEST_DESTINATIONS,
  harvestDestinationLabel,
  harvestProductLabel,
  type CreateHarvestPayload,
  type Harvest,
} from '../../types/harvest.types';
import type { Plot } from '../../types/plot.types';
import { ConfirmDialog } from '../common/ConfirmDialog';
import { HarvestFormModal } from './HarvestFormModal';

/** Formato de fecha corto y legible, sin depender del locale del navegador. */
function formatDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

const number = (value: number, decimals = 0) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });

/**
 * Registro de cosechas del Workspace (MVP-401).
 *
 * Es el listado propio de la producción, hermano del libro de compras: el diario (MVP-305) mezcla las
 * cosechas con el resto de la operativa por fecha, y aquí se ven solas, con sus kilos acumulados y su
 * filtro por destino.
 *
 * El **borrado exige confirmación explícita** (RN-037) y es **lógico**: la cosecha desaparece del
 * listado, del diario y del dashboard, pero no se pierde en base de datos. No hay papelera.
 */
export const CosechasView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const { seasons, activeSeason } = useSeason();

  const harvestService = useMemo(() => createHarvestService(http), [http]);
  const plotService = useMemo(() => createPlotService(http), [http]);

  const [harvests, setHarvests] = useState<Harvest[]>([]);
  const [totalKg, setTotalKg] = useState(0);
  const [plots, setPlots] = useState<Plot[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const [plotFilter, setPlotFilter] = useState('todos');
  // MVP-701 (`P-082`) — El ámbito de temporada ya no arranca en «todas»: lo resuelve el servidor con
  // el defecto de RN-008. Era la causa de que esta pantalla y la Visión General dieran totales
  // distintos de la misma campaña.
  const seasonScope = useSeasonScope();
  // Desestructurado para que las dependencias de `reload` sean identificadores estables y la regla de
  // exhaustividad de los hooks pueda comprobarlas.
  const { requested: seasonRequested, applyFromResponse: applySeasonScope } = seasonScope;
  const [destinationFilter, setDestinationFilter] = useState('todos');

  const [isModalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Harvest | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [pendingDelete, setPendingDelete] = useState<Harvest | null>(null);
  const [isDeleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const [list, plotList] = await Promise.all([
        harvestService.listHarvests({
          plotId: plotFilter === 'todos' ? undefined : plotFilter,
          seasonId: seasonRequested,
          destination: destinationFilter === 'todos' ? undefined : destinationFilter,
        }),
        // Los terrenos se piden activos: es lo que se ofrece para registros nuevos (MVP-202, CA-3).
        plotService.listPlots({ isActive: true }),
      ]);
      setHarvests(list.data);
      setTotalKg(list.meta.total_kg);
      applySeasonScope(list.meta.scope);
      setPlots(plotList);
    } catch (error) {
      setLoadError(error instanceof HttpError ? error.message : 'No se pudieron cargar las cosechas.');
    } finally {
      setIsLoading(false);
    }
  }, [
    harvestService,
    plotService,
    plotFilter,
    seasonRequested,
    applySeasonScope,
    destinationFilter,
  ]);

  useEffect(() => {
    void reload();
  }, [reload]);

  /** Registrar exige terreno y temporada: se dice qué falta y se enlaza, en vez de fallar al guardar. */
  const missingMasters = useMemo(() => {
    const missing: { label: string; to: string }[] = [];
    if (plots.length === 0) missing.push({ label: 'un terreno', to: '/app/terrenos' });
    if (seasons.length === 0) missing.push({ label: 'una temporada', to: '/app/temporadas' });
    return missing;
  }, [plots, seasons]);

  /**
   * Rendimiento medio **ponderado por kilos** de lo que se está viendo (RN-013). Una media aritmética
   * daría el mismo peso a una partida de 50 kg que a una de 5.000, que es justo la lectura equivocada.
   *
   * Entran todas las partidas con rendimiento **efectivo** (MVP-402): tanto las que lo declararon como
   * las que declararon litros y del que se deriva (RN-014). Las que todavía no tienen dato de aceite
   * quedan fuera del promedio, y se cuentan aparte para poder decirlo.
   */
  const yieldSummary = useMemo(() => {
    const withYield = harvests.filter((harvest) => harvest.effective_yield !== null);
    const kilos = withYield.reduce((acc, harvest) => acc + harvest.kgs, 0);
    const average =
      kilos === 0
        ? null
        : (withYield.reduce((acc, h) => acc + (h.effective_yield! / 100) * h.kgs, 0) / kilos) * 100;
    return { average, counted: withYield.length, pending: harvests.length - withYield.length };
  }, [harvests]);

  const openCreate = () => {
    setEditing(null);
    setFormError(null);
    setModalOpen(true);
  };

  const openEdit = (harvest: Harvest) => {
    setEditing(harvest);
    setFormError(null);
    setModalOpen(true);
  };

  const handleSubmit = async (payload: CreateHarvestPayload) => {
    setSubmitting(true);
    setFormError(null);
    setNotice(null);
    try {
      if (editing) {
        await harvestService.updateHarvest(editing.id, editing.version, payload);
      } else {
        await harvestService.createHarvest(payload);
      }
      setModalOpen(false);
      setEditing(null);
      await reload();
      setNotice(editing ? 'Cosecha corregida.' : 'Cosecha registrada.');
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        setModalOpen(false);
        setEditing(null);
        await reload();
        setLoadError(
          'Otra persona modificó esa cosecha mientras la editabas. Se ha recargado el listado con la versión actual; revisa el cambio y vuelve a aplicarlo si hace falta.'
        );
        return;
      }
      setFormError(error instanceof HttpError ? error.message : 'No se pudo guardar la cosecha.');
    } finally {
      setSubmitting(false);
    }
  };

  /** RN-037 — borrado **lógico** tras confirmación explícita. */
  const confirmDelete = async () => {
    if (!pendingDelete) return;
    const harvest = pendingDelete;

    setDeleting(true);
    setDeleteError(null);
    try {
      await harvestService.deleteHarvest(harvest.id, harvest.version);
      setPendingDelete(null);
      await reload();
      setNotice(`Se ha eliminado la cosecha de ${number(harvest.kgs)} kg en ${harvest.plot_name}.`);
    } catch (error) {
      if (
        error instanceof HttpError &&
        (error.code === CONFLICT_VERSION_MISMATCH || error.code === RESOURCE_NOT_FOUND)
      ) {
        setPendingDelete(null);
        await reload();
        setLoadError(
          error.code === RESOURCE_NOT_FOUND
            ? 'Esa cosecha ya no existe: otra persona la eliminó. Se ha recargado el listado.'
            : 'Otra persona modificó esa cosecha mientras la mirabas. Se ha recargado el listado; revísala antes de eliminarla.'
        );
        return;
      }
      setDeleteError(error instanceof HttpError ? error.message : 'No se pudo eliminar la cosecha.');
    } finally {
      setDeleting(false);
    }
  };

  const hasFilters =
    plotFilter !== 'todos' || seasonScope.isExplicit || destinationFilter !== 'todos';

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Cosechas</h2>
          <p className="text-xs text-[#76786b]">
            Partidas recolectadas por terreno y temporada, con su destino y su rendimiento. También
            aparecen en el diario de campo.
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
          <span>Registrar cosecha</span>
        </button>
      </div>

      {!isLoading && missingMasters.length > 0 && (
        <div className="bg-[#fff6e5] border border-[#f0d9a8] rounded-2xl p-4 space-y-2">
          <p className="text-sm font-semibold text-[#8a5a00] flex items-center gap-1.5">
            <span className="material-symbols-outlined text-lg" aria-hidden="true">info</span>
            Antes de registrar una cosecha necesitas {missingMasters.map((m) => m.label).join(' y ')}.
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

      {/* Resumen de lo que se está viendo */}
      {!isLoading && harvests.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <SummaryTile label="Total recolectado" value={`${number(totalKg)} kg`} icon="scale" />
          <SummaryTile
            label="Rendimiento medio"
            value={
              yieldSummary.average === null ? 'Sin datos' : `${number(yieldSummary.average, 1)} L/100kg`
            }
            icon="percent"
            hint={
              yieldSummary.average === null
                ? 'Ninguna partida tiene todavía dato de aceite.'
                : yieldSummary.pending > 0
                  ? `Ponderado por kilos sobre ${yieldSummary.counted} de ${harvests.length} partidas.`
                  : 'Ponderado por kilos, no media de partidas.'
            }
          />
          <SummaryTile label="Partidas" value={String(harvests.length)} icon="inventory_2" />
        </div>
      )}

      {/* Filtros */}
      {(harvests.length > 0 || hasFilters) && (
        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div>
            <label htmlFor="harvest-filter-plot" className="sr-only">Filtrar por terreno</label>
            <select
              id="harvest-filter-plot"
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
            <label htmlFor="harvest-filter-season" className="sr-only">Filtrar por temporada</label>
            <select
              id="harvest-filter-season"
              value={seasonScope.value}
              onChange={(e) => seasonScope.select(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              {/* Mientras no haya llegado la primera respuesta no se sabe qué campaña aplica el
                  servidor: se deja el hueco en vez de rotular una que quizá no sea. */}
              {seasonScope.value === '' && <option value="">Campaña de trabajo…</option>}
              <option value={ALL_SEASONS}>Todas las temporadas</option>
              {seasons.map((season) => (
                <option key={season.id} value={season.id}>{season.name}</option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="harvest-filter-destination" className="sr-only">Filtrar por destino</label>
            <select
              id="harvest-filter-destination"
              value={destinationFilter}
              onChange={(e) => setDestinationFilter(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los destinos</option>
              {HARVEST_DESTINATIONS.map((value) => (
                <option key={value} value={value}>{harvestDestinationLabel(value)}</option>
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

      {/* Listado */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : harvests.length === 0 ? (
        hasFilters ? (
          <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
            No hay cosechas que coincidan con los filtros.
          </p>
        ) : (
          <EmptyHarvests canRegister={missingMasters.length === 0} onRegister={openCreate} />
        )
      ) : (
        <div className="bg-white rounded-2xl border border-[#e5e2dd] ambient-shadow overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs text-[#1c1c19]">
              <thead className="bg-[#f6f3ee] border-b border-[#e5e2dd] text-[11px] font-bold uppercase tracking-wider text-[#45483c]">
                <tr>
                  <th scope="col" className="px-5 py-3.5">Fecha</th>
                  <th scope="col" className="px-5 py-3.5">Terreno</th>
                  <th scope="col" className="px-5 py-3.5">Producto</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Kilos</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Aceite</th>
                  {/* MVP-707 — Importe de la partida: kilos × precio, derivado en servidor. */}
                  <th scope="col" className="px-5 py-3.5 text-right">Importe</th>
                  <th scope="col" className="px-5 py-3.5">Destino</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#f0ede8]">
                {harvests.map((harvest) => (
                  <tr key={harvest.id} className="hover:bg-[#fcf9f4] transition-colors">
                    <td className="px-5 py-4 font-medium text-[#76786b] whitespace-nowrap">
                      {formatDate(harvest.date)}
                      <span className="block text-[11px] text-[#a2a496]">{harvest.season_name}</span>
                      {/* RN-023 — aviso no bloqueante */}
                      {harvest.is_out_of_season_range && (
                        <span
                          title="La fecha queda fuera del rango de la temporada"
                          className="inline-block mt-1 text-[10px] font-bold px-1.5 py-0.5 rounded bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8]"
                        >
                          FUERA DE TEMPORADA
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-4 font-bold text-[#33450d]">{harvest.plot_name}</td>
                    <td className="px-5 py-4 font-semibold">{harvestProductLabel(harvest.product)}</td>
                    <td className="px-5 py-4 text-right font-extrabold whitespace-nowrap">
                      {number(harvest.kgs)} kg
                    </td>
                    {/* RN-013 — siempre en la unidad canónica, sea declarada o derivada de los
                        litros (RN-014). Lo derivado se marca para no presentarlo como declarado. */}
                    <td className="px-5 py-4 text-right whitespace-nowrap">
                      {harvest.effective_yield !== null ? (
                        <>
                          <span
                            className={`inline-block px-2.5 py-0.5 rounded-full font-bold ${
                              harvest.yield_source === 'informado'
                                ? 'bg-[#c9f16f] text-[#33450d]'
                                : 'bg-[#f0ede8] text-[#45483c]'
                            }`}
                            title={
                              harvest.yield_source === 'calculado'
                                ? `Calculado a partir de ${number(harvest.liters ?? 0, 1)} L obtenidos`
                                : undefined
                            }
                          >
                            {number(harvest.effective_yield, 1)} L/100kg
                          </span>
                          {harvest.yield_source === 'calculado' && (
                            <span className="block text-[10px] text-[#76786b] mt-0.5">
                              de {number(harvest.liters ?? 0, 1)} L
                            </span>
                          )}
                        </>
                      ) : (
                        <span className="text-[#a2a496] italic">Sin dato</span>
                      )}
                    </td>
                    {/* MVP-707 — Importe: kilos × precio. «Sin dato» y no «0,00 €» cuando no hay
                        precio: la partida no ha ingresado cero, es que no se sabe (CA-2). */}
                    <td className="px-5 py-4 text-right whitespace-nowrap">
                      {harvest.amount !== null ? (
                        <>
                          <span className="font-extrabold text-[#33450d]">{number(harvest.amount, 2)} €</span>
                          <span className="block text-[10px] text-[#76786b] mt-0.5">
                            {number(harvest.unit_price ?? 0, 2)} €/kg
                          </span>
                        </>
                      ) : (
                        <span className="text-[#a2a496] italic">Sin dato</span>
                      )}
                    </td>
                    <td className="px-5 py-4 whitespace-nowrap">
                      <span
                        className={`font-semibold ${
                          harvest.destination === 'desconocido' ? 'text-[#76786b] italic' : 'text-[#1c1c19]'
                        }`}
                      >
                        {harvestDestinationLabel(harvest.destination)}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-right whitespace-nowrap">
                      <button
                        type="button"
                        onClick={() => openEdit(harvest)}
                        title="Corregir cosecha"
                        aria-label={`Corregir la cosecha de ${harvest.plot_name} del ${formatDate(harvest.date)}`}
                        className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors"
                      >
                        <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          setDeleteError(null);
                          setPendingDelete(harvest);
                        }}
                        title="Eliminar cosecha"
                        aria-label={`Eliminar la cosecha de ${harvest.plot_name} del ${formatDate(harvest.date)}`}
                        className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#ffdad6]/60 hover:text-[#ba1a1a] transition-colors"
                      >
                        <span className="material-symbols-outlined text-base" aria-hidden="true">delete</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <HarvestFormModal
        isOpen={isModalOpen}
        harvest={editing}
        plots={plots}
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

      {/* RN-037 — confirmación explícita antes de eliminar */}
      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title="¿Eliminar la cosecha?"
        message={
          pendingDelete && (
            <>
              <p>
                Vas a eliminar la partida de <strong>{number(pendingDelete.kgs)} kg</strong> registrada
                el {formatDate(pendingDelete.date)} en {pendingDelete.plot_name}.
              </p>
              <p className="text-xs text-[#76786b]">
                Desaparecerá del listado, del diario y del dashboard. No hay papelera: si te equivocas,
                tendrás que volver a registrarla.
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

const SummaryTile: React.FC<{ label: string; value: string; icon: string; hint?: string }> = ({
  label,
  value,
  icon,
  hint,
}) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] px-4 py-3">
    <p className="text-[10px] font-bold text-[#76786b] uppercase flex items-center gap-1">
      <span className="material-symbols-outlined text-sm" aria-hidden="true">{icon}</span>
      {label}
    </p>
    <p className="font-headline font-extrabold text-lg text-[#1c1c19]">{value}</p>
    {hint && <p className="text-[10px] text-[#76786b] leading-tight">{hint}</p>}
  </div>
);

const EmptyHarvests: React.FC<{ canRegister: boolean; onRegister: () => void }> = ({
  canRegister,
  onRegister,
}) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-4">
    <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-3xl" aria-hidden="true">agriculture</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Todavía no hay cosechas</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">
        Apunta la primera partida: de qué terreno salió, cuántos kilos y a dónde va. Si aún no sabes el
        destino ni el rendimiento, puedes dejarlos para después.
      </p>
    </div>
    {canRegister && (
      <button
        type="button"
        onClick={onRegister}
        className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
      >
        <span className="material-symbols-outlined text-lg" aria-hidden="true">add</span>
        Registrar cosecha
      </button>
    )}
  </div>
);
