/**
 * Terreno (parcela) del Workspace (MVP-202). Alta mínima: solo `name` y `ownership_type` son
 * obligatorios (RN-028); el resto es opcional e informativo. La ausencia de `tree_count` no bloquea
 * nada aquí; se marca como dato incompleto para el dashboard (RN-010).
 */
export interface Plot {
  id: string;
  workspace_id: string;
  name: string;
  ownership_type: PlotOwnershipType;
  alias: string | null;
  owner_name: string | null;
  cadastral_reference: string | null;
  location: string | null;
  tree_count: number | null;
  is_active: boolean;
  /** `true` si el terreno tiene número de árboles registrado (RN-010/RN-028). */
  has_tree_count: boolean;
  /**
   * MVP-806 (CA-2) — Cuántos registros la referencian. `null` significa **«no consultado»**, no
   * «ninguno»: solo lo trae el listado. Es lo que decide si se ofrece «Eliminar».
   */
  usage_count: number | null;
}

/** Catálogo cerrado `plot_ownership_type` (MVP-202). */
export type PlotOwnershipType = 'propia' | 'cedida';

export const PLOT_OWNERSHIP_LABELS: Record<PlotOwnershipType, string> = {
  propia: 'Propia',
  cedida: 'Cedida',
};

/** Alta de terreno. Solo `name` y `ownership_type` son obligatorios (RN-028). */
export interface CreatePlotPayload {
  name: string;
  ownership_type: PlotOwnershipType;
  alias?: string | null;
  owner_name?: string | null;
  cadastral_reference?: string | null;
  location?: string | null;
  tree_count?: number | null;
}

/** Edición de terreno. Lleva el conjunto editable; `is_active` opcional (inactivación CA-3). */
export interface UpdatePlotPayload extends CreatePlotPayload {
  is_active?: boolean;
}

export interface PlotListResponse {
  data: Plot[];
  meta: { total: number };
}
