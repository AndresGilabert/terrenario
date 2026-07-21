import React, { useState } from 'react';
import { PurchaseRecord } from '../types';

interface ComprasViewProps {
  purchases: PurchaseRecord[];
  onAddPurchase: (purchase: PurchaseRecord) => void;
}

export const ComprasView: React.FC<ComprasViewProps> = ({ purchases, onAddPurchase }) => {
  const [concepto, setConcepto] = useState('');
  const [categoria, setCategoria] = useState('Fertilizantes');
  const [proveedor, setProveedor] = useState('');
  const [monto, setMonto] = useState('');
  const [cantidad, setCantidad] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!concepto.trim() || !monto) return;

    onAddPurchase({
      id: Date.now().toString(),
      fecha: new Date().toISOString().split('T')[0],
      concepto: concepto.trim(),
      categoria,
      proveedor: proveedor.trim() || 'Proveedor Local',
      monto: parseFloat(monto),
      cantidad: cantidad.trim() || '1 unidad'
    });

    setConcepto('');
    setMonto('');
    setProveedor('');
    setCantidad('');
  };

  const totalGastos = purchases.reduce((acc, p) => acc + p.monto, 0);

  return (
    <div className="space-y-6 max-w-5xl mx-auto pb-12">
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Compras e Insumos</h2>
          <p className="text-xs text-[#76786b]">Libro de gastos para abonos, combustible, piezas y servicios.</p>
        </div>

        <div className="bg-[#f6f3ee] px-4 py-2 rounded-xl border border-[#e5e2dd]">
          <p className="text-[10px] font-bold text-[#76786b] uppercase">Gasto Acumulado</p>
          <p className="font-headline font-extrabold text-lg text-[#ba1a1a]">{totalGastos.toLocaleString()} €</p>
        </div>
      </div>

      {/* Formulario de nueva compra */}
      <form onSubmit={handleSubmit} className="bg-white p-4 rounded-2xl border border-[#e5e2dd] space-y-3">
        <h3 className="font-headline font-bold text-sm text-[#1c1c19]">Añadir Nueva Compra</h3>
        <div className="grid grid-cols-1 sm:grid-cols-5 gap-3">
          <input
            type="text"
            required
            value={concepto}
            onChange={(e) => setConcepto(e.target.value)}
            placeholder="Concepto (ej. Abono NPK 500kg)"
            className="sm:col-span-2 px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs"
          />
          <select
            value={categoria}
            onChange={(e) => setCategoria(e.target.value)}
            className="px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs"
          >
            <option value="Fertilizantes">Fertilizantes</option>
            <option value="Fitosanitarios">Fitosanitarios</option>
            <option value="Riego">Riego y Tuberías</option>
            <option value="Combustible">Combustible</option>
            <option value="Mantenimiento">Mantenimiento</option>
          </select>

          <input
            type="text"
            value={proveedor}
            onChange={(e) => setProveedor(e.target.value)}
            placeholder="Proveedor"
            className="px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs"
          />

          <input
            type="number"
            required
            value={monto}
            onChange={(e) => setMonto(e.target.value)}
            placeholder="Monto (€)"
            className="px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs"
          />
        </div>
        <div className="flex justify-end">
          <button
            type="submit"
            className="px-5 py-2 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl shadow-xs"
          >
            Registrar Gasto
          </button>
        </div>
      </form>

      {/* Table of Purchases */}
      <div className="bg-white rounded-2xl border border-[#e5e2dd] shadow-2xs overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-[#1c1c19]">
            <thead className="bg-[#f6f3ee] border-b border-[#e5e2dd] text-[11px] font-bold uppercase tracking-wider text-[#45483c]">
              <tr>
                <th className="px-5 py-3.5">Fecha</th>
                <th className="px-5 py-3.5">Concepto</th>
                <th className="px-5 py-3.5">Categoría</th>
                <th className="px-5 py-3.5">Proveedor</th>
                <th className="px-5 py-3.5 text-right">Monto (€)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#f0ede8]">
              {purchases.map((p) => (
                <tr key={p.id} className="hover:bg-[#fcf9f4]">
                  <td className="px-5 py-3.5 font-medium text-[#76786b]">{p.fecha}</td>
                  <td className="px-5 py-3.5 font-bold text-[#1c1c19]">{p.concepto}</td>
                  <td className="px-5 py-3.5">
                    <span className="px-2.5 py-0.5 rounded-full bg-[#f0ede8] text-[#33450d] font-semibold text-[11px]">
                      {p.categoria}
                    </span>
                  </td>
                  <td className="px-5 py-3.5 text-[#45483c]">{p.proveedor}</td>
                  <td className="px-5 py-3.5 text-right font-extrabold text-[#ba1a1a]">
                    - {p.monto.toLocaleString()} €
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
