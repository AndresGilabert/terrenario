import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useWorkspace } from '../contexts/WorkspaceContext';

/**
 * Bloquea el área operativa hasta que la sesión tenga un Workspace activo (MVP-102).
 */
export const RequireWorkspace: React.FC = () => {
  const { activeWorkspace, isLoading } = useWorkspace();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return activeWorkspace ? <Outlet /> : <Navigate to="/onboarding" replace />;
};
