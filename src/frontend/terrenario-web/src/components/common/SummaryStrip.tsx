import React from 'react';

/**
 * MVP-702 (`P-090`) — Tira de tarjetas de resumen.
 *
 * En escritorio es la rejilla de siempre. En **móvil** deja de apilarse y pasa a una fila desplazable
 * en horizontal: verificado a 375x812 sobre Cosechas, tres tarjetas apiladas a ancho completo más los
 * filtros sumaban ~780 px antes de la primera fila de datos, así que la pantalla entera se ocupaba con
 * contexto. En una fila, el mismo resumen ocupa la altura de **una** tarjeta.
 *
 * Desplazable y no plegable a propósito: el resumen es lo que se mira de un vistazo, y esconderlo
 * detrás de una pulsación lo convertiría en algo que nadie abre. Los filtros sí se pliegan
 * (`FilterDisclosure`), porque a esos se va cuando ya se sabe qué se quiere acotar.
 *
 * <b>Un solo árbol, no dos.</b> Los hijos se renderizan **una vez** y lo único que cambia entre móvil
 * y escritorio son las clases del contenedor. Pintar dos veces —una versión móvil y otra de
 * escritorio— habría duplicado los nodos en el DOM y, con ellos, cualquier `id` de los hijos: dos
 * elementos con el mismo `id` rompen la relación `label`/campo y lo que anuncia un lector de pantalla.
 * Las variantes `[&>*]` aplican al hijo directo sin tener que clonarlo.
 */
export const SummaryStrip: React.FC<{
  children: React.ReactNode;
  /** Clases de la rejilla en escritorio; cada vista tiene su número de columnas. */
  desktopClassName: string;
}> = ({ children, desktopClassName }) => (
  <div
    className={
      // Móvil: fila desplazable con parada en cada tarjeta (`snap`), sangrada hasta el borde de la
      // pantalla para que la última no quede cortada por el padding del contenedor.
      'flex gap-3 overflow-x-auto snap-x snap-mandatory -mx-4 px-4 pb-1 ' +
      '[&>*]:shrink-0 [&>*]:snap-start [&>*]:w-[70vw] [&>*]:max-w-60 ' +
      // Escritorio: rejilla normal; se deshacen el desplazamiento, la sangría y el ancho fijo.
      `sm:grid sm:overflow-visible sm:mx-0 sm:px-0 sm:pb-0 sm:[&>*]:w-auto sm:[&>*]:max-w-none ${desktopClassName}`
    }
  >
    {children}
  </div>
);
