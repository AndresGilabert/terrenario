import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { MergeMasterDialog } from './MergeMasterDialog';
import type { MasterRecordLike } from '../../types/master.types';

/**
 * MVP-806 (HU-2, CA-4) — El diálogo de fusión es donde el usuario decide algo **irreversible** sobre
 * dos fichas, así que lo que se prueba es que la pantalla diga la verdad antes de que pulse: cuál
 * desaparece, cuántos registros se mueven y cuándo el sentido no lo elige él.
 */
describe('MergeMasterDialog', () => {
  const record = (id: string, name: string, usage: number | null = 0): MasterRecordLike => ({
    id,
    name,
    usage_count: usage,
  });

  const renderDialog = (props: Partial<React.ComponentProps<typeof MergeMasterDialog>> = {}) => {
    const onConfirm = vi.fn();
    render(
      <MergeMasterDialog
        isOpen
        kindLabel="terrenos"
        record={record('a', 'Bancal de arriba')}
        candidates={[record('b', 'Bancal de arriba (2)', 4)]}
        isBusy={false}
        errorMessage={null}
        onCancel={vi.fn()}
        onConfirm={onConfirm}
        {...props}
      />
    );
    return { onConfirm };
  };

  const chooseOther = async (label: string) => {
    await userEvent.selectOptions(screen.getByLabelText('Fusionar con'), label);
  };

  it('no deja confirmar hasta que se elige la otra ficha', () => {
    renderDialog();

    expect(screen.getByRole('button', { name: 'Fusionar' })).toBeDisabled();
  });

  it('nombra la ficha que desaparece y cuántos registros se reapuntan', async () => {
    renderDialog();

    await chooseOther('Bancal de arriba (2)');

    expect(screen.getByText(/Se conserva/)).toHaveTextContent(
      'Se conserva Bancal de arriba y desaparece Bancal de arriba (2).'
    );
    expect(screen.getByText('Se reapuntarán 4 registros a la ficha que se conserva.')).toBeInTheDocument();
  });

  it('permite invertir el sentido cuando ninguna de las dos está protegida', async () => {
    const { onConfirm } = renderDialog();

    await chooseOther('Bancal de arriba (2)');
    await userEvent.click(screen.getByRole('button', { name: /Conservar «Bancal de arriba \(2\)»/ }));
    await userEvent.click(screen.getByRole('button', { name: 'Fusionar' }));

    // Superviviente y absorbida, en ese orden: ahora se conserva la que se eligió en el desplegable.
    expect(onConfirm).toHaveBeenCalledWith('b', 'a');
  });

  it('no promete una cifra que no conoce', async () => {
    // `usage_count: null` es «no consultado». Escribir «0 registros» ahí sería inventarse un dato.
    renderDialog({ candidates: [record('b', 'Sin consultar', null)] });

    await chooseOther('Sin consultar');

    expect(
      screen.getByText('Los registros de la ficha absorbida pasarán a la que se conserva.')
    ).toBeInTheDocument();
  });

  it('fija el sentido cuando la otra ficha es la de un miembro y explica por qué', async () => {
    // CA-4 — la fusión se abrió desde la cuadrilla, pero la que sobrevive es la del miembro.
    const { onConfirm } = renderDialog({
      record: record('crew', 'Juan Pérez (2)', 3),
      candidates: [record('member', 'Juan Pérez', 10)],
      isProtected: (candidate) => candidate.id === 'member',
      protectedReason: 'La ficha de un miembro no puede desaparecer.',
    });

    await chooseOther('Juan Pérez');

    expect(screen.getByText(/Se conserva/)).toHaveTextContent(
      'Se conserva Juan Pérez y desaparece Juan Pérez (2).'
    );
    // Y sin ofrecer invertirlo: no es una preferencia, es una regla.
    expect(screen.queryByRole('button', { name: /Conservar «/ })).not.toBeInTheDocument();
    expect(screen.getByText('La ficha de un miembro no puede desaparecer.')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Fusionar' }));
    expect(onConfirm).toHaveBeenCalledWith('member', 'crew');
  });

  it('impide fusionar dos fichas de miembro, que son dos personas distintas', async () => {
    const { onConfirm } = renderDialog({
      record: record('m1', 'Juan Pérez'),
      candidates: [record('m2', 'Juan Perez')],
      isProtected: () => true,
      protectedReason: 'La ficha de un miembro no puede desaparecer.',
    });

    await chooseOther('Juan Perez');

    expect(screen.getByRole('alert')).toHaveTextContent('La ficha de un miembro no puede desaparecer.');
    expect(screen.getByRole('button', { name: 'Fusionar' })).toBeDisabled();
    expect(onConfirm).not.toHaveBeenCalled();
  });
});
