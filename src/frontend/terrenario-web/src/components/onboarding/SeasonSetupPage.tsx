import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { useSeason } from '../../contexts/SeasonContext';
import { SeasonServiceError } from '../../services/season.service';

const NAME_MAX_LENGTH = 120;

/** Sugerencias de cliente para prellenar el formulario. No se persiste nada hasta que el usuario crea. */
function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}
function suggestedName(): string {
  return `Campaña ${new Date().getFullYear()}`;
}

/**
 * MVP-201 — Crea la (primera) temporada del Workspace. Es una **oferta cancelable**: se presenta al
 * crear el Workspace y también cuando el Workspace activo no tiene temporada. No hay temporada por
 * defecto; solo se crea si el usuario lo confirma. "Ahora no" entra a la app sin crear ninguna.
 * La gestión completa de temporadas (varias, editar, cerrar) llega con el maestro de MVP-203.
 *
 * Referencia visual: `prototype/terrenario-mvp/src/components/OnboardingStep2.tsx`.
 */
export const SeasonSetupPage: React.FC = () => {
  const navigate = useNavigate();
  const { activeWorkspace } = useWorkspace();
  const { createSeason, dismissOffer } = useSeason();

  const [name, setName] = useState(suggestedName());
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSkip = () => {
    // "Ahora no": no se crea temporada; se descarta la oferta para este Workspace y se entra a la app.
    dismissOffer();
    navigate('/app', { replace: true });
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
        error instanceof SeasonServiceError
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

        {/* Formulario */}
        <div className="md:col-span-7 p-6 sm:p-8 space-y-6">
          <div className="space-y-1 border-b border-[#e5e2dd] pb-4">
            <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
              <span className="material-symbols-outlined text-base" aria-hidden="true">calendar_today</span>
              <span>Temporada del Workspace</span>
            </div>
            <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
              Crea tu primera temporada
            </h1>
            <p className="text-xs sm:text-sm text-[#45483c]">
              {activeWorkspace
                ? `«${activeWorkspace.name}» aún no tiene temporada. `
                : 'Tu Workspace aún no tiene temporada. '}
              Créala para organizar cosechas y actividades, o hazlo más tarde. Puedes cancelar.
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4" noValidate>
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
                autoFocus
                disabled={isSubmitting}
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
                  disabled={isSubmitting}
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
                  disabled={isSubmitting}
                  className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white disabled:opacity-60"
                />
              </div>
            </div>

            <div className="flex items-center gap-3 p-3 bg-[#f0ede8] rounded-xl border border-[#e5e2dd]">
              <span className="material-symbols-outlined text-[#33450d]" aria-hidden="true">check_circle</span>
              <span className="text-xs font-semibold text-[#1c1c19]">
                Será la temporada activa por defecto de tu Workspace.
              </span>
            </div>

            {errorMessage && (
              <div
                role="alert"
                className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
              >
                {errorMessage}
              </div>
            )}

            <div className="flex items-center justify-between pt-4 border-t border-[#e5e2dd]">
              <button
                type="button"
                onClick={handleSkip}
                disabled={isSubmitting}
                className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-60"
              >
                Ahora no
              </button>

              <button
                type="submit"
                disabled={isSubmitting}
                className="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
              >
                <span>{isSubmitting ? 'Creando…' : 'Crear temporada'}</span>
                <span className="material-symbols-outlined text-sm" aria-hidden="true">check</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
