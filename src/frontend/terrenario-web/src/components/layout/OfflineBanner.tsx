import React from 'react';
import { useEstadoDeConexion } from '../../lib/connectivity';

/**
 * MVP-709 (`P-091`, HU-1) — Aviso de falta de conexión.
 *
 * <b>Persistente y no descartable.</b> No es una notificación de las que se leen y se cierran: es el
 * estado en el que está la aplicación. Mientras dura, todo lo que se intente va a fallar, y poder
 * quitarlo solo serviría para volver a intentar a ciegas —que es justo lo que la historia evita—. Se
 * va solo cuando vuelve la cobertura, sin recargar (`CA-2`).
 *
 * <b>Dice también lo que no pasa.</b> La aplicación es online-first (`ADR-0002`) y no guarda nada para
 * enviarlo luego. Quien lea «sin conexión» tiene que saber que lo que ya escribió sigue en pantalla y
 * que basta con volver a guardar: sin esa frase, lo razonable es dar por perdido el trabajo y cerrar.
 *
 * `role="status"` y no `alert`: se anuncia sin interrumpir lo que se esté haciendo. Interrumpir a
 * quien está tecleando una labor para decirle que no hay cobertura sería empeorar el momento.
 */
export const OfflineBanner: React.FC = () => {
  const estado = useEstadoDeConexion();

  return (
    // El hueco existe siempre para que el aviso no empuje el contenido al aparecer: la lista que se
    // estaba leyendo no debe saltar de sitio justo cuando se pierde la cobertura.
    <div role="status" aria-live="polite" className="empty:hidden">
      {estado === 'sin-conexion' && (
        <div className="bg-[#8a5a00] text-white px-4 py-2.5 flex items-start gap-2.5 text-sm">
          <span className="material-symbols-outlined text-lg shrink-0" aria-hidden="true">
            cloud_off
          </span>
          <p className="leading-snug">
            <strong className="font-semibold">Sin conexión.</strong>{' '}
            No se pueden cargar ni guardar datos hasta que vuelva la cobertura. Lo que hayas escrito
            sigue en pantalla: cuando vuelva, pulsa guardar otra vez.
          </p>
        </div>
      )}
    </div>
  );
};
