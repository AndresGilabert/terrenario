import { act, renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useSeasonScope } from './season-scope';
import { ALL_SEASONS, type SeasonScope } from '../types/season.types';

const scope = (overrides: Partial<SeasonScope> = {}): SeasonScope => ({
  season: {
    id: 's-1',
    name: 'Campaña 2025/26',
    status: 'abierta',
    start_date: '2025-10-01',
    end_date: '2026-03-31',
  },
  all_seasons: false,
  ...overrides,
});

/**
 * MVP-701 (`P-082`) — El defecto de temporada lo resuelve el servidor (RN-008). Lo que se prueba aquí
 * es la pieza que lo hace posible sin bucle: **lo que se pide** y **lo que se muestra** son dos cosas
 * distintas, y la respuesta solo escribe en la segunda.
 */
describe('useSeasonScope', () => {
  it('no pide temporada mientras el usuario no elija', () => {
    const { result } = renderHook(() => useSeasonScope());

    expect(result.current.requested).toBeUndefined();
    expect(result.current.isExplicit).toBe(false);
  });

  it('muestra la campaña que aplicó el servidor sin volver a pedirla', () => {
    const { result } = renderHook(() => useSeasonScope());

    act(() => result.current.applyFromResponse(scope()));

    expect(result.current.value).toBe('s-1');
    expect(result.current.label).toBe('Campaña 2025/26');
    // La clave de que no haya bucle: registrar la respuesta no cambia lo que se pide.
    expect(result.current.requested).toBeUndefined();
  });

  it('manda «todas» solo cuando el usuario lo elige', () => {
    const { result } = renderHook(() => useSeasonScope());

    act(() => result.current.applyFromResponse(scope()));
    act(() => result.current.select(ALL_SEASONS));

    expect(result.current.requested).toBe(ALL_SEASONS);
    expect(result.current.isExplicit).toBe(true);
  });

  it('la elección del usuario manda sobre lo que diga la respuesta', () => {
    const { result } = renderHook(() => useSeasonScope());

    act(() => result.current.select('s-9'));
    act(() => result.current.applyFromResponse(scope()));

    expect(result.current.value).toBe('s-9');
  });

  it('vuelve al defecto del servidor al quitar filtros', () => {
    const { result } = renderHook(() => useSeasonScope());

    act(() => result.current.applyFromResponse(scope()));
    act(() => result.current.select(ALL_SEASONS));
    act(() => result.current.reset());

    expect(result.current.requested).toBeUndefined();
    expect(result.current.value).toBe('s-1');
  });

  it('se posiciona en «todas» cuando el Workspace no tiene campaña de trabajo', () => {
    const { result } = renderHook(() => useSeasonScope());

    act(() => result.current.applyFromResponse(scope({ season: null, all_seasons: true })));

    expect(result.current.value).toBe(ALL_SEASONS);
    expect(result.current.isExplicit).toBe(false);
  });
});
