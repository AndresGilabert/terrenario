import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { WorkspaceServiceError } from '../../services/workspace.service';

const NAME_MAX_LENGTH = 120;

interface CreateWorkspacePageProps {
  /**
   * `onboarding` (por defecto): primer Workspace del usuario, con indicador de pasos y salida a
   * "Cerrar sesión" (MVP-102). `additional`: alta de un Workspace más desde la app (MVP-107),
   * sin el indicador de onboarding y con "Cancelar" que vuelve a la operativa.
   */
  mode?: 'onboarding' | 'additional';
}

/**
 * MVP-102 — Dar nombre al Workspace. En modo `onboarding` es el primer paso del alta inicial;
 * en modo `additional` (MVP-107) crea un Workspace adicional reutilizando la misma pantalla.
 * Referencia visual: `prototype/terrenario-mvp/src/components/OnboardingStep1.tsx`.
 *
 * MVP-201 — Tras crear el Workspace se entra a `/app`; si el Workspace no tiene temporada, la app
 * ofrece crear una (cancelable) mediante la guarda de oferta de temporada. No se crea ninguna por
 * defecto. Esto aplica igual al primer Workspace (onboarding) y a los adicionales (MVP-107).
 */
export const CreateWorkspacePage: React.FC<CreateWorkspacePageProps> = ({ mode = 'onboarding' }) => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { createWorkspace } = useWorkspace();

  const isAdditional = mode === 'additional';

  const [name, setName] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    const normalizedName = name.trim();
    if (!normalizedName) {
      setErrorMessage('Escribe un nombre para tu Workspace.');
      return;
    }

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await createWorkspace(normalizedName);
      // Se entra a la operativa; si el Workspace no tiene temporada, la guarda ofrece crearla (MVP-201).
      navigate('/app', { replace: true });
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof WorkspaceServiceError
          ? error.message
          : 'No se pudo crear el Workspace. Inténtalo de nuevo.'
      );
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl space-y-6">
        {/* Onboarding en dos momentos (crear Workspace y, después, ofrecer temporada). No se usa un
            contador "Paso X de Y" porque el segundo momento es una oferta cancelable, no un paso
            obligado del asistente (resuelve P-010). */}
        <div className="space-y-1.5">
          <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
            <span className="material-symbols-outlined text-base" aria-hidden="true">eco</span>
            <span>{isAdditional ? 'Nuevo Workspace' : 'Tu espacio de trabajo'}</span>
          </div>
          <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
            {isAdditional
              ? 'Crea un nuevo Workspace'
              : user?.displayName
                ? `Hola, ${user.displayName}`
                : 'Demos nombre a tu espacio de trabajo'}
          </h1>
          <p className="text-sm text-[#45483c]">
            Un Workspace representa tu finca o explotación agrícola. Todos tus terrenos, tareas
            y cosechas vivirán aquí.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5" noValidate>
          <div className="space-y-2">
            <label
              htmlFor="workspace-name"
              className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
            >
              Nombre del Workspace
            </label>
            <div className="relative">
              <span
                className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-[#76786b]"
                aria-hidden="true"
              >
                landscape
              </span>
              <input
                id="workspace-name"
                type="text"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="ej. Finca El Olivar, AgroSoto, etc."
                maxLength={NAME_MAX_LENGTH}
                autoFocus
                disabled={isSubmitting}
                aria-invalid={errorMessage !== null}
                className="w-full pl-11 pr-4 py-3 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white transition-all font-medium disabled:opacity-60"
              />
            </div>
          </div>

          {errorMessage && (
            <div
              role="alert"
              className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
            >
              {errorMessage}
            </div>
          )}

          <div className="bg-[#f0ede8] rounded-xl p-4 border border-[#e5e2dd] flex items-center gap-4">
            {/* MVP-599 — Autoalojada, misma razón que en la landing: una carga externa aquí
                comunicaría la IP de la persona a un tercero. */}
            <img
              src="/campo.jpg"
              alt=""
              aria-hidden="true"
              className="w-20 h-20 rounded-lg object-cover shadow-xs shrink-0"
            />
            <div className="space-y-1">
              <p className="text-xs font-bold text-[#1c1c19]">Workspace personalizado</p>
              <p className="text-xs text-[#76786b] leading-tight">
                Podrás invitar a tus trabajadores y colaboradores a este espacio más adelante.
              </p>
            </div>
          </div>

          <div className="flex items-center justify-between pt-2">
            {isAdditional ? (
              // Alta adicional: la salida vuelve a la operativa, no cierra sesión.
              <button
                type="button"
                onClick={() => navigate('/app', { replace: true })}
                disabled={isSubmitting}
                className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19] disabled:opacity-60"
              >
                Cancelar
              </button>
            ) : (
              <button
                type="button"
                onClick={() => void logout()}
                className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19]"
              >
                Cerrar sesión
              </button>
            )}
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex items-center gap-2 px-6 py-3 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-sm shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <span>{isSubmitting ? 'Creando…' : 'Crear Workspace'}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
