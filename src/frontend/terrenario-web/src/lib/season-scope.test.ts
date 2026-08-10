import { act, renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
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

  it('mantiene la elección del usuario mientras el servidor no ha contestado a ella', () => {
    const { result } = renderHook(() => useSeasonScope());

    // La respuesta que hay registrada es la de la selección **anterior**: no dice nada sobre `s-9`.
    act(() => result.current.applyFromResponse(scope()));
    act(() => result.current.select('s-9'));

    expect(result.current.value).toBe('s-9');
    expect(result.current.requested).toBe('s-9');
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

  /**
   * MVP-801 (`P-108`) — El caso que la pantalla afirmaba en falso: un `season_id` heredado de otro
   * Workspace. El servidor cae al defecto de RN-008, pero el control daba por buena la selección
   * explícita; como ese identificador no está entre las opciones, el `<select>` caía en la primera y
   * rotulaba «Todas las temporadas» mientras se veía **una** campaña.
   */
  describe('cuando el servidor aplica un ámbito distinto del pedido', () => {
    it('muestra la campaña aplicada y no la pedida', () => {
      const { result } = renderHook(() => useSeasonScope());

      act(() => result.current.select('de-otro-workspace'));
      act(() => result.current.applyFromResponse(scope()));

      expect(result.current.value).toBe('s-1');
      expect(result.current.label).toBe('Campaña 2025/26');
    });

    it('devuelve el control al defecto para que deje de pedirse', () => {
      const { result } = renderHook(() => useSeasonScope());

      act(() => result.current.select('de-otro-workspace'));
      act(() => result.current.applyFromResponse(scope()));

      // Corregir la selección es lo que limpia la URL en las vistas que la usan de almacén (CA-4).
      expect(result.current.requested).toBeUndefined();
      expect(result.current.isExplicit).toBe(false);
    });

    it('corrige por la vía de `onCorrect`, que no deja entrada de historial', () => {
      const onSelect = vi.fn();
      const onCorrect = vi.fn();

      const { result } = renderHook(
        ({ selection }: { selection: string }) => useSeasonScope({ selection, onSelect, onCorrect }),
        { initialProps: { selection: 'de-otro-workspace' } }
      );

      act(() => result.current.applyFromResponse(scope()));

      // La corrección **sustituye** la entrada: con `onSelect` se añadiría una, y «atrás» devolvería a
      // la dirección con el ámbito ajeno para volver a corregirla en bucle.
      expect(onCorrect).toHaveBeenCalledWith('');
      expect(onSelect).not.toHaveBeenCalled();
    });

    it('no corrige lo que el servidor sí ha aplicado', () => {
      const { result } = renderHook(() => useSeasonScope());

      act(() => result.current.select('s-1'));
      act(() => result.current.applyFromResponse(scope()));

      expect(result.current.requested).toBe('s-1');
      expect(result.current.value).toBe('s-1');
    });

    it('no corrige la elección explícita de «todas»', () => {
      const { result } = renderHook(() => useSeasonScope());

      act(() => result.current.select(ALL_SEASONS));
      act(() => result.current.applyFromResponse(scope({ season: null, all_seasons: true })));

      expect(result.current.requested).toBe(ALL_SEASONS);
      expect(result.current.value).toBe(ALL_SEASONS);
    });
  });
});
