import React, { useEffect, useMemo, useState } from 'react';
import type { Season } from '../../types/season.types';
import {
  PURCHASE_PRODUCT_MAX_LENGTH,
  type CreatePurchasePayload,
  type ProductSuggestion,
  type Purchase,
} from '../../types/purchase.types';

interface PurchaseFormModalProps {
  isOpen: boolean;
  /** Compra a corregir; nunca `null`: el alta se hace en línea, como en el prototipo. */
  purchase: Purchase | null;
  seasons: Season[];
  suggestions: ProductSuggestion[];
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (payload: CreatePurchasePayload) => void;
}

/**
 * Corrección de una compra ya registrada (MVP-303). El **alta** vive en el formulario en línea del
 * libro de compras, como en el prototipo: apuntar gastos es escribir varias líneas seguidas. Corregir
 * es en cambio una acción puntual sobre una fila concreta, y en una tabla de seis columnas editar en
 * línea sería peor que un formulario con sus etiquetas.
 */
export const PurchaseFormModal: React.FC<PurchaseFormModalProps> = ({
  isOpen,
  purchase,
  seasons,
  suggestions,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const [purchaseDate, setPurchaseDate] = useState('');
  const [seasonId, setSeasonId] = useState('');
  const [product, setProduct] = useState('');
  const [totalQuantity, setTotalQuantity] = useState('');
  const [totalCost, setTotalCost] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen || !purchase) return;
    setPurchaseDate(purchase.purchase_date);
    setSeasonId(purchase.season_id);
    setProduct(purchase.product);
    setTotalQuantity(String(purchase.total_quantity));
    setTotalCost(String(purchase.total_cost));
    setLocalError(null);
  }, [isOpen, purchase]);

  const selectedSeason = useMemo(
    () => seasons.find((season) => season.id === seasonId) ?? null,
    [seasons, seasonId]
  );

  // RN-023 — aviso, nunca bloqueo. Se calcula también en cliente para que aparezca al escribir.
  const isOutOfSeasonRange = useMemo(() => {
    if (!selectedSeason || !purchaseDate) return false;
    if (purchaseDate < selectedSeason.start_date) return true;
    return selectedSeason.end_date !== null && purchaseDate > selectedSeason.end_date;
  }, [selectedSeason, purchaseDate]);

  const unitPrice = useMemo(() => {
    const quantity = Number(totalQuantity);
    const cost = Number(totalCost);
    if (!Number.isFinite(quantity) || !Number.isFinite(cost) || quantity <= 0) return null;
    return cost / quantity;
  }, [totalQuantity, totalCost]);

  if (!isOpen || !purchase) return null;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const quantity = Number(totalQuantity);
    const cost = Number(totalCost);
    if (!product.trim()) {
      setLocalError('Escribe el producto o material comprado.');
      return;
    }
    if (!Number.isFinite(quantity) || quantity <= 0 || !Number.isFinite(cost) || cost <= 0) {
      setLocalError('La cantidad y el coste deben ser mayores que 0.');
      return;
    }

    setLocalError(null);
    onSubmit({
      purchase_date: purchaseDate,
      season_id: seasonId,
      product: product.trim(),
      total_quantity: quantity,
      total_cost: cost,
    });
  };

  const shownError = localError ?? errorMessage;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-lg w-full border border-[#e5e2dd] shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl" aria-hidden="true">receipt_long</span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Corregir compra</h3>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            aria-label="Cerrar"
            className="p-1 rounded-lg text-[#76786b] hover:bg-[#e5e2dd] disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">close</span>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4 text-sm overflow-y-auto" noValidate>
          <div className="space-y-1.5">
            <label htmlFor="purchase-product" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Producto o material <span className="text-[#ba1a1a]">*</span>
            </label>
            <input
              id="purchase-product"
              type="text"
              required
              list="purchase-product-options"
              maxLength={PURCHASE_PRODUCT_MAX_LENGTH}
              value={product}
              onChange={(e) => setProduct(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            />
            {/* RN-031 — sugerencias del histórico, no un catálogo: siempre se puede escribir otra cosa */}
            <datalist id="purchase-product-options">
              {suggestions.map((suggestion) => (
                <option key={suggestion.product} value={suggestion.product} />
              ))}
            </datalist>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="purchase-date" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Fecha <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="purchase-date"
                type="date"
                required
                value={purchaseDate}
                onChange={(e) => setPurchaseDate(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="purchase-season" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Temporada <span className="text-[#ba1a1a]">*</span>
              </label>
              <select
                id="purchase-season"
                value={seasonId}
                onChange={(e) => setSeasonId(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              >
                {seasons.map((season) => (
                  <option key={season.id} value={season.id}>
                    {season.name}
                    {season.is_active ? ' · activa' : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {isOutOfSeasonRange && (
            <p role="status" className="text-[11px] text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-lg px-2.5 py-1.5 flex items-start gap-1.5">
              <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">warning</span>
              <span>
                La fecha queda fuera del rango de «{selectedSeason?.name}». Puedes guardarla igual; solo
                es un aviso.
              </span>
            </p>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="purchase-quantity" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Cantidad <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="purchase-quantity"
                type="number"
                min="0.01"
                step="0.01"
                required
                value={totalQuantity}
                onChange={(e) => setTotalQuantity(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="purchase-cost" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Coste total (€) <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="purchase-cost"
                type="number"
                min="0.01"
                step="0.01"
                required
                value={totalCost}
                onChange={(e) => setTotalCost(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>
          </div>

          {unitPrice !== null && (
            <p className="text-[11px] text-[#76786b] flex items-center gap-1.5">
              <span className="material-symbols-outlined text-sm" aria-hidden="true">calculate</span>
              Precio unitario: {unitPrice.toLocaleString('es-ES', { maximumFractionDigits: 4 })} € por unidad.
            </p>
          )}

          {shownError && (
            <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
              {shownError}
            </div>
          )}

          <div className="pt-3 flex items-center justify-end gap-3 border-t border-[#e5e2dd]">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-4 py-2 text-xs font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl disabled:opacity-60"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex items-center gap-2 px-5 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <span>{isSubmitting ? 'Guardando…' : 'Guardar cambios'}</span>
              <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
