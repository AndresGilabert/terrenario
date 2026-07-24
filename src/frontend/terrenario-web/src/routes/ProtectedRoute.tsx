import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { rememberPostLoginRedirect } from '../lib/post-login-redirect';

export const ProtectedRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (isAuthenticated) return <Outlet />;

  rememberPostLoginRedirect(`${location.pathname}${location.search}`);

  return <Navigate to="/login" replace />;
};
