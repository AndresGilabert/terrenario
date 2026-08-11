import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { DataScopeProvider, useDataScope } from './DataScopeContext';
import { WorkspaceProvider, useWorkspace } from './WorkspaceContext';
import { workspaceService } from '../services/workspace.service';
import type { Workspace } from '../types/workspace.types';

const auth = {
  isAuthenticated: true,
  isLoading: false,
  getAccessToken: async () => 'token',
  setAccessToken: () => {},
};
vi.mock('./AuthContext', () => ({ useAuth: () => auth }));

const workspace = (id: string, name: string): Workspace => ({ id, name }) as Workspace;

/** Deja a la vista el Workspace activo y la clave de remontaje, que es lo que se afirma. */
function Sonda() {
  const { activeWorkspace, switchWorkspace } = useWorkspace();
  const { scopeVersion } = useDataScope();
  return (
    <div>
      <span data-testid="activo">{activeWorkspace?.name ?? '—'}</span>
      <span data-testid="scope">{scopeVersion}</span>
      <button type="button" onClick={() => void switchWorkspace('w-2')}>
        cambiar
      </button>
    </div>
  );
}

/**
 * MVP-811 (`P-116`) — **La consola también es cobertura.**
 *
 * El hallazgo real no fue el aviso, fue que **256 tests pasaban mientras la consola avisaba en cada
 * carga**: ninguno miraba ahí. `invalidateScope()` se llamaba dentro del updater de
 * `setActiveWorkspace`, y ese updater lo ejecuta React en fase de render, así que resultaba un
 * `setState` sobre otro componente durante el render.
 *
 * Hoy no rompía nada visible —`scopeVersion` es solo una clave de remontaje—, pero en `StrictMode` el
 * updater corre dos veces y en versiones posteriores de React esto escala de aviso a fallo. Y lo que
 * sostiene es el mecanismo que corrigió `P-081`: los datos cruzados entre Workspaces.
 */
describe('WorkspaceContext — invalidación del ámbito', () => {
  let errores: string[];

  const montar = () =>
    render(
      <DataScopeProvider>
        <WorkspaceProvider>
          <Sonda />
        </WorkspaceProvider>
      </DataScopeProvider>
    );

  beforeEach(() => {
    errores = [];
    vi.spyOn(console, 'error').mockImplementation((...args: unknown[]) => {
      errores.push(args.map(String).join(' '));
    });

    vi.spyOn(workspaceService, 'getActiveWorkspace').mockResolvedValue(workspace('w-1', 'Rafa'));
    vi.spyOn(workspaceService, 'listWorkspaces').mockResolvedValue([]);
    vi.spyOn(workspaceService, 'switchWorkspace').mockResolvedValue({
      access_token: 'token-2',
      expires_in: 900,
      workspace: workspace('w-2', 'Test 02'),
    });
  });

  afterEach(() => vi.restoreAllMocks());

  it('no avisa de un setState durante el render al cambiar de Workspace', async () => {
    // CA-1 — la prueba que faltaba: la que mira la consola. Falla si el aviso vuelve.
    montar();
    await screen.findByText('Rafa');

    await act(async () => {
      screen.getByRole('button', { name: 'cambiar' }).click();
    });
    await screen.findByText('Test 02');

    expect(errores.filter((mensaje) => mensaje.includes('while rendering a different component')))
      .toEqual([]);
  });

  it('sigue invalidando el ámbito cuando el Workspace cambia de verdad', async () => {
    // CA-2 — es la garantía de `P-081` y no puede degradarse al arreglar el aviso: sin este
    // incremento, `RequireWorkspace` no remonta y las vistas siguen pintando el Workspace anterior.
    montar();
    await screen.findByText('Rafa');
    const antes = Number(screen.getByTestId('scope').textContent);

    await act(async () => {
      screen.getByRole('button', { name: 'cambiar' }).click();
    });
    await screen.findByText('Test 02');

    await waitFor(() =>
      expect(Number(screen.getByTestId('scope').textContent)).toBe(antes + 1)
    );
  });

  it('no invalida nada cuando se resincroniza el mismo Workspace', async () => {
    // MVP-701 — renombrar (MVP-206) resincroniza el contexto sin cambiar de Workspace: remontar el
    // área operativa por un cambio de nombre sería recargarlo todo para nada.
    vi.mocked(workspaceService.switchWorkspace).mockResolvedValue({
      access_token: 'token-1',
      expires_in: 900,
      workspace: workspace('w-1', 'Rafa'),
    });

    montar();
    await screen.findByText('Rafa');
    const antes = Number(screen.getByTestId('scope').textContent);

    await act(async () => {
      screen.getByRole('button', { name: 'cambiar' }).click();
    });

    expect(Number(screen.getByTestId('scope').textContent)).toBe(antes);
  });
});
