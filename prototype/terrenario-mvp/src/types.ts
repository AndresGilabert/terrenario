export type ViewMode = 
  | 'landing'
  | 'login'
  | 'onboarding_step1'
  | 'onboarding_step2'
  | 'diario'
  | 'dashboard'
  | 'terrenos'
  | 'cosechas'
  | 'temporadas'
  | 'trabajadores'
  | 'compras'
  | 'ajustes';

export interface Terreno {
  id: string;
  nombre: string;
  alias: string;
  ubicacion: string;
  olivosCount?: number;
  estado: 'activo' | 'incompleto' | 'inactivo';
  alertaMsg?: string;
  tipoRiego: string;
  estadoPoda: string;
  imagenUrl: string;
  superficieHa?: number;
  variedadPrincipal?: string;
}

export interface DiarioEntry {
  id: string;
  tipo: 'actividad' | 'compra' | 'cosecha' | 'riego' | 'alerta';
  titulo: string;
  descripcion: string;
  fecha: string; // ISO date string or formatted date
  fechaRaw: string;
  hora?: string;
  terrenoId?: string;
  terrenoNombre?: string;
  trabajador?: string;
  duracionHoras?: number;
  monto?: number;
  proveedor?: string;
  cantidadKg?: number;
  rendimientoPct?: number;
  fotos?: string[];
  completado?: boolean;
}

export interface HarvestRecord {
  id: string;
  fecha: string;
  terrenoId: string;
  terrenoNombre: string;
  producto: string;
  kgs: number;
  rendimientoPct: number;
  destino: 'Almazara' | 'Consumo Propio' | 'Sin destino' | string;
  almazaraNombre?: string;
}

export interface Worker {
  id: string;
  nombre: string;
  rol: string;
  telefono: string;
  activo: boolean;
  avatarUrl: string;
}

export interface PurchaseRecord {
  id: string;
  fecha: string;
  concepto: string;
  categoria: string;
  proveedor: string;
  monto: number;
  cantidad: string;
}

export interface WorkspaceConfig {
  nombreWorkspace: string;
  temporadaActiva: string;
  fechaInicioTemporada: string;
  fechaFinEstimada: string;
  userEmail: string;
  userName: string;
  userAvatar: string;
}
