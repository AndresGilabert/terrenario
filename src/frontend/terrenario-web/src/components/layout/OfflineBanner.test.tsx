import { render, screen } from '@testing-library/react';
import { act } from 'react';
import { afterEach, describe, expect, it } from 'vitest';
import { OfflineBanner } from './OfflineBanner';
import { marcarConConexion, marcarSinConexion } from '../../lib/connectivity';

/** MVP-709 (`P-091`, HU-1) — El aviso de falta de conexión. */
describe('OfflineBanner', () => {
  afterEach(() => {
    act(() => marcarConConexion());
  });

  it('no se ve mientras hay conexión', () => {
    render(<OfflineBanner />);

    expect(screen.queryByText(/Sin conexión/)).not.toBeInTheDocument();
  });

  it('aparece al perder la conexión', () => {
    // CA-1 — y dice «sin conexión», no «no se pudieron cargar los datos».
    render(<OfflineBanner />);

    act(() => marcarSinConexion());

    expect(screen.getByText(/Sin conexión/)).toBeInTheDocument();
  });

  it('dice que lo escrito no se ha perdido', () => {
    // Sin esta frase, lo razonable al leer «sin conexión» es dar el trabajo por perdido y cerrar.
    render(<OfflineBanner />);

    act(() => marcarSinConexion());

    expect(screen.getByRole('status')).toHaveTextContent(/sigue en pantalla/i);
  });

  it('se retira al volver la conexión, sin recargar', () => {
    // CA-2 — el componente no se remonta: el mismo árbol deja de pintar el aviso.
    render(<OfflineBanner />);
    act(() => marcarSinConexion());

    act(() => marcarConConexion());

    expect(screen.queryByText(/Sin conexión/)).not.toBeInTheDocument();
  });

  it('se anuncia sin interrumpir', () => {
    // `status` y no `alert`: cortar a quien está tecleando una labor para decirle que no hay
    // cobertura empeora el momento en vez de ayudarlo.
    render(<OfflineBanner />);

    const region = screen.getByRole('status');
    expect(region).toHaveAttribute('aria-live', 'polite');
  });
});
