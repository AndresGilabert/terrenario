import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { NotificationsProvider, useNotifications } from './NotificationsContext';
import type { ReceivedInvitation } from '../types/invitation.types';
import type { ReactivationRequest } from '../types/workspace-lifecycle.types';
import type { Workspace } from '../types/workspace.types';

const auth = { isAuthenticated: true, isLoading: false, getAccessToken: vi.fn() };
const workspace = { acceptInvitationById: vi.fn() };
const listReceivedInvitations = vi.fn();
const rejectReceivedInvitation = vi.fn();
const listPendingAuthorizations = vi.fn();

/** Doble estable: el cliente real es un `useMemo` de identidad fija y aquí debe serlo también. */
const httpClient = { request: vi.fn() };
const reactivationService = {
  listPendingAuthorizations: () => listPendingAuthorizations(),
};

vi.mock('./AuthContext', () => ({ useAuth: () => auth }));
vi.mock('./WorkspaceContext', () => ({ useWorkspace: () => workspace }));
vi.mock('./ApiContext', () => ({ useApiClient: () => httpClient }));
vi.mock('../services/workspace-lifecycle.service', () => ({
  createReactivationService: () => reactivationService,
}));
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

  const reactivation = (id: string): ReactivationRequest => ({
    id,
    workspace: ws(`w-${id}`),
    requested_by: { user_id: `u-${id}`, name: 'Marta', email: 'marta@ejemplo.com' },
    requested_at: '2026-08-09T10:00:00Z',
    expires_at: '2026-08-20T10:00:00Z',
  });

  const reactivationsPayload = (...ids: string[]) => ({
    data: ids.map(reactivation),
    meta: { total: ids.length },
  });

  /** Sonda: expone el valor del contexto sin depender de ninguna vista real. */
  function Probe() {
    const {
      receivedInvitations,
      pendingReactivations,
      pendingCount,
      isLoading,
      newInvitation,
      dismissNew,
      accept,
      reject,
      refresh,
    } = useNotifications();

    return (
      <div>
        <span data-testid="loading">{String(isLoading)}</span>
        <span data-testid="pending">{pendingCount}</span>
        <span data-testid="new">{newInvitation?.id ?? 'ninguna'}</span>
        <span data-testid="ids">{receivedInvitations.map((i) => i.id).join(',')}</span>
        <span data-testid="reactivaciones">
          {pendingReactivations.map((r) => r.id).join(',') || 'ninguna'}
        </span>
        <button onClick={dismissNew}>Descartar</button>
        {/* Lo que hace la pantalla de decisión tras autorizar o denegar (MVP-808, CA-4). */}
        <button onClick={() => void refresh()}>Refrescar</button>
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
    listPendingAuthorizations.mockResolvedValue(reactivationsPayload());
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
      expect(listPendingAuthorizations).not.toHaveBeenCalled();
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

  /**
   * MVP-808 (HU-2, CA-3/CA-4) — Segundo tipo de aviso de la bandeja: la solicitud de reactivación de
   * un Workspace (`RN-040`), que hasta ahora solo se avisaba por correo.
   */
  describe('solicitudes de reactivación pendientes (MVP-808)', () => {
    it('Deberia_PublicarlasEnLaBandeja_Cuando_HayAlgunaEsperandoDecision', async () => {
      listPendingAuthorizations.mockResolvedValue(reactivationsPayload('r1'));

      renderProbe();
      await settled();

      expect(screen.getByTestId('reactivaciones')).toHaveTextContent('r1');
      expect(screen.getByTestId('pending')).toHaveTextContent('1');
    });

    it('Deberia_SumarLosDosTiposEnElContador_Cuando_ConvivenEnLaBandeja', async () => {
      listReceivedInvitations.mockResolvedValue([invitation('a'), invitation('b')]);
      listPendingAuthorizations.mockResolvedValue(reactivationsPayload('r1'));

      renderProbe();
      await settled();

      // La campanita anuncia decisiones pendientes, no invitaciones: las tres cuentan.
      expect(screen.getByTestId('pending')).toHaveTextContent('3');
    });

    it('Deberia_NoOfrecerlaEnElModal_Cuando_HaySolicitudPeroNingunaInvitacion', async () => {
      listPendingAuthorizations.mockResolvedValue(reactivationsPayload('r1'));

      renderProbe();
      await settled();

      // El modal no bloqueante es de invitaciones (MVP-107): autorizar una reactivación es
      // irreversible y se decide en `/reactivations`, no en un aviso emergente.
      expect(screen.getByTestId('new')).toHaveTextContent('ninguna');
    });

    it('Deberia_DesaparecerDeLaBandeja_Cuando_LaSolicitudSeResuelve', async () => {
      const user = userEvent.setup();
      // CA-4: da igual la vía —autorizada o denegada—; el servidor deja de listarla y el refresco
      // que dispara la pantalla de decisión la retira del aviso.
      listPendingAuthorizations
        .mockResolvedValueOnce(reactivationsPayload('r1'))
        .mockResolvedValue(reactivationsPayload());

      renderProbe();
      await settled();
      expect(screen.getByTestId('reactivaciones')).toHaveTextContent('r1');

      await act(async () => {
        await user.click(screen.getByRole('button', { name: 'Refrescar' }));
      });

      await waitFor(() =>
        expect(screen.getByTestId('reactivaciones')).toHaveTextContent('ninguna')
      );
      expect(screen.getByTestId('pending')).toHaveTextContent('0');
    });

    it('Deberia_SeguirMostrandoLaSolicitud_Cuando_FallaLaCargaDeInvitaciones', async () => {
      // Las dos fuentes fallan por separado: un fallo leyendo invitaciones no puede esconder una
      // decisión irreversible pendiente, que es justo el hueco que cierra esta historia.
      listReceivedInvitations.mockRejectedValue(new Error('red caída'));
      listPendingAuthorizations.mockResolvedValue(reactivationsPayload('r1'));

      renderProbe();
      await settled();

      expect(screen.getByTestId('reactivaciones')).toHaveTextContent('r1');
      expect(screen.getByTestId('ids').textContent).toBe('');
    });

    it('Deberia_SeguirMostrandoLasInvitaciones_Cuando_FallaLaCargaDeReactivaciones', async () => {
      listReceivedInvitations.mockResolvedValue([invitation('a')]);
      listPendingAuthorizations.mockRejectedValue(new Error('red caída'));

      renderProbe();
      await settled();

      expect(screen.getByTestId('ids')).toHaveTextContent('a');
      expect(screen.getByTestId('reactivaciones')).toHaveTextContent('ninguna');
    });
  });

  /**
   * MVP-808 (HU-1, CA-1/CA-2) — Refresco al recuperar el foco de la ventana con intervalo mínimo.
   *
   * Los temporizadores son falsos porque la salvaguarda se mide en tiempo transcurrido: el `CA-2`
   * exige **contar peticiones**, no comprobar que existe un intervalo.
   */
  describe('refresco al recuperar el foco', () => {
    const volverALaVentana = async () => {
      await act(async () => {
        document.dispatchEvent(new Event('visibilitychange'));
        window.dispatchEvent(new Event('focus'));
      });
    };

    beforeEach(() => {
      // `shouldAdvanceTime` mantiene vivos los `waitFor` de Testing Library, que se apoyan en
      // temporizadores reales para reintentar.
      vi.useFakeTimers({ shouldAdvanceTime: true });
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('Deberia_TraerLaInvitacionNueva_Cuando_SeVuelveALaVentanaPasadoElIntervalo', async () => {
      listReceivedInvitations.mockResolvedValueOnce([]).mockResolvedValue([invitation('a')]);

      renderProbe();
      await settled();
      expect(screen.getByTestId('pending')).toHaveTextContent('0');

      vi.advanceTimersByTime(30_000);
      await volverALaVentana();

      // CA-1: la invitación emitida con la sesión abierta aparece sin recargar la página.
      await waitFor(() => expect(screen.getByTestId('ids')).toHaveTextContent('a'));
    });

    it('Deberia_NoLanzarUnaPeticionPorCadaCambioDePestana_Cuando_SeVuelveVeinteVecesSeguidas', async () => {
      renderProbe();
      await settled();
      expect(listReceivedInvitations).toHaveBeenCalledTimes(1);

      // Veinte idas y vueltas en el mismo intervalo: cuarenta eventos (`visibilitychange` + `focus`).
      for (let i = 0; i < 20; i++) {
        vi.advanceTimersByTime(500);
        await volverALaVentana();
      }

      // CA-2: ninguna petición nueva. Sin el intervalo mínimo serían 40 más.
      expect(listReceivedInvitations).toHaveBeenCalledTimes(1);
      expect(listPendingAuthorizations).toHaveBeenCalledTimes(1);

      // Pasado el intervalo, la siguiente vuelta sí refresca: la salvaguarda espacia, no anula.
      vi.advanceTimersByTime(30_000);
      await volverALaVentana();

      await waitFor(() => expect(listReceivedInvitations).toHaveBeenCalledTimes(2));
      expect(listPendingAuthorizations).toHaveBeenCalledTimes(2);
    });

    it('Deberia_ContarUnaSolaPeticion_Cuando_LaMismaVueltaDisparaLosDosEventos', async () => {
      renderProbe();
      await settled();

      vi.advanceTimersByTime(30_000);
      await volverALaVentana();

      // Volver a la pestaña emite `visibilitychange` **y** `focus`: sin el intervalo, una sola
      // vuelta ya costaría dos peticiones.
      await waitFor(() => expect(listReceivedInvitations).toHaveBeenCalledTimes(2));
      expect(listReceivedInvitations).toHaveBeenCalledTimes(2);
    });

    it('Deberia_NoRefrescar_Cuando_LaPestanaPasaAOculta', async () => {
      renderProbe();
      await settled();

      vi.advanceTimersByTime(30_000);
      vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden');
      await act(async () => {
        document.dispatchEvent(new Event('visibilitychange'));
      });

      // Irse de la pestaña no es enterarse de nada: el refresco es al **volver**.
      expect(listReceivedInvitations).toHaveBeenCalledTimes(1);
    });

    it('Deberia_NoEscucharElFoco_Cuando_NoHaySesion', async () => {
      auth.isAuthenticated = false;

      renderProbe();
      await settled();

      vi.advanceTimersByTime(30_000);
      await volverALaVentana();

      expect(listReceivedInvitations).not.toHaveBeenCalled();
      expect(listPendingAuthorizations).not.toHaveBeenCalled();
    });
  });
});
