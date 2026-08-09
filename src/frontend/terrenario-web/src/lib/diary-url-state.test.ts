import { act, renderHook } from '@testing-library/react';
import React from 'react';
import { MemoryRouter, useLocation, useNavigationType } from 'react-router';
import { describe, expect, it } from 'vitest';
import { DIARY_FILTER_ALL, useDiaryUrlState } from './diary-url-state';

/** El hook más la URL viva, para poder afirmar sobre lo que acaba en la barra de direcciones. */
function renderDiaryUrl(initialUrl = '/app/diario') {
  const wrapper = ({ children }: { children: React.ReactNode }) =>
    React.createElement(MemoryRouter, { initialEntries: [initialUrl] }, children);

  return renderHook(
    () => {
      const state = useDiaryUrlState();
      const location = useLocation();
      // `PUSH` deja entrada de historial; `REPLACE` la sustituye. Es exactamente lo que separa un
      // cambio de filtro de una pulsación en el cuadro de búsqueda.
      const navigationType = useNavigationType();
      return { state, search: location.search, navigationType };
    },
    { wrapper }
  );
}

/**
 * MVP-705 (`P-072`) — El estado de navegación del diario vive en la URL (RN-007).
 *
 * Lo que se fija aquí son las dos invariantes que hacen que la URL sea usable y no un vertedero: que
 * **los defectos no se escriban** y que **la búsqueda no llene el historial**.
 */
describe('useDiaryUrlState', () => {
  it('arranca sin ensuciar la URL', () => {
    const { result } = renderDiaryUrl();

    expect(result.current.search).toBe('');
    expect(result.current.state.type).toBe(DIARY_FILTER_ALL);
    expect(result.current.state.page).toBe(1);
    expect(result.current.state.hasFilters).toBe(false);
  });

  it('lleva cada filtro a la URL', () => {
    const { result } = renderDiaryUrl();

    act(() => result.current.state.setFilter({ type: 'cosecha' }));
    expect(result.current.search).toContain('type=cosecha');

    act(() => result.current.state.setFilter({ plotId: 'p-1' }));
    expect(result.current.search).toContain('plot_id=p-1');

    act(() => result.current.state.setFilter({ workerId: 'w-1' }));
    expect(result.current.search).toContain('worker_id=w-1');
  });

  it('borra el parámetro al volver al valor por defecto', () => {
    // CA-5 — «todos» no es un filtro: escribirlo dejaría una URL que parece acotada sin estarlo.
    const { result } = renderDiaryUrl('/app/diario?type=cosecha');
    expect(result.current.state.type).toBe('cosecha');

    act(() => result.current.state.setFilter({ type: DIARY_FILTER_ALL }));

    expect(result.current.search).not.toContain('type');
  });

  it('no escribe la temporada por defecto', () => {
    // CA-5 — desde MVP-701 la resuelve el servidor (RN-008). Fijarla en la URL congelaría la campaña
    // de trabajo del día en que se compartió el enlace.
    const { result } = renderDiaryUrl();

    act(() => result.current.state.setFilter({ seasonSelection: '' }));

    expect(result.current.search).not.toContain('season_id');
  });

  it('reproduce la vista al leer una URL con filtros', () => {
    // CA-2 — pegar el enlace en otra pestaña tiene que enseñar lo mismo.
    const { result } = renderDiaryUrl(
      '/app/diario?type=compra&plot_id=p-9&worker_id=w-9&season_id=all&search=abono&page=3'
    );

    expect(result.current.state).toMatchObject({
      type: 'compra',
      plotId: 'p-9',
      workerId: 'w-9',
      seasonSelection: 'all',
      search: 'abono',
      page: 3,
      hasFilters: true,
    });
  });

  it('vuelve a la primera página al cambiar un filtro', () => {
    // Seguir en la página 4 de un diario que acaba de reducirse a 12 entradas dejaría la pantalla
    // vacía sin explicar por qué.
    const { result } = renderDiaryUrl('/app/diario?page=4');

    act(() => result.current.state.setFilter({ type: 'cosecha' }));

    expect(result.current.state.page).toBe(1);
    expect(result.current.search).not.toContain('page');
  });

  it('no escribe la página 1', () => {
    const { result } = renderDiaryUrl('/app/diario?page=3');

    act(() => result.current.state.setPage(1));

    expect(result.current.search).not.toContain('page');
  });

  it('cae a la primera página si la URL trae una basura', () => {
    // Una página −3 en la URL no es un error del usuario: no hay que pedírsela al servidor.
    expect(renderDiaryUrl('/app/diario?page=-3').result.current.state.page).toBe(1);
    expect(renderDiaryUrl('/app/diario?page=abc').result.current.state.page).toBe(1);
    expect(renderDiaryUrl('/app/diario?page=1.5').result.current.state.page).toBe(1);
  });

  it('la búsqueda sustituye la entrada de historial y el filtro la añade', () => {
    // CA-3 — es la diferencia que hace que «atrás» siga sirviendo: teclear no puede dejar una entrada
    // por carácter, pero cambiar un filtro sí tiene que poder deshacerse.
    const { result } = renderDiaryUrl();

    act(() => result.current.state.setSearch('riego'));
    expect(result.current.navigationType).toBe('REPLACE');

    act(() => result.current.state.setFilter({ type: 'cosecha' }));
    expect(result.current.navigationType).toBe('PUSH');

    act(() => result.current.state.setPage(2));
    expect(result.current.navigationType).toBe('PUSH');
  });

  it('quitar filtros deja la URL limpia', () => {
    const { result } = renderDiaryUrl(
      '/app/diario?type=compra&plot_id=p-9&worker_id=w-9&season_id=all&search=abono&page=3'
    );

    act(() => result.current.state.clearFilters());

    expect(result.current.search).toBe('');
    expect(result.current.state.hasFilters).toBe(false);
  });
});
