import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { WorkspaceProvider, useWorkspace } from './contexts/WorkspaceContext';
import { LandingPage } from './components/marketing/LandingPage';
import { LoginPage } from './components/auth/LoginPage';
import { OAuthCallback } from './components/auth/OAuthCallback';
import { CreateWorkspacePage } from './components/onboarding/CreateWorkspacePage';
import { ProtectedRoute } from './routes/ProtectedRoute';
import { RequireWorkspace } from './routes/RequireWorkspace';

function AppRoutes() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/app" replace /> : <LoginPage />}
      />
      <Route path="/auth/callback" element={<OAuthCallback />} />

      {/* Protected area */}
      <Route element={<ProtectedRoute />}>
        <Route path="/onboarding" element={<OnboardingRoute />} />

        {/* Operativa: exige Workspace activo (MVP-102) */}
        <Route element={<RequireWorkspace />}>
          <Route path="/app" element={<AppHome />} />
          <Route path="/app/*" element={<AppHome />} />
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function OnboardingRoute() {
  const { activeWorkspace, isLoading } = useWorkspace();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  return activeWorkspace ? <Navigate to="/app" replace /> : <CreateWorkspacePage />;
}

function AppHome() {
  const { user, logout } = useAuth();
  const { activeWorkspace } = useWorkspace();

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-8 text-center gap-6">
      <div className="w-16 h-16 rounded-2xl bg-[#33450d] text-white flex items-center justify-center">
        <span className="text-3xl">🌿</span>
      </div>
      <div className="space-y-2">
        <h1 className="text-2xl font-bold text-[#1c1c19]">
          ¡Bienvenido, {user?.displayName ?? 'usuario'}!
        </h1>
        {activeWorkspace && (
          <p className="text-sm font-semibold text-[#33450d]">
            Workspace activo: {activeWorkspace.nombre}
          </p>
        )}
      </div>
      <p className="text-[#45483c] text-sm max-w-xs">
        Tu espacio de trabajo está listo. Las funcionalidades de gestión llegarán muy pronto.
      </p>
      <button
        onClick={logout}
        className="px-4 py-2 rounded-xl border border-[#c6c8b8] text-[#1c1c19] text-sm font-semibold hover:bg-[#f0ede8] transition-colors"
      >
        Cerrar sesión
      </button>
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <WorkspaceProvider>
          <AppRoutes />
        </WorkspaceProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
