import React, { useState } from 'react';
import { Terreno } from '../types';

interface TerrenoModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSave: (terreno: Partial<Terreno>) => void;
}

export const TerrenoModal: React.FC<TerrenoModalProps> = ({ isOpen, onClose, onSave }) => {
  const [nombre, setNombre] = useState('');
  const [alias, setAlias] = useState('');
  const [ubicacion, setUbicacion] = useState('');
  const [olivosCount, setOlivosCount] = useState<number | ''>(500);
  const [superficieHa, setSuperficieHa] = useState<number | ''>(2.5);
  const [tipoRiego, setTipoRiego] = useState('Goteo automatizado');
  const [estadoPoda, setEstadoPoda] = useState('Poda al día');
  const [variedadPrincipal, setVariedadPrincipal] = useState('Picual');

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!nombre.trim()) return;

    onSave({
      nombre: nombre.trim(),
      alias: alias.trim() || nombre.substring(0, 3).toUpperCase() + '-0' + Math.floor(Math.random() * 9 + 1),
      ubicacion: ubicacion.trim() || 'Sector Principal',
      olivosCount: olivosCount ? Number(olivosCount) : undefined,
      superficieHa: superficieHa ? Number(superficieHa) : 1.0,
      tipoRiego,
      estadoPoda,
      variedadPrincipal,
      estado: olivosCount ? 'activo' : 'incompleto',
      alertaMsg: olivosCount ? undefined : 'Nº de árboles sin registrar',
      imagenUrl: 'https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=600&auto=format&fit=crop&q=80'
    });

    setNombre('');
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-md w-full border border-[#e5e2dd] shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl">map</span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Añadir Nuevo Terreno</h3>
          </div>
          <button onClick={onClose} className="p-1 rounded-lg text-[#76786b] hover:bg-[#e5e2dd]">
            <span className="material-symbols-outlined text-lg">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm">
          <div className="space-y-1.5">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Nombre del Terreno / Parcela
            </label>
            <input
              type="text"
              required
              value={nombre}
              onChange={(e) => setNombre(e.target.value)}
              placeholder="ej. La Hoya Norte, Olivar Alto"
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Alias / Código
              </label>
              <input
                type="text"
                value={alias}
                onChange={(e) => setAlias(e.target.value)}
                placeholder="ej. LH-04"
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Superficie (Ha)
              </label>
              <input
                type="number"
                step="0.1"
                value={superficieHa}
                onChange={(e) => setSuperficieHa(e.target.value ? parseFloat(e.target.value) : '')}
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Nº de Olivos / Árboles
              </label>
              <input
                type="number"
                value={olivosCount}
                onChange={(e) => setOlivosCount(e.target.value ? parseInt(e.target.value, 10) : '')}
                placeholder="ej. 850"
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Variedad Principal
              </label>
              <select
                value={variedadPrincipal}
                onChange={(e) => setVariedadPrincipal(e.target.value)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              >
                <option value="Picual">Picual</option>
                <option value="Hojiblanca">Hojiblanca</option>
                <option value="Arbequina">Arbequina</option>
                <option value="Manzanilla">Manzanilla</option>
                <option value="Cornicabra">Cornicabra</option>
              </select>
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Ubicación / Sector
            </label>
            <input
              type="text"
              value={ubicacion}
              onChange={(e) => setUbicacion(e.target.value)}
              placeholder="ej. Sector Norte, junto al camino real"
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
            />
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
              Crear Parcela
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
