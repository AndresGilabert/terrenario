import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { LandingPage } from './LandingPage';
import { GOOGLE_ACCOUNT_SIGNUP_URL } from '../../lib/google-account';
import { LANDING_CONTENTS } from '../../content/landings';

/**
 * MVP-712 — La landing es **pública** y es donde se decide si probar el producto. Hasta ahora no
 * decía con qué se entra, así que quien no tiene Gmail se enteraba en el login… o no llegaba nunca
 * (`P-089`). Es la única pantalla del producto que se lee sin haber entrado, y por eso el texto de
 * acceso tiene que estar aquí y no solo detrás del botón.
 *
 * MKT-102 — Se renderiza **sin** `MemoryRouter`: `LandingPage` ya no usa `react-router` (ver
 * comentario del componente), condición para poder pre-renderizarla como HTML estático.
 */
function renderLanding() {
  render(<LandingPage />);
}

describe('LandingPage — acceso con cualquier dirección', () => {
  it('publica un unico h1 para la intención principal de la home', () => {
    const { container } = render(<LandingPage />);

    expect(container.querySelectorAll('h1')).toHaveLength(1);
    expect(screen.getByRole('heading', { level: 1, name: /tu tierra, bajo control/i })).toBeInTheDocument();
  });

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

  it('agrupa las guías en el bloque de ayuda y manuales', () => {
    renderLanding();

    expect(screen.getByRole('heading', { level: 2, name: 'Ayuda y manuales' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Landings públicas' })).toBeInTheDocument();
    expect(
      screen.getByRole('link', { name: 'Cómo empezar en Terrenario' })
    ).toHaveAttribute('href', '/guias/como-empezar-en-terrenario');
  });

  it('alinea las pastillas en tres columnas y no muestra el ancla de Beneficios en la cabecera', () => {
    renderLanding();

    const clasesPastilla = new Set<string>();
    const bloque = screen.getByRole('region', { name: 'Landings públicas' });
    expect(bloque.querySelectorAll('ul')).toHaveLength(3);
    for (const lista of bloque.querySelectorAll('ul')) {
      expect(lista).toHaveClass('sm:grid-cols-2', 'lg:grid-cols-3');
      for (const enlace of lista.querySelectorAll('a')) {
        clasesPastilla.add(enlace.className);
      }
    }

    expect(clasesPastilla.size).toBe(1);
    expect(screen.queryByRole('link', { name: 'Beneficios' })).not.toBeInTheDocument();
  });

  it('navega desde la cabecera a los tres grupos de landings', () => {
    renderLanding();

    const enlaces = [
      ['Funcionalidades', '#funcionalidades'],
      ['Para quién es Terrenario', '#para-quien-es-terrenario'],
      ['Ayuda', '#ayuda'],
    ] as const;

    for (const [nombre, destino] of enlaces) {
      expect(screen.getByRole('link', { name: nombre })).toHaveAttribute('href', destino);
      expect(document.querySelector(destino)).toBeInTheDocument();
    }
  });
});
