import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MobileDisclosure } from './MobileDisclosure';
import { SummaryStrip } from './SummaryStrip';

/** Declara el ancho de pantalla para el componente bajo prueba. */
function conAncho(px: number) {
  vi.stubGlobal('matchMedia', (query: string) => {
    const min = /\(min-width:\s*(\d+)px\)/.exec(query);
    return {
      matches: min ? px >= Number(min[1]) : false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    };
  });
}

const MOVIL = 375;
const ESCRITORIO = 1280;

/**
 * MVP-702 (`P-090`) — Los dos envoltorios que reparten el espacio en móvil.
 *
 * Lo que más se prueba aquí no es que se plieguen, sino que **los hijos se rendericen una sola vez**.
 * La primera versión pintaba el árbol dos veces —uno oculto con `sm:hidden` y otro con `hidden sm:block`—
 * y eso metía en el DOM dos copias de cada control **con el mismo `id`**: al pulsar la etiqueta el foco
 * habría ido al campo equivocado y un lector de pantalla habría anunciado el que no es. Se veía bien y
 * estaba roto, así que la comprobación se queda.
 */
describe('MobileDisclosure', () => {
  const contenido = (
    <div>
      <label htmlFor="filtro-x">Terreno</label>
      <select id="filtro-x">
        <option>Todos</option>
      </select>
    </div>
  );

  beforeEach(() => vi.unstubAllGlobals());

  it('renderiza los controles una sola vez en escritorio', () => {
    conAncho(ESCRITORIO);
    render(<MobileDisclosure label="Filtros" icon="tune">{contenido}</MobileDisclosure>);

    // `getBy*` falla si hay más de uno: es justo lo que se quiere detectar.
    expect(screen.getByLabelText('Terreno')).toBeInTheDocument();
    expect(document.querySelectorAll('#filtro-x')).toHaveLength(1);
  });

  it('renderiza los controles una sola vez en móvil', () => {
    conAncho(MOVIL);
    render(<MobileDisclosure label="Filtros" icon="tune">{contenido}</MobileDisclosure>);

    expect(document.querySelectorAll('#filtro-x')).toHaveLength(1);
  });

  it('en escritorio los filtros están a la vista, sin desplegable', () => {
    conAncho(ESCRITORIO);
    render(<MobileDisclosure label="Filtros" icon="tune">{contenido}</MobileDisclosure>);

    expect(screen.queryByText('Filtros')).not.toBeInTheDocument();
  });

  it('en móvil los pliega y basta una acción para abrirlos', async () => {
    // CA-5 — siguen alcanzables sin exigir más de una acción.
    conAncho(MOVIL);
    render(<MobileDisclosure label="Filtros" icon="tune">{contenido}</MobileDisclosure>);

    const disparador = screen.getByText('Filtros');
    expect(screen.getByRole('group')).not.toHaveAttribute('open');

    await userEvent.click(disparador);

    expect(screen.getByRole('group')).toHaveAttribute('open');
  });

  it('en móvil arranca abierto y con el número cuando ya hay filtros puestos', () => {
    // El peor efecto de esconder filtros es no saber que están puestos: si acotan lo que se ve, se ven.
    conAncho(MOVIL);
    render(<MobileDisclosure label="Filtros" icon="tune" activeCount={2}>{contenido}</MobileDisclosure>);

    expect(screen.getByRole('group')).toHaveAttribute('open');
    expect(screen.getByText('2')).toBeInTheDocument();
  });
});

describe('SummaryStrip', () => {
  beforeEach(() => vi.unstubAllGlobals());

  it('renderiza cada tarjeta una sola vez', () => {
    // Aquí el reparto es solo de clases, así que no hay dos árboles ni con `matchMedia` de móvil.
    conAncho(MOVIL);
    render(
      <SummaryStrip desktopClassName="grid-cols-3">
        <div id="tarjeta-a">Total</div>
        <div id="tarjeta-b">Partidas</div>
      </SummaryStrip>
    );

    expect(document.querySelectorAll('#tarjeta-a')).toHaveLength(1);
    expect(screen.getByText('Total')).toBeInTheDocument();
    expect(screen.getByText('Partidas')).toBeInTheDocument();
  });
});
