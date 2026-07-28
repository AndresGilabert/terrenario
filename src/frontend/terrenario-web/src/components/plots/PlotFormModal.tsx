import React, { useEffect, useState } from 'react';
import type {
  CreatePlotPayload,
  Plot,
  PlotOwnershipType,
} from '../../types/plot.types';

interface PlotFormModalProps {
  isOpen: boolean;
  /** Terreno a editar; `null` para alta. */
  plot: Plot | null;
  isSubmitting: boolean;
  errorMessage: string | null;
  onClose: () => void;
  onSubmit: (payload: CreatePlotPayload) => void;
}

const NAME_MAX = 150;
const ALIAS_MAX = 60;
const OWNER_MAX = 150;
const CADASTRAL_MAX = 50;
const LOCATION_MAX = 200;

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

/**
 * Alta y edición de un terreno (MVP-202). Refleja el **alta mínima** (RN-028): solo «nombre» y «tipo
 * de propiedad» son obligatorios y el botón de guardar se habilita con ellos; el resto de campos son
 * opcionales y pueden completarse después (CA-1/CA-2). No se pide número de árboles para poder
 * empezar: su ausencia se marca luego como dato incompleto, sin bloquear (RN-010).
 */
export const PlotFormModal: React.FC<PlotFormModalProps> = ({
  isOpen,
  plot,
  isSubmitting,
  errorMessage,
  onClose,
  onSubmit,
}) => {
  const isEdit = plot !== null;

  const [name, setName] = useState('');
  const [ownershipType, setOwnershipType] = useState<PlotOwnershipType>('propia');
  const [alias, setAlias] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [cadastralReference, setCadastralReference] = useState('');
  const [location, setLocation] = useState('');
  const [treeCount, setTreeCount] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  // Sincroniza el formulario cada vez que se abre (alta en blanco; edición con los datos actuales).
  useEffect(() => {
    if (!isOpen) return;
    setName(plot?.name ?? '');
    setOwnershipType(plot?.ownership_type ?? 'propia');
    setAlias(plot?.alias ?? '');
    setOwnerName(plot?.owner_name ?? '');
    setCadastralReference(plot?.cadastral_reference ?? '');
    setLocation(plot?.location ?? '');
    setTreeCount(plot?.tree_count != null ? String(plot.tree_count) : '');
    setLocalError(null);
  }, [isOpen, plot]);

  if (!isOpen) return null;

  const canSubmit = name.trim().length > 0 && !isSubmitting;

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!name.trim()) {
      setLocalError('Escribe un nombre para el terreno.');
      return;
    }

    let parsedTreeCount: number | null = null;
    if (treeCount.trim().length > 0) {
      const value = Number(treeCount);
      if (!Number.isInteger(value) || value < 0) {
        setLocalError('El número de árboles debe ser un entero igual o mayor que 0.');
        return;
      }
      parsedTreeCount = value;
    }

    setLocalError(null);
    onSubmit({
      name: name.trim(),
      ownership_type: ownershipType,
      alias: trimmedOrNull(alias),
      owner_name: trimmedOrNull(ownerName),
      cadastral_reference: trimmedOrNull(cadastralReference),
      location: trimmedOrNull(location),
      tree_count: parsedTreeCount,
    });
  };

  const shownError = localError ?? errorMessage;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-xs">
      <div className="bg-white rounded-2xl max-w-lg w-full border border-[#e5e2dd] shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
        <div className="bg-[#f6f3ee] px-6 py-4 border-b border-[#e5e2dd] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] text-xl" aria-hidden="true">map</span>
            <h3 className="font-headline font-bold text-lg text-[#1c1c19]">
              {isEdit ? 'Editar terreno' : 'Añadir nuevo terreno'}
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
          {/* Obligatorios (RN-028) */}
          <div className="space-y-1.5">
            <label htmlFor="plot-name" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Nombre del terreno <span className="text-[#ba1a1a]">*</span>
            </label>
            <input
              id="plot-name"
              type="text"
              required
              autoFocus
              maxLength={NAME_MAX}
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="ej. La Hoya Norte, Olivar Alto"
              disabled={isSubmitting}
              className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            />
          </div>

          <div className="space-y-1.5">
            <label htmlFor="plot-ownership" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
              Tipo de propiedad <span className="text-[#ba1a1a]">*</span>
            </label>
            <select
              id="plot-ownership"
              value={ownershipType}
              onChange={(e) => setOwnershipType(e.target.value as PlotOwnershipType)}
              disabled={isSubmitting}
              className="w-full px-3 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
            >
              <option value="propia">Propia</option>
              <option value="cedida">Cedida</option>
            </select>
          </div>

          <div className="pt-2 border-t border-[#f0ede8]">
            <p className="text-[11px] text-[#76786b] flex items-center gap-1.5 mb-3">
              <span className="material-symbols-outlined text-sm" aria-hidden="true">info</span>
              El resto de datos son opcionales: puedes completarlos ahora o más adelante.
            </p>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <label htmlFor="plot-alias" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Alias / Código
                </label>
                <input
                  id="plot-alias"
                  type="text"
                  maxLength={ALIAS_MAX}
                  value={alias}
                  onChange={(e) => setAlias(e.target.value)}
                  placeholder="ej. LH-04"
                  disabled={isSubmitting}
                  className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>

              <div className="space-y-1.5">
                <label htmlFor="plot-trees" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Nº de árboles
                </label>
                <input
                  id="plot-trees"
                  type="number"
                  min={0}
                  step={1}
                  value={treeCount}
                  onChange={(e) => setTreeCount(e.target.value)}
                  placeholder="ej. 850"
                  disabled={isSubmitting}
                  className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>
            </div>

            <div className="space-y-1.5 mt-3">
              <label htmlFor="plot-owner" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Propietario
              </label>
              <input
                id="plot-owner"
                type="text"
                maxLength={OWNER_MAX}
                value={ownerName}
                onChange={(e) => setOwnerName(e.target.value)}
                placeholder="ej. Antonio García"
                disabled={isSubmitting}
                className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
              />
            </div>

            <div className="grid grid-cols-2 gap-3 mt-3">
              <div className="space-y-1.5">
                <label htmlFor="plot-cadastral" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Ref. catastral
                </label>
                <input
                  id="plot-cadastral"
                  type="text"
                  maxLength={CADASTRAL_MAX}
                  value={cadastralReference}
                  onChange={(e) => setCadastralReference(e.target.value)}
                  placeholder="ej. 28079A00100001"
                  disabled={isSubmitting}
                  className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>

              <div className="space-y-1.5">
                <label htmlFor="plot-location" className="block text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Ubicación
                </label>
                <input
                  id="plot-location"
                  type="text"
                  maxLength={LOCATION_MAX}
                  value={location}
                  onChange={(e) => setLocation(e.target.value)}
                  placeholder="ej. Sector Norte"
                  disabled={isSubmitting}
                  className="w-full px-3.5 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>
            </div>
          </div>

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
              disabled={!canSubmit}
              className="flex items-center gap-2 px-5 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-xs rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <span>{isSubmitting ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear terreno'}</span>
              <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
