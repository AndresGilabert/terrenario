import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it, vi } from 'vitest';
import { OAuthCallback } from './OAuthCallback';

vi.mock('../../contexts/AuthContext', () => ({ useAuth: () => ({ login: vi.fn() }) }));

/**
 * MVP-713 (`P-079`, CA-1) — Lo que la pantalla de vuelta de Google le cuenta al usuario.
 *
 * El caso que motiva la historia se ve **aquí**: quien recarga esta pantalla reenvía un código de un
 * solo uso ya gastado. Que el servidor pase a responder 401 en vez de 500 arregla las alertas; que la
 * persona sepa qué hacer depende del mensaje.
 *
 * Se sustituye `fetch` y no el servicio: así el recorrido incluye la extracción del código de error
 * del cuerpo, que es la pieza que une el contrato de la API con el mensaje de la pantalla.
 */
describe('OAuthCallback — mensajes de error', () => {
  /** Respuesta de error de la API, con la forma de `contratos-api.md`. */
  function apiResponde(status: number, code: string, message: string) {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => ({
        ok: false,
        status,
        json: async () => ({ error: { code, message } }),
      }))
    );
  }

  /** Cada caso usa un `code` distinto: la guarda de idempotencia de MVP-106 es de módulo. */
  function renderCallback(code: string) {
    sessionStorage.setItem('oauth_state', 'estado');
    sessionStorage.setItem('pkce_code_verifier', 'verificador');

    render(
      <MemoryRouter initialEntries={[`/auth/callback?code=${code}&state=estado`]}>
        <OAuthCallback />
      </MemoryRouter>
    );
  }

  it('explica que hay que volver a entrar cuando el código ya se usó o caducó', async () => {
    apiResponde(401, 'AUTH_GOOGLE_CODE_INVALID', 'El código de acceso ha caducado.');

    renderCallback('codigo-caducado');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      /caducado o esta página ya se había usado/i
    );
  });

  it('mantiene el mensaje genérico cuando el fallo es del servidor', async () => {
    // `AUTH_GOOGLE_EXCHANGE_FAILED` queda reservado a lo que de verdad es fallo nuestro: ahí el
    // usuario no puede hacer nada distinto de reintentar, y decirle otra cosa sería engañarle.
    apiResponde(500, 'AUTH_GOOGLE_EXCHANGE_FAILED', 'Error al completar el acceso.');

    renderCallback('codigo-con-google-caido');

    expect(await screen.findByRole('alert')).toHaveTextContent(/Error al completar el acceso/i);
  });

  it('no deja sin explicación un código que no conoce', async () => {
    apiResponde(400, 'AUTH_LO_QUE_SEA', 'desconocido');

    renderCallback('codigo-desconocido');

    expect(await screen.findByRole('alert')).toHaveTextContent(/inténtalo de nuevo/i);
  });
});
