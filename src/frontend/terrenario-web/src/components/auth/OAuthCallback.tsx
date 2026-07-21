import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authService, AuthServiceError } from '../../services/auth.service';
import { useAuth } from '../../contexts/AuthContext';

const REDIRECT_URI = `${window.location.origin}/auth/callback`;

const ERROR_MESSAGES: Record<string, string> = {
  access_denied: 'Acceso cancelado. Puedes intentarlo de nuevo cuando quieras.',
  AUTH_GOOGLE_TOKEN_INVALID: 'No se pudo verificar tu identidad con Google. Inténtalo de nuevo.',
  AUTH_GOOGLE_EXCHANGE_FAILED: 'Error al completar el acceso. Por favor, inténtalo de nuevo.',
};

export const OAuthCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { login } = useAuth();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    const error = searchParams.get('error');
    if (error) {
      const message = ERROR_MESSAGES[error] ?? 'Error al iniciar sesión.';
      setErrorMessage(message);
      return;
    }

    const code = searchParams.get('code');
    const returnedState = searchParams.get('state');
    const storedState = sessionStorage.getItem('oauth_state');
    const codeVerifier = sessionStorage.getItem('pkce_code_verifier');

    if (!code || !returnedState || !storedState || !codeVerifier) {
      setErrorMessage('Parámetros de autenticación inválidos. Vuelve a intentarlo.');
      return;
    }

    if (returnedState !== storedState) {
      setErrorMessage('Error de seguridad en el proceso de autenticación.');
      return;
    }

    sessionStorage.removeItem('oauth_state');
    sessionStorage.removeItem('pkce_code_verifier');

    authService
      .exchangeGoogleCode({ code, redirectUri: REDIRECT_URI, codeVerifier })
      .then((tokenResponse) => {
        login(tokenResponse.access_token, {
          id: tokenResponse.user.id,
          displayName: tokenResponse.user.display_name,
        });
        navigate('/app', { replace: true });
      })
      .catch((err: unknown) => {
        const errorCode =
          err instanceof AuthServiceError ? err.code : 'AUTH_UNKNOWN';
        const message =
          ERROR_MESSAGES[errorCode] ?? 'Error al completar el acceso. Inténtalo de nuevo.';
        setErrorMessage(message);
      });
  }, []);

  if (errorMessage) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-4">
        <div className="w-full max-w-md bg-white rounded-2xl p-8 border border-[#e5e2dd] shadow-xl text-center space-y-6">
          <div className="mx-auto w-16 h-16 rounded-2xl bg-red-100 flex items-center justify-center">
            <span className="text-3xl">⚠️</span>
          </div>
          <div className="space-y-2">
            <h2 className="font-bold text-xl text-[#1c1c19]">No se pudo iniciar sesión</h2>
            <p className="text-sm text-[#45483c]" role="alert">
              {errorMessage}
            </p>
          </div>
          <button
            onClick={() => navigate('/login', { replace: true })}
            className="w-full px-5 py-3 rounded-xl bg-[#33450d] text-white font-semibold text-sm hover:bg-[#4a5d23] transition-colors"
          >
            Volver a intentarlo
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
      <div className="text-center space-y-4">
        <div className="w-12 h-12 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin mx-auto" />
        <p className="text-sm text-[#45483c]">Completando el acceso…</p>
      </div>
    </div>
  );
};
