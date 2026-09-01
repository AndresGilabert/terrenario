import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import { LandingPage } from './LandingPage';
import { GOOGLE_ACCOUNT_SIGNUP_URL } from '../../lib/google-account';
import { LANDING_CONTENTS } from '../../content/landings';

/**
 * MVP-712 — La landing es **pública** y es donde se decide si probar el producto. Hasta ahora no
 * decía con qué se entra, así que quien no tiene Gmail se enteraba en el login… o no llegaba nunca
 * (`P-089`). Es la única pantalla del producto que se lee sin haber entrado, y por eso el texto de
 * acceso tiene que estar aquí y no solo detrás del botón.
 */
function renderLanding() {
  render(
    <MemoryRouter>
      <LandingPage />
    </MemoryRouter>
  );
}

describe('LandingPage — acceso con cualquier dirección', () => {
  it('dice con qué se entra antes de pedir nada', () => {
    renderLanding();

    expect(screen.getByText(/se entra con una cuenta de google/i)).toBeInTheDocument();
  });

  it('dice lo mismo que el login: no hace falta un Gmail, pero sí dar de alta la dirección', () => {
    renderLanding();

    const aviso = screen.getByText(/no hace falta que tu correo sea de gmail/i);

    expect(aviso).toHaveTextContent(/hotmail, outlook o el de tu cooperativa/i);
    expect(aviso).toHaveTextContent(/des de alta esa misma dirección como cuenta de google/i);
  });

  it('enlaza el alta sin cargar nada de un tercero', () => {
    renderLanding();

    const alta = screen.getByRole('link', { name: /dar de alta mi dirección/i });

    // La CSP de la landing es `default-src 'self'` (`RN-042`): un enlace lo sigue la persona, un
    // recurso lo pediría el navegador solo. Aquí solo cabe lo primero.
    expect(alta).toHaveAttribute('href', GOOGLE_ACCOUNT_SIGNUP_URL);
    expect(alta).toHaveAttribute('target', '_blank');
    expect(alta).toHaveAttribute('rel', expect.stringContaining('noreferrer'));
  });
});

describe('LandingPage — hub de enlazado a las landings públicas (MKT-102, CA-3)', () => {
  it('enlaza a cada landing de funcionalidad y de caso de uso por su ruta pública', () => {
    renderLanding();

    for (const content of LANDING_CONTENTS) {
      expect(screen.getByRole('link', { name: content.navLabel })).toHaveAttribute('href', content.path);
    }
  });
});
