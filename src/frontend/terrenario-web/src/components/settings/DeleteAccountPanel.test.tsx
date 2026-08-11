import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DeleteAccountPanel } from './DeleteAccountPanel';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

const logout = vi.fn();
vi.mock('../../contexts/AuthContext', () => ({ useAuth: () => ({ logout }) }));

/**
 * MVP-505 (CA-3/CA-4) — La baja de cuenta es **la única acción del producto sin vuelta atrás**, así
 * que lo que se prueba aquí no es que el botón llame a la API, sino que no se pueda llegar a ella
 * por accidente y que la confirmación esté informada.
 */
describe('DeleteAccountPanel', () => {
  const options = (overrides: Record<string, unknown> = {}) => ({
    is_clear: true,
    obligations: [],
    active_memberships: 0,
    shared_memberships: 0,
    active_sessions: 1,
    confirmation_phrase: 'ELIMINAR MI CUENTA',
    retention_months: 24,
    ...overrides,
  });

  const renderWith = (closure: Record<string, unknown> = {}, extra: Record<string, unknown> = {}) => {
    http = createFakeHttpClient({ '/api/v1/account/closure': options(closure), ...extra });
    return render(
      <MemoryRouter>
        <DeleteAccountPanel />
      </MemoryRouter>
    );
  };

  beforeEach(() => vi.clearAllMocks());

  it('Deberia_ImpedirLaBaja_Cuando_QuedaUnWorkspaceDePropiedadUnica', async () => {
    renderWith({
      is_clear: false,
      obligations: [{ workspace_id: 'w-1', name: 'Finca El Olivar', other_active_members: 2 }],
    });

    // CA-4 — y se dice **cuál** y qué salida tiene, en vez de solo negarse.
    expect(await screen.findByText(/finca el olivar/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /eliminar mi cuenta/i })).toBeDisabled();
  });

  it('Deberia_AvisarDeQueNoHayANadieAQuienTraspasar_Cuando_EstaSola', async () => {
    renderWith({
      is_clear: false,
      obligations: [{ workspace_id: 'w-1', name: 'Finca El Olivar', other_active_members: 0 }],
    });

    expect(await screen.findByText(/sin nadie a quien traspasarlo/i)).toBeInTheDocument();
  });

  it('Deberia_ExplicarQueDesaparece_Antes_DePreguntar', async () => {
    renderWith({ active_memberships: 2, shared_memberships: 2, active_sessions: 3 });

    // Una confirmación que dice «es irreversible» sin decir qué se lleva no informa de nada.
    expect(await screen.findByText(/qué pasará si continúas/i)).toBeInTheDocument();
    expect(screen.getByText(/sales de 2 workspaces compartidos/i)).toBeInTheDocument();
    expect(screen.getByText(/a los 24 meses/i)).toBeInTheDocument();
  });

  /**
   * MVP-811 (`P-118`, CA-4) — El adjetivo era **fijo**: con un solo Workspace salía «Sales de 1
   * Workspace compartidos», y quien era la única persona del suyo leía que lo compartía mientras la
   * misma pantalla le decía más arriba «eres la única persona en este Workspace». En un flujo
   * irreversible, un texto que describe mal la situación resta confianza justo donde hace falta.
   */
  describe('el texto de salida concuerda con la situación (`P-118`)', () => {
    const salida = async () => (await screen.findByText(/Sales de/)).textContent!;

    it('concuerda en número con un solo Workspace', async () => {
      renderWith({ active_memberships: 1, shared_memberships: 1 });

      expect(await salida()).toContain('Sales de 1 Workspace compartido');
      expect(await salida()).not.toContain('Workspace compartidos');
    });

    it('no dice «compartido» cuando la persona está sola', async () => {
      renderWith({ active_memberships: 1, shared_memberships: 0 });

      const texto = await salida();
      expect(texto).toContain('Sales de 1 Workspace');
      expect(texto).not.toContain('compartid');
    });

    it('distingue cuántos de ellos se comparten cuando no son todos', async () => {
      // El caso que la redacción anterior no podía expresar: tres Workspaces, uno en solitario.
      renderWith({ active_memberships: 3, shared_memberships: 2 });

      expect(await salida()).toContain('Sales de 3 Workspaces, 2 de ellos compartidos');
    });
  });

  it('Deberia_ExigirTeclearLaFrase_Cuando_SeConfirma', async () => {
    const user = userEvent.setup();
    renderWith();

    await user.click(await screen.findByRole('button', { name: /eliminar mi cuenta/i }));

    const confirmar = screen.getByRole('button', { name: /sí, eliminar definitivamente/i });
    expect(confirmar).toBeDisabled();

    await user.type(screen.getByLabelText(/para confirmar, escribe/i), 'eliminar mi cuenta');
    // Sensible a mayúsculas a propósito: el gesto tiene que ser deliberado.
    expect(confirmar).toBeDisabled();

    await user.clear(screen.getByLabelText(/para confirmar, escribe/i));
    await user.type(screen.getByLabelText(/para confirmar, escribe/i), 'ELIMINAR MI CUENTA');
    expect(confirmar).toBeEnabled();
  });

  it('Deberia_CerrarSesion_Cuando_LaBajaSeCompleta', async () => {
    const user = userEvent.setup();
    renderWith({}, {
      '/api/v1/account/closure': (opts: { method?: string }) =>
        opts.method === 'POST'
          ? { revoked_sessions: 1, revoked_memberships: 0, cancelled_invitations: 0, purge_after: '2028-07-31T00:00:00Z' }
          : options(),
    });

    await user.click(await screen.findByRole('button', { name: /eliminar mi cuenta/i }));
    await user.type(screen.getByLabelText(/para confirmar, escribe/i), 'ELIMINAR MI CUENTA');
    await user.click(screen.getByRole('button', { name: /sí, eliminar definitivamente/i }));

    // La sesión ya no vale para nada: dejarla abierta daría la sensación de que sigue habiendo cuenta.
    await waitFor(() => expect(logout).toHaveBeenCalled());
  });

  it('Deberia_MostrarElMensajeDeLaApi_Cuando_LaBajaFalla', async () => {
    const user = userEvent.setup();
    const { HttpError } = await import('../../services/http-client');
    renderWith({}, {
      '/api/v1/account/closure': (opts: { method?: string }) => {
        if (opts.method === 'POST') {
          throw new HttpError(422, 'BUSINESS_RULE_WORKSPACE_OWNERSHIP_UNRESOLVED', 'Traspasa tus Workspaces antes.');
        }
        return options();
      },
    });

    await user.click(await screen.findByRole('button', { name: /eliminar mi cuenta/i }));
    await user.type(screen.getByLabelText(/para confirmar, escribe/i), 'ELIMINAR MI CUENTA');
    await user.click(screen.getByRole('button', { name: /sí, eliminar definitivamente/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Traspasa tus Workspaces antes.');
    expect(logout).not.toHaveBeenCalled();
  });
});
