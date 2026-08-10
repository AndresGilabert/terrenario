import { act, renderHook } from '@testing-library/react';
import React from 'react';
import { MemoryRouter, useLocation, useNavigationType } from 'react-router';
import { describe, expect, it } from 'vitest';
import { FILTER_ALL, useListUrlState, type ListUrlSpec } from './list-url-state';

const SPEC: ListUrlSpec = {
  filters: {
    plotId: { param: 'plot_id', fallback: FILTER_ALL },
    seasonSelection: { param: 'season_id', fallback: '' },
    destination: { param: 'destination', fallback: FILTER_ALL },
  },
  search: 'product',
  page: 'page',
};

/** Monta el hook dentro de un router y deja leer la dirección y el tipo de navegación resultantes. */
function renderAt(search = '', spec: ListUrlSpec = SPEC) {
  const location = { search: '', navigationType: '' };

  const wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(
      MemoryRouter,
      { initialEntries: [`/app/cosechas${search}`] },
      children,
      React.createElement(function Probe() {
        location.search = useLocation().search;
        location.navigationType = useNavigationType();
        return null;
      })
    );

  const view = renderHook(() => useListUrlState(spec), { wrapper });
  return { ...view, location };
}

/**
 * MVP-705 · MVP-802 — La pieza que sostiene `RN-007` en las cuatro vistas operativas. Lo que se prueba
 * aquí son sus **dos invariantes de higiene**, que son las que separan una URL usable de un vertedero:
 * los valores por defecto no se escriben, y la búsqueda sustituye la entrada de historial.
 */
describe('useListUrlState', () => {
  it('lee cada filtro de su parámetro y cae en su defecto si no está', () => {
    const { result } = renderAt('?plot_id=p-1');

    expect(result.current.values.plotId).toBe('p-1');
    expect(result.current.values.destination).toBe(FILTER_ALL);
    expect(result.current.values.seasonSelection).toBe('');
  });

  it('escribe en la URL el filtro elegido', () => {
    const { result, location } = renderAt();

    act(() => result.current.setFilter({ destination: 'aceite_para_venta' }));

    expect(location.search).toBe('?destination=aceite_para_venta');
  });

  it('no escribe los valores por defecto: los borra', () => {
    // CA-4 — sin filtros explícitos la dirección queda limpia. Escribir «todos» la llenaría de ruido
    // y, en el caso de la temporada, congelaría en el enlace la campaña de trabajo de hoy.
    const { result, location } = renderAt('?destination=aceite_para_venta');

    act(() => result.current.setFilter({ destination: FILTER_ALL }));

    expect(location.search).toBe('');
  });

  it('vuelve a la primera página al cambiar un filtro', () => {
    // Seguir en la página 4 de una lista que acaba de reducirse dejaría la pantalla vacía sin explicar
    // por qué. Va en la misma escritura que el filtro, no en un efecto aparte.
    const { result, location } = renderAt('?page=4');

    act(() => result.current.setFilter({ plotId: 'p-1' }));

    expect(location.search).toBe('?plot_id=p-1');
  });

  it('conserva los filtros que no se tocan', () => {
    const { result, location } = renderAt('?plot_id=p-1');

    act(() => result.current.setFilter({ destination: 'venta_aceituna' }));

    expect(location.search).toContain('plot_id=p-1');
    expect(location.search).toContain('destination=venta_aceituna');
  });

  it('la búsqueda sustituye la entrada de historial y el filtro la añade', () => {
    // La otra condición de higiene de `RN-007`: si el término rebotado añadiera entrada, escribir
    // «sulfatado» dejaría una por carácter y el botón «atrás» quedaría inservible. Los filtros sí la
    // añaden, para que «atrás» devuelva al estado anterior.
    const { result, location } = renderAt();

    act(() => result.current.setSearch('abono'));
    expect(location.navigationType).toBe('REPLACE');
    expect(location.search).toBe('?product=abono');

    act(() => result.current.setFilter({ plotId: 'p-1' }));
    expect(location.navigationType).toBe('PUSH');
  });

  it('deja escribir un filtro sin entrada de historial cuando es una corrección', () => {
    // MVP-801 — La corrección de un ámbito que el servidor no ha podido aplicar no es una navegación:
    // con entrada propia, «atrás» devolvería a la URL con el ámbito ajeno y volvería a corregirse.
    const { result, location } = renderAt('?season_id=de-otro-workspace');

    act(() => result.current.setFilter({ seasonSelection: '' }, { replace: true }));

    expect(location.navigationType).toBe('REPLACE');
    expect(location.search).toBe('');
  });

  it('cuenta como activos solo los filtros puestos a mano', () => {
    const { result } = renderAt('?plot_id=p-1&product=abono');

    expect(result.current.activeCount).toBe(2);
    expect(result.current.hasFilters).toBe(true);
  });

  it('quita todos los filtros de una vez', () => {
    const { result, location } = renderAt('?plot_id=p-1&product=abono&page=3');

    act(() => result.current.clearFilters());

    expect(location.search).toBe('');
  });

  it('ignora una página que no es un entero positivo', () => {
    // Basura en la URL, no un error del usuario: se cae a la primera en vez de pedir una página −3.
    expect(renderAt('?page=-3').result.current.page).toBe(1);
    expect(renderAt('?page=hola').result.current.page).toBe(1);
  });

  describe('sin paginación ni búsqueda declaradas', () => {
    const SIN_PAGINA: ListUrlSpec = { filters: SPEC.filters };

    it('no escribe `page` al cambiar un filtro', () => {
      const { result, location } = renderAt('', SIN_PAGINA);

      act(() => result.current.setFilter({ plotId: 'p-1' }));

      expect(location.search).toBe('?plot_id=p-1');
    });
  });
});
