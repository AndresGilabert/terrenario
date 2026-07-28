import React, { useState } from 'react';
import type {
  WorkspaceClosureOptions,
  WorkspaceClosureResult,
} from '../../types/workspace-lifecycle.types';

type Decision = 'transfer' | 'delete';

interface CloseWorkspaceModalProps {
  options: WorkspaceClosureOptions;
  isBusy: boolean;
  errorMessage: string | null;
  onCancel: () => void;
  /** `transfer` exige destinatario; `delete` da de baja (o reasigna y saca, según el caso). */
  onConfirm: (decision: Decision, newOwnerUserId?: string) => Promise<WorkspaceClosureResult | void>;
}

/**
 * MVP-206 — Diálogo de salida del Workspace. No es un "¿seguro?" genérico: plantea exactamente la
 * decisión que corresponde según el árbol que resuelve el servidor, para que nadie crea que borra
 * datos cuando en realidad cede el Workspace, ni al revés.
 *
 * - `auto_transfer`: la acción es **salir cediendo la propiedad**; el Workspace sigue vivo (CA-5).
 * - `choose`: se **exige decidir** entre traspasar (eligiendo a quién) o dar de baja (CA-3/CA-4).
 * - `only_delete`: solo cabe la baja lógica, reversible por quien la hace.
 */
export const CloseWorkspaceModal: React.FC<CloseWorkspaceModalProps> = ({
  options,
  isBusy,
  errorMessage,
  onCancel,
  onConfirm,
}) => {
  const mustChoose = options.mode === 'choose';
  const [decision, setDecision] = useState<Decision | null>(mustChoose ? null : 'delete');
  const [newOwnerUserId, setNewOwnerUserId] = useState<string>(
    options.candidates[0]?.user_id ?? ''
  );

  const isAutoTransfer = options.mode === 'auto_transfer';
  const needsOwner = decision === 'transfer';
  const canConfirm = decision !== null && (!needsOwner || newOwnerUserId !== '');

  const title = isAutoTransfer ? 'Salir y ceder mi propiedad' : 'Dar de baja el Workspace';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="fixed inset-0 bg-black/40 backdrop-blur-xs" onClick={onCancel} aria-hidden="true" />

      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="close-workspace-title"
        className="relative z-10 w-full max-w-lg bg-white rounded-2xl border border-[#e5e2dd] shadow-xl p-6 space-y-5 max-h-[90vh] overflow-y-auto"
      >
        <div className="space-y-1.5">
          <h3 id="close-workspace-title" className="font-headline font-bold text-lg text-[#1c1c19]">
            {title}
          </h3>
          <p className="text-sm text-[#45483c]">
            Workspace: <span className="font-semibold">{options.workspace.name}</span>
          </p>
        </div>

        {isAutoTransfer && (
          <div className="p-3 rounded-xl bg-[#f0f4e3] border border-[#d5e0b5] text-[#33450d] text-sm space-y-1">
            <p>
              Este Workspace tiene más personas propietarias, así que <strong>no se dará de baja</strong>:
              pasará a {options.successor_name ?? 'otra persona propietaria'} y seguirá funcionando.
            </p>
            <p>Tú dejarás de tener acceso. Para volver, alguien de dentro tendrá que invitarte.</p>
          </div>
        )}

        {mustChoose && (
          <fieldset className="space-y-3">
            <legend className="text-sm text-[#45483c] mb-2">
              Eres la única persona propietaria. Antes de irte hay que decidir qué pasa con el
              Workspace:
            </legend>

            <label
              className={`flex gap-3 p-3 rounded-xl border cursor-pointer transition-colors ${
                decision === 'transfer' ? 'border-[#33450d] bg-[#f6f8ee]' : 'border-[#e5e2dd] hover:bg-[#faf8f4]'
              }`}
            >
              <input
                type="radio"
                name="closure-decision"
                className="mt-1"
                checked={decision === 'transfer'}
                onChange={() => setDecision('transfer')}
              />
              <span className="text-sm">
                <span className="block font-semibold text-[#1c1c19]">Traspasar la propiedad</span>
                <span className="block text-[#76786b] text-xs">
                  El Workspace sigue vivo con otra persona al frente. Tú te quedas dentro como
                  miembro.
                </span>
              </span>
            </label>

            {decision === 'transfer' && (
              <div className="pl-8 space-y-1.5">
                <label
                  htmlFor="new-owner"
                  className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
                >
                  Nueva persona propietaria
                </label>
                <select
                  id="new-owner"
                  value={newOwnerUserId}
                  onChange={(e) => setNewOwnerUserId(e.target.value)}
                  className="w-full px-4 py-2.5 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm font-medium text-[#1c1c19]"
                >
                  {options.candidates.map((candidate) => (
                    <option key={candidate.user_id} value={candidate.user_id}>
                      {candidate.name} · {candidate.email}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <label
              className={`flex gap-3 p-3 rounded-xl border cursor-pointer transition-colors ${
                decision === 'delete' ? 'border-[#ba1a1a] bg-[#fdf3f3]' : 'border-[#e5e2dd] hover:bg-[#faf8f4]'
              }`}
            >
              <input
                type="radio"
                name="closure-decision"
                className="mt-1"
                checked={decision === 'delete'}
                onChange={() => setDecision('delete')}
              />
              <span className="text-sm">
                <span className="block font-semibold text-[#1c1c19]">Dar de baja el Workspace</span>
                <span className="block text-[#76786b] text-xs">
                  Deja de estar disponible para todo el mundo. No se borra nada: avisaremos al resto
                  de miembros con un enlace para pedirte que se lo traspases, y tú puedes volver a
                  levantarlo cuando quieras.
                </span>
              </span>
            </label>
          </fieldset>
        )}

        {options.mode === 'only_delete' && (
          <div className="p-3 rounded-xl bg-[#faf8f4] border border-[#e5e2dd] text-sm text-[#45483c] space-y-1">
            <p>
              El Workspace dejará de aparecer y de estar disponible, pero <strong>no se borra
              nada</strong>: la baja es reversible.
            </p>
            <p>Podrás volver a levantarlo desde «Workspaces dados de baja» cuando quieras.</p>
          </div>
        )}

        {errorMessage && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {errorMessage}
          </p>
        )}

        <div className="flex flex-wrap justify-end gap-3 pt-1">
          <button
            type="button"
            onClick={onCancel}
            disabled={isBusy}
            className="px-4 py-2.5 text-sm font-semibold text-[#45483c] hover:bg-[#f0ede8] rounded-xl disabled:opacity-60"
          >
            Cancelar
          </button>
          <button
            type="button"
            disabled={isBusy || !canConfirm}
            onClick={() => void onConfirm(decision!, needsOwner ? newOwnerUserId : undefined)}
            className={`px-5 py-2.5 rounded-xl text-sm font-bold text-white shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed ${
              decision === 'transfer' || isAutoTransfer
                ? 'bg-[#33450d] hover:bg-[#4a5d23]'
                : 'bg-[#ba1a1a] hover:bg-[#a01515]'
            }`}
          >
            {isBusy
              ? 'Aplicando…'
              : decision === 'transfer'
                ? 'Traspasar la propiedad'
                : isAutoTransfer
                  ? 'Salir y ceder'
                  : 'Dar de baja'}
          </button>
        </div>
      </div>
    </div>
  );
};
