import React, { Fragment } from 'react';
import { Navigate, Outlet } from 'react-router';
import { useDataScope } from '../contexts/DataScopeContext';
import { useWorkspace } from '../contexts/WorkspaceContext';

/**
 * Bloquea el área operativa hasta que la sesión tenga un Workspace activo (MVP-102).
 *
 * MVP-701 — Es además **el único punto** donde se invalida lo que las vistas tienen cargado: el
 * subárbol entero se remonta cuando cambia el contexto activo (Workspace o temporada de trabajo). Va
 * aquí y no en `AppLayout` porque también cuelgan de esta guarda pantallas que están fuera del shell
 * —el alta de Workspace adicional y la oferta de temporada—, y porque una vista nueva debe heredar el
 * comportamiento sin tener que acordarse de `P-081`.
 */
export const RequireWorkspace: React.FC = () => {
  const { activeWorkspace, isLoading } = useWorkspace();
  const { scopeVersion } = useDataScope();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!activeWorkspace) return <Navigate to="/onboarding" replace />;

  // La clave lleva también el identificador del Workspace: `scopeVersion` cubre los cambios que pasan
  // por el contexto, y el identificador cualquier otra vía por la que el activo acabe siendo otro.
  return (
    <Fragment key={`${activeWorkspace.id}:${scopeVersion}`}>
      <Outlet />
    </Fragment>
  );
};
