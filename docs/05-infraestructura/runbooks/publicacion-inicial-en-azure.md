---
bloque: 05-infraestructura
documento: publicacion-inicial-en-azure
actualizado_en: "2026-08-05"
---

# Runbook — Publicación inicial en Azure

Todo lo que hay que crear y configurar **una sola vez** para que un tag `v*` publique el producto.
A partir de ahí publicar es poner un tag, y este documento solo se vuelve a abrir si algo cambia.

Dominio: **`terrenario.com`** → **`app.terrenario.com`**, un solo origen que sirve cliente y API.

> **Ejecutado el 2026-08-05.** La infraestructura existe y `v0.5.0-hito-e` está publicado. Este
> documento queda como referencia de cómo se montó y para reproducirlo en otro entorno; los avisos en
> bloque de cita son los tropiezos reales del primer montaje, no advertencias teóricas.

---

## 0. La decisión que condiciona el resto

**El cliente y la API tienen que compartir dominio registrable.** No es preferencia: la cookie de
refresco es `SameSite=Strict` (`AuthController`), y `algo.azurestaticapps.net` y
`algo.azurewebsites.net` son **sitios distintos** —los dos sufijos están en la Public Suffix List—,
así que el navegador no enviaría esa cookie al renovar la sesión.

El síntoma sería desagradable de diagnosticar: **todo funciona 15 minutos** —lo que dura el token de
acceso— y después la aplicación echa a la persona fuera sin ningún error visible.

**Cómo quedó resuelto**: al montarlo apareció que Azure Static Web Apps **no tiene región europea
abierta a altas nuevas** —sus únicas cinco regiones son Central US, East US 2, West US 2, West Europe
y East Asia, y West Europe está cerrada—. Servir el cliente desde EE. UU. habría hecho falsas dos
frases de la Política de Privacidad ya publicada.

La salida fue mejor que el plan original: **la propia API sirve el cliente**. Un solo origen en
`https://app.terrenario.com`, todo en Spain Central, y el problema de la cookie desaparece de raíz
porque ya no hay nada cross-site. De paso sobra el CORS y hay un recurso menos que pagar.

---

## 1. Coste, y cómo bajarlo

Cifras **aproximadas** de pago por uso en Spain Central; conviene contrastarlas con la calculadora de
Azure antes de comprometerse.

| Recurso | Plan | Aproximado |
|---------|------|-----------:|
| App Service Plan Linux | B1 | ~13 €/mes |
| PostgreSQL Flexible Server | B1ms + 32 GB | ~16 €/mes |
| **Total** | | **~29 €/mes** |

Tres decisiones ya tomadas para no gastar de más:

- **Sin recurso de hosting estático** (−8 €/mes frente al plan inicial). El cliente lo sirve la API.
- **App Service `B1` y no `F1`/`D1`.** Aquí no hay margen: son los planes que **no admiten dominio
  propio con TLS**, y sin HTTPS la cookie de refresco no puede ser `Secure`. El paso 0 lo impide.
- **PostgreSQL `B1ms`**, el escalón más bajo.

### Cómo bajar más

| Palanca | Ahorro | Qué implica |
|---------|-------:|-------------|
| **Suscripción nueva de Azure** | hasta −16 €/mes durante 12 meses | La oferta gratuita incluye PostgreSQL Flexible Server B1ms (750 h) y 32 GB. **Compruébalo antes de nada**: si la suscripción es nueva, el coste baja a ~13 €/mes el primer año |
| **Reserva de 1 año en la base** | ~−35 % del cómputo | Compromiso de pago por adelantado. Solo tiene sentido si ya sabes que sigue viva dentro de un año |
| **Parar la base fuera de horario** | hasta −50 % del cómputo | Flexible Server se puede detener hasta 7 días. **No lo recomiendo con usuarios reales validando**: la aplicación dejaría de funcionar sin aviso |

### Lo que descarté, y por qué

- **Escalado a cero** (Container Apps con `min=0`, planes gratuitos de Render o Railway): parece la
  opción evidente y **rompe la rutina de expurgo**. `RetentionPurgeWorker` solo corre mientras el
  proceso está vivo; con la aplicación dormida, `RN-041` deja de cumplirse en silencio. Si algún día
  se va por ahí, el expurgo tiene que salir de la API antes.
- **Salir de Azure** (Hetzner ~5 €/mes, Fly.io ~12 €/mes): más barato, pero **no es gratis**. La
  Política de Privacidad publicada nombra a Microsoft Azure y la región de España, y `B-1`/`B-2` del
  gate se cerraron sobre esa base. Cambiar de proveedor obliga a rehacer la política, el inventario
  de `privacidad-datos.md`, la checklist de cumplimiento y el contrato de encargo del art. 28.
  Ahorrar 20 € al mes a cambio de reabrir cumplimiento no compensa **ahora**; si el coste llega a ser
  un problema, Fly.io tiene región en Madrid y sería el candidato, porque mantiene cierta la
  afirmación de que los datos están en España.

**Recomendación**: quedarse en Azure y comprobar la elegibilidad de la oferta gratuita de 12 meses.
Con eso el primer año sale por el precio del App Service.

---

## 2. Crear la infraestructura

Por comandos y no por el portal, por dos motivos: un montaje a mano no se puede repetir igual, y el
primer intento casi nunca sale entero. Los scripts son **idempotentes**: comprueban cada recurso
antes de crearlo, así que volver a ejecutarlos tras un fallo continúa donde se quedaron.

```bash
az login && az account set --subscription "TU_SUSCRIPCION"
```

```bash
export PG_PASSWORD='...' && ./infra/azure/crear-infraestructura.sh
```

Crea el grupo de recursos, PostgreSQL con su base y su regla de acceso desde servicios de Azure, y el
plan y el App Service con HTTPS obligatorio y TLS 1.2. Al terminar imprime los **registros DNS** que
hay que crear.

Todo va a **España (Spain Central)**.

> La región no es cosmética. La Política de Privacidad declara que los datos se alojan en España, así
> que crear la base en otra región convertiría ese documento en falso. El script lo comprueba.

---

## 3. DNS

En el proveedor de `terrenario.com`, con los valores que imprimió el script:

| Registro | Tipo | Valor |
|----------|------|-------|
| `app` | CNAME | `app-terrenario-api.azurewebsites.net` |
| `asuid.app` | TXT | *(el ID de verificación que imprime el script)* |

Comprueba la propagación antes de seguir:

```bash
dig +short app.terrenario.com
```

Después:

```bash
./infra/azure/enlazar-dominios.sh
```

Enlaza el dominio y **emite el certificado gestionado**, que es gratuito y se renueva solo. El script comprueba el DNS primero: si no ha propagado, se detiene sin dejar nada a medias.

---

## 4. Configuración de la API

```bash
export PG_PASSWORD='...' GOOGLE_CLIENT_ID='...' GOOGLE_CLIENT_SECRET='...' \
       EMAIL_HOST='...' EMAIL_USERNAME='...' EMAIL_PASSWORD='...' EMAIL_FROM='...' \
       JWT_PRIVATE_PEM="$(cat jwt-private.pem)" JWT_PUBLIC_PEM="$(cat jwt-public.pem)" \
  && ./infra/azure/configurar-api.sh
```

El par de claves RSA se genera antes con:

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out jwt-private.pem && openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem
```

> Guarda la clave privada donde guardes las contraseñas y **bórrala del disco**. Quien la tenga puede
> emitir tokens válidos para cualquier cuenta.

El script se niega a escribir nada si falta alguna variable, para no dejar el App Service configurado
a medias. Y pone `Cors__AllowedOrigins__0` con el `__0`, que es **el error más fácil de cometer a
mano**: sin el índice la API rechaza al cliente y el navegador muestra un fallo de CORS que parece un
problema de red.

Las **migraciones se aplican solas** en ese reinicio (`Database:MigrateOnStartup`), así que la base
se crea sin intervención. Si una migración falla, la aplicación no arranca: es deliberado, mejor que
servir peticiones contra un esquema equivocado.

```bash
az webapp log tail --resource-group rg-terrenario-prod --name app-terrenario-api
```

---

## 5. GitHub

**Settings → Environments → New environment → `produccion`.** En el entorno y no en el repositorio,
porque así puedes exigir aprobación manual antes de cada publicación (recomendado: marca *Required
reviewers* contigo mismo).

**Sin secretos.** El despliegue usa **identidad federada (OIDC)**: GitHub emite un token de un solo
uso en cada ejecución y Azure solo lo acepta si viene de este repositorio y del entorno `produccion`.
No hay ninguna credencial almacenada que rotar ni que se pueda filtrar.

La alternativa —perfil de publicación— **no funciona**: Azure trae desactivada la autenticación básica
de publicación (`scm` y `ftp` en `false`) desde hace años, así que el perfil llega sin credenciales y
el despliegue falla con «Publish profile is invalid». Reactivarla sería el único punto del montaje
donde se elige la opción menos segura.

El montaje de la identidad, una sola vez:

```bash
az ad app create --display-name "terrenario-deploy"
```

Después: crear el *service principal*, añadir la credencial federada y asignarle el rol
**Website Contributor** acotado a **la aplicación web**, no al grupo de recursos.

> **El sujeto no es el que documenta Microsoft.** GitHub presenta el formato de **identificadores
> inmutables**, con los IDs numéricos de la cuenta y del repositorio:
>
> ```text
> repo:PROPIETARIO@23640134/REPOSITORIO@1303065434:environment:produccion
> ```
>
> Con el formato legible (`repo:PROPIETARIO/REPOSITORIO:environment:produccion`) el login falla con
> `AADSTS700213: No matching federated identity record found`. **El sujeto exacto aparece en el log
> del propio fallo**, en la línea `subject claim`, así que la vía rápida es ejecutar una vez, leerlo
> y crear la credencial con ese valor. Aquí están registradas las dos, por si GitHub cambia de
> criterio.

Los proveedores de recursos hay que registrarlos antes: una suscripción nueva no los trae, y
`Microsoft.Authorization` falla con `MissingSubscription` al asignar el rol.

Variables del entorno (ninguna es secreta):

| Variable | Valor |
|----------|-------|
| `AZURE_CLIENT_ID` | El `appId` del registro de aplicación |
| `AZURE_TENANT_ID` | El identificador del inquilino |
| `AZURE_SUBSCRIPTION_ID` | El identificador de la suscripción |
| `AZURE_WEBAPP_NAME` | `app-terrenario-api` |
| `VITE_GOOGLE_CLIENT_ID` | El identificador de cliente de Google |
| `PUBLIC_WEB_URL` | `https://app.terrenario.com` |

`PUBLIC_WEB_URL` hace de doble uso: es la base de la API para el build del cliente —mismo origen— y
la que usa el smoke. No hace falta una variable aparte.

---

## 6. Google Cloud Console

En **APIs y servicios → Credenciales**, sobre el ID de cliente de OAuth existente:

- **Orígenes autorizados de JavaScript**: añade `https://app.terrenario.com`
- **URI de redirección autorizados**: añade `https://app.terrenario.com/auth/callback`

No borres las de `localhost`: se siguen usando en desarrollo.

> Si la pantalla de consentimiento sigue en **modo de prueba**, solo entrarán las cuentas que figuren
> como usuarios de prueba. Para una validación con pocos usuarios eso vale y evita la verificación de
> Google, pero tenlo presente antes de repartir el enlace.

---

## 7. Publicar

```bash
git checkout main && git pull && git tag -a v0.5.0-hito-e -m "Release v0.5.0 — Hito E: salida controlada a MVP" && git push origin v0.5.0-hito-e
```

`deploy.yml` hace, por este orden:

1. **Comprueba que el gate está en verde** para ese commit. Si el CI falló, no publica: sería
   saltarse el gate de `MVP-504` por la puerta de atrás.
2. Compila el cliente, lo **incrusta** en la publicación de la API y despliega el conjunto.
3. **Smoke**: cabeceras de seguridad de la API, el cliente resuelve una ruta del SPA, la CSP viaja
   como cabecera con `frame-ancestors` —lo que un `meta` no puede hacer— y un endpoint inexistente
   de la API sigue devolviendo `404` en vez de quedar tapado por el cliente.

Si algo falla y lo arreglas, **no hace falta otro tag**: `Actions → Publicar → Run workflow` acepta el
tag como parámetro.

---

## 8. Comprobación manual después del primer despliegue

El smoke automático dice que responde, no que funcione. Esto hay que verlo:

- [ ] `https://app.terrenario.com` carga y la Política de Privacidad se abre **sin iniciar sesión**
- [ ] El acceso con Google completa y entra en la aplicación
- [ ] **Esperar 16 minutos con la pestaña abierta y seguir dentro** — es la comprobación del paso 0,
      la que demuestra que la cookie de refresco viaja. Si el dominio estuviera mal montado, todo lo
      demás daría verde y esto sería lo único que lo delataría
- [ ] Crear un Workspace, registrar una labor y verla en el diario
- [ ] Enviar una invitación por correo y comprobar que llega
- [ ] En Ajustes → Privacidad, el inventario se ve completo
- [ ] En el registro del App Service aparece `Expurgo completado (RN-041)` a los ~5 minutos del
      arranque

---

## 9. Qué queda fuera

- **Copias de seguridad**: PostgreSQL Flexible Server las hace solo (7 días por defecto). Restaurar
  es otro procedimiento; ver [disaster-recovery.md](../disaster-recovery.md).
- **Observabilidad**: sin Application Insights, los registros son los del App Service. Suficiente
  para validar, corto para operar.
- **Entorno de `staging`**: este runbook monta producción directamente, que es la decisión tomada
  para la validación. Duplicar los recursos con sufijo `-stg` es el camino cuando haga falta.

---

## Trazabilidad KB

1. Gate de salida y criterios de promoción: [`../../08-procesos/gate-salida-mvp.md`](../../08-procesos/gate-salida-mvp.md)
2. Proceso de release y rollback: [`../../08-procesos/proceso-release.md`](../../08-procesos/proceso-release.md)
3. Entornos: [`../entornos.md`](../entornos.md)
4. Pipeline: [`../ci-cd.md`](../ci-cd.md)
