---
bloque: 07-seguridad
documento: privacidad-datos
actualizado_en: "2026-06-30"
---

# Privacidad de Datos y GDPR

> **IMPORTANTE para agentes de IA**: Antes de generar código que maneje datos de usuarios,
> leer este documento. Cualquier dato PII requiere tratamiento especial.
>
> **Politica de cumplimiento obligatorio**: Todo el proyecto debe cumplir en todo momento la normativa europea y espanola de proteccion de datos aplicable.
> Lo obligatorio no es negociable ni puede relajarse por criterios de plazo, coste o conveniencia tecnica.

---

## Marco normativo aplicable

### Obligatorio siempre (base legal minima del proyecto)

| Norma | Ambito | Estado de cumplimiento |
|-----------|---------|-------------|
| Reglamento (UE) 2016/679 (RGPD/GDPR) | Tratamiento de datos personales de personas en la UE | **Obligatorio** |
| Ley Organica 3/2018 (LOPDGDD, Espana) | Desarrollo nacional del RGPD y derechos digitales en Espana | **Obligatorio** |

### Obligatorio segun escenario (condicionado)

| Norma | Cuando aplica | Estado |
|-----------|---------|-------------|
| Ley 34/2002 (LSSI-CE, Espana) | Servicios de la sociedad de la informacion, comunicaciones electronicas y uso de cookies/tecnologias similares | **Obligatorio si aplica** |
| Directiva ePrivacy 2002/58/CE (y transposicion nacional) | Confidencialidad de comunicaciones y reglas de cookies/trackers | **Obligatorio si aplica** |
| Evaluacion de Impacto en Proteccion de Datos (EIPD, RGPD art. 35) | Tratamientos de alto riesgo para derechos y libertades | **Obligatorio si aplica** |
| Notificacion de brechas a autoridad y afectados (RGPD arts. 33 y 34) | Violacion de seguridad de datos personales | **Obligatorio si aplica** |

### Recomendado (no sustituye obligaciones legales)

| Referencia | Tipo | Estado |
|-----------|---------|-------------|
| Guias AEPD (cookies, evaluacion de riesgos, anonimización) | Guia interpretativa | Recomendado |
| ISO/IEC 27001 e ISO/IEC 27701 | Buenas practicas certificables | Recomendado |
| NIST Privacy Framework | Buenas practicas | Recomendado |

---

## Reglas de cumplimiento transversal

1. Todo nuevo requisito funcional, tecnico o de datos debe analizar impacto en RGPD + LOPDGDD antes de aprobarse.
2. Si una funcionalidad no puede cumplir una obligacion legal aplicable, no entra en desarrollo.
3. Si una norma es "obligatoria si aplica", el ticket debe dejar evidencia de si aplica o no, con justificacion.
4. Ningun PR que trate datos personales puede aprobarse sin validar esta politica.

## Clasificación de datos

| Categoría | Ejemplos | Tratamiento |
|-----------|---------|-------------|
| **PII básico** | Nombre, email, teléfono | Cifrado en reposo, acceso restringido |
| **PII sensible** | Datos bancarios, documentos de identidad | Cifrado en reposo + en tránsito, acceso muy restringido |
| **Datos de comportamiento** | Logs de uso, historial | Minimizacion, pseudonimizacion y/o anonimizacion segun finalidad |
| **Datos públicos** | IDs, referencias | Sin restricciones especiales |

## Reglas especificas para autenticacion social

Cuando se use un proveedor externo de identidad (por ejemplo Google):

1. Solo se recogeran los datos estrictamente necesarios para crear y mantener la cuenta.
2. Se documentara el origen de los datos y la base juridica del tratamiento.
3. Los tokens y credenciales del proveedor no se almacenaran en claro en logs, URLs ni mensajes de error.
4. Si el proveedor entrega atributos adicionales no necesarios, se descartaran por defecto.
5. Cualquier ampliacion a otros proveedores debera revisarse antes de activarse para confirmar cumplimiento RGPD + LOPDGDD.

---

## Principios GDPR aplicados

| Principio | Implementación |
|-----------|---------------|
| **Minimización** | Solo recoger los datos necesarios para el servicio |
| **Limitación de almacenamiento** | Política de retención activa (ver tabla abajo) |
| **Exactitud** | El usuario puede corregir sus datos |
| **Integridad y confidencialidad** | Cifrado + control de acceso |
| **Responsabilidad** | Logs de auditoría para accesos a PII |

## Base de legitimacion del tratamiento (RGPD art. 6)

Todo tratamiento de datos personales debe mapearse a una base juridica valida antes de implementarse:

| Base juridica | Uso esperado en proyecto |
|---------|---------|
| Ejecucion de contrato | Operativa principal del servicio solicitado por el usuario |
| Cumplimiento de obligacion legal | Conservacion legal de ciertos registros, cuando corresponda |
| Interes legitimo | Solo tras test de ponderacion documentado |
| Consentimiento | Casos especificos (ej. cookies no tecnicas, marketing), siempre revocable |

Si no existe base juridica valida, el tratamiento queda prohibido.

---

## Política de retención de datos

| Tipo de dato | Retención | Acción al expirar |
|-------------|-----------|------------------|
| Datos de cuenta activa | Duración de la cuenta | — |
| Datos de cuenta cancelada | 24 meses tras cancelación | Anonimización / borrado |
| Logs de transacciones de pago | 5 años (si existe obligacion legal aplicable al caso) | Archivado seguro |
| Logs de acceso / auditoría | 12 meses | Borrado |
| Datos de comportamiento | 6 meses | Anonimización |

---

## Derechos del usuario (GDPR Art. 15-22)

| Derecho | Proceso |
|---------|---------|
| Acceso | Procedimiento DSAR con registro de solicitud y respuesta en plazo legal |
| Rectificación | Correccion de datos inexactos por solicitud del titular |
| Supresión (derecho al olvido) | Borrado/anonimizacion cuando proceda legalmente |
| Portabilidad | Exportacion estructurada en formato interoperable |
| Oposición al tratamiento | Evaluacion de base juridica y bloqueo del tratamiento cuando corresponda |
| Limitacion del tratamiento | Marcado de restriccion temporal en sistemas afectados |

Plazo de referencia operativo para respuesta a derechos: 1 mes (prorrogable en casos complejos con justificacion).

---

## Checklist obligatorio por ticket/feature con datos personales

- [ ] Identificado si hay datos personales (si/no, con evidencia)
- [ ] Identificada base juridica del tratamiento
- [ ] Verificado principio de minimizacion
- [ ] Definida retencion y borrado/anonimizacion
- [ ] Verificado impacto en derechos del titular
- [ ] Verificado si aplica EIPD
- [ ] Verificado si aplica LSSI-CE / ePrivacy (cookies, comunicaciones)
- [ ] Actualizada documentacion funcional/tecnica de cumplimiento

---

## Lo que NO hacer con datos PII

- No loguear PII en logs de aplicación o errores
- No incluir PII en URLs (query params o paths)
- No almacenar datos de tarjeta — usar tokens del PSP (Stripe token / vault)
- No enviar PII en mensajes de error devueltos al cliente
- No incluir PII en los tests (usar datos sintéticos)

---

## Nota de gobernanza

Este documento es normativa interna de cumplimiento del proyecto. No sustituye asesoramiento juridico profesional, pero su cumplimiento es obligatorio para todo el equipo.
