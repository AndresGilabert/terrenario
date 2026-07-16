---
bloque: 01-producto
documento: user-journeys
actualizado_en: "2026-06-30"
---

# User Journeys

> Los journeys describen los flujos de usuario más críticos del producto.
> Cada journey debe mapearse desde la perspectiva del usuario, no del sistema.

---

## Journey 1: Revisar estado de temporada en dashboard

**Persona**: Antonio (propietario/gestor de explotacion)
**Objetivo del usuario**: Entender rapidamente estado productivo de la temporada y detectar desviaciones
**Duración estimada**: 1-2 minutos

### Pasos

| Paso | Acción del usuario | Lo que ve/recibe | Emoción | Puntos de fricción |
|------|--------------------|-----------------|---------|-------------------|
| 1 | Entra al dashboard | Vista unica con scroll vertical y 4 widgets | 😊 | Ninguno |
| 2 | Revisa resumen de temporada | Produccion total, litros de aceite, rendimiento medio, kg/arbol | 😊 | Puede aparecer aviso de dato incompleto |
| 3 | Revisa kg por destino | Distribucion por venta de aceituna, aceite para venta, autoconsumo y sin destino | 😐 | Categoria sin destino requiere accion posterior de calidad de dato |
| 4 | Revisa kg por terreno | Barras verticales ordenadas por kg descendente | 😊 | Scroll horizontal si hay muchos terrenos |
| 5 | Revisa evolucion de rendimiento | Serie de temporada y promedio historico disponible | 😐 | Si no hay historico previo, mensaje contextual |
| 6 | Pulsa recarga manual si lo necesita | Datos actualizados manteniendo filtros activos | 😊 | Dependencia de calidad de conectividad |

### Oportunidades de mejora

- Destacar visualmente campos incompletos que impiden KPIs mas precisos.
- Introducir recomendaciones guiadas en base a variaciones de rendimiento (fase posterior).

---

## Journey 2: Registrar cosecha para alimentar dashboard

**Persona**: Antonio
**Objetivo del usuario**: Registrar una cosecha valida para que el dashboard refleje datos fiables
**Duración estimada**: 2-4 minutos

### Pasos

| Paso | Acción del usuario | Lo que ve/recibe | Emoción | Puntos de fricción |
|------|--------------------|-----------------|---------|-------------------|
| 1 | Selecciona terreno y temporada | Formulario de cosecha contextual | 😊 | Ninguno |
| 2 | Introduce unidad principal de cosecha | Validacion de unidad unica por registro | 😐 | Error si mezcla unidades |
| 3 | Introduce destino (o deja sin destino) | Confirmacion de categoria de destino | 😐 | Si queda sin destino, pierde detalle comercial |
| 4 | Introduce datos de rendimiento | Puede informar L/100kg, kg/100kg o kg+litros para calculo | 😐 | Requiere comprender equivalencias |
| 5 | Guarda y vuelve al dashboard | El dato impacta en resumen y graficos tras recarga | 😊 | No hay refresco automatico continuo |

---

## Journey 3: Acceso al sistema sin contrasena (Google Login)

**Persona**: Antonio
**Objetivo del usuario**: Entrar al sistema de forma sencilla sin recordar contrasenas
**Duración estimada**: 30-60 segundos

### Pasos

| Paso | Acción del usuario | Lo que ve/recibe | Emoción | Puntos de fricción |
|------|--------------------|-----------------|---------|-------------------|
| 1 | Llega a pantalla de login | Boton principal "Continuar con Google" | 😊 | Ninguno |
| 2 | Pulsa login con Google | Redireccion segura a proveedor | 😐 | Cambio de contexto puede confundir |
| 3 | Selecciona cuenta Google | Confirmacion de identidad | 😊 | Puede dudar si tiene varias cuentas |
| 4 | Vuelve a la app autenticado | Acceso al dashboard | 😊 | Si falla redireccion, abandono potencial |

### Oportunidades de mejora

- Incluir microcopy claro para publico senior: "No necesitas recordar contrasena".
- Medir abandonos por paso para priorizar mejoras de usabilidad.
- Incorporar Passkeys en fase posterior como acceso aun mas directo.

---

## Flujos de error más comunes

| Flujo | Qué sale mal | Qué experimenta el usuario | Solución actual |
|-------|-------------|---------------------------|----------------|
| KPI kg/arbol incompleto | Faltan arboles en uno o mas terrenos | Ve aviso de "dato incompleto" y valor parcial | Excluir terrenos sin arboles del calculo global y mostrar aviso |
| Sin historico previo | No hay temporadas anteriores registradas | No ve comparativa con temporada anterior | Mostrar mensaje "sin historico previo" |
| Datos sin destino | Parte de cosecha queda en "Sin destino" | Grafico de destinos menos accionable | Mostrar categoria explicita para forzar limpieza de datos |
| Abandono en login | El usuario ve login pero no completa autenticacion | No llega a convertirse en usuario activo | Trazabilidad del embudo y mejoras UX sobre pasos con mayor caida |
