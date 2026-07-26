import React from 'react';
import { NotificationBell } from '../notifications/NotificationBell';

interface AppTopbarProps {
  title: string;
  onOpenMobileMenu: () => void;
}

/**
 * Cabecera superior del área operativa (shell del prototipo): botón de menú en móvil, título
 * contextual y el centro de notificaciones (campanita, MVP-107) a la derecha.
 */
export const AppTopbar: React.FC<AppTopbarProps> = ({ title, onOpenMobileMenu }) => (
  <header className="bg-[#fcf9f4]/80 backdrop-blur-md border-b border-[#e5e2dd] sticky top-0 z-20 px-4 md:px-8 py-3.5 flex items-center justify-between gap-3">
    <div className="flex items-center gap-3 min-w-0">
      <button
        type="button"
        onClick={onOpenMobileMenu}
        className="md:hidden p-2 rounded-lg text-[#45483c] hover:bg-[#f0ede8] transition-colors"
        aria-label="Abrir menú"
      >
        <span className="material-symbols-outlined" aria-hidden="true">menu</span>
      </button>
      <h2 className="font-headline font-bold text-lg md:text-xl text-[#1c1c19] tracking-tight truncate">
        {title}
      </h2>
    </div>

    <div className="flex items-center gap-2 md:gap-3">
      <NotificationBell />
    </div>
  </header>
);
