---
bloque: 01-producto
documento: stakeholders
actualizado_en: "2025-12-29"
---

# Stakeholders y Personas del Producto

Este documento define los stakeholders principales que dirigen el proyecto, asi como las personas (personas) que representan a los usuarios finales del sistema. Estas perfiles son la fuente de referencia para tomar decisiones sobre funcionalidades, arquitectura y experiencia de usuario.

---

## 1. Equipo Interno / Stakeholders directos

Estas personas toman decisiones clave sobre direccionamiento del producto y la tecnologia del proyecto.

| Rol | Persona | Responsabilidades |
|-----|---------|------------------|
| Product Owner & Tech Lead | **Andrés Gilabert** | Define los objetivos de producto, prioridades del backlog (PO), arquitecto principal y responsable tecnico. Toma las decisiones finales sobre la estrategia, el diseno e implementacion tecnica. Es el unico stakeholder interno directo del proyecto. |

---

## 2. Personas de Usuario final

Las siguientes personas representan a los tipos de usuarios que usarán la aplicacion. Se crean periles detallados para dar contexto real al equipo de desarrollo y producto al escribir requisitos funcionales y diseñar la experiencia de usuario.

### 2.1 Antonio García Ruiz (Propietario) — El Cliente Principal

- **Edad:** 70 anos
- **Ubicacion:** Beniarrés, Alicante (Comunidad Valenciana, Espana)
- **Perfil:** Agricultor en tiempo libre que gestiona varias parcellas agricolas pequenas con sus dos hijos y su hermano. No se dedica exclusivamente a la agricultura de forma profesionall pero tiene profundos conocimientos locales sobre su tierra.
- **Necesidades clave:**
  - Visibilidad clara y sencilla de todos los terrenos gestionados (propios o cedidos).
  - Control total de costes, mano de obra y materiales usados en cada parcela.
  - Capacidad de delegar tareas a otros familiares/trabajadores sin perder el control del sistema.
  - Informes sencillos sobre rendimientos para tomar mejores decisiones futuras.

### 2.2 Carlos Martínez Ruiz (Trabajador) — El Usuario Operativo

- **Edad:** 50 anos
- **Ubicacion:** Zona de Alicante, Espana
- **Perfil:** Jornalero/trabajador temporal que presta servicio a varios propietarios en la comarca. Ayuda a Antonio con las tareas agricolas periodicas (siembra, cosechas, aplicacion de insumos). Tiene un conocimiento tecnico muy practico.
- **Necesidades clave:**
  - Interfaz extremadamente sencilla y rapida para registrar actividades realizadas (minimizar clics/intrusiones durante el trabajo en campo).
  - Que su labor quede claramente documentada: tiempo dedicado y tareas completadas.
  - Recibir instrucciones claras de los propietarios sobre que trabajar y cuando, sin confusiones administrativas.

---

## Nota sobre las personas

- Estas personas se actualizarian si surge una nueva segmentacion significativa en el usuario final o la validacion del producto.
- Al escribir historias de usuario (en `docs/09-desarrollos`), referenciar a estas personas usando sus nombres reales para mantener contexto humano al redactar los requisitos.
