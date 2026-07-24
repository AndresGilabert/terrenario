const POST_LOGIN_REDIRECT_KEY = 'terrenario_post_login_redirect';

/**
 * Los enlaces de invitación (MVP-103) suelen abrirse sin sesión iniciada. Guardar el destino
 * antes de mandar al login evita que la persona invitada acabe en `/app` y pierda la invitación.
 */
export function rememberPostLoginRedirect(path: string): void {
  sessionStorage.setItem(POST_LOGIN_REDIRECT_KEY, path);
}

/** Devuelve el destino pendiente (si hay) y lo descarta para que no se reutilice. */
export function consumePostLoginRedirect(): string | null {
  const path = sessionStorage.getItem(POST_LOGIN_REDIRECT_KEY);
  sessionStorage.removeItem(POST_LOGIN_REDIRECT_KEY);

  // Solo rutas internas: nunca un destino absoluto venido de fuera.
  return path?.startsWith('/') && !path.startsWith('//') ? path : null;
}
