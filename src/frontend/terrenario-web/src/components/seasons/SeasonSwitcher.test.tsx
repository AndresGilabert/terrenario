import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { SeasonSwitcher } from './SeasonSwitcher';
import type { Season } from '../../types/season.types';

/**
 * El conmutador se apoya en `SeasonContext`, cuyo provider arrastra toda la pila de sesión. Aquí
 * interesa la decisión del componente, no el cableado, así que se sustituye el hook.
 */
const seasonContext = {
  activeSeason: null as Season | null,
  seasons: [] as Season[],
  isLoading: false,
  offerDismissed: false,
  createSeason: vi.fn(),
  activateSeason: vi.fn(),
  dismissOffer: vi.fn(),
  refresh: vi.fn(),
};

vi.mock('../../contexts/SeasonContext', () => ({
  useSeason: () => seasonContext,
}));

/**
 * MVP-701 (`P-083`, CA-5) — La píldora de campaña de la cabecera era un rótulo mientras **sí** había
 * campaña de trabajo, que es justo cuando hace falta poder cambiarla: desde esta historia la campaña
 * de trabajo es el defecto de todas las vistas operativas (RN-008).
 */
describe('SeasonSwitcher', () => {
  const season = (overrides: Partial<Season> = {}): Season => ({
    id: 's-1',
    name: 'Campaña 2025/26',
    start_date: '2025-10-01',
    end_date: '2026-03-31',
    is_closed: false,
    is_working: true,
    status: 'abierta',
    ...overrides,
  });

  const renderSwitcher = () =>
    render(
      <MemoryRouter>
        <SeasonSwitcher />
      </MemoryRouter>
    );

  beforeEach(() => {
    vi.clearAllMocks();
    seasonContext.activeSeason = season();
    seasonContext.seasons = [
      season(),
      season({ id: 's-0', name: 'Campaña 2024/25', is_working: false, status: 'cerrada', is_closed: true }),
    ];
    seasonContext.isLoading = false;
    seasonContext.activateSeason = vi.fn().mockResolvedValue(season({ id: 's-0' }));
  });

  it('ofrece cambiar de campaña cuando hay una de trabajo', async () => {
    renderSwitcher();

    // El defecto anterior: con campaña de trabajo la píldora no era pulsable.
    const pill = screen.getByRole('button', { name: /Campaña 2025\/26/ });
    await userEvent.click(pill);

    expect(screen.getByRole('option', { name: /Campaña 2024\/25/ })).toBeInTheDocument();
  });

  it('fija la campaña elegida como la de trabajo', async () => {
    renderSwitcher();

    await userEvent.click(screen.getByRole('button', { name: /Campaña 2025\/26/ }));
    await userEvent.click(screen.getByRole('button', { name: /Campaña 2024\/25/ }));

    await waitFor(() => expect(seasonContext.activateSeason).toHaveBeenCalledWith('s-0'));
  });

  it('lleva a crear cuando el Workspace no tiene ninguna campaña', () => {
    seasonContext.activeSeason = null;
    seasonContext.seasons = [];

    renderSwitcher();

    // Sin ninguna no hay entre qué elegir: lo que toca es crear (MVP-208, CA-8).
    expect(screen.getByRole('button', { name: /Sin temporada · Crear/ })).toBeInTheDocument();
  });

  it('ofrece elegir cuando hay campañas pero ninguna es la de trabajo', async () => {
    seasonContext.activeSeason = null;
    seasonContext.seasons = [season({ is_working: false })];

    renderSwitcher();

    await userEvent.click(screen.getByRole('button', { name: /elegir una/i }));

    expect(screen.getByRole('option', { name: /Campaña 2025\/26/ })).toBeInTheDocument();
  });

  it('avisa sin cerrar el desplegable cuando el cambio falla', async () => {
    seasonContext.activateSeason = vi.fn().mockRejectedValue(new Error('boom'));

    renderSwitcher();
    await userEvent.click(screen.getByRole('button', { name: /Campaña 2025\/26/ }));
    await userEvent.click(screen.getByRole('button', { name: /Campaña 2024\/25/ }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/No se pudo cambiar de temporada/);
  });
});
