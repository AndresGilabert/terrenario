---
id: "MVP-713"
tipo: feature
titulo: "Errores de OAuth y ruido en las alertas"
estado: borrador
prioridad: media
sprint: ""
hito: "Hito G — Ajustes de uso real"
esfuerzo_estimado: "1d"
tickets: []
epica: "MVP-007--ajustes-mvp-01"
depende_de: []
bloquea: []
relacionado_con: ["MVP-999"]
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["autenticacion", "observabilidad"]
  modulo_path: "03-modulos/"
  componentes: ["google-oidc", "slo", "alertas"]
  etiquetas: ["mvp", "ajustes", "bug"]
  nivel_riesgo: medio
creado_en: "2026-08-07"
actualizado_en: "2026-08-07"
---

# MVP-713 — Errores de OAuth y ruido en las alertas

> **Origen**: `P-079` del registro de `MVP-999`, clasificados por el Product Owner en la revision
> completa del MVP (2026-08-06/07).

## Contexto

`GoogleOidcService` mapea **cualquier** respuesta no exitosa del endpoint de token de Google a
`AUTH_GOOGLE_EXCHANGE_FAILED`, que `AuthController` traduce a **HTTP 500**. Eso incluye `invalid_grant`,
que es lo que Google devuelve ante un codigo ya usado o expirado: basta con recargar la pantalla de
callback para provocarlo.

Consecuencias verificadas en la revision de `MVP-699`: cuenta contra el SLO de tasa de error (objetivo
< 0,1 %) y contra `HighErrorRate`, que es **critica**. Un solo 500 de este tipo sobre 70 peticiones dio
1,43 % y disparo la alerta, con envio de correo real.

Es comportamiento anterior a la epica de observabilidad (`MVP-101`/`MVP-502`), pero solo tiene
consecuencia desde `MVP-603`, porque antes no lo miraba nadie.

## Objetivo

Que un error del cliente deje de contarse como fallo del servidor, para que las alertas criticas solo
salten cuando pasa algo de verdad.

## Requisitos de usuario

### HU-1 — Alertas en las que se pueda confiar

**Como** responsable tecnico,
**quiero** que `HighErrorRate` no salte porque alguien recargo la pantalla de vuelta de Google,
**para** que siga sirviendo cuando el problema sea real.

## Alcance (in-scope)

- Distinguir el vocabulario cerrado de errores de OAuth 2.0: `invalid_grant` e `invalid_request` son de
  cliente (401/400); `invalid_client`, `unauthorized_client` y las caidas de Google son de servidor
  (500).
- Mensaje util al usuario en el caso de cliente: el codigo ha caducado o ya se uso, hay que volver a
  entrar.
- Que los casos de cliente dejen de contar en el SLO de tasa de error.

## Fuera de alcance (out-of-scope)

- Cambiar el flujo de autenticacion o el proveedor.
- Revisar el resto de codigos de error de la API.
- Redefinir umbrales de alerta o SLOs: el problema es la clasificacion, no el umbral.

## Criterios de aceptación

- [ ] **CA-1**: Recargar la pantalla de callback con un codigo ya usado devuelve 400/401, no 500, y la
  pantalla lo explica.
- [ ] **CA-2**: Un fallo de configuracion (`invalid_client`) o una caida de Google siguen devolviendo
  500, que es lo que son.
- [ ] **CA-3**: Los casos de cliente no incrementan la tasa de error del SLO.
- [ ] **CA-4**: Reproducido el escenario que disparo la alerta en `MVP-699` y comprobado que ya no la
  dispara.
- [ ] **CA-5**: Test de regresion sobre el mapeo de codigos de OAuth.

## Maquetas y referencias visuales

No aplica: es comportamiento de servidor y de la pantalla de vuelta de Google.

## Checklist de implementacion (prototipo + KB)

| Pantalla prototipo | Regla KB asociada | Estado | Evidencia de prueba |
|---|---|---|---|
| LoginPage / callback | docs/05-infraestructura/observabilidad.md | falta | Un 500 evitable dispara alerta critica |

## Notas y decisiones

- Una alerta critica que salta sin motivo se acaba ignorando **tambien cuando el motivo es real**. Ese
  es el dano de este punto, no el codigo de estado.
