import React, { Fragment, useEffect, useRef } from 'react';
import { Navigate, Outlet, useSearchParams } from 'react-router';
import { useDataScope } from '../contexts/DataScopeContext';
import { useWorkspace } from '../contexts/WorkspaceContext';

/**
 * MVP-801 — Parámetros que nombran **entidades del Workspace**: al cambiar de Workspace dejan de
 * significar nada. El resto de la navegación (tipo, búsqueda, página) no se toca: no está atada a
 * ninguna ficha y borrarla sería tirar trabajo del usuario sin motivo.
 */
const SCOPE_PARAMS = ['season_id', 'plot_id', 'plot_ids'] as const;

/**
 * Bloquea el área operativa hasta que la sesión tenga un Workspace activo (MVP-102).
 *
 * MVP-701 — Es además **el único punto** donde se invalida lo que las vistas tienen cargado: el
 * subárbol entero se remonta cuando cambia el contexto activo (Workspace o temporada de trabajo). Va
 * aquí y no en `AppLayout` porque también cuelgan de esta guarda pantallas que están fuera del shell
 * —el alta de Workspace adicional y la oferta de temporada—, y porque una vista nueva debe heredar el
 * comportamiento sin tener que acordarse de `P-081`.
 *
 * MVP-801 (`P-107`) — Y por el mismo motivo es donde se **limpia el ámbito de la URL** al cambiar de
 * Workspace. `switchWorkspace` reemitía la sesión y remontaba el área operativa, pero la dirección
 * seguía pidiendo la campaña y los terrenos del Workspace anterior. No sustituye a la caída al defecto
 * del servidor —un enlace compartido o un marcador reproducen el escenario sin pasar por el selector—:
 * evita que el caso llegue siquiera a producirse por el camino más frecuente.
 */
export const RequireWorkspace: React.FC = () => {
  const { activeWorkspace, isLoading } = useWorkspace();
  const { scopeVersion } = useDataScope();
  const [searchParams, setSearchParams] = useSearchParams();

  const workspaceId = activeWorkspace?.id ?? null;
  // El Workspace del render anterior. La **primera** resolución (de «no hay» a «este») no es un cambio
  // de contexto: limpiaría la URL de quien acaba de abrir un enlace compartido.
  const previousWorkspaceId = useRef<string | null>(null);

  useEffect(() => {
    const previous = previousWorkspaceId.current;
    previousWorkspaceId.current = workspaceId;
    if (previous === null || workspaceId === null || previous === workspaceId) return;

    if (!SCOPE_PARAMS.some((param) => searchParams.has(param))) return;

    const next = new URLSearchParams(searchParams);
    SCOPE_PARAMS.forEach((param) => next.delete(param));
    // Sustituye la entrada: «atrás» no debe devolver a la dirección con el ámbito del otro Workspace.
    setSearchParams(next, { replace: true });
  }, [workspaceId, searchParams, setSearchParams]);

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
