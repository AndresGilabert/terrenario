import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useSeason } from '../../contexts/SeasonContext';
import { createPurchaseService } from '../../services/purchase.service';
import { HttpError } from '../../services/http-client';
import { useSeasonScope } from '../../lib/season-scope';
import { useListUrlState } from '../../lib/list-url-state';
import { ALL_SEASONS } from '../../types/season.types';
import { CONFLICT_VERSION_MISMATCH } from '../../types/activity.types';
import {
  PURCHASE_PRODUCT_MAX_LENGTH,
  type CreatePurchasePayload,
  type ProductSuggestion,
  type Purchase,
} from '../../types/purchase.types';
import { createPlotService } from '../../services/plot.service';
import { createConsumptionService } from '../../services/consumption.service';
import type { Plot } from '../../types/plot.types';
import type { Consumption } from '../../types/consumption.types';
import { ConfirmDialog } from '../common/ConfirmDialog';
import { MobileDisclosure } from '../common/MobileDisclosure';
import { RecordCard, RecordCardList } from '../common/RecordCard';
import { useIsWide } from '../../lib/use-media-query';
import { PurchaseFormModal } from './PurchaseFormModal';
import { ConsumptionFormModal, type ConsumptionFormValues } from './ConsumptionFormModal';
import { fechaDeNegocio } from '../../lib/fechas';

/** Registro pendiente de confirmación de borrado (MVP-305, RN-037). */
type PendingDelete =
  | { kind: 'purchase'; purchase: Purchase }
  | { kind: 'consumption'; consumption: Consumption };

function todayIso(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
}

const euros = (value: number) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * MVP-802 — Los filtros del libro de compras en la URL (RN-007). Son los mismos para las compras y
 * para su bloque de consumos: las dos listas conviven en esta pantalla y hablar de campañas distintas
 * sería el propio `P-082` dentro de una sola vista.
 */
const PURCHASE_URL_SPEC = {
  filters: {
    seasonSelection: { param: 'season_id', fallback: '' },
  },
  search: 'product',
} as const;

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
  const consumptionService = useMemo(() => createConsumptionService(http), [http]);
  const plotService = useMemo(() => createPlotService(http), [http]);

  const [purchases, setPurchases] = useState<Purchase[]>([]);
  const [totalCost, setTotalCost] = useState(0);
  const [suggestions, setSuggestions] = useState<ProductSuggestion[]>([]);
  const [consumptions, setConsumptions] = useState<Consumption[]>([]);
  const [consumptionsWithoutPurchase, setConsumptionsWithoutPurchase] = useState(0);
  const [plots, setPlots] = useState<Plot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  /**
   * MVP-802 (`P-109`) — La campaña y el material buscado viven en la **URL** (RN-007), con la misma
   * pieza que el diario y que Cosechas. Hasta aquí su estado vivía en memoria: recargar deshacía el
   * filtro y no había forma de compartir «mira estas compras».
   */
  const url = useListUrlState(PURCHASE_URL_SPEC);
  const { setFilter, setSearch, search: appliedProduct } = url;

  // MVP-701 (`P-082`) — El defecto de temporada lo resuelve el servidor (RN-008), igual que en el
  // dashboard: el libro de compras ya no arranca en «todas».
  //
  // MVP-802 — En modo controlado desde que la elección vive en la URL, con la corrección de `MVP-801`
  // conectada: llevar el filtro a la URL es justo lo que expone `P-108`, y sin `onCorrect` esta vista
  // lo estrenaría (CA-5).
  const seasonScope = useSeasonScope({
    selection: url.values.seasonSelection,
    onSelect: useCallback((value: string) => setFilter({ seasonSelection: value }), [setFilter]),
    onCorrect: useCallback(
      (value: string) => setFilter({ seasonSelection: value }, { replace: true }),
      [setFilter]
    ),
  });
  // Desestructurado para que las dependencias de `reload` sean identificadores estables y la regla de
  // exhaustividad de los hooks pueda comprobarlas.
  const { requested: seasonRequested, applyFromResponse: applySeasonScope } = seasonScope;

  /**
   * Lo que se está tecleando. Es lo **único** de la navegación que no vive en la URL: escribirlo allí
   * en cada pulsación llenaría el historial. El término ya rebotado sí viaja, y de ahí sale
   * `appliedProduct`.
   *
   * MVP-802 — El rebote es nuevo aquí: hasta ahora cada pulsación disparaba una petición de compras,
   * otra de consumos y un repintado del libro. Es la condición de higiene de `RN-007` que el diario ya
   * cumplía, y la que impide que llevar la búsqueda a la URL deje una entrada de historial por letra.
   */
  const [productFilter, setProductFilter] = useState(appliedProduct);

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

  /** Consumo en curso (MVP-304): imputación de una compra, consumo sin compra o corrección. */
  const [consumptionForm, setConsumptionForm] = useState<{
    purchase: Purchase | null;
    consumption: Consumption | null;
  } | null>(null);
  const [isSubmittingConsumption, setSubmittingConsumption] = useState(false);
  const [consumptionError, setConsumptionError] = useState<string | null>(null);

  // MVP-803 — Por encima de `lg:` las dos tablas caben; por debajo, las listas se leen como tarjetas.
  const isWide = useIsWide();

  const [pendingDelete, setPendingDelete] = useState<PendingDelete | null>(null);
  const [isDeleting, setDeleting] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const [list, productList, consumptionList, plotList] = await Promise.all([
        purchaseService.listPurchases({
          seasonId: seasonRequested,
          product: appliedProduct || undefined,
        }),
        purchaseService.listProductSuggestions(),
        consumptionService.listConsumptions({
          // El mismo ámbito que el libro: las dos listas conviven en esta pantalla y hablar de
          // campañas distintas sería el propio `P-082` dentro de una sola vista.
          seasonId: seasonRequested,
          // R-06 (MVP-399) — el buscador de material filtraba las compras pero no los consumos.
          product: appliedProduct || undefined,
        }),
        // Solo los activos: es lo que se ofrece para registros nuevos (MVP-202, CA-3).
        plotService.listPlots({ isActive: true }),
      ]);
      setPurchases(list.data);
      setTotalCost(list.meta.total_cost);
      applySeasonScope(list.meta.scope);
      setSuggestions(productList);
      setConsumptions(consumptionList.data);
      setConsumptionsWithoutPurchase(consumptionList.meta.without_purchase);
      setPlots(plotList);
    } catch (error) {
      setLoadError(
        error instanceof HttpError ? error.message : 'No se pudo cargar el libro de compras.'
      );
    } finally {
      setIsLoading(false);
    }
  }, [
    purchaseService,
    consumptionService,
    plotService,
    seasonRequested,
    applySeasonScope,
    appliedProduct,
  ]);

  useEffect(() => {
    void reload();
  }, [reload]);

  /**
   * MVP-802 — La búsqueda viaja al servidor y a la URL cuando la persona para de teclear, no en cada
   * pulsación, y **sustituyendo** la entrada de historial (RN-007). Mismo rebote y mismo criterio que
   * el diario desde `MVP-506`/`MVP-705`.
   */
  useEffect(() => {
    if (productFilter.trim() === appliedProduct) return;

    const timer = setTimeout(() => setSearch(productFilter.trim()), 350);
    return () => clearTimeout(timer);
  }, [productFilter, appliedProduct, setSearch]);

  /**
   * Cuando la URL cambia **por fuera** —«atrás», «adelante», o un enlace pegado— el cuadro de búsqueda
   * tiene que seguirla. La comparación con lo tecleado evita que este efecto pise lo que se escribe:
   * mientras se teclea, la URL todavía no ha cambiado.
   */
  useEffect(() => {
    setProductFilter((current) => (current.trim() === appliedProduct ? current : appliedProduct));
  }, [appliedProduct]);

  useEffect(() => {
    if (createdCount > 0) newProductInput.current?.focus();
  }, [createdCount]);

  // RN-021 — la temporada de una compra nueva es la activa del Workspace, sin preguntarla en el
  // formulario en línea: se puede cambiar después al corregir, que es el caso raro.
  const defaultSeasonId = activeSeason?.id ?? seasons[0]?.id ?? null;

  /**
   * MVP-803 — Las acciones se disparan igual desde la tabla y desde la tarjeta. Se nombran aquí para
   * que las dos maquetas compartan el gesto: una acción con dos implementaciones es una acción que
   * puede empezar a comportarse distinto según el ancho de la pantalla.
   */
  const askImpute = (purchase: Purchase) => {
    setConsumptionError(null);
    setConsumptionForm({ purchase, consumption: null });
  };
  const askEditPurchase = (purchase: Purchase) => {
    setFormError(null);
    setEditing(purchase);
  };
  const askDeletePurchase = (purchase: Purchase) => {
    setDeleteError(null);
    setPendingDelete({ kind: 'purchase', purchase });
  };
  const askEditConsumption = (consumption: Consumption) => {
    setConsumptionError(null);
    setConsumptionForm({ purchase: null, consumption });
  };
  const askDeleteConsumption = (consumption: Consumption) => {
    setDeleteError(null);
    setPendingDelete({ kind: 'consumption', consumption });
  };

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

  /**
   * MVP-304 — Guarda el consumo por la ruta que corresponde: imputación si cuelga de una compra,
   * consumo sin compra si no (RN-032), o corrección si ya existía.
   */
  const handleConsumptionSubmit = async (values: ConsumptionFormValues) => {
    if (!consumptionForm) return;
    const { purchase, consumption } = consumptionForm;

    setSubmittingConsumption(true);
    setConsumptionError(null);
    try {
      if (consumption) {
        await consumptionService.updateConsumption(consumption.id, consumption.version, {
          date: values.date,
          plot_id: values.plot_id,
          season_id: values.season_id,
          product: values.product,
          quantity: values.quantity,
        });
      } else if (purchase) {
        await consumptionService.imputePurchase(purchase.id, {
          date: values.date,
          plot_id: values.plot_id,
          quantity: values.quantity,
        });
      } else {
        await consumptionService.registerConsumption(values);
      }
      setConsumptionForm(null);
      await reload();
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        setConsumptionForm(null);
        await reload();
        setLoadError(
          'Otra persona modificó ese consumo mientras lo editabas. Se ha recargado la pantalla con la versión actual.'
        );
        return;
      }
      // El 400 de sobre-imputación llega con el margen disponible en el mensaje: se muestra tal cual.
      setConsumptionError(
        error instanceof HttpError ? error.message : 'No se pudo guardar el consumo.'
      );
    } finally {
      setSubmittingConsumption(false);
    }
  };

  /**
   * MVP-305 (RN-037, CA-3) — Borrado **lógico** tras confirmación explícita, también desde los
   * listados y no solo desde el diario. Una compra con imputaciones vivas responde 422 (MVP-304) y el
   * mensaje se muestra dentro del diálogo, que es donde se está decidiendo.
   */
  const confirmDelete = async () => {
    if (!pendingDelete) return;

    setDeleting(true);
    setDeleteError(null);
    try {
      if (pendingDelete.kind === 'purchase') {
        await purchaseService.deletePurchase(
          pendingDelete.purchase.id, pendingDelete.purchase.version);
      } else {
        await consumptionService.deleteConsumption(
          pendingDelete.consumption.id, pendingDelete.consumption.version);
      }
      setPendingDelete(null);
      await reload();
    } catch (error) {
      if (error instanceof HttpError && error.code === CONFLICT_VERSION_MISMATCH) {
        setPendingDelete(null);
        await reload();
        setLoadError(
          'Otra persona modificó ese registro mientras lo mirabas. Se ha recargado la pantalla; revísalo antes de eliminarlo.'
        );
        return;
      }
      setDeleteError(
        error instanceof HttpError ? error.message : 'No se pudo eliminar el registro.'
      );
    } finally {
      setDeleting(false);
    }
  };

  const hasSeason = defaultSeasonId !== null;
  const canRegisterConsumption = hasSeason && plots.length > 0;

  return (
    <div className="space-y-6 pb-12">
      {/* Cabecera con el gasto acumulado (prototipo) */}
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">Compras e insumos</h2>
          <p className="text-xs text-[#76786b]">
            Libro de gastos de abonos, fitosanitarios, combustible y material, y reparto de lo
            consumido por terrenos.
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

      {/* Alta en línea (prototipo).

          MVP-702 — Plegada en móvil. Es el único formulario del producto que vive en línea en vez de
          en un modal —Diario y Cosechas abren el suyo desde un botón—, y sus ~335 px dejaban la
          primera fila del libro en el borde inferior de la pantalla: técnicamente visible, en la
          práctica no. Plegado se comporta como los otros dos, y en escritorio no cambia nada. */}
      {hasSeason && (
        <MobileDisclosure label="Registrar compra" icon="add">
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
        </MobileDisclosure>
      )}

      {/* Filtros */}
      {/* MVP-702 — Filtros plegados en móvil para que el libro se vea al entrar. */}
      {/* MVP-802 — Lo decide la URL: si hay parámetro, hay filtro. Los defectos no llegan a escribirse. */}
      {(purchases.length > 0 || url.hasFilters) && (
        <MobileDisclosure label="Filtros" icon="tune" activeCount={url.activeCount}>
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
              value={seasonScope.value}
              onChange={(e) => seasonScope.select(e.target.value)}
              className="w-full px-3 py-2.5 bg-white border border-[#e5e2dd] rounded-2xl text-xs font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d]"
            >
              {/* Hasta la primera respuesta no se sabe qué campaña aplica el servidor. */}
              {seasonScope.value === '' && <option value="">Campaña de trabajo…</option>}
              <option value={ALL_SEASONS}>Todas las temporadas</option>
              {seasons.map((season) => (
                <option key={season.id} value={season.id}>{season.name}</option>
              ))}
            </select>
          </div>
        </div>
        </MobileDisclosure>
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
            {url.hasFilters
              ? 'No hay compras que coincidan'
              : seasonScope.label
                ? `Sin compras en ${seasonScope.label}`
                : 'Todavía no has registrado compras'}
          </h3>
          <p className="text-sm text-[#45483c] max-w-md mx-auto">
            {url.hasFilters
              ? 'Prueba a cambiar el material buscado o la campaña.'
              : 'Apunta arriba lo que compras para la explotación: abonos, fitosanitarios, combustible o material.'}
          </p>
        </div>
      ) : !isWide ? (
        /* MVP-803 — La misma maqueta de tarjeta que Cosechas. El libro tiene ocho columnas y mide
           ~881 px: por debajo de `lg:` no cabía tampoco, aunque el `spec` de la historia diera por
           hecho que sí (se replanteó al medirlo, decisión del PO). */
        <RecordCardList label="Compras registradas">
          {purchases.map((purchase) => (
            <RecordCard
              key={purchase.id}
              title={purchase.product}
              subtitle={`${fechaDeNegocio(purchase.purchase_date)} · ${purchase.season_name}`}
              badges={
                purchase.is_out_of_season_range ? (
                  <span
                    title="La fecha queda fuera del rango de la temporada"
                    className="px-2 py-0.5 rounded-full bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8] font-semibold text-[10px]"
                  >
                    fuera de rango
                  </span>
                ) : undefined
              }
              highlight={
                <span className="font-extrabold text-base text-[#ba1a1a]">
                  - {euros(purchase.total_cost)} €
                </span>
              }
              fields={[
                {
                  label: 'Cantidad',
                  value: purchase.total_quantity.toLocaleString('es-ES'),
                },
                {
                  label: 'Precio ud.',
                  value: `${purchase.unit_price.toLocaleString('es-ES', { maximumFractionDigits: 4 })} €`,
                },
                {
                  // MVP-304 — cuánto se ha repartido ya por terrenos.
                  label: 'Imputado',
                  value: (
                    <span
                      className={
                        purchase.pending_quantity <= 0 ? 'text-[#33450d] font-semibold' : 'text-[#76786b]'
                      }
                    >
                      {purchase.imputed_quantity.toLocaleString('es-ES')} /{' '}
                      {purchase.total_quantity.toLocaleString('es-ES')}
                    </span>
                  ),
                },
              ]}
              actions={
                <PurchaseActions
                  purchase={purchase}
                  canImpute={plots.length > 0}
                  onImpute={askImpute}
                  onEdit={askEditPurchase}
                  onDelete={askDeletePurchase}
                />
              }
            />
          ))}
        </RecordCardList>
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
                  {/* MVP-304 — cuánto se ha repartido ya por terrenos */}
                  <th scope="col" className="px-5 py-3.5 text-right">Imputado</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Precio ud.</th>
                  <th scope="col" className="px-5 py-3.5 text-right">Coste</th>
                  <th scope="col" className="px-5 py-3.5 text-right sr-only">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#f0ede8]">
                {purchases.map((purchase) => (
                  <tr key={purchase.id} className="hover:bg-[#fcf9f4]">
                    <td className="px-5 py-3.5 font-medium text-[#76786b] whitespace-nowrap">
                      {fechaDeNegocio(purchase.purchase_date)}
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
                    <td
                      className={`px-5 py-3.5 text-right whitespace-nowrap ${
                        purchase.pending_quantity <= 0 ? 'text-[#33450d] font-semibold' : 'text-[#76786b]'
                      }`}
                      title={
                        purchase.pending_quantity <= 0
                          ? 'Toda la compra está repartida entre terrenos'
                          : `Quedan ${purchase.pending_quantity.toLocaleString('es-ES')} por repartir`
                      }
                    >
                      {purchase.imputed_quantity.toLocaleString('es-ES')} / {purchase.total_quantity.toLocaleString('es-ES')}
                    </td>
                    <td className="px-5 py-3.5 text-right text-[#76786b] whitespace-nowrap">
                      {purchase.unit_price.toLocaleString('es-ES', { maximumFractionDigits: 4 })} €
                    </td>
                    <td className="px-5 py-3.5 text-right font-extrabold text-[#ba1a1a] whitespace-nowrap">
                      - {euros(purchase.total_cost)} €
                    </td>
                    <td className="px-3 py-3.5 text-right whitespace-nowrap">
                      <PurchaseActions
                        purchase={purchase}
                        canImpute={plots.length > 0}
                        onImpute={askImpute}
                        onEdit={askEditPurchase}
                        onDelete={askDeletePurchase}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Consumos e imputaciones (MVP-304). Van debajo del libro y no en otra pantalla porque son
          la contrapartida de la compra: dónde acabó el material. */}
      <section className="space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Consumos por terreno</h3>
            <p className="text-xs text-[#76786b]">
              Dónde se ha gastado el material. Reparte una compra con{' '}
              <span className="material-symbols-outlined text-sm align-middle" aria-hidden="true">call_split</span>{' '}
              o apunta un consumo aunque no tengas la compra registrada.
            </p>
          </div>

          <button
            type="button"
            onClick={() => {
              setConsumptionError(null);
              setConsumptionForm({ purchase: null, consumption: null });
            }}
            disabled={!canRegisterConsumption}
            title={canRegisterConsumption ? undefined : 'Necesitas un terreno y una temporada'}
            className="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-xl bg-white border border-[#c6c8b8] hover:bg-[#f0ede8] text-[#45483c] text-xs font-semibold transition-colors disabled:opacity-60 disabled:cursor-not-allowed shrink-0"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">inventory_2</span>
            <span>Consumo sin compra</span>
          </button>
        </div>

        {/* CA-3 de la épica — el impacto en la calidad del dato queda visible, no escondido */}
        {consumptionsWithoutPurchase > 0 && (
          <p className="text-xs text-[#8a5a00] bg-[#fff6e5] border border-[#f0d9a8] rounded-xl px-3 py-2 flex items-start gap-1.5">
            <span className="material-symbols-outlined text-base shrink-0" aria-hidden="true">info</span>
            <span>
              {consumptionsWithoutPurchase === 1
                ? 'Hay 1 consumo registrado sin compra previa: su coste consta como 0 porque se desconoce.'
                : `Hay ${consumptionsWithoutPurchase} consumos registrados sin compra previa: su coste consta como 0 porque se desconoce.`}
            </span>
          </p>
        )}

        {consumptions.length === 0 ? (
          <p className="text-sm text-[#76786b] italic bg-white p-6 rounded-2xl border border-[#e5e2dd] text-center">
            Todavía no has repartido ninguna compra por terrenos.
          </p>
        ) : !isWide ? (
          <RecordCardList label="Consumos por terreno">
            {consumptions.map((consumption) => (
              <RecordCard
                key={consumption.id}
                title={consumption.product}
                subtitle={`${fechaDeNegocio(consumption.date)} · ${consumption.plot_name}`}
                badges={
                  <>
                    {!consumption.has_purchase && (
                      <span
                        title="Registrado sin compra previa: el coste se desconoce"
                        className="px-2 py-0.5 rounded-full bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8] font-semibold text-[10px]"
                      >
                        sin compra
                      </span>
                    )}
                    {/* RN-043 (MVP-708) — señala sin impedir, igual que en la tabla. */}
                    {consumption.is_before_purchase_date && (
                      <span
                        title={
                          consumption.purchase_date
                            ? `El consumo es anterior a su compra, del ${fechaDeNegocio(consumption.purchase_date)}`
                            : 'El consumo es anterior a su compra'
                        }
                        className="px-2 py-0.5 rounded-full bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8] font-semibold text-[10px]"
                      >
                        antes de la compra
                      </span>
                    )}
                  </>
                }
                highlight={
                  <span
                    className={
                      consumption.has_purchase
                        ? 'font-extrabold text-base text-[#ba1a1a]'
                        : 'text-xs text-[#76786b]'
                    }
                  >
                    {consumption.has_purchase
                      ? `- ${euros(consumption.proportional_cost)} €`
                      : 'sin coste'}
                  </span>
                }
                fields={[
                  { label: 'Cantidad', value: consumption.quantity.toLocaleString('es-ES') },
                  { label: 'Terreno', value: consumption.plot_name },
                ]}
                actions={
                  <ConsumptionActions
                    consumption={consumption}
                    onEdit={askEditConsumption}
                    onDelete={askDeleteConsumption}
                  />
                }
              />
            ))}
          </RecordCardList>
        ) : (
          <div className="bg-white rounded-2xl border border-[#e5e2dd] ambient-shadow overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs text-[#1c1c19]">
                <thead className="bg-[#f6f3ee] border-b border-[#e5e2dd] text-[11px] font-bold uppercase tracking-wider text-[#45483c]">
                  <tr>
                    <th scope="col" className="px-5 py-3.5">Fecha</th>
                    <th scope="col" className="px-5 py-3.5">Material</th>
                    <th scope="col" className="px-5 py-3.5">Terreno</th>
                    <th scope="col" className="px-5 py-3.5 text-right">Cantidad</th>
                    <th scope="col" className="px-5 py-3.5 text-right">Coste</th>
                    <th scope="col" className="px-5 py-3.5 text-right sr-only">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#f0ede8]">
                  {consumptions.map((consumption) => (
                    <tr key={consumption.id} className="hover:bg-[#fcf9f4]">
                      <td className="px-5 py-3.5 font-medium text-[#76786b] whitespace-nowrap">
                        {fechaDeNegocio(consumption.date)}
                      </td>
                      <td className="px-5 py-3.5 font-bold text-[#1c1c19]">
                        {consumption.product}
                        {!consumption.has_purchase && (
                          <span
                            title="Registrado sin compra previa: el coste se desconoce"
                            className="ml-1.5 px-2 py-0.5 rounded-full bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8] font-semibold text-[10px] whitespace-nowrap"
                          >
                            sin compra
                          </span>
                        )}
                        {/* RN-043 (MVP-708) — misma etiqueta discreta que «fuera de rango» en el
                            libro: señala sin impedir, y la fila se corrige con el lápiz de al lado */}
                        {consumption.is_before_purchase_date && (
                          <span
                            title={
                              consumption.purchase_date
                                ? `El consumo es anterior a su compra, del ${fechaDeNegocio(consumption.purchase_date)}`
                                : 'El consumo es anterior a su compra'
                            }
                            className="ml-1.5 px-2 py-0.5 rounded-full bg-[#fff6e5] text-[#8a5a00] border border-[#f0d9a8] font-semibold text-[10px] whitespace-nowrap"
                          >
                            antes de la compra
                          </span>
                        )}
                      </td>
                      <td className="px-5 py-3.5 text-[#45483c]">{consumption.plot_name}</td>
                      <td className="px-5 py-3.5 text-right text-[#45483c] whitespace-nowrap">
                        {consumption.quantity.toLocaleString('es-ES')}
                      </td>
                      <td
                        className={`px-5 py-3.5 text-right whitespace-nowrap ${
                          consumption.has_purchase ? 'font-extrabold text-[#ba1a1a]' : 'text-[#76786b]'
                        }`}
                      >
                        {consumption.has_purchase
                          ? `- ${euros(consumption.proportional_cost)} €`
                          : 'sin coste'}
                      </td>
                      <td className="px-3 py-3.5 text-right">
                        <ConsumptionActions
                          consumption={consumption}
                          onEdit={askEditConsumption}
                          onDelete={askDeleteConsumption}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </section>

      <ConsumptionFormModal
        isOpen={consumptionForm !== null}
        purchase={consumptionForm?.purchase ?? null}
        consumption={consumptionForm?.consumption ?? null}
        plots={plots}
        seasons={seasons}
        activeSeason={activeSeason}
        /* MVP-708 (`P-057`) — el mismo vocabulario que el alta en línea: es el mismo campo de texto
           libre (RN-031) y tener dos listas distintas en la misma pantalla era el origen del punto. */
        suggestions={suggestions}
        pendingQuantity={consumptionForm?.purchase?.pending_quantity ?? null}
        isSubmitting={isSubmittingConsumption}
        errorMessage={consumptionError}
        onClose={() => setConsumptionForm(null)}
        onSubmit={(values) => void handleConsumptionSubmit(values)}
      />

      {/* RN-037 (CA-3) — confirmación explícita antes de eliminar, también desde los listados */}
      <ConfirmDialog
        isOpen={pendingDelete !== null}
        title={pendingDelete?.kind === 'purchase' ? '¿Eliminar la compra?' : '¿Eliminar el consumo?'}
        message={
          pendingDelete && (
            <>
              <p>
                Vas a eliminar{' '}
                <strong>
                  «{pendingDelete.kind === 'purchase'
                    ? pendingDelete.purchase.product
                    : pendingDelete.consumption.product}»
                </strong>{' '}
                del{' '}
                {fechaDeNegocio(
                  pendingDelete.kind === 'purchase'
                    ? pendingDelete.purchase.purchase_date
                    : pendingDelete.consumption.date
                )}
                {pendingDelete.kind === 'consumption' ? ` en ${pendingDelete.consumption.plot_name}` : ''}.
              </p>
              <p className="text-xs text-[#76786b]">
                Desaparecerá del libro y del diario. No hay papelera: si te equivocas, tendrás que
                volver a registrarlo.
              </p>
            </>
          )
        }
        isBusy={isDeleting}
        errorMessage={deleteError}
        onCancel={() => {
          setPendingDelete(null);
          setDeleteError(null);
        }}
        onConfirm={() => void confirmDelete()}
      />

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

/**
 * MVP-803 (CA-4) — Acciones de una compra, con **la misma etiqueta accesible** en la tabla y en la
 * tarjeta. Se extraen a un componente por eso: dos copias del mismo trío de botones son dos sitios
 * donde la etiqueta puede dejar de nombrar la compra a la que apunta.
 */
const PurchaseActions: React.FC<{
  purchase: Purchase;
  /** Repartir exige al menos un terreno (MVP-304, HU-1). */
  canImpute: boolean;
  onImpute: (purchase: Purchase) => void;
  onEdit: (purchase: Purchase) => void;
  onDelete: (purchase: Purchase) => void;
}> = ({ purchase, canImpute, onImpute, onEdit, onDelete }) => (
  <>
    {/* MVP-304 (HU-1) — repartir la compra entre terrenos */}
    <button
      type="button"
      onClick={() => onImpute(purchase)}
      disabled={!canImpute || purchase.pending_quantity <= 0}
      title={
        !canImpute
          ? 'Necesitas al menos un terreno'
          : purchase.pending_quantity <= 0
            ? 'Toda la compra ya está repartida'
            : 'Imputar a un terreno'
      }
      aria-label={`Imputar la compra de ${purchase.product} a un terreno`}
      className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
    >
      <span className="material-symbols-outlined text-base" aria-hidden="true">call_split</span>
    </button>
    <button
      type="button"
      onClick={() => onEdit(purchase)}
      title="Corregir compra"
      aria-label={`Corregir la compra de ${purchase.product}`}
      className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors"
    >
      <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
    </button>
    <button
      type="button"
      onClick={() => onDelete(purchase)}
      title="Eliminar compra"
      aria-label={`Eliminar la compra de ${purchase.product}`}
      className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#ffdad6]/60 hover:text-[#ba1a1a] transition-colors"
    >
      <span className="material-symbols-outlined text-base" aria-hidden="true">delete</span>
    </button>
  </>
);

/** MVP-803 — Lo mismo para un consumo. */
const ConsumptionActions: React.FC<{
  consumption: Consumption;
  onEdit: (consumption: Consumption) => void;
  onDelete: (consumption: Consumption) => void;
}> = ({ consumption, onEdit, onDelete }) => {
  const nombre = `el consumo de ${consumption.product} en ${consumption.plot_name}`;
  return (
    <>
      <button
        type="button"
        onClick={() => onEdit(consumption)}
        title="Corregir consumo"
        aria-label={`Corregir ${nombre}`}
        className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#f0ede8] hover:text-[#33450d] transition-colors"
      >
        <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
      </button>
      <button
        type="button"
        onClick={() => onDelete(consumption)}
        title="Eliminar consumo"
        aria-label={`Eliminar ${nombre}`}
        className="p-1.5 rounded-lg text-[#76786b] hover:bg-[#ffdad6]/60 hover:text-[#ba1a1a] transition-colors"
      >
        <span className="material-symbols-outlined text-base" aria-hidden="true">delete</span>
      </button>
    </>
  );
};
