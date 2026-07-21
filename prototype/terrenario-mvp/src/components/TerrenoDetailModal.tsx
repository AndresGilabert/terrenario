import React, { useState } from 'react';
import { Terreno, DiarioEntry, HarvestRecord } from '../types';

interface TerrenoDetailModalProps {
  terreno: Terreno | null;
  diario: DiarioEntry[];
  harvests: HarvestRecord[];
  onClose: () => void;
  onUpdateTerreno: (updated: Terreno) => void;
}

export const TerrenoDetailModal: React.FC<TerrenoDetailModalProps> = ({
  terreno,
  diario,
  harvests,
  onClose,
  onUpdateTerreno
}) => {
  if (!terreno) return null;

  const [isEditing, setIsEditing] = useState(false);
  const [olivosCount, setOlivosCount] = useState(terreno.olivosCount || 0);
  const [superficieHa, setSuperficieHa] = useState(terreno.superficieHa || 0);
  const [tipoRiego, setTipoRiego] = useState(terreno.tipoRiego);
  const [estadoPoda, setEstadoPoda] = useState(terreno.estadoPoda);

  const relatedDiario = diario.filter((d) => d.terrenoId === terreno.id);
  const relatedHarvests = harvests.filter((h) => h.terrenoId === terreno.id);

  const handleSaveEdit = () => {
    onUpdateTerreno({
      ...terreno,
      olivosCount: Number(olivosCount) > 0 ? Number(olivosCount) : undefined,
      superficieHa: Number(superficieHa),
      tipoRiego,
      estadoPoda,
      estado: Number(olivosCount) > 0 ? 'activo' : 'incompleto',
      alertaMsg: Number(olivosCount) > 0 ? undefined : 'Nº de árboles sin registrar'
    });
    setIsEditing(false);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-2xl w-full border border-[#e5e2dd] shadow-2xl overflow-hidden max-h-[90vh] flex flex-col animate-in fade-in zoom-in duration-200">
        {/* Header with image */}
        <div className="relative h-44 bg-[#33450d] text-white">
          <img
            src={terreno.imagenUrl}
            alt={terreno.nombre}
            className="w-full h-full object-cover opacity-60 mix-blend-overlay"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/20 to-transparent p-6 flex flex-col justify-end">
            <div className="flex items-center justify-between">
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-xs px-2.5 py-0.5 rounded-md bg-[#c9f16f] text-[#33450d] font-bold">
                    {terreno.alias}
                  </span>
                  {terreno.estado === 'incompleto' && (
                    <span className="text-xs px-2.5 py-0.5 rounded-md bg-[#ffdad6] text-[#93000a] font-bold flex items-center gap-1">
                      <span className="material-symbols-outlined text-xs">warning</span>
                      <span>Incompleto</span>
                    </span>
                  )}
                </div>
                <h2 className="font-headline font-extrabold text-2xl text-white mt-1">{terreno.nombre}</h2>
                <p className="text-xs text-stone-200">{terreno.ubicacion}</p>
              </div>

              <button
                onClick={onClose}
                className="w-9 h-9 rounded-full bg-white/20 hover:bg-white/30 backdrop-blur-md flex items-center justify-center text-white"
              >
                <span className="material-symbols-outlined text-xl">close</span>
              </button>
            </div>
          </div>
        </div>

        {/* Content body */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1 text-sm">
          {/* Quick Stats Grid */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 bg-[#f6f3ee] p-4 rounded-xl border border-[#e5e2dd]">
            <div>
              <p className="text-[11px] font-bold text-[#76786b] uppercase">Superficie</p>
              <p className="text-base font-extrabold text-[#1c1c19]">{terreno.superficieHa || '1.0'} Ha</p>
            </div>
            <div>
              <p className="text-[11px] font-bold text-[#76786b] uppercase">Nº de Árboles</p>
              <p className="text-base font-extrabold text-[#1c1c19]">
                {terreno.olivosCount ? `${terreno.olivosCount} olivos` : 'Sin registrar'}
              </p>
            </div>
            <div>
              <p className="text-[11px] font-bold text-[#76786b] uppercase">Sistema Riego</p>
              <p className="text-xs font-semibold text-[#33450d] mt-1">{terreno.tipoRiego}</p>
            </div>
            <div>
              <p className="text-[11px] font-bold text-[#76786b] uppercase">Estado Poda</p>
              <p className="text-xs font-semibold text-[#45483c] mt-1">{terreno.estadoPoda}</p>
            </div>
          </div>

          {/* Edit Form inline */}
          {isEditing ? (
            <div className="p-4 bg-[#f0ede8] rounded-xl border border-[#c6c8b8] space-y-3">
              <h4 className="font-headline font-bold text-[#33450d]">Editar Parcela</h4>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-bold text-[#45483c]">Nº de Olivos</label>
                  <input
                    type="number"
                    value={olivosCount}
                    onChange={(e) => setOlivosCount(Number(e.target.value))}
                    className="w-full p-2 bg-white border border-[#c6c8b8] rounded-lg mt-1"
                  />
                </div>
                <div>
                  <label className="text-xs font-bold text-[#45483c]">Superficie (Ha)</label>
                  <input
                    type="number"
                    step="0.1"
                    value={superficieHa}
                    onChange={(e) => setSuperficieHa(Number(e.target.value))}
                    className="w-full p-2 bg-white border border-[#c6c8b8] rounded-lg mt-1"
                  />
                </div>
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button
                  onClick={() => setIsEditing(false)}
                  className="px-3 py-1.5 text-xs text-[#76786b]"
                >
                  Cancelar
                </button>
                <button
                  onClick={handleSaveEdit}
                  className="px-4 py-1.5 bg-[#33450d] text-white rounded-lg text-xs font-bold"
                >
                  Guardar Cambios
                </button>
              </div>
            </div>
          ) : (
            <div className="flex justify-between items-center">
              <span className="text-xs text-[#76786b]">Variedad principal: <strong>{terreno.variedadPrincipal || 'Picual'}</strong></span>
              <button
                onClick={() => setIsEditing(true)}
                className="flex items-center gap-1 text-xs font-semibold text-[#33450d] hover:underline"
              >
                <span className="material-symbols-outlined text-sm">edit</span>
                <span>Editar Datos de Parcela</span>
              </button>
            </div>
          )}

          {/* Historial de Cosechas en este terreno */}
          <div className="space-y-3">
            <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
              <span className="material-symbols-outlined text-[#33450d]">agriculture</span>
              <span>Cosechas Recientes en esta Parcela</span>
            </h3>

            {relatedHarvests.length === 0 ? (
              <p className="text-xs text-[#76786b] italic bg-[#f6f3ee] p-3 rounded-xl">
                No hay cosechas registradas para este terreno aún.
              </p>
            ) : (
              <div className="space-y-2">
                {relatedHarvests.map((h) => (
                  <div key={h.id} className="bg-[#f6f3ee] p-3 rounded-xl border border-[#e5e2dd] flex items-center justify-between text-xs">
                    <div>
                      <p className="font-bold text-[#1c1c19]">{h.producto} • {h.kgs.toLocaleString()} Kg</p>
                      <p className="text-[#76786b]">{h.fecha} • {h.destino} ({h.almazaraNombre || 'S.N.'})</p>
                    </div>
                    <span className="font-bold text-[#33450d] bg-[#c9f16f] px-2.5 py-1 rounded-full text-[11px]">
                      {h.rendimientoPct}% Rdt
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Labores del diario en este terreno */}
          <div className="space-y-3">
            <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
              <span className="material-symbols-outlined text-[#33450d]">event_note</span>
              <span>Historial de Labores</span>
            </h3>

            {relatedDiario.length === 0 ? (
              <p className="text-xs text-[#76786b] italic bg-[#f6f3ee] p-3 rounded-xl">
                Sin labores ni eventos anotados en el diario para este terreno.
              </p>
            ) : (
              <div className="space-y-2">
                {relatedDiario.map((d) => (
                  <div key={d.id} className="bg-[#f6f3ee] p-3 rounded-xl border border-[#e5e2dd] space-y-1 text-xs">
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-[#1c1c19]">{d.titulo}</span>
                      <span className="text-[#76786b]">{d.fecha}</span>
                    </div>
                    <p className="text-[#45483c] text-[11px]">{d.descripcion}</p>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 bg-[#f0ede8] border-t border-[#e5e2dd] flex justify-end">
          <button
            onClick={onClose}
            className="px-5 py-2 bg-[#33450d] text-white text-xs font-bold rounded-xl"
          >
            Cerrar
          </button>
        </div>
      </div>
    </div>
  );
};
