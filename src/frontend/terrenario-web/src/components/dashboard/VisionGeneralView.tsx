import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApiClient } from '../../contexts/ApiContext';
import { createDashboardService } from '../../services/dashboard.service';
import { HttpError } from '../../services/http-client';
import type {
  DashboardKgByDestination,
  DashboardKgByPlot,
  DashboardSummary,
  DashboardYieldEvolution,
} from '../../types/dashboard.types';
import { harvestDestinationLabel } from '../../types/harvest.types';

const number = (value: number, decimals = 0) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });

/** Etiqueta legible de un periodo `YYYY-MM` o `YYYY-Www` para el eje del gráfico de evolución. */
function periodLabel(period: string): string {
  const week = period.match(/^(\d{4})-W(\d{2})$/);
  if (week) return `Sem. ${Number(week[2])}`;
  const month = period.match(/^(\d{4})-(\d{2})$/);
  if (!month) return period;
  const date = new Date(Number(month[1]), Number(month[2]) - 1, 1);
  return date.toLocaleDateString('es-ES', { month: 'short', year: '2-digit' });
}

/**
 * Color de cada destino en el gráfico. El orden de la paleta acompaña al orden de lectura (kg
 * descendentes), y `desconocido` va siempre en gris: no es una categoría más, es la ausencia de
 * clasificación (RN-012).
 */
const DESTINATION_COLORS: Record<string, string> = {
  aceite_para_venta: 'bg-[#33450d]',
  venta_aceituna: 'bg-[#4a5d23]',
  aceite_personal: 'bg-[#c9f16f]',
  desconocido: 'bg-[#dcd9d2]',
};

const colorFor = (destination: string) => DESTINATION_COLORS[destination] ?? 'bg-[#7a6a1f]';

/**
 * Visión General del Workspace (MVP-403 · MVP-404): los **cuatro** widgets del dashboard MVP —resumen,
 * kg por destino, kg por terreno y evolución de rendimiento— en **una sola pantalla con scroll
 * vertical** (RN-005) y **sin refresco continuo** (RN-006): los datos se recalculan al entrar o al
 * recargar, y la pantalla lo dice en vez de dejar creer que están vivos.
 *
 * Los filtros por temporada y terrenos, su persistencia tras recarga y el KPI de kg/árbol con su aviso
 * de dato incompleto son alcance de `MVP-405`; aquí el ámbito lo resuelve el servidor con los defectos
 * de RN-008 (temporada activa y todos los terrenos activos) y se muestra cuál ha aplicado.
 */
export const VisionGeneralView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const dashboardService = useMemo(() => createDashboardService(http), [http]);

  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [destinations, setDestinations] = useState<DashboardKgByDestination | null>(null);
  const [plots, setPlots] = useState<DashboardKgByPlot | null>(null);
  const [evolution, setEvolution] = useState<DashboardYieldEvolution | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      // Las cuatro peticiones van juntas: agregan el mismo ámbito y la pantalla las presenta a la vez.
      const [summaryData, destinationData, plotData, evolutionData] = await Promise.all([
        dashboardService.getSummary(),
        dashboardService.getKgByDestination(),
        dashboardService.getKgByPlot(),
        dashboardService.getYieldEvolution(),
      ]);
      setSummary(summaryData);
      setDestinations(destinationData);
      setPlots(plotData);
      setEvolution(evolutionData);
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudo cargar la visión general.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [dashboardService]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const season = summary?.scope.season ?? null;
  const totalKg = destinations?.meta.total_kg ?? 0;
  const hasProduction = (summary?.harvests ?? 0) > 0;

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Visión General</h2>
          <p className="text-xs text-[#76786b]">
            {season
              ? `Producción de ${season.name}${season.is_active ? ' (activa)' : ''}, sobre ${
                  summary?.scope.plots ?? 0
                } ${summary?.scope.plots === 1 ? 'terreno' : 'terrenos'}.`
              : 'Resumen de producción del Workspace.'}
          </p>
        </div>

        {/* RN-006 — no hay actualización en segundo plano: el refresco es un acto explícito */}
        <button
          type="button"
          onClick={() => void reload()}
          className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-white border border-[#c6c8b8] hover:bg-[#f0ede8] text-[#33450d] text-xs font-semibold transition-colors shrink-0"
        >
          <span className="material-symbols-outlined text-lg" aria-hidden="true">refresh</span>
          <span>Actualizar</span>
        </button>
      </div>

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {/* RN-021 — sin temporada no hay nada que agregar: se pide, no se muestran ceros */}
      {!season ? (
        <EmptyState
          icon="calendar_today"
          title="Todavía no hay temporada que mirar"
          message="Toda la producción se asocia a una campaña. Crea o activa una temporada y aquí aparecerá su resumen."
          actionLabel="Ir a Temporadas"
          onAction={() => navigate('/app/temporadas')}
        />
      ) : !hasProduction ? (
        <EmptyState
          icon="agriculture"
          title={`Sin cosechas en ${season.name}`}
          message="En cuanto registres la primera partida verás aquí los kilos, el aceite obtenido y el reparto por destino."
          actionLabel="Registrar cosecha"
          onAction={() => navigate('/app/cosechas')}
        />
      ) : (
        <>
          {/* Resumen de temporada (CA-1) */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <MetricCard
              label="Kg recolectados"
              value={`${number(summary!.total_kg)} kg`}
              icon="scale"
              iconClass="bg-[#f0ede8] text-[#33450d]"
              hint={`${summary!.harvests} ${summary!.harvests === 1 ? 'partida' : 'partidas'}`}
            />
            <MetricCard
              label="Aceite obtenido"
              /* RN-014 — declarado o derivado del rendimiento. `null` es desconocido, no cero. */
              value={summary!.total_liters === null ? 'Sin dato' : `${number(summary!.total_liters)} L`}
              icon="water_drop"
              iconClass="bg-[#c9f16f] text-[#33450d]"
              hint={
                summary!.total_liters === null
                  ? 'Ninguna partida declara aceite todavía'
                  : coverageHint(summary!)
              }
            />
            <MetricCard
              label="Rendimiento medio"
              /* RN-013 — unidad canónica L/100kg, ponderado por kilos */
              value={
                summary!.average_yield === null
                  ? 'Sin dato'
                  : `${number(summary!.average_yield, 1)} L/100kg`
              }
              icon="percent"
              iconClass="bg-[#f0ede8] text-[#4a5d23]"
              hint={
                summary!.average_yield === null
                  ? 'Se calculará al declarar el aceite'
                  : 'Ponderado por kilos, no media de partidas'
              }
            />
          </div>

          {/* Aviso de cobertura parcial: una media sobre parte de las partidas no es la de la campaña */}
          {summary!.harvests_with_oil_data > 0 &&
            summary!.harvests_with_oil_data < summary!.harvests && (
              <p className="text-xs text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-xl px-3 py-2 flex items-start gap-1.5">
                <span className="material-symbols-outlined text-base shrink-0" aria-hidden="true">info</span>
                <span>
                  El aceite y el rendimiento se calculan sobre {summary!.harvests_with_oil_data} de{' '}
                  {summary!.harvests} partidas: el resto todavía no declara litros ni rendimiento.
                </span>
              </p>
            )}

          {/* Kg por destino (CA-2) */}
          <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-5">
            <div>
              <h3 className="font-headline font-bold text-base text-[#1c1c19]">Kg por destino</h3>
              <p className="text-xs text-[#76786b]">A dónde va lo recolectado en esta temporada.</p>
            </div>

            {destinations!.data.length === 0 ? (
              <p className="text-sm text-[#76786b] italic">Sin datos de destino en este ámbito.</p>
            ) : (
              <>
                {/* Barra apilada: el reparto de un vistazo */}
                <div className="w-full h-4 rounded-xl overflow-hidden flex shadow-inner bg-[#f0ede8]">
                  {destinations!.data.map((item) => {
                    const share = totalKg > 0 ? (item.kg / totalKg) * 100 : 0;
                    return (
                      <div
                        key={item.destination}
                        className={`${colorFor(item.destination)} h-full`}
                        style={{ width: `${share}%` }}
                        title={`${harvestDestinationLabel(item.destination)}: ${number(item.kg)} kg`}
                      />
                    );
                  })}
                </div>

                <ul className="space-y-2">
                  {destinations!.data.map((item) => {
                    const share = totalKg > 0 ? (item.kg / totalKg) * 100 : 0;
                    const isUnknown = item.destination === 'desconocido';
                    return (
                      <li
                        key={item.destination}
                        className="flex items-center justify-between gap-3 text-xs p-2.5 bg-[#f6f3ee] rounded-xl border border-[#e5e2dd]"
                      >
                        <span className="flex items-center gap-2.5 min-w-0">
                          <span
                            className={`w-3 h-3 rounded-full ${colorFor(item.destination)} border border-black/10 shrink-0`}
                            aria-hidden="true"
                          />
                          <span
                            className={`font-semibold truncate ${
                              isUnknown ? 'text-[#76786b] italic' : 'text-[#1c1c19]'
                            }`}
                          >
                            {harvestDestinationLabel(item.destination)}
                          </span>
                        </span>
                        <span className="text-right shrink-0">
                          <span className="block font-bold text-[#1c1c19]">{number(item.kg)} kg</span>
                          <span className="block text-[11px] text-[#76786b]">
                            {number(share, 1)} % del total
                          </span>
                        </span>
                      </li>
                    );
                  })}
                </ul>

                {/* RN-012 — el destino sin clasificar es parte de la lectura, no un error a esconder */}
                {destinations!.data.some((item) => item.destination === 'desconocido') && (
                  <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
                    <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
                    «Sin destino» son partidas cuyo cierre comercial o de uso todavía no se ha
                    registrado. Puedes completarlo desde Cosechas cuando lo sepas.
                  </p>
                )}
              </>
            )}
          </div>

          {/* Kg por terreno (MVP-404, CA-1) — barras en orden fijo de RN-011 */}
          {plots && plots.data.length > 0 && (
            <KgByPlotWidget data={plots.data} totalKg={plots.meta.total_kg} />
          )}

          {/* Evolución de rendimiento (MVP-404, CA-2) — serie en L/100kg con histórico básico */}
          {evolution && <YieldEvolutionWidget evolution={evolution} />}

          <p className="text-[11px] text-[#76786b] text-center">
            Los datos se calculan al entrar en la pantalla o al pulsar «Actualizar»; no se refrescan
            solos.
          </p>
        </>
      )}
    </div>
  );
};

/**
 * Kg por terreno (MVP-404, CA-1). Barras horizontales en el **orden fijo de RN-011** —kg descendentes,
 * ya resuelto en servidor—, con la barra proporcional al mayor para comparar de un vistazo qué parcela
 * aporta más. No hay orden manual (RN-011).
 */
const KgByPlotWidget: React.FC<{ data: DashboardKgByPlot['data']; totalKg: number }> = ({
  data,
  totalKg,
}) => {
  const max = data[0]?.kg ?? 0;
  return (
    <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-5">
      <div>
        <h3 className="font-headline font-bold text-base text-[#1c1c19]">Kg por terreno</h3>
        <p className="text-xs text-[#76786b]">Cuánto aporta cada parcela, de mayor a menor.</p>
      </div>

      <ul className="space-y-3">
        {data.map((item) => {
          const width = max > 0 ? (item.kg / max) * 100 : 0;
          const share = totalKg > 0 ? (item.kg / totalKg) * 100 : 0;
          return (
            <li key={item.plot_id} className="space-y-1">
              <div className="flex items-center justify-between text-xs font-semibold gap-2">
                <span className="text-[#1c1c19] truncate">{item.plot_name}</span>
                <span className="text-[#33450d] shrink-0">
                  {number(item.kg)} kg <span className="text-[#76786b] font-normal">({number(share, 1)} %)</span>
                </span>
              </div>
              <div className="w-full bg-[#f0ede8] h-3.5 rounded-lg overflow-hidden">
                <div
                  className="bg-[#33450d] h-full rounded-lg transition-all"
                  style={{ width: `${width}%` }}
                />
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
};

/**
 * Evolución de rendimiento (MVP-404, CA-2). Serie del ámbito en la unidad canónica L/100kg (RN-013) y,
 * cuando hay histórico suficiente, la **línea de referencia** de la media histórica (RN-015).
 *
 * La comparativa aparece solo si el servidor la calcula: sin temporadas previas con dato, `history` es
 * `null` y no se dibuja una referencia inventada.
 */
const YieldEvolutionWidget: React.FC<{ evolution: DashboardYieldEvolution }> = ({ evolution }) => {
  const { data, history } = evolution;
  const reference = history.average;

  // Escala común para las barras y la línea de referencia: el máximo de la serie y de la referencia,
  // con un pequeño margen para que la barra más alta no toque el techo.
  const peak = Math.max(...data.map((p) => p.yield_l_per_100kg), reference ?? 0);
  const ceiling = peak > 0 ? peak * 1.1 : 1;

  if (data.length === 0) {
    return (
      <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-2">
        <h3 className="font-headline font-bold text-base text-[#1c1c19]">Evolución del rendimiento</h3>
        <p className="text-sm text-[#76786b] italic">
          Aún no hay rendimiento registrado en esta temporada. Aparecerá aquí cuando declares el aceite
          de alguna partida.
        </p>
      </div>
    );
  }

  return (
    <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-5">
      <div className="flex items-start justify-between gap-3 flex-wrap">
        <div>
          <h3 className="font-headline font-bold text-base text-[#1c1c19]">Evolución del rendimiento</h3>
          <p className="text-xs text-[#76786b]">Rendimiento medio por periodo, en litros por 100 kg.</p>
        </div>
        {reference !== null && (
          <span className="text-[11px] font-semibold text-[#4a5d23] bg-[#eef2e0] border border-[#c9dba0] px-2.5 py-1 rounded-full">
            Media histórica: {number(reference, 1)} L/100kg
          </span>
        )}
      </div>

      {/* Gráfico de barras con la referencia histórica superpuesta */}
      <div className="relative h-44 flex items-end justify-between gap-3 pt-6 border-b border-[#e5e2dd] px-1">
        {reference !== null && (
          <div
            className="absolute left-0 right-0 border-t-2 border-dashed border-[#4a5d23]/60 z-10"
            style={{ bottom: `${(reference / ceiling) * 100}%` }}
            title={`Media histórica: ${number(reference, 1)} L/100kg`}
          />
        )}
        {data.map((point) => {
          const height = ceiling > 0 ? (point.yield_l_per_100kg / ceiling) * 100 : 0;
          return (
            <div
              key={point.period}
              className="flex-1 flex flex-col items-center gap-1.5 h-full justify-end group min-w-0"
            >
              <span className="text-[10px] font-bold text-[#33450d]">
                {number(point.yield_l_per_100kg, 1)}
              </span>
              <div className="w-full max-w-[40px] flex items-end h-full">
                <div
                  className="w-full bg-[#33450d] rounded-t-lg transition-all group-hover:bg-[#4a5d23]"
                  style={{ height: `${height}%` }}
                  title={`${periodLabel(point.period)}: ${number(point.yield_l_per_100kg, 1)} L/100kg sobre ${number(point.kg)} kg`}
                />
              </div>
              <span className="text-[10px] font-semibold text-[#76786b] truncate w-full text-center">
                {periodLabel(point.period)}
              </span>
            </div>
          );
        })}
      </div>

      {/* CA-2 — el histórico solo aparece cuando existe suficiente información; se dice sobre cuánto */}
      {reference === null ? (
        <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
          <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
          La comparativa histórica aparecerá cuando haya temporadas anteriores con rendimiento
          registrado.
        </p>
      ) : (
        <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
          <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
          La línea de referencia es la media de {history.prior_seasons_with_data}{' '}
          {history.prior_seasons_with_data === 1 ? 'temporada anterior' : 'temporadas anteriores'} con
          dato de rendimiento.
        </p>
      )}
    </div>
  );
};

/** Sobre qué parte de la campaña se ha calculado el aceite, cuando no es sobre toda. */
function coverageHint(summary: DashboardSummary): string {
  return summary.harvests_with_oil_data === summary.harvests
    ? 'Declarado o calculado del rendimiento'
    : `Sobre ${summary.harvests_with_oil_data} de ${summary.harvests} partidas`;
}

const MetricCard: React.FC<{
  label: string;
  value: string;
  icon: string;
  iconClass: string;
  hint?: string;
}> = ({ label, value, icon, iconClass, hint }) => (
  <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-3">
    <div className="flex items-center justify-between gap-2">
      <span className="text-xs font-bold uppercase tracking-wider text-[#76786b]">{label}</span>
      <div className={`w-8 h-8 rounded-lg flex items-center justify-center shrink-0 ${iconClass}`}>
        <span className="material-symbols-outlined text-lg" aria-hidden="true">{icon}</span>
      </div>
    </div>
    <div>
      <p className="font-headline font-extrabold text-2xl text-[#1c1c19]">{value}</p>
      {hint && <p className="text-[11px] text-[#76786b] mt-1 leading-tight">{hint}</p>}
    </div>
  </div>
);

const EmptyState: React.FC<{
  icon: string;
  title: string;
  message: string;
  actionLabel: string;
  onAction: () => void;
}> = ({ icon, title, message, actionLabel, onAction }) => (
  <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-4">
    <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
      <span className="material-symbols-outlined text-3xl" aria-hidden="true">{icon}</span>
    </div>
    <div className="space-y-1">
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">{title}</h3>
      <p className="text-sm text-[#45483c] max-w-md mx-auto">{message}</p>
    </div>
    <button
      type="button"
      onClick={onAction}
      className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
    >
      {actionLabel}
    </button>
  </div>
);
