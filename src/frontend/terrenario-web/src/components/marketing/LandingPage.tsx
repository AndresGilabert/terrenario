import React from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * Landing pública. Recupera el lenguaje visual del prototipo (marca `eco`, tipografía display y
 * hero a dos columnas con imagen), manteniendo las decisiones de copy/CTA de MVP-106: un único
 * patrón de acceso ("Acceder"), sin reclamos de gratuidad ni métricas inventadas.
 */
export const LandingPage: React.FC = () => {
  const navigate = useNavigate();

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
            <h1 className="font-headline font-bold text-xl text-[#33450d] tracking-tight">Terrenario</h1>
            <p className="text-xs text-[#76786b] font-medium hidden sm:block">Tu tierra, bajo control</p>
          </div>
        </div>

        <nav className="hidden md:flex items-center gap-8 text-sm font-semibold text-[#45483c]">
          <a href="#beneficios" className="hover:text-[#33450d] transition-colors">Beneficios</a>
        </nav>

        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/login')}
            className="px-4 py-2 text-sm font-semibold text-white bg-[#33450d] hover:bg-[#4a5d23] rounded-xl shadow-sm transition-colors"
          >
            Acceder
          </button>
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
            <button
              onClick={() => navigate('/login')}
              className="flex items-center justify-center gap-3 px-6 py-3.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-base shadow-md transition-all"
            >
              Acceder a la plataforma
            </button>
          </div>
        </div>

        {/* Hero image (decorativa; sin métricas inventadas, coherente con MVP-106) */}
        <div className="lg:col-span-5">
          <div className="relative rounded-2xl overflow-hidden shadow-2xl border-4 border-white bg-[#f0ede8]">
            <img
              src="https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=1000&auto=format&fit=crop&q=80"
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

      {/* CTA */}
      <section className="py-16 px-6 lg:px-12 bg-[#33450d] text-white text-center">
        <div className="max-w-3xl mx-auto space-y-6">
          <h2 className="font-headline font-bold text-3xl sm:text-4xl">
            Comienza a digitalizar tu finca hoy mismo
          </h2>
          <p className="text-base text-[#bed58e]">
            Únete a agricultores que ya gestionan sus cultivos de forma organizada.
          </p>
          <button
            onClick={() => navigate('/login')}
            className="px-8 py-4 rounded-xl bg-[#c9f16f] text-[#33450d] hover:bg-[#aed456] font-bold text-base shadow-lg transition-all"
          >
            Acceder a la plataforma
          </button>
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
            <button onClick={() => navigate('/login')} className="hover:underline">Acceder</button>
          </div>
        </div>
      </footer>
    </div>
  );
};
