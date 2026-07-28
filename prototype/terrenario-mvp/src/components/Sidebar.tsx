import React from 'react';
import { ViewMode, WorkspaceConfig } from '../types';

interface SidebarProps {
  currentView: ViewMode;
  onSelectView: (view: ViewMode) => void;
  workspace: WorkspaceConfig;
  isOpenMobile: boolean;
  onCloseMobile: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({
  currentView,
  onSelectView,
  workspace,
  isOpenMobile,
  onCloseMobile
}) => {
  const navItems: { view: ViewMode; label: string; icon: string }[] = [
    { view: 'diario', label: 'Diario de Campo', icon: 'event_note' },
    { view: 'dashboard', label: 'Visión General', icon: 'monitoring' },
    { view: 'terrenos', label: 'Terrenos', icon: 'map' },
    { view: 'cosechas', label: 'Cosechas', icon: 'agriculture' },
    { view: 'temporadas', label: 'Temporadas', icon: 'calendar_today' },
    { view: 'trabajadores', label: 'Trabajadores', icon: 'group' },
    { view: 'compras', label: 'Compras', icon: 'receipt_long' },
    { view: 'ajustes', label: 'Ajustes', icon: 'settings' }
  ];

  const sidebarContent = (
    <aside className="w-64 bg-[#f6f3ee] border-r border-[#e5e2dd] h-full flex flex-col justify-between select-none">
      <div className="p-5">
        {/* Brand Header */}
        <div 
          onClick={() => onSelectView('landing')}
          className="flex items-center gap-3 cursor-pointer group mb-6"
        >
          <div className="w-10 h-10 rounded-xl bg-[#33450d] text-white flex items-center justify-center font-bold shadow-md group-hover:bg-[#4a5d23] transition-colors">
            <span className="material-symbols-outlined text-2xl fill">eco</span>
          </div>
          <div>
            <h1 className="font-headline font-bold text-xl text-[#33450d] tracking-tight">Terrenario</h1>
            <p className="text-xs text-[#76786b] font-medium">Gestión Agrícola</p>
          </div>
        </div>

        {/* Workspace selector badge */}
        <div className="mb-6 bg-white rounded-xl p-3 border border-[#e5e2dd] shadow-xs flex items-center justify-between">
          <div className="flex items-center gap-2.5 min-w-0">
            <span className="material-symbols-outlined text-[#33450d]">landscape</span>
            <div className="min-w-0">
              <p className="text-xs font-semibold text-[#1c1c19] truncate">{workspace.nombreWorkspace}</p>
              <p className="text-[11px] text-[#76786b]">{workspace.temporadaActiva}</p>
            </div>
          </div>
          <span className="material-symbols-outlined text-[#76786b] text-sm">unfold_more</span>
        </div>

        {/* Navigation links */}
        <nav className="space-y-1">
          {navItems.map((item) => {
            const isActive = currentView === item.view;
            return (
              <button
                key={item.view}
                onClick={() => {
                  onSelectView(item.view);
                  onCloseMobile();
                }}
                className={`w-full flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
                  isActive
                    ? 'bg-[#33450d] text-white font-semibold shadow-xs'
                    : 'text-[#45483c] hover:bg-[#ebe8e3] hover:text-[#1c1c19]'
                }`}
              >
                <span className={`material-symbols-outlined text-xl ${isActive ? 'fill' : ''}`}>
                  {item.icon}
                </span>
                <span>{item.label}</span>
              </button>
            );
          })}
        </nav>
      </div>

      {/* User profile footer */}
      <div className="p-4 border-t border-[#e5e2dd] bg-[#f0ede8]">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3 min-w-0">
            <img 
              src={workspace.userAvatar} 
              alt={workspace.userName}
              className="w-9 h-9 rounded-full object-cover border border-[#c6c8b8]"
              referrerPolicy="no-referrer"
            />
            <div className="min-w-0">
              <p className="text-xs font-semibold text-[#1c1c19] truncate">{workspace.userName}</p>
              <p className="text-[11px] text-[#76786b] truncate">{workspace.userEmail}</p>
            </div>
          </div>
          <button
            onClick={() => onSelectView('login')}
            title="Cerrar sesión"
            className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#e5e2dd] hover:text-[#ba1a1a] transition-colors"
          >
            <span className="material-symbols-outlined text-lg">logout</span>
          </button>
        </div>
      </div>
    </aside>
  );

  return (
    <>
      {/* Desktop Sidebar */}
      <div className="hidden md:block h-screen sticky top-0 z-30">
        {sidebarContent}
      </div>

      {/* Mobile Drawer */}
      {isOpenMobile && (
        <div className="fixed inset-0 z-50 md:hidden flex">
          <div 
            className="fixed inset-0 bg-black/40 backdrop-blur-xs transition-opacity"
            onClick={onCloseMobile}
          />
          <div className="relative z-10 h-full max-w-xs w-full shadow-2xl">
            {sidebarContent}
          </div>
        </div>
      )}
    </>
  );
};
