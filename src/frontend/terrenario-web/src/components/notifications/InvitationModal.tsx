import React from 'react';
import { useNotifications } from '../../contexts/NotificationsContext';
import { Modal } from '../common/Modal';
import { ReceivedInvitationCard } from './ReceivedInvitationCard';
import { useInvitationActions } from './useInvitationActions';

/**
 * MVP-107 (HU-2, CA-3) — Modal no bloqueante que aparece al llegar a la operativa con una invitación
 * nueva pendiente. Se puede cerrar dejándola pendiente: no es una puerta obligatoria. Aceptar sitúa
 * la sesión en el Workspace; rechazar la declina sin sacar de la plataforma.
 *
 * `MVP-704` lo trajo a {@link Modal}. Tenía ya su propio `Escape` y su propio clic en el velo, escritos
 * a mano y distintos de los de los demás: eran dos de las tres formas de hacer lo mismo que había en el
 * producto. Lo que aporta el componente y aquí faltaba es apagar el fondo y atrapar el foco —justo lo
 * que más importa en un diálogo que aparece **solo**, sin que nadie lo haya pedido, y que por tanto
 * puede pillar al usuario tecleando en otra cosa—.
 */
export const InvitationModal: React.FC = () => {
  const { newInvitation, dismissNew } = useNotifications();
  const { busyFor, error, acceptInvitation, rejectInvitation } = useInvitationActions();

  const busy = newInvitation ? busyFor(newInvitation.id) : null;

  if (!newInvitation) return null;

  return (
    <Modal
      isOpen
      onClose={dismissNew}
      title="Tienes una invitación"
      header={null}
      panelClassName="max-w-md"
      // Mientras se acepta o se rechaza no se cierra por ninguna de las tres vías: el resultado hay
      // que verlo, y era ya el criterio del clic en el velo antes de la unificación.
      closeDisabled={busy !== null}
    >
      {/* El panel de este diálogo es crema y no blanco. Se pinta desde dentro en vez de sobrescribir
          el fondo del panel: dos utilidades de fondo en la misma clase dependerían del orden de la
          hoja de estilos, que no es algo que se pueda dar por seguro. */}
      <div className="bg-[#fcf9f4] p-6 space-y-4">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
            <span className="material-symbols-outlined text-base" aria-hidden="true">mail</span>
            <span>Tienes una invitación</span>
          </div>
          <button
            type="button"
            onClick={dismissNew}
            disabled={busy !== null}
            aria-label="Cerrar y decidir más tarde"
            className="text-[#76786b] hover:text-[#1c1c19] text-xl leading-none px-1 disabled:opacity-60"
          >
            ×
          </button>
        </div>

        {error && (
          <p role="alert" className="text-sm text-red-700">
            {error}
          </p>
        )}

        <ReceivedInvitationCard
          invitation={newInvitation}
          busy={busy}
          onAccept={() => void acceptInvitation(newInvitation.id)}
          onReject={() => void rejectInvitation(newInvitation.id)}
        />

        <button
          type="button"
          onClick={dismissNew}
          disabled={busy !== null}
          className="w-full text-center text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] py-1 disabled:opacity-60"
        >
          Decidir más tarde
        </button>
      </div>
    </Modal>
  );
};
