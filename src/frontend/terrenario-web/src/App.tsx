import { useState } from 'react';
import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { WorkspaceProvider, useWorkspace } from './contexts/WorkspaceContext';
import { ApiProvider } from './contexts/ApiContext';
import { SeasonProvider, useSeason } from './contexts/SeasonContext';
import { NotificationsProvider, useNotifications } from './contexts/NotificationsContext';
import { LandingPage } from './components/marketing/LandingPage';
import { LoginPage } from './components/auth/LoginPage';
import { OAuthCallback } from './components/auth/OAuthCallback';
import { CreateWorkspacePage } from './components/onboarding/CreateWorkspacePage';
import { SeasonSetupPage } from './components/onboarding/SeasonSetupPage';
import { AcceptInvitationPage } from './components/invitations/AcceptInvitationPage';
import { ReceivedInvitationsPage } from './components/invitations/ReceivedInvitationsPage';
import { InvitePeoplePage } from './components/workspace/InvitePeoplePage';
import { TerrenosView } from './components/plots/TerrenosView';
import { TemporadasView } from './components/seasons/TemporadasView';
import { TrabajadoresView } from './components/workers/TrabajadoresView';
import { TareasView } from './components/tasks/TareasView';
import { MiembrosView } from './components/members/MiembrosView';
import { AjustesView } from './components/settings/AjustesView';
import { ReactivationRequestPage } from './components/workspace/ReactivationRequestPage';
import { ReactivationInboxPage } from './components/workspace/ReactivationInboxPage';
import { HomeView } from './components/home/HomeView';
import { AppLayout } from './components/layout/AppLayout';
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

        {/* Aceptar invitación no exige Workspace previo: es la vía de entrada al primero (MVP-103) */}
        <Route path="/invitations/:token" element={<AcceptInvitationPage />} />

        {/* Vuelta de un Workspace dado de baja (MVP-206). Fuera de la guarda de Workspace a
            propósito: el Workspace de estas pantallas no resuelve contexto (CA-8) y puede ser el
            único que tuvieran las personas implicadas. */}
        <Route path="/reactivations" element={<ReactivationInboxPage />} />
        <Route path="/reactivations/:token" element={<ReactivationRequestPage />} />

        {/* Operativa: exige Workspace activo (MVP-102). El layout aporta cabecera y modal (MVP-107) */}
        <Route element={<RequireWorkspace />}>
          {/* Alta de un Workspace adicional desde la app (MVP-107): pantalla completa, sin cabecera */}
          <Route path="/app/workspaces/new" element={<CreateWorkspacePage mode="additional" />} />
          {/* Oferta de temporada (MVP-201): pantalla de creación, fuera de la guarda para no hacer bucle */}
          <Route path="/app/temporada/nueva" element={<SeasonSetupPage />} />
          <Route element={<AppLayout />}>
            {/* Maestros de administración (MVP-202/203/204/205): dentro del shell pero FUERA de la
                guarda de oferta, para que gestionar terrenos, temporadas, trabajadores, accesos y el
                catálogo de tareas sea siempre accesible aunque el Workspace no tenga temporada
                activa. Terrenos era el único que quedaba dentro de la guarda: corregido en MVP-207
                (CA-5), porque preparar la explotación no debe exigir crear antes una temporada (la
                temporada es un acto cancelable por decisión de producto de MVP-201). */}
            <Route path="/app/terrenos" element={<TerrenosView />} />
            <Route path="/app/temporadas" element={<TemporadasView />} />
            <Route path="/app/trabajadores" element={<TrabajadoresView />} />
            <Route path="/app/miembros" element={<MiembrosView />} />
            <Route path="/app/tareas" element={<TareasView />} />
            {/* Ciclo de vida del Workspace (MVP-206): renombrar y dar de baja no dependen de que
                haya temporada activa. */}
            <Route path="/app/ajustes" element={<AjustesView />} />
            {/* Resto de operativa: si el Workspace activo no tiene temporada, se ofrece crearla (cancelable) */}
            <Route element={<RequireSeasonOffer />}>
              <Route path="/app" element={<HomeView />} />
              <Route path="/app/invitations" element={<InvitePeoplePage />} />
              <Route path="/app/*" element={<HomeView />} />
            </Route>
          </Route>
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

function OnboardingRoute() {
  const { activeWorkspace, isLoading } = useWorkspace();
  const { receivedInvitations, isLoading: isLoadingInvitations } = useNotifications();
  // Salida secundaria explícita: forzar el asistente aunque haya invitaciones pendientes (MVP-107).
  const [createOwn, setCreateOwn] = useState(false);

  if (isLoading || isLoadingInvitations) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (activeWorkspace) return <Navigate to="/app" replace />;

  // Invitado sin Workspace: se prioriza la decisión de invitación, con enlace a crear el propio.
  if (receivedInvitations.length > 0 && !createOwn) {
    return <ReceivedInvitationsPage onCreateOwn={() => setCreateOwn(true)} />;
  }

  return <CreateWorkspacePage />;
}

/**
 * Guarda de oferta de temporada (MVP-201). Si el Workspace activo no tiene temporada y el usuario no
 * ha rechazado la oferta en esta sesión, redirige a la pantalla de creación (cancelable). Cubre por
 * igual el alta de un Workspace nuevo y la selección de uno existente sin temporada.
 */
function RequireSeasonOffer() {
  const { activeSeason, isLoading, offerDismissed } = useSeason();

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#fcf9f4] flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-[#33450d] border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (!activeSeason && !offerDismissed) return <Navigate to="/app/temporada/nueva" replace />;

  return <Outlet />;
}

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <WorkspaceProvider>
          <ApiProvider>
            <SeasonProvider>
              <NotificationsProvider>
                <AppRoutes />
              </NotificationsProvider>
            </SeasonProvider>
          </ApiProvider>
        </WorkspaceProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
