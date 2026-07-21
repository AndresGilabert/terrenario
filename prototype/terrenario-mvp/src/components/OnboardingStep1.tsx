import React, { useState } from 'react';
import { ViewMode, WorkspaceConfig } from '../types';

interface OnboardingStep1Props {
  workspace: WorkspaceConfig;
  onUpdateWorkspace: (updated: Partial<WorkspaceConfig>) => void;
  onNavigate: (view: ViewMode) => void;
}

export const OnboardingStep1: React.FC<OnboardingStep1Props> = ({
  workspace,
  onUpdateWorkspace,
  onNavigate
}) => {
  const [nombre, setNombre] = useState(workspace.nombreWorkspace || 'Finca El Olivar');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (nombre.trim()) {
      onUpdateWorkspace({ nombreWorkspace: nombre.trim() });
      onNavigate('onboarding_step2');
    }
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl space-y-6">
        {/* Progress header */}
        <div className="flex items-center justify-between border-b border-[#e5e2dd] pb-4">
          <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
            <span className="material-symbols-outlined text-base">eco</span>
            <span>Paso 1 de 3</span>
          </div>
          <div className="w-24 bg-[#e5e2dd] h-1.5 rounded-full overflow-hidden">
            <div className="bg-[#33450d] h-full w-1/3"></div>
          </div>
        </div>

        {/* Title */}
        <div className="space-y-1.5">
          <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
            Demos nombre a tu espacio de trabajo
          </h1>
          <p className="text-sm text-[#45483c]">
            Un Workspace representa tu finca o explotación agrícola. Todos tus terrenos, tareas y cosechas vivirán aquí.
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="space-y-2">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Nombre del Workspace
            </label>
            <div className="relative">
              <span className="material-symbols-outlined absolute left-3.5 top-3 text-[#76786b]">landscape</span>
              <input
                type="text"
                value={nombre}
                onChange={(e) => setNombre(e.target.value)}
                placeholder="ej. Finca El Olivar, AgroSoto, etc."
                required
                className="w-full pl-11 pr-4 py-3 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white transition-all font-medium"
              />
            </div>
          </div>

          {/* Graphic Banner */}
          <div className="bg-[#f0ede8] rounded-xl p-4 border border-[#e5e2dd] flex items-center gap-4">
            <img
              src="https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=300&auto=format&fit=crop&q=80"
              alt="Finca ilustrativa"
              className="w-20 h-20 rounded-lg object-cover shadow-xs"
            />
            <div className="space-y-1">
              <p className="text-xs font-bold text-[#1c1c19]">Workspace Personalizado</p>
              <p className="text-xs text-[#76786b] leading-tight">
                Podrás invitar a tus trabajadores y colaboradores a este espacio más adelante.
              </p>
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex items-center justify-between pt-2">
            <button
              type="button"
              onClick={() => onNavigate('landing')}
              className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19]"
            >
              Cancelar
            </button>
            <button
              type="submit"
              className="flex items-center gap-2 px-6 py-3 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors"
            >
              <span>Siguiente</span>
              <span className="material-symbols-outlined text-sm">arrow_forward</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
