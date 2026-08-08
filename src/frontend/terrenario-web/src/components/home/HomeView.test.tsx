import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { HomeView } from './HomeView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { displayName: 'Andrés' } }),
}));
vi.mock('../../contexts/WorkspaceContext', () => ({
  useWorkspace: () => ({ activeWorkspace: { id: 'w-1', name: 'Finca El Olivar' } }),
}));

const seasonState = {
  activeSeason: { id: 's-1', name: 'Campaña 2025/26' } as { id: string; name: string } | null,
  seasons: [{ id: 's-1', name: 'Campaña 2025/26' }],
};
vi.mock('../../contexts/SeasonContext', () => ({ useSeason: () => seasonState }));

/**
 * MVP-703 (`P-087`) — El arranque del área operativa.
 *
 * `MVP-499` había decidido que con la explotación preparada el Home **fuese** la Visión General
 * (`P-040`). Lo que faltaba era el dato de que la cosecha se concentra al final de campaña: durante la
 * mayor parte del año lo primero que se veía al entrar era «Sin cosechas en {temporada}», y además
 * contradecía a `RN-033`, que declara el diario vista principal del MVP.
 *
 * Lo que **no** cambia es la primera cara: mientras falten maestros, el checklist sigue siendo lo
 * primero. Esa parte está aquí para que no se pierda al mover la segunda.
 */
describe('HomeView — arranque del área operativa', () => {
  const master = (n: number) => ({ data: Array.from({ length: n }, (_, i) => ({ id: `x-${i}` })) });

  const renderHome = (counts: { plots: number; workers: number; tasks: number }) => {
    http = createFakeHttpClient({
      '/api/v1/plots': master(counts.plots),
      '/api/v1/workers': master(counts.workers),
      '/api/v1/tasks': master(counts.tasks),
    });

    return render(
      <MemoryRouter initialEntries={['/app']}>
        <Routes>
          <Route path="/app" element={<HomeView />} />
          <Route path="/app/diario" element={<h1>Diario de campo</h1>} />
        </Routes>
      </MemoryRouter>
    );
  };

  beforeEach(() => {
    vi.clearAllMocks();
    seasonState.activeSeason = { id: 's-1', name: 'Campaña 2025/26' };
  });

  it('lleva al diario cuando la explotación está preparada', async () => {
    // CA-1 — se entra a trabajar, que es el diario (RN-033), no a un panel que la mayor parte del año
    // está vacío porque la cosecha se concentra al final de campaña.
    renderHome({ plots: 2, workers: 1, tasks: 3 });

    expect(await screen.findByRole('heading', { name: 'Diario de campo' })).toBeInTheDocument();
  });

  it('sigue mostrando el checklist cuando faltan maestros', async () => {
    // CA-2 — la primera cara del Home no cambia: preparar la explotación sigue siendo lo primero.
    renderHome({ plots: 0, workers: 1, tasks: 3 });

    expect(await screen.findByText('Prepara tu explotación')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Diario de campo' })).not.toBeInTheDocument();
  });

  it('sigue mostrando el checklist cuando falta la temporada, aunque los demás maestros estén', async () => {
    // La temporada es un paso más del checklist: sin ella tampoco se arranca en el diario.
    seasonState.activeSeason = null;
    renderHome({ plots: 2, workers: 1, tasks: 3 });

    expect(await screen.findByText('Prepara tu explotación')).toBeInTheDocument();
  });
});
