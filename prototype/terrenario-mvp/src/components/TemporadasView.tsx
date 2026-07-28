import React, { useState } from 'react';
import { WorkspaceConfig } from '../types';

interface TemporadasViewProps {
  workspace: WorkspaceConfig;
  onSelectActiveSeason: (seasonName: string) => void;
}

export const TemporadasView: React.FC<TemporadasViewProps> = ({ workspace, onSelectActiveSeason }) => {
  const [temporadas, setTemporadas] = useState([
    { id: '1', nombre: 'Campaña 2024', inicio: '2024-01-15', fin: '2024-12-30', kgs: 45000, activa: true },
    { id: '2', nombre: 'Campaña 2023', inicio: '2023-01-10', fin: '2023-12-28', kgs: 42800, activa: false },
    { id: '3', nombre: 'Campaña 2022', inicio: '2022-01-12', fin: '2022-12-20', kgs: 39500, activa: false }
  ]);

  const [nuevaNombre, setNuevaNombre] = useState('');

  const handleCrear = (e: React.FormEvent) => {
    e.preventDefault();
    if (!nuevaNombre.trim()) return;
    const item = {
      id: Date.now().toString(),
      nombre: nuevaNombre.trim(),
      inicio: '2025-01-01',
      fin: '2025-12-31',
      kgs: 0,
      activa: false
    };
    setTemporadas([item, ...temporadas]);
    setNuevaNombre('');
  };

  const handleActivar = (nombre: string) => {
    setTemporadas(temporadas.map(t => ({ ...t, activa: t.nombre === nombre })));
    onSelectActiveSeason(nombre);
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Gestión de Temporadas y Campañas</h2>
          <p className="text-xs text-[#76786b]">Agrupa cosechas, labores y contabilidad por marcos temporales anuales.</p>
        </div>
      </div>

      {/* Form para nueva campaña */}
      <form onSubmit={handleCrear} className="bg-white p-4 rounded-2xl border border-[#e5e2dd] flex items-center gap-3">
        <input
          type="text"
          value={nuevaNombre}
          onChange={(e) => setNuevaNombre(e.target.value)}
          placeholder="Nombre de nueva temporada (ej. Campaña 2025)"
          className="flex-1 px-4 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19]"
        />
        <button
          type="submit"
          className="px-4 py-2 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl shadow-xs"
        >
          Crear Temporada
        </button>
      </form>

      {/* Temporadas List */}
      <div className="space-y-3">
        {temporadas.map((t) => (
          <div
            key={t.id}
            className={`bg-white p-5 rounded-2xl border flex items-center justify-between transition-all ${
              t.activa ? 'border-[#33450d] ring-2 ring-[#33450d]/20' : 'border-[#e5e2dd]'
            }`}
          >
            <div className="flex items-center gap-4">
              <div className={`w-10 h-10 rounded-xl flex items-center justify-center font-bold ${
                t.activa ? 'bg-[#33450d] text-white' : 'bg-[#f0ede8] text-[#76786b]'
              }`}>
                <span className="material-symbols-outlined text-xl">calendar_today</span>
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="font-headline font-bold text-base text-[#1c1c19]">{t.nombre}</h3>
                  {t.activa && (
                    <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-[#c9f16f] text-[#33450d]">
                      ACTIVA
                    </span>
                  )}
                </div>
                <p className="text-xs text-[#76786b]">{t.inicio} al {t.fin}</p>
              </div>
            </div>

            <div className="flex items-center gap-6">
              <div className="text-right hidden sm:block">
                <p className="text-xs font-extrabold text-[#1c1c19]">{t.kgs.toLocaleString()} Kg</p>
                <p className="text-[11px] text-[#76786b]">Producción total</p>
              </div>

              {!t.activa && (
                <button
                  onClick={() => handleActivar(t.nombre)}
                  className="px-3.5 py-1.5 rounded-xl bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d] text-xs font-bold"
                >
                  Activar esta Campaña
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
