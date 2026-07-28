import { Terreno, DiarioEntry, HarvestRecord, Worker, PurchaseRecord, WorkspaceConfig } from '../types';

export const initialWorkspace: WorkspaceConfig = {
  nombreWorkspace: 'Finca El Olivar',
  temporadaActiva: 'Campaña 2024',
  fechaInicioTemporada: '2024-01-15',
  fechaFinEstimada: '2024-12-30',
  userEmail: 'andresgilabertsanchez@gmail.com',
  userName: 'Juan Pérez',
  userAvatar: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAXAO-74S9-2IPDCvPZqJFAtxDV5Bn0xzBbLjzqD5KG5GgKmKJ77c9cCMttqK45autdTP1NXkrAWiD_YD-e5ro3cbGckg8xZz01WdfK1IvgCixvfZfYn7hvEUY_-L2qVKEspE1p1C2bpMI0sSdVT1DJrvOWdb1UFdkJQb6taU0SY6iydmwUsUJWQs9j-6lpqDu0zlO7Ik98VdLMCYGjH9qCZMFuG33knY-kkxev306Ul8HHSx19RSj6wA'
};

export const initialTerrenos: Terreno[] = [
  {
    id: 't-1',
    nombre: 'Loma del Sol',
    alias: 'LS-01',
    ubicacion: 'Sector Norte, Finca Principal',
    olivosCount: 1200,
    estado: 'activo',
    tipoRiego: 'Riego óptimo',
    estadoPoda: 'Poda al día',
    superficieHa: 4.5,
    variedadPrincipal: 'Picual',
    imagenUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDufJk1MAow65aHUCkakusw9wk2JRr6qKBgiljFz0IYsfRfg2u1abi9ffMjQH5cCKaMsT50xLM-FNPscY72MqGB54ggc7tmCcasjHtpkYTUWPFBDPYIxtmJdN02uVZLIYnN3HSGqfhSxrgAeJhblZAEBIru5qQnGfI1Qae0ub97H-Ck1pz0LlhVSoA_ShnBDmb_E3LloVEb83mBZ-fxQjgpwyTypDxRBg9BdJ-OVmxdA4vGX3T9zmtPwA'
  },
  {
    id: 't-2',
    nombre: 'Valle Bajo',
    alias: 'VB-02',
    ubicacion: 'Sector Sur, Ribera',
    olivosCount: 800,
    estado: 'activo',
    tipoRiego: 'Goteo automatizado',
    estadoPoda: 'Poda pendiente',
    superficieHa: 3.2,
    variedadPrincipal: 'Hojiblanca',
    imagenUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDAaIM-XEjMWAGV9CxhXDvnQP1-l8SqQN_gTx1_ngwsh_tF9zNNALRZk4XSQwVhM-JN44zg5ky3_4GjY1JNqGiBJSZAW_s_LD6zCOrT8Vt2Q5X40_346FRj4gU_RVdHVuT4DiEfUppa81BgWW_IZhBAeKiJgGjMvmK1klZvdAOtu0ZAT6KOh83iL8Deyea0XsinfwCoxJjVSDgfZ4noFh9MxLn6Pnl-9mHXHxN6-827bvmfsLXsyjF7WA'
  },
  {
    id: 't-3',
    nombre: 'La Vega',
    alias: 'LV-03',
    ubicacion: 'Sector Este',
    olivosCount: undefined,
    estado: 'incompleto',
    alertaMsg: 'Nº de árboles sin registrar',
    tipoRiego: 'Secano tradicional',
    estadoPoda: 'Sin evaluar',
    superficieHa: 2.1,
    variedadPrincipal: 'Arbequina',
    imagenUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBvlILRo02pF40ufISuID3lnpj6kh-Hucx-okmdi4Q5YYl7HTwt0J_aPJ5rF4_WtZIzf5v17xYQY4Y5e6xYE7fYPSGtvQbfy9OWEBskIZxB10_TPiPc2BBEf4aTGy9Hz7DjrQPKzzSXYOFunTuD3WwKa7NaRxqhECag0lc_M9ziwDiSvfEXB-5Qv0jAll5ajY36Du11NM3QLQaz8u8Vc8W4XQAP7E7JZsn9y2kKs1EFIqNWmygsdUmVQw'
  },
  {
    id: 't-4',
    nombre: 'El Rincón',
    alias: 'ER-04',
    ubicacion: 'Sector Oeste',
    olivosCount: 650,
    estado: 'activo',
    tipoRiego: 'Apoyo puntual',
    estadoPoda: 'Poda al día',
    superficieHa: 2.8,
    variedadPrincipal: 'Picual',
    imagenUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBPXJ_4PDHJlVW6KVp1Hy08dUYLaE8lNZFruqqCzLNItKnI8_3QwISdr1wrnaHTALDl-GV45L2u3P7nKpjdt85ocaAEjsBQviXO4tqkIoRZ5EF8l0ckkpKlfqtT21-Uq8JYAHjulrbO9SqzHMsv26cKWlAZOU8EtwERGzElyC3Slip07A1e9hUOyUtBIb1SoQE_2qj0Hq-Ke5YGgRq5UKb5dNd8di3Om1LoiXCw6RB-t8IjyOrPfv2e-A'
  }
];

export const initialDiario: DiarioEntry[] = [
  {
    id: 'd-1',
    tipo: 'actividad',
    titulo: 'Poda de formación',
    descripcion: 'Mantenimiento correctivo en sector norte del olivar.',
    fecha: 'Hoy, 24 Octubre',
    fechaRaw: '2024-10-24',
    hora: '08:00 - 12:00',
    terrenoId: 't-1',
    terrenoNombre: 'Loma del Sol',
    trabajador: 'Juan Pérez',
    duracionHoras: 4,
    completado: true
  },
  {
    id: 'd-2',
    tipo: 'compra',
    titulo: 'Abono NPK',
    descripcion: 'Suministro para fertilización de primavera.',
    fecha: 'Ayer, 14:30',
    fechaRaw: '2024-10-23',
    monto: 450,
    proveedor: 'AgroCentro Sur',
    cantidadKg: 500
  },
  {
    id: 'd-3',
    tipo: 'cosecha',
    titulo: 'Aceituna Picual',
    descripcion: 'Recolecta inicial con cuadrilla mecanizada.',
    fecha: '21 Octubre',
    fechaRaw: '2024-10-21',
    terrenoId: 't-2',
    terrenoNombre: 'Valle Bajo',
    cantidadKg: 1200,
    rendimientoPct: 21.0
  },
  {
    id: 'd-4',
    tipo: 'riego',
    titulo: 'Riego Sector Norte',
    descripcion: 'Se aplicaron 50L/m² de agua en el sector norte. Presión del sistema estable a 3 bar.',
    fecha: '20 Octubre, 08:30 AM',
    fechaRaw: '2024-10-20',
    terrenoId: 't-1',
    terrenoNombre: 'Loma del Sol',
    trabajador: 'Juan Pérez',
    completado: true
  },
  {
    id: 'd-5',
    tipo: 'actividad',
    titulo: 'Preparación de Suelo',
    descripcion: 'Arado profundo en Lote B preparativo para la siembra. Tractores T-01 y T-02 operativos.',
    fecha: '18 Octubre, 15:45 PM',
    fechaRaw: '2024-10-18',
    terrenoId: 't-2',
    terrenoNombre: 'Valle Bajo',
    fotos: [
      'https://lh3.googleusercontent.com/aida-public/AB6AXuD1s6Fp1owKeY4VKHnPz_5YZn3zoq4bvJ9I2STqnmPz2py8DVTY-6PLc49-geDLt_eNuUIMm5pe6q_zbzAOcUMtrr4_4XhUi8w1yknBFlEl6pgoSt8Qo1bSsUrCu4HulK5HPxcgAsUN3ynYymEV4X_CWF0PgH8cP0vyeSBFV28o3fS7vDWRwXZjgpfQH73RxU3Nf2Bu_OdR8gxEtvBq6HrG76B-iFRL_LMwuCkSiy_Lr3ucxfEjlYUOZQ',
      'https://lh3.googleusercontent.com/aida-public/AB6AXuDGlpSINk6h_nFDyKPJQXu3OWIdi9svBr_wbIIOhGU2AQyzi6xJOh3WXEHRfujJetrBpP8xJ_kCNP3qHuh5GXoUv35kfj_82pCy2lDvvow-iJJCc5Hu6AonO6VkUevrjIfycbENwXygcACM78NYYYKc8SDxLaWatVErBGlOehwfB7o0kTqYvWXk6b66BsxaU_id6mHw8xP0o32V0iCxuzXJZ4H3bfh6oRxpHLLk60KMo_i-CDUkUUxd2Q'
    ]
  },
  {
    id: 'd-6',
    tipo: 'alerta',
    titulo: 'Falla en Sensor de Humedad',
    descripcion: 'El sensor de humedad HS-04 en el sector sur ha dejado de transmitir datos. Se requiere revisión técnica.',
    fecha: '15 Octubre, 10:15 AM',
    fechaRaw: '2024-10-15',
    terrenoId: 't-2',
    terrenoNombre: 'Valle Bajo'
  },
  {
    id: 'd-7',
    tipo: 'compra',
    titulo: 'Recepción de Fertilizantes',
    descripcion: 'Llegada de 500kg de fertilizante NPK. Almacenado en galpón principal.',
    fecha: '12 Octubre, 14:00 PM',
    fechaRaw: '2024-10-12',
    monto: 380,
    proveedor: 'Fertilizantes Agrícolas S.L.'
  }
];

export const initialHarvests: HarvestRecord[] = [
  {
    id: 'h-1',
    fecha: '15 Nov 2023',
    terrenoId: 't-1',
    terrenoNombre: 'Loma del Sol',
    producto: 'Aceituna Picual',
    kgs: 1200,
    rendimientoPct: 21.0,
    destino: 'Almazara',
    almazaraNombre: 'Almazara Regional'
  },
  {
    id: 'h-2',
    fecha: '12 Nov 2023',
    terrenoId: 't-2',
    terrenoNombre: 'Cerro Sur',
    producto: 'Aceituna Hojiblanca',
    kgs: 800,
    rendimientoPct: 18.0,
    destino: 'Sin destino'
  },
  {
    id: 'h-3',
    fecha: '08 Nov 2023',
    terrenoId: 't-1',
    terrenoNombre: 'Loma del Sol',
    producto: 'Aceituna Arbequina',
    kgs: 450,
    rendimientoPct: 19.2,
    destino: 'Almazara',
    almazaraNombre: 'Almazara Regional'
  },
  {
    id: 'h-4',
    fecha: '02 Nov 2023',
    terrenoId: 't-4',
    terrenoNombre: 'El Rincón',
    producto: 'Aceituna Picual',
    kgs: 950,
    rendimientoPct: 20.4,
    destino: 'Consumo Propio'
  }
];

export const initialWorkers: Worker[] = [
  {
    id: 'w-1',
    nombre: 'Juan Pérez',
    rol: 'Encargado de Finca / Capataz',
    telefono: '+34 612 345 678',
    activo: true,
    avatarUrl: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAXAO-74S9-2IPDCvPZqJFAtxDV5Bn0xzBbLjzqD5KG5GgKmKJ77c9cCMttqK45autdTP1NXkrAWiD_YD-e5ro3cbGckg8xZz01WdfK1IvgCixvfZfYn7hvEUY_-L2qVKEspE1p1C2bpMI0sSdVT1DJrvOWdb1UFdkJQb6taU0SY6iydmwUsUJWQs9j-6lpqDu0zlO7Ik98VdLMCYGjH9qCZMFuG33knY-kkxev306Ul8HHSx19RSj6wA'
  },
  {
    id: 'w-2',
    nombre: 'Carlos Mendoza',
    rol: 'Tractorista & Maquinista',
    telefono: '+34 622 987 654',
    activo: true,
    avatarUrl: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&auto=format&fit=crop&q=80'
  },
  {
    id: 'w-3',
    nombre: 'Lucía Fernández',
    rol: 'Especialista en Riego & Fitosanitarios',
    telefono: '+34 633 456 789',
    activo: true,
    avatarUrl: 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=150&auto=format&fit=crop&q=80'
  }
];

export const initialPurchases: PurchaseRecord[] = [
  {
    id: 'p-1',
    fecha: '2024-10-23',
    concepto: 'Abono NPK 15-15-15',
    categoria: 'Fertilizantes',
    proveedor: 'AgroCentro Sur',
    monto: 450,
    cantidad: '500 kg'
  },
  {
    id: 'p-2',
    fecha: '2024-10-12',
    concepto: 'Filtro de gasoil y lubricante tractor',
    categoria: 'Mantenimiento',
    proveedor: 'Repuestos Agrícolas Baeza',
    monto: 125,
    cantidad: '2 unidades'
  },
  {
    id: 'p-3',
    fecha: '2024-09-28',
    concepto: 'Tubería de goteo autocompensante 16mm',
    categoria: 'Riego',
    proveedor: 'HidroRiego Guadalquivir',
    monto: 680,
    cantidad: '1,000 metros'
  }
];
