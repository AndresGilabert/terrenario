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
import { DiarioView } from './components/diary/DiarioView';
import { CosechasView } from './components/harvests/CosechasView';
import { VisionGeneralView } from './components/dashboard/VisionGeneralView';
import { ComprasView } from './components/purchases/ComprasView';
import { TerrenosView } from './components/plots/TerrenosView';
import { TemporadasView } from './components/seasons/TemporadasView';
import { TrabajadoresView } from './components/workers/TrabajadoresView';
import { TareasView } from './components/tasks/TareasView';
import { MiembrosView } from './components/members/MiembrosView';
import { AjustesView } from './components/settings/AjustesView';
import { ReactivationRequestPage } from './components/workspace/ReactivationRequestPage';
import { ReactivationInboxPage } from './components/workspace/ReactivationInboxPage';
import { HomeView } from './components/home/HomeView';
import { NotFoundView } from './components/errors/NotFoundView';
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
            {/* Administración (MVP-202/203/204/205/206): dentro del shell pero FUERA de la guarda de
                oferta, para que preparar la explotación —terrenos, temporadas, trabajadores, tareas,
                personas y ajustes— sea siempre accesible aunque el Workspace no tenga temporada
                activa. Preparar no debe exigir crear antes una temporada: la temporada es un acto
                cancelable por decisión de producto de MVP-201.
                Terrenos era el único maestro que quedaba dentro: corregido en MVP-207 (CA-5). Invitar
                también estaba dentro (MVP-999, P-038) y producía el mismo desvío al pulsar «Invitar
                persona» desde Miembros, que sí estaba fuera; se corrige en la misma pasada. */}
            {/* Diario de campo (MVP-301). Va **fuera** de la guarda de oferta de temporada, como el
                resto de secciones del shell: quien entra al diario sin temporada activa recibe el
                aviso de qué le falta y un enlace al maestro, en vez de un desvío que no explica nada
                (misma lección que P-038 en MVP-207). */}
            <Route path="/app/diario" element={<DiarioView />} />
            {/* Libro de compras (MVP-303). Fuera de la guarda de oferta de temporada como el resto
                del shell: si falta temporada se dice y se enlaza, en vez de desviar sin explicar. */}
            <Route path="/app/compras" element={<ComprasView />} />
            {/* Registro de cosechas (MVP-401). Fuera de la guarda de oferta de temporada como el
                resto del shell: si falta temporada se dice y se enlaza, en vez de desviar sin
                explicar (misma lección que P-038 en MVP-207). */}
            <Route path="/app/cosechas" element={<CosechasView />} />
            {/* Visión General (MVP-403): una sola pantalla con scroll vertical (RN-005). Fuera de la
                guarda de oferta de temporada como el resto del shell: si falta temporada la propia
                pantalla lo explica y enlaza al maestro, en vez de desviar sin decir nada.
                Si el Home pasa a ser esta vista lo decide `P-040` en MVP-499. */}
            <Route path="/app/vision-general" element={<VisionGeneralView />} />
            <Route path="/app/terrenos" element={<TerrenosView />} />
            <Route path="/app/temporadas" element={<TemporadasView />} />
            <Route path="/app/trabajadores" element={<TrabajadoresView />} />
            <Route path="/app/miembros" element={<MiembrosView />} />
            <Route path="/app/tareas" element={<TareasView />} />
            <Route path="/app/invitations" element={<InvitePeoplePage />} />
            {/* Ciclo de vida del Workspace (MVP-206): renombrar y dar de baja no dependen de que
                haya temporada activa. */}
            <Route path="/app/ajustes" element={<AjustesView />} />
            {/* Arranque de la aplicación: si el Workspace activo no tiene temporada, se ofrece
                crearla (cancelable). Es el único destino que sigue tras la guarda, que es donde
                MVP-201 la quería: al entrar, no al administrar. */}
            <Route element={<RequireSeasonOffer />}>
              <Route path="/app" element={<HomeView />} />
            </Route>
            {/* Ruta desconocida bajo /app (MVP-406, P-046): 404 **dentro del shell** —conserva el
                lateral para no desorientar— y **fuera** de la guarda de oferta de temporada: un enlace
                roto no debe forzar a crear una campaña. Antes caía en el Home y parecía válida. */}
            <Route path="/app/*" element={<NotFoundView />} />
          </Route>
        </Route>
      </Route>

      {/* Ruta desconocida fuera de /app (MVP-406, P-046): 404 a pantalla completa con salida al Home
          o a la landing según haya sesión, en vez de redirigir en silencio a `/`. */}
      <Route path="*" element={<NotFoundView variant="fullscreen" />} />
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
