import React, { useState } from 'react';
import { HarvestRecord, Terreno } from '../types';

interface CosechasViewProps {
  harvests: HarvestRecord[];
  terrenos: Terreno[];
  onOpenNewHarvestModal: () => void;
  onDeleteHarvest: (id: string) => void;
}

export const CosechasView: React.FC<CosechasViewProps> = ({
  harvests,
  terrenos,
  onOpenNewHarvestModal,
  onDeleteHarvest
}) => {
  const [selectedTerreno, setSelectedTerreno] = useState('todos');

  const filteredHarvests = harvests.filter((h) => {
    if (selectedTerreno !== 'todos' && h.terrenoId !== selectedTerreno) return false;
    return true;
  });

  const totalKg = filteredHarvests.reduce((acc, h) => acc + h.kgs, 0);
  const avgRendimiento = (
    filteredHarvests.reduce((acc, h) => acc + h.rendimientoPct, 0) / (filteredHarvests.length || 1)
  ).toFixed(1);

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Registro de Cosechas</h2>
          <p className="text-xs text-[#76786b]">Lotes recolectados, rendimientos grasos y entregas a almazaras.</p>
        </div>

        <button
          onClick={onOpenNewHarvestModal}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors"
        >
          <span className="material-symbols-outlined text-lg">add</span>
          <span>Registrar Cosecha</span>
        </button>
      </div>

      {/* KPI Stats summary */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] space-y-1">
          <p className="text-[11px] font-bold text-[#76786b] uppercase">Total Recolectado</p>
          <p className="font-headline font-extrabold text-2xl text-[#1c1c19]">
            {totalKg.toLocaleString()} <span className="text-sm font-normal text-[#76786b]">Kg</span>
          </p>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] space-y-1">
          <p className="text-[11px] font-bold text-[#76786b] uppercase">Rendimiento Medio</p>
          <p className="font-headline font-extrabold text-2xl text-[#33450d]">
            {avgRendimiento}%
          </p>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-[#e5e2dd] space-y-1">
          <p className="text-[11px] font-bold text-[#76786b] uppercase">Lotes Entregados</p>
          <p className="font-headline font-extrabold text-2xl text-[#1c1c19]">
            {filteredHarvests.length} <span className="text-sm font-normal text-[#76786b]">partidas</span>
          </p>
        </div>
      </div>

      {/* Filter bar */}
      <div className="bg-white p-3 rounded-2xl border border-[#e5e2dd] flex items-center justify-between gap-4">
        <div className="flex items-center gap-2">
          <span className="text-xs font-bold text-[#45483c]">Filtrar Terreno:</span>
          <select
            value={selectedTerreno}
            onChange={(e) => setSelectedTerreno(e.target.value)}
            className="px-3 py-1.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19]"
          >
            <option value="todos">Todos los terrenos</option>
            {terrenos.map((t) => (
              <option key={t.id} value={t.id}>{t.nombre}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Table of Harvests */}
      <div className="bg-white rounded-2xl border border-[#e5e2dd] shadow-2xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-[#1c1c19]">
            <thead className="bg-[#f6f3ee] border-b border-[#e5e2dd] text-[11px] font-bold uppercase tracking-wider text-[#45483c]">
              <tr>
                <th className="px-5 py-3.5">Fecha</th>
                <th className="px-5 py-3.5">Terreno</th>
                <th className="px-5 py-3.5">Producto</th>
                <th className="px-5 py-3.5 text-right">Kilos (Kg)</th>
                <th className="px-5 py-3.5 text-right">Rendimiento (%)</th>
                <th className="px-5 py-3.5">Destino</th>
                <th className="px-5 py-3.5 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#f0ede8]">
              {filteredHarvests.map((h) => (
                <tr key={h.id} className="hover:bg-[#fcf9f4] transition-colors">
                  <td className="px-5 py-4 font-medium text-[#76786b]">{h.fecha}</td>
                  <td className="px-5 py-4 font-bold text-[#33450d]">{h.terrenoNombre}</td>
                  <td className="px-5 py-4 font-semibold">{h.producto}</td>
                  <td className="px-5 py-4 text-right font-extrabold text-[#1c1c19]">
                    {h.kgs.toLocaleString()} Kg
                  </td>
                  <td className="px-5 py-4 text-right">
                    <span className="inline-block px-2.5 py-0.5 rounded-full bg-[#c9f16f] text-[#33450d] font-bold">
                      {h.rendimientoPct}%
                    </span>
                  </td>
                  <td className="px-5 py-4">
                    <div>
                      <span className="font-semibold text-[#1c1c19]">{h.destino}</span>
                      {h.almazaraNombre && (
                        <p className="text-[11px] text-[#76786b]">{h.almazaraNombre}</p>
                      )}
                    </div>
                  </td>
                  <td className="px-5 py-4 text-right">
                    <button
                      onClick={() => onDeleteHarvest(h.id)}
                      className="p-1 rounded-lg text-[#76786b] hover:text-[#ba1a1a] hover:bg-[#ffdad6]"
                      title="Borrar partida"
                    >
                      <span className="material-symbols-outlined text-base">delete</span>
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
