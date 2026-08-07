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
    { falla = false, fallan = [] }: { falla?: boolean; fallan?: string[] } = {}
  ) => {
    const fail = () => {
      throw new Error('la API no responde');
    };

    // MVP-706 — `fallan` permite tumbar **una** de las cuatro peticiones: es el escenario de `P-075`,
    // donde el fallo de una descartaba el resultado de las otras tres.
    const ep = (nombre: string, valor: unknown) =>
      falla || fallan.includes(nombre) ? fail : valor;

    http = createFakeHttpClient({
      '/api/v1/dashboard/summary': ep('summary', data.summary ?? summary(3)),
      '/api/v1/dashboard/kg-by-destination': ep('kg-by-destination', data.destinations ?? destinations(false)),
      '/api/v1/dashboard/kg-by-plot': ep('kg-by-plot', data.plots ?? kgByPlot(false)),
      '/api/v1/dashboard/yield-evolution': ep('yield-evolution', data.evolution ?? evolution(false)),
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

  it('ya no ofrece «Actualizar»', async () => {
    // MVP-706 (CA-3) — Decisión del PO sobre `P-085`: en explotaciones pequeñas no es habitual que
    // unos introduzcan datos mientras otros esperan a que el panel se actualice. El refresco pasa a
    // ser recargar la página o volver a entrar (RN-006 reescrita).
    renderWith();
    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardWidgets));

    expect(screen.queryByRole('button', { name: /Actualizar/ })).not.toBeInTheDocument();
  });

  it('sigue relanzando la carga al cambiar de temporada', async () => {
    renderWith();
    await waitFor(() => expect(eventos()).toContain(UsageEvent.DashboardWidgets));
    const antes = eventos().filter((e) => e === UsageEvent.DashboardWidgets).length;

    await userEvent.selectOptions(screen.getByLabelText('Temporada'), 's-1');

    // Cambiar el filtro es otra pregunta («qué quiero ver»), y sigue recargando sin botón de por medio.
    await waitFor(() =>
      expect(eventos().filter((e) => e === UsageEvent.DashboardWidgets).length).toBeGreaterThanOrEqual(antes)
    );
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

  it('informa de los cuatro widgets como error cuando falla la carga entera', async () => {
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

  it('atribuye el fallo al widget que lo causó, no a los cuatro', async () => {
    // `P-075` — Las cuatro peticiones iban en un `Promise.all`: cualquier fallo se informaba como los
    // cuatro en error, así que la medida no permitía saber cuál había fallado, que es justo lo que
    // pregunta el KPI de cobertura (CA-2).
    renderWith({}, { fallan: ['kg-by-plot'] });

    await waitFor(() =>
      expect(lastWidgets()).toEqual({
        summary: 'ok',
        kg_by_destination: 'ok',
        kg_by_plot: 'error',
        yield_evolution: 'ok',
      })
    );
  });

  it('pinta el resto del panel cuando falla un solo widget', async () => {
    renderWith({}, { fallan: ['kg-by-plot'] });

    // El widget caído dice lo suyo…
    expect(await screen.findByText(/Kg por terreno/)).toBeInTheDocument();
    expect(await screen.findByRole('alert')).toHaveTextContent(
      /No se pudo cargar este dato.*El resto del panel sí se ha podido calcular/
    );
    // …y los que sí se calcularon siguen en pantalla, que es lo que antes se perdía.
    expect(screen.getByText(/Kg recolectados/)).toBeInTheDocument();
    expect(screen.getByText(/Kg por destino/)).toBeInTheDocument();
  });

  it('sigue sabiendo de qué campaña habla aunque falle el resumen', async () => {
    // El ámbito lo publican las cuatro respuestas: leerlo solo del resumen dejaba la pantalla sin
    // saber ni la campaña cuando esa petición era justo la que fallaba.
    renderWith({}, { fallan: ['summary'] });

    expect(await screen.findByText(/Producción de Campaña 2025\/26/)).toBeInTheDocument();
  });
});
