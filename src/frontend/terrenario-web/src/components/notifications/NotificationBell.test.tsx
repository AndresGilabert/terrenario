import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { NotificationBell } from './NotificationBell';
import type { ReceivedInvitation } from '../../types/invitation.types';
import type { ReactivationRequest } from '../../types/workspace-lifecycle.types';
import type { Workspace } from '../../types/workspace.types';

const notifications = {
  receivedInvitations: [] as ReceivedInvitation[],
  pendingReactivations: [] as ReactivationRequest[],
  pendingCount: 0,
  accept: vi.fn(),
  reject: vi.fn(),
};

vi.mock('../../contexts/NotificationsContext', () => ({
  useNotifications: () => notifications,
}));

/**
 * MVP-808 (CA-3) — La campanita deja de ser solo de invitaciones: también anuncia las solicitudes de
 * reactivación de Workspace pendientes de decidir, que hasta ahora únicamente llegaban por correo.
 */
describe('NotificationBell', () => {
  const ws = (id: string): Workspace => ({ id, name: `Finca ${id}` }) as Workspace;

  const invitation = (id: string): ReceivedInvitation => ({
    id,
    channel: 'email',
    workspace: ws(`w-${id}`),
    invited_by: 'Antonio',
    expires_at: '2099-01-01T10:00:00Z',
    created_at: '2026-07-30T10:00:00Z',
  });

  const reactivation = (id: string): ReactivationRequest => ({
    id,
    workspace: { id: `w-${id}`, name: 'Finca El Olivar' } as Workspace,
    requested_by: { user_id: `u-${id}`, name: 'Marta', email: 'marta@ejemplo.com' },
    requested_at: '2026-08-09T10:00:00Z',
    expires_at: '2099-01-01T10:00:00Z',
  });

  const renderBell = () =>
    render(
      <MemoryRouter>
        <NotificationBell />
      </MemoryRouter>
    );

  const openTray = async () => {
    const user = userEvent.setup();
    renderBell();
    await user.click(screen.getByRole('button', { name: /Notificaciones/ }));
    return user;
  };

  beforeEach(() => {
    notifications.receivedInvitations = [];
    notifications.pendingReactivations = [];
    notifications.pendingCount = 0;
  });

  it('Deberia_AnunciarLaSolicitudConEnlaceADecidir_Cuando_HayUnaReactivacionPendiente', async () => {
    notifications.pendingReactivations = [reactivation('r1')];
    notifications.pendingCount = 1;

    await openTray();

    expect(screen.getByText('Finca El Olivar')).toBeInTheDocument();
    expect(screen.getByText(/Marta pide que le traspases/)).toBeInTheDocument();
    // CA-3: el aviso lleva a la pantalla de decisión que ya existe, sin pasar por el correo.
    expect(screen.getByRole('link', { name: 'Ver y decidir' })).toHaveAttribute(
      'href',
      '/reactivations'
    );
  });

  it('Deberia_ContarLosDosTiposEnLaChapa_Cuando_ConvivenAvisos', async () => {
    notifications.receivedInvitations = [invitation('a')];
    notifications.pendingReactivations = [reactivation('r1')];
    notifications.pendingCount = 2;

    await openTray();

    expect(
      screen.getByRole('button', { name: 'Notificaciones: 2 aviso(s) pendiente(s)' })
    ).toBeInTheDocument();
    // Con los dos tipos a la vez cada bloque se rotula; con uno solo el rótulo sobra.
    expect(screen.getByText('Invitaciones')).toBeInTheDocument();
    expect(screen.getByText('Reactivaciones')).toBeInTheDocument();
  });

  it('Deberia_NoRotularLosBloques_Cuando_SoloHayUnTipoDeAviso', async () => {
    notifications.pendingReactivations = [reactivation('r1')];
    notifications.pendingCount = 1;

    await openTray();

    expect(screen.queryByText('Reactivaciones')).not.toBeInTheDocument();
    expect(screen.queryByText('Invitaciones')).not.toBeInTheDocument();
  });

  it('Deberia_DecirQueNoHayNada_Cuando_LaBandejaEstaVacia', async () => {
    await openTray();

    expect(screen.getByText('No tienes avisos pendientes.')).toBeInTheDocument();
  });

  it('Deberia_NoOfrecerAutorizarNiDenegarDesdeLaCampanita_Cuando_HayUnaSolicitud', async () => {
    notifications.pendingReactivations = [reactivation('r1')];
    notifications.pendingCount = 1;

    await openTray();

    // La decisión es irreversible —el Workspace vuelve y cambia de propietario—: se toma en
    // `/reactivations`, que es donde se explica lo que implica, no en un menú emergente.
    expect(screen.queryByRole('button', { name: /Autorizar/ })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /Rechazar/ })).not.toBeInTheDocument();
  });
});
