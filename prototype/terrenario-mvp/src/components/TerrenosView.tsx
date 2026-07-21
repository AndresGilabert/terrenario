import React, { useState } from 'react';
import { Terreno } from '../types';

interface TerrenosViewProps {
  terrenos: Terreno[];
  onOpenAddModal: () => void;
  onSelectTerreno: (terreno: Terreno) => void;
}

export const TerrenosView: React.FC<TerrenosViewProps> = ({
  terrenos,
  onOpenAddModal,
  onSelectTerreno
}) => {
  const [searchTerm, setSearchTerm] = useState('');

  const filteredTerrenos = terrenos.filter((t) => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return (
      t.nombre.toLowerCase().includes(term) ||
      t.alias.toLowerCase().includes(term) ||
      t.ubicacion.toLowerCase().includes(term)
    );
  });

  return (
    <div className="space-y-6 max-w-6xl mx-auto pb-12">
      {/* Header and Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Terrenos y Parcelas</h2>
          <p className="text-xs text-[#76786b]">Monitoriza el inventario de olivos, riego y poda por parcela.</p>
        </div>

        <button
          onClick={onOpenAddModal}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors"
        >
          <span className="material-symbols-outlined text-lg">add</span>
          <span>Añadir Terreno</span>
        </button>
      </div>

      {/* Search Bar */}
      <div className="bg-white p-3 rounded-2xl border border-[#e5e2dd] flex items-center gap-3">
        <span className="material-symbols-outlined text-[#76786b] pl-2">search</span>
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Buscar parcela por nombre, alias o sector..."
          className="w-full bg-transparent text-xs font-medium text-[#1c1c19] focus:outline-none"
        />
        {searchTerm && (
          <button onClick={() => setSearchTerm('')} className="text-xs text-[#76786b] pr-2">Limpiar</button>
        )}
      </div>

      {/* Grid of Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {filteredTerrenos.map((t) => {
          const isIncomplete = t.estado === 'incompleto';
          return (
            <div
              key={t.id}
              className={`bg-white rounded-2xl border transition-all duration-200 overflow-hidden flex flex-col justify-between shadow-2xs hover:shadow-md ${
                isIncomplete ? 'border-[#ba1a1a] ring-1 ring-[#ba1a1a]' : 'border-[#e5e2dd]'
              }`}
            >
              {/* Card Image Banner */}
              <div className="relative h-44 overflow-hidden bg-[#f0ede8]">
                <img
                  src={t.imagenUrl}
                  alt={t.nombre}
                  className="w-full h-full object-cover transition-transform duration-500 hover:scale-105"
                />
                <div className="absolute top-3 left-3 flex items-center gap-2">
                  <span className="text-xs font-bold px-2.5 py-1 rounded-md bg-white/90 backdrop-blur-md text-[#33450d] shadow-xs">
                    {t.alias}
                  </span>
                  {isIncomplete && (
                    <span className="text-xs font-bold px-2.5 py-1 rounded-md bg-[#ba1a1a] text-white shadow-xs flex items-center gap-1">
                      <span className="material-symbols-outlined text-xs">warning</span>
                      <span>Incompleto</span>
                    </span>
                  )}
                </div>
              </div>

              {/* Card Body */}
              <div className="p-5 space-y-3 flex-1 flex flex-col justify-between">
                <div>
                  <h3 className="font-headline font-bold text-lg text-[#1c1c19] tracking-tight">{t.nombre}</h3>
                  <p className="text-xs text-[#76786b] flex items-center gap-1 mt-0.5">
                    <span className="material-symbols-outlined text-sm">location_on</span>
                    <span>{t.ubicacion}</span>
                  </p>
                </div>

                {/* Data Points */}
                {isIncomplete ? (
                  <div className="bg-[#ffdad6] p-3 rounded-xl border border-[#ffb4ab] text-xs text-[#93000a] space-y-1">
                    <p className="font-bold flex items-center gap-1">
                      <span className="material-symbols-outlined text-base">error_med</span>
                      <span>{t.alertaMsg || 'Datos incompletos'}</span>
                    </p>
                    <p className="text-[11px]">Completa los datos del inventario para habilitar el seguimiento de cosechas.</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-2 gap-2 text-xs bg-[#f6f3ee] p-3 rounded-xl border border-[#e5e2dd]">
                    <div>
                      <p className="text-[10px] font-bold text-[#76786b] uppercase">Olivos</p>
                      <p className="font-extrabold text-[#1c1c19]">{t.olivosCount?.toLocaleString()} árboles</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-bold text-[#76786b] uppercase">Superficie</p>
                      <p className="font-extrabold text-[#1c1c19]">{t.superficieHa || '3.5'} Ha</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-bold text-[#76786b] uppercase">Riego</p>
                      <p className="font-semibold text-[#33450d]">{t.tipoRiego}</p>
                    </div>
                    <div>
                      <p className="text-[10px] font-bold text-[#76786b] uppercase">Poda</p>
                      <p className="font-semibold text-[#45483c]">{t.estadoPoda}</p>
                    </div>
                  </div>
                )}

                {/* Actions */}
                <div className="pt-2 border-t border-[#f0ede8] flex items-center justify-between">
                  <span className="text-xs font-semibold text-[#76786b]">
                    Variedad: {t.variedadPrincipal || 'Picual'}
                  </span>

                  <button
                    onClick={() => onSelectTerreno(t)}
                    className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition-colors ${
                      isIncomplete 
                        ? 'bg-[#ba1a1a] hover:bg-[#93000a] text-white shadow-xs' 
                        : 'bg-[#f0ede8] hover:bg-[#ebe8e3] text-[#33450d]'
                    }`}
                  >
                    {isIncomplete ? 'Completar Datos' : 'Ver Detalles'}
                  </button>
                </div>
              </div>
            </div>
          );
        })}

        {/* Add Terreno Card Button */}
        <button
          onClick={onOpenAddModal}
          className="min-h-[300px] bg-[#f6f3ee] hover:bg-[#f0ede8] rounded-2xl border-2 border-dashed border-[#c6c8b8] p-6 flex flex-col items-center justify-center text-center space-y-3 transition-colors group"
        >
          <div className="w-14 h-14 rounded-full bg-white text-[#33450d] flex items-center justify-center shadow-md group-hover:scale-110 transition-transform">
            <span className="material-symbols-outlined text-3xl">add_location_alt</span>
          </div>
          <div>
            <h3 className="font-headline font-bold text-base text-[#1c1c19]">Añadir Nueva Parcela</h3>
            <p className="text-xs text-[#76786b] max-w-xs mt-1">Registra un nuevo terreno para comenzar a anotar riegos, labores y cosechas.</p>
          </div>
        </button>
      </div>
    </div>
  );
};
