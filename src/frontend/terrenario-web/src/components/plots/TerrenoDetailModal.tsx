import React, { useEffect, useMemo, useState } from 'react';
import { useApiClient } from '../../contexts/ApiContext';
import { createDiaryService } from '../../services/diary.service';
import { HttpError } from '../../services/http-client';
import type { DiaryEntry } from '../../types/diary.types';
import { harvestDestinationLabel } from '../../types/harvest.types';
import { Modal } from '../common/Modal';
import { PLOT_OWNERSHIP_LABELS, type Plot } from '../../types/plot.types';

const number = (value: number, decimals = 0) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });

/** `YYYY-MM-DD` → «5 oct 2026», la fecha de negocio legible del diario. */
function dateLabel(iso: string): string {
  const parts = iso.match(/^(\d{4})-(\d{2})-(\d{2})$/);
  if (!parts) return iso;
  return new Date(Number(parts[1]), Number(parts[2]) - 1, Number(parts[3])).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

/**
 * MVP-407 (P-019) — Detalle de un terreno con su **histórico** de cosechas y labores. El maestro
 * (MVP-202) entregó el CRUD pero difirió esta vista porque sus datos dependían de MVP-003/MVP-004.
 *
 * Es una **lectura** que compone lo que la parcela tiene detrás: sus datos reales (RN-028, no los
 * campos inventados por el prototipo —superficie/riego/poda—) y su historia, que se lee del diario
 * unificado (`GET /diary?plot_id=…`, MVP-305) a través de todas las temporadas. Editar reutiliza el
 * formulario del maestro (no se duplica el alta/edición aquí).
 */
interface TerrenoDetailModalProps {
  plot: Plot | null;
  onClose: () => void;
  /** Abre el formulario de edición del maestro para este terreno (no se edita en el detalle). */
  onEdit: (plot: Plot) => void;
}

export const TerrenoDetailModal: React.FC<TerrenoDetailModalProps> = ({ plot, onClose, onEdit }) => {
  const http = useApiClient();
  const diaryService = useMemo(() => createDiaryService(http), [http]);

  const [entries, setEntries] = useState<DiaryEntry[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  const plotId = plot?.id ?? null;

  useEffect(() => {
    if (!plotId) return;

    let cancelled = false;
    setIsLoading(true);
    setLoadError(null);
    setEntries([]);

    (async () => {
      try {
        // Sin rango de fechas ni temporada: el detalle es el histórico completo de la parcela.
        const result = await diaryService.listDiary({ plotId });
        if (!cancelled) setEntries(result.data);
      } catch (error) {
        if (!cancelled) {
          setLoadError(
            error instanceof HttpError ? error.message : 'No se pudo cargar el histórico del terreno.'
          );
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [plotId, diaryService]);

  // Más recientes primero (RN-033 ordena el diario por fecha de negocio).
  const sorted = useMemo(
    () => [...entries].sort((a, b) => b.date.localeCompare(a.date)),
    [entries]
  );
  const harvests = useMemo(() => sorted.filter((e) => e.type === 'cosecha'), [sorted]);
  const labores = useMemo(() => sorted.filter((e) => e.type === 'actividad'), [sorted]);

  if (!plot) return null;

  return (
    <Modal
      isOpen
      onClose={onClose}
      title={`Detalle del terreno ${plot.name}`}
      // Cabecera propia: lleva las etiquetas del terreno y el nombre a tamaño de titular, que no cabe
      // en la de por defecto.
      header={null}
      panelClassName="max-w-2xl"
    >
      {/* Cabecera */}
      <div className="p-6 border-b border-[#e5e2dd] flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            {plot.alias && (
              <span className="text-[11px] font-bold px-2 py-0.5 rounded-md bg-[#f0ede8] text-[#33450d]">
                {plot.alias}
              </span>
            )}
            <span className="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-[#eef2e0] text-[#33450d]">
              {PLOT_OWNERSHIP_LABELS[plot.ownership_type]}
            </span>
            {!plot.is_active && (
              <span className="text-[11px] font-bold px-2 py-0.5 rounded-md bg-[#e5e2dd] text-[#76786b]">
                Inactivo
              </span>
            )}
          </div>
          <h2 className="font-headline font-extrabold text-2xl text-[#1c1c19] mt-1 truncate">
            {plot.name}
          </h2>
          {plot.location && (
            <p className="text-xs text-[#76786b] flex items-center gap-1 mt-0.5">
              <span className="material-symbols-outlined text-sm" aria-hidden="true">location_on</span>
              <span className="truncate">{plot.location}</span>
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label="Cerrar"
          className="w-9 h-9 rounded-full bg-[#f0ede8] hover:bg-[#ebe8e3] flex items-center justify-center text-[#45483c] shrink-0"
        >
          <span className="material-symbols-outlined text-xl" aria-hidden="true">close</span>
        </button>
      </div>

      {/* Cuerpo */}
      <div className="p-6 overflow-y-auto space-y-6 flex-1 text-sm">
        {/* Datos reales del terreno (RN-028) */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 bg-[#f6f3ee] p-4 rounded-xl border border-[#e5e2dd]">
          <div>
            <p className="text-[11px] font-bold text-[#76786b] uppercase">Nº de árboles</p>
            {plot.tree_count != null ? (
              <p className="text-base font-extrabold text-[#1c1c19]">{number(plot.tree_count)}</p>
            ) : (
              <p className="text-xs font-semibold text-[#8a6d1a] flex items-center gap-1 mt-1">
                <span className="material-symbols-outlined text-sm" aria-hidden="true">error</span>
                Sin registrar
              </p>
            )}
          </div>
          <div className="min-w-0">
            <p className="text-[11px] font-bold text-[#76786b] uppercase">Propietario</p>
            <p className="text-xs font-semibold text-[#45483c] mt-1 truncate">{plot.owner_name ?? '—'}</p>
          </div>
          <div className="min-w-0">
            <p className="text-[11px] font-bold text-[#76786b] uppercase">Ref. catastral</p>
            <p className="text-xs font-semibold text-[#45483c] mt-1 truncate">
              {plot.cadastral_reference ?? '—'}
            </p>
          </div>
          <div>
            <p className="text-[11px] font-bold text-[#76786b] uppercase">Estado</p>
            <p className="text-xs font-semibold text-[#45483c] mt-1">
              {plot.is_active ? 'Activo' : 'Inactivo'}
            </p>
          </div>
        </div>

        {loadError && (
          <div role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {loadError}
          </div>
        )}

        {isLoading ? (
          <div className="flex items-center justify-center py-10">
            <div className="w-7 h-7 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
          </div>
        ) : (
          <>
            {/* Histórico de cosechas (CA-2) */}
            <section className="space-y-3">
              <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
                <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">agriculture</span>
                <span>Cosechas en esta parcela</span>
                {harvests.length > 0 && (
                  <span className="text-[11px] font-semibold text-[#76786b]">({harvests.length})</span>
                )}
              </h3>

              {harvests.length === 0 ? (
                <p className="text-xs text-[#76786b] italic bg-[#f6f3ee] p-3 rounded-xl">
                  Aún no hay cosechas registradas en este terreno.
                </p>
              ) : (
                <ul className="space-y-2">
                  {harvests.map((h) => (
                    <li
                      key={h.id}
                      className="bg-[#f6f3ee] p-3 rounded-xl border border-[#e5e2dd] flex items-center justify-between gap-3 text-xs"
                    >
                      <div className="min-w-0">
                        <p className="font-bold text-[#1c1c19]">
                          {h.kgs != null ? `${number(h.kgs)} kg` : 'Cosecha'}
                          {h.destination && (
                            <span className="font-normal text-[#76786b]">
                              {' '}· {harvestDestinationLabel(h.destination)}
                            </span>
                          )}
                        </p>
                        <p className="text-[#76786b]">
                          {dateLabel(h.date)} · {h.season_name}
                        </p>
                      </div>
                      {h.yield != null && (
                        <span className="font-bold text-[#33450d] bg-[#c9f16f] px-2.5 py-1 rounded-full text-[11px] shrink-0">
                          {number(h.yield, 1)} L/100kg
                        </span>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </section>

            {/* Histórico de labores (CA-3) */}
            <section className="space-y-3">
              <h3 className="font-headline font-bold text-base text-[#1c1c19] flex items-center gap-2">
                <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">event_note</span>
                <span>Historial de labores</span>
                {labores.length > 0 && (
                  <span className="text-[11px] font-semibold text-[#76786b]">({labores.length})</span>
                )}
              </h3>

              {labores.length === 0 ? (
                <p className="text-xs text-[#76786b] italic bg-[#f6f3ee] p-3 rounded-xl">
                  Sin labores anotadas en el diario para este terreno.
                </p>
              ) : (
                <ul className="space-y-2">
                  {labores.map((l) => (
                    <li key={l.id} className="bg-[#f6f3ee] p-3 rounded-xl border border-[#e5e2dd] space-y-1 text-xs">
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-bold text-[#1c1c19] truncate">{l.title}</span>
                        <span className="text-[#76786b] shrink-0">{dateLabel(l.date)}</span>
                      </div>
                      <p className="text-[11px] text-[#76786b] flex flex-wrap gap-x-3 gap-y-0.5">
                        {l.worker_name && (
                          <span className="flex items-center gap-1">
                            <span className="material-symbols-outlined text-sm" aria-hidden="true">person</span>
                            {l.worker_name}
                          </span>
                        )}
                        {l.hours != null && (
                          <span className="flex items-center gap-1">
                            <span className="material-symbols-outlined text-sm" aria-hidden="true">schedule</span>
                            {number(l.hours, 1)} h
                          </span>
                        )}
                        <span>{l.season_name}</span>
                      </p>
                      {l.description && <p className="text-[11px] text-[#45483c]">{l.description}</p>}
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </>
        )}
      </div>

      {/* Pie */}
      <div className="p-4 bg-[#f0ede8] border-t border-[#e5e2dd] flex justify-between items-center gap-2">
        <button
          type="button"
          onClick={() => onEdit(plot)}
          className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-white border border-[#c6c8b8] hover:bg-[#ebe8e3] text-[#33450d] text-xs font-semibold transition-colors"
        >
          <span className="material-symbols-outlined text-base" aria-hidden="true">edit</span>
          Editar terreno
        </button>
        <button
          type="button"
          onClick={onClose}
          className="px-5 py-2 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl transition-colors"
        >
          Cerrar
        </button>
      </div>
    </Modal>
  );
};
