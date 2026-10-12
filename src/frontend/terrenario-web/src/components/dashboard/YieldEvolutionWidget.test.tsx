import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { DashboardYieldEvolution, TrendLine } from '../../types/dashboard.types';
import { YieldEvolutionWidget } from './YieldEvolutionWidget';

const scope = { season: null, plots: [] } as unknown as DashboardYieldEvolution['scope'];

const line = (from: [string, number], to: [string, number]): TrendLine => ({
  from: { date: from[0], value: from[1] },
  to: { date: to[0], value: to[1] },
});

const evolution = (partial: Partial<DashboardYieldEvolution> = {}): DashboardYieldEvolution => ({
  scope,
  data: [],
  trend: null,
  history: { general: null, last_5_years: null, last_10_years: null, prior_years_with_data: 0 },
  ...partial,
});

/**
 * PRD-101 — Lo que el agricultor ve: un punto por día, la tendencia de su temporada y las rectas
 * históricas con las que decidir si adelanta o retrasa la cosecha.
 */
describe('YieldEvolutionWidget', () => {
  it('dibuja un punto por día cosechado y la recta de la temporada', () => {
    render(
      <YieldEvolutionWidget
        evolution={evolution({
          data: [
            { date: '2026-10-05', yield_l_per_100kg: 18, kg: 1000 },
            { date: '2026-10-15', yield_l_per_100kg: 20, kg: 2000 },
          ],
          trend: line(['2026-10-05', 17.5], ['2026-10-15', 20.5]),
        })}
      />
    );

    expect(screen.getAllByTestId('season-point')).toHaveLength(2);
    expect(screen.getByTestId('line-season')).toBeInTheDocument();
    expect(screen.getByTestId('line-trend')).toBeInTheDocument();
    expect(screen.getByText('Tendencia de esta temporada')).toBeInTheDocument();
  });

  it('con una única cosecha dibuja el punto y no inventa una recta', () => {
    // Un usuario con un único bancal y una única cosecha: no hay pendiente que calcular.
    render(
      <YieldEvolutionWidget
        evolution={evolution({ data: [{ date: '2026-10-05', yield_l_per_100kg: 18, kg: 1000 }] })}
      />
    );

    expect(screen.getAllByTestId('season-point')).toHaveLength(1);
    expect(screen.queryByTestId('line-season')).not.toBeInTheDocument();
    expect(screen.queryByTestId('line-trend')).not.toBeInTheDocument();
  });

  it('dibuja solo las rectas históricas disponibles y las rotula', () => {
    render(
      <YieldEvolutionWidget
        evolution={evolution({
          data: [{ date: '2026-10-05', yield_l_per_100kg: 18, kg: 1000 }],
          history: {
            general: line(['2026-10-01', 15], ['2026-12-20', 21]),
            last_5_years: line(['2026-10-01', 16], ['2026-12-20', 20]),
            last_10_years: null,
            prior_years_with_data: 6,
          },
        })}
      />
    );

    expect(screen.getByTestId('line-general')).toBeInTheDocument();
    expect(screen.getByTestId('line-last_5_years')).toBeInTheDocument();
    expect(screen.queryByTestId('line-last_10_years')).not.toBeInTheDocument();
    expect(screen.getByText('Últimos 5 años')).toBeInTheDocument();
    expect(screen.queryByText('Últimos 10 años')).not.toBeInTheDocument();
    expect(screen.getByText(/6 años anteriores/)).toBeInTheDocument();
  });

  it('sin cosechas esta temporada muestra solo la referencia histórica', () => {
    render(
      <YieldEvolutionWidget
        evolution={evolution({
          history: {
            general: line(['2026-11-15', 20], ['2026-12-10', 24]),
            last_5_years: null,
            last_10_years: null,
            prior_years_with_data: 2,
          },
        })}
      />
    );

    expect(screen.queryByTestId('season-point')).not.toBeInTheDocument();
    expect(screen.getByTestId('line-general')).toBeInTheDocument();
    expect(screen.getByText(/Todavía no hay cosechas esta temporada/)).toBeInTheDocument();
  });

  it('sin serie ni histórico explica que aún no hay rendimiento', () => {
    render(<YieldEvolutionWidget evolution={evolution()} />);

    expect(screen.getByText(/Aún no hay rendimiento registrado/)).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });

  it('coloca los puntos según su fecha y su rendimiento', () => {
    render(
      <YieldEvolutionWidget
        evolution={evolution({
          data: [
            { date: '2026-10-01', yield_l_per_100kg: 10, kg: 100 },
            { date: '2026-10-11', yield_l_per_100kg: 20, kg: 100 },
          ],
        })}
      />
    );

    const [first, second] = screen.getAllByTestId('season-point');
    expect(first.style.left).toBe('0%');
    expect(second.style.left).toBe('100%');
    // El de más rendimiento queda más arriba (menor `top`).
    expect(parseFloat(second.style.top)).toBeLessThan(parseFloat(first.style.top));
  });
});
