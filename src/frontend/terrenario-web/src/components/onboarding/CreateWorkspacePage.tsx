import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { WorkspaceServiceError } from '../../services/workspace.service';

const NOMBRE_MAX_LENGTH = 120;

/**
 * MVP-102 — Primer paso del onboarding: dar nombre al Workspace.
 * Referencia visual: `prototype/terrenario-mvp/src/components/OnboardingStep1.tsx`.
 * El paso 2 (temporada inicial) llega con MVP-201.
 */
export const CreateWorkspacePage: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();
  const { createWorkspace } = useWorkspace();

  const [nombre, setNombre] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    const nombreNormalizado = nombre.trim();
    if (!nombreNormalizado) {
      setErrorMessage('Escribe un nombre para tu Workspace.');
      return;
    }

    setErrorMessage(null);
    setIsSubmitting(true);

    try {
      await createWorkspace(nombreNormalizado);
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
        <div className="flex items-center justify-between border-b border-[#e5e2dd] pb-4">
          <div className="flex items-center gap-2 text-xs font-bold text-[#33450d]">
            <span aria-hidden="true">🌿</span>
            <span>Paso 1 de 3</span>
          </div>
          <div
            className="w-24 bg-[#e5e2dd] h-1.5 rounded-full overflow-hidden"
            role="progressbar"
            aria-valuenow={1}
            aria-valuemin={1}
            aria-valuemax={3}
            aria-label="Progreso del onboarding"
          >
            <div className="bg-[#33450d] h-full w-1/3" />
          </div>
        </div>

        <div className="space-y-1.5">
          <h1 className="font-bold text-2xl text-[#1c1c19]">
            {user?.displayName ? `Hola, ${user.displayName}` : 'Demos nombre a tu espacio de trabajo'}
          </h1>
          <p className="text-sm text-[#45483c]">
            Un Workspace representa tu finca o explotación agrícola. Todos tus terrenos, tareas
            y cosechas vivirán aquí.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5" noValidate>
          <div className="space-y-2">
            <label
              htmlFor="workspace-nombre"
              className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
            >
              Nombre del Workspace
            </label>
            <input
              id="workspace-nombre"
              type="text"
              value={nombre}
              onChange={(event) => setNombre(event.target.value)}
              placeholder="ej. Finca El Olivar, AgroSoto, etc."
              maxLength={NOMBRE_MAX_LENGTH}
              autoFocus
              disabled={isSubmitting}
              aria-invalid={errorMessage !== null}
              className="w-full px-4 py-3 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#33450d] focus:bg-white transition-all font-medium disabled:opacity-60"
            />
          </div>

          {errorMessage && (
            <div
              role="alert"
              className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
            >
              {errorMessage}
            </div>
          )}

          <div className="bg-[#f0ede8] rounded-xl p-4 border border-[#e5e2dd] space-y-1">
            <p className="text-xs font-bold text-[#1c1c19]">Workspace personalizado</p>
            <p className="text-xs text-[#76786b] leading-tight">
              Podrás invitar a tus trabajadores y colaboradores a este espacio más adelante.
            </p>
          </div>

          <div className="flex items-center justify-between pt-2">
            <button
              type="button"
              onClick={() => void logout()}
              className="px-4 py-2 text-xs font-semibold text-[#76786b] hover:text-[#1c1c19]"
            >
              Cerrar sesión
            </button>
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
