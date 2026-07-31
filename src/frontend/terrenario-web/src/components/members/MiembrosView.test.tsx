import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MiembrosView } from './MiembrosView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import type { WorkspacePerson } from '../../types/member.types';

/**
 * El cliente HTTP se inyecta por contexto y su provider exige toda la pila de sesión. Aquí interesa
 * la **decisión de la vista**, no el cableado del provider, así que se sustituye el hook.
 */
let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({
  useApiClient: () => http,
}));

/**
 * MVP-501 — «Miembros y accesos» (MVP-204/207/208) es la vista con más lógica de decisión pura del
 * frontend: qué acciones se ofrecen depende de `status`, `can_revoke`, `is_self` y `channel`, y una
 * regresión aquí ofrece retirar el acceso a quien no se puede (o lo esconde a quien sí). Registrado
 * como `P-023` en `MVP-999` precisamente por estar cubierta solo por tipado y QA manual.
 */
describe('MiembrosView', () => {
  const member = (overrides: Partial<WorkspacePerson> = {}): WorkspacePerson => ({
    kind: 'member',
    status: 'activo',
    email: 'antonio@example.test',
    name: 'Antonio Ruiz',
    user_id: 'u-1',
    role: 'workspace_member',
    is_self: false,
    can_revoke: true,
    ...overrides,
  });

  const invitation = (overrides: Partial<WorkspacePerson> = {}): WorkspacePerson => ({
    kind: 'invitation',
    status: 'invitado',
    email: 'nuevo@example.test',
    name: null,
    invitation_id: 'i-1',
    channel: 'email',
    is_expired: false,
    ...overrides,
  });

  const renderWith = (people: WorkspacePerson[], extraRoutes: Record<string, unknown> = {}) => {
    http = createFakeHttpClient({
      '/api/v1/workspace-members': {
        data: people,
        meta: { total: people.length, active: 0, invited: 0, revoked: 0 },
      },
      ...extraRoutes,
    });

    return render(
      <MemoryRouter>
        <MiembrosView />
      </MemoryRouter>
    );
  };

  const rowOf = async (title: string) => {
    const heading = await screen.findByText(title);
    const row = heading.closest('li');
    if (!row) throw new Error(`No se encontró la fila de «${title}»`);
    return within(row);
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('gating de «Retirar acceso» (CA-7/CA-8)', () => {
    it('Deberia_OfrecerRetirarAcceso_Cuando_ElMiembroEsRevocableYNoEsUnoMismo', async () => {
      renderWith([member()]);

      const row = await rowOf('Antonio Ruiz');
      expect(row.getByRole('button', { name: /retirar acceso/i })).toBeInTheDocument();
    });

    it('Deberia_OcultarRetirarAcceso_Cuando_ElMiembroEsUnoMismo', async () => {
      // Aunque el servidor lo marque revocable: nadie se retira el acceso a sí mismo desde aquí.
      renderWith([member({ is_self: true })]);

      const row = await rowOf('Antonio Ruiz');
      expect(row.queryByRole('button', { name: /retirar acceso/i })).not.toBeInTheDocument();
    });

    it('Deberia_OcultarRetirarAcceso_Cuando_ElServidorDiceQueNoEsRevocable', async () => {
      // `can_revoke: false` es la señal de CA-8 (propietario único o último activo).
      renderWith([member({ can_revoke: false, role: 'workspace_owner' })]);

      const row = await rowOf('Antonio Ruiz');
      expect(row.queryByRole('button', { name: /retirar acceso/i })).not.toBeInTheDocument();
    });

    it('Deberia_OcultarRetirarAcceso_Cuando_ElMiembroYaEstaRevocado', async () => {
      renderWith([member({ status: 'revocado' })]);

      const row = await rowOf('Antonio Ruiz');
      expect(row.queryByRole('button', { name: /retirar acceso/i })).not.toBeInTheDocument();
      expect(row.getByText(/dejó de tener acceso/i)).toBeInTheDocument();
    });
  });

  describe('confirmación de la revocación', () => {
    it('Deberia_NoLlamarALaApi_Cuando_SoloSeAbrioLaConfirmacion', async () => {
      const user = userEvent.setup();
      renderWith([member()]);

      const row = await rowOf('Antonio Ruiz');
      await user.click(row.getByRole('button', { name: /retirar acceso/i }));

      expect(row.getByText(/¿retirar el acceso de esta persona\?/i)).toBeInTheDocument();
      // Retirar un acceso no puede ocurrir con un solo clic accidental.
      expect(http.callsTo('/api/v1/workspace-members/u-1/revoke')).toHaveLength(0);
    });

    it('Deberia_RevocarYRecargar_Cuando_SeConfirma', async () => {
      const user = userEvent.setup();
      renderWith([member()], { '/api/v1/workspace-members/u-1/revoke': undefined });

      const row = await rowOf('Antonio Ruiz');
      await user.click(row.getByRole('button', { name: /retirar acceso/i }));
      await user.click(row.getByRole('button', { name: /sí, retirar/i }));

      await waitFor(() => {
        expect(http.callsTo('/api/v1/workspace-members/u-1/revoke')).toHaveLength(1);
      });
      // La lista se recarga tras revocar: dos llamadas al listado (montaje + recarga).
      await waitFor(() => {
        expect(http.calls.filter((call) => call.path === '/api/v1/workspace-members')).toHaveLength(2);
      });
    });

    it('Deberia_MostrarElMensajeDeLaApi_Cuando_LaRevocacionFalla', async () => {
      const user = userEvent.setup();
      const { HttpError } = await import('../../services/http-client');
      renderWith([member()], {
        '/api/v1/workspace-members/u-1/revoke': () => {
          throw new HttpError(409, 'BUSINESS_RULE_LAST_ACTIVE_MEMBER', 'No puede quedarse sin miembros activos.');
        },
      });

      const row = await rowOf('Antonio Ruiz');
      await user.click(row.getByRole('button', { name: /retirar acceso/i }));
      await user.click(row.getByRole('button', { name: /sí, retirar/i }));

      expect(await screen.findByRole('alert')).toHaveTextContent('No puede quedarse sin miembros activos.');
    });
  });

  describe('invitaciones pendientes por canal (MVP-208, CA-7)', () => {
    it('Deberia_OfrecerReenvioPorEmailYPorEnlace_Cuando_LaInvitacionEsPorEmail', async () => {
      renderWith([invitation()]);

      const row = await rowOf('nuevo@example.test');
      expect(row.getByRole('button', { name: /por email/i })).toBeInTheDocument();
      expect(row.getByRole('button', { name: /obtener enlace/i })).toBeInTheDocument();
      expect(row.getByRole('button', { name: /anular invitación/i })).toBeInTheDocument();
    });

    it('Deberia_OcultarElReenvioPorEmail_Cuando_LaInvitacionEsPorEnlace', async () => {
      renderWith([invitation({ channel: 'enlace', email: null })]);

      // Un enlace no tiene destinatario: ofrecer «por email» sería ofrecer escribir a nadie.
      const row = await rowOf('Invitación por enlace');
      expect(row.queryByRole('button', { name: /por email/i })).not.toBeInTheDocument();
      expect(row.getByRole('button', { name: /generar enlace nuevo/i })).toBeInTheDocument();
      expect(row.getByRole('button', { name: /anular enlace/i })).toBeInTheDocument();
    });

    it('Deberia_MarcarLaInvitacionComoCaducada_Cuando_ElServidorLoIndica', async () => {
      renderWith([invitation({ is_expired: true })]);

      const row = await rowOf('nuevo@example.test');
      expect(row.getByText('CADUCADA')).toBeInTheDocument();
    });

    it('Deberia_MostrarElEnlaceNuevo_Cuando_SeRenuevaLaInvitacion', async () => {
      const user = userEvent.setup();
      renderWith([invitation()], {
        '/api/v1/workspaces/invitations/i-1/resend': {
          id: 'i-1',
          channel: 'email',
          email: 'nuevo@example.test',
          accept_url: 'https://terrenario.test/invitations/token-nuevo',
          expires_at: '2026-08-06T10:00:00Z',
          email_sent: false,
        },
      });

      const row = await rowOf('nuevo@example.test');
      await user.click(row.getByRole('button', { name: /obtener enlace/i }));

      const link = await screen.findByLabelText('Enlace de invitación');
      expect(link).toHaveValue('https://terrenario.test/invitations/token-nuevo');
      // Sin envío de correo el aviso debe decir que el enlace no se volverá a mostrar.
      expect(screen.getByRole('status')).toHaveTextContent(/no volveremos a mostrarlo/i);
    });

    it('Deberia_PedirConfirmacionAntesDeAnular_Cuando_SePulsaAnularInvitacion', async () => {
      const user = userEvent.setup();
      renderWith([invitation()], { '/api/v1/workspaces/invitations/i-1/cancel': undefined });

      const row = await rowOf('nuevo@example.test');
      await user.click(row.getByRole('button', { name: /anular invitación/i }));

      expect(row.getByText(/¿anular esta invitación\?/i)).toBeInTheDocument();
      expect(http.callsTo('/api/v1/workspaces/invitations/i-1/cancel')).toHaveLength(0);

      await user.click(row.getByRole('button', { name: /sí, anular/i }));
      await waitFor(() => {
        expect(http.callsTo('/api/v1/workspaces/invitations/i-1/cancel')).toHaveLength(1);
      });
    });
  });

  it('Deberia_ExplicarElFallo_Cuando_NoSePuedeCargarLaLista', async () => {
    const { HttpError } = await import('../../services/http-client');
    http = createFakeHttpClient({
      '/api/v1/workspace-members': () => {
        throw new HttpError(500, 'INTERNAL_ERROR', 'Algo ha fallado.');
      },
    });

    render(
      <MemoryRouter>
        <MiembrosView />
      </MemoryRouter>
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('Algo ha fallado.');
  });
});
