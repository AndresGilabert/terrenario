import React from 'react';

export interface RecordCardField {
  label: string;
  value: React.ReactNode;
}

/**
 * MVP-803 (`P-095`) — Un registro operativo **como tarjeta**, para los anchos en los que su tabla no
 * cabe.
 *
 * Existe porque Cosechas y Compras eran las dos únicas listas del producto sin maqueta adaptada: sus
 * tablas de ocho columnas miden ~890 px y por debajo de `lg:` viven en un contenedor de 341 px (móvil)
 * o 704 px (tableta), así que se leían arrastrando de lado a lado. El diario y los maestros ya se leen
 * como tarjetas; esto es lo que las pone a la misma altura.
 *
 * <b>Es una sola pieza y no una por vista</b> por el mismo motivo que `list-url-state`: dos maquetas
 * de tarjeta para el mismo tipo de contenido acaban divergiendo, y aquí lo que se busca es
 * precisamente que las cuatro listas se lean igual.
 *
 * La jerarquía de la tarjeta es la de la lectura, no la de la tabla:
 *
 * - `title` es **de qué** es el registro (el terreno, el material). Es lo primero que se busca.
 * - `subtitle` sitúa en el tiempo: la fecha y la campaña.
 * - `highlight` es la cifra que manda —los kilos, el coste—, en grande y a la derecha, para poder
 *   recorrer la lista con la vista sin leer cada etiqueta.
 * - `fields` es el resto, en dos columnas con su rótulo. Aquí sí hace falta el rótulo: sin la cabecera
 *   de la tabla, un número suelto no dice de qué es.
 * - `actions` cierra la tarjeta, con las mismas etiquetas accesibles que en la tabla.
 */
export const RecordCard: React.FC<{
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  /** Avisos no bloqueantes del registro (fuera de temporada, sin compra…). */
  badges?: React.ReactNode;
  highlight?: React.ReactNode;
  fields: RecordCardField[];
  actions?: React.ReactNode;
}> = ({ title, subtitle, badges, highlight, fields, actions }) => (
  <li className="bg-white rounded-2xl border border-[#e5e2dd] ambient-shadow p-4 space-y-3">
    <div className="flex items-start justify-between gap-3">
      <div className="min-w-0">
        <p className="font-bold text-sm text-[#33450d] truncate">{title}</p>
        {subtitle && <p className="text-[11px] text-[#76786b] mt-0.5">{subtitle}</p>}
      </div>
      {highlight && <div className="text-right shrink-0">{highlight}</div>}
    </div>

    {badges && <div className="flex flex-wrap gap-1.5">{badges}</div>}

    {fields.length > 0 && (
      <dl className="grid grid-cols-2 gap-x-3 gap-y-2 text-xs">
        {fields.map((field) => (
          <div key={field.label} className="min-w-0">
            <dt className="text-[10px] font-bold uppercase tracking-wider text-[#76786b]">
              {field.label}
            </dt>
            <dd className="text-[#1c1c19] mt-0.5">{field.value}</dd>
          </div>
        ))}
      </dl>
    )}

    {actions && (
      <div className="flex items-center justify-end gap-1 pt-1 border-t border-[#f0ede8]">
        {actions}
      </div>
    )}
  </li>
);

/** Contenedor de las tarjetas. Es una lista para que un lector de pantalla anuncie cuántas hay. */
export const RecordCardList: React.FC<{ children: React.ReactNode; label: string }> = ({
  children,
  label,
}) => (
  <ul aria-label={label} className="space-y-3">
    {children}
  </ul>
);
