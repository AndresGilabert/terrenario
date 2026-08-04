import type { HttpClient } from './http-client';

/** Workspace del que la cuenta es única propietaria y que bloquea la baja (RN-038). */
export interface AccountClosureObligation {
  workspace_id: string;
  name: string;
  other_active_members: number;
}

export interface AccountClosureOptions {
  is_clear: boolean;
  obligations: AccountClosureObligation[];
  active_memberships: number;
  active_sessions: number;
  /** Frase que hay que escribir para confirmar. La dicta el servidor: la UI no se la inventa. */
  confirmation_phrase: string;
  retention_months: number;
}

export interface AccountClosureResult {
  revoked_sessions: number;
  revoked_memberships: number;
  cancelled_invitations: number;
  purge_after: string;
}

/**
 * MVP-505 (HU-3) — Baja de cuenta: el derecho de supresión ejercido desde la aplicación.
 *
 * No pasa por el cliente HTTP con ámbito de Workspace por casualidad: la baja es de la **cuenta**, y
 * quien no tenga ningún Workspace también tiene derecho a ejercerla.
 */
export function createAccountService(http: HttpClient) {
  return {
    /** Qué bloquea la baja y qué alcance tendrá, para que la confirmación sea informada. */
    async getClosureOptions(): Promise<AccountClosureOptions> {
      return http.request<AccountClosureOptions>('/api/v1/account/closure');
    },

    /** Ejecuta la baja. Irreversible: no hay periodo de gracia ni papelera. */
    async closeAccount(confirmation: string): Promise<AccountClosureResult> {
      return http.request<AccountClosureResult>('/api/v1/account/closure', {
        method: 'POST',
        body: { confirmation },
      });
    },
  };
}
