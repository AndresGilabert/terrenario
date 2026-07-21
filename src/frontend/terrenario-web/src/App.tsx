import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { LandingPage } from './components/marketing/LandingPage';
import { LoginPage } from './components/auth/LoginPage';
import { OAuthCallback } from './components/auth/OAuthCallback';
import { ProtectedRoute } from './routes/ProtectedRoute';

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
        <Route path="/app" element={<AppHome />} />
        <Route path="/app/*" element={<AppHome />} />
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function AppHome() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-[#fcf9f4] flex flex-col items-center justify-center p-8 text-center gap-6">
      <div className="w-16 h-16 rounded-2xl bg-[#33450d] text-white flex items-center justify-center">
        <span className="text-3xl">🌿</span>
      </div>
      <h1 className="text-2xl font-bold text-[#1c1c19]">
        ¡Bienvenido, {user?.displayName ?? 'usuario'}!
      </h1>
      <p className="text-[#45483c] text-sm max-w-xs">
        Has iniciado sesión correctamente. Las funcionalidades de gestión llegarán muy pronto.
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
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
