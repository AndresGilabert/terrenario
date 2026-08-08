import type { HttpClient } from './http-client';
import type { FeedbackReport } from '../types/feedback.types';

/**
 * MVP-711 (`P-088`) — Canal de sugerencias e incidencias.
 *
 * Va por el cliente HTTP común y no por su cuenta, al revés que la telemetría (`MVP-602`): allí una
 * llamada con el token recién caducado no podía cerrarle la sesión a nadie porque medir es
 * accesorio; aquí, si la sesión ya no vale, lo correcto **es** que la aplicación reaccione como en
 * cualquier otra pantalla, en vez de aceptar un reporte que no va a salir.
 *
 * La respuesta es `202` sin cuerpo: el servidor confirma que el correo ha salido, no que alguien lo
 * haya leído. El producto no tiene estados de reporte y no los finge (fuera de alcance del spec).
 */
export function createFeedbackService(http: HttpClient) {
  return {
    async send(report: FeedbackReport): Promise<void> {
      await http.request<void>('/api/v1/feedback', { method: 'POST', body: report });
    },
  };
}
