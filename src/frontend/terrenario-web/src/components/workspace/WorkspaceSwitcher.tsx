import React, { useEffect, useRef, useState } from 'react';
import { useWorkspace } from '../../contexts/WorkspaceContext';
import { WorkspaceServiceError } from '../../services/workspace.service';

/**
 * MVP-104 — Selector visible de Workspace activo (HU-1, HU-2).
 * Referencia visual: la insignia de Workspace de `prototype/terrenario-mvp/src/components/Sidebar.tsx`.
 * Muestra los Workspaces disponibles, distingue el activo y permite alternar sin ambigüedad.
 */
export const WorkspaceSwitcher: React.FC = () => {
  const { activeWorkspace, workspaces, switchWorkspace } = useWorkspace();
  const [isOpen, setIsOpen] = useState(false);
  const [switchingId, setSwitchingId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const handlePointerDown = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsOpen(false);
    };

    document.addEventListener('mousedown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen]);

  const handleSelect = async (workspaceId: string) => {
    if (workspaceId === activeWorkspace?.id) {
      setIsOpen(false);
      return;
    }

    setErrorMessage(null);
    setSwitchingId(workspaceId);

    try {
      await switchWorkspace(workspaceId);
      setIsOpen(false);
    } catch (error: unknown) {
      setErrorMessage(
        error instanceof WorkspaceServiceError
          ? error.message
          : 'No se pudo cambiar de Workspace. Inténtalo de nuevo.'
      );
    } finally {
      setSwitchingId(null);
    }
  };

  const activeName = activeWorkspace?.name ?? 'Sin Workspace';
  const hasAlternatives = workspaces.length > 1;

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        disabled={workspaces.length === 0}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-label={`Workspace activo: ${activeName}. ${
          hasAlternatives ? 'Pulsa para cambiar.' : ''
        }`}
        className="w-full bg-white rounded-xl p-3 border border-[#e5e2dd] shadow-xs flex items-center justify-between gap-2 hover:border-[#c6c8b8] transition-colors disabled:opacity-60 disabled:cursor-default"
      >
        <span className="flex items-center gap-2.5 min-w-0">
          <span aria-hidden="true" className="text-[#33450d]">🌿</span>
          <span className="min-w-0 text-left">
            <span className="block text-xs font-semibold text-[#1c1c19] truncate">{activeName}</span>
            <span className="block text-[11px] text-[#76786b]">
              {hasAlternatives ? `${workspaces.length} Workspaces` : 'Workspace activo'}
            </span>
          </span>
        </span>
        {hasAlternatives && (
          <span aria-hidden="true" className="text-[#76786b] text-sm shrink-0">
            {isOpen ? '▲' : '▼'}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute z-40 mt-1 w-full bg-white rounded-xl border border-[#e5e2dd] shadow-lg overflow-hidden">
          <ul role="listbox" aria-label="Selecciona un Workspace" className="max-h-72 overflow-y-auto py-1">
            {workspaces.map((workspace) => {
              const isActive = workspace.id === activeWorkspace?.id;
              const isSwitching = switchingId === workspace.id;
              return (
                <li key={workspace.id} role="option" aria-selected={isActive}>
                  <button
                    type="button"
                    onClick={() => void handleSelect(workspace.id)}
                    disabled={switchingId !== null}
                    className={`w-full flex items-center justify-between gap-2 px-3.5 py-2.5 text-left text-sm transition-colors disabled:cursor-wait ${
                      isActive
                        ? 'bg-[#f0ede8] font-semibold text-[#33450d]'
                        : 'text-[#45483c] hover:bg-[#f6f3ee]'
                    }`}
                  >
                    <span className="truncate">{workspace.name}</span>
                    {isActive && (
                      <span aria-label="Workspace activo" className="text-[#33450d] shrink-0">
                        ✓
                      </span>
                    )}
                    {isSwitching && (
                      <span aria-hidden="true" className="text-[#76786b] shrink-0 text-xs">
                        …
                      </span>
                    )}
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      )}

      {errorMessage && (
        <p role="alert" className="mt-1.5 text-xs text-red-700">
          {errorMessage}
        </p>
      )}
    </div>
  );
};
