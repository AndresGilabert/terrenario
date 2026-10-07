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

export type LandingCluster = 'funcionalidad' | 'perfil' | 'guia';

export interface LandingBullet {
  /** Nombre de glifo de Material Symbols Outlined, literal (ver `estandares-codigo.md`). */
  icon: string;
  title: string;
  text: string;
  image?: {
    src: string;
    alt: string;
    width: number;
    height: number;
  };
}

export interface LandingFaq {
  question: string;
  answer: string;
}

export interface LandingSection {
  id: string;
  title: string;
  intro?: string;
  tone: 'plain' | 'muted' | 'accent';
  items: LandingBullet[];
}

export interface LandingCta {
  title: string;
  text: string;
  label: string;
}

export interface LandingSocialMeta {
  title?: string;
  description?: string;
  image?: string;
  imageAlt?: string;
}

export interface LandingSeo {
  openGraph?: LandingSocialMeta;
  twitter?: LandingSocialMeta;
  structuredDataDescription?: string;
}

export interface LandingContent {
  slug: string;
  /** Ruta pública, sin barra final, tal y como la fija el `spec.md` de `MKT-102`. */
  path: string;
  cluster: LandingCluster;
  navLabel: string;
  title: string;
  metaDescription: string;
  seo?: LandingSeo;
  eyebrow: string;
  h1: string;
  intro: string;
  bullets: LandingBullet[];
  sections?: LandingSection[];
  finalCta?: LandingCta;
  faqs: LandingFaq[];
  relatedSlugs: string[];
}

const LANDING_FAQS: Record<string, LandingFaq[]> = {
  'software-gestion-agricola': [
    { question: '¿Qué necesito para empezar a utilizar Terrenario?', answer: 'Nada, accede a la aplicación con tu cuenta de Google y el asistente te guiará en la creación del grupo de trabajo (Workspace), la primera temporada y los primeros terrenos. En menos de 5 minutos podrás empezar a registrar tus trabajos y cosechas.' },
    { question: '¿La gestión agrícola puede ser colaborativa?', answer: 'Sí, con Terrenario, podrás agregar usuarios al grupo de trabajo y que la gestión sea compartida.' },
	{ question: '¿Puedo usar Terrenario para explotaciones distintas al olivar?', answer: 'Actualmente Terrenario está centrado en la gestión agrícola del olivar, por ser el cultivo más extendido y conocido por el equipo de Terrenario, aunque la base de la aplicación está pensada para crecer con otros cultivos. Si estás interesado en otros cultivos, puedes ponerte en contacto con nosotros escribiendo a hola@andresgilabert.dev ' },
  ],
  'gestion-terrenos': [
    { question: '¿Qué datos necesito para crear un terreno?', answer: 'Puedes empezar con el nombre y el tipo de propiedad. La ubicación, el propietario y el número de olivos se pueden completar después.' },
    { question: '¿Por qué cada registro se asocia a un terreno?', answer: 'El terreno es la base para relacionar actividades, cosechas y compras, y para consultar después costes y rendimiento por parcela.' },
  ],
  'diario-de-campo': [
    { question: '¿Qué puedo registrar en el diario de campo?', answer: 'El diario reúne actividades, compras, consumos y cosechas en un único eje cronológico por fecha.' },
    { question: '¿Qué información lleva una actividad?', answer: 'Cada actividad registra terreno, tarea, responsable, horas y coste manual.' },
  ],
  'control-cosechas': [
    { question: '¿Qué puedo registrar de una cosecha de aceituna?', answer: 'Cada cosecha incluye terreno, temporada, fecha, kilos y destino. También puedes informar el rendimiento de aceite o los litros obtenidos.' },
    { question: '¿Cómo expresa Terrenario el rendimiento de aceite?', answer: 'Terrenario guarda el rendimiento en litros de aceite por cada 100 kg de aceituna. Puedes introducir ese valor, informar kg de aceite por 100 kg o calcularlo desde los kilos entregados y los litros obtenidos.' },
    { question: '¿Puedo comparar la cosecha con campañas anteriores?', answer: 'Sí. El dashboard muestra la evolución del rendimiento y el promedio histórico desde el primer año disponible; los promedios de 5 y 10 años aparecen cuando existe histórico suficiente.' },
  ],
  'compras-y-consumos': [
    { question: '¿Puedo registrar un consumo sin haber anotado antes la compra?', answer: 'Sí. El consumo queda registrado con coste 0 y un aviso; una compra posterior no recalcula ese coste histórico.' },
    { question: '¿Cómo reparto un material entre varios terrenos?', answer: 'Puedes imputar un consumo aproximado a cada terreno y Terrenario calcula el coste proporcional cuando está vinculado a la compra.' },
  ],
  'dashboard-campana': [
    { question: '¿Qué muestra el dashboard de campaña?', answer: 'Muestra producción total, litros de aceite, rendimiento medio, kilos por terreno, kilos por destino y la evolución del rendimiento.' },
    { question: '¿Qué ocurre si faltan datos para un indicador?', answer: 'Cuando falta un dato como el número de olivos, el panel marca el resultado como incompleto en lugar de estimarlo.' },
  ],
  'workspaces-colaboracion': [
    { question: '¿Cómo invito a otra persona a mi Workspace?', answer: 'Puedes enviar una invitación por email o compartir un enlace de un solo uso.' },
    { question: '¿Qué puede hacer un miembro del Workspace?', answer: 'En esta fase, cualquier miembro puede registrar y consultar la operativa completa del Workspace.' },
  ],
  'trabajadores-y-tareas': [
    { question: '¿Los miembros invitados aparecen como responsables?', answer: 'Sí. Quien invitas al Workspace aparece automáticamente como trabajador seleccionable.' },
    { question: '¿Puedo registrar trabajadores sin cuenta?', answer: 'Sí. Puedes crear fichas de trabajadores externos sin darles acceso al Workspace.' },
  ],
  'agricultor-particular': [
    { question: '¿Para qué tipo de explotación sirve Terrenario?', answer: 'Está pensado para gestionar una o varias parcelas familiares o adquiridas, incluso cuando las trabajas en tu tiempo libre.' },
    { question: '¿Qué puedo consultar al final de una temporada?', answer: 'Puedes revisar qué se hizo en cada terreno, cuánto costó y cuánto se recolectó desde el mismo registro.' },
  ],
  'explotacion-familiar': [
    { question: '¿Puede una familia trabajar sobre la misma información?', answer: 'Sí. Un Workspace compartido permite que las personas invitadas vean los mismos terrenos, diario y dashboard.' },
    { question: '¿Hay que configurar roles para empezar?', answer: 'No. En esta fase todos los miembros del Workspace pueden registrar y consultar la operativa.' },
  ],
  'gestion-multiterreno': [
    { question: '¿Cómo comparo varias parcelas?', answer: 'Cada terreno tiene su propia ficha e histórico, y el dashboard desglosa producción y coste por terreno.' },
    { question: '¿Qué datos se ven por terreno?', answer: 'Puedes consultar los kilos y el rendimiento por terreno, además de los costes de actividades y compras registradas.' },
  ],
};

export const LANDING_CONTENTS: LandingContent[] = [
  {
    slug: 'software-gestion-agricola',
    path: '/funcionalidades/software-gestion-agricola',
    cluster: 'funcionalidad',
    navLabel: 'Software de gestión agrícola',
    title: 'Software de gestión agrícola | Terrenario',
    metaDescription:
      'Gestiona tus bancales con el software de gestión agrícola de Terrenario.',
    seo: {
      openGraph: {
        title: 'Software de gestión agrícola Terrenario',
        description: 'Centraliza la gestión de tus terrenos, las cosechas y las tareas que realizas día a día.',
        imageAlt: 'Terrenario, Software de gestión agrícola',
      },
      twitter: {
        title: 'Software de gestión agrícola Terrenario',
        description: 'Centraliza la gestión de tus terrenos, las cosechas y las tareas que realizas día a día.',
        imageAlt: 'Terrenario, Software de gestión agrícola',
      },
      structuredDataDescription:
        'Software de gestión agrícola para centralizar la información de tus terrenos, cosechas y tareas agrícolas.',
    },
    eyebrow: 'Software de gestión agrícola',
    h1: 'Software de gestión agrícola',
    intro:
      'Con el software de gestión agrícola de Terrenario, podrás centralizar la información de tus terrenos de manera fácil y sencilla. Con una sencilla configuración, podrás centralizar la gestión agrícola con un control de la cosecha, las actividades y los gastos de los productos que compras. Todos los registros quedan vinculados a la campaña o temporada a la que corresponden, permitiendo un control agrícola temporada a temporada, con comparación de resultados entre las distintas campañas.',
    bullets: [
      {
        icon: 'agriculture',
        title: 'Registro por cosecha',
        text: 'Anota fecha, terreno, temporada, kilos de aceituna y destino para llevar el control agrícola de tus cosechas a un nivel que no esperabas.',
      },
      {
        icon: 'layers',
        title: 'Seguimiento por terreno',
        text: 'Con el dashboard de nuestro software agrícola, controlaras las cosechas por terreno y por destino, ordenaras los bancales por producción y controlaras los cálculos incompletos en lugar de inventar datos.',
      },
      {
        icon: 'event_note',
        title: 'Evolución entre campañas',
        text: 'Revisa la evolución de la producción y el promedio histórico. Con el tiempo, generarás un histórico que te permitira comparativas de 5 y 10 años.',
      },
      {
        icon: 'event_note',
        title: 'No empieces desde cero',
        text: 'Crea campañas pasadas e introduce de una manera rápida los datos históricos de tus cosechas para empezar con un historial completo.',
      },
    ],
    sections: [
      {
        id: 'problema',
        title: '¿Necesitas un programa de gestión agrícola donde centralizar tus apuntes?',
        intro: 'Cuando el control agrícola queda repartido entre cuadernos, notas y memoria, comparar cosechas y gastos entre temporadas exige recalcular todo una y otra vez.',
        tone: 'plain',
        items: [
          {
            icon: 'event_note',
            title: 'Información dispersa',
            text: 'La producción, los trabajos y los gastos terminan en cajones y libretas dispersas y cuesta recuperar una visión completa de la campaña.',
          },
          {
            icon: 'insights',
            title: 'Control agrícola imposible',
            text: 'Sin un software de agricultura donde centralizar tus apuntes, el control de las campañas se vuelve caótico y poco fiable.',
          },
          {
            icon: 'layers',
            title: 'Poca visión histórica',
            text: 'Sin registros de campañas anteriores y datos que se van perdiendo, las comparativas entre temporadas se basan solamente en suposiciones.',
          },
        ],
      },
      {
        id: 'funcionalidades',
        title: 'Toda la gestión agrícola, ordenada y comparable',
        intro: 'El software agrícola de Terrenario centraliza los datos necesarios para conseguir un control de cosecha sin convertir el trabajo diario en una tarea administrativa.',
        tone: 'muted',
        items: [],
      },
      {
        id: 'beneficios',
        title: 'Centraliza la gestión agrícola en un único software',
        intro: 'Con Terrenario dispondrás de una aplicación donde centralizar el control de tus bancales y empezar a desligarte de recibos y libretas físicas.',
        tone: 'accent',
        items: [
          {
            icon: 'checklist',
            title: 'Ahorra tiempo registrando todo en un único lugar.',
            text: 'Dispondrás de todos los datos de tus terrenos en un solo lugar, accesible desde tu movíl estés donde estés.',
          },
          {
            icon: 'map',
            title: 'Información agrupada por terreno.',
            text: 'Toda la información está disponble como totales, o como datos disgregados por terreno. Con sulta los totales o lo de un bancal concreto según necesites.',
          },
          {
            icon: 'agriculture',
            title: 'Controla el destino de cada cosecha.',
            text: '¿Recuerdas la parte de la cosecha que destinaste a aceite para consumo propio? ¿Y para aceite para venta? ¿venta de aceituna? Con Terrenario puedes controlar y revisar la distribución de tu cosecha según el destino que le des.',
          },
        ],
      },
    ],
    finalCta: {
      title: 'Centraliza y controla la gestión de tu olivar',
      text: 'No pierdas datos con distintos fuentes de datos. Centraliza la gestión agrícola de tu olivar en un solo lugar.',
      label: 'Acceder a Terrenario',
    },
    faqs: LANDING_FAQS['software-gestion-agricola'],
    relatedSlugs: ['dashboard-campana', 'gestion-terrenos', 'diario-de-campo'],
  },
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
    faqs: LANDING_FAQS['gestion-terrenos'],
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
    faqs: LANDING_FAQS['diario-de-campo'],
    relatedSlugs: ['gestion-terrenos', 'compras-y-consumos', 'trabajadores-y-tareas', 'control-cosechas'],
  },
  {
    slug: 'control-cosechas',
    path: '/funcionalidades/control-cosechas',
    cluster: 'funcionalidad',
    navLabel: 'Control de cosecha de olivar',
    title: 'Control de cosecha de olivar y rendimiento de aceite | Terrenario',
    metaDescription:
      'Registra kilos de aceituna, rendimiento de aceite y destino por terreno. Compara campañas y consulta kilos por árbol en Terrenario.',
    seo: {
      openGraph: {
        title: 'Control de cosecha de olivar con Terrenario',
        description: 'Centraliza kilos de aceituna, rendimiento de aceite y destino por terreno, y compara la evolución de tus campañas.',
        imageAlt: 'Terrenario, control de cosecha de olivar por terreno',
      },
      twitter: {
        title: 'Control de cosecha de olivar con Terrenario',
        description: 'Registra kilos, rendimiento de aceite y destino por terreno en un histórico de campaña.',
        imageAlt: 'Terrenario, control de cosecha de olivar por terreno',
      },
      structuredDataDescription:
        'Aplicación web para registrar cosechas de aceituna, consultar el rendimiento de aceite y comparar la producción por terreno y temporada.',
    },
    eyebrow: 'Control de cosecha para olivar',
    h1: 'Controla tu cosecha de olivar, terreno a terreno',
    intro:
      'Centraliza los kilos de aceituna, el destino y el rendimiento de aceite de cada terreno. Terrenario ordena la recolección por temporada y te permite comparar la evolución de tu olivar sin depender de papeles ni cálculos dispersos.',
    bullets: [
      {
        icon: 'agriculture',
        title: 'Registro por cosecha',
        text: 'Anota fecha, terreno, temporada, kilos de aceituna y destino. Si todavía no conoces el destino, puedes guardar la cosecha como «Sin destino» y completarlo después.',
      },
      {
        icon: 'insights',
        title: 'Rendimiento de aceite comparable',
        text: 'Consulta el rendimiento en litros por cada 100 kg de aceituna. Puedes introducirlo directamente o calcularlo desde los kilos entregados y los litros obtenidos.',
      },
      {
        icon: 'layers',
        title: 'Seguimiento por terreno',
        text: 'El dashboard desglosa los kilos por terreno y por destino, ordena las parcelas por producción y marca los cálculos incompletos en lugar de inventar datos.',
      },
      {
        icon: 'event_note',
        title: 'Evolución entre campañas',
        text: 'Revisa la evolución del rendimiento y el promedio histórico. Las referencias de 5 y 10 años solo aparecen cuando existe histórico suficiente.',
      },
    ],
    sections: [
      {
        id: 'problema',
        title: '¿Puedes saber qué terreno rindió mejor sin reconstruir la campaña?',
        intro: 'Cuando la información de la cosecha queda repartida entre cuadernos, notas y memoria, comparar terrenos y temporadas exige rehacer cuentas cada vez.',
        tone: 'plain',
        items: [
          {
            icon: 'event_note',
            title: 'Información dispersa',
            text: 'Los kilos, el destino y el rendimiento terminan en soportes distintos y cuesta recuperar una visión completa de la campaña.',
          },
          {
            icon: 'insights',
            title: 'Rendimiento difícil de seguir',
            text: 'Sin una unidad común, comparar entregas o conocer la evolución del rendimiento de aceite obliga a normalizar los datos a mano.',
          },
          {
            icon: 'layers',
            title: 'Poca perspectiva histórica',
            text: 'Sin un registro por terreno y temporada, resulta difícil contrastar la campaña actual con los años anteriores.',
          },
        ],
      },
      {
        id: 'funcionalidades',
        title: 'Toda la cosecha del olivar, ordenada y comparable',
        intro: 'Terrenario reúne los datos necesarios para seguir la producción sin convertir el trabajo diario en una tarea administrativa.',
        tone: 'muted',
        items: [],
      },
      {
        id: 'beneficios',
        title: 'Decide con datos registrados, no con estimaciones de memoria',
        intro: 'El valor no está en acumular cifras, sino en poder localizar qué ocurrió en cada terreno y comparar campañas con el mismo criterio.',
        tone: 'accent',
        items: [
          {
            icon: 'checklist',
            title: 'Menos tiempo reconstruyendo datos',
            text: 'La cosecha queda vinculada desde el principio a una fecha, un terreno y una temporada.',
          },
          {
            icon: 'map',
            title: 'Producción visible por terreno',
            text: 'Consulta los kilos por parcela y, cuando has informado el número de olivos, los kilos por árbol.',
          },
          {
            icon: 'agriculture',
            title: 'Destino trazable',
            text: 'Distingue venta de aceituna, aceite para venta, aceite para consumo propio y cosechas todavía sin destino.',
          },
        ],
      },
    ],
    finalCta: {
      title: 'Empieza a registrar la cosecha de tu olivar',
      text: 'Reúne kilos, rendimiento de aceite y destino por terreno en un único histórico de campaña.',
      label: 'Acceder a Terrenario',
    },
    faqs: LANDING_FAQS['control-cosechas'],
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
    faqs: LANDING_FAQS['compras-y-consumos'],
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
    faqs: LANDING_FAQS['dashboard-campana'],
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
    faqs: LANDING_FAQS['workspaces-colaboracion'],
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
    faqs: LANDING_FAQS['trabajadores-y-tareas'],
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
    faqs: LANDING_FAQS['agricultor-particular'],
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
    faqs: LANDING_FAQS['explotacion-familiar'],
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
    faqs: LANDING_FAQS['gestion-multiterreno'],
    relatedSlugs: ['gestion-terrenos', 'dashboard-campana', 'control-cosechas'],
  },
  {
    slug: 'como-empezar-en-terrenario',
    path: '/guias/como-empezar-en-terrenario',
    cluster: 'guia',
    navLabel: 'Cómo empezar en Terrenario',
    title: 'Cómo empezar en Terrenario: primeros pasos',
    metaDescription:
      'Manual de primeros pasos en Terrenario. Descubre lo sencillo que resulta comenzar a usar nuestro software agricola. Con unos pocos pasos tendrás todo listo para empezar con el control de tu cosecha.',
    eyebrow: 'Ayuda y manuales',
    h1: 'Cómo empezar en Terrenario',
    intro:
      'Queremos que tu experiencia en Terrenario sea lo más sencilla posible, no queremos complicarte con datos que necesites recordar. Entra con tu Cuenta de Google (se incorporarán más opciones de login próximamente), crea el Workspace (grupo de trabajo) de tu explotación y prepara los datos básicos. Esta guía recorre el primer acceso y explica qué ocurre después, sin necesidad de rellenar más información de la necesaria y con formularios lo más simplificados posibles.',
    bullets: [
      {
        icon: 'login',
        title: '1. Inicia sesión',
        text: 'Pulsa «Acceder a la plataforma» e inicia sesión con tu Cuenta de Google. No necesitas una dirección de Gmail: puedes dar de alta en Google la dirección que ya utilizas.',
      },
      {
        icon: 'login',
        title: '2. Crea tu Workspace',
        text: 'Después de iniciar sesión, lo primero que necesitarás será crear tu Workspace (grupo de trabajo), donde se aglutinarán todos los terrenos y usuarios de la explotación, escribe el nombre de tu finca o explotación y pulsa «Crear Workspace». Así de sencillo, solamente necesitas el nombre.',
        image: {
          src: '/landings/como-empezar-en-terrenario/01_crear_workspace_terrenario.png',
          alt: 'Formulario para crear un Workspace, con el campo para escribir su nombre.',
          width: 400,
          height: 400,
        },
      },
      {
        icon: 'event',
        title: '3. Decide si quieres crear una temporada',
        text: 'Como el Workspace aún no tiene temporada, Terrenario ofrece crearla con un nombre y una fecha de inicio. Recomendamos incluir también la fecha de final de temporada, de modo que defina claramente a partír de que fecha, los registros corresponderán a otras temporadas o campañas. Por supuesto, las fechas no son limitantes para nada, podrás añadir o modificar registros y cosechas a temporadas pasadas o futuras. Puedes crearla ahora o elegir «Ahora no» y continuar.',
        image: {
          src: '/landings/como-empezar-en-terrenario/02_crear_temporada_terrenario.png',
          alt: 'Formulario de creación de temporada con nombre, fecha de inicio, fecha final opcional y opción para dejarlo para más tarde.',
          width: 400,
          height: 400,
        },
      },
      {
        icon: 'checklist',
        title: '4. Abre la preparación de terrenos',
        text: 'Con el Workspace y la temporada creada, es el momento de definir tu primer terreno. Mientras no tengas definido ninguno, al acceder a la plataforma, se mostrará la pantalla «Prepara tu explotación», localiza el apartado Terrenos y selecciona «Añadir terrenos» para crear tu primera parcela.',
        image: {
          src: '/landings/como-empezar-en-terrenario/03_anadir_terreno_terrenario.png',
          alt: 'Panel de preparación de la explotación con el apartado Terrenos y el botón Añadir terrenos señalado.',
          width: 400,
          height: 400,
        },
      },
      {
        icon: 'landscape',
        title: '5. Añade tu primer terreno',
        text: 'Como aún no tendrás definido ningún terreno, el sistema te informará de esto y te resaltará la opción de «Añadir mi primer terreno». Al pulsar sobre este botón, se abrirá el formulario para dar de alta tu primer terreno.',
        image: {
          src: '/landings/como-empezar-en-terrenario/04_nuevo_terreno_terrenario.png',
          alt: 'Estado inicial del apartado Terrenos con el botón Añadir mi primer terreno.',
          width: 400,
          height: 400,
        },
      },
      {
        icon: 'edit_note',
        title: '6. Completa la ficha del terreno',
        text: 'Indica el nombre y el tipo de propiedad, son los únicos datos obligatorios que necesitas rellenar. Alias, número de árboles, propietario, referencia catastral y ubicación son opcionales y puedes completarlos más adelante.',
        image: {
          src: '/landings/como-empezar-en-terrenario/05_nuevo_terreno_formulario_terrenario.png',
          alt: 'Formulario de terreno con nombre y tipo de propiedad obligatorios, y campos opcionales para alias, árboles, propietario, referencia catastral y ubicación.',
          width: 400,
          height: 400,
        },
      },
      {
        icon: 'edit_note',
        title: '7. Felicidades! Ya lo tienes listo',
        text: 'Con estos sencillos pasos, ya tendrás la configuración básica para empezar a trabajar con Terrenario. Esperamos que tu gestión agrícola sea más eficiente y productiva con la ayuda de Terrenario.',
      },
    ],
    faqs: [
      {
        question: '¿Necesito una dirección de Gmail para entrar?',
        answer: 'No. El acceso usa una Cuenta de Google, que puedes crear con la dirección de correo que ya tienes.',
      },
      {
        question: '¿Tengo que crear una temporada durante el primer acceso?',
        answer: 'No. Si todavía no hay temporada, Terrenario ofrece crear una, pero puedes cancelar esa oferta y continuar.',
      },
      {
        question: '¿Qué datos necesito para crear mi primer terreno?',
        answer: 'Puedes empezar con el nombre y el tipo de propiedad. La ubicación, el propietario y el número de olivos se pueden completar después.',
      },
    ],
    relatedSlugs: ['software-gestion-agricola', 'gestion-terrenos', 'dashboard-campana'],
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

/**
 * MKT-102 — Metadatos de la home (`/`) para `scripts/prerenderizar-landings.mjs`. La home no es
 * una `LandingContent` más: su marcado vive en `LandingPage.tsx` (hero con imagen propia, no el
 * layout genérico de `ContentLandingPage`), así que solo necesita lo mínimo para construir su
 * documento — el resto (título y descripción) se mantiene igual que el que ya publicaba
 * `index.html`, para no cambiar lo que ya está indexado en `/`.
 */
export const HOME_META = {
  path: '/',
  title: 'Terrenario — Tu tierra, bajo control',
  metaDescription:
    'La herramienta sencilla para el agricultor: gestiona terrenos, cosechas, compras y el diario de campo de tu explotación en un solo sitio.',
};

