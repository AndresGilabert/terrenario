import React from 'react';
import { ViewMode } from '../types';

interface LandingPageProps {
  onNavigate: (view: ViewMode) => void;
}

export const LandingPage: React.FC<LandingPageProps> = ({ onNavigate }) => {
  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col font-sans">
      {/* Navbar */}
      <header className="border-b border-[#e5e2dd] bg-[#fcf9f4]/90 backdrop-blur-md sticky top-0 z-40 px-6 lg:px-12 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3 cursor-pointer" onClick={() => onNavigate('landing')}>
          <div className="w-10 h-10 rounded-xl bg-[#33450d] text-white flex items-center justify-center font-bold shadow-md">
            <span className="material-symbols-outlined text-2xl fill">eco</span>
          </div>
          <div>
            <h1 className="font-headline font-bold text-xl text-[#33450d] tracking-tight">Terrenario</h1>
            <p className="text-xs text-[#76786b] font-medium hidden sm:block">Tu tierra, bajo control</p>
          </div>
        </div>

        <nav className="hidden md:flex items-center gap-8 text-sm font-semibold text-[#45483c]">
          <a href="#beneficios" className="hover:text-[#33450d] transition-colors">Beneficios</a>
          <a href="#funciones" className="hover:text-[#33450d] transition-colors">Funcionalidades</a>
          <a href="#testimonios" className="hover:text-[#33450d] transition-colors">Testimonios</a>
        </nav>

        <div className="flex items-center gap-3">
          <button
            onClick={() => onNavigate('login')}
            className="px-4 py-2 text-sm font-semibold text-[#33450d] hover:bg-[#f0ede8] rounded-xl transition-colors"
          >
            Ingresar
          </button>
          <button
            onClick={() => onNavigate('onboarding_step1')}
            className="px-4 py-2 text-sm font-semibold text-white bg-[#33450d] hover:bg-[#4a5d23] rounded-xl shadow-xs transition-colors"
          >
            Empezar Gratis
          </button>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative px-6 lg:px-12 py-12 lg:py-20 max-w-7xl mx-auto w-full grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
        <div className="lg:col-span-7 space-y-6">
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-[#c9f16f] text-[#33450d] text-xs font-bold border border-[#aed456] shadow-2xs">
            <span className="material-symbols-outlined text-base">auto_awesome</span>
            <span>Novedad: Gestión por Campañas 2024</span>
          </div>

          <h1 className="font-headline font-extrabold text-4xl sm:text-5xl lg:text-6xl text-[#1c1c19] tracking-tight leading-[1.1]">
            Tu tierra, <br />
            <span className="text-[#33450d]">bajo control.</span>
          </h1>

          <p className="text-lg sm:text-xl text-[#45483c] max-w-2xl font-normal leading-relaxed">
            La herramienta sencilla para el agricultor moderno. Gestiona tus terrenos, cosechas y tareas diarias con precisión, claridad y calidez.
          </p>

          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 pt-2">
            <button
              onClick={() => onNavigate('onboarding_step1')}
              className="flex items-center justify-center gap-3 px-6 py-3.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white font-semibold text-base shadow-md transition-all hover:scale-[1.01]"
            >
              <svg className="w-5 h-5 fill-current" viewBox="0 0 24 24">
                <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
                <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
                <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" fill="#FBBC05"/>
                <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" fill="#EA4335"/>
              </svg>
              <span>Empezar gratis con Google</span>
            </button>

            <button
              onClick={() => onNavigate('diario')}
              className="flex items-center justify-center gap-2 px-6 py-3.5 rounded-xl bg-white hover:bg-[#f0ede8] border border-[#c6c8b8] text-[#1c1c19] font-semibold text-base transition-colors"
            >
              <span className="material-symbols-outlined text-[#33450d]">play_circle</span>
              <span>Probar Demo interactiva</span>
            </button>
          </div>

          <div className="pt-4 flex items-center gap-6 text-xs font-semibold text-[#76786b]">
            <div className="flex items-center gap-1.5">
              <span className="material-symbols-outlined text-base text-[#4c6700]">check_circle</span>
              <span>Sin tarjeta de crédito</span>
            </div>
            <div className="flex items-center gap-1.5">
              <span className="material-symbols-outlined text-base text-[#4c6700]">check_circle</span>
              <span>Configuración en 2 minutos</span>
            </div>
          </div>
        </div>

        {/* Hero Banner Box */}
        <div className="lg:col-span-5 relative">
          <div className="relative rounded-2xl overflow-hidden shadow-2xl border-4 border-white bg-[#f0ede8]">
            <img
              src="https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=1000&auto=format&fit=crop&q=80"
              alt="Olivar Terrenario al amanecer"
              className="w-full h-[380px] sm:h-[440px] object-cover"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent flex flex-col justify-end p-6 text-white">
              <span className="text-xs font-bold uppercase tracking-wider text-[#c9f16f]">Finca El Olivar</span>
              <h3 className="font-headline text-2xl font-bold">Campaña 2024 en curso</h3>
              <p className="text-xs text-stone-200 mt-1">45,000 Kg recolectados • 4 Terrenos activos</p>
            </div>
          </div>

          {/* Floating badge */}
          <div className="absolute -bottom-6 -left-6 bg-white p-4 rounded-2xl shadow-xl border border-[#e5e2dd] hidden sm:flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-[#c9f16f] text-[#33450d] flex items-center justify-center font-bold">
              <span className="material-symbols-outlined text-xl">trending_up</span>
            </div>
            <div>
              <p className="text-xs text-[#76786b] font-medium">Rendimiento Medio</p>
              <p className="text-sm font-extrabold text-[#1c1c19]">20.5% Aceite Picual</p>
            </div>
          </div>
        </div>
      </section>

      {/* Benefits Section */}
      <section id="beneficios" className="bg-[#f0ede8] py-16 px-6 lg:px-12 border-y border-[#e5e2dd]">
        <div className="max-w-7xl mx-auto space-y-12">
          <div className="text-center max-w-2xl mx-auto space-y-3">
            <h2 className="font-headline font-bold text-3xl sm:text-4xl text-[#1c1c19]">
              Todo lo que necesitas, en un solo lugar
            </h2>
            <p className="text-base text-[#45483c]">
              Diseñado pensando en la realidad del trabajo en el campo. Sin complicaciones técnicas.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Card 1 */}
            <div className="bg-[#fcf9f4] p-8 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-4 hover:border-[#33450d] transition-all">
              <div className="w-12 h-12 rounded-xl bg-[#4a5d23] text-white flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">map</span>
              </div>
              <h3 className="font-headline font-bold text-xl text-[#1c1c19]">Gestión de Terrenos</h3>
              <p className="text-sm text-[#45483c] leading-relaxed">
                Organiza tus parcelas con conteo de olivos, alias por sector, métodos de riego y estado de poda en tiempo real.
              </p>
            </div>

            {/* Card 2 */}
            <div className="bg-[#fcf9f4] p-8 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-4 hover:border-[#33450d] transition-all">
              <div className="w-12 h-12 rounded-xl bg-[#4c6700] text-white flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">agriculture</span>
              </div>
              <h3 className="font-headline font-bold text-xl text-[#1c1c19]">Control de Cosechas</h3>
              <p className="text-sm text-[#45483c] leading-relaxed">
                Registra la recolección por lotes, calcula automáticamente el % de rendimiento graso y asigna almazaras de destino.
              </p>
            </div>

            {/* Card 3 */}
            <div className="bg-[#fcf9f4] p-8 rounded-2xl border border-[#e5e2dd] shadow-xs space-y-4 hover:border-[#33450d] transition-all">
              <div className="w-12 h-12 rounded-xl bg-[#5a3811] text-white flex items-center justify-center">
                <span className="material-symbols-outlined text-2xl">event_note</span>
              </div>
              <h3 className="font-headline font-bold text-xl text-[#1c1c19]">Diario de Campo</h3>
              <p className="text-sm text-[#45483c] leading-relaxed">
                Anota podas, riegos, fertilizaciones, compras de insumos y asignación de personal en un muro cronológico claro.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-16 px-6 lg:px-12 bg-[#33450d] text-white text-center">
        <div className="max-w-3xl mx-auto space-y-6">
          <h2 className="font-headline font-bold text-3xl sm:text-4xl">
            Comienza a digitalizar tu finca hoy mismo
          </h2>
          <p className="text-base text-[#bed58e]">
            Únete a cientos de agricultores que ya gestionan sus cultivos de forma organizada y sin esfuerzo.
          </p>
          <div className="pt-2">
            <button
              onClick={() => onNavigate('onboarding_step1')}
              className="px-8 py-4 rounded-xl bg-[#c9f16f] text-[#33450d] hover:bg-[#aed456] font-bold text-base shadow-lg transition-all"
            >
              Crear mi Workspace gratis
            </button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="mt-auto border-t border-[#e5e2dd] bg-[#f0ede8] py-8 px-6 lg:px-12 text-xs text-[#76786b]">
        <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-[#33450d] fill">eco</span>
            <span className="font-headline font-bold text-[#1c1c19]">Terrenario</span>
            <span>© 2024. Todos los derechos reservados.</span>
          </div>
          <div className="flex items-center gap-6">
            <button onClick={() => onNavigate('login')} className="hover:underline">Iniciar Sesión</button>
            <button onClick={() => onNavigate('ajustes')} className="hover:underline">Soporte y Ayuda</button>
          </div>
        </div>
      </footer>
    </div>
  );
};
