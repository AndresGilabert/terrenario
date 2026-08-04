import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { NotificationsProvider, useNotifications } from './NotificationsContext';
import type { ReceivedInvitation } from '../types/invitation.types';
import type { Workspace } from '../types/workspace.types';

const auth = { isAuthenticated: true, isLoading: false, getAccessToken: vi.fn() };
const workspace = { acceptInvitationById: vi.fn() };
const listReceivedInvitations = vi.fn();
const rejectReceivedInvitation = vi.fn();

vi.mock('./AuthContext', () => ({ useAuth: () => auth }));
vi.mock('./WorkspaceContext', () => ({ useWorkspace: () => workspace }));
vi.mock('../services/invitation.service', () => ({
  invitationService: {
    listReceivedInvitations: (...args: unknown[]) => listReceivedInvitations(...args),
    rejectReceivedInvitation: (...args: unknown[]) => rejectReceivedInvitation(...args),
  },
  InvitationServiceError: class extends Error {},
}));

const SEEN_STORAGE_KEY = 'terrenario:seen_invitations';

/**
 * MVP-501 — El centro de notificaciones (MVP-107) decide qué invitación se ofrece en el modal no
 * bloqueante y cuál ya se vio. Esa decisión vive en `localStorage` y no la cubre el tipado: es el
 * caso que `P-012` señala por su nombre en `MVP-999`.
 */
describe('NotificationsProvider', () => {
  const ws = (id: string): Workspace => ({ id, name: `Workspace ${id}` }) as Workspace;

  const invitation = (id: string): ReceivedInvitation => ({
    id,
    channel: 'email',
    workspace: ws(`w-${id}`),
    invited_by: 'Antonio',
    expires_at: '2026-08-06T10:00:00Z',
    created_at: '2026-07-30T10:00:00Z',
  });

  /** Sonda: expone el valor del contexto sin depender de ninguna vista real. */
  function Probe() {
    const { receivedInvitations, pendingCount, isLoading, newInvitation, dismissNew, accept, reject } =
      useNotifications();

    return (
      <div>
        <span data-testid="loading">{String(isLoading)}</span>
        <span data-testid="pending">{pendingCount}</span>
        <span data-testid="new">{newInvitation?.id ?? 'ninguna'}</span>
        <span data-testid="ids">{receivedInvitations.map((i) => i.id).join(',')}</span>
        <button onClick={dismissNew}>Descartar</button>
        {/* Las dos acciones propagan el error a quien las llama (lo consume `useInvitationActions`
            para pintar el mensaje). La sonda lo absorbe para que un rechazo esperado no se cuente
            como fallo no gestionado del test. */}
        <button onClick={() => void accept(receivedInvitations[0]?.id).catch(() => {})}>Aceptar</button>
        <button onClick={() => void reject(receivedInvitations[0]?.id).catch(() => {})}>Rechazar</button>
      </div>
    );
  }

  const renderProbe = () =>
    render(
      <NotificationsProvider>
        <Probe />
      </NotificationsProvider>
    );

  const settled = async () => {
    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'));
  };

  beforeEach(() => {
    vi.clearAllMocks();
    auth.isAuthenticated = true;
    auth.isLoading = false;
    auth.getAccessToken.mockResolvedValue('token-valido');
    listReceivedInvitations.mockResolvedValue([]);
  });

  describe('carga de la bandeja', () => {
    it('Deberia_PublicarLasInvitacionesYSuContador_Cuando_LaCargaVaBien', async () => {
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);

      renderProbe();
      await settled();

      expect(screen.getByTestId('pending')).toHaveTextContent('2');
      expect(screen.getByTestId('ids')).toHaveTextContent('a,b');
    });

    it('Deberia_NoPedirNada_Cuando_NoHaySesion', async () => {
      auth.isAuthenticated = false;

      renderProbe();
      await settled();

      // La bandeja es de la cuenta autenticada: sin sesión no hay a quién preguntar.
      expect(listReceivedInvitations).not.toHaveBeenCalled();
      expect(screen.getByTestId('pending')).toHaveTextContent('0');
    });

    it('Deberia_DejarLaBandejaVacia_Cuando_LaCargaFalla', async () => {
      listReceivedInvitations.mockRejectedValue(new Error('red caída'));

      renderProbe();
      await settled();

      // La bandeja es informativa: si falla, la operativa sigue. Lo que no puede es romper la app.
      expect(screen.getByTestId('pending')).toHaveTextContent('0');
    });
  });

  describe('invitación «nueva» y tracking de vistas', () => {
    it('Deberia_OfrecerLaPrimeraNoVista_Cuando_HayVariasPendientes', async () => {
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);

      renderProbe();
      await settled();

      expect(screen.getByTestId('new')).toHaveTextContent('a');
    });

    it('Deberia_PasarALaSiguiente_Cuando_SeDescartaLaActual', async () => {
      const user = userEvent.setup();
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);

      renderProbe();
      await settled();
      await user.click(screen.getByRole('button', { name: 'Descartar' }));

      expect(screen.getByTestId('new')).toHaveTextContent('b');
      // Descartar la retira del modal pero **no** de la bandeja: sigue pendiente de decidir.
      expect(screen.getByTestId('pending')).toHaveTextContent('2');
    });

    it('Deberia_NoVolverAOfrecerla_Cuando_YaSeMarcoComoVistaEnUnaSesionAnterior', async () => {
      localStorage.setItem(SEEN_STORAGE_KEY, JSON.stringify(['a']));
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);

      renderProbe();
      await settled();

      expect(screen.getByTestId('new')).toHaveTextContent('b');
    });

    it('Deberia_PodarLasVistasQueYaNoEstanPendientes_Cuando_SeRecargaLaBandeja', async () => {
      // «a» se aceptó desde otro dispositivo: conservarla en el almacén lo haría crecer sin fin.
      localStorage.setItem(SEEN_STORAGE_KEY, JSON.stringify(['a', 'b']));
      listReceivedInvitations.mockResolvedValue([invitation('b')]);

      renderProbe();
      await settled();

      await waitFor(() => {
        expect(JSON.parse(localStorage.getItem(SEEN_STORAGE_KEY) ?? '[]')).toEqual(['b']);
      });
    });

    it('Deberia_SeguirFuncionando_Cuando_ElAlmacenTieneContenidoIlegible', async () => {
      localStorage.setItem(SEEN_STORAGE_KEY, 'esto-no-es-json');
      listReceivedInvitations.mockResolvedValue([invitation('a')]);

      renderProbe();
      await settled();

      expect(screen.getByTestId('new')).toHaveTextContent('a');
    });
  });

  describe('decisión sobre una invitación', () => {
    it('Deberia_SacarlaDeLaBandeja_Cuando_SeAcepta', async () => {
      const user = userEvent.setup();
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);
      workspace.acceptInvitationById.mockResolvedValue(ws('w-a'));

      renderProbe();
      await settled();
      await act(async () => {
        await user.click(screen.getByRole('button', { name: 'Aceptar' }));
      });

      expect(workspace.acceptInvitationById).toHaveBeenCalledWith('a');
      // Aceptada ⇒ ya se es miembro: deja de ser una decisión pendiente de inmediato.
      await waitFor(() => expect(screen.getByTestId('ids')).toHaveTextContent('b'));
    });

    it('Deberia_SacarlaDeLaBandeja_Cuando_SeRechaza', async () => {
      const user = userEvent.setup();
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);
      rejectReceivedInvitation.mockResolvedValue(undefined);

      renderProbe();
      await settled();
      await act(async () => {
        await user.click(screen.getByRole('button', { name: 'Rechazar' }));
      });

      expect(rejectReceivedInvitation).toHaveBeenCalledWith('a', 'token-valido');
      await waitFor(() => expect(screen.getByTestId('ids')).toHaveTextContent('b'));
    });

    it('Deberia_NoRechazar_Cuando_LaSesionYaNoEsValida', async () => {
      const user = userEvent.setup();
      listReceivedInvitations.mockResolvedValue([invitation('a')]);

      renderProbe();
      await settled();

      auth.getAccessToken.mockResolvedValue(null);
      await act(async () => {
        await user.click(screen.getByRole('button', { name: 'Rechazar' }));
      });

      expect(rejectReceivedInvitation).not.toHaveBeenCalled();
      // La invitación sigue viva: no se pierde una decisión pendiente por un token caducado.
      expect(screen.getByTestId('ids')).toHaveTextContent('a');
    });
  });
});
