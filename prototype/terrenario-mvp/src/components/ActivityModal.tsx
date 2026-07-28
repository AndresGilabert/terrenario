import React, { useState } from 'react';
import { Terreno, DiarioEntry, Worker } from '../types';

interface ActivityModalProps {
  isOpen: boolean;
  onClose: () => void;
  terrenos: Terreno[];
  workers: Worker[];
  onSave: (entry: Partial<DiarioEntry>) => void;
}

export const ActivityModal: React.FC<ActivityModalProps> = ({
  isOpen,
  onClose,
  terrenos,
  workers,
  onSave
}) => {
  const [tipo, setTipo] = useState<'actividad' | 'compra' | 'cosecha' | 'riego'>('actividad');
  const [titulo, setTitulo] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [terrenoId, setTerrenoId] = useState(terrenos[0]?.id || '');
  const [trabajador, setTrabajador] = useState(workers[0]?.nombre || '');
  const [fechaRaw, setFechaRaw] = useState(new Date().toISOString().split('T')[0]);
  const [duracionHoras, setDuracionHoras] = useState(4);
  const [monto, setMonto] = useState(0);
  const [cantidadKg, setCantidadKg] = useState(0);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!titulo.trim()) return;

    const selectedTerreno = terrenos.find((t) => t.id === terrenoId);

    onSave({
      tipo,
      titulo: titulo.trim(),
      descripcion: descripcion.trim(),
      fecha: 'Hoy, ' + new Date().toLocaleDateString('es-ES', { day: 'numeric', month: 'short' }),
      fechaRaw,
      terrenoId,
      terrenoNombre: selectedTerreno?.nombre || 'General',
      trabajador,
      duracionHoras: Number(duracionHoras),
      monto: Number(monto),
      cantidadKg: Number(cantidadKg),
      completado: true
    });

    // Reset form
    setTitulo('');
    setDescripcion('');
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-lg w-full border border-[#e5e2dd] shadow-2xl overflow-hidden animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl">post_add</span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Nuevo Registro en Diario</h3>
          </div>
          <button
            onClick={onClose}
            className="p-1 rounded-lg text-[#76786b] hover:bg-[#e5e2dd] transition-colors"
          >
            <span className="material-symbols-outlined text-lg">close</span>
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm">
          {/* Type Selector Tabs */}
          <div className="grid grid-cols-4 gap-1 p-1 bg-[#f0ede8] rounded-xl text-xs font-semibold">
            <button
              type="button"
              onClick={() => setTipo('actividad')}
              className={`py-2 rounded-lg text-center transition-all ${
                tipo === 'actividad' ? 'bg-[#33450d] text-white shadow-xs' : 'text-[#45483c] hover:text-[#1c1c19]'
              }`}
            >
              Labor
            </button>
            <button
              type="button"
              onClick={() => setTipo('riego')}
              className={`py-2 rounded-lg text-center transition-all ${
                tipo === 'riego' ? 'bg-[#33450d] text-white shadow-xs' : 'text-[#45483c] hover:text-[#1c1c19]'
              }`}
            >
              Riego
            </button>
            <button
              type="button"
              onClick={() => setTipo('cosecha')}
              className={`py-2 rounded-lg text-center transition-all ${
                tipo === 'cosecha' ? 'bg-[#33450d] text-white shadow-xs' : 'text-[#45483c] hover:text-[#1c1c19]'
              }`}
            >
              Cosecha
            </button>
            <button
              type="button"
              onClick={() => setTipo('compra')}
              className={`py-2 rounded-lg text-center transition-all ${
                tipo === 'compra' ? 'bg-[#33450d] text-white shadow-xs' : 'text-[#45483c] hover:text-[#1c1c19]'
              }`}
            >
              Compra
            </button>
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Título / Concepto
            </label>
            <input
              type="text"
              required
              value={titulo}
              onChange={(e) => setTitulo(e.target.value)}
              placeholder={
                tipo === 'actividad' ? 'ej. Poda de formación, Fitosanitario' :
                tipo === 'riego' ? 'ej. Riego Lote A' :
                tipo === 'cosecha' ? 'ej. Recolección Picual' : 'ej. Abono NPK 500kg'
              }
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Terreno Asignado
              </label>
              <select
                value={terrenoId}
                onChange={(e) => setTerrenoId(e.target.value)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
              >
                {terrenos.map((t) => (
                  <option key={t.id} value={t.id}>{t.nombre} ({t.alias})</option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Fecha
              </label>
              <input
                type="date"
                value={fechaRaw}
                onChange={(e) => setFechaRaw(e.target.value)}
                required
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
              />
            </div>
          </div>

          {tipo === 'actividad' && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Responsable
                </label>
                <select
                  value={trabajador}
                  onChange={(e) => setTrabajador(e.target.value)}
                  className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
                >
                  {workers.map((w) => (
                    <option key={w.id} value={w.nombre}>{w.nombre}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Horas Dedicadas
                </label>
                <input
                  type="number"
                  min="0.5"
                  step="0.5"
                  value={duracionHoras}
                  onChange={(e) => setDuracionHoras(parseFloat(e.target.value) || 0)}
                  className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
                />
              </div>
            </div>
          )}

          {tipo === 'compra' && (
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Monto (€)
              </label>
              <input
                type="number"
                min="0"
                value={monto}
                onChange={(e) => setMonto(parseFloat(e.target.value) || 0)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>
          )}

          {tipo === 'cosecha' && (
            <div className="space-y-1.5">
              <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Cantidad Recolectada (Kg)
              </label>
              <input
                type="number"
                min="0"
                value={cantidadKg}
                onChange={(e) => setCantidadKg(parseFloat(e.target.value) || 0)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19]"
              />
            </div>
          )}

          <div className="space-y-1.5">
            <label className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Observaciones / Detalles
            </label>
            <textarea
              rows={3}
              value={descripcion}
              onChange={(e) => setDescripcion(e.target.value)}
              placeholder="Detalles sobre dosis, estado de la maquinaria, sector o incidencias..."
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
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
              Guardar Registro
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
