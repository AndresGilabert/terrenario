/**
 * MKT-102 — Contenido de las landings públicas de funcionalidades y casos de uso.
 *
 * El contenido factual (qué hace cada funcionalidad) sale de las fichas de módulo en
 * `docs/03-modulos/*README.md` y de `docs/01-producto/reglas-de-negocio.md` y `personas.md`: no se
 * describe ninguna capacidad que el producto no tenga hoy (sin precio/molturación/balance de
 * cosecha, sin IA, sin permisos granulares, sin analítica de terceros — `ADR-0011`).
 *
 * `relatedSlugs` conecta funcionalidades que se usan juntas en el mismo flujo operativo (mismo
 * criterio que el mapa de módulos de `docs/03-modulos/_vision-general.md`), no un enlazado
 * arbitrario: es lo que CA-2 de `MKT-102` pide y lo que evita un enlazado interno que no signifique
 * nada para quien lo sigue.
 */

export type LandingCluster = 'funcionalidad' | 'perfil';

export interface LandingBullet {
  /** Nombre de glifo de Material Symbols Outlined, literal (ver `estandares-codigo.md`). */
  icon: string;
  title: string;
  text: string;
}

export interface LandingContent {
  slug: string;
  /** Ruta pública, sin barra final, tal y como la fija el `spec.md` de `MKT-102`. */
  path: string;
  cluster: LandingCluster;
  navLabel: string;
  title: string;
  metaDescription: string;
  eyebrow: string;
  h1: string;
  intro: string;
  bullets: LandingBullet[];
  relatedSlugs: string[];
}

export const LANDING_CONTENTS: LandingContent[] = [
  {
    slug: 'gestion-terrenos',
    path: '/funcionalidades/gestion-terrenos',
    cluster: 'funcionalidad',
    navLabel: 'Gestión de terrenos',
    title: 'Gestión de terrenos agrícolas | Terrenario',
    metaDescription:
      'Registra cada parcela con propietario, ubicación y número de olivos. La ficha de terreno es la base de todo lo que registras en Terrenario.',
    eyebrow: 'Funcionalidad',
    h1: 'Gestión de terrenos: la ficha de cada parcela, siempre a mano',
    intro:
      'Cada terreno que trabajas —propio, familiar o compartido— tiene su ficha: nombre, tipo de propiedad, propietario, ubicación y número de olivos. Es el primer dato que registras y el que enlaza todo lo demás: actividades, cosechas y compras se apuntan siempre a un terreno.',
    bullets: [
      {
        icon: 'map',
        title: 'Alta mínima, ficha completa después',
        text: 'Da de alta un terreno con el nombre y el tipo de propiedad. Añade propietario, alias, referencia catastral, ubicación y número de olivos cuando tengas el dato, no antes.',
      },
      {
        icon: 'layers',
        title: 'Base de todo registro operativo',
        text: 'Actividades, cosechas y compras se apuntan siempre a un terreno: es el eje que después permite ver cuánto cuesta y cuánto rinde cada parcela por separado.',
      },
      {
        icon: 'insights',
        title: 'Número de olivos, para KPIs reales',
        text: 'Con el número de olivos registrado, el dashboard puede calcular el rendimiento por árbol además del rendimiento por kilo.',
      },
    ],
    relatedSlugs: ['diario-de-campo', 'control-cosechas', 'dashboard-campana', 'gestion-multiterreno'],
  },
  {
    slug: 'diario-de-campo',
    path: '/funcionalidades/diario-de-campo',
    cluster: 'funcionalidad',
    navLabel: 'Diario de campo',
    title: 'Diario de campo agrícola | Terrenario',
    metaDescription:
      'Registra podas, riegos, fertilizaciones y el trabajo de cada persona en un único diario cronológico por terreno y temporada.',
    eyebrow: 'Funcionalidad',
    h1: 'Diario de campo: todo lo que pasa en tu explotación, por fecha',
    intro:
      'El diario de campo es la vista principal de Terrenario: actividades, compras, consumos y cosechas mezclados en un único eje cronológico. Cada actividad registra terreno, tarea, responsable, horas y coste, para que no dependas de la memoria ni del papel.',
    bullets: [
      {
        icon: 'event_note',
        title: 'Un registro por cada jornada',
        text: 'Anota terreno, tarea, responsable y horas dedicadas. La tarea puede venir de un catálogo reutilizable o escribirse libremente, y Terrenario la aprende para la próxima vez.',
      },
      {
        icon: 'groups',
        title: 'Quién trabajó y cuánto',
        text: 'Los miembros del Workspace aparecen automáticamente como responsables seleccionables; también puedes registrar trabajadores externos.',
      },
      {
        icon: 'checklist',
        title: 'Coste manual, sin sorpresas',
        text: 'El coste de cada actividad lo escribes tú: Terrenario no calcula tarifas automáticas, así que lo que ves es exactamente lo que decidiste anotar.',
      },
    ],
    relatedSlugs: ['gestion-terrenos', 'compras-y-consumos', 'trabajadores-y-tareas', 'control-cosechas'],
  },
  {
    slug: 'control-cosechas',
    path: '/funcionalidades/control-cosechas',
    cluster: 'funcionalidad',
    navLabel: 'Control de cosechas',
    title: 'Control de cosechas agrícolas | Terrenario',
    metaDescription:
      'Registra la recolección por terreno y temporada, con kilos, destino y rendimiento, y consulta la evolución de cada campaña.',
    eyebrow: 'Funcionalidad',
    h1: 'Control de cosechas: la recolección, terreno a terreno',
    intro:
      'Registra cada cosecha con su terreno, temporada, producto y kilos. Añade el destino —incluida la opción «desconocido» cuando aún no lo sepas— y el rendimiento o los litros obtenidos, sin bloquear el registro por datos que todavía no tienes.',
    bullets: [
      {
        icon: 'agriculture',
        title: 'Kilos y destino, siempre',
        text: 'El peso recolectado y el destino son los datos obligatorios de cada cosecha; el resto se completa cuando lo sepas.',
      },
      {
        icon: 'insights',
        title: 'Rendimiento o litros, uno de los dos',
        text: 'Terrenario admite rendimiento o litros de aceite por cosecha, nunca los dos a la vez, para que el dato no se contradiga.',
      },
      {
        icon: 'event_note',
        title: 'Cosecha dentro del diario',
        text: 'Cada cosecha aparece también en el diario cronológico unificado, junto al resto de la operativa de esa fecha.',
      },
    ],
    relatedSlugs: ['dashboard-campana', 'gestion-terrenos', 'diario-de-campo'],
  },
  {
    slug: 'compras-y-consumos',
    path: '/funcionalidades/compras-y-consumos',
    cluster: 'funcionalidad',
    navLabel: 'Compras y consumos',
    title: 'Compras y consumos agrícolas | Terrenario',
    metaDescription:
      'Registra qué compraste, en qué terreno se consumió y cuánto costó, con reparto proporcional entre parcelas cuando el material se comparte.',
    eyebrow: 'Funcionalidad',
    h1: 'Compras y consumos: qué compraste, dónde se usó y cuánto costó',
    intro:
      'Anota cada compra de material con su cantidad y coste total, y reparte el consumo entre los terrenos donde se usó. Si el consumo se produce antes de registrar la compra, Terrenario lo admite igualmente y lo avisa, sin bloquear tu trabajo diario.',
    bullets: [
      {
        icon: 'shopping_cart',
        title: 'Material, cantidad y coste total',
        text: 'Registra el material como texto libre, con sugerencias basadas en tu propio histórico de compras y consumos.',
      },
      {
        icon: 'layers',
        title: 'Reparto entre terrenos',
        text: 'Cuando un mismo material se usa en varias parcelas, imputa el consumo aproximado y el coste proporcional a cada una.',
      },
      {
        icon: 'checklist',
        title: 'Consumo sin compra previa',
        text: 'Puedes registrar el consumo aunque la compra todavía no exista: queda con coste 0 y un aviso, y no se recalcula si la compra llega más tarde.',
      },
    ],
    relatedSlugs: ['diario-de-campo', 'dashboard-campana'],
  },
  {
    slug: 'dashboard-campana',
    path: '/funcionalidades/dashboard-campana',
    cluster: 'funcionalidad',
    navLabel: 'Dashboard de campaña',
    title: 'Dashboard de campaña agrícola | Terrenario',
    metaDescription:
      'Consulta producción total, litros de aceite, rendimiento medio y kilos por terreno y por destino de tu campaña, en un único panel.',
    eyebrow: 'Funcionalidad',
    h1: 'Dashboard de campaña: la foto completa de tu temporada',
    intro:
      'El panel agrega lo que registras en el diario y en las cosechas: producción total, litros de aceite, rendimiento medio, kilos por terreno, kilos por destino y la evolución del rendimiento a lo largo de la campaña. Si tienes el número de olivos de un terreno, también calcula el rendimiento por árbol.',
    bullets: [
      {
        icon: 'insights',
        title: 'Los indicadores que ya llevabas en tu cabeza',
        text: 'Producción total, litros de aceite, rendimiento medio y kilos por terreno y por destino, calculados a partir de lo que ya registraste.',
      },
      {
        icon: 'agriculture',
        title: 'Valor económico de la campaña',
        text: 'El panel lee el coste de las actividades y compras del diario para mostrar el valor económico de la temporada, sin que tengas que recalcular nada aparte.',
      },
      {
        icon: 'layers',
        title: 'Datos incompletos, marcados, no inventados',
        text: 'Cuando falta un dato para un cálculo —como el número de olivos de un terreno— el panel lo marca en vez de estimarlo.',
      },
    ],
    relatedSlugs: ['control-cosechas', 'compras-y-consumos', 'gestion-terrenos'],
  },
  {
    slug: 'workspaces-colaboracion',
    path: '/funcionalidades/workspaces-colaboracion',
    cluster: 'funcionalidad',
    navLabel: 'Workspaces y colaboración',
    title: 'Workspaces y colaboración agrícola | Terrenario',
    metaDescription:
      'Comparte la gestión de tu explotación con tu familia o tu equipo: invita por email o por enlace y trabajad todos sobre los mismos datos.',
    eyebrow: 'Funcionalidad',
    h1: 'Workspaces y colaboración: la misma explotación, varias personas',
    intro:
      'Un Workspace es la explotación que gestionas en Terrenario. Invita a otras personas por email o por enlace compartible para que trabajéis todos sobre los mismos terrenos, el mismo diario y el mismo dashboard, sin duplicar hojas ni depender de que una sola persona tenga toda la información.',
    bullets: [
      {
        icon: 'groups',
        title: 'Invitaciones por email o por enlace',
        text: 'Añade a quien necesites con una invitación de un solo uso, por correo o por enlace compartible.',
      },
      {
        icon: 'checklist',
        title: 'Todos pueden operar',
        text: 'En esta fase, cualquier miembro del Workspace puede registrar y consultar la operativa completa: no hace falta repartir permisos para empezar a trabajar juntos.',
      },
      {
        icon: 'family_restroom',
        title: 'Salir sin perder el histórico',
        text: 'Quien abandona el Workspace deja de aparecer como responsable seleccionable, pero su trabajo pasado se conserva tal cual quedó registrado.',
      },
    ],
    relatedSlugs: ['trabajadores-y-tareas', 'diario-de-campo'],
  },
  {
    slug: 'trabajadores-y-tareas',
    path: '/funcionalidades/trabajadores-y-tareas',
    cluster: 'funcionalidad',
    navLabel: 'Trabajadores y tareas',
    title: 'Trabajadores y tareas del campo | Terrenario',
    metaDescription:
      'Un catálogo de tareas que aprende de tu propio trabajo, y un maestro de trabajadores que incluye automáticamente a quien invitas al Workspace.',
    eyebrow: 'Funcionalidad',
    h1: 'Trabajadores y tareas: quién hace qué, con un catálogo que aprende',
    intro:
      'Cada actividad del diario se anota contra una tarea y un responsable. El catálogo de tareas es propio de tu Workspace y se puede escribir en texto libre la primera vez: Terrenario la aprende y la deja lista para la próxima jornada.',
    bullets: [
      {
        icon: 'person',
        title: 'Miembros del Workspace, ya disponibles',
        text: 'Quien invitas a tu Workspace aparece automáticamente como trabajador seleccionable, sin alta manual adicional.',
      },
      {
        icon: 'checklist',
        title: 'Catálogo de tareas por Workspace',
        text: 'Empieza vacío y se completa con el uso: cada tarea nueva que escribes se puede guardar para reutilizarla.',
      },
      {
        icon: 'groups',
        title: 'Trabajadores externos también',
        text: 'Puedes registrar personas que trabajan tu explotación sin que tengan cuenta ni acceso al Workspace.',
      },
    ],
    relatedSlugs: ['diario-de-campo', 'workspaces-colaboracion'],
  },
  {
    slug: 'agricultor-particular',
    path: '/para/agricultor-particular',
    cluster: 'perfil',
    navLabel: 'Agricultor particular',
    title: 'Terrenario para el agricultor particular',
    metaDescription:
      'Gestiona tus parcelas familiares en tu tiempo libre: terreno, coste y cosecha en un solo sitio, sin hojas de papel ni cálculos a memoria.',
    eyebrow: 'Para ti',
    h1: 'Terrenario para el agricultor particular',
    intro:
      'Si gestionas una o varias parcelas heredadas o adquiridas, en tu tiempo libre y sin dedicación profesional, Terrenario sustituye las cuentas en papel por un registro único: qué se hizo en cada terreno, cuánto costó y cuánto se recolectó.',
    bullets: [
      {
        icon: 'person',
        title: 'Visión global, no dispersa',
        text: 'Deja de repartir la información entre papel y memoria: cada terreno, cada actividad y cada cosecha quedan en el mismo sitio.',
      },
      {
        icon: 'agriculture',
        title: 'Coste real por terreno',
        text: 'Registra el coste de cada jornada y cada compra, terreno a terreno, para saber cuánto inviertes de verdad en cada parcela.',
      },
      {
        icon: 'insights',
        title: 'Rendimiento por campaña',
        text: 'Consulta el rendimiento de cada temporada y compáralo con campañas anteriores desde el mismo panel.',
      },
    ],
    relatedSlugs: ['gestion-terrenos', 'diario-de-campo', 'control-cosechas'],
  },
  {
    slug: 'explotacion-familiar',
    path: '/para/explotacion-familiar',
    cluster: 'perfil',
    navLabel: 'Explotación familiar',
    title: 'Terrenario para explotaciones familiares',
    metaDescription:
      'Comparte la gestión de la explotación con tu familia: todos trabajáis sobre el mismo Workspace, los mismos terrenos y el mismo diario.',
    eyebrow: 'Para ti',
    h1: 'Terrenario para explotaciones familiares',
    intro:
      'Cuando varias personas de la familia trabajan la misma tierra, la información no puede depender de una sola persona ni de conversaciones sueltas. Un Workspace compartido deja el registro accesible para todos los que ayudan, con invitación por email o por enlace.',
    bullets: [
      {
        icon: 'family_restroom',
        title: 'Un Workspace, toda la familia',
        text: 'Invita a quien ayude en la explotación —hijos, hermanos, pareja— y todos veréis los mismos terrenos, el mismo diario y el mismo dashboard.',
      },
      {
        icon: 'groups',
        title: 'El trabajo de cada persona, registrado',
        text: 'Cada actividad queda asociada a quien la hizo, así que al cerrar la temporada no hace falta reconstruir de memoria quién hizo qué.',
      },
      {
        icon: 'checklist',
        title: 'Sin reparto de permisos que aprender',
        text: 'Cualquier miembro del Workspace puede registrar y consultar la operativa: no hay que configurar roles para empezar.',
      },
    ],
    relatedSlugs: ['workspaces-colaboracion', 'trabajadores-y-tareas', 'diario-de-campo'],
  },
  {
    slug: 'gestion-multiterreno',
    path: '/para/gestion-multiterreno',
    cluster: 'perfil',
    navLabel: 'Gestión multiterreno',
    title: 'Terrenario para gestión multiterreno',
    metaDescription:
      'Compara coste y rendimiento entre varias parcelas dispersas, con una ficha propia por terreno y kilos por terreno en el dashboard.',
    eyebrow: 'Para ti',
    h1: 'Terrenario para quien gestiona varios terrenos',
    intro:
      'Cuando las parcelas están repartidas y no todas rinden igual, hace falta compararlas, no solo sumarlas. Cada terreno tiene su ficha y su propio histórico, y el dashboard desglosa la producción y el coste por terreno para que la comparación sea directa.',
    bullets: [
      {
        icon: 'layers',
        title: 'Cada terreno, con su ficha',
        text: 'Nombre, ubicación y número de olivos por parcela, para no perder de vista ninguna de tus tierras.',
      },
      {
        icon: 'insights',
        title: 'Kilos y rendimiento por terreno',
        text: 'El dashboard desglosa la producción por terreno y por destino, así que ves de un vistazo cuál rinde más.',
      },
      {
        icon: 'agriculture',
        title: 'Coste comparable entre parcelas',
        text: 'Actividades y compras se registran siempre por terreno, así que el coste de cada parcela queda separado del resto desde el primer día.',
      },
    ],
    relatedSlugs: ['gestion-terrenos', 'dashboard-campana', 'control-cosechas'],
  },
];

export function getLandingBySlug(slug: string): LandingContent | undefined {
  return LANDING_CONTENTS.find((content) => content.slug === slug);
}

export function getRelatedLandings(content: LandingContent): LandingContent[] {
  return content.relatedSlugs
    .map((slug) => getLandingBySlug(slug))
    .filter((related): related is LandingContent => related !== undefined);
}
