import type { TokenResponse } from '../types/auth.types';
import { API_BASE } from './api.config';

export interface AuthCallbackParams {
  code: string;
  redirectUri: string;
  codeVerifier: string;
}

export const authService = {
  async exchangeGoogleCode(params: AuthCallbackParams): Promise<TokenResponse> {
    const response = await fetch(`${API_BASE}/api/v1/auth/google/callback`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        code: params.code,
        redirect_uri: params.redirectUri,
        code_verifier: params.codeVerifier,
      }),
    });

    if (!response.ok) {
      const errorBody = await response.json().catch(() => null);
      throw new AuthServiceError(
        errorBody?.error?.code ?? 'AUTH_UNKNOWN',
        errorBody?.error?.message ?? 'Error al iniciar sesión'
      );
    }

    return response.json();
  },

  async refreshToken(): Promise<{ access_token: string; expires_in: number }> {
    const response = await fetch(`${API_BASE}/api/v1/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
    });

    if (!response.ok) {
      throw new AuthServiceError('AUTH_REFRESH_TOKEN_INVALID', 'Sesión expirada');
    }

    return response.json();
  },

  async logout(): Promise<void> {
    await fetch(`${API_BASE}/api/v1/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    });
  },

  async getMe(accessToken: string): Promise<{ id: string; display_name: string }> {
    const response = await fetch(`${API_BASE}/api/v1/auth/me`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (!response.ok) {
      throw new AuthServiceError('AUTH_UNAUTHENTICATED', 'Token no válido');
    }

    return response.json();
  },

  buildGoogleAuthUrl(params: {
    clientId: string;
    redirectUri: string;
    codeChallenge: string;
    state: string;
  }): string {
    const url = new URL('https://accounts.google.com/o/oauth2/v2/auth');
    url.searchParams.set('response_type', 'code');
    url.searchParams.set('client_id', params.clientId);
    url.searchParams.set('redirect_uri', params.redirectUri);
    url.searchParams.set('scope', 'openid email profile');
    url.searchParams.set('code_challenge', params.codeChallenge);
    url.searchParams.set('code_challenge_method', 'S256');
    url.searchParams.set('state', params.state);
    url.searchParams.set('access_type', 'online');
    return url.toString();
  },
};

export class AuthServiceError extends Error {
  readonly code: string;

  constructor(code: string, message: string) {
    super(message);
    this.name = 'AuthServiceError';
    this.code = code;
  }
}
