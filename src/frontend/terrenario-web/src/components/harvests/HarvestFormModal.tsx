import React, { useEffect, useMemo, useState } from 'react';
import type { Plot } from '../../types/plot.types';
import type { Season } from '../../types/season.types';
import {
  HARVEST_DESTINATIONS,
  HARVEST_PRODUCTS,
  HARVEST_YIELD_MAX,
  OIL_DENSITY_KG_PER_LITRE,
  harvestDestinationLabel,
  harvestProductLabel,
  type CreateHarvestPayload,
  type Harvest,
  type YieldInputMode,
} from '../../types/harvest.types';

interface HarvestFormModalProps {
  isOpen: boolean;
  /** Cosecha a corregir, o `null` para registrar una nueva. */
  harvest: Harvest | null;
  plots: Plot[];
  seasons: Season[];
  /** Temporada activa del Workspace: se autoselecciona en el alta (RN-021). */
  activeSeason: Season | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (payload: CreateHarvestPayload) => void;
}

const today = () => new Date().toISOString().slice(0, 10);

/**
 * Alta y corrección de una cosecha (MVP-401, HU-1/HU-2).
 *
 * Dos decisiones de forma que vienen de las reglas, no del gusto:
 *
 * - **Rendimiento y litros son un selector, no dos campos sueltos.** RN-004 los declara excluyentes,
 *   así que ofrecerlos a la vez invitaría a rellenar los dos y a recibir un error que el usuario no
 *   ha provocado. Se elige *cómo* se informa la producción de aceite —o no informarla— y solo aparece
 *   el campo que corresponde. Los tres modos son los tres orígenes que admite RN-014: L/100kg (la
 *   unidad canónica de RN-013), kg de aceite por 100 kg (el «rendimiento graso» que dan las almazaras,
 *   convertido en servidor con la densidad de RN-016) y litros obtenidos, de los que se deriva.
 * - **La fecha fuera del rango de la temporada avisa mientras se escribe** (RN-023) y nunca impide
 *   guardar: es el mismo aviso que ya dan la actividad y la compra.
 */
export const HarvestFormModal: React.FC<HarvestFormModalProps> = ({
  isOpen,
  harvest,
  plots,
  seasons,
  activeSeason,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const [date, setDate] = useState(today());
  const [plotId, setPlotId] = useState('');
  const [seasonId, setSeasonId] = useState('');
  const [product, setProduct] = useState<string>(HARVEST_PRODUCTS[0]);
  const [kgs, setKgs] = useState('');
  const [destination, setDestination] = useState<string>('desconocido');
  const [yieldMode, setYieldMode] = useState<YieldInputMode>('ninguno');
  const [yieldValue, setYieldValue] = useState('');
  const [fatYield, setFatYield] = useState('');
  const [liters, setLiters] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    setLocalError(null);

    if (harvest) {
      setDate(harvest.date);
      setPlotId(harvest.plot_id);
      setSeasonId(harvest.season_id);
      setProduct(harvest.product);
      setKgs(String(harvest.kgs));
      setDestination(harvest.destination);
      // Al corregir se ofrece la unidad canónica, que es la que se persistió (RN-013): mostrar el
      // valor original en kg/100kg exigiría guardar la unidad de entrada, y lo que compara el
      // dashboard es la canónica. Quien quiera volver a escribirlo en rendimiento graso tiene el modo
      // disponible.
      setYieldMode(
        harvest.yield !== null ? 'rendimiento' : harvest.liters !== null ? 'litros' : 'ninguno'
      );
      setYieldValue(harvest.yield !== null ? String(harvest.yield) : '');
      setFatYield('');
      setLiters(harvest.liters !== null ? String(harvest.liters) : '');
      return;
    }

    setDate(today());
    setPlotId(plots[0]?.id ?? '');
    // RN-021 — la temporada activa se autoselecciona para minimizar fricción.
    setSeasonId(activeSeason?.id ?? seasons[0]?.id ?? '');
    setProduct(HARVEST_PRODUCTS[0]);
    setKgs('');
    // RN-012 — no conocer todavía el cierre comercial no puede retrasar el registro.
    setDestination('desconocido');
    setYieldMode('ninguno');
    setYieldValue('');
    setFatYield('');
    setLiters('');
  }, [isOpen, harvest, plots, seasons, activeSeason]);

  const selectedSeason = useMemo(
    () => seasons.find((season) => season.id === seasonId) ?? null,
    [seasons, seasonId]
  );

  // RN-023 — aviso, nunca bloqueo. Se calcula también en cliente para que aparezca al escribir.
  const isOutOfSeasonRange = useMemo(() => {
    if (!selectedSeason || !date) return false;
    if (date < selectedSeason.start_date) return true;
    return selectedSeason.end_date !== null && date > selectedSeason.end_date;
  }, [selectedSeason, date]);

  /**
   * Rendimiento equivalente en la unidad canónica L/100kg, como ayuda de lectura mientras se escribe
   * (RN-013/RN-014). El cálculo bueno lo hace el servidor: aquí solo se anticipa lo que va a guardar,
   * para que nadie escriba un número a ciegas.
   */
  const canonicalPreview = useMemo(() => {
    const kilos = Number(kgs);

    if (yieldMode === 'litros') {
      const litros = Number(liters);
      if (!Number.isFinite(kilos) || kilos <= 0 || !Number.isFinite(litros) || litros <= 0) return null;
      return (litros / kilos) * 100;
    }

    if (yieldMode === 'rendimiento_graso') {
      const graso = Number(fatYield);
      if (!Number.isFinite(graso) || graso <= 0) return null;
      // RN-016 — densidad por defecto 0,92 kg/L.
      return graso / OIL_DENSITY_KG_PER_LITRE;
    }

    return null;
  }, [yieldMode, kgs, liters, fatYield]);

  if (!isOpen) return null;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const kilos = Number(kgs);
    if (!plotId || !seasonId) {
      setLocalError('Elige el terreno y la temporada de la cosecha.');
      return;
    }
    if (!Number.isFinite(kilos) || kilos <= 0) {
      setLocalError('Los kilos recolectados deben ser mayores que 0.');
      return;
    }

    let yieldPayload: number | null = null;
    let litersPayload: number | null = null;
    // RN-014 — la unidad viaja aparte del valor; el servidor guarda siempre la canónica (RN-013).
    let yieldUnit: CreateHarvestPayload['yield_unit'] = null;

    if (yieldMode === 'rendimiento') {
      const value = Number(yieldValue);
      if (!Number.isFinite(value) || value <= 0 || value > HARVEST_YIELD_MAX) {
        setLocalError(`El rendimiento debe estar entre 0 y ${HARVEST_YIELD_MAX} L/100kg.`);
        return;
      }
      yieldPayload = value;
      yieldUnit = 'l_100kg';
    } else if (yieldMode === 'rendimiento_graso') {
      const value = Number(fatYield);
      // La cota se aplica sobre la canónica, que es lo que valida el servidor.
      if (!Number.isFinite(value) || value <= 0 || value / OIL_DENSITY_KG_PER_LITRE > HARVEST_YIELD_MAX) {
        setLocalError('El rendimiento graso debe ser mayor que 0 y no puede superar el 92 % (más aceite que fruto).');
        return;
      }
      yieldPayload = value;
      yieldUnit = 'kg_100kg';
    } else if (yieldMode === 'litros') {
      const value = Number(liters);
      if (!Number.isFinite(value) || value <= 0) {
        setLocalError('Los litros obtenidos deben ser mayores que 0.');
        return;
      }
      litersPayload = value;
    }

    setLocalError(null);
    onSubmit({
      date,
      plot_id: plotId,
      season_id: seasonId,
      product,
      kgs: kilos,
      destination,
      // RN-004 — se envían siempre los dos para que el `PATCH` sustituya la pareja completa: así
      // cambiar de rendimiento a litros (o retirar ambos) no deja el campo anterior colgando.
      yield: yieldPayload,
      liters: litersPayload,
      yield_unit: yieldUnit,
    });
  };

  const shownError = localError ?? errorMessage;
  const isEditing = harvest !== null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-lg w-full border border-[#e5e2dd] shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl" aria-hidden="true">agriculture</span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">
              {isEditing ? 'Corregir cosecha' : 'Registrar cosecha'}
            </h3>
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <label htmlFor="harvest-plot" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Terreno <span className="text-[#ba1a1a]">*</span>
              </label>
              <select
                id="harvest-plot"
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
              <label htmlFor="harvest-date" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Fecha de recolección <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="harvest-date"
                type="date"
                required
                value={date}
                onChange={(e) => setDate(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label htmlFor="harvest-season" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Temporada <span className="text-[#ba1a1a]">*</span>
            </label>
            <select
              id="harvest-season"
              value={seasonId}
              onChange={(e) => setSeasonId(e.target.value)}
              disabled={isSubmitting}
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
              <label htmlFor="harvest-product" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Producto <span className="text-[#ba1a1a]">*</span>
              </label>
              <select
                id="harvest-product"
                value={product}
                onChange={(e) => setProduct(e.target.value)}
                disabled={isSubmitting}
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              >
                {/* RN-030 — catálogo global fijo, no editable por el usuario. Hoy el MVP está ligado
                    al olivar y no distingue variedades: eso pertenece al terreno, no a la cosecha. */}
                {HARVEST_PRODUCTS.map((value) => (
                  <option key={value} value={value}>{harvestProductLabel(value)}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label htmlFor="harvest-kgs" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Kilos recolectados <span className="text-[#ba1a1a]">*</span>
              </label>
              <input
                id="harvest-kgs"
                type="number"
                min="0.01"
                step="0.01"
                required
                value={kgs}
                onChange={(e) => setKgs(e.target.value)}
                disabled={isSubmitting}
                placeholder="1200"
                className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label htmlFor="harvest-destination" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Destino <span className="text-[#ba1a1a]">*</span>
            </label>
            <select
              id="harvest-destination"
              value={destination}
              onChange={(e) => setDestination(e.target.value)}
              disabled={isSubmitting}
              className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            >
              {HARVEST_DESTINATIONS.map((value) => (
                <option key={value} value={value}>{harvestDestinationLabel(value)}</option>
              ))}
            </select>
            {destination === 'desconocido' && (
              <p className="text-[11px] text-[#76786b]">
                Puedes registrar la cosecha sin conocer todavía el destino y completarlo después.
              </p>
            )}
          </div>

          {/* RN-004 — rendimiento y litros son excluyentes: se elige cómo se informa, o ninguno */}
          <fieldset className="space-y-2 pt-1 border-t border-[#f0ede8]">
            <legend className="text-xs font-bold uppercase tracking-wider text-[#45483c] pt-3">
              Aceite obtenido
            </legend>
            <div className="flex flex-wrap gap-2" role="radiogroup" aria-label="Cómo informar el aceite obtenido">
              {/* RN-014 — los tres orígenes admitidos, más «no lo sé»: cualquiera de ellos acaba en la
                  misma unidad canónica L/100kg (RN-013). */}
              {(
                [
                  ['ninguno', 'Todavía no lo sé'],
                  ['rendimiento', 'Rendimiento (L/100kg)'],
                  ['rendimiento_graso', 'Rendimiento graso (kg/100kg)'],
                  ['litros', 'Litros obtenidos'],
                ] as [YieldInputMode, string][]
              ).map(([mode, label]) => (
                <button
                  key={mode}
                  type="button"
                  role="radio"
                  aria-checked={yieldMode === mode}
                  onClick={() => setYieldMode(mode)}
                  disabled={isSubmitting}
                  className={`px-3 py-1.5 rounded-xl text-xs font-semibold border transition-colors disabled:opacity-60 ${
                    yieldMode === mode
                      ? 'bg-[#33450d] text-white border-[#33450d]'
                      : 'bg-[#f6f3ee] text-[#45483c] border-[#c6c8b8] hover:bg-[#ebe8e3]'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>

            {yieldMode === 'rendimiento' && (
              <div className="space-y-1.5">
                <label htmlFor="harvest-yield" className="sr-only">Rendimiento en litros por cada 100 kg</label>
                <input
                  id="harvest-yield"
                  type="number"
                  min="0.01"
                  max={HARVEST_YIELD_MAX}
                  step="0.01"
                  value={yieldValue}
                  onChange={(e) => setYieldValue(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="18,5"
                  className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
                <p className="text-[11px] text-[#76786b]">
                  Litros de aceite por cada 100 kg de aceituna: la unidad canónica del producto.
                </p>
              </div>
            )}

            {yieldMode === 'rendimiento_graso' && (
              <div className="space-y-1.5">
                <label htmlFor="harvest-fat-yield" className="sr-only">
                  Rendimiento graso en kilos de aceite por cada 100 kg
                </label>
                <input
                  id="harvest-fat-yield"
                  type="number"
                  min="0.01"
                  max="92"
                  step="0.01"
                  value={fatYield}
                  onChange={(e) => setFatYield(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="20"
                  className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
                <p className="text-[11px] text-[#76786b]">
                  Kilos de aceite por cada 100 kg de aceituna, que es como suele darlo la almazara.
                </p>
              </div>
            )}

            {yieldMode === 'litros' && (
              <div className="space-y-1.5">
                <label htmlFor="harvest-liters" className="sr-only">Litros de aceite obtenidos</label>
                <input
                  id="harvest-liters"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={liters}
                  onChange={(e) => setLiters(e.target.value)}
                  disabled={isSubmitting}
                  placeholder="220"
                  className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>
            )}

            {/* Se anticipa lo que va a guardar el servidor, para que nadie escriba a ciegas */}
            {canonicalPreview !== null && (
              <p className="text-[11px] text-[#76786b] flex items-center gap-1.5">
                <span className="material-symbols-outlined text-sm" aria-hidden="true">calculate</span>
                Equivale a {canonicalPreview.toLocaleString('es-ES', { maximumFractionDigits: 2 })} L/100kg,
                que es la unidad con la que se comparan las campañas.
              </p>
            )}
          </fieldset>

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
              <span>{isSubmitting ? 'Guardando…' : isEditing ? 'Guardar cambios' : 'Guardar cosecha'}</span>
              <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
