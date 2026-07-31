import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useSeason } from '../../contexts/SeasonContext';
import { HttpError } from '../../services/http-client';
import { SEASON_STATUS_LABELS, type Season } from '../../types/season.types';

const NAME_MAX_LENGTH = 120;

/** Sugerencias de cliente para prellenar el formulario. No se persiste nada hasta que el usuario crea. */
function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Nombre sugerido libre: «Campaña 2026» y, si ya existe, el primer año siguiente que no esté usado.
 * Sin esto, un Workspace con temporadas pero ninguna activa llegaba aquí con un nombre ya ocupado y
 * chocaba con el 409 de nombre duplicado de MVP-207 (MVP-208, HU-4).
 */
function suggestedName(seasons: Season[]): string {
  const taken = new Set(seasons.map((s) => s.name.trim().toLowerCase()));
  const year = new Date().getFullYear();

  for (let offset = 0; offset < 20; offset += 1) {
    const candidate = `Campaña ${year + offset}`;
    if (!taken.has(candidate.toLowerCase())) return candidate;
  }

  return '';
}

/**
 * MVP-201 · MVP-208 (CA-8) — Oferta de temporada del Workspace activo. Es una **oferta cancelable**:
 * se presenta al crear el Workspace y también cuando el Workspace activo no tiene temporada activa.
 * No hay temporada por defecto; solo se crea o se activa si el usuario lo confirma. «Ahora no» entra
 * a la app sin tocar nada.
 *
 * La pantalla distingue los dos estados que antes confundía (hallazgo R-17):
 *  - **Sin ninguna temporada**: se ofrece crear la primera, como hacía MVP-201.
 *  - **Con temporadas pero ninguna activa** —alcanzable desde MVP-203 al cerrar la activa, que
 *    libera el hueco—: ya no afirma que no hay ninguna. Ofrece **activar** una existente, que casi
 *    siempre es lo que se quiere, y deja crear otra como segunda opción.
 *
 * Referencia visual: `prototype/terrenario-mvp/src/components/OnboardingStep2.tsx`.
 */
export const SeasonSetupPage: React.FC = () => {
  const { seasons, isLoading } = useSeason();

  // La pantalla es alcanzable directamente (píldora de cabecera, marcador), no solo por la guarda de
  // oferta: hasta saber qué temporadas hay no se puede decidir qué ofrecer, y montar el formulario
  // antes dejaría el nombre sugerido y el modo calculados sobre una lista vacía.
  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return <SeasonSetup seasons={seasons} />;
};

const SeasonSetup: React.FC<{ seasons: Season[] }> = ({ seasons }) => {
  const navigate = useNavigate();
  const { activeWorkspace } = useWorkspace();
  const { createSeason, activateSeason, dismissOffer } = useSeason();

  const hasSeasons = seasons.length > 0;
  // Orden de trabajo: las abiertas primero (son las candidatas naturales a activarse) y las cerradas
  // al final, sin ocultarlas: reabrirlas es una decisión del usuario, no nuestra.
  const selectable = useMemo(
    () => [...seasons].sort((a, b) => Number(a.is_closed) - Number(b.is_closed)),
    [seasons]
  );

  const [name, setName] = useState(() => suggestedName(seasons));
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [activatingId, setActivatingId] = useState<string | null>(null);
  // Con temporadas existentes, crear otra es la opción secundaria: el formulario empieza plegado.
  const [isCreating, setIsCreating] = useState(!hasSeasons);

  const isBusy = isSubmitting || activatingId !== null;

  const handleSkip = () => {
    // "Ahora no": no se crea ni se activa nada; se descarta la oferta y se entra a la app.
    dismissOffer();
    navigate('/app', { replace: true });
  };

  const handleActivate = async (season: Season) => {
    setActivatingId(season.id);
    setErrorMessage(null);
    try {
      await activateSeason(season.id);
      navigate('/app', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof HttpError
          ? error.message
          : 'No se pudo activar la temporada. Inténtalo de nuevo.'
      );
      setActivatingId(null);
    }
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    const normalizedName = name.trim();
    if (!normalizedName) {
      setErrorMessage('Escribe un nombre para la temporada.');
      return;
    }
    if (!startDate) {
      setErrorMessage('Indica la fecha de inicio de la temporada.');
      return;
    }
    if (endDate && endDate < startDate) {
      setErrorMessage('La fecha de fin no puede ser anterior a la de inicio.');
      return;
    }

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await createSeason({ name: normalizedName, start_date: startDate, end_date: endDate || null });
      navigate('/app', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof HttpError
          ? error.message
          : 'No se pudo crear la temporada. Inténtalo de nuevo.'
      );
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center p-4 lg:p-8">
      <div className="w-full max-w-4xl bg-white rounded-2xl border border-[#e5e2dd] shadow-2xl overflow-hidden grid grid-cols-1 md:grid-cols-12">
        {/* Banner lateral (escritorio) */}
        <div className="hidden md:flex md:col-span-5 relative bg-[#33450d] text-white p-8 flex-col justify-between overflow-hidden">
          <div className="relative z-10 space-y-3">
            <div className="w-10 h-10 rounded-xl bg-white/20 backdrop-blur-md flex items-center justify-center">
              <span className="material-symbols-outlined fill text-white text-2xl" aria-hidden="true">eco</span>
            </div>
            <h2 className="font-headline font-bold text-2xl tracking-tight">Terrenario</h2>
            <p className="text-xs text-stone-200">
              Cultivando el futuro con precisión y tecnología sencilla.
            </p>
          </div>

          <div className="relative z-10 bg-white/10 backdrop-blur-md p-4 rounded-xl border border-white/20 text-xs space-y-1">
            <p className="font-bold text-[#c9f16f]">¿Por qué usar temporadas?</p>
            <p className="text-stone-200">
              Agrupan tus actividades, cosechas y gastos por campaña para poder compararlos año tras año.
            </p>
          </div>
        </div>

        {/* Decisión: activar una existente o crear una nueva */}
        <div className="md:col-span-7 p-6 sm:p-8 space-y-6 max-h-[90vh] overflow-y-auto">
          <div className="space-y-1 border-b border-[#e5e2dd] pb-4">
            <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
              <span className="material-symbols-outlined text-base" aria-hidden="true">calendar_today</span>
              <span>Temporada del Workspace</span>
            </div>
            <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
              {hasSeasons ? 'Elige la temporada de trabajo' : 'Crea tu primera temporada'}
            </h1>
            <p className="text-xs sm:text-sm text-[#45483c]">
              {hasSeasons ? (
                <>
                  {activeWorkspace ? `«${activeWorkspace.name}» ` : 'Tu Workspace '}
                  tiene {seasons.length === 1 ? 'una temporada' : `${seasons.length} temporadas`}, pero
                  ninguna activa. Activa la que toque o crea otra. Puedes cancelar.
                </>
              ) : (
                <>
                  {activeWorkspace
                    ? `«${activeWorkspace.name}» aún no tiene temporada. `
                    : 'Tu Workspace aún no tiene temporada. '}
                  Créala para organizar cosechas y actividades, o hazlo más tarde. Puedes cancelar.
                </>
              )}
            </p>
          </div>

          {errorMessage && (
            <div
              role="alert"
              className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
            >
              {errorMessage}
            </div>
          )}

          {hasSeasons && (
            <section className="space-y-2">
              <h2 className="text-xs font-bold uppercase tracking-wider text-[#45483c]">
                Temporadas de este Workspace
              </h2>
              <ul className="space-y-2">
                {selectable.map((season) => (
                  <li
                    key={season.id}
                    className="flex items-center justify-between gap-3 p-3 rounded-xl border border-[#e5e2dd] bg-[#f6f3ee]"
                  >
                    <div className="min-w-0">
                      <p className="text-sm font-semibold text-[#1c1c19] truncate">{season.name}</p>
                      <p className="text-xs text-[#76786b]">
                        {SEASON_STATUS_LABELS[season.status]} · desde {formatDate(season.start_date)}
                        {season.end_date ? ` hasta ${formatDate(season.end_date)}` : ''}
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => void handleActivate(season)}
                      disabled={isBusy}
                      className="shrink-0 px-4 py-2 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-semibold transition-colors disabled:opacity-60"
                    >
                      {activatingId === season.id ? 'Activando…' : 'Activar'}
                    </button>
                  </li>
                ))}
              </ul>
              <p className="text-[11px] text-[#76786b]">
                Activar una temporada cerrada la reabre como activa. Solo puede haber una activa a la
                vez.
              </p>
            </section>
          )}

          {hasSeasons && !isCreating ? (
            <button
              type="button"
              onClick={() => setIsCreating(true)}
              className="w-full px-4 py-2.5 rounded-xl border border-[#c6c8b8] bg-white hover:bg-[#f0ede8] text-[#33450d] text-xs font-semibold transition-colors flex items-center justify-center gap-1.5"
            >
              <span className="material-symbols-outlined text-base" aria-hidden="true">add</span>
              Crear una temporada nueva
            </button>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4" noValidate>
              {hasSeasons && (
                <h2 className="text-xs font-bold uppercase tracking-wider text-[#45483c]">
                  Crear una temporada nueva
                </h2>
              )}

              <div className="space-y-1.5">
                <label
                  htmlFor="season-name"
                  className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
                >
                  Nombre de la temporada
                </label>
                <input
                  id="season-name"
                  type="text"
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  placeholder="ej. Campaña Oliva 2026"
                  maxLength={NAME_MAX_LENGTH}
                  autoFocus={!hasSeasons}
                  disabled={isBusy}
                  aria-invalid={errorMessage !== null}
                  className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="space-y-1.5">
                  <label
                    htmlFor="season-start"
                    className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
                  >
                    Fecha de inicio
                  </label>
                  <input
                    id="season-start"
                    type="date"
                    value={startDate}
                    onChange={(event) => setStartDate(event.target.value)}
                    disabled={isBusy}
                    className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                  />
                </div>

                <div className="space-y-1.5">
                  <label
                    htmlFor="season-end"
                    className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
                  >
                    Fecha fin <span className="normal-case text-[#76786b]">(estimada, opcional)</span>
                  </label>
                  <input
                    id="season-end"
                    type="date"
                    value={endDate}
                    min={startDate || undefined}
                    onChange={(event) => setEndDate(event.target.value)}
                    disabled={isBusy}
                    className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                  />
                </div>
              </div>

              <div className="flex items-center gap-3 p-3 bg-[#f0ede8] rounded-xl border border-[#e5e2dd]">
                <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">check_circle</span>
                <span className="text-xs font-semibold text-[#1c1c19]">
                  {hasSeasons
                    ? 'Pasará a ser la temporada activa y desbancará a las demás.'
                    : 'Será la temporada activa por defecto de tu Workspace.'}
                </span>
              </div>

              <div className="flex items-center justify-end">
                <button
                  type="submit"
                  disabled={isBusy}
                  className="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
                >
                  <span>{isSubmitting ? 'Creando…' : 'Crear temporada'}</span>
                  <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
                </button>
              </div>
            </form>
          )}

          <div className="pt-4 border-t border-[#e5e2dd]">
            <button
              type="button"
              onClick={handleSkip}
              disabled={isBusy}
              className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-60"
            >
              Ahora no
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

function formatDate(isoDate: string): string {
  return new Date(isoDate).toLocaleDateString('es-ES', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}
