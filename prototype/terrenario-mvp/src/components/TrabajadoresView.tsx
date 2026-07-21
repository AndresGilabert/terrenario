import React, { useState } from 'react';
import { Worker } from '../types';

interface TrabajadoresViewProps {
  workers: Worker[];
  onAddWorker: (worker: Worker) => void;
  onToggleWorkerStatus: (id: string) => void;
}

export const TrabajadoresView: React.FC<TrabajadoresViewProps> = ({
  workers,
  onAddWorker,
  onToggleWorkerStatus
}) => {
  const [nombre, setNombre] = useState('');
  const [rol, setRol] = useState('Jornalero / Operario');
  const [telefono, setTelefono] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!nombre.trim()) return;

    onAddWorker({
      id: Date.now().toString(),
      nombre: nombre.trim(),
      rol,
      telefono: telefono.trim() || '+34 600 000 000',
      activo: true,
      avatarUrl: `https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150&auto=format&fit=crop&q=80`
    });

    setNombre('');
    setTelefono('');
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Personal y Cuadrilla</h2>
          <p className="text-xs text-[#76786b]">Equipo de trabajo asignable a las labores del diario.</p>
        </div>
      </div>

      {/* Form para añadir trabajador */}
      <form onSubmit={handleSubmit} className="bg-white p-4 rounded-2xl border border-[#e5e2dd] grid grid-cols-1 sm:grid-cols-4 gap-3">
        <input
          type="text"
          required
          value={nombre}
          onChange={(e) => setNombre(e.target.value)}
          placeholder="Nombre completo"
          className="px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19]"
        />
        <input
          type="text"
          value={rol}
          onChange={(e) => setRol(e.target.value)}
          placeholder="Rol / Especialidad"
          className="px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19]"
        />
        <input
          type="text"
          value={telefono}
          onChange={(e) => setTelefono(e.target.value)}
          placeholder="Teléfono"
          className="px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19]"
        />
        <button
          type="submit"
          className="px-4 py-2 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl shadow-xs"
        >
          Añadir a Cuadrilla
        </button>
      </form>

      {/* Grid of Workers */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {workers.map((w) => (
          <div key={w.id} className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-4">
            <div className="flex items-center gap-3">
              <img
                src={w.avatarUrl}
                alt={w.nombre}
                className="w-12 h-12 rounded-full object-cover border border-[#c6c8b8]"
              />
              <div>
                <h3 className="font-headline font-bold text-base text-[#1c1c19]">{w.nombre}</h3>
                <p className="text-xs text-[#76786b]">{w.rol}</p>
              </div>
            </div>

            <div className="space-y-1 text-xs text-[#45483c] bg-[#f6f3ee] p-3 rounded-xl">
              <p className="flex items-center gap-1">
                <span className="material-symbols-outlined text-sm text-[#76786b]">call</span>
                <span>{w.telefono}</span>
              </p>
            </div>

            <div className="flex items-center justify-between pt-2 border-t border-[#f0ede8]">
              <span className={`text-[10px] font-bold px-2.5 py-1 rounded-full ${
                w.activo ? 'bg-[#c9f16f] text-[#33450d]' : 'bg-[#e5e2dd] text-[#76786b]'
              }`}>
                {w.activo ? 'ACTIVO EN CAMPAÑA' : 'INACTIVO'}
              </span>

              <button
                onClick={() => onToggleWorkerStatus(w.id)}
                className="text-xs font-bold text-[#33450d] hover:underline"
              >
                {w.activo ? 'Desactivar' : 'Activar'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
