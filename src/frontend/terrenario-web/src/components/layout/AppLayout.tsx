import React, { useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { AppSidebar } from './AppSidebar';
import { AppTopbar } from './AppTopbar';
import { InvitationModal } from '../notifications/InvitationModal';

/** Título contextual de la cabecera según la ruta activa. */
function titleForPath(pathname: string): string {
  if (pathname.startsWith('/app/invitations')) return 'Invitar a alguien';
  if (pathname.startsWith('/app/temporadas')) return 'Temporadas';
  if (pathname.startsWith('/app/terrenos')) return 'Terrenos';
  return 'Inicio';
}

/**
 * Shell del área operativa (estructura del prototipo): barra lateral + cabecera superior + contenido,
 * con drawer en móvil. Aloja además el modal de invitación no bloqueante (MVP-107).
 */
export const AppLayout: React.FC = () => {
  const location = useLocation();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col md:flex-row">
      {/* Barra lateral fija en escritorio */}
      <div className="hidden md:block h-screen sticky top-0 z-30">
        <AppSidebar />
      </div>

      {/* Drawer en móvil */}
      {isMobileMenuOpen && (
        <div className="fixed inset-0 z-50 md:hidden flex">
          <div
            className="fixed inset-0 bg-black/40 backdrop-blur-xs"
            onClick={() => setIsMobileMenuOpen(false)}
            aria-hidden="true"
          />
          <div className="relative z-10 h-full max-w-xs w-full shadow-2xl">
            <AppSidebar onNavigate={() => setIsMobileMenuOpen(false)} />
          </div>
        </div>
      )}

      {/* Contenido */}
      <div className="flex-1 flex flex-col min-w-0 min-h-screen">
        <AppTopbar
          title={titleForPath(location.pathname)}
          onOpenMobileMenu={() => setIsMobileMenuOpen(true)}
        />
        {/* Contenedor de contenido común a TODAS las secciones: misma anchura y padding, para que
            las cajas de las distintas pantallas mantengan tamaño y espaciado coherentes. */}
        <main className="flex-1 overflow-y-auto p-4 sm:p-6 md:p-8">
          <div className="max-w-3xl mx-auto">
            <Outlet />
          </div>
        </main>
      </div>

      <InvitationModal />
    </div>
  );
};
