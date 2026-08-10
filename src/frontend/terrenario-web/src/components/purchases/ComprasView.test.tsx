import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComprasView } from './ComprasView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import type { RequestOptions } from '../../services/http-client';
import type { Consumption } from '../../types/consumption.types';
import type { Purchase } from '../../types/purchase.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

const seasonState = {
  seasons: [{ id: 's-1', name: 'Campaña 2026/27' }],
  activeSeason: { id: 's-1', name: 'Campaña 2026/27' },
};
vi.mock('../../contexts/SeasonContext', () => ({ useSeason: () => seasonState }));

/**
 * MVP-708 — Los dos roces de captura del libro de compras, en el cliente.
 *
 * La vista no tenía cobertura propia: se estrena con lo que esta historia introduce, que es
 * exactamente lo que no se ve leyendo el backend —qué sugiere el campo de material del consumo
 * (`P-057`) y dónde aparece el aviso de fecha anterior a la compra (`P-058`)—.
 */
describe('ComprasView — roces de captura (MVP-708)', () => {
  const purchase = (overrides: Partial<Purchase> = {}): Purchase => ({
    id: 'pu-1',
    workspace_id: 'w-1',
    purchase_date: '2026-07-31',
    season_id: 's-1',
    season_name: 'Campaña 2026/27',
    product: 'Abono NPK',
    total_quantity: 200,
    total_cost: 400,
    unit_price: 2,
    is_out_of_season_range: false,
    imputed_quantity: 0,
    pending_quantity: 200,
    version: 1,
    created_at: '2026-07-31T09:00:00Z',
    updated_at: '2026-07-31T09:00:00Z',
    ...overrides,
  });

  const consumption = (overrides: Partial<Consumption> = {}): Consumption => ({
    id: 'co-1',
    workspace_id: 'w-1',
    purchase_id: 'pu-1',
    has_purchase: true,
    purchase_date: '2026-07-31',
    plot_id: 'p-1',
    plot_name: 'La Hoya',
    season_id: 's-1',
    season_name: 'Campaña 2026/27',
    date: '2026-08-05',
    product: 'Abono NPK',
    quantity: 50,
    unit_price: 2,
    proportional_cost: 100,
    is_out_of_season_range: false,
    is_before_purchase_date: false,
    version: 1,
    created_at: '2026-08-05T09:00:00Z',
    updated_at: '2026-08-05T09:00:00Z',
    ...overrides,
  });

  const scope = {
    season: {
      id: 's-1',
      name: 'Campaña 2026/27',
      status: 'abierta',
      start_date: '2026-07-01',
      end_date: '2027-03-31',
    },
    all_seasons: false,
  };

  const renderWith = (
    consumptions: Consumption[],
    suggestions: { product: string; times_used: number }[] = [
      { product: 'Abono NPK', times_used: 2 },
      { product: 'Cobre de la nave', times_used: 1 },
    ]
  ) => {
    http = createFakeHttpClient({
      // La más específica gana, así que el vocabulario se declara aparte del libro.
      '/api/v1/purchases/products': { data: suggestions, meta: { total: suggestions.length } },
      '/api/v1/purchases': { data: [purchase()], meta: { scope, total: 1, total_cost: 400 } },
      '/api/v1/consumptions': {
        data: consumptions,
        meta: {
          scope,
          total: consumptions.length,
          total_cost: 100,
          without_purchase: consumptions.filter((c) => !c.has_purchase).length,
        },
      },
      '/api/v1/plots': { data: [{ id: 'p-1', name: 'La Hoya', is_active: true }] },
    });

    return render(
      <MemoryRouter>
        <ComprasView />
      </MemoryRouter>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('vocabulario de material compartido (`P-057`, CA-1)', () => {
    it('Deberia_SugerirElVocabularioDelHistorico_Cuando_SeRegistraUnConsumoSinCompra', async () => {
      const user = userEvent.setup();
      renderWith([consumption()]);

      await user.click(await screen.findByRole('button', { name: 'Consumo sin compra' }));

      // Con el asterisco: el alta en línea de arriba tiene su propio campo, con etiqueta `sr-only`
      // sin marca de obligatorio, y sin ese matiz las dos coincidirían.
      const field = await screen.findByLabelText('Producto o material *');
      const options = document.getElementById(field.getAttribute('list')!)!;

      // El material que solo existe en los consumos también se ofrece: es el punto entero de `P-057`.
      expect(Array.from(options.querySelectorAll('option')).map((o) => o.getAttribute('value')))
        .toEqual(['Abono NPK', 'Cobre de la nave']);
    });
  });

  describe('aviso de consumo anterior a su compra (`P-058`, RN-043)', () => {
    it('Deberia_EtiquetarLaFila_Cuando_ElConsumoEsAnteriorASuCompra', async () => {
      // CA-3 — la etiqueta cuelga del material, igual que «sin compra» y que «fuera de rango»
      renderWith([consumption({ date: '2020-01-01', is_before_purchase_date: true })]);

      const row = (await screen.findByText('antes de la compra')).closest('tr')!;
      expect(within(row).getByText('La Hoya')).toBeInTheDocument();
    });

    it('NoDeberia_EtiquetarLaFila_Cuando_LaFechaEsPosteriorALaCompra', async () => {
      renderWith([consumption()]);

      await screen.findByText('La Hoya');
      expect(screen.queryByText('antes de la compra')).not.toBeInTheDocument();
    });

    it('Deberia_AvisarEnElFormulario_SinBloquear_Cuando_SeTecleaUnaFechaAnterior', async () => {
      // CA-2 — el aviso aparece al teclear y el botón de guardar sigue disponible
      const user = userEvent.setup();
      renderWith([consumption()]);

      await user.click(
        await screen.findByRole('button', { name: /Imputar la compra de Abono NPK/ })
      );

      const submit = screen.getByRole('button', { name: /Registrar consumo/ });
      expect(screen.queryByText(/anterior a su compra/)).not.toBeInTheDocument();

      await user.clear(screen.getByLabelText('Fecha *'));
      await user.type(screen.getByLabelText('Fecha *'), '2020-01-01');

      expect(await screen.findByText(/anterior a su compra/)).toBeInTheDocument();
      expect(submit).toBeEnabled();
    });
  });
});

/**
 * MVP-802 (`P-109`) — Los filtros del libro de compras en la URL (`RN-007`).
 *
 * La pantalla tiene **dos** listas —el libro y sus consumos— y las dos comparten filtros: hablar de
 * campañas distintas dentro de una sola vista sería el propio `P-082` en pequeño.
 */
describe('ComprasView — filtros en la URL (MVP-802)', () => {
  const purchase = (): Purchase => ({
    id: 'pu-1',
    workspace_id: 'w-1',
    purchase_date: '2026-07-31',
    season_id: 's-1',
    season_name: 'Campaña 2026/27',
    product: 'Abono NPK',
    total_quantity: 200,
    total_cost: 400,
    unit_price: 2,
    is_out_of_season_range: false,
    imputed_quantity: 0,
    pending_quantity: 200,
    version: 1,
    created_at: '2026-07-31T09:00:00Z',
    updated_at: '2026-07-31T09:00:00Z',
  });

  const scope = {
    season: {
      id: 's-1',
      name: 'Campaña 2026/27',
      status: 'abierta',
      start_date: '2026-07-01',
      end_date: '2027-03-31',
    },
    all_seasons: false,
  };

  /**
   * El ámbito que devolvería el servidor: honra `all` y una campaña conocida, y **cae al defecto** con
   * cualquier otra (RN-008). Sin esto el doble mentiría justo en el caso que `CA-5` comprueba.
   */
  const appliedScope = (options: RequestOptions) => {
    const requested = options.query?.season_id;
    if (requested === 'all') return { season: null, all_seasons: true };
    return scope;
  };

  const renderAt = (search = '') => {
    http = createFakeHttpClient({
      '/api/v1/purchases/products': { data: [], meta: { total: 0 } },
      '/api/v1/purchases': (options: RequestOptions) => ({
        data: [purchase()],
        meta: { scope: appliedScope(options), total: 1, total_cost: 400 },
      }),
      '/api/v1/consumptions': (options: RequestOptions) => ({
        data: [],
        meta: { scope: appliedScope(options), total: 0, total_cost: 0, without_purchase: 0 },
      }),
      '/api/v1/plots': { data: [{ id: 'p-1', name: 'La Hoya', is_active: true }] },
    });

    const location = { search: '' };
    const Probe = () => {
      location.search = useLocation().search;
      return null;
    };

    render(
      <MemoryRouter initialEntries={[`/app/compras${search}`]}>
        <ComprasView />
        <Probe />
      </MemoryRouter>
    );

    return location;
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('escribe la campaña elegida en la dirección', async () => {
    const user = userEvent.setup();
    const location = renderAt();

    await user.selectOptions(await screen.findByLabelText('Filtrar por temporada'), 'all');

    expect(location.search).toBe('?season_id=all');
  });

  it('aplica a las dos listas lo que trae la dirección', async () => {
    // CA-2 — el bloque de consumos comparte los filtros del libro, no tiene los suyos.
    renderAt('?season_id=s-9&product=abono');

    await waitFor(() => expect(http.callsTo('/api/v1/consumptions')).not.toHaveLength(0));

    const libro = http.callsTo('/api/v1/purchases').find((c) => !c.path.includes('/products'));
    const consumos = http.callsTo('/api/v1/consumptions')[0];

    expect(libro?.options.query).toMatchObject({ season_id: 's-9', product: 'abono' });
    expect(consumos.options.query).toMatchObject({ season_id: 's-9', product: 'abono' });
  });

  it('rellena el buscador con lo que trae la dirección', async () => {
    renderAt('?product=abono');

    expect(await screen.findByLabelText('Buscar material')).toHaveValue('abono');
  });

  it('escribe la búsqueda una sola vez, cuando se deja de teclear', async () => {
    // CA-4 y la higiene de `RN-007`: sin el rebote habría una entrada de historial por carácter, y
    // hasta esta historia había además una petición al servidor por pulsación.
    const user = userEvent.setup();
    const location = renderAt();

    await user.type(await screen.findByLabelText('Buscar material'), 'abono');
    expect(location.search).toBe('');

    await waitFor(() => expect(location.search).toBe('?product=abono'));
  });

  it('no escribe la campaña por defecto: la resuelve el servidor', async () => {
    const location = renderAt();

    await screen.findByLabelText('Filtrar por temporada');
    expect(location.search).toBe('');
  });

  it('cae al defecto y corrige la dirección con una campaña de otro Workspace', async () => {
    // CA-5 — la misma comprobación que en Cosechas: llevar el filtro a la URL es el mecanismo que
    // expone `P-108`, y sin la reconciliación de `MVP-801` esta vista lo estrenaría.
    const location = renderAt('?season_id=de-otro-workspace');

    const filtro = await screen.findByLabelText('Filtrar por temporada');
    await waitFor(() => expect(filtro).toHaveValue('s-1'));
    await waitFor(() => expect(location.search).toBe(''));
  });
});
