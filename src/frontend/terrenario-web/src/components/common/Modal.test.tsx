import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { Modal } from './Modal';

/**
 * Escenario del defecto original de `P-055`: un formulario **en línea, en el fondo**, y un modal
 * encima. El fallo era que el envío del fondo seguía alcanzándose y disparaba el alta equivocada.
 */
function Pantalla({ closeOnBackdrop = true }: { closeOnBackdrop?: boolean } = {}) {
  const [abierto, setAbierto] = useState(false);
  const [altaDelFondo, setAltaDelFondo] = useState(0);

  return (
    // El `id="root"` no es decorativo: es el nodo que el modal marca como inerte.
    <div id="root">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          setAltaDelFondo((n) => n + 1);
        }}
      >
        <input aria-label="Material del fondo" />
        <button type="submit">Registrar del fondo</button>
      </form>

      <p>Altas del fondo: {altaDelFondo}</p>

      <button type="button" onClick={() => setAbierto(true)}>
        Abrir modal
      </button>

      <Modal
        isOpen={abierto}
        onClose={() => setAbierto(false)}
        title="Registrar cosecha"
        closeOnBackdrop={closeOnBackdrop}
      >
        <input aria-label="Kilos" />
        <button type="button">Guardar</button>
      </Modal>
    </div>
  );
}

/**
 * MVP-704 (`P-055`) — El modal común.
 *
 * Este punto **se perdió una vez**: se le asignó destino `MVP-502` y esa historia se cerró sin
 * recogerlo. Por eso la cobertura es explícita sobre cada una de las tres piezas y sobre el defecto
 * funcional que lo originó, y no solo sobre «se abre y se cierra».
 */
describe('Modal', () => {
  const abrir = async () => {
    render(<Pantalla />);
    await userEvent.click(screen.getByRole('button', { name: 'Abrir modal' }));
    return screen.getByRole('dialog');
  };

  it('expone role, aria-modal y nombre accesible', async () => {
    const dialogo = await abrir();

    expect(dialogo).toHaveAttribute('aria-modal', 'true');
    expect(dialogo).toHaveAccessibleName('Registrar cosecha');
  });

  it('apaga el fondo mientras está abierto', async () => {
    // CA-1 — es lo que de verdad impide llegar al fondo: no solo el tabulador, también el clic y el
    // recorrido de un lector de pantalla.
    await abrir();

    expect(document.getElementById('root')).toHaveAttribute('inert');
  });

  it('devuelve el fondo a la vida al cerrar', async () => {
    await abrir();
    await userEvent.keyboard('{Escape}');

    expect(document.getElementById('root')).not.toHaveAttribute('inert');
  });

  it('no deja disparar el alta del fondo con el modal abierto', async () => {
    // CA-5 — el defecto exacto que originó `P-055`, reproducido.
    render(<Pantalla />);
    await userEvent.click(screen.getByRole('button', { name: 'Abrir modal' }));

    const envioDelFondo = screen.getByRole('button', { name: 'Registrar del fondo' });
    // `inert` no lo simula jsdom, así que se comprueba lo que sí puede comprobarse aquí: que el
    // control del fondo está dentro del subárbol marcado como inerte. En navegador eso lo hace
    // inalcanzable; la comprobación conducida sobre la aplicación real cierra el criterio.
    expect(envioDelFondo.closest('[inert]')).not.toBeNull();
    // Y el diálogo **no** está dentro de ese subárbol: vive en un portal fuera de `#root`.
    expect(screen.getByRole('dialog').closest('[inert]')).toBeNull();
  });

  it('cicla el foco dentro del diálogo', async () => {
    // CA-1 — tabular no alcanza ningún control del fondo y el foco vuelve al principio.
    await abrir();

    const kilos = screen.getByLabelText('Kilos');
    const guardar = screen.getByRole('button', { name: 'Guardar' });
    const cerrar = screen.getByRole('button', { name: 'Cerrar' });

    expect(kilos).toHaveFocus();
    await userEvent.tab();
    expect(guardar).toHaveFocus();
    await userEvent.tab();
    // El último es el botón de cerrar de la cabecera; el siguiente vuelve al primero.
    expect(cerrar).toHaveFocus();
    await userEvent.tab();
    expect(kilos).toHaveFocus();
  });

  it('cicla hacia atrás con Mayús+Tab', async () => {
    await abrir();

    expect(screen.getByLabelText('Kilos')).toHaveFocus();
    await userEvent.tab({ shift: true });
    expect(screen.getByRole('button', { name: 'Cerrar' })).toHaveFocus();
  });

  it('lleva el foco al primer control al abrir', async () => {
    await abrir();

    expect(screen.getByLabelText('Kilos')).toHaveFocus();
  });

  it('devuelve el foco al control que lo abrió', async () => {
    // CA-3 — si no, el foco aterriza en el `body` y hay que rehacer todo el camino con el teclado.
    render(<Pantalla />);
    const disparador = screen.getByRole('button', { name: 'Abrir modal' });
    await userEvent.click(disparador);

    await userEvent.keyboard('{Escape}');

    expect(disparador).toHaveFocus();
  });

  it('cierra con Escape', async () => {
    // CA-2 — de forma uniforme en todo el producto, que es la mitad del valor de tener uno común.
    await abrir();

    await userEvent.keyboard('{Escape}');

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('cierra al pulsar fuera del panel', async () => {
    const dialogo = await abrir();

    await userEvent.click(dialogo.parentElement!);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('no cierra al pulsar dentro del panel', async () => {
    // Un clic en el cuerpo del formulario no puede descartar lo escrito.
    await abrir();

    await userEvent.click(screen.getByLabelText('Kilos'));

    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('permite desactivar el cierre al pulsar fuera', async () => {
    render(<Pantalla closeOnBackdrop={false} />);
    await userEvent.click(screen.getByRole('button', { name: 'Abrir modal' }));
    const dialogo = screen.getByRole('dialog');

    await userEvent.click(dialogo.parentElement!);

    expect(screen.getByRole('dialog')).toBeInTheDocument();
  });

  it('mantiene el fondo apagado hasta que se cierra el último de dos modales', async () => {
    // Confirmar un borrado desde el modal de corrección apila dos: con un booleano en vez de un
    // contador, cerrar el de arriba reactivaría el fondo con el de abajo todavía abierto.
    const cerrarPrimero = vi.fn();
    const { rerender } = render(
      <div id="root">
        <Modal isOpen onClose={cerrarPrimero} title="Corregir">
          <button type="button">A</button>
        </Modal>
        <Modal isOpen onClose={() => {}} title="Confirmar">
          <button type="button">B</button>
        </Modal>
      </div>
    );

    expect(document.getElementById('root')).toHaveAttribute('inert');

    rerender(
      <div id="root">
        <Modal isOpen onClose={cerrarPrimero} title="Corregir">
          <button type="button">A</button>
        </Modal>
        <Modal isOpen={false} onClose={() => {}} title="Confirmar">
          <button type="button">B</button>
        </Modal>
      </div>
    );

    expect(document.getElementById('root')).toHaveAttribute('inert');
  });
});
