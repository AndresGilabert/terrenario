import { useEffect, useState } from 'react';

/**
 * MVP-702 — Suscripción a una media query, para los casos en los que la **estructura** cambia entre
 * móvil y escritorio y no basta con clases.
 *
 * Cuando lo único que cambia es la presentación se usa CSS: duplicar el árbol para pintarlo distinto
 * duplicaría también los `id` de los controles, y dos elementos con el mismo `id` rompen la relación
 * `label`/campo —y con ella el foco al pulsar la etiqueta y lo que anuncia un lector de pantalla—.
 * Este hook existe para elegir **un** árbol, no para pintar dos.
 */
export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() =>
    typeof window === 'undefined' ? false : window.matchMedia(query).matches
  );

  useEffect(() => {
    const list = window.matchMedia(query);
    const update = () => setMatches(list.matches);
    update();
    list.addEventListener('change', update);
    return () => list.removeEventListener('change', update);
  }, [query]);

  return matches;
}

/** Corte de `sm:` de Tailwind. Es el mismo que separa el móvil del resto en todo el producto. */
export const useIsDesktop = () => useMediaQuery('(min-width: 640px)');

/**
 * MVP-803 — Corte de `lg:` de Tailwind, que desde esta historia es **donde aparece la barra lateral**.
 * Por debajo de él el contenido tiene el ancho entero de la pantalla y las listas de ocho columnas se
 * leen como tarjetas; por encima, caben como tabla.
 *
 * Es un corte distinto de `useIsDesktop` a propósito: aquel separa «móvil» de «lo demás» para plegar
 * controles, y este separa «cabe una tabla ancha» de «no cabe». A 768 px las dos respuestas son
 * distintas y confundirlas es justo lo que dejaba la tabla de Cosechas en 448 px de sitio.
 */
export const useIsWide = () => useMediaQuery('(min-width: 1024px)');
