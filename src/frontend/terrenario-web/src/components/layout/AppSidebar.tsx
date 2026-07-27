import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { WorkspaceSwitcher } from '../workspace/WorkspaceSwitcher';

/**
 * Barra lateral del área operativa (shell del prototipo). La navegación lista los módulos previstos
 * del producto; los que aún no están entregados (épicas MVP-002…004) se muestran deshabilitados con
 * la etiqueta "Pronto", de forma honesta y sin enlaces rotos, para que se enciendan al implementarse.
 */

interface NavItem {
  label: string;
  icon: string;
  /** Ruta si el módulo ya está disponible; ausente = pendiente de una épica posterior. */
  to?: string;
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Diario de Campo', icon: 'event_note' },
  { label: 'Visión General', icon: 'monitoring' },
  { label: 'Terrenos', icon: 'map', to: '/app/terrenos' },
  { label: 'Cosechas', icon: 'agriculture' },
  { label: 'Temporadas', icon: 'calendar_today' },
  { label: 'Trabajadores', icon: 'group' },
  { label: 'Compras', icon: 'receipt_long' },
  { label: 'Ajustes', icon: 'settings' },
];

function initials(name: string | undefined): string {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((p) => p.charAt(0).toUpperCase()).join('') || '?';
}

interface AppSidebarProps {
  /** Se invoca al navegar, para cerrar el drawer en móvil. */
  onNavigate?: () => void;
}

export const AppSidebar: React.FC<AppSidebarProps> = ({ onNavigate }) => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  return (
    <aside className="w-64 bg-[#f6f3ee] border-r border-[#e5e2dd] h-full flex flex-col justify-between select-none">
      <div className="p-5">
        {/* Marca */}
        <button
          type="button"
          onClick={() => {
            navigate('/app');
            onNavigate?.();
          }}
          className="flex items-center gap-3 group mb-6 w-full text-left"
        >
          <div className="w-10 h-10 rounded-xl bg-[#33450d] text-white flex items-center justify-center shadow-md group-hover:bg-[#4a5d23] transition-colors">
            <span className="material-symbols-outlined fill text-2xl" aria-hidden="true">eco</span>
          </div>
          <div>
            <h1 className="font-headline font-bold text-xl text-[#33450d] tracking-tight">Terrenario</h1>
            <p className="text-xs text-[#76786b] font-medium">Gestión Agrícola</p>
          </div>
        </button>

        {/* Selector de Workspace activo (MVP-104) */}
        <div className="mb-6">
          <WorkspaceSwitcher />
        </div>

        {/* Navegación: módulos del producto (los pendientes, deshabilitados con "Pronto") */}
        <nav className="space-y-1" aria-label="Navegación principal">
          {NAV_ITEMS.map((item) => {
            const available = Boolean(item.to);
            return (
              <button
                key={item.label}
                type="button"
                disabled={!available}
                onClick={() => {
                  if (item.to) {
                    navigate(item.to);
                    onNavigate?.();
                  }
                }}
                title={available ? undefined : 'Disponible próximamente'}
                className={`w-full flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
                  available
                    ? 'text-[#45483c] hover:bg-[#ebe8e3] hover:text-[#1c1c19]'
                    : 'text-[#a2a496] cursor-not-allowed'
                }`}
              >
                <span className="material-symbols-outlined text-xl" aria-hidden="true">{item.icon}</span>
                <span className="flex-1 text-left">{item.label}</span>
                {!available && (
                  <span className="text-[10px] font-semibold uppercase tracking-wide text-[#a2a496] border border-[#dcd9d2] rounded-full px-1.5 py-0.5">
                    Pronto
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Footer de usuario */}
      <div className="p-4 border-t border-[#e5e2dd] bg-[#f0ede8]">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-3 min-w-0">
            <div className="w-9 h-9 rounded-full bg-[#33450d] text-white flex items-center justify-center text-xs font-bold shrink-0">
              {initials(user?.displayName)}
            </div>
            <div className="min-w-0">
              <p className="text-xs font-semibold text-[#1c1c19] truncate">
                {user?.displayName ?? 'Usuario'}
              </p>
              <p className="text-[11px] text-[#76786b] truncate">Sesión activa</p>
            </div>
          </div>
          <button
            type="button"
            onClick={() => void logout()}
            title="Cerrar sesión"
            aria-label="Cerrar sesión"
            className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#e5e2dd] hover:text-[#ba1a1a] transition-colors shrink-0"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">logout</span>
          </button>
        </div>
      </div>
    </aside>
  );
};
