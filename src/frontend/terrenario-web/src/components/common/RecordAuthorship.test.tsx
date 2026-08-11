import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { RecordAuthorship, type AuthoredRecord } from './RecordAuthorship';

const registro = (overrides: Partial<AuthoredRecord> = {}): AuthoredRecord => ({
  created_by_name: 'Andrés Gilabert',
  created_at: '2025-10-20T09:00:00Z',
  updated_by_name: 'Andrés Gilabert',
  updated_at: '2025-10-20T09:00:00Z',
  ...overrides,
});

/**
 * MVP-804 (`RU-21`, `P-113`) — La autoría tal y como se lee en el modal de corrección.
 *
 * Lo que se cubre aquí es la decisión que **no** se ve en el servidor: cuándo se calla la línea de
 * última edición. El servidor manda siempre los dos nombres —es un dato del registro, no una
 * decisión de pantalla— y es la interfaz la que decide no repetirlos.
 */
describe('RecordAuthorship', () => {
  it('dice quién apuntó el registro', () => {
    // CA-2 — es la razón de ser de la historia: ante una cifra que no cuadra, saber a quién preguntar
    // sin tener que preguntar a todo el mundo (`RN-034`).
    render(<RecordAuthorship record={registro()} />);

    expect(screen.getByText(/Registrado por/)).toHaveTextContent('Andrés Gilabert');
    expect(screen.getByText(/Registrado por/)).toHaveTextContent('20 oct 2025');
  });

  it('omite la línea de última edición cuando nadie ha tocado el registro', () => {
    // CA-2 — un registro recién apuntado tiene los dos instantes iguales. Repetir el mismo nombre dos
    // veces no informa: solo mete ruido justo donde la información es de apoyo.
    render(<RecordAuthorship record={registro()} />);

    expect(screen.queryByText(/Última edición/)).not.toBeInTheDocument();
  });

  it('cuenta la última edición cuando la hizo otra persona', () => {
    render(
      <RecordAuthorship
        record={registro({ updated_by_name: 'Lucía Pérez', updated_at: '2025-11-03T18:22:00Z' })}
      />
    );

    expect(screen.getByText(/Última edición/)).toHaveTextContent('Lucía Pérez');
    expect(screen.getByText(/Última edición/)).toHaveTextContent('3 nov 2025');
  });

  it('cuenta la última edición aunque la haya hecho quien lo apuntó', () => {
    // La omisión mira el **instante**, no el nombre: corregir tu propio registro sigue siendo una
    // corrección, y esconderla dejaría creyendo que la cifra está tal cual se apuntó.
    render(<RecordAuthorship record={registro({ updated_at: '2025-10-22T11:00:00Z' })} />);

    expect(screen.getByText(/Última edición/)).toHaveTextContent('Andrés Gilabert');
    expect(screen.getByText(/Última edición/)).toHaveTextContent('22 oct 2025');
  });

  it('muestra «Cuenta eliminada» tal y como lo manda el servidor, sin inventar nada', () => {
    // CA-3 — El rótulo lo decide el servidor (`RecordAuthor.NameOf`), que es donde está el dato. Aquí
    // solo se comprueba que la interfaz no lo sustituye por un hueco ni por un «desconocido» propio:
    // el histórico operativo de terceros conserva quién lo registró justo para poder decir esto.
    render(
      <RecordAuthorship
        record={registro({
          created_by_name: 'Cuenta eliminada',
          updated_by_name: 'Cuenta eliminada',
          updated_at: '2025-11-03T18:22:00Z',
        })}
      />
    );

    expect(screen.getByText(/Registrado por/)).toHaveTextContent('Cuenta eliminada');
    expect(screen.getByText(/Última edición/)).toHaveTextContent('Cuenta eliminada');
  });
});
