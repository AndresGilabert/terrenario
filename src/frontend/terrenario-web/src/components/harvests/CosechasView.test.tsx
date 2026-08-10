import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CosechasView } from './CosechasView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import type { RequestOptions } from '../../services/http-client';
import type { Harvest } from '../../types/harvest.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

const seasonState = {
  seasons: [
    { id: 's-1', name: 'Campana 2025' },
    { id: 's-2', name: 'Campaña 2026' },
  ],
  activeSeason: { id: 's-1', name: 'Campana 2025' },
};
vi.mock('../../contexts/SeasonContext', () => ({ useSeason: () => seasonState }));

const harvest = (overrides: Partial<Harvest> = {}): Harvest => ({
  id: 'h-1',
  workspace_id: 'w-1',
  date: '2025-10-20',
  plot_id: 'p-1',
  plot_name: 'Matorral',
  season_id: 's-1',
  season_name: 'Campana 2025',
  product: 'aceituna_olivar',
  kgs: 1000,
  yield: null,
  liters: 160,
  effective_yield: 16,
  yield_source: 'calculado',
  destination: 'aceite_para_venta',
  unit_price: null,
  amount: null,
  is_out_of_season_range: false,
  version: 1,
  created_at: '2025-10-20T09:00:00Z',
  updated_at: '2025-10-20T09:00:00Z',
  ...overrides,
});

/** El ámbito que el servidor dice haber aplicado: siempre la campaña de trabajo salvo que se diga. */
const scope = (seasonId = 's-1', name = 'Campana 2025') => ({
  season: { id: seasonId, name, status: 'abierta', start_date: '2025-09-01', end_date: '2026-02-28' },
  all_seasons: false,
});

/**
 * MVP-802 (`P-109`) — Los filtros de Cosechas en la URL (`RN-007`).
 *
 * La vista no tenía cobertura propia. Se estrena con lo que esta historia introduce, que es justo lo
 * que no se ve leyendo el servicio: **dónde vive el filtro**. Hasta aquí filtrar por «Campaña 2026»
 * pasaba la tabla de 1 a 4 filas y la dirección seguía siendo `/app/cosechas`.
 */
describe('CosechasView — filtros en la URL', () => {
  const renderAt = (search = '', applied = scope()) => {
    http = createFakeHttpClient({
      // El doble aplica RN-008 como el servidor: honra `all` y una campaña conocida, y cae al defecto
      // con cualquier otra. Si devolviera siempre lo mismo, mentiría justo en el caso de `CA-5`.
      '/api/v1/harvests': (options: RequestOptions) => ({
        data: [harvest()],
        meta: {
          scope:
            options.query?.season_id === 'all' ? { season: null, all_seasons: true } : applied,
          total: 1,
          total_kg: 1000,
        },
      }),
      '/api/v1/plots': {
        data: [
          { id: 'p-1', name: 'Matorral', is_active: true },
          { id: 'p-2', name: 'La Via', is_active: true },
        ],
        meta: { total: 2 },
      },
    });

    const location = { search: '' };
    const Probe = () => {
      location.search = useLocation().search;
      return null;
    };

    render(
      <MemoryRouter initialEntries={[`/app/cosechas${search}`]}>
        <CosechasView />
        <Probe />
      </MemoryRouter>
    );

    return location;
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('escribe el terreno elegido en la dirección', async () => {
    const user = userEvent.setup();
    const location = renderAt();

    await user.selectOptions(await screen.findByLabelText('Filtrar por terreno'), 'p-2');

    expect(location.search).toBe('?plot_id=p-2');
  });

  it('escribe el destino elegido en la dirección', async () => {
    const user = userEvent.setup();
    const location = renderAt();

    await user.selectOptions(
      await screen.findByLabelText('Filtrar por destino'),
      'venta_aceituna'
    );

    expect(location.search).toBe('?destination=venta_aceituna');
  });

  it('pide al servidor lo que trae la dirección', async () => {
    // CA-1/CA-3 — es lo que hace que recargar mantenga la lista y que un enlace la reproduzca: el
    // filtro no se «restaura», es que nunca vivió en otro sitio.
    renderAt('?plot_id=p-2&destination=venta_aceituna&season_id=s-2');

    await waitFor(() => expect(http.callsTo('/api/v1/harvests')).not.toHaveLength(0));

    expect(http.callsTo('/api/v1/harvests')[0].options.query).toMatchObject({
      plot_id: 'p-2',
      destination: 'venta_aceituna',
      season_id: 's-2',
    });
  });

  it('deja la dirección limpia al volver a «todos»', async () => {
    // CA-4 — los valores por defecto no se escriben.
    const user = userEvent.setup();
    const location = renderAt('?destination=venta_aceituna');

    await user.selectOptions(await screen.findByLabelText('Filtrar por destino'), 'todos');

    expect(location.search).toBe('');
  });

  it('no escribe la campaña por defecto: la resuelve el servidor', async () => {
    // Fijarla congelaría en el enlace la campaña de trabajo del día en que se compartió (RN-008).
    const location = renderAt();

    await screen.findByLabelText('Filtrar por temporada');
    expect(location.search).toBe('');
  });

  it('cae al defecto y corrige la dirección con una campaña de otro Workspace', async () => {
    // CA-5 — llevar el filtro a la URL es justo el mecanismo que expone `P-108`. Sin la reconciliación
    // de `MVP-801`, esta vista lo estrenaría: el `<select>` caería en su primera opción.
    const location = renderAt('?season_id=de-otro-workspace');

    const filtro = await screen.findByLabelText('Filtrar por temporada');
    await waitFor(() => expect(filtro).toHaveValue('s-1'));
    await waitFor(() => expect(location.search).toBe(''));
  });
});

/**
 * MVP-803 (`P-095`) — Cosechas por debajo del punto de corte.
 *
 * La tabla mide ~897 px y el contenido tiene 341 px a 375 y 704 a 768: en los dos anchos se leía
 * arrastrando de lado a lado. Lo que se comprueba aquí es que la lista **cambia de maqueta**, no que
 * quepa: el ancho real se mide en el navegador y va en la evidencia del `spec`.
 */
describe('CosechasView — maqueta adaptada', () => {
  const renderStrecho = () => {
    // El doble común declara escritorio (1280 px); esta pantalla es la de un móvil.
    vi.stubGlobal('matchMedia', (query: string) => {
      const min = /\(min-width:\s*(\d+)px\)/.exec(query);
      return {
        matches: min ? 375 >= Number(min[1]) : false,
        media: query,
        onchange: null,
        addEventListener: () => {},
        removeEventListener: () => {},
        addListener: () => {},
        removeListener: () => {},
        dispatchEvent: () => false,
      };
    });

    http = createFakeHttpClient({
      '/api/v1/harvests': { data: [harvest()], meta: { scope: scope(), total: 1, total_kg: 1000 } },
      '/api/v1/plots': { data: [{ id: 'p-1', name: 'Matorral', is_active: true }], meta: { total: 1 } },
    });

    render(
      <MemoryRouter initialEntries={['/app/cosechas']}>
        <CosechasView />
      </MemoryRouter>
    );
  };

  it('no pinta la tabla de ocho columnas', async () => {
    renderStrecho();

    await screen.findByRole('list', { name: 'Partidas recolectadas' });
    expect(document.querySelector('table')).toBeNull();
  });

  it('conserva toda la información de la partida', async () => {
    // CA-1 — «toda la información de cada partida es legible sin desplazar»: la tarjeta no puede ser
    // la tabla con columnas quitadas.
    renderStrecho();

    const tarjeta = (await screen.findByRole('list', { name: 'Partidas recolectadas' }))
      .firstElementChild as HTMLElement;

    for (const dato of ['Matorral', '20 oct 2025', 'Campana 2025', '1000 kg', 'Aceituna de olivar']) {
      expect(tarjeta.textContent).toContain(dato);
    }
    // El rendimiento derivado sigue marcándose como tal (RN-013/RN-014).
    expect(tarjeta.textContent).toContain('16,0 L/100kg');
    expect(tarjeta.textContent).toContain('de 160,0 L');
  });

  it('mantiene las acciones y su etiqueta accesible', async () => {
    // CA-4 — la etiqueta tiene que seguir nombrando **a qué partida** apunta.
    renderStrecho();

    expect(
      await screen.findByRole('button', { name: 'Corregir la cosecha de Matorral del 20 oct 2025' })
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Eliminar la cosecha de Matorral del 20 oct 2025' })
    ).toBeInTheDocument();
  });
});
