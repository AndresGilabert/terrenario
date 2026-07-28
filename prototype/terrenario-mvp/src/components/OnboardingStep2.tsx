import React, { useState } from 'react';
import { ViewMode, WorkspaceConfig } from '../types';

interface OnboardingStep2Props {
  workspace: WorkspaceConfig;
  onUpdateWorkspace: (updated: Partial<WorkspaceConfig>) => void;
  onNavigate: (view: ViewMode) => void;
}

export const OnboardingStep2: React.FC<OnboardingStep2Props> = ({
  workspace,
  onUpdateWorkspace,
  onNavigate
}) => {
  const [temporada, setTemporada] = useState(workspace.temporadaActiva || 'Campaña Oliva 2024');
  const [fechaInicio, setFechaInicio] = useState(workspace.fechaInicioTemporada || '2024-01-15');
  const [fechaFin, setFechaFin] = useState(workspace.fechaFinEstimada || '2024-12-30');
  const [activa, setActiva] = useState(true);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onUpdateWorkspace({
      temporadaActiva: temporada,
      fechaInicioTemporada: fechaInicio,
      fechaFinEstimada: fechaFin
    });
    onNavigate('diario');
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center p-4 lg:p-8">
      <div className="w-full max-w-4xl bg-white rounded-2xl border border-[#e5e2dd] shadow-2xl overflow-hidden grid grid-cols-1 md:grid-cols-12">
        {/* Left Side Image Banner (Desktop) */}
        <div className="hidden md:block md:col-span-5 relative bg-[#33450d] text-white p-8 flex flex-col justify-between overflow-hidden">
          <img
            src="https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=800&auto=format&fit=crop&q=80"
            alt="Olivar al amanecer"
            className="absolute inset-0 w-full h-full object-cover opacity-40 mix-blend-overlay"
          />
          <div className="relative z-10 space-y-3">
            <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur-md flex items-center justify-center font-bold">
              <span className="material-symbols-outlined text-white text-2xl fill">eco</span>
            </div>
            <h2 className="font-headline font-bold text-2xl tracking-tight">Terrenario</h2>
            <p className="text-xs text-stone-200">Cultivando el futuro con precisión y tecnología sencilla.</p>
          </div>

          <div className="relative z-10 bg-white/10 backdrop-blur-md p-4 rounded-xl border border-white/20 text-xs space-y-1">
            <p className="font-bold text-[#c9f16f]">¿Por qué usar temporadas?</p>
            <p className="text-stone-200">
              Te permite comparar cosechas y gastos año tras año de forma estructurada.
            </p>
          </div>
        </div>

        {/* Right Side Form */}
        <div className="md:col-span-7 p-6 sm:p-8 space-y-6">
          {/* Header */}
          <div className="space-y-2 border-b border-[#e5e2dd] pb-4">
            <div className="flex items-center justify-between text-xs font-bold text-[#33450d]">
              <span>Paso 2 de 3 — Configuración de Campaña</span>
              <span className="text-[#76786b]">66% Completado</span>
            </div>
            <div className="w-full bg-[#e5e2dd] h-1.5 rounded-full overflow-hidden">
              <div className="bg-[#33450d] h-full w-2/3 transition-all duration-300"></div>
            </div>
          </div>

          <div className="space-y-1">
            <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
              Configura tu temporada actual
            </h1>
            <p className="text-xs sm:text-sm text-[#45483c]">
              Las actividades y cosechas se agrupan por temporadas. Define el marco temporal de tu próxima campaña.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Nombre de la temporada
              </label>
              <input
                type="text"
                value={temporada}
                onChange={(e) => setTemporada(e.target.value)}
                placeholder="ej. Campaña Oliva 2024"
                required
                className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white"
              />
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Fecha de inicio
                </label>
                <input
                  type="date"
                  value={fechaInicio}
                  onChange={(e) => setFechaInicio(e.target.value)}
                  required
                  className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white"
                />
              </div>

              <div className="space-y-1.5">
                <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Fecha fin (Estimada)
                </label>
                <input
                  type="date"
                  value={fechaFin}
                  onChange={(e) => setFechaFin(e.target.value)}
                  required
                  className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white"
                />
              </div>
            </div>

            <div className="pt-2">
              <label className="flex items-center gap-3 p-3 bg-[#f0ede8] rounded-xl border border-[#e5e2dd] cursor-pointer">
                <input
                  type="checkbox"
                  checked={activa}
                  onChange={(e) => setActiva(e.target.checked)}
                  className="w-4 h-4 text-[#33450d] rounded border-[#c6c8b8] focus:ring-[#33450d]"
                />
                <span className="text-xs font-semibold text-[#1c1c19]">
                  Establecer como la temporada activa por defecto
                </span>
              </label>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between pt-4 border-t border-[#e5e2dd]">
              <button
                type="button"
                onClick={() => onNavigate('onboarding_step1')}
                className="flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl"
              >
                <span className="material-symbols-outlined text-sm">arrow_back</span>
                <span>Anterior</span>
              </button>

              <button
                type="submit"
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors"
              >
                <span>Finalizar y Entrar</span>
                <span className="material-symbols-outlined text-sm">check</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
