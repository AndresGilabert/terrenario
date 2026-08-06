import { Link } from 'react-router';
import React, { useEffect, useRef, useState } from 'react';
import { authService } from '../../services/auth.service';
import { generateCodeVerifier, generateCodeChallenge, generateState } from '../../lib/pkce';
import { logLoginEvent } from '../../services/telemetry.service';
import {
  LOGIN_INACTIVITY_TIMEOUT_MS,
  LoginFunnelEvent,
  beginLoginScreen,
  isLoginStarted,
  markLoginStarted,
  restartLoginFlow,
} from '../../lib/login-telemetry';

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID ?? '';
const REDIRECT_URI = `${window.location.origin}/auth/callback`;

export const LoginPage: React.FC = () => {
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const flowIdRef = useRef<string>('');
  const abandonedRef = useRef(false);

  // MVP-105 · MVP-601 — Traza del embudo de login (RN-020): "pantalla vista" al entrar y "abandono"
  // al salir sin haber pulsado Google **o tras quedarse quieto** en la pantalla. El éxito y el error
  // los emite el servidor durante el intercambio.
  //
  // Hasta MVP-601 el abandono solo se emitía al salir de la página, y `observabilidad.md` pide las dos
  // vías. La que faltaba es justo la que cuenta el caso más silencioso: la pestaña que se queda
  // abierta en el login y a la que nadie vuelve, que nunca dispara `pagehide`.
  useEffect(() => {
    const flowId = beginLoginScreen();
    flowIdRef.current = flowId;
    logLoginEvent(LoginFunnelEvent.ScreenViewed, flowId);

    let inactivityTimer: number | undefined;

    const reportAbandonment = (beacon: boolean) => {
      // Quien ya pulsó Google no ha abandonado: está en Google. Y un intento no se abandona dos veces.
      if (isLoginStarted() || abandonedRef.current) return;
      abandonedRef.current = true;
      logLoginEvent(LoginFunnelEvent.Abandonment, flowIdRef.current, { beacon });
    };

    const armInactivityTimer = () => {
      window.clearTimeout(inactivityTimer);
      inactivityTimer = window.setTimeout(
        () => reportAbandonment(false),
        LOGIN_INACTIVITY_TIMEOUT_MS
      );
    };

    // Volver a interactuar tras un abandono ya emitido abre un intento **nuevo**: si se reutilizara el
    // mismo flow_id, ese intento sumaría abandono y éxito a la vez y la conversión del embudo dejaría
    // de cuadrar.
    const handleActivity = () => {
      if (abandonedRef.current) {
        const renewedFlowId = restartLoginFlow();
        flowIdRef.current = renewedFlowId;
        abandonedRef.current = false;
        logLoginEvent(LoginFunnelEvent.ScreenViewed, renewedFlowId);
      }
      armInactivityTimer();
    };

    const handlePageHide = () => reportAbandonment(true);

    armInactivityTimer();
    window.addEventListener('pointerdown', handleActivity);
    window.addEventListener('keydown', handleActivity);
    window.addEventListener('pagehide', handlePageHide);

    return () => {
      window.clearTimeout(inactivityTimer);
      window.removeEventListener('pointerdown', handleActivity);
      window.removeEventListener('keydown', handleActivity);
      window.removeEventListener('pagehide', handlePageHide);
    };
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
          <span className="material-symbols-outlined fill text-3xl" aria-hidden="true">eco</span>
        </div>

        <div className="space-y-2">
          <h1 className="font-headline font-extrabold text-2xl text-[#1c1c19]">Terrenario</h1>
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

      {/* MVP-505 (CA-1) — Enlaces legales **vivos**. Hasta ahora eran botones deshabilitados con
          «próximamente» porque el contenido no existía (`P-008`): MVP-106 arregló el enlace roto, no
          la falta de contenido. Ahora llevan a páginas reales, que es lo que HU-1 pide: poder leer a
          qué te comprometes **antes** de entrar. */}
      <footer className="mt-8 text-center text-xs text-[#76786b] space-x-4">
        <Link to="/legal/privacidad" className="hover:underline hover:text-[#33450d]">
          Política de Privacidad
        </Link>
        <span aria-hidden="true">•</span>
        <Link to="/legal/terminos" className="hover:underline hover:text-[#33450d]">
          Términos del Servicio
        </Link>
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
