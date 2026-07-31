import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useNotifications } from '../../contexts/NotificationsContext';
import { InvitationServiceError } from '../../services/invitation.service';

interface BusyState {
  id: string;
  action: 'accept' | 'reject';
}

/**
 * Lógica compartida por la campanita, el modal y la pantalla de decisión (MVP-107): aceptar sitúa
 * la sesión en el Workspace y navega a la operativa; rechazar declina sin sacar de la plataforma.
 * Centraliza el estado de "ocupado" por invitación y el mensaje de error.
 */
export function useInvitationActions() {
  const { accept, reject } = useNotifications();
  const navigate = useNavigate();
  const [busy, setBusy] = useState<BusyState | null>(null);
  const [error, setError] = useState<string | null>(null);

  const busyFor = (id: string): 'accept' | 'reject' | null =>
    busy?.id === id ? busy.action : null;

  const run = async (
    id: string,
    action: 'accept' | 'reject',
    fallback: string,
    task: () => Promise<void>
  ): Promise<boolean> => {
    setError(null);
    setBusy({ id, action });
    try {
      await task();
      return true;
    } catch (err: unknown) {
      setError(err instanceof InvitationServiceError ? err.message : fallback);
      return false;
    } finally {
      setBusy(null);
    }
  };

  const acceptInvitation = async (id: string, navigateOnSuccess = true): Promise<boolean> => {
    const ok = await run(id, 'accept', 'No se pudo aceptar la invitación. Inténtalo de nuevo.', async () => {
      await accept(id);
    });
    if (ok && navigateOnSuccess) navigate('/app', { replace: true });
    return ok;
  };

  const rejectInvitation = (id: string): Promise<boolean> =>
    run(id, 'reject', 'No se pudo rechazar la invitación. Inténtalo de nuevo.', async () => {
      await reject(id);
    });

  return { busyFor, error, acceptInvitation, rejectInvitation };
}
