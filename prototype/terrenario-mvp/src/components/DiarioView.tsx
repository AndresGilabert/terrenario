import React, { useState } from 'react';
import { DiarioEntry, Terreno, Worker } from '../types';

interface DiarioViewProps {
  entries: DiarioEntry[];
  terrenos: Terreno[];
  workers: Worker[];
  onOpenNewModal: () => void;
  onToggleComplete: (id: string) => void;
  onDeleteEntry: (id: string) => void;
}

export const DiarioView: React.FC<DiarioViewProps> = ({
  entries,
  terrenos,
  workers,
  onOpenNewModal,
  onToggleComplete,
  onDeleteEntry
}) => {
  const [selectedTerreno, setSelectedTerreno] = useState<string>('todos');
  const [selectedTipo, setSelectedTipo] = useState<string>('todos');
  const [searchTerm, setSearchTerm] = useState<string>('');

  const filteredEntries = entries.filter((entry) => {
    if (selectedTerreno !== 'todos' && entry.terrenoId !== selectedTerreno) return false;
    if (selectedTipo !== 'todos' && entry.tipo !== selectedTipo) return false;
    if (searchTerm.trim()) {
      const term = searchTerm.toLowerCase();
      const matchTitle = entry.titulo.toLowerCase().includes(term);
      const matchDesc = entry.descripcion.toLowerCase().includes(term);
      const matchTerreno = entry.terrenoNombre?.toLowerCase().includes(term);
      const matchTrabajador = entry.trabajador?.toLowerCase().includes(term);
      return matchTitle || matchDesc || matchTerreno || matchTrabajador;
    }
    return true;
  });

  const handleExport = () => {
    const csvHeader = "Fecha,Tipo,Titulo,Terreno,Trabajador,Monto,CantidadKg\n";
    const csvRows = filteredEntries.map(e => 
      `"${e.fechaRaw || e.fecha}","${e.tipo}","${e.titulo}","${e.terrenoNombre || ''}","${e.trabajador || ''}","${e.monto || ''}","${e.cantidadKg || ''}"`
    ).join("\n");
    const blob = new Blob([csvHeader + csvRows], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `Diario_Terrenario_${new Date().toISOString().split('T')[0]}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const getItemBadge = (tipo: DiarioEntry['tipo']) => {
    switch (tipo) {
      case 'actividad':
        return { bg: 'bg-[#4a5d23]', text: 'text-white', icon: 'content_cut', label: 'Labor' };
      case 'compra':
        return { bg: 'bg-[#5a3811]', text: 'text-white', icon: 'shopping_bag', label: 'Compra' };
      case 'cosecha':
        return { bg: 'bg-[#4c6700]', text: 'text-white', icon: 'agriculture', label: 'Cosecha' };
      case 'riego':
        return { bg: 'bg-[#0284c7]', text: 'text-white', icon: 'water_drop', label: 'Riego' };
      case 'alerta':
        return { bg: 'bg-[#ba1a1a]', text: 'text-white', icon: 'warning', label: 'Alerta' };
      default:
        return { bg: 'bg-[#33450d]', text: 'text-white', icon: 'event_note', label: 'Registro' };
    }
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      {/* Top Banner & Control Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Diario de Campo</h2>
          <p className="text-xs text-[#76786b]">Muro cronológico de actividades, riegos, compras y cosechas.</p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleExport}
            className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#45483c] text-xs font-semibold transition-colors border border-[#c6c8b8]"
          >
            <span className="material-symbols-outlined text-sm">download</span>
            <span>Exportar CSV</span>
          </button>

          <button
            onClick={onOpenNewModal}
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors"
          >
            <span className="material-symbols-outlined text-base">add</span>
            <span>Nuevo Registro</span>
          </button>
        </div>
      </div>

      {/* Glassmorphic Filter Controls */}
      <div className="bg-white/80 backdrop-blur-md p-4 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-3">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {/* Search bar */}
          <div className="relative">
            <span className="material-symbols-outlined absolute left-3 top-2.5 text-[#76786b] text-lg">search</span>
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar en el diario..."
              className="w-full pl-9 pr-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            />
          </div>

          {/* Terreno filter */}
          <div>
            <select
              value={selectedTerreno}
              onChange={(e) => setSelectedTerreno(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los Terrenos</option>
              {terrenos.map((t) => (
                <option key={t.id} value={t.id}>{t.nombre}</option>
              ))}
            </select>
          </div>

          {/* Type filter */}
          <div>
            <select
              value={selectedTipo}
              onChange={(e) => setSelectedTipo(e.target.value)}
              className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todos">Todos los Tipos</option>
              <option value="actividad">Labores de Campo</option>
              <option value="riego">Riegos</option>
              <option value="cosecha">Cosechas</option>
              <option value="compra">Compras / Gastos</option>
              <option value="alerta">Alertas</option>
            </select>
          </div>
        </div>
      </div>

      {/* Timeline Feed */}
      {filteredEntries.length === 0 ? (
        <div className="bg-white p-12 text-center rounded-2xl border border-[#e5e2dd] space-y-3">
          <span className="material-symbols-outlined text-4xl text-[#76786b]">event_busy</span>
          <h3 className="font-headline font-bold text-base text-[#1c1c19]">No hay registros encontrados</h3>
          <p className="text-xs text-[#76786b]">Intenta ajustar los filtros de búsqueda o agrega una nueva entrada.</p>
          <button
            onClick={onOpenNewModal}
            className="px-4 py-2 bg-[#33450d] text-white text-xs font-bold rounded-xl mt-2 inline-flex items-center gap-1"
          >
            <span className="material-symbols-outlined text-sm">add</span>
            <span>Añadir Registro</span>
          </button>
        </div>
      ) : (
        <div className="relative pl-6 space-y-6 before:absolute before:left-3.5 before:top-3 before:bottom-3 before:w-0.5 before:bg-[#c6c8b8]">
          {filteredEntries.map((entry) => {
            const badge = getItemBadge(entry.tipo);
            return (
              <div key={entry.id} className="relative group">
                {/* Timeline node icon */}
                <div className={`absolute -left-6 top-4 w-7 h-7 rounded-full ${badge.bg} ${badge.text} flex items-center justify-center text-sm shadow-md ring-4 ring-[#fcf9f4]`}>
                  <span className="material-symbols-outlined text-base">{badge.icon}</span>
                </div>

                {/* Card Container */}
                <div className="bg-white rounded-2xl border border-[#e5e2dd] p-5 shadow-2xs hover:shadow-md transition-all space-y-3 ml-3">
                  {/* Header line */}
                  <div className="flex items-start justify-between gap-3">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded-md ${badge.bg} text-white uppercase tracking-wider`}>
                          {badge.label}
                        </span>
                        <span className="text-xs font-bold text-[#33450d]">
                          {entry.fecha}
                        </span>
                        {entry.hora && (
                          <span className="text-[11px] text-[#76786b]">• {entry.hora}</span>
                        )}
                      </div>
                      <h3 className="font-headline font-bold text-lg text-[#1c1c19] tracking-tight">
                        {entry.titulo}
                      </h3>
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => onToggleComplete(entry.id)}
                        title={entry.completado ? 'Marcar incompleto' : 'Marcar completado'}
                        className={`p-1.5 rounded-lg border transition-colors ${
                          entry.completado 
                            ? 'bg-[#c9f16f] border-[#aed456] text-[#33450d]' 
                            : 'bg-[#f6f3ee] border-[#c6c8b8] text-[#76786b]'
                        }`}
                      >
                        <span className="material-symbols-outlined text-base">check</span>
                      </button>

                      <button
                        onClick={() => onDeleteEntry(entry.id)}
                        title="Eliminar de diario"
                        className="p-1.5 rounded-lg text-[#76786b] hover:text-[#ba1a1a] hover:bg-[#ffdad6]/50 transition-colors"
                      >
                        <span className="material-symbols-outlined text-base">delete</span>
                      </button>
                    </div>
                  </div>

                  {/* Description */}
                  <p className="text-xs sm:text-sm text-[#45483c] leading-relaxed">
                    {entry.descripcion}
                  </p>

                  {/* Data Highlights / Badges */}
                  <div className="flex items-center gap-4 text-xs font-semibold text-[#1c1c19] flex-wrap pt-1 border-t border-[#f0ede8]">
                    {entry.terrenoNombre && (
                      <div className="flex items-center gap-1 text-[#33450d]">
                        <span className="material-symbols-outlined text-base">location_on</span>
                        <span>{entry.terrenoNombre}</span>
                      </div>
                    )}

                    {entry.trabajador && (
                      <div className="flex items-center gap-1 text-[#45483c]">
                        <span className="material-symbols-outlined text-base">person</span>
                        <span>{entry.trabajador}</span>
                      </div>
                    )}

                    {entry.duracionHoras && (
                      <div className="flex items-center gap-1 text-[#76786b]">
                        <span className="material-symbols-outlined text-base">schedule</span>
                        <span>{entry.duracionHoras}h</span>
                      </div>
                    )}

                    {entry.monto && (
                      <div className="flex items-center gap-1 text-[#ba1a1a] font-bold">
                        <span className="material-symbols-outlined text-base">payments</span>
                        <span>- {entry.monto} €</span>
                      </div>
                    )}

                    {entry.cantidadKg && (
                      <div className="flex items-center gap-1 text-[#4c6700] font-bold">
                        <span className="material-symbols-outlined text-base">scale</span>
                        <span>{entry.cantidadKg.toLocaleString()} Kg</span>
                      </div>
                    )}
                  </div>

                  {/* Optional Photos attached */}
                  {entry.fotos && entry.fotos.length > 0 && (
                    <div className="flex items-center gap-3 pt-2 overflow-x-auto hide-scrollbar">
                      {entry.fotos.map((foto, idx) => (
                        <img
                          key={idx}
                          src={foto}
                          alt={`Adjunto ${idx + 1}`}
                          className="w-24 h-20 rounded-xl object-cover border border-[#e5e2dd] shadow-xs hover:scale-105 transition-transform"
                        />
                      ))}
                    </div>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
