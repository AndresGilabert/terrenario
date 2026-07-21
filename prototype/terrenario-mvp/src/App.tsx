import React, { useState } from 'react';
import {
  ViewMode,
  WorkspaceConfig,
  Terreno,
  DiarioEntry,
  HarvestRecord,
  Worker,
  PurchaseRecord
} from './types';
import {
  initialWorkspace,
  initialTerrenos,
  initialDiario,
  initialHarvests,
  initialWorkers,
  initialPurchases
} from './data/initialData';

import { Sidebar } from './components/Sidebar';
import { TopNavbar } from './components/TopNavbar';
import { LandingPage } from './components/LandingPage';
import { LoginPage } from './components/LoginPage';
import { OnboardingStep1 } from './components/OnboardingStep1';
import { OnboardingStep2 } from './components/OnboardingStep2';

import { DiarioView } from './components/DiarioView';
import { DashboardView } from './components/DashboardView';
import { TerrenosView } from './components/TerrenosView';
import { CosechasView } from './components/CosechasView';
import { TemporadasView } from './components/TemporadasView';
import { TrabajadoresView } from './components/TrabajadoresView';
import { ComprasView } from './components/ComprasView';
import { AjustesView } from './components/AjustesView';

import { ActivityModal } from './components/ActivityModal';
import { TerrenoModal } from './components/TerrenoModal';
import { CosechaModal } from './components/CosechaModal';
import { TerrenoDetailModal } from './components/TerrenoDetailModal';

export function App() {
  // Navigation & Workspace State
  const [currentView, setCurrentView] = useState<ViewMode>('diario');
  const [workspace, setWorkspace] = useState<WorkspaceConfig>(initialWorkspace);

  // Core Data Collections
  const [terrenos, setTerrenos] = useState<Terreno[]>(initialTerrenos);
  const [diario, setDiario] = useState<DiarioEntry[]>(initialDiario);
  const [harvests, setHarvests] = useState<HarvestRecord[]>(initialHarvests);
  const [workers, setWorkers] = useState<Worker[]>(initialWorkers);
  const [purchases, setPurchases] = useState<PurchaseRecord[]>(initialPurchases);

  // UI Modals & Drawers
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isActivityModalOpen, setIsActivityModalOpen] = useState(false);
  const [isTerrenoModalOpen, setIsTerrenoModalOpen] = useState(false);
  const [isCosechaModalOpen, setIsCosechaModalOpen] = useState(false);
  const [selectedTerrenoDetail, setSelectedTerrenoDetail] = useState<Terreno | null>(null);

  // Workspace Updates
  const handleUpdateWorkspace = (updated: Partial<WorkspaceConfig>) => {
    setWorkspace((prev) => ({ ...prev, ...updated }));
  };

  // Diario Actions
  const handleSaveActivity = (entry: Partial<DiarioEntry>) => {
    const newEntry: DiarioEntry = {
      id: Date.now().toString(),
      tipo: entry.tipo || 'actividad',
      titulo: entry.titulo || 'Nueva Labor',
      descripcion: entry.descripcion || '',
      fecha: entry.fecha || 'Hoy',
      fechaRaw: entry.fechaRaw || new Date().toISOString().split('T')[0],
      terrenoId: entry.terrenoId,
      terrenoNombre: entry.terrenoNombre,
      trabajador: entry.trabajador,
      duracionHoras: entry.duracionHoras,
      monto: entry.monto,
      cantidadKg: entry.cantidadKg,
      completado: true
    };
    setDiario([newEntry, ...diario]);
  };

  const handleToggleCompleteDiario = (id: string) => {
    setDiario(diario.map((d) => (d.id === id ? { ...d, completado: !d.completado } : d)));
  };

  const handleDeleteDiario = (id: string) => {
    setDiario(diario.filter((d) => d.id !== id));
  };

  // Terrenos Actions
  const handleSaveTerreno = (terreno: Partial<Terreno>) => {
    const newTerreno: Terreno = {
      id: `t-${Date.now()}`,
      nombre: terreno.nombre || 'Nueva Parcela',
      alias: terreno.alias || 'P-00',
      ubicacion: terreno.ubicacion || 'Sector Finca',
      olivosCount: terreno.olivosCount,
      superficieHa: terreno.superficieHa || 1.0,
      estado: terreno.olivosCount ? 'activo' : 'incompleto',
      alertaMsg: terreno.olivosCount ? undefined : 'Nº de árboles sin registrar',
      tipoRiego: terreno.tipoRiego || 'Secano',
      estadoPoda: terreno.estadoPoda || 'Pendiente',
      variedadPrincipal: terreno.variedadPrincipal || 'Picual',
      imagenUrl: 'https://images.unsplash.com/photo-1500382017468-9049fed747ef?w=600&auto=format&fit=crop&q=80'
    };
    setTerrenos([...terrenos, newTerreno]);
  };

  const handleUpdateTerreno = (updated: Terreno) => {
    setTerrenos(terrenos.map((t) => (t.id === updated.id ? updated : t)));
    setSelectedTerrenoDetail(updated);
  };

  // Harvest Actions
  const handleSaveHarvest = (harvest: Partial<HarvestRecord>) => {
    const newHarvest: HarvestRecord = {
      id: `h-${Date.now()}`,
      fecha: harvest.fecha || new Date().toLocaleDateString('es-ES'),
      terrenoId: harvest.terrenoId || terrenos[0]?.id || 't-1',
      terrenoNombre: harvest.terrenoNombre || terrenos[0]?.nombre || 'General',
      producto: harvest.producto || 'Aceituna Picual',
      kgs: harvest.kgs || 1000,
      rendimientoPct: harvest.rendimientoPct || 20.0,
      destino: harvest.destino || 'Almazara',
      almazaraNombre: harvest.almazaraNombre
    };
    setHarvests([newHarvest, ...harvests]);

    // Also insert automatic entry in Diario!
    handleSaveActivity({
      tipo: 'cosecha',
      titulo: `${newHarvest.producto} (${newHarvest.kgs} Kg)`,
      descripcion: `Cosecha registrada con ${newHarvest.rendimientoPct}% rendimiento graso. Destino: ${newHarvest.destino}`,
      terrenoId: newHarvest.terrenoId,
      terrenoNombre: newHarvest.terrenoNombre,
      cantidadKg: newHarvest.kgs
    });
  };

  const handleDeleteHarvest = (id: string) => {
    setHarvests(harvests.filter((h) => h.id !== id));
  };

  // Worker Actions
  const handleAddWorker = (worker: Worker) => {
    setWorkers([...workers, worker]);
  };

  const handleToggleWorkerStatus = (id: string) => {
    setWorkers(workers.map((w) => (w.id === id ? { ...w, activo: !w.activo } : w)));
  };

  // Purchase Actions
  const handleAddPurchase = (purchase: PurchaseRecord) => {
    setPurchases([purchase, ...purchases]);
    // Also record in Diario
    handleSaveActivity({
      tipo: 'compra',
      titulo: purchase.concepto,
      descripcion: `Compra a ${purchase.proveedor} (${purchase.categoria})`,
      monto: purchase.monto
    });
  };

  // Check if current view is a full-page screen without shell layout
  const isFullPage = ['landing', 'login', 'onboarding_step1', 'onboarding_step2'].includes(currentView);

  if (isFullPage) {
    switch (currentView) {
      case 'landing':
        return <LandingPage onNavigate={setCurrentView} />;
      case 'login':
        return (
          <LoginPage
            onLoginSuccess={() => setCurrentView('diario')}
            onNavigate={setCurrentView}
          />
        );
      case 'onboarding_step1':
        return (
          <OnboardingStep1
            workspace={workspace}
            onUpdateWorkspace={handleUpdateWorkspace}
            onNavigate={setCurrentView}
          />
        );
      case 'onboarding_step2':
        return (
          <OnboardingStep2
            workspace={workspace}
            onUpdateWorkspace={handleUpdateWorkspace}
            onNavigate={setCurrentView}
          />
        );
      default:
        return <LandingPage onNavigate={setCurrentView} />;
    }
  }

  // Main App Shell (Sidebar + Topbar + Content)
  return (
    <div className="min-h-screen bg-[#fcf9f4] text-[#1c1c19] flex flex-col md:flex-row antialiased font-sans">
      {/* Sidebar */}
      <Sidebar
        currentView={currentView}
        onSelectView={setCurrentView}
        workspace={workspace}
        isOpenMobile={isMobileMenuOpen}
        onCloseMobile={() => setIsMobileMenuOpen(false)}
      />

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0 min-h-screen">
        <TopNavbar
          currentView={currentView}
          workspace={workspace}
          onOpenMobileMenu={() => setIsMobileMenuOpen(true)}
          onOpenNewActivityModal={() => setIsActivityModalOpen(true)}
          onSelectView={setCurrentView}
        />

        <main className="flex-1 p-4 sm:p-6 md:p-8 overflow-y-auto">
          {currentView === 'diario' && (
            <DiarioView
              entries={diario}
              terrenos={terrenos}
              workers={workers}
              onOpenNewModal={() => setIsActivityModalOpen(true)}
              onToggleComplete={handleToggleCompleteDiario}
              onDeleteEntry={handleDeleteDiario}
            />
          )}

          {currentView === 'dashboard' && (
            <DashboardView terrenos={terrenos} harvests={harvests} />
          )}

          {currentView === 'terrenos' && (
            <TerrenosView
              terrenos={terrenos}
              onOpenAddModal={() => setIsTerrenoModalOpen(true)}
              onSelectTerreno={setSelectedTerrenoDetail}
            />
          )}

          {currentView === 'cosechas' && (
            <CosechasView
              harvests={harvests}
              terrenos={terrenos}
              onOpenNewHarvestModal={() => setIsCosechaModalOpen(true)}
              onDeleteHarvest={handleDeleteHarvest}
            />
          )}

          {currentView === 'temporadas' && (
            <TemporadasView
              workspace={workspace}
              onSelectActiveSeason={(season) => handleUpdateWorkspace({ temporadaActiva: season })}
            />
          )}

          {currentView === 'trabajadores' && (
            <TrabajadoresView
              workers={workers}
              onAddWorker={handleAddWorker}
              onToggleWorkerStatus={handleToggleWorkerStatus}
            />
          )}

          {currentView === 'compras' && (
            <ComprasView purchases={purchases} onAddPurchase={handleAddPurchase} />
          )}

          {currentView === 'ajustes' && (
            <AjustesView
              workspace={workspace}
              onUpdateWorkspace={handleUpdateWorkspace}
              onNavigate={setCurrentView}
            />
          )}
        </main>
      </div>

      {/* Global Modals */}
      <ActivityModal
        isOpen={isActivityModalOpen}
        onClose={() => setIsActivityModalOpen(false)}
        terrenos={terrenos}
        workers={workers}
        onSave={handleSaveActivity}
      />

      <TerrenoModal
        isOpen={isTerrenoModalOpen}
        onClose={() => setIsTerrenoModalOpen(false)}
        onSave={handleSaveTerreno}
      />

      <CosechaModal
        isOpen={isCosechaModalOpen}
        onClose={() => setIsCosechaModalOpen(false)}
        terrenos={terrenos}
        onSave={handleSaveHarvest}
      />

      <TerrenoDetailModal
        terreno={selectedTerrenoDetail}
        diario={diario}
        harvests={harvests}
        onClose={() => setSelectedTerrenoDetail(null)}
        onUpdateTerreno={handleUpdateTerreno}
      />
    </div>
  );
}

export default App;
