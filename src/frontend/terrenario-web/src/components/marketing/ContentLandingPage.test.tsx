import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ContentLandingPage } from './ContentLandingPage';
import { getLandingBySlug, LANDING_CONTENTS } from '../../content/landings';

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

  it('muestra el paso de acceso y las cinco capturas alternadas en la guía de inicio', () => {
    renderLanding('como-empezar-en-terrenario');

    expect(screen.getByRole('heading', { level: 3, name: 'Inicia sesión' })).toBeInTheDocument();
    const capturas = screen.getAllByRole('img');
    expect(capturas).toHaveLength(5);
    expect(capturas.map((imagen) => imagen.getAttribute('src'))).toEqual([
      '/landings/como-empezar-en-terrenario/01_crear_workspace_terrenario.png',
      '/landings/como-empezar-en-terrenario/02_crear_temporada_terrenario.png',
      '/landings/como-empezar-en-terrenario/03_anadir_terreno_terrenario.png',
      '/landings/como-empezar-en-terrenario/04_nuevo_terreno_terrenario.png',
      '/landings/como-empezar-en-terrenario/05_nuevo_terreno_formulario_terrenario.png',
    ]);
    expect(capturas[0]).toHaveAttribute('alt', expect.stringContaining('Workspace'));
    expect(capturas[1]).toHaveAttribute('alt', expect.stringContaining('temporada'));
    expect(capturas[2]).toHaveAttribute('alt', expect.stringContaining('preparación de la explotación'));
    expect(capturas[3]).toHaveAttribute('alt', expect.stringContaining('Estado inicial'));
    expect(capturas[4]).toHaveAttribute('alt', expect.stringContaining('Formulario de terreno'));
    const pasos = document.querySelectorAll('ol > li');
    expect(pasos).toHaveLength(7);
    expect(pasos[0].querySelector('figure')).not.toBeInTheDocument();
    expect(pasos[1].querySelector('figure')).not.toHaveClass('lg:col-start-1');
    expect(pasos[2].querySelector('figure')).toHaveClass('lg:col-start-1');
    expect(pasos[3].querySelector('figure')).not.toHaveClass('lg:col-start-1');
    expect(pasos[4].querySelector('figure')).toHaveClass('lg:col-start-1');
    expect(pasos[5].querySelector('figure')).not.toHaveClass('lg:col-start-1');
    expect(pasos[6].querySelector('figure')).not.toBeInTheDocument();
  });

  it('muestra los bloques editoriales de la landing de control de cosecha de olivar', () => {
    renderLanding('control-cosechas');

    expect(screen.getByRole('heading', { name: /qué terreno rindió mejor/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /toda la cosecha del olivar/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /decide con datos registrados/i })).toBeInTheDocument();
    expect(screen.getByText(/litros por cada 100 kg de aceituna/i)).toBeInTheDocument();
    expect(screen.getByText(/venta de aceituna, aceite para venta/i)).toBeInTheDocument();
    expect(screen.getByRole('navigation', { name: /contenido de la página/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /por qué terrenario/i })).toHaveAttribute('href', '#problema');
    expect(screen.getByRole('heading', { name: /empieza a registrar la cosecha de tu olivar/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /^acceder a terrenario$/i })).toHaveAttribute('href', '/login');
  });

  it('el CTA principal y el del pie enlazan a /login', () => {
    renderLanding('diario-de-campo');

    const ctas = screen.getAllByRole('link', { name: /acceder a la plataforma|^acceder$/i });
    expect(ctas.length).toBeGreaterThan(0);
    for (const cta of ctas) {
      expect(cta).toHaveAttribute('href', '/login');
    }
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
