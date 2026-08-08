import { beforeEach, describe, expect, it } from 'vitest';
import {
  getReportContext,
  recordFailedRequest,
  recordVisitedPath,
  resetReportContext,
} from './report-context';

/**
 * MVP-711 (HU-2) — El contexto que acompaña al reporte. Lo que se protege aquí son las dos reglas
 * que lo hacen útil: que la ruta sea **desde dónde se reporta** y no el propio formulario, y que la
 * correlación del último fallo no se pierda por una respuesta posterior sin cabecera.
 */
describe('report-context', () => {
  beforeEach(() => resetReportContext());

  it('Deberia_RecordarLaUltimaPantalla_Cuando_SeNavegaPorElArea', () => {
    recordVisitedPath('/app/diario');
    recordVisitedPath('/app/cosechas');

    expect(getReportContext().path).toBe('/app/cosechas');
  });

  it('Deberia_IgnorarLaPropiaPantallaDelCanal', () => {
    recordVisitedPath('/app/cosechas');
    recordVisitedPath('/app/feedback');

    // Si el canal se registrara, todo reporte diría «estaba en el formulario de sugerencias», que es
    // justo lo único que no aporta nada a quien lo lee.
    expect(getReportContext().path).toBe('/app/cosechas');
  });

  it('Deberia_NoInventarseUnaPantalla_Cuando_ElCanalEsLoPrimeroQueSeAbre', () => {
    expect(getReportContext().path).toBeNull();
  });

  it('Deberia_QuedarseConElUltimoFallo_YNoBorrarloConUnValorVacio', () => {
    recordFailedRequest('primero');
    recordFailedRequest('segundo');
    recordFailedRequest(null);
    recordFailedRequest(undefined);

    expect(getReportContext().lastFailedRequestId).toBe('segundo');
  });
});
