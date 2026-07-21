import React from 'react';
import { ViewMode, WorkspaceConfig } from '../types';

interface TopNavbarProps {
  currentView: ViewMode;
  workspace: WorkspaceConfig;
  onOpenMobileMenu: () => void;
  onOpenNewActivityModal: () => void;
  onSelectView: (view: ViewMode) => void;
}

export const TopNavbar: React.FC<TopNavbarProps> = ({
  currentView,
  workspace,
  onOpenMobileMenu,
  onOpenNewActivityModal,
  onSelectView
}) => {
  const getTitle = () => {
    switch (currentView) {
      case 'diario': return 'Diario de Campo';
      case 'dashboard': return 'Visión General';
      case 'terrenos': return 'Gestión de Terrenos';
      case 'cosechas': return 'Registro de Cosechas';
      case 'temporadas': return 'Temporadas';
      case 'trabajadores': return 'Trabajadores';
      case 'compras': return 'Compras y Suministros';
      case 'ajustes': return 'Ajustes del Workspace';
      default: return 'Terrenario';
    }
  };

  return (
    <header className="bg-[#fcf9f4]/80 backdrop-blur-md border-b border-[#e5e2dd] sticky top-0 z-20 px-4 md:px-8 py-3.5 flex items-center justify-between">
      <div className="flex items-center gap-3">
        <button
          onClick={onOpenMobileMenu}
          className="md:hidden p-2 rounded-lg text-[#45483c] hover:bg-[#f0ede8] transition-colors"
          aria-label="Abrir menú"
        >
          <span className="material-symbols-outlined">menu</span>
        </button>

        <div>
          <div className="flex items-center gap-2">
            <span className="inline-flex items-center gap-1 text-xs px-2.5 py-0.5 rounded-full bg-[#c9f16f] text-[#33450d] font-semibold border border-[#aed456]">
              <span className="w-1.5 h-1.5 rounded-full bg-[#33450d] animate-pulse"></span>
              {workspace.temporadaActiva}
            </span>
            <span className="hidden sm:inline text-xs text-[#76786b]">• {workspace.nombreWorkspace}</span>
          </div>
          <h2 className="font-headline font-bold text-lg md:text-xl text-[#1c1c19] tracking-tight">
            {getTitle()}
          </h2>
        </div>
      </div>

      <div className="flex items-center gap-2 md:gap-3">
        {/* Quick action: landing preview */}
        <button
          onClick={() => onSelectView('landing')}
          className="hidden sm:flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold text-[#45483c] bg-[#f0ede8] hover:bg-[#ebe8e3] transition-colors"
          title="Ver Landing Page"
        >
          <span className="material-symbols-outlined text-sm">open_in_new</span>
          <span>Ver Landing</span>
        </button>

        {/* Global Add Activity Button */}
        <button
          onClick={onOpenNewActivityModal}
          className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs md:text-sm font-semibold shadow-xs transition-colors"
        >
          <span className="material-symbols-outlined text-lg">add</span>
          <span className="hidden sm:inline">Nuevo Registro</span>
          <span className="sm:hidden">Nuevo</span>
        </button>
      </div>
    </header>
  );
};
