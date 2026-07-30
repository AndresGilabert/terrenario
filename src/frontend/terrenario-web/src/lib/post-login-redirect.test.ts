import { describe, expect, it } from 'vitest';
import { consumePostLoginRedirect, rememberPostLoginRedirect } from './post-login-redirect';

/**
 * MVP-501 — El destino post-login (MVP-103) decide dónde acaba una persona que abre un enlace de
 * invitación sin sesión. Es además el único punto del cliente que filtra un destino de navegación,
 * así que su guarda de rutas externas se cubre explícitamente.
 */
describe('post-login-redirect', () => {
  it('Deberia_DevolverElDestino_Cuando_SeGuardoUnaRutaInterna', () => {
    rememberPostLoginRedirect('/invitations/abc123');

    expect(consumePostLoginRedirect()).toBe('/invitations/abc123');
  });

  it('Deberia_DescartarElDestino_Cuando_YaSeConsumioUnaVez', () => {
    rememberPostLoginRedirect('/invitations/abc123');
    consumePostLoginRedirect();

    // Un destino de un solo uso: si se reutilizara, el siguiente login volvería a desviar.
    expect(consumePostLoginRedirect()).toBeNull();
  });

  it('Deberia_DevolverNull_Cuando_NoHayDestinoGuardado', () => {
    expect(consumePostLoginRedirect()).toBeNull();
  });

  it.each([
    ['https://evil.example/phishing', 'destino absoluto'],
    ['//evil.example/phishing', 'destino protocol-relative'],
    ['app/diario', 'ruta sin barra inicial'],
  ])('Deberia_RechazarElDestino_Cuando_NoEsUnaRutaInterna (%s)', (path) => {
    rememberPostLoginRedirect(path);

    expect(consumePostLoginRedirect()).toBeNull();
  });
});
