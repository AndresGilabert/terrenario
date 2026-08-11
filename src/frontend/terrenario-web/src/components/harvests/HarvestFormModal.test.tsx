import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { HarvestFormModal } from './HarvestFormModal';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import type { RequestOptions } from '../../services/http-client';
import type { Harvest } from '../../types/harvest.types';
import type { Plot } from '../../types/plot.types';
import type { Season } from '../../types/season.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

const plots: Plot[] = [
  { id: 'p-1', workspace_id: 'w-1', name: 'Matorral', ownership_type: 'propia', is_active: true } as Plot,
  { id: 'p-2', workspace_id: 'w-1', name: 'La Via', ownership_type: 'propia', is_active: true } as Plot,
];

const seasons: Season[] = [
  {
    id: 's-1',
    name: 'Campaña 2025/26',
    start_date: '2025-10-01',
    end_date: '2026-03-31',
    is_closed: false,
    is_working: true,
    status: 'abierta',
  },
];

const existente = { id: 'h-1', kgs: 1000, destination: 'aceite_para_venta' };

/**
 * MVP-805 (`P-110`, RN-044) — El aviso de cosecha repetida en el formulario.
 *
 * `RU-24` estaba marcado «Estado: MVP» y nunca se construyó: es una de las tres consecuencias de
 * `P-114`. Lo que se cubre aquí es lo que no se ve en el servidor —**cuándo** se pregunta y **qué**
 * dice el aviso—; que la comparación sea la correcta lo fijan las pruebas de integración.
 */
describe('HarvestFormModal — aviso de cosecha repetida', () => {
  const renderModal = (
    { harvest = null, duplicates = [existente] }: { harvest?: Harvest | null; duplicates?: unknown[] } = {}
  ) => {
    http = createFakeHttpClient({
      '/api/v1/harvests/duplicates': (options: RequestOptions) => {
        // El doble se comporta como el servidor: solo hay duplicado si coinciden los tres campos.
        const coincide =
          options.query?.plot_id === 'p-1' && options.query?.date === '2025-10-20';
        return { data: coincide ? duplicates : [], meta: { total: coincide ? duplicates.length : 0 } };
      },
    });

    return render(
      <HarvestFormModal
        isOpen
        harvest={harvest}
        plots={plots}
        seasons={seasons}
        activeSeason={seasons[0]}
        isSubmitting={false}
        errorMessage={null}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );
  };

  const escribirFecha = async (fecha: string) => {
    const user = userEvent.setup();
    const campo = screen.getByLabelText(/Fecha/);
    await user.clear(campo);
    await user.type(campo, fecha);
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('avisa nombrando los kilos y el destino de la partida que ya existe', async () => {
    // CA-1 — sin nombrarla, quien lee el aviso no puede distinguir si es la misma que acaba de apuntar
    // o una segunda de verdad.
    renderModal();
    await escribirFecha('2025-10-20');

    const aviso = await screen.findByText(/Ya hay una partida de este terreno/);
    expect(aviso).toHaveTextContent('1000 kg, Aceite para venta');
  });

  it('no impide guardar', async () => {
    // CA-2 — `RU-24` dice expresamente «se permite guardar igual (sin bloqueo)», y dos partidas del
    // mismo terreno y día son un caso real.
    renderModal();
    await escribirFecha('2025-10-20');

    await screen.findByText(/Ya hay una partida de este terreno/);
    expect(screen.getByRole('button', { name: /Registrar cosecha|Guardar/ })).toBeEnabled();
  });

  it('no avisa mientras no coincidan los tres campos', async () => {
    const user = userEvent.setup();
    renderModal();
    await escribirFecha('2025-10-20');
    await screen.findByText(/Ya hay una partida de este terreno/);

    await user.selectOptions(screen.getByLabelText(/Terreno/), 'p-2');

    await waitFor(() =>
      expect(screen.queryByText(/Ya hay una partida de este terreno/)).not.toBeInTheDocument()
    );
  });

  it('excluye la propia partida al corregirla', async () => {
    // CA-3 — cambiarle el destino a una cosecha no puede avisar de que esa cosecha ya existe. Lo que
    // se comprueba aquí es que el formulario **manda** su identificador; que el servidor lo excluya lo
    // fija la prueba de integración.
    renderModal({
      harvest: {
        id: 'h-1',
        plot_id: 'p-1',
        season_id: 's-1',
        date: '2025-10-20',
        product: 'aceituna_olivar',
        kgs: 1000,
        destination: 'aceite_para_venta',
        yield: null,
        liters: null,
        effective_yield: null,
        yield_source: null,
        unit_price: null,
        amount: null,
        is_out_of_season_range: false,
        version: 1,
      } as Harvest,
    });

    await waitFor(() => expect(http.callsTo('/api/v1/harvests/duplicates')).not.toHaveLength(0));
    expect(http.callsTo('/api/v1/harvests/duplicates')[0].options.query).toMatchObject({
      exclude_id: 'h-1',
    });
  });

  it('convive con el aviso de fecha fuera de rango sin ocultarlo', async () => {
    // CA-5 — son cosas distintas: una fecha rara y una partida repetida. Esconder una porque salga la
    // otra dejaría al usuario decidiendo con la mitad de la información.
    http = createFakeHttpClient({
      '/api/v1/harvests/duplicates': { data: [existente], meta: { total: 1 } },
    });
    render(
      <HarvestFormModal
        isOpen
        harvest={null}
        plots={plots}
        seasons={seasons}
        activeSeason={seasons[0]}
        isSubmitting={false}
        errorMessage={null}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    // Fuera del rango de la campaña (2025-10-01 → 2026-03-31).
    await escribirFecha('2024-01-15');

    expect(await screen.findByText(/queda fuera del rango/)).toBeInTheDocument();
    expect(await screen.findByText(/Ya hay una partida de este terreno/)).toBeInTheDocument();
  });

  it('no rompe el formulario si la comprobación falla', async () => {
    // Un fallo se trata como «no se sabe»: no poder comprobarlo no puede impedir registrar una cosecha.
    http = createFakeHttpClient({
      '/api/v1/harvests/duplicates': () => {
        throw new Error('la API no responde');
      },
    });
    render(
      <HarvestFormModal
        isOpen
        harvest={null}
        plots={plots}
        seasons={seasons}
        activeSeason={seasons[0]}
        isSubmitting={false}
        errorMessage={null}
        onClose={() => {}}
        onSubmit={() => {}}
      />
    );

    await escribirFecha('2025-10-20');

    await waitFor(() => expect(http.callsTo('/api/v1/harvests/duplicates')).not.toHaveLength(0));
    expect(screen.queryByText(/Ya hay una partida de este terreno/)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Registrar cosecha|Guardar/ })).toBeEnabled();
  });
});
