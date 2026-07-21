export interface AuthUser {
  id: string;
  displayName: string;
}

export interface TokenResponse {
  access_token: string;
  expires_in: number;
  user: {
    id: string;
    display_name: string;
  };
}

export interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

export type AuthAction =
  | { type: 'LOGIN_SUCCESS'; payload: { user: AuthUser; accessToken: string } }
  | { type: 'LOGOUT' }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'REFRESH_SUCCESS'; payload: { accessToken: string } };
