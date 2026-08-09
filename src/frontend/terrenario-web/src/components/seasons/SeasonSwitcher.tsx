import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router';
import { useSeason } from '../../contexts/SeasonContext';
import { HttpError } from '../../services/http-client';
import { SEASON_STATUS_LABELS } from '../../types/season.types';

/** Forma y tamaño comunes de la píldora de contexto: solo cambia el color según el estado. */
const PILL_BASE =
  'inline-flex items-center gap-1.5 text-xs px-2.5 py-0.5 rounded-full font-semibold border';

/**
 * MVP-701 (`P-083`, HU-3) — Conmutador de la **temporada de trabajo** en la píldora de la cabecera.
 *
 * Hasta aquí la píldora era un `<span>` decorativo mientras **sí** había temporada de trabajo, y solo
 * se volvía pulsable cuando no la había: cambiar de campaña obligaba a ir al maestro de Temporadas y
 * pulsar «Activar», tres pasos fuera del flujo. Con `P-082` resuelto deja de ser una comodidad: la
 * temporada de trabajo es ahora el **defecto de todas las vistas operativas** (RN-008), así que
 * cambiarla tiene que poder hacerse desde donde se anuncia.
 *
 * Interacción y accesibilidad calcadas del selector de Workspace (`WorkspaceSwitcher`, MVP-104): son
 * los dos conmutadores del contexto activo y no tendría sentido que se manejaran distinto.
 *
 * Cambiar la temporada de trabajo invalida lo cargado (`SeasonContext.activateSeason` →
 * `invalidateScope`), así que la vista en curso se rehace sola con el ámbito nuevo (CA-5).
 */
export const SeasonSwitcher: React.FC = () => {
  const navigate = useNavigate();
  const { activeSeason, seasons, isLoading, activateSeason } = useSeason();
  const [isOpen, setIsOpen] = useState(false);
  const [switchingId, setSwitchingId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const handlePointerDown = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsOpen(false);
    };

    document.addEventListener('mousedown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen]);

  if (isLoading) return null;

  // El Workspace no tiene ninguna temporada: lo que toca es crearla, no elegir (MVP-208, CA-8).
  if (seasons.length === 0) {
    return (
      <button
        type="button"
        onClick={() => navigate('/app/temporada/nueva')}
        className={`${PILL_BASE} bg-[#f0ede8] text-[#45483c] border-[#dcd9d2] hover:bg-[#ebe8e3] transition-colors`}
        title="Este Workspace no tiene ninguna temporada"
      >
        <span className="w-1.5 h-1.5 rounded-full bg-[#a2a496] shrink-0" aria-hidden="true" />
        <span>Sin temporada · Crear</span>
      </button>
    );
  }

  const handleSelect = async (seasonId: string) => {
    if (seasonId === activeSeason?.id) {
      setIsOpen(false);
      return;
    }

    setErrorMessage(null);
    setSwitchingId(seasonId);

    try {
      await activateSeason(seasonId);
      setIsOpen(false);
    } catch (error) {
      setErrorMessage(
        error instanceof HttpError ? error.message : 'No se pudo cambiar de temporada. Inténtalo de nuevo.'
      );
    } finally {
      setSwitchingId(null);
    }
  };

  const label = activeSeason?.name ?? 'Sin temporada activa · Elegir';

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-haspopup="menu"
        aria-expanded={isOpen}
        aria-label={
          activeSeason
            ? `Campaña de trabajo: ${activeSeason.name}. Pulsa para cambiar de campaña.`
            : 'Ninguna campaña de trabajo seleccionada. Pulsa para elegir una.'
        }
        className={
          activeSeason
            ? `${PILL_BASE} bg-[#c9f16f] text-[#33450d] border-[#aed456] hover:bg-[#bfe95c] transition-colors max-w-[55vw] sm:max-w-none`
            : `${PILL_BASE} bg-[#f0ede8] text-[#45483c] border-[#dcd9d2] hover:bg-[#ebe8e3] transition-colors max-w-[55vw] sm:max-w-none`
        }
      >
        <span
          className={`w-1.5 h-1.5 rounded-full shrink-0 ${
            activeSeason ? 'bg-[#33450d] animate-pulse' : 'bg-[#a2a496]'
          }`}
          aria-hidden="true"
        />
        <span className="truncate">{label}</span>
        <span aria-hidden="true" className="text-[10px] shrink-0 opacity-70">
          {isOpen ? '▲' : '▼'}
        </span>
      </button>

      {isOpen && (
        <div className="absolute z-40 mt-1 min-w-[16rem] bg-white rounded-xl border border-[#e5e2dd] shadow-lg overflow-hidden">
          <p className="px-3.5 pt-2.5 pb-1 text-[11px] font-semibold uppercase tracking-wide text-[#76786b]">
            Campaña de trabajo
          </p>
          <ul role="listbox" aria-label="Selecciona la campaña de trabajo" className="max-h-72 overflow-y-auto py-1">
            {seasons.map((season) => {
              const isWorking = season.id === activeSeason?.id;
              const isSwitching = switchingId === season.id;
              return (
                <li key={season.id} role="option" aria-selected={isWorking}>
                  <button
                    type="button"
                    onClick={() => void handleSelect(season.id)}
                    disabled={switchingId !== null}
                    className={`w-full flex items-center justify-between gap-2 px-3.5 py-2.5 text-left text-sm transition-colors disabled:cursor-wait ${
                      isWorking
                        ? 'bg-[#f0ede8] font-semibold text-[#33450d]'
                        : 'text-[#45483c] hover:bg-[#f6f3ee]'
                    }`}
                  >
                    <span className="min-w-0">
                      <span className="block truncate">{season.name}</span>
                      <span className="block text-[11px] text-[#76786b]">
                        {SEASON_STATUS_LABELS[season.status]}
                      </span>
                    </span>
                    {isWorking && (
                      <span aria-label="Campaña de trabajo" className="text-[#33450d] shrink-0">
                        ✓
                      </span>
                    )}
                    {isSwitching && (
                      <span aria-hidden="true" className="text-[#76786b] shrink-0 text-xs">
                        …
                      </span>
                    )}
                  </button>
                </li>
              );
            })}
          </ul>

          {/* Salida al maestro: cambiar de campaña es una cosa y administrarlas otra. */}
          <div className="border-t border-[#e5e2dd]">
            <button
              type="button"
              onClick={() => {
                setIsOpen(false);
                navigate('/app/temporadas');
              }}
              className="w-full flex items-center gap-2 px-3.5 py-2.5 text-left text-sm font-semibold text-[#33450d] hover:bg-[#f6f3ee] transition-colors"
            >
              <span className="material-symbols-outlined text-lg" aria-hidden="true">tune</span>
              <span>Gestionar campañas</span>
            </button>
          </div>
        </div>
      )}

      {errorMessage && (
        <p role="alert" className="absolute left-0 top-full mt-1 text-xs text-red-700 whitespace-nowrap">
          {errorMessage}
        </p>
      )}
    </div>
  );
};
