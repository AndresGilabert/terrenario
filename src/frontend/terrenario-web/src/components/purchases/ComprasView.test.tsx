import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComprasView } from './ComprasView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
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
