import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import { PrivacyPanel } from './PrivacyPanel';

/**
 * MVP-599 (`R-02`/`R-03`) — Este panel es lo que la Política de Privacidad llama «el inventario
 * completo», así que lo que se prueba no es que renderice: es que **siga coincidiendo con el
 * inventario de `docs/07-seguridad/privacidad-datos.md`**.
 *
 * Existe porque se desfasó justo por no tenerlo. `MVP-503` corrigió el inventario de la KB y
 * `MVP-504` el de la Política, y este se quedó atrás con cuatro de las siete tecnologías y con una
 * afirmación que ya se había declarado inexacta. Sin un test que lo ancle, volvería a pasar.
 */
describe('PrivacyPanel', () => {
  const renderPanel = () => render(<MemoryRouter><PrivacyPanel /></MemoryRouter>);

  it('lista las siete tecnologías del inventario', () => {
    renderPanel();

    // Una por cada fila del inventario de la KB. Si entra una nueva y no se declara aquí, este test
    // no la caza —no puede—, pero sí impide que desaparezca alguna de las declaradas.
    expect(screen.getByText(/refresh_token/)).toBeInTheDocument();
    expect(screen.getByText(/Token de acceso/)).toBeInTheDocument();
    expect(screen.getByText(/PKCE y anti-CSRF/)).toBeInTheDocument();
    expect(screen.getByText(/Destino pendiente tras el acceso/)).toBeInTheDocument();
    expect(screen.getByText(/Avisos ya vistos/)).toBeInTheDocument();
    expect(screen.getByText(/Medición del acceso/)).toBeInTheDocument();
    expect(screen.getByText(/Inicio de sesión con Google/)).toBeInTheDocument();
  });

  it('no afirma que no haya analítica, porque la medición del acceso es propia', () => {
    renderPanel();

    // La afirmación absoluta era la que contradecía a la Política tras la corrección de `MVP-504`.
    // Lo defendible es acotarla a terceros, no negar la medición.
    expect(screen.queryByText(/No usamos analítica/)).not.toBeInTheDocument();
    expect(screen.getByText(/No hay analítica de terceros/)).toBeInTheDocument();
  });

  it('mantiene el motivo por el que no hay banner: nada requiere consentimiento', () => {
    renderPanel();

    // RN-042: mientras todo esté exento, informar es lo correcto y el banner sería peor cumplimiento.
    expect(screen.getByText(/no hay nada que aceptar ni que rechazar/)).toBeInTheDocument();
    // Ocho desde MVP-602, que añade la medición del uso de la aplicación al inventario.
    expect(screen.getAllByText(/NECESARIA/).length).toBe(8);
  });
});
