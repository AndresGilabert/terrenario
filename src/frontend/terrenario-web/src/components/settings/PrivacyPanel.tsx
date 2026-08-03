import React from 'react';
import { Link } from 'react-router';

/**
 * MVP-505 (HU-2, CA-2 · RN-042) — Panel de privacidad: el inventario de lo que la aplicación guarda
 * en el navegador y de con quién habla, consultable en cualquier momento.
 *
 * **Por qué esto y no un banner de cookies.** El producto no usa ninguna tecnología no esencial: no
 * hay analítica, ni publicidad, ni perfilado, y desde esta historia las tipografías se autoalojan, así
 * que tampoco se transfiere la IP de nadie a un tercero. La guía de la AEPD reserva el banner para las
 * tecnologías **no exentas**; mostrarlo cuando solo se usan las técnicas normaliza el clic automático
 * sin proteger nada, y es mala práctica reconocida.
 *
 * Lo que la norma sí exige —y esto entrega— es **informar**. Si algún día entra una tecnología no
 * esencial, `RN-042` obliga a recabar consentimiento antes de activarla, y este panel es donde vivirá
 * esa decisión.
 */
const TECHNOLOGIES: { name: string; purpose: string; essential: boolean }[] = [
  {
    name: 'Cookie de sesión (refresh_token)',
    purpose: 'Mantener tu sesión iniciada sin pedirte el acceso a cada rato.',
    essential: true,
  },
  {
    name: 'Token de acceso (almacenamiento de la pestaña)',
    purpose: 'Autorizar tus peticiones mientras navegas. Se borra al cerrar la pestaña.',
    essential: true,
  },
  {
    name: 'Avisos ya vistos',
    purpose: 'No repetirte el aviso de una invitación que ya has leído.',
    essential: true,
  },
  {
    name: 'Inicio de sesión con Google',
    purpose: 'Es el método con el que accedes: sin él no hay servicio.',
    essential: true,
  },
];

export const PrivacyPanel: React.FC = () => (
  <section className="bg-white rounded-2xl border border-[#e5e2dd] p-5 ambient-shadow space-y-4">
    <div>
      <h3 className="font-headline font-bold text-lg text-[#1c1c19]">Privacidad</h3>
      <p className="text-xs text-[#76786b] mt-1">
        Qué guarda Terrenario en tu navegador y con quién habla. Puedes consultarlo cuando quieras.
      </p>
    </div>

    <div className="rounded-xl bg-[#eef2e0] border border-[#c9dba0] p-3 flex items-start gap-2">
      <span className="material-symbols-outlined text-lg text-[#33450d] shrink-0" aria-hidden="true">
        verified_user
      </span>
      <p className="text-xs text-[#33450d] leading-relaxed">
        <strong>No usamos analítica, publicidad ni perfilado.</strong> Solo lo estrictamente necesario
        para que la aplicación funcione, así que no hay nada que consentir ni que rechazar. Si eso
        cambiara, te lo pediríamos antes de activarlo.
      </p>
    </div>

    <ul className="space-y-2">
      {TECHNOLOGIES.map((technology) => (
        <li
          key={technology.name}
          className="flex items-start justify-between gap-3 p-3 rounded-xl bg-[#f6f3ee] border border-[#e5e2dd]"
        >
          <div className="min-w-0">
            <p className="text-xs font-semibold text-[#1c1c19]">{technology.name}</p>
            <p className="text-[11px] text-[#76786b] mt-0.5">{technology.purpose}</p>
          </div>
          <span className="text-[10px] font-bold px-2 py-1 rounded-full bg-[#c9f16f] text-[#33450d] shrink-0">
            NECESARIA
          </span>
        </li>
      ))}
    </ul>

    <p className="text-xs text-[#76786b]">
      Más detalle en la{' '}
      <Link to="/legal/privacidad" className="font-semibold text-[#33450d] hover:underline">
        Política de Privacidad
      </Link>{' '}
      y en los{' '}
      <Link to="/legal/terminos" className="font-semibold text-[#33450d] hover:underline">
        Términos del Servicio
      </Link>
      .
    </p>
  </section>
);
