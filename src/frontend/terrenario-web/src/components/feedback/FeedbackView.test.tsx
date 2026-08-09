import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { FeedbackView } from './FeedbackView';
import { createFakeHttpClient, type FakeHttpClient } from '../../test/http';
import {
  recordFailedRequest,
  recordVisitedPath,
  resetReportContext,
} from '../../lib/report-context';

let http: FakeHttpClient;
vi.mock('../../contexts/ApiContext', () => ({ useApiClient: () => http }));

/**
 * MVP-711 — Lo que se prueba no es que el botón llame a la API, sino que lo que sale lleve el
 * contexto que hace útil el reporte (HU-2) y que un fallo se cuente en vez de confirmarse (CA-3).
 */
describe('FeedbackView', () => {
  const renderWith = (routes: Record<string, unknown> = { '/api/v1/feedback': undefined }) => {
    http = createFakeHttpClient(routes);
    return render(<FeedbackView />);
  };

  const bodyOf = () => http.callsTo('/api/v1/feedback')[0].options.body as Record<string, unknown>;

  beforeEach(() => {
    vi.clearAllMocks();
    resetReportContext();
  });

  it('Deberia_EnviarLaIncidencia_ConLaPantallaYElUltimoErrorDeLaSesion', async () => {
    const user = userEvent.setup();
    recordVisitedPath('/app/diario');
    recordFailedRequest('a1b2c3d4e5f6');
    renderWith();

    await user.type(screen.getByLabelText(/cuéntanoslo/i), 'No puedo guardar una labor.');
    await user.click(screen.getByRole('button', { name: /enviar/i }));

    expect(bodyOf()).toEqual({
      kind: 'incidencia',
      message: 'No puedo guardar una labor.',
      path: '/app/diario',
      last_failed_request_id: 'a1b2c3d4e5f6',
    });
  });

  it('Deberia_EnviarSugerencia_Cuando_SeCambiaElTipo', async () => {
    const user = userEvent.setup();
    renderWith();

    await user.click(screen.getByRole('radio', { name: /se me ocurre algo/i }));
    await user.type(screen.getByLabelText(/cuéntanoslo/i), 'Estaría bien un buscador.');
    await user.click(screen.getByRole('button', { name: /enviar/i }));

    expect(bodyOf().kind).toBe('sugerencia');
  });

  it('Deberia_ConfirmarEnPantalla_Cuando_ElReporteSale', async () => {
    const user = userEvent.setup();
    renderWith();

    await user.type(screen.getByLabelText(/cuéntanoslo/i), 'Algo que contar.');
    await user.click(screen.getByRole('button', { name: /enviar/i }));

    // CA-3 — sin confirmación, la duda razonable es «¿se habrá enviado?» y se manda otra vez.
    expect(await screen.findByRole('status')).toHaveTextContent(/enviado/i);
    // Y el formulario se vacía, para que reenviar lo mismo cueste tanto como escribirlo.
    expect(screen.getByLabelText(/cuéntanoslo/i)).toHaveValue('');
  });

  it('Deberia_MostrarElMensajeDeLaApi_Cuando_SeAgotaElLimite', async () => {
    const user = userEvent.setup();
    const { HttpError } = await import('../../services/http-client');
    renderWith({
      '/api/v1/feedback': () => {
        throw new HttpError(429, 'RATE_LIMIT_FEEDBACK', 'Has enviado 3 mensajes en la última hora.');
      },
    });

    await user.type(screen.getByLabelText(/cuéntanoslo/i), 'Otra vez lo mismo.');
    await user.click(screen.getByRole('button', { name: /enviar/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/3 mensajes en la última hora/i);
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('Deberia_ImpedirElEnvio_Cuando_NoHayTexto', async () => {
    const user = userEvent.setup();
    renderWith();

    expect(screen.getByRole('button', { name: /enviar/i })).toBeDisabled();

    // Solo espacios tampoco cuenta: el servidor lo rechazaría y sería un viaje para nada.
    await user.type(screen.getByLabelText(/cuéntanoslo/i), '   ');
    expect(screen.getByRole('button', { name: /enviar/i })).toBeDisabled();
  });

  it('Deberia_DecirQueSeEnvia_Antes_DeEnviarlo', async () => {
    renderWith();

    // Transparencia (RGPD art. 13): que el reporte lleve el correo de la cuenta y el navegador se
    // dice en la propia pantalla, no en un documento aparte.
    expect(screen.getByText(/correo de tu cuenta/i)).toBeInTheDocument();
    expect(screen.getByText(/nada de tu explotación/i)).toBeInTheDocument();
  });
});
