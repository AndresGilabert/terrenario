import React from 'react';
import { useIsDesktop } from '../../lib/use-media-query';

/**
 * MVP-702 (`P-090`) — Bloque plegado en móvil y a la vista en escritorio.
 *
 * Lo usan los dos bloques que en móvil empujaban los datos por debajo del pliegue:
 *
 * - **Los filtros.** Cinco controles a ancho completo antes de la primera entrada del Diario. Detrás
 *   de un desplegable ocupan la altura de **un** botón, y siguen a **una sola acción** (CA-5).
 * - **El alta de compra.** Es el único formulario del producto que vive en línea en vez de en un
 *   modal —Diario y Cosechas abren el suyo desde un botón—, y sus 335 px dejaban la primera fila del
 *   libro justo en el borde inferior de la pantalla. Plegado, se comporta como los otros dos.
 *
 * Se pliega esto y no el resumen porque responden a intenciones distintas: el resumen se mira al
 * entrar, y a los filtros y al alta se va cuando ya se sabe qué se quiere hacer.
 *
 * <b>Los hijos se renderizan una sola vez.</b> Aquí no basta con clases —la estructura cambia: en
 * móvil hay un `<details>` y en escritorio no—, así que se elige el envoltorio con una media query en
 * lugar de pintar los dos y ocultar uno. Pintar los dos habría metido en el DOM **dos copias de cada
 * control**, con el mismo `id` las dos, y eso rompe la relación `label`/campo: al pulsar la etiqueta
 * el foco iría al control equivocado y un lector de pantalla anunciaría el que no es.
 *
 * Se usa `<details>` nativo y no un desplegable propio: trae de serie el plegado accesible —teclado,
 * estado expuesto y anuncio en lector de pantalla— sin escribir nada de eso.
 *
 * `activeCount` es lo que evita el peor efecto de esconder filtros: no saber que están puestos. Con el
 * panel cerrado, el número dice cuántos acotan lo que se está viendo.
 */
export const MobileDisclosure: React.FC<{
  children: React.ReactNode;
  /** Rótulo del disparador en móvil. */
  label: string;
  /** Icono de Material Symbols del disparador. */
  icon: string;
  /** Cuántos filtros hay puestos a mano. `0` mientras no se ha tocado nada. */
  activeCount?: number;
}> = ({ children, label, icon, activeCount = 0 }) => {
  const isDesktop = useIsDesktop();

  if (isDesktop) return <>{children}</>;

  return (
    // Abierto de entrada si ya hay filtros puestos: si acotan lo que se ve, tienen que verse.
    <details className="group" open={activeCount > 0}>
      <summary className="list-none [&::-webkit-details-marker]:hidden cursor-pointer flex items-center justify-between gap-2 px-4 py-2.5 rounded-xl bg-white border border-[#c6c8b8] text-[#33450d] text-xs font-semibold">
        <span className="flex items-center gap-1.5">
          <span className="material-symbols-outlined text-base" aria-hidden="true">{icon}</span>
          <span>{label}</span>
          {activeCount > 0 && (
            <span className="ml-1 px-1.5 py-0.5 rounded-full bg-[#c9f16f] text-[#33450d] text-[10px] font-bold">
              {activeCount}
            </span>
          )}
        </span>
        <span
          className="material-symbols-outlined text-base transition-transform group-open:rotate-180"
          aria-hidden="true"
        >
          expand_more
        </span>
      </summary>
      <div className="mt-3">{children}</div>
    </details>
  );
};
