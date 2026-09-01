import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ContentLandingPage } from './ContentLandingPage';
import { getLandingBySlug, LANDING_CONTENTS } from '../../content/landings';
import { GOOGLE_ACCOUNT_SIGNUP_URL } from '../../lib/google-account';

/**
 * MKT-102 (CA-1, CA-2) — Se renderiza **sin** `MemoryRouter` a propósito: `ContentLandingPage` no
 * usa `react-router` (ver comentario del componente), y montarlo sin Router es justo lo que prueba
 * que el pre-renderizado del build (que tampoco tiene Router disponible) puede hacer lo mismo.
 */
function renderLanding(slug: string) {
  const content = getLandingBySlug(slug);
  if (!content) throw new Error(`No existe contenido de landing para "${slug}"`);
  render(<ContentLandingPage content={content} />);
  return content;
}

describe('ContentLandingPage', () => {
  it('publica un unico h1 alineado con cada landing', () => {
    for (const content of LANDING_CONTENTS) {
      const { container } = render(<ContentLandingPage content={content} />);

      expect(container.querySelectorAll('h1')).toHaveLength(1);
      expect(container.querySelector('h1')).toHaveTextContent(content.h1);
    }
  });

  it('muestra el H1 y la intro del contenido recibido', () => {
    const content = renderLanding('gestion-terrenos');

    expect(screen.getByRole('heading', { level: 1, name: content.h1 })).toBeInTheDocument();
    expect(screen.getByText(content.intro)).toBeInTheDocument();
  });

  it('muestra las FAQ que usa el dato estructurado de la landing', () => {
    const content = renderLanding('gestion-terrenos');

    expect(screen.getByRole('heading', { level: 2, name: /preguntas frecuentes/i })).toBeInTheDocument();
    for (const faq of content.faqs) {
      expect(screen.getByText(faq.question)).toBeInTheDocument();
      expect(screen.getByText(faq.answer)).toBeInTheDocument();
    }
  });

  it('el CTA principal y el del pie enlazan a /login', () => {
    renderLanding('diario-de-campo');

    const ctas = screen.getAllByRole('link', { name: /acceder a la plataforma|^acceder$/i });
    expect(ctas.length).toBeGreaterThan(0);
    for (const cta of ctas) {
      expect(cta).toHaveAttribute('href', '/login');
    }
  });

  it('dice con qué cuenta se entra, igual que la home (RN-036)', () => {
    renderLanding('control-cosechas');

    expect(screen.getByText(/se entra con una cuenta de google/i)).toBeInTheDocument();
    const alta = screen.getByRole('link', { name: /dar de alta mi dirección/i });
    expect(alta).toHaveAttribute('href', GOOGLE_ACCOUNT_SIGNUP_URL);
  });

  it('enlaza a cada landing relacionada por su ruta pública (CA-2)', () => {
    const content = renderLanding('gestion-terrenos');

    for (const relatedSlug of content.relatedSlugs) {
      const related = getLandingBySlug(relatedSlug)!;
      expect(screen.getByRole('link', { name: new RegExp(related.navLabel, 'i') })).toHaveAttribute(
        'href',
        related.path
      );
    }
  });

  it('enlaza a las páginas legales y a la home', () => {
    renderLanding('workspaces-colaboracion');

    expect(screen.getByRole('link', { name: /privacidad/i })).toHaveAttribute('href', '/legal/privacidad');
    expect(screen.getByRole('link', { name: /términos/i })).toHaveAttribute('href', '/legal/terminos');
    expect(screen.getByRole('link', { name: /^inicio$/i })).toHaveAttribute('href', '/');
  });
});
