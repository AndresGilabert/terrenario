import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApiClient } from '../../contexts/ApiContext';
import { useAuth } from '../../contexts/AuthContext';
import { createAccountService, type AccountClosureOptions } from '../../services/account.service';
import { HttpError } from '../../services/http-client';

/**
 * MVP-505 (HU-3, CA-3/CA-4) — **Eliminar mi cuenta**: el derecho de supresión, ejercido sin escribir
 * a nadie.
 *
 * Tres cosas que la pantalla tiene que hacer bien, porque la operación no tiene vuelta atrás:
 *
 * 1. **Decir qué se lleva por delante antes de preguntar.** Una confirmación que dice «esto es
 *    irreversible» sin decir qué desaparece no es una confirmación informada.
 * 2. **Exigir teclear la frase**, no un clic. Es el patrón de las operaciones destructivas: el gesto
 *    tiene que ser deliberado. El servidor la vuelve a comprobar.
 * 3. **Llevar a resolver lo que la bloquea**, no solo negarse. Si quedan Workspaces de propiedad
 *    única (RN-038, CA-4), se listan con su enlace: el camino de salida está a un clic.
 */
export const DeleteAccountPanel: React.FC = () => {
  const http = useApiClient();
  const navigate = useNavigate();
  const { logout } = useAuth();
  const accountService = useMemo(() => createAccountService(http), [http]);

  const [options, setOptions] = useState<AccountClosureOptions | null>(null);
  const [isOpen, setOpen] = useState(false);
  const [typed, setTyped] = useState('');
  const [isDeleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setOptions(await accountService.getClosureOptions());
    } catch {
      // El panel es informativo: si no carga, no se ofrece la baja en vez de ofrecerla a ciegas.
      setOptions(null);
    }
  }, [accountService]);

  useEffect(() => {
    void load();
  }, [load]);

  const phrase = options?.confirmation_phrase ?? 'ELIMINAR MI CUENTA';
  const canConfirm = typed.trim() === phrase && !isDeleting;

  const remove = async () => {
    setDeleting(true);
    setError(null);
    try {
      await accountService.closeAccount(typed.trim());
      // La sesión ya no vale para nada: se cierra en cliente y se sale a la landing.
      await logout();
      navigate('/', { replace: true });
    } catch (err) {
      setError(err instanceof HttpError ? err.message : 'No se pudo eliminar la cuenta.');
      setDeleting(false);
    }
  };

  if (!options) return null;

  return (
    <section className="bg-white rounded-2xl border border-[#f0caca] p-5 space-y-4">
      <div>
        <h3 className="font-headline font-bold text-lg text-[#ba1a1a]">Eliminar mi cuenta</h3>
        <p className="text-xs text-[#76786b] mt-1">
          Ejerce tu derecho de supresión. Es <strong>irreversible</strong>: no hay papelera ni periodo
          de gracia.
        </p>
      </div>

      {/* CA-4 — Lo que bloquea la baja, con su salida. Negarse sin decir cómo resolverlo dejaría a la
          persona atrapada en su propia cuenta. */}
      {!options.is_clear && (
        <div className="rounded-xl bg-[#fff6e5] border border-[#f0d9a8] p-3 space-y-2">
          <p className="text-xs text-[#8a5a00] leading-relaxed">
            <strong>Antes tienes que resolver estos Workspaces.</strong> Eres la única persona
            propietaria: si te vas sin traspasarlos ni darlos de baja, se quedarían sin dueño.
          </p>
          <ul className="space-y-1">
            {options.obligations.map((obligation) => (
              <li key={obligation.workspace_id} className="text-xs text-[#8a5a00]">
                • <strong>{obligation.name}</strong>{' '}
                {obligation.other_active_members > 0
                  ? `(${obligation.other_active_members} ${obligation.other_active_members === 1 ? 'persona' : 'personas'} a quien traspasarlo)`
                  : '(sin nadie a quien traspasarlo: tendrás que darlo de baja)'}
              </li>
            ))}
          </ul>
          <button
            type="button"
            onClick={() => navigate('/app/ajustes')}
            className="text-xs font-semibold text-[#8a5a00] hover:underline"
          >
            Resolverlos en la zona de propiedad de cada Workspace
          </button>
        </div>
      )}

      {/* CA-3 — Qué desaparece y qué queda. La confirmación tiene que ser informada. */}
      <div className="rounded-xl bg-[#f6f3ee] border border-[#e5e2dd] p-3 text-xs text-[#45483c] space-y-1.5">
        <p className="font-semibold text-[#1c1c19]">Qué pasará si continúas</p>
        <p>• Tu nombre y tu correo desaparecen de la cuenta y de los Workspaces donde participabas.</p>
        <p>
          • Se cierran tus {options.active_sessions === 1 ? 'sesión' : 'sesiones'} (
          {options.active_sessions}) y no podrás volver a entrar con esta cuenta.
        </p>
        {options.active_memberships > 0 && (
          <p>
            • Sales de {options.active_memberships}{' '}
            {options.active_memberships === 1 ? 'Workspace' : 'Workspaces'} compartidos. Lo que
            registraste allí <strong>no se borra</strong>: seguiría figurando como «Cuenta eliminada».
          </p>
        )}
        <p>
          • Lo que queda es un registro anonimizado que ya no te identifica, y que se elimina
          definitivamente a los {options.retention_months} meses.
        </p>
      </div>

      {error && (
        <p role="alert" className="text-xs text-red-700 bg-red-50 border border-red-200 rounded-xl p-3">
          {error}
        </p>
      )}

      {!isOpen ? (
        <button
          type="button"
          onClick={() => setOpen(true)}
          disabled={!options.is_clear}
          className="px-4 py-2.5 rounded-xl bg-[#ba1a1a] hover:bg-[#a01515] text-white text-xs font-bold disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Eliminar mi cuenta
        </button>
      ) : (
        <div className="space-y-3">
          <label htmlFor="delete-account-confirmation" className="block text-xs text-[#45483c]">
            Para confirmar, escribe <strong>{phrase}</strong>
          </label>
          <input
            id="delete-account-confirmation"
            type="text"
            value={typed}
            onChange={(event) => setTyped(event.target.value)}
            autoComplete="off"
            className="w-full px-3 py-2 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] focus:outline-none focus:border-[#ba1a1a]"
          />
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => void remove()}
              disabled={!canConfirm}
              className="px-4 py-2.5 rounded-xl bg-[#ba1a1a] hover:bg-[#a01515] text-white text-xs font-bold disabled:opacity-40 disabled:cursor-not-allowed"
            >
              {isDeleting ? 'Eliminando…' : 'Sí, eliminar definitivamente'}
            </button>
            <button
              type="button"
              onClick={() => {
                setOpen(false);
                setTyped('');
                setError(null);
              }}
              disabled={isDeleting}
              className="px-4 py-2.5 rounded-xl text-[#45483c] hover:bg-[#f0ede8] text-xs font-semibold"
            >
              Cancelar
            </button>
          </div>
        </div>
      )}
    </section>
  );
};
