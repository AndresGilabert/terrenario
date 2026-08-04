import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import { PrivacyPolicyPage } from './PrivacyPolicyPage';
import { TermsPage } from './TermsPage';
import { legalEntity } from '../../config/legal-entity';

const renderPage = (page: React.ReactNode) =>
  render(<MemoryRouter>{page}</MemoryRouter>);

/**
 * MVP-504 (B-1) — Estas páginas son la evidencia de cumplimiento de la LSSI art. 10 y del RGPD
 * art. 13. Lo que se comprueba es que **publican lo que la norma exige** y que ya no queda ni un
 * marcador: hasta esta historia no eran publicables.
 */
describe('páginas legales', () => {
  it('la Política de Privacidad identifica al responsable y da dónde ejercer derechos', () => {
    renderPage(<PrivacyPolicyPage />);

    expect(screen.getByText(/Andrés Gilabert Sánchez/)).toBeInTheDocument();
    expect(screen.getByText(new RegExp(legalEntity.taxId.replace(/\./g, '\\.')))).toBeInTheDocument();
    expect(screen.getByText(/Muro de Alcoi/)).toBeInTheDocument();
    expect(screen.getByText(/No designado/)).toBeInTheDocument();

    // El contacto tiene que ser accionable, no un texto suelto: es la vía de los arts. 15-22.
    const contactos = screen.getAllByRole('link', { name: legalEntity.privacyEmail });
    expect(contactos.length).toBeGreaterThan(0);
    expect(contactos[0]).toHaveAttribute('href', `mailto:${legalEntity.privacyEmail}`);
  });

  it('la Política de Privacidad declara los encargados y dónde se alojan los datos', () => {
    renderPage(<PrivacyPolicyPage />);

    // Aparecen en la tabla de encargados y de nuevo al explicar dónde se alojan los datos.
    expect(screen.getAllByText(/Arsys/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Microsoft Azure/).length).toBeGreaterThan(0);
    // Decir dónde se alojan los datos es lo que sostiene que ningún encargado sale de la UE.
    expect(screen.getByText(/dentro de la Unión Europea/)).toBeInTheDocument();
  });

  it('encuadra a Google como responsable independiente, no como encargado', () => {
    renderPage(<PrivacyPolicyPage />);

    // Cuando alguien entra con **su** cuenta de Google, Google trata esos datos bajo su propia
    // política y no por cuenta nuestra: no es un encargado del art. 28. Confundirlo obligaría a un
    // contrato de encargo que no procede, y describiría mal la relación real.
    expect(screen.getByText(/responsable independiente/)).toBeInTheDocument();
    expect(screen.getByText(/no por cuenta nuestra/)).toBeInTheDocument();
    // La salida del EEE se declara igualmente, con la garantía que la ampara.
    expect(screen.getByText(/cláusulas contractuales tipo/)).toBeInTheDocument();
  });

  it('los Términos identifican al prestador y no imponen fuero al consumidor', () => {
    renderPage(<TermsPage />);

    expect(screen.getByText(/Andrés Gilabert Sánchez/)).toBeInTheDocument();
    expect(screen.getByText(/legislación española/)).toBeInTheDocument();
    // Imponer un fuero distinto al domicilio del consumidor sería una cláusula abusiva.
    expect(screen.getByText(/los de tu domicilio/)).toBeInTheDocument();
  });

  it('ninguna de las dos deja marcadores ni el aviso de documento pendiente', () => {
    const { unmount } = renderPage(<PrivacyPolicyPage />);
    expect(screen.queryByText(/Documento pendiente de completar/)).not.toBeInTheDocument();
    expect(screen.queryByText(/\[[A-ZÁÉÍÓÚÑ ]{4,}\]/)).not.toBeInTheDocument();
    unmount();

    renderPage(<TermsPage />);
    expect(screen.queryByText(/Documento pendiente de completar/)).not.toBeInTheDocument();
    expect(screen.queryByText(/\[[A-ZÁÉÍÓÚÑ ]{4,}\]/)).not.toBeInTheDocument();
  });
});
