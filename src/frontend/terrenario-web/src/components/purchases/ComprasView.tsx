import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { createPurchaseService } from '../../services/purchase.service';
import { HttpError } from '../../services/http-client';
import { CONFLICT_VERSION_MISMATCH } from '../../types/activity.types';
import {
  PURCHASE_PRODUCT_MAX_LENGTH,
  type CreatePurchasePayload,
  type ProductSuggestion,
  type Purchase,
} from '../../types/purchase.types';
import { PurchaseFormModal } from './PurchaseFormModal';

function todayIso(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
}

function formatDate(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

const euros = (value: number) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * Libro de compras del Workspace (MVP-303).
 *
 * Sigue la estructura de `ComprasView` del prototipo —cabecera con el gasto acumulado, formulario de
 * alta **en línea** y tabla del histórico— porque apuntar gastos es escribir varias líneas seguidas y
 * abrir un modal por cada una sería fricción pura (mismo razonamiento que el catálogo de tareas en
 * MVP-205). Corregir sí abre modal: es una acción puntual sobre una fila y en una tabla de seis
 * columnas la edición en línea sería peor.
 *
 * El producto es **texto libre** (RN-031) con sugerencias del histórico en un `datalist`: ayudan a
 * repetir el mismo nombre sin convertirse en un catálogo cerrado.
 *
 * La imputación por terrenos y el consumo sin compra previa son alcance de `MVP-304`; el borrado con
 * confirmación, de `MVP-305`.
 */
export const ComprasView: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const { seasons, activeSeason } = useSeason();
  const purchaseService = useMemo(() => createPurchaseService(http), [http]);

  const [purchases, setPurchases] = useState<Purchase[]>([]);
  const [totalCost, setTotalCost] = useState(0);
  const [suggestions, setSuggestions] = useState<ProductSuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [seasonFilter, setSeasonFilter] = useState('todas');
  const [productFilter, setProductFilter] = useState('');

  // Alta en línea (prototipo)
  const [newDate, setNewDate] = useState(todayIso());
  const [newProduct, setNewProduct] = useState('');
  const [newQuantity, setNewQuantity] = useState('');
  const [newCost, setNewCost] = useState('');
  const [isCreating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const newProductInput = useRef<HTMLInputElement>(null);
  /**
   * Contador de altas correctas. Existe solo para devolver el foco al producto **después** de que
   * React haya vuelto a renderizar: llamar a `focus()` dentro del propio manejador no funcionaría,
   * porque en ese momento el input todavía está `disabled` y enfocar un elemento deshabilitado no
   * hace nada. Ver `MVP-999`, `P-053`.
   */
  const [createdCount, setCreatedCount] = useState(0);

  const [editing, setEditing] = useState<Purchase | null>(null);
  const [isSubmitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const [list, productList] = await Promise.all([
        purchaseService.listPurchases({
          seasonId: seasonFilter === 'todas' ? undefined : seasonFilter,
          product: productFilter.trim() || undefined,
        }),
        purchaseService.listProductSuggestions(),
      ]);
      setPurchases(list.data);
      setTotalCost(list.meta.total_cost);
      setSuggestions(productList);
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudo cargar el libro de compras.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [purchaseService, seasonFilter, productFilter]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    if (createdCount > 0) newProductInput.current?.focus();
  }, [createdCount]);

  // RN-021 — la temporada de una compra nueva es la activa del Workspace, sin preguntarla en el
  // formulario en línea: se puede cambiar después al corregir, que es el caso raro.
  const defaultSeasonId = activeSeason?.id ?? seasons[0]?.id ?? null;

  const handleCreate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (isCreating || !defaultSeasonId) return;

    const quantity = Number(newQuantity);
    const cost = Number(newCost);
    if (!newProduct.trim()) {
      setCreateError('Escribe el producto o material comprado.');
      return;
    }
    if (!Number.isFinite(quantity) || quantity <= 0 || !Number.isFinite(cost) || cost <= 0) {
      setCreateError('La cantidad y el coste deben ser mayores que 0.');
      return;
    }

    setCreating(true);
    setCreateError(null);
    try {
      await purchaseService.createPurchase({
        purchase_date: newDate,
        season_id: defaultSeasonId,
        product: newProduct.trim(),
        total_quantity: quantity,
        total_cost: cost,
      });
      setNewProduct('');
      setNewQuantity('');
      setNewCost('');
      await reload();
      // El foco vuelve al producto (apuntar gastos es escribir varias líneas seguidas), pero se pide
      // por efecto, no aquí: ver `createdCount`.
      setCreatedCount((count) => count + 1);
    } catch (error) {
      setCreateError(
        error instanceof HttpError ? error.message : 'No se pudo registrar la compra.'
      );
    } finally {
      setCreating(false);
    }
  };

  const handleUpdate = async (payload: CreatePurchasePayload) => {
    if (!editing) return;
    setSubmitting(true);
    setFormError(null);
    try {
      await purchaseService.updatePurchase(editing.id, editing.version, payload);
      setEditing(null);
      await reload();
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        // ADR-0005 — se recarga y se explica, en vez de dejar un formulario que ya no puede guardar.
        setEditing(null);
        await reload();
        setLoadError(
          'Otra persona modificó esa compra mientras la editabas. Se ha recargado el libro con la versión actual; revisa el cambio y vuelve a aplicarlo si hace falta.'
        );
        return;
      }
      setFormError(error instanceof HttpError ? error.message : 'No se pudo guardar la compra.');
    } finally {
      setSubmitting(false);
    }
  };

  const hasSeason = defaultSeasonId !== null;

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera con el gasto acumulado (prototipo) */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Compras e insumos</h2>
          <p className="text-xs text-[#76786b]">
            Libro de gastos de abonos, fitosanitarios, combustible y material. El reparto por terrenos
            llegará después.
          </p>
        </div>

        <div className="bg-[#f6f3ee] px-4 py-2 rounded-xl border border-[#e5e2dd] shrink-0">
          <p className="text-[10px] font-bold text-[#76786b] uppercase">Gasto acumulado</p>
          <p className="font-headline font-extrabold text-lg text-[#ba1a1a]">{euros(totalCost)} €</p>
        </div>
      </div>

      {/* Registrar exige temporada (RN-021): se dice y se enlaza en vez de fallar al guardar */}
      {!isLoading && !hasSeason && (
        <div className="bg-[#fff6e5] border border-[#f0d9a8] rounded-2xl p-4 space-y-2">
          <p className="text-sm font-semibold text-[#8a5a00] flex items-center gap-1.5">
            <span className="material-symbols-outlined text-lg" aria-hidden="true">info</span>
            Toda compra se agrupa por campaña, así que antes necesitas una temporada.
          </p>
          <button
            type="button"
            onClick={() => navigate('/app/temporadas')}
            className="px-3 py-1.5 rounded-lg bg-white border border-[#f0d9a8] text-xs font-semibold text-[#8a5a00] hover:bg-[#fdf0d8]"
          >
            Crear temporada
          </button>
        </div>
      )}

      {/* Alta en línea (prototipo) */}
      {hasSeason && (
        <form
          onSubmit={(e) => void handleCreate(e)}
          className="bg-white p-4 rounded-2xl border border-[#e5e2dd] space-y-3"
        >
          <h3 className="font-headline font-bold text-sm text-[#1c1c19]">Registrar compra</h3>

          <div className="grid grid-cols-1 sm:grid-cols-6 gap-3">
            <div className="sm:col-span-2">
              <label htmlFor="new-purchase-product" className="sr-only">Producto o material</label>
              <input
                id="new-purchase-product"
                ref={newProductInput}
                type="text"
                required
                list="new-purchase-product-options"
                maxLength={PURCHASE_PRODUCT_MAX_LENGTH}
                value={newProduct}
                onChange={(e) => {
                  setNewProduct(e.target.value);
                  if (createError) setCreateError(null);
                }}
                placeholder="Producto (ej. Abono NPK)"
                disabled={isCreating}
                className="w-full px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
              {/* RN-031 — vocabulario del histórico: ayuda a repetir el mismo nombre, no lo impone */}
              <datalist id="new-purchase-product-options">
                {suggestions.map((suggestion) => (
                  <option key={suggestion.product} value={suggestion.product} />
                ))}
              </datalist>
            </div>

            <div>
              <label htmlFor="new-purchase-quantity" className="sr-only">Cantidad</label>
              <input
                id="new-purchase-quantity"
                type="number"
                min="0.01"
                step="0.01"
                required
                value={newQuantity}
                onChange={(e) => setNewQuantity(e.target.value)}
                placeholder="Cantidad"
                disabled={isCreating}
                className="w-full px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <div>
              <label htmlFor="new-purchase-cost" className="sr-only">Coste total (€)</label>
              <input
                id="new-purchase-cost"
                type="number"
                min="0.01"
                step="0.01"
                required
                value={newCost}
                onChange={(e) => setNewCost(e.target.value)}
                placeholder="Coste (€)"
                disabled={isCreating}
                className="w-full px-3.5 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <div>
              <label htmlFor="new-purchase-date" className="sr-only">Fecha</label>
              <input
                id="new-purchase-date"
                type="date"
                required
                value={newDate}
                onChange={(e) => setNewDate(e.target.value)}
                disabled={isCreating}
                className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-xs text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <button
              type="submit"
              disabled={isCreating}
              className="flex items-center justify-center gap-1.5 px-4 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <span className="material-symbols-outlined text-base" aria-hidden="true">add</span>
              <span>{isCreating ? 'Guardando…' : 'Registrar'}</span>
            </button>
          </div>

          <p className="text-[11px] text-[#76786b]">
            Se registrará en «{activeSeason?.name ?? seasons[0]?.name}»; puedes cambiar la campaña al
            corregir la compra.
          </p>

          {createError && (
            <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
              {createError}
            </div>
          )}
        </form>
      )}

      {/* Filtros */}
      {(purchases.length > 0 || productFilter || seasonFilter !== 'todas') && (
        <div className="flex flex-col sm:flex-row gap-3">
          <div className="flex-1 bg-white p-3 rounded-2xl border border-[#e5e2dd] flex items-center gap-3">
            <span className="material-symbols-outlined text-[#76786b] pl-2" aria-hidden="true">search</span>
            <label htmlFor="purchase-search" className="sr-only">Buscar material</label>
            <input
              id="purchase-search"
              type="text"
              value={productFilter}
              onChange={(e) => setProductFilter(e.target.value)}
              placeholder="Buscar material…"
              className="w-full bg-transparent text-xs font-medium text-[#1c1c19] focus:outline-none"
            />
            {productFilter && (
              <button onClick={() => setProductFilter('')} className="text-xs text-[#76786b] pr-2">
                Limpiar
              </button>
            )}
          </div>

          <div className="sm:w-56">
            <label htmlFor="purchase-season-filter" className="sr-only">Filtrar por temporada</label>
            <select
              id="purchase-season-filter"
              value={seasonFilter}
              onChange={(e) => setSeasonFilter(e.target.value)}
              className="w-full px-3 py-2.5 bg-white border border-[#e5e2dd] rounded-2xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              <option value="todas">Todas las temporadas</option>
              {seasons.map((season) => (
                <option key={season.id} value={season.id}>{season.name}</option>
              ))}
            </select>
          </div>
        </div>
      )}

      {loadError && (
        <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {/* Histórico */}
      {isLoading ? (
        <div className="flex items-center justify-center py-16">
          <div className="w-8 h-8 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
        </div>
      ) : purchases.length === 0 ? (
        <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-3">
          <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
            <span className="material-symbols-outlined text-3xl" aria-hidden="true">receipt_long</span>
          </div>
          <h3 className="font-headline font-bold text-lg text-[#1c1c19]">
            {productFilter || seasonFilter !== 'todas'
              ? 'No hay compras que coincidan'
              : 'Todavía no has registrado compras'}
          </h3>
          <p className="text-sm text-[#45483c] max-w-md mx-auto">
            {productFilter || seasonFilter !== 'todas'
              ? 'Prueba a cambiar el material buscado o la campaña.'
              : 'Apunta arriba lo que compras para la explotación: abonos, fitosanitarios, combustible o material.'}
          </p>
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-[#e5e2dd] ambient-shadow overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs text-[#1c1c19]">
              <thead className="bg-[#f6f3ee] border-b border-[#e5e2dd] text-[11px] font-bold uppercase tracking-wider text-[#45483c]">
                <tr>
                  <th scope="col" className="px-5 py-3.5">Fecha</th>
                  <th scope="col" className="px-5 py-3.5">Producto</th>
                  <th scope="col" className="px-5 py-3.5">Campaña</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Cantidad</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Precio ud.</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Coste</th>
                  <th scope="col" className="px-5 py-3.5 text-right sr-only">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#f0ede8]">
                {purchases.map((purchase) => (
                  <tr key={purchase.id} className="hover:bg-[#fcf9f4]">
                    <td className="px-5 py-3.5 font-medium text-[#76786b] whitespace-nowrap">
                      {formatDate(purchase.purchase_date)}
                    </td>
                    <td className="px-5 py-3.5 font-bold text-[#1c1c19]">{purchase.product}</td>
                    <td className="px-5 py-3.5">
                      <span className="px-2.5 py-0.5 rounded-full bg-[#f0ede8] text-[#33450d] font-semibold text-[11px] whitespace-nowrap">
                        {purchase.season_name}
                      </span>
                      {purchase.is_out_of_season_range && (
                        <span
                          title="La fecha queda fuera del rango de la temporada"
                          className="ml-1.5 px-2 py-0.5 rounded-full bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8] font-semibold text-[10px] whitespace-nowrap"
                        >
                          fuera de rango
                        </span>
                      )}
                    </td>
                    <td className="px-5 py-3.5 text-right text-[#45483c] whitespace-nowrap">
                      {purchase.total_quantity.toLocaleString('es-ES')}
                    </td>
                    <td className="px-5 py-3.5 text-right text-[#76786b] whitespace-nowrap">
                      {purchase.unit_price.toLocaleString('es-ES', { maximumFractionDigits: 4 })} €
                    </td>
                    <td className="px-5 py-3.5 text-right font-extrabold text-[#ba1a1a] whitespace-nowrap">
                      - {euros(purchase.total_cost)} €
                    </td>
                    <td className="px-3 py-3.5 text-right">
                      <button
                        type="button"
                        onClick={() => {
                          setFormError(null);
                          setEditing(purchase);
                        }}
                        title="Corregir compra"
                        aria-label={`Corregir la compra de ${purchase.product}`}
                        className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors"
                      >
                        <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <PurchaseFormModal
        isOpen={editing !== null}
        purchase={editing}
        seasons={seasons}
        suggestions={suggestions}
        isSubmitting={isSubmitting}
        errorMessage={formError}
        onClose={() => setEditing(null)}
        onSubmit={(payload) => void handleUpdate(payload)}
      />
    </div>
  );
};
