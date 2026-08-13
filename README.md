# Terrenario — Tu tierra, bajo control

Plataforma de gestión agrícola para pequeñas explotaciones personales. Registra terrenos, temporadas,
trabajos del día, compras y cosechas en un único espacio de trabajo, y los convierte en indicadores de
campaña sin necesidad de una hoja de cálculo ni de conocimientos técnicos.

> **Aplicación publicada**: [`https://app.terrenario.com`](https://app.terrenario.com) ·
> **Última versión**: `v0.8.0-hito-h` ([notas de release](./docs/10-releases/v0.8.0-hito-h.md)) ·
> **Estado**: MVP entregado, hitos A–H cerrados

---

## Índice

1. [Descripción general](#descripción-general)
2. [Stack tecnológico](#stack-tecnológico)
3. [Instalación y ejecución](#instalación-y-ejecución)
4. [Estructura del proyecto](#estructura-del-proyecto)
5. [Funcionalidades](#funcionalidades)
6. [Tests y calidad](#tests-y-calidad)
7. [Despliegue](#despliegue)
8. [Base de conocimiento (KB)](#base-de-conocimiento-kb)
9. [Solución de problemas frecuentes](#solución-de-problemas-frecuentes)
10. [Contribuir](#contribuir)
11. [Licencia](#licencia)

---

## Descripción general

Terrenario nace de un problema concreto: quien cultiva unas pocas fincas —olivar, frutales, huerta— sin
dedicarse profesionalmente a ello lleva la gestión repartida entre libretas, hojas de cálculo y memoria.
No hay una única fuente de verdad, la trazabilidad del trabajo es incompleta y el coste real de una
campaña solo se intuye.

El producto sustituye esa gestión fragmentada por un registro estructurado y medible:

- **La unidad base es el terreno.** Toda actividad, compra o cosecha cuelga de un terreno concreto y de
  una temporada, propia o cedida.
- **La unidad organizativa es el Workspace** (la explotación). Una persona puede crear varios o unirse
  a otros por invitación, y alterna entre ellos con un selector siempre visible. Todos los miembros de
  un Workspace ven y editan los mismos registros: en el MVP no hay permisos granulares.
- **La vista principal es cronológica**, un diario de campo: la acción sencilla por delante del gráfico
  complejo, porque el usuario objetivo no es un perfil técnico.
- **Online-first.** No hay captura offline en el MVP; todo registro se confirma contra la API en el
  momento. La resiliencia offline es el siguiente hito del roadmap.

Arquitectónicamente es un **monolito modular** ([ADR-0002](./docs/02-arquitectura/decisiones/ADR-0002--arquitectura-monolito-modular-online-first.md)):
una API REST en .NET que también sirve la SPA de React, con PostgreSQL como única fuente de verdad
transaccional. Los módulos son fronteras de responsabilidad y de documentación, no unidades de
despliegue.

| Para profundizar | Documento |
|---|---|
| Visión de producto, alcance del MVP y objetivos | [`docs/01-producto/vision-y-objetivos.md`](./docs/01-producto/vision-y-objetivos.md) |
| Requisitos de usuario (RU-01…RU-13) y transversales | [`docs/01-producto/definicion-requisitos-usuario.md`](./docs/01-producto/definicion-requisitos-usuario.md) |
| Reglas de negocio (`RN-xxx`) | [`docs/01-producto/reglas-de-negocio.md`](./docs/01-producto/reglas-de-negocio.md) |
| Arquitectura, diagramas C4 y decisiones técnicas | [`docs/02-arquitectura/vision-general.md`](./docs/02-arquitectura/vision-general.md) |
| Roadmap por hitos | [`docs/01-producto/roadmap.md`](./docs/01-producto/roadmap.md) |

---

## Stack tecnológico

> Versiones **efectivas en el código**. Los criterios de selección, las alternativas descartadas y las
> tecnologías en evaluación están en [`docs/02-arquitectura/tech-stack.md`](./docs/02-arquitectura/tech-stack.md);
> cada decisión tiene su ADR en [`docs/02-arquitectura/decisiones/`](./docs/02-arquitectura/decisiones/).

### Backend

| Tecnología | Versión | Propósito |
|---|---|---|
| .NET / ASP.NET Core Web API | 9.0 (`net9.0`) | API REST versionada en `/api/v1`, con Controllers |
| Entity Framework Core + Npgsql | 9.x | ORM y migraciones code-first |
| PostgreSQL | 15+ | Persistencia transaccional y fuente de verdad |
| Google.Apis.Auth | 1.68 | Verificación del token de identidad de Google |
| JWT Bearer (RS256) | 9.x | Sesión propia: access token + refresh token en cookie |
| MailKit | 4.x | Correo transaccional (invitaciones, avisos, alertas) |
| Microsoft.AspNetCore.OpenApi | 9.x | Contrato OpenAPI generado desde el código |

### Frontend

| Tecnología | Versión | Propósito |
|---|---|---|
| React | 19 | SPA del área operativa y de la presencia pública |
| TypeScript | 6.x | Tipado estático |
| Vite | 8.x | Build y servidor de desarrollo |
| React Router | 8.x | Enrutado, guardas de sesión y de Workspace |
| Tailwind CSS | 4.x | Estilos (plugin oficial de Vite) |
| Material Symbols · Inter · Plus Jakarta Sans | — | Iconografía y tipografías, servidas como subconjunto propio |

### Calidad y operación

| Herramienta | Propósito |
|---|---|
| xUnit + FluentAssertions + NSubstitute | Tests de backend (unitarios e integración) |
| Testcontainers for PostgreSQL | La suite de backend corre contra el **mismo motor** que producción |
| Vitest + Testing Library | Tests de frontend |
| Oxlint | Lint del cliente |
| GitHub Actions | CI (build, lint, tests, `npm audit`) y despliegue por tag |
| markdownlint + `validar_pipeline_kb.py` | Gate bloqueante de la base de conocimiento |
| Azure App Service + PostgreSQL Flexible Server | Alojamiento de producción (España, Spain Central) |

---

## Instalación y ejecución

> Guía compacta para levantar el entorno completo. El detalle —variables de configuración una a una,
> creación de la credencial de Google paso a paso, cuenta de envío de correo, migraciones y esquema de
> base de datos— está en
> [`docs/05-infraestructura/desarrollo-local.md`](./docs/05-infraestructura/desarrollo-local.md).

### Requisitos previos

| Herramienta | Versión | Notas |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | `dotnet --version` |
| [Node.js](https://nodejs.org/) | 22 LTS | La misma que usa el CI |
| [PostgreSQL](https://www.postgresql.org/download/) | 15+ | Local o en Docker |
| [Docker](https://www.docker.com/) | 27.x | **Obligatorio para los tests de backend** (Testcontainers) |
| [Git](https://git-scm.com/) | 2.x | — |
| Proyecto en Google Cloud | — | Credencial OAuth 2.0 para el login |
| Cuenta SMTP | — | Opcional en local: sin ella los correos se registran en la traza |

### 1. Clonar el repositorio

```bash
git clone https://github.com/AndresGilabert/terrenario.git
cd terrenario
git checkout develop
```

### 2. Credencial de Google OAuth 2.0

En [Google Cloud Console](https://console.cloud.google.com/) → **APIs y servicios → Credenciales → ID
de cliente OAuth 2.0**, tipo **Aplicación web**:

- Orígenes de JavaScript autorizados: `http://localhost:5173`
- URIs de redireccionamiento autorizados: `http://localhost:5173/auth/callback`

Guarda el **Client ID** y el **Client Secret**: los necesitas en los pasos 4 y 5.

### 3. Par de claves RSA para firmar los JWT

```bash
openssl genrsa -out jwt_private.pem 2048
openssl rsa -in jwt_private.pem -pubout -out jwt_public.pem
```

En Windows, OpenSSL viene con Git for Windows: `$env:Path += ";C:\Program Files\Git\usr\bin"`.

> No commitees estos ficheros. El `.gitignore` ya excluye `*.pem`.

### 4. Base de datos y secretos del backend

Crea la base de datos (`CREATE DATABASE terrenario_dev;`) o levántala con Docker:

```bash
docker run --name terrenario-pg -e POSTGRES_DB=terrenario_dev -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:15
```

Los secretos van en **dotnet User Secrets**, nunca en ficheros versionados:

```bash
cd src/backend/Terrenario.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=terrenario_dev;Username=postgres;Password=postgres"
dotnet user-secrets set "Auth:Google:ClientId" "TU_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Auth:Google:ClientSecret" "TU_CLIENT_SECRET"
dotnet user-secrets set "Auth:Jwt:PrivateKeyPem" "$(cat ../../../jwt_private.pem)"
dotnet user-secrets set "Auth:Jwt:PublicKeyPem" "$(cat ../../../jwt_public.pem)"
```

En PowerShell, las dos últimas líneas usan `(Get-Content ..\..\..\jwt_private.pem -Raw)` en lugar de
`$(cat …)`. Comprueba el resultado con `dotnet user-secrets list`.

Para que las invitaciones salgan por correo hacen falta además `Email:Host`, `Email:Username`,
`Email:Password` y `Email:FromAddress`; sin ellas el backend arranca igual y deja el correo en la
traza. Ver la sección de cuenta de envío en
[`desarrollo-local.md`](./docs/05-infraestructura/desarrollo-local.md).

### 5. Entorno del frontend

```bash
cd src/frontend/terrenario-web
cp .env.example .env
```

Rellena en `.env` el `VITE_GOOGLE_CLIENT_ID` (el mismo del paso 2) y deja
`VITE_API_BASE_URL=http://localhost:5127`.

### 6. Arrancar

```bash
cd src/backend/Terrenario.Api
dotnet run
```

En modo Development el backend aplica las migraciones pendientes al arrancar, expone la API en
`http://localhost:5127` y el contrato OpenAPI en `http://localhost:5127/openapi/v1.json`.

```bash
cd src/frontend/terrenario-web
npm install
npm run dev
```

La aplicación queda en `http://localhost:5173`.

### 7. Comprobar que todo está en pie

```bash
curl http://localhost:5127/api/v1/health
```

Y en el navegador: entra en `http://localhost:5173`, pulsa **Empezar gratis con Google**, autoriza con
tu cuenta y el asistente te llevará a crear tu primer Workspace y su primera temporada.

---

## Estructura del proyecto

```text
terrenario/
├── .github/
│   ├── workflows/                CI, validación de la KB y despliegue por tag
│   └── prompts/                  Prompts de las fases de producto y diseño
├── docs/                         Base de conocimiento (KB) — ver bloque dedicado
│   ├── 00-meta/                  Convenciones, plantillas y scripts de validación
│   ├── 01-producto/              Visión, requisitos, reglas de negocio, roadmap, KPIs
│   ├── 02-arquitectura/          C4, ADRs, modelo de datos, contratos de API, stack
│   ├── 03-modulos/               Fichas de los seis módulos (Bounded Contexts)
│   ├── 04-ingenieria/            Estándares de código, flujo Git, testing, code review
│   ├── 05-infraestructura/       Entornos, CI/CD, desarrollo local, runbooks, DR
│   ├── 06-integraciones/         Sistemas externos y correos del producto
│   ├── 07-seguridad/             Modelo de seguridad, autenticación, privacidad, RGPD
│   ├── 08-procesos/              DoR/DoD, gate de salida, releases, incidentes
│   ├── 09-desarrollos/epicas/    Épicas MVP-001…MVP-008 con spec y tech-design por historia
│   ├── 10-releases/              Notas de release por versión (hitos A–H)
│   └── 99-glosario/              Lenguaje ubicuo del dominio
├── src/
│   ├── backend/
│   │   ├── Terrenario.Api/
│   │   │   ├── Domain/           Entidades y reglas de negocio por dominio
│   │   │   ├── Application/      Servicios de aplicación y casos de uso
│   │   │   ├── Infrastructure/   EF Core, autenticación, correo, telemetría, retención
│   │   │   ├── Controllers/      Endpoints REST de /api/v1
│   │   │   └── Common/           Contrato de error, ámbito de Workspace, utilidades HTTP
│   │   ├── Terrenario.Api.Tests/ Suite xUnit, organizada por dominio + integración
│   │   └── Terrenario.sln
│   └── frontend/terrenario-web/
│       ├── src/
│       │   ├── components/       Vistas y componentes, agrupados por dominio funcional
│       │   ├── contexts/         Sesión, Workspace activo, temporada, avisos, cliente HTTP
│       │   ├── routes/           Guardas de sesión y de Workspace
│       │   ├── services/         Clientes de la API, uno por recurso
│       │   ├── lib/              Utilidades: estado en la URL, fechas, PKCE, telemetría
│       │   ├── config/           Identidad legal publicada (LSSI)
│       │   └── generado/         Subconjunto de iconos y tipografías generado en el build
│       └── scripts/              Generación de iconos y presupuesto de peso de primera carga
├── infra/azure/                  Scripts de aprovisionamiento del entorno de producción
├── prototype/                    Prototipos de diseño (referencia visual)
├── artifacts/                    Evidencias de ejecución (correos generados y enviados)
├── AGENTS.md                     Reglas para agentes de IA que trabajan sobre la KB
└── CONTRIBUTING.md               Cómo contribuir: ramas, commits, PRs
```

El reparto en módulos del backend y del cliente, y qué épica construyó cada uno, está en
[`docs/03-modulos/_vision-general.md`](./docs/03-modulos/_vision-general.md).

---

## Funcionalidades

El producto se organiza en seis módulos. Cada ficha enlaza los diseños técnicos de las historias que lo
construyeron.

| Módulo | Qué cubre |
|---|---|
| [`identidad-y-workspaces`](./docs/03-modulos/identidad-y-workspaces/README.md) | Login con Google, sesión, ciclo de vida del Workspace, invitaciones, membresía y baja de cuenta |
| [`maestros-operativos`](./docs/03-modulos/maestros-operativos/README.md) | Terrenos, temporadas, trabajadores, catálogo de tareas y onboarding |
| [`diario-y-operativa`](./docs/03-modulos/diario-y-operativa/README.md) | Actividades, compras, imputaciones y Diario de campo unificado |
| [`produccion-y-dashboard`](./docs/03-modulos/produccion-y-dashboard/README.md) | Cosechas, catálogo de destinos y Visión General con los KPI de campaña |
| [`plataforma-de-aplicacion`](./docs/03-modulos/plataforma-de-aplicacion/README.md) | Contrato de error, concurrencia, acceso a datos, cliente HTTP, shell y presencia pública |
| [`observabilidad`](./docs/03-modulos/observabilidad/README.md) | Embudo de login, métricas de uso, SLO, señales operativas y alertas |

### Acceso e identidad

- **Login con Google OIDC** mediante flujo de código con PKCE. No hay contraseña local ni acceso
  anónimo: la autenticación es obligatoria desde el primer día.
- **Sesión propia**: access token JWT firmado con RS256 en memoria del cliente y refresh token en
  cookie `HttpOnly`, `SameSite=Strict`.
- **Páginas legales públicas** (privacidad y términos), accesibles antes de entrar.
- **Baja de cuenta** con anonimización inmediata de los datos operativos y retención separada de las
  evidencias legales, según RGPD/LOPDGDD.

### Workspaces y colaboración

- Creación de varios Workspaces por usuario y **selector de contexto activo** siempre visible.
- **Invitaciones por email y por enlace**, con bandeja de invitaciones recibidas y aceptación no
  bloqueante.
- **Gestión de miembros**: alta, revocación, salida voluntaria del Workspace y garantías para que
  ninguno se quede sin propietario ni vacío.
- **Ciclo de vida del Workspace**: renombrado, baja y solicitud de reactivación, avisada tanto por
  correo como dentro de la aplicación.
- **Permisos planos**: todos los miembros ven y editan todos los registros del Workspace. Los roles
  granulares están fuera del alcance del MVP.

### Maestros operativos

- **Terrenos** propios o cedidos, con ubicación, número de árboles y metadatos de suelo, y ficha de
  detalle con su histórico.
- **Temporadas** con una única activa por Workspace, y temporada de trabajo propia por usuario.
- **Trabajadores** con o sin cuenta en la plataforma; los miembros del Workspace aparecen siempre como
  posibles responsables.
- **Catálogo de tareas** por Workspace, que aprende automáticamente las tareas escritas a mano.
- **Depuración de maestros**: borrado de los que nunca se usaron y fusión de fichas duplicadas,
  reapuntando su histórico.
- **Onboarding**: al crear el primer Workspace se ofrece crear su primera temporada.

### Operativa diaria

- **Registro de actividades**: qué se hizo, en qué terreno, quién fue el responsable, cuántas horas y
  qué coste manual.
- **Compras** de materiales (producto, cantidad y coste total) e **imputación de consumos** por
  terreno, con aviso —sin bloquear— cuando se consume un producto sin compra previa.
- **Diario de campo cronológico y unificado**, que mezcla actividades, compras, consumos y cosechas en
  un único eje temporal, con navegación y filtros conservados en la URL.
- **Autoría visible**: cada registro muestra quién lo creó y quién lo modificó por última vez.
- **Borrado con confirmación** y control optimista de concurrencia (HTTP 409 ante edición simultánea).

### Producción y dashboard

- **Registro de cosechas** con peso bruto obligatorio y `rendimiento` o `litros` opcionales y
  mutuamente excluyentes, sobre un catálogo cerrado de destinos.
- **Aviso de cosecha duplicada**: si ya existe una partida del mismo terreno, fecha y producto, el
  formulario lo advierte nombrándola, sin impedir el registro.
- **Visión General** en una sola pantalla con cuatro widgets: resumen de temporada (kg, litros,
  rendimiento medio, kg/árbol), kg por destino, kg por terreno y evolución del rendimiento.
- **Valor económico de la campaña** a partir de las actividades y compras imputadas.
- **Filtros persistentes en la URL** —recargar no deshace el trabajo y la vista filtrada se puede
  compartir— y marca explícita de *dato incompleto* cuando falta información para un KPI.

### Plataforma y operación

- **Contrato de error único**: toda respuesta de error de la API viaja como JSON con el mismo
  envoltorio, incluidos los 404 de enrutado.
- **Maqueta adaptada** a móvil, tableta y escritorio, con modales accesibles y respuesta explícita a la
  pérdida de conexión.
- **Presupuesto de peso de primera carga**, comprobado en el `build`: la carga inicial bajó de 4,6 MB a
  881 kB al generar un subconjunto propio de la fuente de iconos.
- **Canal de feedback** dentro de la aplicación para sugerencias e incidencias.
- **Observabilidad**: embudo de login, métricas de uso del dashboard, señales de degradación y alertas
  por correo.

> El detalle funcional por historia —criterios de aceptación, decisiones y alternativas descartadas—
> vive en [`docs/09-desarrollos/epicas/`](./docs/09-desarrollos/epicas/). Lo entregado en cada hito se
> resume en [`docs/10-releases/`](./docs/10-releases/).

---

## Tests y calidad

```bash
# Backend — requiere Docker en marcha (Testcontainers levanta PostgreSQL)
dotnet test src/backend/Terrenario.sln

# Frontend
cd src/frontend/terrenario-web
npm test          # Vitest
npm run lint      # Oxlint
npm run build     # tsc -b + vite build (falla si se excede el presupuesto de peso)
```

A cierre del Hito H (`v0.8.0`) la suite eran **1.051 tests de backend** y **355 de cliente**, con
`build -warnaserror` sin advertencias. La estrategia —niveles, cobertura objetivo y casos obligatorios—
está en [`docs/04-ingenieria/estrategia-testing.md`](./docs/04-ingenieria/estrategia-testing.md).

Además de código, el CI valida la base de conocimiento y **bloquea el PR** si falla:

```bash
python docs/00-meta/scripts/validar_pipeline_kb.py --solo-cambios --base-ref origin/develop --check-indices-clean
```

Ese pipeline comprueba estructura y frontmatter, que los `_indice.md` de las épicas estén regenerados,
que ningún requisito marcado como MVP se quede sin destino, y pasa markdownlint sobre la documentación
—este `README.md` incluido—.

---

## Despliegue

Producción vive en **Azure, región España (Spain Central)**: App Service Linux + PostgreSQL Flexible
Server, con **un solo origen** —la API sirve también el cliente—, condición necesaria para que la
cookie de refresco `SameSite=Strict` sobreviva.

El despliegue **no se dispara al mergear a `main`**, sino al publicar un tag `v*`: el workflow exige el
gate de CI en verde sobre ese commit, espera aprobación humana en el entorno `produccion` y ejecuta un
smoke contra `/api/v1/health` y las páginas legales. La autenticación con Azure usa identidad federada
OIDC, sin secretos almacenados en GitHub.

Detalle en [`docs/05-infraestructura/ci-cd.md`](./docs/05-infraestructura/ci-cd.md),
[`docs/05-infraestructura/entornos.md`](./docs/05-infraestructura/entornos.md) y los
[runbooks de operación](./docs/05-infraestructura/runbooks/).

---

## Base de conocimiento (KB)

Todo el proyecto —producto, arquitectura, ingeniería, seguridad y desarrollos— está documentado en
[`docs/`](./docs/), con un punto de entrada y una guía de navegación en
[`docs/00-meta/README.md`](./docs/00-meta/README.md).

| Necesito… | Empieza por |
|---|---|
| Entender el negocio y el alcance | [`01-producto/vision-y-objetivos.md`](./docs/01-producto/vision-y-objetivos.md) |
| Entender la arquitectura | [`02-arquitectura/vision-general.md`](./docs/02-arquitectura/vision-general.md) |
| Saber por qué se decidió algo | [`02-arquitectura/decisiones/`](./docs/02-arquitectura/decisiones/) |
| Trabajar sobre un dominio funcional | [`03-modulos/_vision-general.md`](./docs/03-modulos/_vision-general.md) |
| Escribir código con los estándares del proyecto | [`04-ingenieria/estandares-codigo.md`](./docs/04-ingenieria/estandares-codigo.md) |
| Montar el entorno o desplegar | [`05-infraestructura/desarrollo-local.md`](./docs/05-infraestructura/desarrollo-local.md) |
| Tocar datos personales o autenticación | [`07-seguridad/modelo-seguridad.md`](./docs/07-seguridad/modelo-seguridad.md) |
| Saber cuándo una historia está lista o terminada | [`08-procesos/definition-of-ready.md`](./docs/08-procesos/definition-of-ready.md) |
| Buscar una funcionalidad concreta | [`09-desarrollos/epicas/`](./docs/09-desarrollos/epicas/) |
| Consultar un término del dominio | [`99-glosario/glosario.md`](./docs/99-glosario/glosario.md) |
| Trabajar con un agente de IA sobre este repo | [`AGENTS.md`](./AGENTS.md) |

---

## Solución de problemas frecuentes

### «REPLACE_IN_SECRETS» al arrancar el backend

Los User Secrets no están configurados. Repasa el [paso 4](#4-base-de-datos-y-secretos-del-backend) y
comprueba con `dotnet user-secrets list`.

### «redirect_uri_mismatch» en la pantalla de Google

La URL de callback no coincide con la registrada. Verifica que `http://localhost:5173/auth/callback`
está entre los URIs de redireccionamiento autorizados en Google Cloud Console.

### «Error establishing a database connection»

PostgreSQL no está corriendo o la cadena de conexión es incorrecta. Comprueba con
`psql -h localhost -U postgres -d terrenario_dev`.

### La migración falla con «role does not exist»

El usuario de PostgreSQL configurado no existe. Ajusta `Username` y `Password` en los User Secrets.

### El frontend no llega al backend (CORS o error de red)

Verifica que `VITE_API_BASE_URL` apunta a `http://localhost:5127` y que el backend está arrancado.

### Los tests de backend fallan al arrancar los contenedores

La suite necesita **Docker en marcha**: usa Testcontainers para levantar un PostgreSQL real. En Windows
puede bloquearlo la política de Application Control del equipo.

### El backend no compila porque el `.exe` está bloqueado

Hay una instancia escuchando en el 5127. Párala antes de compilar o lanzar los tests.

### Errores de tipos en el frontend

```bash
cd src/frontend/terrenario-web
npx tsc --noEmit
```

---

## Contribuir

El flujo de ramas, el formato de commits y el proceso de PR están en
[`CONTRIBUTING.md`](./CONTRIBUTING.md) y en
[`docs/04-ingenieria/flujo-git.md`](./docs/04-ingenieria/flujo-git.md). Antes de abrir un PR conviene
pasar en local todo lo que valida el CI: tests de backend, `build` y `lint` del cliente, y el pipeline
de la KB.

---

## Licencia

Distribuido bajo los términos del fichero [`LICENSE`](./LICENSE). Ver también [`NOTICE`](./NOTICE).
