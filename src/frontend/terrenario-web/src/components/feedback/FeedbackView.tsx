import React, { useMemo, useState } from 'react';
import { useApiClient } from '../../contexts/ApiContext';
import { createFeedbackService } from '../../services/feedback.service';
import { HttpError } from '../../services/http-client';
import { getReportContext } from '../../lib/report-context';
import type { FeedbackKind } from '../../types/feedback.types';

/**
 * MVP-711 (HU-1, CA-1/CA-2/CA-3 · `P-088`) — «Sugerencias e incidencias».
 *
 * **Pantalla propia dentro del shell, no un diálogo.** Dos motivos. Uno de producto: contar un fallo
 * puede llevar un par de párrafos y a veces hay que releer lo que se ha escrito, y un modal empuja a
 * despachar. Otro de oportunidad: `MVP-704` está unificando todos los modales del producto en un
 * componente común con trampa de foco, y estrenar uno propio a la vez sería un modal más que migrar.
 *
 * **Sin nada de terceros** (CA-4): no hay widget de tickets, ni script, ni iframe, ni recurso remoto.
 * `RN-042` sigue sin activarse y la CSP no se toca, que es exactamente lo que el Product Owner pidió
 * al descartar una herramienta externa.
 *
 * Los iconos son de la tipografía Material Symbols ya **autoalojada** desde `MVP-505`, como en el
 * resto del shell: no traen ninguna petición a un dominio ajeno.
 */
const KINDS: { value: FeedbackKind; label: string; hint: string; icon: string }[] = [
  {
    value: 'incidencia',
    label: 'Algo no funciona',
    hint: 'Un error, algo que no se guarda o una pantalla que no responde.',
    icon: 'bug_report',
  },
  {
    value: 'sugerencia',
    label: 'Se me ocurre algo',
    hint: 'Algo que echas en falta o que te resultaría más cómodo de otra forma.',
    icon: 'lightbulb',
  },
];

/** El mismo tope que aplica el servidor (`FeedbackController.MaxMessageLength`). */
const MAX_MESSAGE_LENGTH = 2000;

export const FeedbackView: React.FC = () => {
  const http = useApiClient();
  const feedback = useMemo(() => createFeedbackService(http), [http]);

  const [kind, setKind] = useState<FeedbackKind>('incidencia');
  const [message, setMessage] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [isSent, setIsSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setIsSending(true);

    // El contexto se resuelve al enviar, no al montar: entre abrir la pantalla y darle a enviar no
    // pasa nada que lo cambie, pero leerlo aquí evita depender del momento del render.
    const context = getReportContext();

    try {
      await feedback.send({
        kind,
        message: message.trim(),
        // Si el canal es lo primero que se abre en esta carga de página no hay pantalla anterior que
        // contar. Se manda la del propio canal en vez de inventarse otra: es la verdad.
        path: context.path ?? window.location.pathname,
        last_failed_request_id: context.lastFailedRequestId,
      });

      setIsSent(true);
      setMessage('');
    } catch (cause) {
      // CA-3 — el mensaje de la API dice qué ha pasado y qué se puede hacer (esperar, reintentar).
      setError(
        cause instanceof HttpError
          ? cause.message
          : 'No hemos podido enviar tu mensaje. Comprueba la conexión y vuelve a intentarlo.'
      );
    } finally {
      setIsSending(false);
    }
  };

  return (
    <div className="space-y-6 pb-12">
      <div className="bg-white p-5 rounded-2xl border border-[#e5e2dd] ambient-shadow">
        <h2 className="font-headline font-extrabold text-xl text-[#1c1c19]">
          Sugerencias e incidencias
        </h2>
        <p className="text-xs text-[#76786b]">
          Cuéntanos qué te ha pasado o qué echas en falta. Lo leemos nosotros; no es un chat, así que
          si hace falta te respondemos por correo.
        </p>
      </div>

      <form
        onSubmit={submit}
        className="bg-white p-6 rounded-2xl border border-[#e5e2dd] shadow-2xs space-y-5"
      >
        <fieldset className="space-y-2">
          <legend className="text-xs font-bold uppercase tracking-wider text-[#45483c] mb-2">
            ¿Qué nos quieres contar?
          </legend>

          <div className="grid gap-2 sm:grid-cols-2">
            {KINDS.map((option) => (
              <label
                key={option.value}
                className={`flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-colors ${
                  kind === option.value
                    ? 'border-[#33450d] bg-[#eef2e0]'
                    : 'border-[#e5e2dd] bg-[#f6f3ee] hover:bg-[#f0ede8]'
                }`}
              >
                <input
                  type="radio"
                  name="feedback-kind"
                  value={option.value}
                  checked={kind === option.value}
                  onChange={() => {
                    setKind(option.value);
                    setIsSent(false);
                  }}
                  className="mt-1 accent-[#33450d]"
                />
                <span className="min-w-0">
                  <span className="flex items-center gap-1.5 text-sm font-semibold text-[#1c1c19]">
                    <span className="material-symbols-outlined text-base" aria-hidden="true">
                      {option.icon}
                    </span>
                    {option.label}
                  </span>
                  <span className="block text-[11px] text-[#76786b] mt-0.5">{option.hint}</span>
                </span>
              </label>
            ))}
          </div>
        </fieldset>

        <div className="space-y-1.5">
          <label
            htmlFor="feedback-message"
            className="block text-xs font-bold uppercase tracking-wider text-[#45483c]"
          >
            Cuéntanoslo
          </label>
          <textarea
            id="feedback-message"
            rows={7}
            value={message}
            maxLength={MAX_MESSAGE_LENGTH}
            onChange={(e) => {
              setMessage(e.target.value);
              setIsSent(false);
            }}
            placeholder="Qué estabas haciendo, qué esperabas que pasara y qué pasó."
            className="w-full px-4 py-3 bg-[#f6f3ee] border border-[#c6c8b8] rounded-xl text-sm text-[#1c1c19] resize-y"
          />
          <p className="text-[11px] text-[#76786b] text-right">
            {message.trim().length} / {MAX_MESSAGE_LENGTH}
          </p>
        </div>

        {/* Transparencia (RGPD art. 13): se dice **antes** de enviar qué se adjunta, y no en un
            documento aparte. Es también lo que evita la sorpresa de que el reporte lleve el correo
            de la cuenta, que va porque sin él no se puede responder. */}
        <div className="rounded-xl bg-[#f6f3ee] border border-[#e5e2dd] p-3 space-y-1">
          <p className="text-[11px] font-semibold text-[#45483c]">Qué se envía con tu mensaje</p>
          <ul className="text-[11px] text-[#76786b] list-disc pl-4 space-y-0.5">
            <li>Tu nombre y el correo de tu cuenta, para poder responderte.</li>
            <li>La versión de Terrenario que estás usando y tu navegador.</li>
            <li>La pantalla en la que estabas y la referencia del último error, si lo hubo.</li>
            <li>
              <strong>Nada de tu explotación</strong>: ni terrenos, ni campañas, ni registros.
            </li>
          </ul>
        </div>

        {error && (
          <p role="alert" className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm">
            {error}
          </p>
        )}

        {isSent && (
          <p
            role="status"
            className="p-3 bg-[#c9f16f] text-[#33450d] rounded-xl font-bold text-xs flex items-center gap-2"
          >
            <span className="material-symbols-outlined text-base" aria-hidden="true">
              check_circle
            </span>
            <span>Enviado. Gracias por contarlo: lo leemos y te respondemos si hace falta.</span>
          </p>
        )}

        <div className="pt-1 flex justify-end">
          <button
            type="submit"
            disabled={isSending || message.trim().length === 0}
            className="px-6 py-2.5 bg-[#33450d] hover:bg-[#4a5d23] text-white text-xs font-bold rounded-xl shadow-xs transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isSending ? 'Enviando…' : 'Enviar'}
          </button>
        </div>
      </form>
    </div>
  );
};
