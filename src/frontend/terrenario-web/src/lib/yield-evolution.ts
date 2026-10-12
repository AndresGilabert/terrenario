import type { DashboardYieldEvolution } from '../types/dashboard.types';

/** PRD-101 — Hay algo que dibujar de la referencia histórica: al menos una recta. */
export const hasYieldHistory = (history: DashboardYieldEvolution['history']) =>
  history.general !== null || history.last_5_years !== null || history.last_10_years !== null;
