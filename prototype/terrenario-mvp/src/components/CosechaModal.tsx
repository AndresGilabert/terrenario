import React, { useState } from 'react';
import { Terreno, HarvestRecord } from '../types';

interface CosechaModalProps {
  isOpen: boolean;
  onClose: () => void;
  terrenos: Terreno[];
  onSave: (harvest: Partial<HarvestRecord>) => void;
}

export const CosechaModal: React.FC<CosechaModalProps> = ({
  isOpen,
  onClose,
  terrenos,
  onSave
}) => {
  const [terrenoId, setTerrenoId] = useState(terrenos[0]?.id || '');
  const [producto, setProducto] = useState('Aceituna Picual');
  const [kgs, setKgs] = useState(1200);
  const [rendimientoPct, setRendimientoPct] = useState(20.5);
  const [destino, setDestino] = useState('Almazara');
  const [almazaraNombre, setAlmazaraNombre] = useState('Almazara Regional');
  const [fecha, setFecha] = useState(new Date().toISOString().split('T')[0]);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const selTerreno = terrenos.find((t) => t.id === terrenoId);

    onSave({
      fecha: new Date(fecha).toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' }),
      terrenoId,
      terrenoNombre: selTerreno?.nombre || 'General',
      producto,
      kgs: Number(kgs),
      rendimientoPct: Number(rendimientoPct),
      destino,
      almazaraNombre: destino === 'Almazara' ? almazaraNombre : undefined
    });

    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-md w-full border border-[#e5e2dd] shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl">agriculture</span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Registrar Cosecha / Entrega</h3>
          </div>
          <button onClick={onClose} className="p-1 rounded-lg text-[#76786b] hover:bg-[#e5e2dd]">
            <span className="material-symbols-outlined text-lg">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Terreno Origen
              </label>
              <select
                value={terrenoId}
                onChange={(e) => setTerrenoId(e.target.value)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              >
                {terrenos.map((t) => (
                  <option key={t.id} value={t.id}>{t.nombre}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Fecha Recolección
              </label>
              <input
                type="date"
                value={fecha}
                onChange={(e) => setFecha(e.target.value)}
                required
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Producto / Variedad
            </label>
            <input
              type="text"
              required
              value={producto}
              onChange={(e) => setProducto(e.target.value)}
              placeholder="ej. Aceituna Picual, Hojiblanca, Arbequina"
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Kilos Totales (Kg)
              </label>
              <input
                type="number"
                min="1"
                required
                value={kgs}
                onChange={(e) => setKgs(parseFloat(e.target.value) || 0)}
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Rendimiento Graso (%)
              </label>
              <input
                type="number"
                min="0"
                max="100"
                step="0.1"
                value={rendimientoPct}
                onChange={(e) => setRendimientoPct(parseFloat(e.target.value) || 0)}
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Destino
              </label>
              <select
                value={destino}
                onChange={(e) => setDestino(e.target.value)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              >
                <option value="Almazara">Almazara / Cooperativa</option>
                <option value="Consumo Propio">Consumo Propio</option>
                <option value="Sin destino">Sin destino asignado</option>
              </select>
            </div>

            {destino === 'Almazara' && (
              <div className="space-y-1.5">
                <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Nombre Almazara
                </label>
                <input
                  type="text"
                  value={almazaraNombre}
                  onChange={(e) => setAlmazaraNombre(e.target.value)}
                  placeholder="ej. Almazara Regional"
                  className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
                />
              </div>
            )}
          </div>

          <div className="pt-2 flex items-center justify-end gap-3 border-t border-[#e5e2dd]">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl"
            >
              Cancelar
            </button>
            <button
              type="submit"
              className="px-5 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors"
            >
              Guardar Cosecha
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
