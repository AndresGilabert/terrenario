import React from 'react';
import { useNavigate } from 'react-router';
import { NotificationBell } from '../notifications/NotificationBell';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useSeason } from '../../contexts/SeasonContext';

interface AppTopbarProps {
  title: string;
  onOpenMobileMenu: () => void;
}

/** Forma y tamaño comunes de la píldora de contexto: solo cambia el color según el estado. */
const PILL_BASE =
  'inline-flex items-center gap-1.5 text-xs px-2.5 py-0.5 rounded-full font-semibold border';

/**
 * Cabecera superior del área operativa (shell del prototipo): botón de menú en móvil, la
 * temporada activa y el Workspace (para ver de un vistazo qué está activo), el título contextual y
 * el centro de notificaciones (campanita, MVP-107).
 *
 * La píldora de temporada replica el prototipo (`TopNavbar`): verde con punto pulsante cuando hay
 * temporada activa. Si el Workspace no tiene ninguna activa, conduce a la misma decisión (MVP-201) en
 * vez de dejar el hueco vacío; desde MVP-208 (CA-8) distingue «no hay ninguna» de «hay pero ninguna
 * activa», que es donde antes ofrecía crear una temporada que probablemente ya existía.
 */
export const AppTopbar: React.FC<AppTopbarProps> = ({ title, onOpenMobileMenu }) => {
  const navigate = useNavigate();
  const { activeWorkspace } = useWorkspace();
  const { activeSeason, seasons, isLoading } = useSeason();

  return (
    <header className="bg-[#fcf9f4]/80 backdrop-blur-md border-b border-[#e5e2dd] sticky top-0 z-20 px-4 md:px-8 py-3 flex items-center justify-between gap-3">
      <div className="flex items-center gap-3 min-w-0">
        <button
          type="button"
          onClick={onOpenMobileMenu}
          className="md:hidden p-2 rounded-lg text-[#45483c] hover:bg-[#f0ede8] transition-colors"
          aria-label="Abrir menú"
        >
          <span className="material-symbols-outlined" aria-hidden="true">menu</span>
        </button>

        <div className="min-w-0">
          {/* Contexto activo: temporada + Workspace, de un vistazo (prototipo TopNavbar).
              Ambas píldoras comparten forma y tamaño (PILL_BASE); solo cambia el color y, en la
              activa, el punto pulsa. Así el diseño es coherente entre estados. */}
          <div className="flex items-center gap-2 min-w-0">
            {isLoading ? null : activeSeason ? (
              <span className={`${PILL_BASE} bg-[#c9f16f] text-[#33450d] border-[#aed456] max-w-[55vw] sm:max-w-none`}>
                <span className="w-1.5 h-1.5 rounded-full bg-[#33450d] animate-pulse shrink-0" aria-hidden="true" />
                <span className="truncate">{activeSeason.name}</span>
              </span>
            ) : (
              // Sin temporada activa: la píldora lleva a la decisión (MVP-201), pero solo promete
              // «crear» cuando de verdad no hay ninguna; si las hay, lo que toca es elegir.
              <button
                type="button"
                onClick={() => navigate('/app/temporada/nueva')}
                className={`${PILL_BASE} bg-[#f0ede8] text-[#45483c] border-[#dcd9d2] hover:bg-[#ebe8e3] transition-colors`}
                title="Este Workspace no tiene temporada activa"
              >
                <span className="w-1.5 h-1.5 rounded-full bg-[#a2a496] shrink-0" aria-hidden="true" />
                <span>{seasons.length > 0 ? 'Sin temporada activa · Elegir' : 'Sin temporada · Crear'}</span>
              </button>
            )}
            {activeWorkspace && (
              <span className="hidden sm:inline text-xs text-[#76786b] truncate">
                • {activeWorkspace.name}
              </span>
            )}
          </div>
          <h2 className="font-headline font-bold text-lg md:text-xl text-[#1c1c19] tracking-tight truncate">
            {title}
          </h2>
        </div>
      </div>

      <div className="flex items-center gap-2 md:gap-3">
        <NotificationBell />
      </div>
    </header>
  );
};
