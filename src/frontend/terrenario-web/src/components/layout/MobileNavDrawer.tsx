import React, { useRef } from 'react';
import { createPortal } from 'react-dom';
import { useCapaModal } from '../../lib/use-capa-modal';
import { AppSidebar } from './AppSidebar';

interface MobileNavDrawerProps {
  isOpen: boolean;
  onClose: () => void;
}

/**
 * MVP-999 (`P-104`) — Navegación lateral en móvil.
 *
 * Era el **último overlay del producto sin trampa de foco**. `MVP-704` cerró `P-055` en los once
 * modales, pero este quedó fuera: tapaba visualmente y cerraba al pulsar el velo, y nada más. Con él
 * abierto, el tabulador seguía recorriendo la página de detrás, no había `Escape` y un lector de
 * pantalla leía las dos cosas a la vez.
 *
 * <b>No pasa por `Modal` a propósito.</b> Su forma es otra —panel lateral a sangre, alto completo, sin
 * título ni cabecera— y meterlo en el componente común habría obligado a darle un modo «lateral», es
 * decir, una segunda personalidad por un solo uso. Lo que sí comparte es lo que **no** es maqueta:
 * {@link useCapaModal} aporta `inert` sobre el fondo, la trampa de foco y la restauración.
 *
 * <b>El portal no es opcional.</b> `inert` se aplica sobre `#root`; si el panel se pintara donde está
 * declarado quedaría dentro y se apagaría a sí mismo. Es la misma razón por la que `Modal` porta a
 * `body`.
 */
export const MobileNavDrawer: React.FC<MobileNavDrawerProps> = ({ isOpen, onClose }) => {
  const panelRef = useRef<HTMLDivElement>(null);

  useCapaModal({ activa: isOpen, contenedor: panelRef, onEscape: onClose });

  if (!isOpen) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 lg:hidden flex">
      <div
        className="fixed inset-0 bg-black/40 backdrop-blur-xs"
        onClick={onClose}
        aria-hidden="true"
      />
      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label="Navegación"
        tabIndex={-1}
        className="relative z-10 h-full max-w-xs w-full shadow-2xl outline-none"
      >
        <AppSidebar onNavigate={onClose} />
      </div>
    </div>,
    document.body
  );
};
