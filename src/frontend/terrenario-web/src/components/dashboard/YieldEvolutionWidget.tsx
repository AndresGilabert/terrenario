import React from 'react';
import type { DashboardYieldEvolution, TrendLine } from '../../types/dashboard.types';

const number = (value: number, decimals = 0) =>
  value.toLocaleString('es-ES', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });

/** Una fecha `YYYY-MM-DD` como instante UTC, para que la escala no dependa del huso del navegador. */
const time = (date: string) => Date.parse(`${date}T00:00:00Z`);

const dayLabel = (timestamp: number) =>
  new Date(timestamp).toLocaleDateString('es-ES', { day: 'numeric', month: 'short', timeZone: 'UTC' });

const DAY_MS = 86_400_000;

const SEASON_COLOR = '#33450d';
// Colores de la paleta de la aplicación (el lima es el de «aceite personal» en kg por destino). Las
// rectas históricas van más finas y claras que la temporada en curso, que es lo importante.
const HISTORY_STYLES = {
  general: { color: '#c9f16f', width: 3 },
  last_5_years: { color: '#8a5a00', width: 1.5 },
  last_10_years: { color: '#76786b', width: 1.5 },
} as const;

interface Reference {
  key: keyof typeof HISTORY_STYLES;
  label: string;
  color: string;
  width: number;
  line: TrendLine;
}

/**
 * PRD-101 — Evolución del rendimiento: un punto por día cosechado, la recta de tendencia de la
 * temporada y las rectas de los años anteriores (RN-015), que se prolongan por todo el periodo
 * histórico de cosecha para ver cómo se espera que evolucione el rendimiento en las próximas semanas.
 *
 * Todo se dibuja en una escala común. Las rectas van en un SVG estirado sin deformar el trazo y los
 * puntos y las etiquetas son HTML posicionado en porcentaje, para que el texto y los círculos no se
 * deformen al cambiar el ancho de la pantalla.
 */
export const YieldEvolutionWidget: React.FC<{ evolution: DashboardYieldEvolution }> = ({ evolution }) => {
  const { data, trend, history } = evolution;
  const hasSeries = data.length > 0;

  const references: Reference[] = (
    [
      { key: 'general', label: 'Histórico general', line: history.general },
      { key: 'last_5_years', label: 'Últimos 5 años', line: history.last_5_years },
      { key: 'last_10_years', label: 'Últimos 10 años', line: history.last_10_years },
    ] as const
  )
    .filter((reference): reference is typeof reference & { line: TrendLine } => reference.line !== null)
    .map((reference) => ({ ...reference, ...HISTORY_STYLES[reference.key] }));

  // Ni serie ni rectas: no hay nada que enseñar todavía.
  if (!hasSeries && references.length === 0) {
    return (
      <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-2">
        <h3 className="font-headline font-bold text-base text-[#1c1c19]">Evolución del rendimiento</h3>
        <p className="text-sm text-[#76786b] italic">
          Aún no hay rendimiento registrado, ni en esta temporada ni en años anteriores. Aparecerá aquí
          cuando declares el aceite de alguna partida.
        </p>
      </div>
    );
  }

  const times = [
    ...data.map((point) => time(point.date)),
    ...[trend, ...references.map((reference) => reference.line)].flatMap((line) =>
      line ? [time(line.from.date), time(line.to.date)] : []
    ),
  ];
  const values = [
    ...data.map((point) => point.yield_l_per_100kg),
    ...[trend, ...references.map((reference) => reference.line)].flatMap((line) =>
      line ? [line.from.value, line.to.value] : []
    ),
  ];

  // Con una sola fecha no hay escala: se abre una semana alrededor para que el punto quede centrado.
  const rawMinX = Math.min(...times);
  const rawMaxX = Math.max(...times);
  const [minX, maxX] = rawMinX === rawMaxX ? [rawMinX - 3.5 * DAY_MS, rawMaxX + 3.5 * DAY_MS] : [rawMinX, rawMaxX];

  const rawMinY = Math.min(...values);
  const rawMaxY = Math.max(...values);
  const padding = rawMinY === rawMaxY ? 1 : (rawMaxY - rawMinY) * 0.12;
  const minY = rawMinY - padding;
  const maxY = rawMaxY + padding;

  const x = (date: string) => ((time(date) - minX) / (maxX - minX)) * 100;
  const y = (value: number) => 100 - ((value - minY) / (maxY - minY)) * 100;

  const segment = (line: TrendLine) => ({
    x1: x(line.from.date),
    y1: y(line.from.value),
    x2: x(line.to.date),
    y2: y(line.to.value),
  });

  const xTicks = [0, 1 / 3, 2 / 3, 1].map((fraction) => minX + (maxX - minX) * fraction);
  const lineProps = { vectorEffect: 'non-scaling-stroke' as const, strokeLinecap: 'round' as const };

  return (
    <div className="bg-white p-6 rounded-2xl border border-[#e5e2dd] ambient-shadow space-y-5">
      <div>
        <h3 className="font-headline font-bold text-base text-[#1c1c19]">Evolución del rendimiento</h3>
        <p className="text-xs text-[#76786b]">
          {hasSeries
            ? 'Rendimiento de cada día cosechado, en litros por 100 kg.'
            : 'Todavía no hay cosechas esta temporada: se muestra la referencia de estas mismas fechas en años anteriores.'}
        </p>
      </div>

      <div
        role="img"
        aria-label={`Evolución del rendimiento: ${data.length} ${data.length === 1 ? 'día' : 'días'} con cosecha${
          references.length > 0 ? ` y ${references.length} ${references.length === 1 ? 'recta histórica' : 'rectas históricas'}` : ''
        }.`}
        className="grid grid-cols-[auto_1fr] gap-x-2"
      >
        <div className="flex flex-col justify-between h-44 text-[10px] font-semibold text-[#76786b] text-right">
          <span>{number(maxY, 1)}</span>
          <span>{number(minY, 1)}</span>
        </div>

        <div>
          <div className="relative h-44 border-b border-l border-[#e5e2dd]">
            <svg className="absolute inset-0 w-full h-full" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
              {references.map((reference) => (
                <line
                  key={reference.key}
                  {...segment(reference.line)}
                  stroke={reference.color}
                  strokeWidth={reference.width}
                  strokeDasharray="6 4"
                  data-testid={`line-${reference.key}`}
                  {...lineProps}
                />
              ))}
              {hasSeries && data.length > 1 && (
                <polyline
                  points={data.map((point) => `${x(point.date)},${y(point.yield_l_per_100kg)}`).join(' ')}
                  fill="none"
                  stroke={SEASON_COLOR}
                  strokeWidth={2.5}
                  strokeLinejoin="round"
                  data-testid="line-season"
                  {...lineProps}
                />
              )}
              {trend && (
                <line
                  {...segment(trend)}
                  stroke="#1c1c19"
                  strokeWidth={1.5}
                  strokeDasharray="3 3"
                  data-testid="line-trend"
                  {...lineProps}
                />
              )}
            </svg>

            {data.map((point) => (
              <span
                key={point.date}
                data-testid="season-point"
                className="absolute w-2.5 h-2.5 -translate-x-1/2 -translate-y-1/2 rounded-full bg-[#33450d] ring-2 ring-white"
                style={{ left: `${x(point.date)}%`, top: `${y(point.yield_l_per_100kg)}%` }}
                title={`${dayLabel(time(point.date))}: ${number(point.yield_l_per_100kg, 1)} L/100kg sobre ${number(point.kg)} kg`}
              />
            ))}
          </div>

          <div className="flex justify-between pt-1.5 text-[10px] font-semibold text-[#76786b]">
            {xTicks.map((tick) => (
              <span key={tick}>{dayLabel(tick)}</span>
            ))}
          </div>
        </div>
      </div>

      <ul className="flex flex-wrap gap-x-4 gap-y-1.5 text-[11px] font-semibold text-[#4a4b42]">
        {hasSeries && (
          <li className="flex items-center gap-1.5">
            <span className="w-4 h-0.5 rounded" style={{ backgroundColor: SEASON_COLOR }} aria-hidden="true" />
            Esta temporada
          </li>
        )}
        {trend && (
          <li className="flex items-center gap-1.5">
            <span className="w-4 border-t-2 border-dashed border-[#1c1c19]" aria-hidden="true" />
            Tendencia de esta temporada
          </li>
        )}
        {references.map((reference) => (
          <li key={reference.key} className="flex items-center gap-1.5">
            <span
              className="w-4 border-t-2 border-dashed"
              style={{ borderColor: reference.color, borderTopWidth: reference.width }}
              aria-hidden="true"
            />
            {reference.label}
          </li>
        ))}
      </ul>

      <p className="text-[11px] text-[#76786b] flex items-start gap-1.5">
        <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">info</span>
        {references.length > 0
          ? `Las rectas históricas comparan los mismos días de ${history.prior_years_with_data} ${
              history.prior_years_with_data === 1 ? 'año anterior' : 'años anteriores'
            } con dato de rendimiento y llegan hasta el final del periodo de cosecha de esos años.`
          : 'La referencia histórica aparecerá cuando haya cosechas de estas mismas fechas en años anteriores.'}
      </p>
    </div>
  );
};
