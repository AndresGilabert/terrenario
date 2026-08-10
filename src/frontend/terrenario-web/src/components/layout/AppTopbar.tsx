import React from 'react';
import { NotificationBell } from '../notifications/NotificationBell';
import { SeasonSwitcher } from '../seasons/SeasonSwitcher';
import { useWorkspace } from '../../contexts/WorkspaceContext';

interface AppTopbarProps {
  title: string;
  onOpenMobileMenu: () => void;
}

/**
 * Cabecera superior del área operativa (shell del prototipo): botón de menú en móvil, la
 * temporada activa y el Workspace (para ver de un vistazo qué está activo), el título contextual y
 * el centro de notificaciones (campanita, MVP-107).
 *
 * La píldora de temporada replica el prototipo (`TopNavbar`): verde con punto pulsante cuando hay
 * temporada activa. Desde MVP-701 (`P-083`) es además el **conmutador** de la campaña de trabajo
 * (`SeasonSwitcher`), no un rótulo: es la clase de control que pertenece al shell, como el de
 * Workspace, ahora que la campaña de trabajo gobierna el defecto de todas las vistas (RN-008).
 */
export const AppTopbar: React.FC<AppTopbarProps> = ({ title, onOpenMobileMenu }) => {
  const { activeWorkspace } = useWorkspace();

  return (
    <header className="bg-[#fcf9f4]/80 backdrop-blur-md border-b border-[#e5e2dd] sticky top-0 z-20 px-4 md:px-8 py-3 flex items-center justify-between gap-3">
      <div className="flex items-center gap-3 min-w-0">
        <button
          type="button"
          onClick={onOpenMobileMenu}
          className="lg:hidden p-2 rounded-lg text-[#45483c] hover:bg-[#f0ede8] transition-colors"
          aria-label="Abrir menú"
        >
          <span className="material-symbols-outlined" aria-hidden="true">menu</span>
        </button>

        <div className="min-w-0">
          {/* Contexto activo: temporada + Workspace, de un vistazo (prototipo TopNavbar).
              La píldora de campaña es un conmutador desde MVP-701; el selector de Workspace vive en
              la barra lateral (MVP-104), que es donde lo puso el prototipo. */}
          <div className="flex items-center gap-2 min-w-0">
            <SeasonSwitcher />
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
