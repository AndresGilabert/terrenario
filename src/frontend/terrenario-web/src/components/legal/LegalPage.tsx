import React from 'react';
import { Link } from 'react-router';
import { legalEntity, missingLegalFields } from '../../config/legal-entity';

/**
 * MVP-505 (CA-1) — Armazón común de las páginas legales.
 *
 * Son **públicas**: se leen antes de entrar, que es justo cuando hacen falta (HU-1). Por eso viven
 * fuera de la guarda de sesión y no dependen de tener Workspace.
 */
export const LegalPage: React.FC<{
  title: string;
  updatedAt: string;
  children: React.ReactNode;
}> = ({ title, updatedAt, children }) => {
  const missing = missingLegalFields(legalEntity);

  return (
    <div className="min-h-screen bg-[#fcf9f4]">
      <header className="border-b border-[#e5e2dd] bg-white">
        <div className="max-w-3xl mx-auto px-5 py-4 flex items-center justify-between gap-4">
          <Link to="/" className="flex items-center gap-2 text-[#33450d]">
            <span className="material-symbols-outlined text-2xl" aria-hidden="true">eco</span>
            <span className="font-headline font-extrabold text-lg">Terrenario</span>
          </Link>
          <Link to="/" className="text-xs font-semibold text-[#45483c] hover:underline">
            Volver al inicio
          </Link>
        </div>
      </header>

      <main className="max-w-3xl mx-auto px-5 py-10">
        <h1 className="font-headline font-extrabold text-3xl text-[#1c1c19]">{title}</h1>
        <p className="mt-2 text-xs text-[#76786b]">Última actualización: {updatedAt}</p>

        {/* Aviso de estado del documento. Antes estaba escrito a mano —justo la clase de aviso que se
            queda puesto por olvido, o peor, se quita antes de tiempo—; ahora sale del dato. Con la
            identidad legal completa desaparece solo, y si alguien añade un campo y lo deja vacío
            vuelve a aparecer en vez de publicar un hueco. */}
        {missing.length > 0 && (
          <div className="mt-6 p-4 rounded-2xl bg-[#fff6e5] border border-[#f0d9a8] text-sm text-[#8a5a00]">
            <p className="font-semibold flex items-center gap-1.5">
              <span className="material-symbols-outlined text-lg" aria-hidden="true">info</span>
              Documento pendiente de completar
            </p>
            <p className="mt-1 text-xs leading-relaxed">
              Faltan datos del responsable del tratamiento. Mientras falten, esta página no es
              publicable y quien la lea tiene que saberlo.
            </p>
          </div>
        )}

        <article className="mt-8 space-y-6 text-sm leading-relaxed text-[#45483c] [&_h2]:font-headline [&_h2]:font-bold [&_h2]:text-lg [&_h2]:text-[#1c1c19] [&_h2]:mt-8 [&_h2]:mb-2 [&_ul]:list-disc [&_ul]:pl-5 [&_ul]:space-y-1 [&_table]:w-full [&_table]:text-xs [&_th]:text-left [&_th]:py-1.5 [&_td]:py-1.5 [&_td]:align-top [&_th]:border-b [&_th]:border-[#e5e2dd] [&_td]:border-b [&_td]:border-[#f0ede8]">
          {children}
        </article>

        <nav className="mt-12 pt-6 border-t border-[#e5e2dd] flex flex-wrap gap-4 text-xs font-semibold text-[#33450d]">
          <Link to="/legal/privacidad" className="hover:underline">Política de Privacidad</Link>
          <Link to="/legal/terminos" className="hover:underline">Términos del Servicio</Link>
        </nav>
      </main>
    </div>
  );
};
