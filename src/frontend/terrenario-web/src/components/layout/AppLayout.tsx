import React, { useEffect, useState } from 'react';
import { Outlet, useLocation } from 'react-router';
import { AppSidebar } from './AppSidebar';
import { AppTopbar } from './AppTopbar';
import { OfflineBanner } from './OfflineBanner';
import { InvitationModal } from '../notifications/InvitationModal';
import { useUsageTelemetry } from '../../lib/use-usage-telemetry';
import { UsageEvent, UsageMark, markOnceInSession } from '../../lib/usage-telemetry';
import { recordVisitedPath } from '../../lib/report-context';

/**
 * MVP-702 (`P-086`) — Ancho útil del contenido, **por tipo de contenido**.
 *
 * Hasta aquí todas las secciones compartían un único `max-w-3xl` (768 px). El contenedor único se
 * introdujo en `P-016` para dar coherencia de tamaño y espaciado entre secciones, y **ese objetivo
 * sigue siendo correcto**: lo que había que revisar era la cota, no retirarla. A 1920 px, con el
 * lateral de ~256 px, el contenido ocupaba 768 px y el resto era fondo.
 *
 * Por eso son dos cotas y no ninguna: la coherencia se mantiene **dentro de cada tipo** (CA-3), que es
 * la invariante que `P-016` protegía.
 *
 * - `ancho`: listados y panel. Su contenido son tablas y rejillas, que ganan con el sitio.
 * - `estrecho`: formularios y pantallas de lectura. Aquí ensanchar **empeora**: un campo de texto de
 *   1.000 px o un párrafo de 200 caracteres por línea se leen peor (CA-2).
 *
 * El mapa vive aquí y no en cada vista **a propósito**: si cada pantalla eligiera su ancho, la
 * coherencia duraría hasta la siguiente que se añadiera. Es el mismo criterio que `titleForPath`.
 */
const CONTENIDO_ANCHO = 'max-w-7xl';
const CONTENIDO_ESTRECHO = 'max-w-3xl';

/** Rutas cuyo contenido es de lectura o formulario. El resto son listados o panel. */
const RUTAS_ESTRECHAS = ['/app/ajustes', '/app/invitations', '/app/feedback'];

function anchoParaRuta(pathname: string): string {
  // El Home es checklist de preparación: texto y una lista de pasos, no una tabla.
  if (pathname === '/app' || pathname === '/app/') return CONTENIDO_ESTRECHO;
  return RUTAS_ESTRECHAS.some((ruta) => pathname.startsWith(ruta))
    ? CONTENIDO_ESTRECHO
    : CONTENIDO_ANCHO;
}

/** Título contextual de la cabecera según la ruta activa. */
function titleForPath(pathname: string): string {
  if (pathname.startsWith('/app/diario')) return 'Diario de campo';
  if (pathname.startsWith('/app/vision-general')) return 'Visión General';
  if (pathname.startsWith('/app/cosechas')) return 'Cosechas';
  if (pathname.startsWith('/app/compras')) return 'Compras e insumos';
  if (pathname.startsWith('/app/invitations')) return 'Invitar a alguien';
  if (pathname.startsWith('/app/temporadas')) return 'Temporadas';
  if (pathname.startsWith('/app/terrenos')) return 'Terrenos';
  if (pathname.startsWith('/app/trabajadores')) return 'Trabajadores';
  if (pathname.startsWith('/app/miembros')) return 'Miembros y accesos';
  if (pathname.startsWith('/app/tareas')) return 'Catálogo de tareas';
  if (pathname.startsWith('/app/ajustes')) return 'Ajustes del Workspace';
  if (pathname.startsWith('/app/feedback')) return 'Sugerencias e incidencias';
  // Solo el Home y las rutas desconocidas llegan aquí (las conocidas retornan antes): el shell aloja
  // la pantalla 404 de MVP-406, así que la cabecera lo dice en vez de rotular «Inicio» algo que no lo es.
  if (pathname === '/app' || pathname === '/app/') return 'Inicio';
  return 'Página no encontrada';
}

/**
 * Shell del área operativa (estructura del prototipo): barra lateral + cabecera superior + contenido,
 * con drawer en móvil. Aloja además el modal de invitación no bloqueante (MVP-107).
 */
export const AppLayout: React.FC = () => {
  const location = useLocation();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const logUsage = useUsageTelemetry();

  // MVP-602 · MVP-703 — «Sesión activa»: la que llega al **área operativa**. Es el divisor del KPI de
  // uso del dashboard, así que se cuenta aquí y no en la propia pantalla del panel: contarlo allí haría
  // que el porcentaje fuese siempre 100 %, porque solo entrarían en el divisor las sesiones que ya lo
  // han abierto. Una vez por sesión, no por navegación entre pantallas.
  //
  // MVP-703 (CA-4) — La definición se fija aquí y en la KB con las mismas palabras. Este componente
  // cuelga de `RequireWorkspace`, así que una sesión que se queda en el onboarding **no** emite la
  // señal: es la definición, no un olvido. Emitirla también allí se descartó a propósito —metería en
  // el divisor sesiones en las que el panel todavía no existe—.
  useEffect(() => {
    if (markOnceInSession(UsageMark.AppSession)) logUsage(UsageEvent.AppSessionStarted);
  }, [logUsage]);

  // MVP-711 — Dónde estaba quien reporta. Se anota en el shell y no en cada vista porque es una
  // propiedad de la navegación, no de ninguna pantalla concreta: quien se topa con un fallo va al
  // canal desde donde le pasó, y sin este rastro el reporte diría «estaba en el formulario de
  // sugerencias», que es lo único que no interesa saber. Solo la ruta, nunca la query: los filtros
  // del panel llevan identificadores de terreno (`getReportContext`).
  useEffect(() => {
    recordVisitedPath(location.pathname);
  }, [location.pathname]);

  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col md:flex-row">
      {/* Barra lateral fija en escritorio */}
      <div className="hidden md:block h-screen sticky top-0 z-30">
        <AppSidebar />
      </div>

      {/* Drawer en móvil */}
      {isMobileMenuOpen && (
        <div className="fixed inset-0 z-50 md:hidden flex">
          <div
            className="fixed inset-0 bg-black/40 backdrop-blur-xs"
            onClick={() => setIsMobileMenuOpen(false)}
            aria-hidden="true"
          />
          <div className="relative z-10 h-full max-w-xs w-full shadow-2xl">
            <AppSidebar onNavigate={() => setIsMobileMenuOpen(false)} />
          </div>
        </div>
      )}

      {/* Contenido */}
      <div className="flex-1 flex flex-col min-w-0 min-h-screen">
        <AppTopbar
          title={titleForPath(location.pathname)}
          onOpenMobileMenu={() => setIsMobileMenuOpen(true)}
        />
        {/* Bajo la cabecera y **fuera** del área desplazable: si fuera dentro, bastaría con bajar por
            la lista para dejar de ver que no hay conexión (MVP-709). */}
        <OfflineBanner />
        {/* Contenedor de contenido: mismo padding para todas las secciones y **dos** anchuras según el
            tipo de contenido (MVP-702). Las de un mismo tipo siguen compartiendo medida y espaciado,
            que es la coherencia que buscaba `P-016`.

            `@container` marca este bloque como contenedor de consulta: desde aquí las rejillas pueden
            reaccionar al **ancho real disponible** en vez de al del viewport, que es lo que apretujaba
            las tarjetas del panel en 768 px con la pantalla a 1920. */}
        <main className="@container flex-1 overflow-y-auto p-4 sm:p-6 md:p-8">
          <div className={`${anchoParaRuta(location.pathname)} mx-auto`}>
            <Outlet />
          </div>
        </main>
      </div>

      <InvitationModal />
    </div>
  );
};
