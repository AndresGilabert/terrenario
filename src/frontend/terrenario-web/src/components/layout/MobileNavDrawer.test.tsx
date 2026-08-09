import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { MemoryRouter } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { MobileNavDrawer } from './MobileNavDrawer';
import { reiniciarCapasParaTests } from '../../lib/use-capa-modal';

vi.mock('./AppSidebar', () => ({
  AppSidebar: ({ onNavigate }: { onNavigate?: () => void }) => (
    <nav>
      <button type="button" onClick={onNavigate}>
        Diario
      </button>
      <button type="button" onClick={onNavigate}>
        Cosechas
      </button>
    </nav>
  ),
}));

function Pantalla() {
  const [abierto, setAbierto] = useState(false);
  return (
    // El `id="root"` no es decorativo: es el nodo que la capa marca como inerte.
    <div id="root">
      <button type="button" onClick={() => setAbierto(true)}>
        Abrir menú
      </button>
      <a href="/algo">Enlace del fondo</a>
      <MobileNavDrawer isOpen={abierto} onClose={() => setAbierto(false)} />
    </div>
  );
}

const abrir = async () => {
  render(
    <MemoryRouter>
      <Pantalla />
    </MemoryRouter>
  );
  await userEvent.click(screen.getByRole('button', { name: 'Abrir menú' }));
};

/**
 * MVP-999 (`P-104`) — El drawer era el último overlay del producto sin trampa de foco. `MVP-704` cerró
 * `P-055` en los once modales y este quedó fuera por tener otra forma.
 */
describe('MobileNavDrawer', () => {
  // En `beforeEach` y no en `afterEach`: los hooks de limpieza de testing-library corren **después**
  // de los propios, así que reiniciar al final dejaba el contador en -1 —el desmontaje del caso
  // anterior todavía estaba por venir— y el siguiente `abrir` no llegaba a 1, con lo que el fondo no
  // se apagaba. El síntoma era un fallo en la prueba, no en el producto.
  beforeEach(() => reiniciarCapasParaTests());

  it('no se pinta mientras está cerrado', () => {
    render(
      <MemoryRouter>
        <Pantalla />
      </MemoryRouter>
    );

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(document.getElementById('root')).not.toHaveAttribute('inert');
  });

  it('expone role, aria-modal y nombre accesible', async () => {
    await abrir();

    const dialogo = screen.getByRole('dialog');
    expect(dialogo).toHaveAttribute('aria-modal', 'true');
    expect(dialogo).toHaveAccessibleName('Navegación');
  });

  it('apaga el fondo y vive fuera de él', async () => {
    await abrir();

    // El portal no es opcional: `inert` se aplica sobre `#root`, así que un drawer pintado dentro se
    // apagaría a sí mismo.
    expect(document.getElementById('root')).toHaveAttribute('inert');
    expect(screen.getByRole('dialog').closest('#root')).toBeNull();
    expect(screen.getByText('Enlace del fondo').closest('[inert]')).not.toBeNull();
  });

  it('cierra con Escape', async () => {
    // No lo tenía: solo cerraba pulsando el velo, que con teclado no se alcanza.
    await abrir();

    await userEvent.keyboard('{Escape}');

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('cicla el foco dentro del panel', async () => {
    await abrir();

    const diario = screen.getByRole('button', { name: 'Diario' });
    const cosechas = screen.getByRole('button', { name: 'Cosechas' });

    expect(diario).toHaveFocus();
    await userEvent.tab();
    expect(cosechas).toHaveFocus();
    await userEvent.tab();
    expect(diario).toHaveFocus();
  });

  it('devuelve el foco al botón que lo abrió', async () => {
    await abrir();

    await userEvent.keyboard('{Escape}');

    expect(screen.getByRole('button', { name: 'Abrir menú' })).toHaveFocus();
  });

  it('devuelve el fondo a la vida al cerrar', async () => {
    await abrir();

    await userEvent.keyboard('{Escape}');

    expect(document.getElementById('root')).not.toHaveAttribute('inert');
  });

  it('cierra al navegar', async () => {
    // Es lo que ya hacía y no debe perderse: pulsar una sección lleva a ella y cierra el menú.
    await abrir();

    await userEvent.click(screen.getByRole('button', { name: 'Cosechas' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
});
