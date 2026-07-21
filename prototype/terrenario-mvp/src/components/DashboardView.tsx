import React, { useState } from 'react';
import { Terreno, HarvestRecord } from '../types';

interface DashboardViewProps {
  terrenos: Terreno[];
  harvests: HarvestRecord[];
}

export const DashboardView: React.FC<DashboardViewProps> = ({ terrenos, harvests }) => {
  const [selectedSeason, setSelectedSeason] = useState('Campaña 2024');
  const [selectedTerreno, setSelectedTerreno] = useState('todos');

  // Compute stats
  const totalKgs = harvests.reduce((acc, h) => acc + h.kgs, 45000); // 45k base + dynamic
  const totalLitros = Math.round(totalKgs * 0.20); // ~20% oil ratio
  const avgRendimiento = (harvests.reduce((acc, h) => acc + h.rendimientoPct, 0) / (harvests.length || 1)).toFixed(1);

  const terrenosProduction = [
    { nombre: 'Loma del Sol', kgs: 18500, pct: 41 },
    { nombre: 'Valle Bajo', kgs: 12000, pct: 27 },
    { nombre: 'El Rincón', kgs: 9500, pct: 21 },
    { nombre: 'La Vega', kgs: 5000, pct: 11 },
  ];

  const destinationData = [
    { destino: 'Almazara Regional', kgs: 38000, pct: 84, color: 'bg-[#33450d]' },
    { destino: 'Consumo Propio', kgs: 5000, pct: 11, color: 'bg-[#c9f16f]' },
    { destino: 'Sin destino', kgs: 2000, pct: 5, color: 'bg-[#e5e2dd]' },
  ];

  const yieldTrend = [
    { mes: 'Oct', pct: 16.0 },
    { mes: 'Nov', pct: 18.5 },
    { mes: 'Dic', pct: 17.5 },
    { mes: 'Ene', pct: 20.0 },
    { mes: 'Feb', pct: 21.5 },
  ];

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Top Banner & Filters */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Visión General</h2>
          <p className="text-xs text-[#76786b]">Monitoreo de rendimiento, kilos recolectados y proyecciones de aceite.</p>
        </div>

        <div className="flex items-center gap-2">
          <select
            value={selectedSeason}
            onChange={(e) => setSelectedSeason(e.target.value)}
            className="px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-semibold text-[#1c1c19] focus:outline-none"
          >
            <option value="Campaña 2024">Campaña 2024 (Activa)</option>
            <option value="Campaña 2023">Campaña 2023</option>
            <option value="Campaña 2022">Campaña 2022</option>
          </select>

          <select
            value={selectedTerreno}
            onChange={(e) => setSelectedTerreno(e.target.value)}
            className="px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-semibold text-[#1c1c19] focus:outline-none"
          >
            <option value="todos">Todos los Terrenos</option>
            {terrenos.map((t) => (
              <option key={t.id} value={t.id}>{t.nombre}</option>
            ))}
          </select>
        </div>
      </div>

      {/* Metric Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Card 1: Kgs Totales */}
        <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-[#76786b]">Kg Recolectados</span>
            <div className="w-8 h-8 rounded-lg bg-[#f0ede8] text-[#33450d] flex items-center justify-center">
              <span className="material-symbols-outlined text-lg">scale</span>
            </div>
          </div>
          <div>
            <p className="font-headline font-extrabold text-2xl sm:text-3xl text-[#1c1c19]">
              {totalKgs.toLocaleString()} <span className="text-sm font-semibold text-[#76786b]">Kg</span>
            </p>
            <p className="text-xs font-semibold text-[#4c6700] mt-1 flex items-center gap-1">
              <span className="material-symbols-outlined text-sm">trending_up</span>
              <span>+5.2% vs temporada anterior</span>
            </p>
          </div>
        </div>

        {/* Card 2: Litros Estimados */}
        <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-[#76786b]">Aceite Estimado</span>
            <div className="w-8 h-8 rounded-lg bg-[#c9f16f] text-[#33450d] flex items-center justify-center">
              <span className="material-symbols-outlined text-lg">water_drop</span>
            </div>
          </div>
          <div>
            <p className="font-headline font-extrabold text-2xl sm:text-3xl text-[#1c1c19]">
              {totalLitros.toLocaleString()} <span className="text-sm font-semibold text-[#76786b]">Litros</span>
            </p>
            <p className="text-xs font-semibold text-[#33450d] mt-1">
              AOVE (Virgen Extra)
            </p>
          </div>
        </div>

        {/* Card 3: Rendimiento Medio */}
        <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-[#76786b]">Rendimiento Medio</span>
            <div className="w-8 h-8 rounded-lg bg-[#f0ede8] text-[#4a5d23] flex items-center justify-center">
              <span className="material-symbols-outlined text-lg">percent</span>
            </div>
          </div>
          <div>
            <p className="font-headline font-extrabold text-2xl sm:text-3xl text-[#1c1c19]">
              {avgRendimiento}%
            </p>
            {/* Visual progress bar */}
            <div className="w-full bg-[#e5e2dd] h-2 rounded-full overflow-hidden mt-2">
              <div className="bg-[#33450d] h-full" style={{ width: `${avgRendimiento}%` }}></div>
            </div>
          </div>
        </div>

        {/* Card 4: Producción por Árbol */}
        <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-[#76786b]">Producción por Árbol</span>
            <div className="w-8 h-8 rounded-lg bg-[#f0ede8] text-[#5a3811] flex items-center justify-center">
              <span className="material-symbols-outlined text-lg">park</span>
            </div>
          </div>
          <div>
            <p className="font-headline font-extrabold text-2xl sm:text-3xl text-[#1c1c19]">
              15.2 <span className="text-sm font-semibold text-[#76786b]">Kg/olivo</span>
            </p>
            <p className="text-xs font-semibold text-[#33450d] mt-1">
              Rango óptimo alcanzado
            </p>
          </div>
        </div>
      </div>

      {/* Main Charts Section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left: Bar Chart - Producción por Terreno */}
        <div className="lg:col-span-7 bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-headline font-bold text-base text-[#1c1c19]">Producción por Terreno</h3>
              <p className="text-xs text-[#76786b]">Desglose de kilos cosechados por parcela</p>
            </div>
            <span className="text-xs font-bold text-[#33450d] bg-[#f0ede8] px-2.5 py-1 rounded-md">
              4 Parteras
            </span>
          </div>

          <div className="space-y-4 pt-2">
            {terrenosProduction.map((item) => (
              <div key={item.nombre} className="space-y-1.5">
                <div className="flex items-center justify-between text-xs font-semibold">
                  <span className="text-[#1c1c19]">{item.nombre}</span>
                  <span className="text-[#33450d] font-bold">{item.kgs.toLocaleString()} Kg ({item.pct}%)</span>
                </div>
                <div className="w-full bg-[#f0ede8] h-3.5 rounded-xl overflow-hidden p-0.5">
                  <div
                    className="bg-[#33450d] h-full rounded-lg transition-all duration-500"
                    style={{ width: `${item.pct}%` }}
                  ></div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Right: Destination Breakdown */}
        <div className="lg:col-span-5 bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-6 flex flex-col justify-between">
          <div>
            <h3 className="font-headline font-bold text-base text-[#1c1c19]">Distribución por Destino</h3>
            <p className="text-xs text-[#76786b]">Destino de la aceituna recolectada</p>
          </div>

          <div className="space-y-4">
            {/* Visual stacked bar */}
            <div className="w-full h-4 rounded-xl overflow-hidden flex shadow-inner">
              {destinationData.map((d) => (
                <div
                  key={d.destino}
                  className={`${d.color} h-full transition-all`}
                  style={{ width: `${d.pct}%` }}
                  title={`${d.destino}: ${d.pct}%`}
                />
              ))}
            </div>

            {/* List */}
            <div className="space-y-3 pt-2">
              {destinationData.map((d) => (
                <div key={d.destino} className="flex items-center justify-between text-xs p-2.5 bg-[#f6f3ee] rounded-xl border border-[#e5e2dd]">
                  <div className="flex items-center gap-2.5">
                    <span className={`w-3 h-3 rounded-full ${d.color} border border-black/10`} />
                    <span className="font-semibold text-[#1c1c19]">{d.destino}</span>
                  </div>
                  <div className="text-right">
                    <p className="font-bold text-[#1c1c19]">{d.kgs.toLocaleString()} Kg</p>
                    <p className="text-[11px] text-[#76786b]">{d.pct}% del total</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="p-3 bg-[#f0ede8] rounded-xl text-xs text-[#45483c] flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d]">info</span>
            <span>84% entregado a Almazara Regional para la molienda de noviembre.</span>
          </div>
        </div>
      </div>

      {/* Yield Trend Line */}
      <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="font-headline font-bold text-base text-[#1c1c19]">Evolución del Rendimiento Graso (%)</h3>
            <p className="text-xs text-[#76786b]">Evolución mensual durante la campaña</p>
          </div>
          <span className="text-xs font-bold text-[#4c6700] bg-[#c9f16f] px-2.5 py-1 rounded-full">
            Máximo: 21.5%
          </span>
        </div>

        {/* Trend Bar Chart Visualization */}
        <div className="h-44 flex items-end justify-between gap-4 pt-6 border-b border-[#e5e2dd] px-4">
          {yieldTrend.map((t) => {
            const barHeight = ((t.pct - 10) / 15) * 100; // normalized
            return (
              <div key={t.mes} className="flex-1 flex flex-col items-center gap-2 h-full justify-end group">
                <span className="text-xs font-bold text-[#33450d] group-hover:scale-110 transition-transform">
                  {t.pct}%
                </span>
                <div className="w-full max-w-[48px] bg-[#f0ede8] rounded-t-xl overflow-hidden h-full flex items-end p-1">
                  <div
                    className="w-full bg-[#33450d] rounded-t-lg transition-all duration-500 group-hover:bg-[#4a5d23]"
                    style={{ height: `${barHeight}%` }}
                  ></div>
                </div>
                <span className="text-xs font-semibold text-[#76786b]">{t.mes}</span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
