import React from 'react';
import { NavLink, useNavigate } from 'react-router';
import { useAuth } from '../../contexts/AuthContext';
import { WorkspaceSwitcher } from '../workspace/WorkspaceSwitcher';

/**
 * Barra lateral del área operativa (shell del prototipo). Con los diez módulos del MVP encendidos, la
 * navegación deja de ser una lista plana (MVP-406, `P-025`): se agrupa por lo que hago a diario
 * (**Operativa**), los datos base (**Maestros**) y la **Configuración**. Cada entrada es un `NavLink`,
 * así que la sección activa queda marcada visualmente y con `aria-current` (`P-037`), accesible por
 * teclado y lector de pantalla.
 */

interface NavItem {
  label: string;
  icon: string;
  to: string;
}

interface NavSection {
  title: string;
  items: NavItem[];
}

const NAV_SECTIONS: NavSection[] = [
  {
    title: 'Operativa',
    items: [
      { label: 'Diario de Campo', icon: 'event_note', to: '/app/diario' },
      { label: 'Visión General', icon: 'monitoring', to: '/app/vision-general' },
      { label: 'Cosechas', icon: 'agriculture', to: '/app/cosechas' },
      { label: 'Compras', icon: 'receipt_long', to: '/app/compras' },
    ],
  },
  {
    title: 'Maestros',
    items: [
      { label: 'Terrenos', icon: 'map', to: '/app/terrenos' },
      { label: 'Temporadas', icon: 'calendar_today', to: '/app/temporadas' },
      { label: 'Trabajadores', icon: 'group', to: '/app/trabajadores' },
      { label: 'Tareas', icon: 'checklist', to: '/app/tareas' },
      { label: 'Miembros y accesos', icon: 'manage_accounts', to: '/app/miembros' },
    ],
  },
  {
    title: 'Configuración',
    items: [
      { label: 'Ajustes', icon: 'settings', to: '/app/ajustes' },
      // MVP-711 (CA-1) — La entrada al canal va **en la navegación** y no como un panel al final de
      // Ajustes. Ajustes termina en la zona de baja de cuenta, que está deliberadamente al final por
      // ser lo irreversible (MVP-505): colgar el canal ahí lo dejaría por debajo de lo más peligroso
      // de la aplicación, que es lo contrario de «entrada visible». Aquí se ve desde cualquier
      // pantalla, que es donde hace falta cuando algo acaba de fallar.
      { label: 'Sugerencias e incidencias', icon: 'feedback', to: '/app/feedback' },
    ],
  },
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

        {/* Navegación agrupada (MVP-406). NavLink marca la sección activa (aria-current) por sí solo. */}
        <nav className="space-y-5" aria-label="Navegación principal">
          {NAV_SECTIONS.map((section) => (
            <div key={section.title} className="space-y-1">
              <p className="px-3.5 text-[11px] font-bold uppercase tracking-wider text-[#a2a496]">
                {section.title}
              </p>
              {section.items.map((item) => (
                <NavLink
                  key={item.label}
                  to={item.to}
                  onClick={() => onNavigate?.()}
                  className={({ isActive }) =>
                    `w-full flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition-all duration-150 ${
                      isActive
                        ? 'bg-[#33450d] text-white shadow-sm'
                        : 'text-[#45483c] hover:bg-[#ebe8e3] hover:text-[#1c1c19]'
                    }`
                  }
                >
                  <span className="material-symbols-outlined text-xl" aria-hidden="true">{item.icon}</span>
                  <span className="flex-1 text-left">{item.label}</span>
                </NavLink>
              ))}
            </div>
          ))}
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
