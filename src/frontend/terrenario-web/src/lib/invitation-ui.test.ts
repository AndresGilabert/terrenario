import { afterEach, describe, expect, it, vi } from 'vitest';
import { expiresLabel, shouldOfferGoogleSignup, viewerReasonMessage } from './invitation-ui';
import type { InvitationViewerReason } from '../types/invitation.types';

/**
 * MVP-501 — Mensajes de aptitud del preview de invitación (MVP-107, R-C) y etiqueta de caducidad.
 * Son la parte de la bandeja que decide qué lee el usuario antes de aceptar, y hasta ahora estaba
 * cubierta solo por tipado (`MVP-999`, `P-012`).
 */
describe('invitation-ui', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  describe('viewerReasonMessage', () => {
    it('Deberia_DevolverNull_Cuando_NoHayMotivoDeInaptitud', () => {
      expect(viewerReasonMessage(null)).toBeNull();
    });

    it.each<InvitationViewerReason>([
      'email_mismatch',
      'expired',
      'already_used',
      'already_rejected',
      'cancelled',
      'already_member',
    ])('Deberia_DevolverUnMensajeAccionable_Cuando_ElMotivoEs_%s', (reason) => {
      const message = viewerReasonMessage(reason);

      expect(message).toBeTruthy();
      // Ningún motivo puede quedarse sin texto: el usuario vería un hueco donde debería haber la
      // explicación de por qué no puede aceptar.
      expect(message).not.toBe('');
    });

    it('Deberia_ExplicarQueEsOtraCuenta_Cuando_ElMotivoEsEmailMismatch', () => {
      expect(viewerReasonMessage('email_mismatch')).toContain('otra cuenta de correo');
    });

    it('Deberia_ExplicarLaSalida_Cuando_LaDireccionInvitadaPuedeNoSerCuentaDeGoogle', () => {
      // MVP-712 (CA-3) — El aviso decía solo el problema. Quien fue invitado en una dirección sin
      // Cuenta de Google leía «entra con esa cuenta» y no tenía ninguna con la que entrar: callejón
      // sin salida (`P-089`, caso (b)).
      const message = viewerReasonMessage('email_mismatch');

      expect(message).toContain('dada de alta como Cuenta de Google');
      expect(message).toContain('vuelve a abrir este enlace');
    });
  });

  describe('shouldOfferGoogleSignup', () => {
    it('Deberia_OfrecerElAlta_Cuando_ElMotivoEsEmailMismatch', () => {
      // No se puede saber si la dirección invitada tiene Cuenta de Google —el preview no la revela—,
      // así que el enlace acompaña siempre a este motivo: sobra para quien se equivocó de cuenta y es
      // la única salida para quien no tiene ninguna.
      expect(shouldOfferGoogleSignup('email_mismatch')).toBe(true);
    });

    it.each<InvitationViewerReason>([
      'expired',
      'already_used',
      'already_rejected',
      'cancelled',
      'already_member',
    ])('Deberia_NoOfrecerElAlta_Cuando_ElMotivoEs_%s', (reason) => {
      // Darse de alta en Google no arregla una invitación caducada, anulada o ya usada: ofrecerlo
      // solo distraería de lo que sí toca hacer.
      expect(shouldOfferGoogleSignup(reason)).toBe(false);
    });

    it('Deberia_NoOfrecerElAlta_Cuando_NoHayMotivoDeInaptitud', () => {
      expect(shouldOfferGoogleSignup(null)).toBe(false);
    });
  });

  describe('expiresLabel', () => {
    /**
     * Instante fijo: la etiqueta se calcula contra «ahora» y sin congelarlo el test es aleatorio.
     * Mediodía local para que el día de calendario sea el mismo en cualquier huso donde corra el CI.
     */
    const now = new Date(2026, 6, 30, 12, 0, 0);

    const labelAt = (expiresAt: string): string => {
      vi.useFakeTimers();
      vi.setSystemTime(now);
      try {
        return expiresLabel(expiresAt);
      } finally {
        vi.useRealTimers();
      }
    };

    /** Fecha local, para que el día de calendario no dependa del huso donde corra el test. */
    const local = (year: number, month: number, day: number, hour: number) =>
      new Date(year, month - 1, day, hour).toISOString();

    it('Deberia_DecirQueYaCaduco_Cuando_LaFechaYaPaso', () => {
      // Antes decía «Caduca hoy», que además de confuso era falso: ya había caducado (`P-065`).
      expect(labelAt(local(2026, 7, 29, 10))).toBe('Caducada');
    });

    it('Deberia_DecirQueCaducaHoy_Cuando_VenceMasTardeElMismoDia', () => {
      // El caso que destapó `P-065`: quedan horas, pero es hoy, no mañana.
      expect(labelAt(local(2026, 7, 30, 18))).toBe('Caduca hoy');
    });

    it('Deberia_DecirQueCaducaManana_Cuando_VenceElDiaSiguiente', () => {
      // Vence de madrugada: faltan menos de 24 horas, pero en el calendario es mañana.
      expect(labelAt(local(2026, 7, 31, 2))).toBe('Caduca mañana');
    });

    it('Deberia_DecirQueCaducaManana_Cuando_FaltaUnDiaExacto', () => {
      expect(labelAt(local(2026, 7, 31, 12))).toBe('Caduca mañana');
    });

    it('Deberia_DecirLosDiasQueFaltan_Cuando_QuedaMasDeUnDia', () => {
      expect(labelAt(local(2026, 8, 6, 10))).toBe('Caduca en 7 días');
    });

    it('Deberia_ContarPorDiasDeCalendario_Cuando_ElVencimientoEsPorLaNoche', () => {
      // 7 días y 10 horas por reloj: contar fracciones daba «8 días», pero en el calendario son 7.
      expect(labelAt(local(2026, 8, 6, 22))).toBe('Caduca en 7 días');
    });
  });
});
