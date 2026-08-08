import React, { useEffect, useMemo, useState } from 'react';
import type { Plot } from '../../types/plot.types';
import type { Season } from '../../types/season.types';
import type { ProductSuggestion, Purchase } from '../../types/purchase.types';
import {
  CONSUMPTION_PRODUCT_MAX_LENGTH,
  type Consumption,
} from '../../types/consumption.types';

/** Lo que el formulario devuelve; quien lo abre decide a qué endpoint va (MVP-304). */
export interface ConsumptionFormValues {
  date: string;
  plot_id: string;
  season_id: string;
  product: string;
  quantity: number;
}

interface ConsumptionFormModalProps {
  isOpen: boolean;
  /**
   * Compra que se está imputando; `null` para un consumo **sin compra previa** (RN-032). Es lo único
   * que cambia entre los dos modos: con compra, el producto y la temporada se heredan y hay una
   * cantidad máxima; sin compra, se escriben y el coste será 0.
   */
  purchase: Purchase | null;
  /** Consumo a corregir; `null` para alta. */
  consumption: Consumption | null;
  plots: Plot[];
  seasons: Season[];
  activeSeason: Season | null;
  /**
   * MVP-708 (`P-057`) — Vocabulario de materiales del Workspace (RN-031), el mismo que usa el alta de
   * compra. Solo se ofrece cuando el material se escribe: al imputar lo pone la compra.
   */
  suggestions: ProductSuggestion[];
  /** Cantidad ya imputada de la compra, para saber cuánto queda por repartir (CA-1). */
  pendingQuantity: number | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (values: ConsumptionFormValues) => void;
}

function todayIso(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
}

/** Fecha `YYYY-MM-DD` en formato corto, para poder nombrar la de la compra dentro del aviso. */
function formatDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

/**
 * Imputación de una compra a un terreno y consumo sin compra previa (MVP-304).
 *
 * Es **un solo formulario** porque es un solo hecho: «se ha consumido X de Y en el terreno Z el día
 * D». Lo que cambia es de dónde sale el coste, y eso el formulario lo dice explícitamente en vez de
 * esconderlo: con compra muestra el coste proporcional que se va a guardar y cuánto queda por
 * repartir; sin compra avisa de que el coste será 0 (CA-2).
 */
export const ConsumptionFormModal: React.FC<ConsumptionFormModalProps> = ({
  isOpen,
  purchase,
  consumption,
  plots,
  seasons,
  activeSeason,
  suggestions,
  pendingQuantity,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const isEdit = consumption !== null;
  // Al corregir, un consumo con compra sigue heredando de ella: no se puede cambiar su producto.
  const inheritsFromPurchase = purchase !== null || (isEdit && consumption.has_purchase);

  const [date, setDate] = useState(todayIso());
  const [plotId, setPlotId] = useState('');
  const [seasonId, setSeasonId] = useState('');
  const [product, setProduct] = useState('');
  const [quantity, setQuantity] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    setDate(consumption?.date ?? todayIso());
    setPlotId(consumption?.plot_id ?? plots[0]?.id ?? '');
    setSeasonId(
      consumption?.season_id ?? purchase?.season_id ?? activeSeason?.id ?? seasons[0]?.id ?? ''
    );
    setProduct(consumption?.product ?? purchase?.product ?? '');
    setQuantity(consumption ? String(consumption.quantity) : '');
    setLocalError(null);
  }, [isOpen, consumption, purchase, plots, seasons, activeSeason]);

  const selectedSeason = useMemo(
    () => seasons.find((season) => season.id === seasonId) ?? null,
    [seasons, seasonId]
  );

  // RN-023 — aviso, nunca bloqueo.
  const isOutOfSeasonRange = useMemo(() => {
    if (!selectedSeason || !date) return false;
    if (date < selectedSeason.start_date) return true;
    return selectedSeason.end_date !== null && date > selectedSeason.end_date;
  }, [selectedSeason, date]);

  /**
   * RN-043 (MVP-708, `P-058`) — Fecha de la compra que paga este consumo: la que se está imputando o,
   * al corregir, la que el consumo ya arrastra. `null` en un consumo sin compra previa, donde no hay
   * nada contra lo que comparar.
   */
  const purchaseDate = purchase?.purchase_date ?? consumption?.purchase_date ?? null;

  // Aviso, nunca bloqueo: la captura retroactiva es legítima (RN-032) y quien imputa una compra vieja
  // sabe lo que hace. Se compara como texto porque las dos fechas son `YYYY-MM-DD`, igual que el
  // aviso de temporada de arriba.
  const isBeforePurchaseDate = useMemo(
    () => purchaseDate !== null && date !== '' && date < purchaseDate,
    [purchaseDate, date]
  );

  const unitPrice = purchase?.unit_price ?? consumption?.unit_price ?? 0;

  const projectedCost = useMemo(() => {
    const parsed = Number(quantity);
    if (!Number.isFinite(parsed) || parsed <= 0) return null;
    return Math.round(parsed * unitPrice * 100) / 100;
  }, [quantity, unitPrice]);

  if (!isOpen) return null;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const parsed = Number(quantity);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setLocalError('La cantidad consumida debe ser mayor que 0.');
      return;
    }
    if (!product.trim()) {
      setLocalError('Escribe el producto o material consumido.');
      return;
    }

    setLocalError(null);
    onSubmit({
      date,
      plot_id: plotId,
      season_id: seasonId,
      product: product.trim(),
      quantity: parsed,
    });
  };

  const shownError = localError ?? errorMessage;
  const title = isEdit
    ? 'Corregir consumo'
    : inheritsFromPurchase
      ? 'Imputar compra a un terreno'
      : 'Registrar consumo sin compra';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-lg w-full border border-[#e5e2dd] shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl" aria-hidden="true">
              {inheritsFromPurchase ? 'call_split' : 'inventory_2'}
            </span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">{title}</h3>
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
          {/* Con compra, el material no se elige: lo pone la compra */}
          {inheritsFromPurchase ? (
            <div className="bg-[#f6f3ee] border border-[#e5e2dd] rounded-xl px-3.5 py-2.5">
              <p className="text-[10px] font-bold uppercase tracking-wider text-[#76786b]">Material</p>
              <p className="text-sm font-bold text-[#1c1c19]">{product}</p>
              {pendingQuantity !== null && (
                <p className="text-[11px] text-[#76786b] mt-0.5">
                  Quedan {pendingQuantity.toLocaleString('es-ES')} por repartir de esta compra ·
                  {' '}{unitPrice.toLocaleString('es-ES', { maximumFractionDigits: 4 })} € por unidad.
                </p>
              )}
            </div>
          ) : (
            <div className="space-y-1.5">
              <label htmlFor="consumption-product" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Producto o material <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="consumption-product"
                type="text"
                required
                list="consumption-product-options"
                maxLength={CONSUMPTION_PRODUCT_MAX_LENGTH}
                value={product}
                onChange={(e) => setProduct(e.target.value)}
                placeholder="ej. Abono que había en la nave"
                disabled={isSubmitting}
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
              {/* MVP-708 (`P-057`) — El mismo vocabulario que el alta de compra (RN-031). Aquí no lo
                  había, y era justo el campo donde nacen los nombres nuevos: sin sugerencias, «Abono
                  NPK» comprado y «abono npk» consumido convivían sin que nadie lo notara. Sigue
                  siendo texto libre: sugiere, no impone. */}
              <datalist id="consumption-product-options">
                {suggestions.map((suggestion) => (
                  <option key={suggestion.product} value={suggestion.product} />
                ))}
              </datalist>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="consumption-plot" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Terreno <span className="text-[#ba1a1a]">*</span>
              </label>
              <select
                id="consumption-plot"
                value={plotId}
                onChange={(e) => setPlotId(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              >
                {plots.map((plot) => (
                  <option key={plot.id} value={plot.id}>{plot.name}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label htmlFor="consumption-quantity" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Cantidad consumida <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="consumption-quantity"
                type="number"
                min="0.01"
                step="0.01"
                required
                autoFocus
                value={quantity}
                onChange={(e) => setQuantity(e.target.value)}
                placeholder="Aproximada"
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="consumption-date" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Fecha <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="consumption-date"
                type="date"
                required
                value={date}
                onChange={(e) => setDate(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="consumption-season" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Temporada <span className="text-[#ba1a1a]">*</span>
              </label>
              <select
                id="consumption-season"
                value={seasonId}
                onChange={(e) => setSeasonId(e.target.value)}
                /* Con compra la temporada la hereda de ella: cambiarla aquí desalinearía el reparto */
                disabled={isSubmitting || (inheritsFromPurchase && !isEdit)}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              >
                {seasons.map((season) => (
                  <option key={season.id} value={season.id}>
                    {season.name}
                    {season.is_working ? ' · en curso' : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {isOutOfSeasonRange && (
            <p role="status" className="text-[11px] text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-lg px-2.5 py-1.5 flex items-start gap-1.5">
              <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">warning</span>
              <span>
                La fecha queda fuera del rango de «{selectedSeason?.name}». Puedes guardarla igual;
                solo es un aviso.
              </span>
            </p>
          )}

          {/* RN-043 (`P-058`) — Mismo trato que RN-023: se avisa y se deja guardar. Apuntar hoy lo que
              se gastó la semana pasada es normal (RN-032 ya asume que el papeleo va por detrás del
              campo), pero gastar algo *antes* de comprarlo casi siempre es un tecleo en la fecha. */}
          {isBeforePurchaseDate && purchaseDate !== null && (
            <p role="status" className="text-[11px] text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-lg px-2.5 py-1.5 flex items-start gap-1.5">
              <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">warning</span>
              <span>
                Este consumo es anterior a su compra, del {formatDate(purchaseDate)}. Puedes
                guardarlo igual; revisa la fecha por si se te ha colado un año.
              </span>
            </p>
          )}

          {/* CA-2 — el aviso de RN-032: sin compra el coste es 0 porque se desconoce */}
          {inheritsFromPurchase ? (
            projectedCost !== null && (
              <p className="text-xs text-[#45483c] bg-[#eef2e0] border border-[#c9dba0] rounded-xl px-3 py-2 flex items-start gap-1.5">
                <span className="material-symbols-outlined text-base shrink-0" aria-hidden="true">calculate</span>
                <span>
                  Coste proporcional que se imputará:{' '}
                  <strong>{projectedCost.toLocaleString('es-ES', { minimumFractionDigits: 2 })} €</strong>.
                </span>
              </p>
            )
          ) : (
            <p role="status" className="text-xs text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-xl px-3 py-2 flex items-start gap-1.5">
              <span className="material-symbols-outlined text-base shrink-0" aria-hidden="true">info</span>
              <span>
                Como no hay una compra detrás, este consumo se guardará con <strong>coste 0</strong>:
                queda el registro de qué se gastó y dónde, pero no cuánto costó. Si registras la compra
                más adelante, este consumo <strong>no</strong> se recalculará.
              </span>
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
              <span>
                {isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Registrar consumo'}
              </span>
              <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
