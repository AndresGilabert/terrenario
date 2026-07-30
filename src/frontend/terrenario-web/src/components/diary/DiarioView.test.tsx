import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DiarioView } from './DiarioView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import type { DiaryEntry, DiaryListResponse } from '../../types/diary.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

const seasonState = {
  seasons: [{ id: 's-1', name: 'Campaña 2025/26' }],
  activeSeason: { id: 's-1', name: 'Campaña 2025/26' },
};
vi.mock('../../contexts/SeasonContext', () => ({ useSeason: () => seasonState }));

/**
 * MVP-501 — El diario es **la vista principal del MVP** (RN-033) y la que más lógica de presentación
 * concentra: qué filtros viajan al servidor, cuál se resuelve en cliente, qué avisos aparecen y qué
 * exige confirmación (RN-037). Es también la superficie que `MVP-506` va a reescribir para paginar,
 * así que esta cobertura es la red de regresión de esa historia.
 */
describe('DiarioView', () => {
  const entry = (overrides: Partial<DiaryEntry> = {}): DiaryEntry => ({
    type: 'actividad',
    id: 'a-1',
    date: '2026-07-20',
    title: 'Poda',
    description: null,
    plot_id: 'p-1',
    plot_name: 'La Vega',
    season_id: 's-1',
    season_name: 'Campaña 2025/26',
    cost: 120,
    version: 1,
    is_out_of_season_range: false,
    created_at: '2026-07-20T09:00:00Z',
    worker_name: 'Antonio',
    hours: 4,
    task_id: 't-1',
    quantity: null,
    has_purchase: null,
    kgs: null,
    destination: null,
    yield: null,
    ...overrides,
  });

  const meta = (overrides: Partial<DiaryListResponse['meta']> = {}): DiaryListResponse['meta'] => ({
    total: 1,
    total_cost: 120,
    imputed_cost: 0,
    activities: 1,
    purchases: 0,
    consumptions: 0,
    consumptions_without_purchase: 0,
    harvests: 0,
    total_kg: 0,
    ...overrides,
  });

  const renderWith = (
    diary: Partial<DiaryListResponse> = {},
    masters: { plots?: unknown[]; workers?: unknown[]; tasks?: unknown[] } = {},
    extraRoutes: Record<string, unknown> = {}
  ) => {
    http = createFakeHttpClient({
      '/api/v1/diary': { data: diary.data ?? [entry()], meta: diary.meta ?? meta() },
      // Los maestros responden con la envoltura `{ data, meta }` del contrato: sus servicios
      // devuelven `body.data`, así que el doble tiene que hablar el mismo idioma que la API.
      '/api/v1/plots': { data: masters.plots ?? [{ id: 'p-1', name: 'La Vega', is_active: true }] },
      '/api/v1/workers': { data: masters.workers ?? [{ id: 'w-1', name: 'Antonio', is_active: true }] },
      '/api/v1/tasks': { data: masters.tasks ?? [{ id: 't-1', name: 'Poda', is_active: true }] },
      ...extraRoutes,
    });

    return render(
      <MemoryRouter>
        <DiarioView />
      </MemoryRouter>
    );
  };

  const lastDiaryQuery = () => {
    const calls = http.callsTo('/api/v1/diary');
    return calls[calls.length - 1]?.options.query ?? {};
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('filtros resueltos en servidor (MVP-305)', () => {
    it('Deberia_PedirElDiarioSinFiltros_Cuando_SeAbreLaVista', async () => {
      renderWith();
      await screen.findByText('Poda');

      expect(lastDiaryQuery()).toMatchObject({
        plot_id: undefined,
        season_id: undefined,
        type: undefined,
      });
    });

    it('Deberia_MandarElTipoAlServidor_Cuando_SeFiltraPorTipoDeRegistro', async () => {
      const user = userEvent.setup();
      renderWith();
      await screen.findByText('Poda');

      await user.selectOptions(screen.getByLabelText('Filtrar por tipo de registro'), 'cosecha');

      await waitFor(() => expect(lastDiaryQuery().type).toBe('cosecha'));
    });

    it('Deberia_MandarElTerrenoAlServidor_Cuando_SeFiltraPorTerreno', async () => {
      const user = userEvent.setup();
      renderWith();
      await screen.findByText('Poda');

      await user.selectOptions(screen.getByLabelText('Filtrar por terreno'), 'p-1');

      await waitFor(() => expect(lastDiaryQuery().plot_id).toBe('p-1'));
    });

    it('Deberia_MandarLaTemporadaAlServidor_Cuando_SeFiltraPorTemporada', async () => {
      const user = userEvent.setup();
      renderWith();
      await screen.findByText('Poda');

      await user.selectOptions(screen.getByLabelText('Filtrar por temporada'), 's-1');

      await waitFor(() => expect(lastDiaryQuery().season_id).toBe('s-1'));
    });

    it('Deberia_AvisarDeQueLasComprasQuedanFuera_Cuando_SeFiltraPorTerreno', async () => {
      const user = userEvent.setup();
      renderWith();
      await screen.findByText('Poda');

      await user.selectOptions(screen.getByLabelText('Filtrar por terreno'), 'p-1');

      // Una compra es del Workspace, no de un terreno: se explica en vez de dejar un hueco.
      expect(
        await screen.findByText(/al filtrar por terreno no se muestran compras/i)
      ).toBeInTheDocument();
    });
  });

  describe('búsqueda por texto (P-052: hoy es local)', () => {
    const entries = [
      entry({ id: 'a-1', title: 'Poda', plot_name: 'La Vega', worker_name: 'Antonio' }),
      entry({ id: 'a-2', title: 'Riego', plot_name: 'El Cerro', worker_name: 'Lucía' }),
    ];

    it('Deberia_NoLlamarAlServidor_Cuando_SeEscribeEnLaBusqueda', async () => {
      const user = userEvent.setup();
      renderWith({ data: entries, meta: meta({ total: 2, activities: 2 }) });
      await screen.findByText('Poda');

      const before = http.callsTo('/api/v1/diary').length;
      await user.type(screen.getByLabelText('Buscar en el diario'), 'riego');

      // Comportamiento actual: teclear no dispara una petición por letra. `MVP-506` lo cambia.
      expect(http.callsTo('/api/v1/diary')).toHaveLength(before);
    });

    it('Deberia_DejarSoloLoQueCoincide_Cuando_SeBuscaPorTitulo', async () => {
      const user = userEvent.setup();
      renderWith({ data: entries, meta: meta({ total: 2, activities: 2 }) });
      await screen.findByText('Poda');

      await user.type(screen.getByLabelText('Buscar en el diario'), 'riego');

      expect(screen.queryByText('Poda')).not.toBeInTheDocument();
      expect(screen.getByText('Riego')).toBeInTheDocument();
    });

    it('Deberia_BuscarTambienPorResponsable_Cuando_ElTerminoNoEstaEnElTitulo', async () => {
      const user = userEvent.setup();
      renderWith({ data: entries, meta: meta({ total: 2, activities: 2 }) });
      await screen.findByText('Poda');

      await user.type(screen.getByLabelText('Buscar en el diario'), 'lucía');

      expect(screen.getByText('Riego')).toBeInTheDocument();
      expect(screen.queryByText('Poda')).not.toBeInTheDocument();
    });

    it('Deberia_DecirloExplicitamente_Cuando_LaBusquedaNoDejaNada', async () => {
      const user = userEvent.setup();
      renderWith({ data: entries, meta: meta({ total: 2, activities: 2 }) });
      await screen.findByText('Poda');

      await user.type(screen.getByLabelText('Buscar en el diario'), 'zzz');

      // Un muro vacío sin explicación se lee como «no hay nada registrado», que es otra cosa.
      expect(screen.getByText(/no hay registros que coincidan con los filtros/i)).toBeInTheDocument();
    });
  });

  describe('avisos de calidad del dato', () => {
    it('Deberia_AvisarDeLosConsumosSinCompra_Cuando_LosHay', async () => {
      renderWith({
        data: [entry({ type: 'consumo', id: 'c-1', title: 'Abono', has_purchase: false, cost: 0 })],
        meta: meta({ consumptions: 1, consumptions_without_purchase: 1, activities: 0 }),
      });

      expect(
        await screen.findByText(/su coste consta como 0 porque se desconoce/i)
      ).toBeInTheDocument();
    });

    it('Deberia_DesglosarLoImputado_Cuando_ElGastoIncluyeRepartos', async () => {
      renderWith({ meta: meta({ total_cost: 300, imputed_cost: 120 }) });

      // R-01 de MVP-399: lo imputado es desglose de `total_cost`, no gasto añadido.
      expect(
        await screen.findByText(/de ese gasto, 120,00 € ya están repartidos por terrenos/i)
      ).toBeInTheDocument();
    });

    it('Deberia_DecirQueMaestrosFaltan_Cuando_NoSePuedeRegistrarTodavia', async () => {
      renderWith({ data: [], meta: meta({ total: 0, activities: 0, total_cost: 0 }) }, { plots: [], workers: [] });

      expect(await screen.findByText(/antes de registrar necesitas/i)).toHaveTextContent(
        'un terreno y un responsable'
      );
    });
  });

  describe('borrado con confirmación explícita (RN-037)', () => {
    it('Deberia_NoBorrarNada_Cuando_SoloSeAbrioElDialogo', async () => {
      const user = userEvent.setup();
      renderWith();
      await screen.findByText('Poda');

      await user.click(screen.getByRole('button', { name: /eliminar/i }));

      expect(await screen.findByRole('dialog')).toHaveTextContent(/eliminar la actividad/i);
      expect(http.callsTo('/api/v1/activities/a-1')).toHaveLength(0);
    });

    it('Deberia_EnviarLaVersionVigente_Cuando_SeConfirmaElBorrado', async () => {
      const user = userEvent.setup();
      renderWith({}, {}, { '/api/v1/activities/a-1': undefined });
      await screen.findByText('Poda');

      await user.click(screen.getByRole('button', { name: /eliminar/i }));
      const dialog = await screen.findByRole('dialog');
      await user.click(within(dialog).getByRole('button', { name: /^eliminar$/i }));

      await waitFor(() => {
        const call = http.callsTo('/api/v1/activities/a-1')[0];
        expect(call?.options.method).toBe('DELETE');
        // ADR-0005: el borrado optimista viaja con la versión que se estaba viendo.
        expect(call?.options.headers?.['If-Match']).toBe('1');
      });
    });
  });
});
