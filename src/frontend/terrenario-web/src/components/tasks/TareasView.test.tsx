import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { TareasView } from './TareasView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import { HttpError } from '../../services/http-client';
import type { WorkTask } from '../../types/task.types';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({
  useApiClient: () => http,
}));

/**
 * MVP-806 (CA-1/CA-2) — La decisión que toma esta vista con el recuento de uso.
 *
 * El catálogo de tareas es el maestro más simple, así que sirve de referencia de lo que hacen los
 * cuatro: ofrecer «Eliminar» **solo** con un «sin uso» confirmado por el servidor, y mostrar tal cual
 * el 422 que trae la cifra cuando el listado iba desfasado. Lo que no se prueba aquí es el recuento
 * en sí: eso es del servidor y tiene sus propias pruebas contra PostgreSQL.
 */
describe('TareasView — depuración del maestro', () => {
  const task = (overrides: Partial<WorkTask> = {}): WorkTask => ({
    id: 't-1',
    workspace_id: 'w-1',
    name: 'Poda',
    is_active: true,
    usage_count: 0,
    ...overrides,
  });

  const renderWith = (tasks: WorkTask[], extraRoutes: Record<string, unknown> = {}) => {
    http = createFakeHttpClient({
      '/api/v1/tasks': { data: tasks, meta: { total: tasks.length } },
      ...extraRoutes,
    });
    return render(<TareasView />);
  };

  it('ofrece eliminar la tarea que nunca se usó', async () => {
    renderWith([task()]);

    expect(await screen.findByRole('button', { name: 'Eliminar Poda' })).toBeInTheDocument();
  });

  it('no ofrece eliminar una tarea con histórico', async () => {
    renderWith([task({ usage_count: 3 })]);

    await screen.findByText('Poda');
    expect(screen.queryByRole('button', { name: 'Eliminar Poda' })).not.toBeInTheDocument();
  });

  it('no ofrece eliminar cuando el recuento no se ha consultado', async () => {
    // `null` es «no lo sé», no «ninguno»: ofrecer un borrado que el servidor va a rechazar es peor
    // que no ofrecerlo.
    renderWith([task({ usage_count: null })]);

    await screen.findByText('Poda');
    expect(screen.queryByRole('button', { name: 'Eliminar Poda' })).not.toBeInTheDocument();
  });

  it('pide confirmación explícita antes de eliminar y solo entonces llama al servidor', async () => {
    renderWith([task()]);

    await userEvent.click(await screen.findByRole('button', { name: 'Eliminar Poda' }));

    expect(screen.getByText(/Se eliminará/)).toHaveTextContent('Se eliminará Poda de forma definitiva.');
    expect(http.calls.some((call) => call.options.method === 'DELETE')).toBe(false);

    await userEvent.click(screen.getByRole('button', { name: /Eliminar$/ }));

    await waitFor(() =>
      expect(http.calls.filter((call) => call.options.method === 'DELETE')).toHaveLength(1)
    );
    expect(http.calls.find((call) => call.options.method === 'DELETE')!.path).toBe('/api/v1/tasks/t-1');
  });

  it('muestra tal cual el error del servidor, que es el que trae la cifra', async () => {
    // CA-2 — el listado puede ir desfasado: la comprobación que manda es la del servidor, y su
    // mensaje dice cuántas actividades la referencian. Reescribirlo en cliente lo perdería.
    renderWith([task()], {
      '/api/v1/tasks/t-1': (options: { method?: string }) => {
        if (options.method !== 'DELETE') return { data: [], meta: { total: 0 } };
        throw new HttpError(
          422,
          'BUSINESS_RULE_MASTER_IN_USE',
          'No se puede eliminar la tarea «Poda»: 2 actividades la referencian.'
        );
      },
    });

    await userEvent.click(await screen.findByRole('button', { name: 'Eliminar Poda' }));
    await userEvent.click(screen.getByRole('button', { name: /Eliminar$/ }));

    expect(
      await screen.findByText('No se puede eliminar la tarea «Poda»: 2 actividades la referencian.')
    ).toBeInTheDocument();
  });

  it('fusiona dos tareas y avisa de cuántos registros se reapuntaron', async () => {
    renderWith([task(), task({ id: 't-2', name: 'Poda (2)', usage_count: 5 })], {
      '/api/v1/tasks/t-1/merge': {
        survivor_id: 't-1',
        survivor_name: 'Poda',
        absorbed_id: 't-2',
        absorbed_name: 'Poda (2)',
        reassigned_count: 5,
      },
    });

    await userEvent.click(await screen.findByRole('button', { name: 'Fusionar Poda con otra tarea' }));
    await userEvent.selectOptions(screen.getByLabelText('Fusionar con'), 'Poda (2)');
    await userEvent.click(screen.getByRole('button', { name: 'Fusionar' }));

    expect(await screen.findByRole('status')).toHaveTextContent(
      '«Poda (2)» se ha fusionado con «Poda»: 5 registros reapuntados.'
    );
    expect(http.callsTo('/api/v1/tasks/t-1/merge')[0].options.body).toEqual({ absorbed_id: 't-2' });
  });

  it('no ofrece fusionar cuando solo hay una tarea', async () => {
    renderWith([task()]);

    await screen.findByText('Poda');
    expect(screen.queryByRole('button', { name: /Fusionar/ })).not.toBeInTheDocument();
  });
});
