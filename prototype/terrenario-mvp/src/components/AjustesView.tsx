import React, { useState } from 'react';
import { WorkspaceConfig, ViewMode } from '../types';

interface AjustesViewProps {
  workspace: WorkspaceConfig;
  onUpdateWorkspace: (updated: Partial<WorkspaceConfig>) => void;
  onNavigate: (view: ViewMode) => void;
}

export const AjustesView: React.FC<AjustesViewProps> = ({
  workspace,
  onUpdateWorkspace,
  onNavigate
}) => {
  const [nombreWs, setNombreWs] = useState(workspace.nombreWorkspace);
  const [userName, setUserName] = useState(workspace.userName);
  const [userEmail, setUserEmail] = useState(workspace.userEmail);
  const [savedSuccess, setSavedSuccess] = useState(false);

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    onUpdateWorkspace({
      nombreWorkspace: nombreWs,
      userName,
      userEmail
    });
    setSavedSuccess(true);
    setTimeout(() => setSavedSuccess(false), 3000);
  };

  return (
    <div className="space-y-6 max-w-4xl mx-auto pb-12">
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs">
        <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Ajustes de Workspace</h2>
        <p className="text-xs text-[#76786b]">Configura la información general de la explotación y perfil del titular.</p>
      </div>

      {savedSuccess && (
        <div className="p-4 bg-[#c9f16f] text-[#33450d] rounded-2xl font-bold text-xs flex items-center gap-2 shadow-xs">
          <span className="material-symbols-outlined text-base">check_circle</span>
          <span>¡Cambios guardados correctamente en tu Workspace!</span>
        </div>
      )}

      <form onSubmit={handleSave} className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-6">
        <div className="space-y-4">
          <h3 className="font-headline font-bold text-base text-[#1c1c19] border-b border-[#f0ede8] pb-2">
            Datos del Workspace / Finca
          </h3>

          <div className="space-y-1.5">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Nombre del Workspace
            </label>
            <input
              type="text"
              value={nombreWs}
              onChange={(e) => setNombreWs(e.target.value)}
              className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19]"
            />
          </div>
        </div>

        <div className="space-y-4 pt-2">
          <h3 className="font-headline font-bold text-base text-[#1c1c19] border-b border-[#f0ede8] pb-2">
            Perfil del Titular
          </h3>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Nombre y Apellidos
              </label>
              <input
                type="text"
                value={userName}
                onChange={(e) => setUserName(e.target.value)}
                className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19]"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Correo Electrónico (Google)
              </label>
              <input
                type="email"
                value={userEmail}
                onChange={(e) => setUserEmail(e.target.value)}
                className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19]"
              />
            </div>
          </div>
        </div>

        <div className="pt-4 border-t border-[#e5e2dd] flex items-center justify-between">
          <button
            type="button"
            onClick={() => onNavigate('landing')}
            className="text-xs font-semibold text-[#76786b] hover:underline"
          >
            Ver Landing Page Principal
          </button>

          <button
            type="submit"
            className="px-6 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl shadow-xs"
          >
            Guardar Cambios
          </button>
        </div>
      </form>
    </div>
  );
};
