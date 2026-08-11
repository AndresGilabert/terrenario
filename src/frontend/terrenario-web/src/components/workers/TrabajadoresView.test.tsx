import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { TrabajadoresView } from './TrabajadoresView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import type { Worker } from '../../types/worker.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({
  useApiClient: () => http,
}));

/**
 * MVP-806 (CA-4) — El caso que motiva la historia, visto desde la pantalla: la cuadrilla que MVP-207
 * renombró « (2)» al materializarse el miembro homónimo de MVP-208.
 *
 * La regla no se aprende del error del servidor: la pantalla ya fija el sentido, de modo que quien
 * abre la fusión desde la fila de cuadrilla no puede pedir que desaparezca la del miembro.
 */
describe('TrabajadoresView — depuración del maestro', () => {
  const worker = (overrides: Partial<Worker> = {}): Worker => ({
    id: 'w-crew',
    workspace_id: 'ws-1',
    name: 'Juan Pérez (2)',
    hourly_rate: null,
    is_active: true,
    kind: 'crew',
    user_account_id: null,
    usage_count: 0,
    ...overrides,
  });

  const member = worker({
    id: 'w-member',
    name: 'Juan Pérez',
    kind: 'member',
    user_account_id: 'u-1',
    usage_count: 0,
  });

  const renderWith = (workers: Worker[], extraRoutes: Record<string, unknown> = {}) => {
    http = createFakeHttpClient({
      '/api/v1/workers': {
        data: workers,
        meta: {
          total: workers.length,
          members: workers.filter((w) => w.kind === 'member').length,
          crew: workers.filter((w) => w.kind === 'crew').length,
        },
      },
      ...extraRoutes,
    });
    return render(
      <MemoryRouter>
        <TrabajadoresView />
      </MemoryRouter>
    );
  };

  it('nunca ofrece eliminar la ficha de un miembro, aunque no tenga histórico', async () => {
    // MVP-208 (CA-4) — depende de su acceso, no del maestro. Con `usage_count: 0` la regla general
    // diría que sí: es justo la excepción que hay que respetar.
    renderWith([member, worker()]);

    await screen.findByText('Juan Pérez');
    expect(screen.queryByRole('button', { name: 'Eliminar Juan Pérez' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Eliminar Juan Pérez (2)' })).toBeInTheDocument();
  });

  it('no ofrece eliminar la cuadrilla con histórico', async () => {
    renderWith([worker({ usage_count: 7 })]);

    await screen.findByText('Juan Pérez (2)');
    expect(screen.queryByRole('button', { name: /Eliminar/ })).not.toBeInTheDocument();
  });

  it('al fusionar desde la cuadrilla conserva la ficha del miembro', async () => {
    renderWith([member, worker({ usage_count: 3 })], {
      '/api/v1/workers/w-member/merge': {
        survivor_id: 'w-member',
        survivor_name: 'Juan Pérez',
        absorbed_id: 'w-crew',
        absorbed_name: 'Juan Pérez (2)',
        reassigned_count: 3,
      },
    });

    await userEvent.click(
      await screen.findByRole('button', { name: 'Fusionar Juan Pérez (2) con otra ficha' })
    );
    await userEvent.selectOptions(screen.getByLabelText('Fusionar con'), 'Juan Pérez');

    expect(screen.getByText(/Se conserva/)).toHaveTextContent(
      'Se conserva Juan Pérez y desaparece Juan Pérez (2).'
    );

    await userEvent.click(screen.getByRole('button', { name: 'Fusionar' }));

    // La petición va contra la ficha del miembro, que es la que sobrevive.
    const merge = http.callsTo('/api/v1/workers/w-member/merge');
    expect(merge).toHaveLength(1);
    expect(merge[0].options.body).toEqual({ absorbed_id: 'w-crew' });
    expect(await screen.findByRole('status')).toHaveTextContent('3 registros reapuntados');
  });
});
