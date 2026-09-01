import React from 'react';
import {
  ANY_EMAIL_WORKS_HINT,
  GOOGLE_ACCOUNT_SIGNUP_LABEL,
  GOOGLE_ACCOUNT_SIGNUP_URL,
} from '../../lib/google-account';
import { LANDING_CONTENTS } from '../../content/landings';

/**
 * Landing pública (la home, `/`). Recupera el lenguaje visual del prototipo (marca `eco`,
 * tipografía display y hero a dos columnas con imagen), manteniendo las decisiones de copy/CTA de
 * MVP-106: un único patrón de acceso ("Acceder"), sin reclamos de gratuidad ni métricas inventadas.
 *
 * MKT-102 — Sin `react-router`, igual que `ContentLandingPage`: se pre-renderiza a HTML estático en
 * el build (`scripts/prerenderizar-landings.mjs` -> `dist/home.html`, servido en `/` por un
 * middleware propio en `Program.cs`, **no** por `dist/index.html`) y no necesita Router para eso.
 */
export const LandingPage: React.FC = () => {
  const funcionalidades = LANDING_CONTENTS.filter((content) => content.cluster === 'funcionalidad');
  const perfiles = LANDING_CONTENTS.filter((content) => content.cluster === 'perfil');

  const benefits = [
    {
      icon: 'map',
      bg: '#4a5d23',
      title: 'Gestión de Terrenos',
      text: 'Organiza tus parcelas con conteo de árboles, ubicación y estado de poda en tiempo real.',
    },
    {
      icon: 'agriculture',
      bg: '#4c6700',
      title: 'Control de Cosechas',
      text: 'Registra la recolección por lotes, calcula rendimientos y asigna destinos de venta.',
    },
    {
      icon: 'event_note',
      bg: '#5a3811',
      title: 'Diario de Campo',
      text: 'Anota podas, riegos, fertilizaciones y asignación de personal en un muro cronológico.',
    },
  ];

  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col">
      {/* Navbar */}
      <header className="border-b border-[#e5e2dd] bg-[#fcf9f4]/90 backdrop-blur-md sticky top-0 z-40 px-6 lg:px-12 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-[#33450d] text-white flex items-center justify-center shadow-md">
            <span className="material-symbols-outlined fill text-2xl" aria-hidden="true">eco</span>
          </div>
          <div>
            <p className="font-headline font-bold text-xl text-[#33450d] tracking-tight">Terrenario</p>
            <p className="text-xs text-[#76786b] font-medium hidden sm:block">Tu tierra, bajo control</p>
          </div>
        </div>

        <nav className="hidden md:flex items-center gap-8 text-sm font-semibold text-[#45483c]">
          <a href="#beneficios" className="hover:text-[#33450d] transition-colors">Beneficios</a>
        </nav>

        <div className="flex items-center gap-3">
          <a
            href="/login"
            className="px-4 py-2 text-sm font-semibold text-white bg-[#33450d] hover:bg-[#4a5d23] rounded-xl shadow-sm transition-colors"
          >
            Acceder
          </a>
        </div>
      </header>

      {/* Hero */}
      <section className="relative px-6 lg:px-12 py-12 lg:py-20 max-w-7xl mx-auto w-full grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
        <div className="lg:col-span-7 space-y-6">
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[#c9f16f] text-[#33450d] text-xs font-bold border border-[#aed456]">
            <span className="material-symbols-outlined text-base" aria-hidden="true">auto_awesome</span>
            <span>Gestión agrícola sencilla</span>
          </div>

          <h1 className="font-headline font-extrabold text-4xl sm:text-5xl lg:text-6xl text-[#1c1c19] tracking-tight leading-[1.1]">
            Tu tierra, <br className="hidden sm:block" />
            <span className="text-[#33450d]">bajo control.</span>
          </h1>

          <p className="text-lg sm:text-xl text-[#45483c] max-w-2xl leading-relaxed">
            La herramienta sencilla para el agricultor moderno. Gestiona tus terrenos, cosechas y
            tareas diarias con precisión y claridad.
          </p>

          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 pt-2">
            <a
              href="/login"
              className="flex items-center justify-center gap-3 px-6 py-3.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-base shadow-md transition-all"
            >
              Acceder a la plataforma
            </a>
          </div>

          {/* MVP-712 (CA-2) — Antes de pedir nada. Esta es la pantalla donde se decide si probar el
              producto, y hasta ahora no decía con qué se entra: quien no tiene Gmail se enteraba en
              el login, o no llegaba. El enlace al alta es **enlace**, no recurso: la landing es
              pública y su CSP no admite terceros (`RN-042`). */}
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
        </div>

        {/* Hero image (decorativa; sin métricas inventadas, coherente con MVP-106) */}
        <div className="lg:col-span-5">
          <div className="relative rounded-2xl overflow-hidden shadow-2xl border-4 border-white bg-[#f0ede8]">
            {/* MVP-599 — Autoalojada. Venía de `images.unsplash.com`, y eso comunicaba la IP de cada
                visitante a un tercero: contradice lo que declara la Política de Privacidad y lo que
                afirma la checklist de cumplimiento. La CSP la bloqueaba, que era la señal correcta. */}
            <img
              src="/campo.jpg"
              alt=""
              aria-hidden="true"
              className="w-full h-[300px] sm:h-[420px] object-cover"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/55 via-transparent to-transparent flex flex-col justify-end p-6 text-white">
              <span className="text-xs font-bold uppercase tracking-wider text-[#c9f16f]">Terrenario</span>
              <h3 className="font-headline text-2xl font-bold">Tu explotación, organizada</h3>
            </div>
          </div>
        </div>
      </section>

      {/* Benefits */}
      <section id="beneficios" className="bg-[#f0ede8] py-16 px-6 lg:px-12 border-y border-[#e5e2dd]">
        <div className="max-w-7xl mx-auto space-y-12">
          <div className="text-center max-w-2xl mx-auto space-y-3">
            <h2 className="font-headline font-bold text-3xl sm:text-4xl text-[#1c1c19]">
              Todo lo que necesitas, en un solo lugar
            </h2>
            <p className="text-base text-[#45483c]">Diseñado pensando en la realidad del trabajo en el campo.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {benefits.map((card) => (
              <div
                key={card.title}
                className="bg-[#fcf9f4] p-8 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-4 hover:border-[#33450d] transition-all"
              >
                <div
                  className="w-12 h-12 rounded-xl text-white flex items-center justify-center"
                  style={{ backgroundColor: card.bg }}
                >
                  <span className="material-symbols-outlined text-2xl" aria-hidden="true">{card.icon}</span>
                </div>
                <h3 className="font-headline font-bold text-xl text-[#1c1c19]">{card.title}</h3>
                <p className="text-sm text-[#45483c] leading-relaxed">{card.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* MKT-102 (CA-3) — Hub de enlazado a las landings públicas de funcionalidades y casos de
          uso. Son páginas estáticas pre-renderizadas fuera de la SPA (ver
          `components/marketing/ContentLandingPage.tsx`), así que los enlaces son `<a>` reales y no
          `<Link>`: no están dadas de alta en el router del cliente y una navegación de React Router
          hacia una ruta que no existe ahí caería en el 404 de la SPA en vez de servir la página. */}
      <section aria-label="Funcionalidades" className="py-16 px-6 lg:px-12 max-w-7xl mx-auto w-full space-y-8">
        <div className="text-center max-w-2xl mx-auto space-y-3">
          <h2 className="font-headline font-bold text-3xl sm:text-4xl text-[#1c1c19]">Explora por funcionalidad</h2>
        </div>
        <ul className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {funcionalidades.map((content) => (
            <li key={content.slug}>
              <a
                href={content.path}
                className="block p-5 rounded-xl border border-[#e5e2dd] hover:border-[#33450d] transition-colors"
              >
                <span className="font-semibold text-[#33450d]">{content.navLabel}</span>
              </a>
            </li>
          ))}
        </ul>

        <div className="text-center max-w-2xl mx-auto space-y-3 pt-6">
          <h2 className="font-headline font-bold text-3xl sm:text-4xl text-[#1c1c19]">¿Para quién es Terrenario?</h2>
        </div>
        <ul className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {perfiles.map((content) => (
            <li key={content.slug}>
              <a
                href={content.path}
                className="block p-5 rounded-xl border border-[#e5e2dd] hover:border-[#33450d] transition-colors"
              >
                <span className="font-semibold text-[#33450d]">{content.navLabel}</span>
              </a>
            </li>
          ))}
        </ul>
      </section>

      {/* CTA */}
      <section className="py-16 px-6 lg:px-12 bg-[#33450d] text-white text-center">
        <div className="max-w-3xl mx-auto space-y-6">
          <h2 className="font-headline font-bold text-3xl sm:text-4xl">
            Comienza a digitalizar tu finca hoy mismo
          </h2>
          <p className="text-base text-[#bed58e]">
            Únete a agricultores que ya gestionan sus cultivos de forma organizada.
          </p>
          <a
            href="/login"
            className="inline-block px-8 py-4 rounded-xl bg-[#c9f16f] text-[#33450d] hover:bg-[#aed456] font-bold text-base shadow-lg transition-all"
          >
            Acceder a la plataforma
          </a>
        </div>
      </section>

      {/* Footer */}
      <footer className="mt-auto border-t border-[#e5e2dd] bg-[#f0ede8] py-8 px-6 lg:px-12 text-xs text-[#76786b]">
        <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined fill text-[#33450d]" aria-hidden="true">eco</span>
            <span className="font-headline font-bold text-[#1c1c19]">Terrenario</span>
            <span>© 2026. Todos los derechos reservados.</span>
          </div>
          <div className="flex items-center gap-6">
            {/* MVP-505 (CA-1) — Las páginas legales tienen que alcanzarse también desde la landing:
                es la primera pantalla y la única que ve quien todavía no ha entrado. */}
            <a href="/legal/privacidad" className="hover:underline">Privacidad</a>
            <a href="/legal/terminos" className="hover:underline">Términos</a>
            <a href="/login" className="hover:underline">Acceder</a>
          </div>
        </div>
      </footer>
    </div>
  );
};
