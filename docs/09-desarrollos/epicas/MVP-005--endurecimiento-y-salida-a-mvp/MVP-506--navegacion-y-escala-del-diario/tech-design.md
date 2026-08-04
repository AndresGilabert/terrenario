---
id: "MVP-506"
tipo: feature
titulo: "TDD: Navegación y escala del diario"
estado: completado
tickets: []
epica: "MVP-005--endurecimiento-y-salida-a-mvp"
responsable: "@andres"
revisores: []
ai_context:
  dominios: ["diario", "escala", "rendimiento"]
  modulo_path: "03-modulos/"
  componentes: ["diario", "paginacion", "busqueda", "filtros"]
  etiquetas: ["mvp", "diario", "escala", "hardening"]
  nivel_riesgo: medio
creado_en: "2026-07-31"
actualizado_en: "2026-07-31"
---

# TDD: MVP-506 — Navegación y escala del diario

> **Referencia al spec**: [spec.md](./spec.md)

## Resumen técnico

El diario es la vista principal del MVP (RN-033) y hasta ahora devolvía **todas** las entradas vivas
del Workspace. Esta historia lo pagina de verdad, que es más que añadir `page` y `limit`:

| Pieza | Antes | Después |
|---|---|---|
| Mezcla de los cuatro tipos | En memoria, sobre lo que devolvían los cuatro puertos operativos | `UNION ALL` en SQL, con un repositorio propio |
| Paginación | No había | `page`/`limit` con `meta:{ total, page, limit }`, resuelta por la base de datos |
| Búsqueda por texto | Local, sobre lo ya traído | En servidor, sobre el diario completo |
| Filtro por responsable | No existía | `worker_id`, coherente con `GET /activities` |
| Totales de cabecera | Sumados en memoria sobre la lista completa | Una consulta agregada sobre el conjunto filtrado |

**La decisión de patrón** (que el spec dejaba abierta): **paginación clásica** `page`/`limit`. Es la
que ya definen las convenciones de `contratos-api.md`, es la de menor riesgo, y el scroll infinito
sigue siendo una capa de cliente que puede añadirse encima sin tocar el contrato.

## Componentes afectados

| Componente | Tipo de cambio | Descripción |
|---|---|---|
| `Domain/Diary/IDiaryRepository.cs` | nuevo | Puerto del diario: `DiaryRow`, filtros, página y totales |
| `Infrastructure/Data/Repositories/DiaryRepository.cs` | nuevo | La unión en SQL, los filtros y la agregación |
| `Application/Diary/DiaryQueryService.cs` | reescrito | Pasa de mezclar cuatro listas a mapear filas y derivar RN-023 |
| `Controllers/DiaryController.cs` | modificado | `worker_id`, `search`, `page`, `limit` y el `meta` nuevo |
| `frontend/.../DiarioView.tsx` | modificado | Paginación, búsqueda con espera, filtro por responsable y guarda de carreras |
| `frontend/.../diary.service.ts` · `diary.types.ts` | modificado | Los parámetros nuevos y el `meta` con `page`/`limit` |
| `docs/02-arquitectura/contratos-api.md` | modificado | El contrato del diario paginado |

## Diseño detallado

### Por qué hacía falta mover la mezcla a SQL

La versión de MVP-305 pedía a los cuatro puertos operativos sus listas completas, las concatenaba y
las ordenaba en memoria. **Era equivalente mientras no hubiera paginación**: en los dos casos se
traían todas las filas del rango, así que la diferencia era de forma, no de volumen.

Con paginación deja de serlo. Recortar la página sobre cuatro listas ya materializadas seguiría
trayendo el histórico entero a memoria en cada petición: se vería una interfaz paginada sobre un
backend que no lo está, que es exactamente el defecto que `P-051` describía y que el propio código de
MVP-305 dejó anotado.

### La forma común

Las cuatro entidades se proyectan a `DiaryRow`. Los campos que solo aplican a un tipo viajan nulos en
los demás: es el precio de que la unión la resuelva SQL, y a cambio se puede ordenar, paginar y contar
sobre el conjunto real.

Un detalle de EF que costó y conviene no volver a descubrir: **`DiaryRow` usa propiedades con
inicializador de objeto, no parámetros de constructor**. EF Core no sabe aplicar una operación de
conjunto sobre una proyección construida con constructor —la trata como proyección de cliente y falla
con «Unable to translate set operation after client projection has been applied»—; con asignaciones de
miembro sí puede empujarlas a la proyección SQL.

### Dos consultas, no cinco

- **La página**: la unión, ordenada y recortada por la base de datos.
- **Los totales**: una sola agregación `GROUP BY (tipo, tiene-compra)` que resuelve los ocho números
  de la cabecera. El corte por `HasPurchase` es el que separa gasto real de reparto (`R-01` de
  `MVP-399`) y el que cuenta los consumos sin compra previa (RN-032).

Los totales se calculan sobre el **conjunto filtrado completo**, no sobre la página: son la cabecera
del muro y cambiarían en cada avance si contaran solo lo visible.

### Los filtros se aplican antes de la unión

Sobre columnas reales de cada tabla, que es lo que EF sabe traducir (lección de `P-014`) y además
evita construir filas que se van a descartar. La búsqueda también: cada tipo busca en los campos que
tiene —una compra no tiene terreno ni responsable— en vez de filtrar después sobre la proyección.

### Qué queda fuera de cada filtro, y por qué

`worker_id` deja fuera cosechas, compras y consumos. No es una limitación: **no tienen responsable**,
igual que una compra no tiene terreno. Se sigue el precedente que ya existía con `plot_id`, incluido
el aviso en la UI, para que el muro no parezca vacío sin explicación. Combinar `worker_id` con un
`type` sin responsable devuelve vacío, que es la respuesta honesta.

### Estabilidad de la paginación

El orden desempata por `id` tras fecha de negocio y fecha de captura. Sin ese tercer criterio, dos
entradas dadas de alta en el mismo instante —un alta en lote— podrían **repetirse o perderse** entre
páginas, que es el fallo clásico de paginar sin orden total. Tiene test propio.

### Cliente: la búsqueda espera, y las respuestas se ordenan

- **Espera antes de buscar** (350 ms). La búsqueda era local justamente porque teclear no podía
  disparar una petición por letra; al moverla al servidor esa objeción sigue en pie, y se responde
  esperando a que la persona pare.
- **Volver a la primera página al cambiar un filtro**, y hacerlo **en el mismo lote** que el cambio,
  no en un efecto aparte. Ver «Lo que la verificación en navegador destapó».
- **Guarda de carreras**: con filtros y paginación en servidor pueden quedar dos peticiones en vuelo.

## Lo que la verificación en navegador destapó

Los tests estaban en verde y la funcionalidad parecía correcta. Al probarla contra la API real con 37
entradas, escribir en el buscador dejaba el muro **vacío** aunque la API devolvía 11 resultados.

Dos defectos encadenados, ninguno visible desde los tests:

1. **Una petición de más.** El reinicio de página vivía en un `useEffect` sobre `appliedSearch`, así
   que React llegaba a pintar un estado intermedio —término nuevo, página vieja— y salía una consulta
   con `search=…&page=2` antes de la buena con `page=1`.
2. **Y la vieja llegaba la última.** La respuesta de `page=2` (vacía, porque el resultado filtrado
   cabía en una página) pisaba a la de `page=1`, que sí traía datos.

Corregido en los dos frentes: el reinicio de página va **junto al cambio de filtro**, en el mismo
lote, y `reload` lleva una **secuencia** que descarta cualquier respuesta que ya no corresponda a lo
que la pantalla está pidiendo. La segunda es la importante: elimina la clase entera de fallo, no solo
este caso. Las dos tienen test de regresión.

## Corrección arrastrada de `MVP-502` (`P-068`)

Al ejecutar la suite apareció un fallo intermitente en el sucesor del traspaso de Workspace. La causa
resultó ser que **el desempate de `MVP-502` estaba a medias**: `Guid.CompareTo` de .NET **no ordena
igual** que el tipo `uuid` de PostgreSQL. El repositorio ordenaba en SQL y el handler que **anuncia**
al sucesor reproducía el criterio en memoria, así que podían elegir personas distintas — justo el
defecto que `P-068` venía a cerrar.

Se corrige de raíz: el handler **pregunta al repositorio** en vez de repetir la regla. Un único sitio
decide, y por tanto no puede haber discrepancia. Los tests dejan de reproducir el criterio en LINQ y
lo comprueban contra la base de datos.

## Alternativas descartadas

| Alternativa | Por qué se descartó |
|---|---|
| Scroll infinito | El spec lo contemplaba. La paginación clásica es la que ya definen las convenciones y la de menor riesgo; el scroll infinito puede montarse encima sin cambiar el contrato |
| Ventana temporal por defecto (p. ej. la temporada de trabajo) | Añade un filtro implícito que el usuario no pidió: se lee como «faltan entradas» |
| Añadir `page`/`limit` sin tocar la mezcla | Es lo que `P-051` prohíbe expresamente: interfaz paginada sobre un backend que no lo está |
| Filtrar la búsqueda después de la unión | Menos código, pero obliga a traducir un `WHERE` sobre la proyección y a buscar en campos que un tipo no tiene |
| Dejar la búsqueda en cliente | Sobre una vista paginada, buscar en lo visible da un resultado falso (`P-052`) |

## Riesgos e impacto

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| La unión en SQL cambia sutilmente lo que devolvía la mezcla en memoria | media | Los 13 comportamientos que fijaban los tests antiguos se conservan uno a uno, ahora contra PostgreSQL real |
| Dos consultas por petición en vez de una | baja | Antes eran **cuatro** (una por tipo) y traían el histórico entero; ahora son dos y traen una página |
| Regresión en la vista principal del MVP | media | Red de regresión de `MVP-501` + verificación conducida en navegador con datos reales |

## Plan de testing

- [x] Repositorio contra PostgreSQL real: 22 casos. Sustituyen a `DiaryQueryServiceTests`, que
      mezclaba con repositorios doblados —esa lógica ya no existe—, conservando **todos** sus
      comportamientos y añadiendo paginación, búsqueda, responsable y estabilidad del orden.
- [x] Integración HTTP: 9 casos nuevos sobre el contrato (`page`/`limit`, defectos, acotado del
      máximo, rechazo de valores no positivos, búsqueda, responsable y combinación).
- [x] Frontend: 9 casos nuevos (búsqueda en servidor con espera, controles de paginación, reinicio de
      página, filtro por responsable y su aviso) más la regresión de la carrera entre respuestas.
- [x] Verificación conducida en navegador con API y PostgreSQL reales.

## Resultado

| Suite | Antes | Después |
|---|---|---|
| Backend | 631 | **654** |
| Frontend | 72 | **81** |

## Checklist de implementación

- [x] Diseño técnico revisado
- [x] Sin migraciones (esta historia no toca el esquema)
- [x] Tests escritos y pasando
- [x] Contrato de API actualizado con la paginación y los filtros nuevos
- [x] `P-051`, `P-052` y `P-056` cerrados en `MVP-999`
- [x] Verificado en navegador con datos reales, incluida la limpieza de la siembra temporal
- [x] Sin `TODO` sin resolver en este documento
