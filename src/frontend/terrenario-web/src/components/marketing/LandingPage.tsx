import React from 'react';
import { useNavigate } from 'react-router-dom';

export const LandingPage: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col font-sans">
      {/* Navbar */}
      <header className="border-b border-[#e5e2dd] bg-[#fcf9f4]/90 backdrop-blur-md sticky top-0 z-40 px-6 lg:px-12 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-[#33450d] text-white flex items-center justify-center font-bold shadow-md">
            <span className="text-2xl">🌿</span>
          </div>
          <div>
            <h1 className="font-bold text-xl text-[#33450d] tracking-tight">Terrenario</h1>
            <p className="text-xs text-[#76786b] font-medium hidden sm:block">Tu tierra, bajo control</p>
          </div>
        </div>

        <nav className="hidden md:flex items-center gap-8 text-sm font-semibold text-[#45483c]">
          <a href="#beneficios" className="hover:text-[#33450d] transition-colors">Beneficios</a>
          <a href="#funciones" className="hover:text-[#33450d] transition-colors">Funcionalidades</a>
        </nav>

        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/login')}
            className="px-4 py-2 text-sm font-semibold text-[#33450d] hover:bg-[#f0ede8] rounded-xl transition-colors"
          >
            Ingresar
          </button>
          <button
            onClick={() => navigate('/login')}
            className="px-4 py-2 text-sm font-semibold text-white bg-[#33450d] hover:bg-[#4a5d23] rounded-xl shadow-sm transition-colors"
          >
            Empezar Gratis
          </button>
        </div>
      </header>

      {/* Hero */}
      <section className="relative px-6 lg:px-12 py-16 lg:py-24 max-w-5xl mx-auto w-full text-center space-y-8">
        <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[#c9f16f] text-[#33450d] text-xs font-bold border border-[#aed456]">
          ✨ Gestión agrícola sencilla
        </div>

        <h1 className="font-bold text-4xl sm:text-5xl lg:text-6xl text-[#1c1c19] tracking-tight leading-tight">
          Tu tierra, <span className="text-[#33450d]">bajo control.</span>
        </h1>

        <p className="text-lg sm:text-xl text-[#45483c] max-w-2xl mx-auto leading-relaxed">
          La herramienta sencilla para el agricultor moderno. Gestiona tus terrenos, cosechas y
          tareas diarias con precisión y claridad.
        </p>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
          <button
            onClick={() => navigate('/login')}
            className="flex items-center justify-center gap-3 px-6 py-3.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-base shadow-md transition-all"
          >
            Empezar gratis con Google
          </button>
        </div>

        <div className="pt-4 flex items-center justify-center gap-6 text-xs font-semibold text-[#76786b]">
          <span>✅ Sin tarjeta de crédito</span>
          <span>✅ Configuración en 2 minutos</span>
        </div>
      </section>

      {/* Benefits */}
      <section id="beneficios" className="bg-[#f0ede8] py-16 px-6 lg:px-12 border-y border-[#e5e2dd]">
        <div className="max-w-5xl mx-auto space-y-10">
          <div className="text-center space-y-3">
            <h2 className="font-bold text-3xl sm:text-4xl text-[#1c1c19]">
              Todo lo que necesitas, en un solo lugar
            </h2>
            <p className="text-base text-[#45483c]">Diseñado pensando en la realidad del trabajo en el campo.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {[
              { icon: '🗺️', title: 'Gestión de Terrenos', text: 'Organiza tus parcelas con conteo de árboles, ubicación y estado de poda en tiempo real.' },
              { icon: '🌾', title: 'Control de Cosechas', text: 'Registra la recolección por lotes, calcula rendimientos y asigna destinos de venta.' },
              { icon: '📅', title: 'Diario de Campo', text: 'Anota podas, riegos, fertilizaciones y asignación de personal en un muro cronológico.' },
            ].map((card) => (
              <div key={card.title} className="bg-[#fcf9f4] p-8 rounded-2xl border border-[#e5e2dd] shadow-sm space-y-4 hover:border-[#33450d] transition-all">
                <div className="text-4xl">{card.icon}</div>
                <h3 className="font-bold text-xl text-[#1c1c19]">{card.title}</h3>
                <p className="text-sm text-[#45483c] leading-relaxed">{card.text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-16 px-6 lg:px-12 bg-[#33450d] text-white text-center">
        <div className="max-w-3xl mx-auto space-y-6">
          <h2 className="font-bold text-3xl sm:text-4xl">
            Comienza a digitalizar tu finca hoy mismo
          </h2>
          <p className="text-base text-[#bed58e]">
            Únete a agricultores que ya gestionan sus cultivos de forma organizada.
          </p>
          <button
            onClick={() => navigate('/login')}
            className="px-8 py-4 rounded-xl bg-[#c9f16f] text-[#33450d] hover:bg-[#aed456] font-bold text-base shadow-lg transition-all"
          >
            Crear mi Workspace gratis
          </button>
        </div>
      </section>

      {/* Footer */}
      <footer className="mt-auto border-t border-[#e5e2dd] bg-[#f0ede8] py-8 px-6 lg:px-12 text-xs text-[#76786b]">
        <div className="max-w-5xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span>🌿</span>
            <span className="font-bold text-[#1c1c19]">Terrenario</span>
            <span>© 2026. Todos los derechos reservados.</span>
          </div>
          <div className="flex items-center gap-6">
            <button onClick={() => navigate('/login')} className="hover:underline">Iniciar Sesión</button>
          </div>
        </div>
      </footer>
    </div>
  );
};
