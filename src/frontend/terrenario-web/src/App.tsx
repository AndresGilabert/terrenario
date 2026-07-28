import { useState } from 'react';
import { BrowserRouter, Navigate, Outlet, Route, Routes, useNavigate } from 'react-router-dom';
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
            {/* Maestros de administración (MVP-203/204/205): dentro del shell pero FUERA de la
                guarda de oferta, para que gestionar temporadas, trabajadores, accesos y el catálogo
                de tareas sea siempre accesible aunque el Workspace no tenga temporada activa. */}
            <Route path="/app/temporadas" element={<TemporadasView />} />
            <Route path="/app/trabajadores" element={<TrabajadoresView />} />
            <Route path="/app/miembros" element={<MiembrosView />} />
            <Route path="/app/tareas" element={<TareasView />} />
            {/* Ciclo de vida del Workspace (MVP-206): renombrar y dar de baja no dependen de que
                haya temporada activa. */}
            <Route path="/app/ajustes" element={<AjustesView />} />
            {/* Resto de operativa: si el Workspace activo no tiene temporada, se ofrece crearla (cancelable) */}
            <Route element={<RequireSeasonOffer />}>
              <Route path="/app" element={<AppHome />} />
              <Route path="/app/terrenos" element={<TerrenosView />} />
              <Route path="/app/invitations" element={<InvitePeoplePage />} />
              <Route path="/app/*" element={<AppHome />} />
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

function AppHome() {
  const { user } = useAuth();
  const { activeWorkspace } = useWorkspace();
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      {/* El selector, la campanita, la navegación y el cierre de sesión viven en el shell (MVP-107). */}
      <div className="bg-white rounded-2xl border border-[#e5e2dd] p-8 ambient-shadow space-y-4">
        <div className="w-14 h-14 rounded-2xl bg-[#33450d] text-white flex items-center justify-center">
          <span className="material-symbols-outlined fill text-3xl" aria-hidden="true">eco</span>
        </div>
        <div className="space-y-1">
          <h1 className="font-headline font-bold text-2xl text-[#1c1c19]">
            ¡Bienvenido, {user?.displayName ?? 'usuario'}!
          </h1>
          {activeWorkspace && (
            <p className="text-sm font-semibold text-[#33450d]">
              Estás trabajando en «{activeWorkspace.name}».
            </p>
          )}
        </div>
        <p className="text-[#45483c] text-sm max-w-lg">
          Tu espacio de trabajo está listo. Los módulos de gestión (diario, terrenos, cosechas…)
          aparecerán en el menú lateral a medida que se vayan habilitando.
        </p>
        <div className="flex flex-col sm:flex-row items-start gap-3 pt-1">
          <button
            onClick={() => navigate('/app/invitations')}
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
          >
            <span className="material-symbols-outlined text-lg" aria-hidden="true">person_add</span>
            Invitar a alguien
          </button>
        </div>
      </div>
    </div>
  );
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
