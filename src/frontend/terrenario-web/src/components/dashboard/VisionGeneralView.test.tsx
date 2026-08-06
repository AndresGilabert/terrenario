import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { VisionGeneralView } from './VisionGeneralView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import { UsageEvent } from '../../lib/usage-telemetry';
import type { UsageEventPayload } from '../../services/telemetry.service';
import type {
  DashboardKgByDestination,
  DashboardKgByPlot,
  DashboardScope,
  DashboardSummary,
  DashboardYieldEvolution,
} from '../../types/dashboard.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

vi.mock('../../contexts/SeasonContext', () => ({
  useSeason: () => ({
    seasons: [{ id: 's-1', name: 'Campaña 2025/26' }],
    activeSeason: { id: 's-1', name: 'Campaña 2025/26' },
  }),
}));

const logUsage = vi.fn();
vi.mock('../../lib/use-usage-telemetry', () => ({ useUsageTelemetry: () => logUsage }));

const scope: DashboardScope = {
  season: {
    id: 's-1',
    name: 'Campaña 2025/26',
    status: 'abierta',
    start_date: '2025-10-01',
    end_date: null,
  },
  plot_ids: ['p-1'],
  plots: 1,
};

const summary = (harvests: number): DashboardSummary => ({
  scope,
  total_kg: harvests > 0 ? 1200 : 0,
  total_liters: null,
  average_yield: null,
  harvests,
  harvests_with_oil_data: 0,
  kg_per_tree: null,
  trees_counted: 0,
  plots_counted: 0,
  plots_without_tree_count: 0,
});

const destinations = (empty: boolean): DashboardKgByDestination => ({
  scope,
  data: empty ? [] : [{ destination: 'aceite_para_venta', kg: 1200 }],
  meta: { total_kg: empty ? 0 : 1200 },
});

const kgByPlot = (empty: boolean): DashboardKgByPlot => ({
  scope,
  data: empty ? [] : [{ plot_id: 'p-1', plot_name: 'La Vega', kg: 1200 }],
  meta: { total_kg: empty ? 0 : 1200 },
});

const evolution = (empty: boolean, history: number | null = null): DashboardYieldEvolution => ({
  scope,
  granularity: 'month',
  data: empty ? [] : [{ period: '2026-01', yield_l_per_100kg: 18, kg: 1200 }],
  history: {
    average: history,
    average_5_years: null,
    average_10_years: null,
    prior_years_with_data: history === null ? 0 : 3,
    window: null,
  },
});

/** Widgets de la última señal `dashboard_widgets`, como mapa widget → estado. */
function lastWidgets(): Record<string, string> {
  const calls = logUsage.mock.calls.filter((call) => call[0] === UsageEvent.DashboardWidgets);
  const payload = calls[calls.length - 1]?.[1] as UsageEventPayload | undefined;
  return Object.fromEntries((payload?.widgets ?? []).map((w) => [w.widget, w.status]));
}

const eventos = () => logUsage.mock.calls.map((call) => call[0] as string);

/** Marca de «primera vez en la sesión» de la señal de entrada al dashboard. */
function firstInSession(): boolean | undefined {
  const vista = logUsage.mock.calls.find((call) => call[0] === UsageEvent.DashboardViewed);
  return vista === undefined ? undefined : (vista[1] as UsageEventPayload | undefined)?.firstInSession;
}

/**
 * MVP-602 — Las señales de uso del dashboard. Lo que se comprueba aquí no es que la pantalla pinte,
 * que ya lo cubre MVP-403/404, sino que **mide lo que el KPI de la KB pregunta**: sesiones con uso
 * frente a visitas, recarga manual frente a cambio de filtro, y vacío frente a error.
 */
describe('VisionGeneralView — señales de uso', () => {
  const renderWith = (
    data: {
      summary?: DashboardSummary;
      destinations?: DashboardKgByDestination;
      plots?: DashboardKgByPlot;
      evolution?: DashboardYieldEvolution;
    } = {},
    { falla = false }: { falla?: boolean } = {}
  ) => {
    const fail = () => {
      throw new Error('la API no responde');
    };

    http = createFakeHttpClient({
      '/api/v1/dashboard/summary': falla ? fail : (data.summary ?? summary(3)),
      '/api/v1/dashboard/kg-by-destination': falla ? fail : (data.destinations ?? destinations(false)),
      '/api/v1/dashboard/kg-by-plot': falla ? fail : (data.plots ?? kgByPlot(false)),
      '/api/v1/dashboard/yield-evolution': falla ? fail : (data.evolution ?? evolution(false)),
      '/api/v1/plots': { data: [], meta: { total: 0 } },
    });

    render(
      <MemoryRouter>
        <VisionGeneralView />
      </MemoryRouter>
    );
  };

  beforeEach(() => {
    sessionStorage.clear();
    logUsage.mockClear();
  });

  it('marca la entrada como primera de la sesión', async () => {
    renderWith();

    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardViewed));
    expect(firstInSession()).toBe(true);
  });

  it('no vuelve a marcarla como primera si la sesión ya pasó por aquí', async () => {
    renderWith();
    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardViewed));
    logUsage.mockClear();

    renderWith();

    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardViewed));
    expect(firstInSession()).toBe(false);
  });

  it('cuenta la recarga manual solo al pulsar «Actualizar»', async () => {
    renderWith();
    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardWidgets));
    expect(eventos()).not.toContain(UsageEvent.DashboardManualRefresh);

    await userEvent.click(screen.getByRole('button', { name: /Actualizar/ }));

    expect(eventos().filter((e) => e === UsageEvent.DashboardManualRefresh)).toHaveLength(1);
  });

  it('no cuenta como recarga manual el cambio de temporada', async () => {
    // Cambiar el filtro también relanza la carga, pero es otra pregunta: «qué quiero ver», no «dame lo
    // último». Mezclarlas inflaría el KPI de recargas con cada uso normal de los filtros.
    renderWith();
    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardWidgets));

    await userEvent.selectOptions(screen.getByLabelText('Temporada'), 's-1');

    expect(eventos()).not.toContain(UsageEvent.DashboardManualRefresh);
  });

  it('informa de los cuatro widgets con datos como «ok»', async () => {
    renderWith();

    await waitFor(() =>
      expect(lastWidgets()).toEqual({
        summary: 'ok',
        kg_by_destination: 'ok',
        kg_by_plot: 'ok',
        yield_evolution: 'ok',
      })
    );
  });

  it('distingue el widget vacío del widget roto', async () => {
    // Un Workspace que aún no ha cosechado no tiene el dashboard roto. Si «vacío» contase como error,
    // la cobertura bajaría con cada alta nueva y la alerta saltaría por el uso normal del producto.
    renderWith({
      summary: summary(0),
      destinations: destinations(true),
      plots: kgByPlot(true),
      evolution: evolution(true),
    });

    await waitFor(() =>
      expect(lastWidgets()).toEqual({
        summary: 'empty',
        kg_by_destination: 'empty',
        kg_by_plot: 'empty',
        yield_evolution: 'empty',
      })
    );
  });

  it('cuenta la evolución como mostrable cuando solo trae histórico', async () => {
    // Es el caso que MVP-404 resolvió para que la pantalla no quedase en blanco antes de la primera
    // cosecha: hay algo que enseñar, así que el widget está cubierto.
    renderWith({ summary: summary(0), evolution: evolution(true, 17.5) });

    await waitFor(() => expect(lastWidgets().yield_evolution).toBe('ok'));
  });

  it('informa de los cuatro widgets como error cuando la carga falla', async () => {
    renderWith({}, { falla: true });

    await waitFor(() =>
      expect(lastWidgets()).toEqual({
        summary: 'error',
        kg_by_destination: 'error',
        kg_by_plot: 'error',
        yield_evolution: 'error',
      })
    );
  });
});
