import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

interface NotFoundViewProps {
  /**
   * `embedded` (defecto) se pinta dentro del shell del área operativa, que ya aporta cabecera y
   * lateral: solo devuelve la tarjeta. `fullscreen` es para rutas desconocidas **fuera** de `/app`,
   * donde no hay shell, así que lleva su propio fondo a pantalla completa.
   */
  variant?: 'embedded' | 'fullscreen';
}

/**
 * MVP-406 (CA-3, `P-046`) — Pantalla de ruta desconocida. Antes, `App.tsx` mapeaba `/app/*` al Home y
 * el resto a `/`, así que un enlace roto o un error de tecleo renderizaba una pantalla válida y la
 * persona creía que había llegado a donde quería. Aquí se dice que la dirección no existe y se ofrece
 * una salida útil: al Home si hay sesión, a la landing si no.
 */
export const NotFoundView: React.FC<NotFoundViewProps> = ({ variant = 'embedded' }) => {
  const { isAuthenticated } = useAuth();
  const homeTo = isAuthenticated ? '/app' : '/';
  const homeLabel = isAuthenticated ? 'Volver al inicio' : 'Ir a la página principal';

  const card = (
    <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 text-center ambient-shadow space-y-4 max-w-md mx-auto">
      <div className="w-14 h-14 rounded-2xl bg-[#f0ede8] text-[#33450d] flex items-center justify-center mx-auto">
        <span className="material-symbols-outlined text-3xl" aria-hidden="true">explore_off</span>
      </div>
      <div className="space-y-1">
        <p className="text-xs font-bold uppercase tracking-wider text-[#a2a496]">Error 404</p>
        <h1 className="font-headline font-bold text-lg text-[#1c1c19]">Esta página no existe</h1>
        <p className="text-sm text-[#45483c]">
          La dirección a la que has llegado no corresponde a ninguna sección. Puede que el enlace esté
          roto o que la ruta haya cambiado.
        </p>
      </div>
      <Link
        to={homeTo}
        className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
      >
        <span className="material-symbols-outlined text-lg" aria-hidden="true">home</span>
        {homeLabel}
      </Link>
    </div>
  );

  if (variant === 'fullscreen') {
    return (
      <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex items-center justify-center p-6">
        {card}
      </div>
    );
  }

  return card;
};
