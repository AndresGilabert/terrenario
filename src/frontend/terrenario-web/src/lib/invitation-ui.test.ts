import { afterEach, describe, expect, it, vi } from 'vitest';
import { expiresLabel, viewerReasonMessage } from './invitation-ui';
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
  });

  describe('expiresLabel', () => {
    /** Instante fijo: la etiqueta se calcula contra «ahora» y sin congelarlo el test es aleatorio. */
    const now = new Date('2026-07-30T10:00:00.000Z');

    const labelAt = (expiresAt: string): string => {
      vi.useFakeTimers();
      vi.setSystemTime(now);
      try {
        return expiresLabel(expiresAt);
      } finally {
        vi.useRealTimers();
      }
    };

    it('Deberia_DecirQueCaducaHoy_Cuando_LaFechaYaPaso', () => {
      expect(labelAt('2026-07-29T10:00:00.000Z')).toBe('Caduca hoy');
    });

    // Comportamiento **actual**, no el deseable: `Math.ceil` sobre la fracción de día hace que
    // cualquier invitación con tiempo restante —aunque venza esta misma tarde— rotule «Caduca
    // mañana», y que «Caduca hoy» solo salga cuando ya ha caducado. Registrado como hallazgo de
    // MVP-501; la corrección no es de esta historia (que es cobertura, no arreglos de UX).
    it('Deberia_DecirQueCaducaManana_Cuando_FaltanHorasDelMismoDia (comportamiento actual)', () => {
      expect(labelAt('2026-07-30T18:00:00.000Z')).toBe('Caduca mañana');
    });

    it('Deberia_DecirQueCaducaManana_Cuando_FaltaUnDiaExacto', () => {
      expect(labelAt('2026-07-31T10:00:00.000Z')).toBe('Caduca mañana');
    });

    it('Deberia_DecirLosDiasQueFaltan_Cuando_QuedaMasDeUnDia', () => {
      expect(labelAt('2026-08-06T10:00:00.000Z')).toBe('Caduca en 7 días');
    });
  });
});
