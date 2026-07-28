import React from 'react';
import { ViewMode } from '../types';

interface LoginPageProps {
  onLoginSuccess: () => void;
  onNavigate: (view: ViewMode) => void;
}

export const LoginPage: React.FC<LoginPageProps> = ({ onLoginSuccess, onNavigate }) => {
  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl text-center space-y-6">
        {/* Leaf Icon Header */}
        <div className="mx-auto w-16 h-16 rounded-2xl bg-[#33450d] text-white flex items-center justify-center shadow-lg">
          <span className="material-symbols-outlined text-3xl fill">eco</span>
        </div>

        {/* Title & Microcopy */}
        <div className="space-y-2">
          <h1 className="font-headline font-extrabold text-2xl text-[#1c1c19]">Terrenario</h1>
          <p className="text-sm text-[#45483c] leading-relaxed">
            Gestiona tu finca de forma sencilla. Sin contraseñas complicadas, accede directamente con tu cuenta de Google.
          </p>
        </div>

        {/* Google Login Button */}
        <button
          onClick={onLoginSuccess}
          className="w-full flex items-center justify-center gap-3 px-5 py-3.5 rounded-xl border border-[#c6c8b8] bg-white hover:bg-[#f6f3ee] text-[#1c1c19] font-semibold text-sm shadow-xs transition-all hover:border-[#33450d]"
        >
          <svg className="w-5 h-5" viewBox="0 0 24 24">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" fill="#FBBC05"/>
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" fill="#EA4335"/>
          </svg>
          <span>Continuar con Google</span>
        </button>

        {/* Demo shortcut */}
        <div className="pt-2 border-t border-[#e5e2dd]">
          <p className="text-xs text-[#76786b] mb-2">¿Quieres ver la aplicación antes?</p>
          <button
            onClick={() => onNavigate('diario')}
            className="text-xs font-semibold text-[#33450d] hover:underline"
          >
            Acceder como invitado / Demo
          </button>
        </div>
      </div>

      {/* Footer */}
      <div className="mt-8 text-center text-xs text-[#76786b] space-x-4">
        <a href="#privacidad" className="hover:underline">Política de Privacidad</a>
        <span>•</span>
        <a href="#terminos" className="hover:underline">Términos del Servicio</a>
      </div>
    </div>
  );
};
