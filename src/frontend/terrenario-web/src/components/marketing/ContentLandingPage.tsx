import React from 'react';
import {
  ANY_EMAIL_WORKS_HINT,
  GOOGLE_ACCOUNT_SIGNUP_LABEL,
  GOOGLE_ACCOUNT_SIGNUP_URL,
} from '../../lib/google-account';
import { getRelatedLandings, type LandingContent } from '../../content/landings';

/**
 * MKT-102 — Página pública de una funcionalidad o de un caso de uso (`/funcionalidades/*`,
 * `/para/*`).
 *
 * A diferencia de `LandingPage` (la home, montada dentro de la SPA), este componente **no usa
 * `react-router`**: se pre-renderiza a HTML estático en el `build`
 * (`scripts/prerenderizar-landings.mjs`) y no necesita hidratarse, porque no tiene ningún estado ni
 * interacción propia — solo enlaces. Usar `<a>` en vez de `<Link>` es lo que permite renderizarlo
 * fuera de un `<BrowserRouter>` sin envolverlo en nada, tanto en el `build` como en los tests.
 *
 * El estilo replica el de `LandingPage` a propósito: es la misma marca, y una landing que se ve
 * distinta a la home mina la confianza justo en la pantalla que tiene que generarla.
 */
export const ContentLandingPage: React.FC<{ content: LandingContent }> = ({ content }) => {
  const relatedLandings = getRelatedLandings(content);

  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col">
      <header className="border-b border-[#e5e2dd] bg-[#fcf9f4]/90 px-6 lg:px-12 py-4 flex items-center justify-between">
        <a href="/" className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-[#33450d] text-white flex items-center justify-center shadow-md">
            <span className="material-symbols-outlined fill text-2xl" aria-hidden="true">eco</span>
          </div>
          <div>
            <h2 className="font-headline font-bold text-xl text-[#33450d] tracking-tight">Terrenario</h2>
            <p className="text-xs text-[#76786b] font-medium hidden sm:block">Tu tierra, bajo control</p>
          </div>
        </a>

        <a
          href="/login"
          className="px-4 py-2 text-sm font-semibold text-white bg-[#33450d] hover:bg-[#4a5d23] rounded-xl shadow-sm transition-colors"
        >
          Acceder
        </a>
      </header>

      <main>
        <section className="px-6 lg:px-12 py-12 lg:py-16 max-w-4xl mx-auto w-full space-y-6">
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[#c9f16f] text-[#33450d] text-xs font-bold border border-[#aed456]">
            <span>{content.eyebrow}</span>
          </div>

          <h1 className="font-headline font-extrabold text-3xl sm:text-4xl lg:text-5xl text-[#1c1c19] tracking-tight leading-[1.15]">
            {content.h1}
          </h1>

          <p className="text-lg text-[#45483c] leading-relaxed max-w-3xl">{content.intro}</p>

          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 pt-2">
            <a
              href="/login"
              className="flex items-center justify-center gap-3 px-6 py-3.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-base shadow-md transition-all"
            >
              Acceder a la plataforma
            </a>
          </div>

          <p className="text-sm text-[#76786b] leading-relaxed max-w-xl">
            Se entra con una Cuenta de Google. {ANY_EMAIL_WORKS_HINT}{' '}
            <a
              href={GOOGLE_ACCOUNT_SIGNUP_URL}
              target="_blank"
              rel="noreferrer"
              className="font-semibold text-[#33450d] hover:underline"
            >
              {GOOGLE_ACCOUNT_SIGNUP_LABEL}
            </a>
          </p>
        </section>

        <section className="bg-[#f0ede8] py-14 px-6 lg:px-12 border-y border-[#e5e2dd]">
          <div className="max-w-5xl mx-auto grid grid-cols-1 md:grid-cols-3 gap-6">
            {content.bullets.map((bullet) => (
              <div
                key={bullet.title}
                className="bg-[#fcf9f4] p-8 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-4"
              >
                <div className="w-12 h-12 rounded-xl text-white flex items-center justify-center bg-[#33450d]">
                  <span className="material-symbols-outlined text-2xl" aria-hidden="true">{bullet.icon}</span>
                </div>
                <h3 className="font-headline font-bold text-xl text-[#1c1c19]">{bullet.title}</h3>
                <p className="text-sm text-[#45483c] leading-relaxed">{bullet.text}</p>
              </div>
            ))}
          </div>
        </section>

        <section aria-labelledby="faq-heading" className="py-14 px-6 lg:px-12">
          <div className="max-w-4xl mx-auto space-y-6">
            <h2 id="faq-heading" className="font-headline font-bold text-2xl text-[#1c1c19]">
              Preguntas frecuentes
            </h2>
            <dl className="space-y-4">
              {content.faqs.map((faq) => (
                <div key={faq.question} className="border-b border-[#e5e2dd] pb-4">
                  <dt className="font-headline font-bold text-lg text-[#1c1c19]">{faq.question}</dt>
                  <dd className="mt-2 text-sm text-[#45483c] leading-relaxed">{faq.answer}</dd>
                </div>
              ))}
            </dl>
          </div>
        </section>

        {relatedLandings.length > 0 && (
          <nav aria-label="Funcionalidades relacionadas" className="py-14 px-6 lg:px-12">
            <div className="max-w-5xl mx-auto space-y-6">
              <h2 className="font-headline font-bold text-2xl text-[#1c1c19]">También te puede interesar</h2>
              <ul className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {relatedLandings.map((related) => (
                  <li key={related.slug}>
                    <a
                      href={related.path}
                      className="block p-5 rounded-xl border border-[#e5e2dd] hover:border-[#33450d] transition-colors"
                    >
                      <span className="font-semibold text-[#33450d]">{related.navLabel}</span>
                      <p className="text-sm text-[#45483c] mt-1">{related.h1}</p>
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          </nav>
        )}

        <section className="py-16 px-6 lg:px-12 bg-[#33450d] text-white text-center">
          <div className="max-w-3xl mx-auto space-y-6">
            <h2 className="font-headline font-bold text-3xl sm:text-4xl">
              Comienza a digitalizar tu finca hoy mismo
            </h2>
            <a
              href="/login"
              className="inline-block px-8 py-4 rounded-xl bg-[#c9f16f] text-[#33450d] hover:bg-[#aed456] font-bold text-base shadow-lg transition-all"
            >
              Acceder a la plataforma
            </a>
          </div>
        </section>
      </main>

      <footer className="mt-auto border-t border-[#e5e2dd] bg-[#f0ede8] py-8 px-6 lg:px-12 text-xs text-[#76786b]">
        <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined fill text-[#33450d]" aria-hidden="true">eco</span>
            <span className="font-headline font-bold text-[#1c1c19]">Terrenario</span>
            <span>© 2026. Todos los derechos reservados.</span>
          </div>
          <div className="flex items-center gap-6">
            <a href="/" className="hover:underline">Inicio</a>
            <a href="/legal/privacidad" className="hover:underline">Privacidad</a>
            <a href="/legal/terminos" className="hover:underline">Términos</a>
            <a href="/login" className="hover:underline">Acceder</a>
          </div>
        </div>
      </footer>
    </div>
  );
};
