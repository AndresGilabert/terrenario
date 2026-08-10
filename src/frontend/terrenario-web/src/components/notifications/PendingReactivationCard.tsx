import React from 'react';
import { Link } from 'react-router';
import type { ReactivationRequest } from '../../types/workspace-lifecycle.types';
import { expiresLabel } from '../../lib/invitation-ui';

interface PendingReactivationCardProps {
  request: ReactivationRequest;
  /** Cierra la bandeja al navegar: el aviso ha cumplido su función en cuanto lleva a la decisión. */
  onNavigate?: () => void;
}

/**
 * MVP-808 (HU-2, CA-3) — Aviso de que alguien ha pedido reactivar un Workspace que esta cuenta dio
 * de baja (`RN-040`), con enlace a la pantalla donde se decide.
 *
 * Presentacional y **sin acciones propias**: autorizar o denegar tiene consecuencias irreversibles
 * —el Workspace vuelve y cambia de propietario— y esa decisión se toma en `/reactivations`, que es
 * donde se explica lo que implica. Una campanita no es sitio para eso. Lo que el aviso quita es la
 * dependencia de que llegue un correo, no el paso de leer antes de decidir.
 */
export const PendingReactivationCard: React.FC<PendingReactivationCardProps> = ({
  request,
  onNavigate,
}) => (
  <div className="rounded-xl border border-[#e5e2dd] bg-white p-4 space-y-3">
    <div className="space-y-1">
      <p className="font-bold text-[#1c1c19] leading-tight">{request.workspace.name}</p>
      <p className="text-sm text-[#45483c]">
        {request.requested_by.name} pide que le traspases esta explotación y se reactive.
      </p>
      <p className="text-xs text-[#76786b]">{expiresLabel(request.expires_at)}</p>
    </div>

    <Link
      to="/reactivations"
      onClick={onNavigate}
      className="inline-flex px-4 py-2 rounded-lg bg-[#33450d] hover:bg-[#4a5d23] text-white text-sm font-semibold transition-colors"
    >
      Ver y decidir
    </Link>
  </div>
);
