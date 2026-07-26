import React, { useEffect, useRef, useState } from 'react';
import { authService } from '../../services/auth.service';
import { generateCodeVerifier, generateCodeChallenge, generateState } from '../../lib/pkce';
import { logLoginEvent } from '../../services/telemetry.service';
import {
  LoginFunnelEvent,
  beginLoginScreen,
  isLoginStarted,
  markLoginStarted,
} from '../../lib/login-telemetry';

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '';
const REDIRECT_URI = `${window.location.origin}/auth/callback`;

export const LoginPage: React.FC = () => {
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const flowIdRef = useRef<string>('');

  // MVP-105 — Traza del embudo de login (RN-020): "pantalla vista" al entrar y "abandono" al salir
  // sin haber pulsado Google. El éxito y el error los emite el servidor durante el intercambio.
  useEffect(() => {
    const flowId = beginLoginScreen();
    flowIdRef.current = flowId;
    logLoginEvent(LoginFunnelEvent.ScreenViewed, flowId);

    const handlePageHide = () => {
      if (!isLoginStarted()) {
        logLoginEvent(LoginFunnelEvent.Abandonment, flowId, { beacon: true });
      }
    };

    window.addEventListener('pagehide', handlePageHide);
    return () => window.removeEventListener('pagehide', handlePageHide);
  }, []);

  const handleGoogleLogin = async () => {
    setError(null);
    setIsLoading(true);

    try {
      const codeVerifier = generateCodeVerifier();
      const codeChallenge = await generateCodeChallenge(codeVerifier);
      const state = generateState();

      sessionStorage.setItem('pkce_code_verifier', codeVerifier);
      sessionStorage.setItem('oauth_state', state);

      // "login iniciado": a partir de aquí la salida de la página es la redirección a Google, no un
      // abandono.
      logLoginEvent(LoginFunnelEvent.GoogleClicked, flowIdRef.current);
      markLoginStarted();

      const authUrl = authService.buildGoogleAuthUrl({
        clientId: GOOGLE_CLIENT_ID,
        redirectUri: REDIRECT_URI,
        codeChallenge,
        state,
      });

      window.location.href = authUrl;
    } catch {
      setError('No se pudo iniciar el proceso de autenticación. Inténtalo de nuevo.');
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
      <div className="w-full max-w-md bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl text-center space-y-6">
        <div className="mx-auto w-16 h-16 rounded-2xl bg-[#33450d] text-white flex items-center justify-center shadow-lg">
          <span style={{ fontSize: '1.875rem' }}>🌿</span>
        </div>

        <div className="space-y-2">
          <h1 className="font-bold text-2xl text-[#1c1c19]">Terrenario</h1>
          <p className="text-sm text-[#45483c] leading-relaxed">
            Gestiona tu finca de forma sencilla. Sin contraseñas complicadas, accede
            directamente con tu cuenta de Google.
          </p>
        </div>

        {error && (
          <div
            role="alert"
            className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm"
          >
            {error}
          </div>
        )}

        <button
          onClick={handleGoogleLogin}
          disabled={isLoading}
          className="w-full flex items-center justify-center gap-3 px-5 py-3.5 rounded-xl border border-[#c6c8b8] bg-white hover:bg-[#f6f3ee] text-[#1c1c19] font-semibold text-sm shadow-xs transition-all hover:border-[#33450d] disabled:opacity-60 disabled:cursor-not-allowed"
        >
          <GoogleLogoIcon />
          <span>{isLoading ? 'Redirigiendo…' : 'Continuar con Google'}</span>
        </button>
      </div>

      {/*
        Enlaces legales (MVP-106, CA-3): el contenido legal (Política de Privacidad y Términos del
        Servicio) está diferido a P-008 (MVP-999). Hasta que exista, se muestran deshabilitados con
        indicación "próximamente" en vez de navegar a rutas inexistentes que el catch-all redirigía
        a la landing (recarga incluida). No inician ninguna navegación.
      */}
      <footer className="mt-8 text-center text-xs text-[#76786b] space-x-4">
        <button
          type="button"
          disabled
          title="Disponible próximamente"
          aria-label="Política de Privacidad (disponible próximamente)"
          className="cursor-not-allowed opacity-60"
        >
          Política de Privacidad
        </button>
        <span aria-hidden="true">•</span>
        <button
          type="button"
          disabled
          title="Disponible próximamente"
          aria-label="Términos del Servicio (disponible próximamente)"
          className="cursor-not-allowed opacity-60"
        >
          Términos del Servicio
        </button>
      </footer>
    </div>
  );
};

function GoogleLogoIcon() {
  return (
    <svg className="w-5 h-5" viewBox="0 0 24 24" aria-hidden="true">
      <path
        d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
        fill="#4285F4"
      />
      <path
        d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
        fill="#34A853"
      />
      <path
        d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z"
        fill="#FBBC05"
      />
      <path
        d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z"
        fill="#EA4335"
      />
    </svg>
  );
}
