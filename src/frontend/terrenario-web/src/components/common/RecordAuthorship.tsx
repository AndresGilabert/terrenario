import React from 'react';
import { fechaDelInstante } from '../../lib/fechas';

/**
 * Lo que hace falta para contar la autoría. Es la forma que ya tienen los cuatro registros operativos
 * —actividad, cosecha, compra y consumo—, así que se pasan tal cual.
 */
export interface AuthoredRecord {
  created_by_name: string;
  created_at: string;
  updated_by_name: string;
  updated_at: string;
}

/**
 * MVP-804 (`RU-21`, `P-113`) — **Quién apuntó este registro y quién lo corrigió por última vez.**
 *
 * Pesa por `RN-034`: en el MVP los permisos son planos, de modo que cualquier miembro del Workspace
 * puede corregir el registro de cualquier otro. Ante una cifra que no cuadra, esto es lo que evita
 * tener que preguntar uno por uno.
 *
 * **Va en el modal de corrección y en ningún otro sitio.** El producto no tiene pantalla de detalle:
 * el detalle de un registro *es* su modal. Y `CA-4` es explícito en que la autoría no puede aparecer
 * como una columna más ni cambiar la densidad de las listas, así que aquí es información de apoyo —al
 * pie, en gris, con la tipografía más pequeña del formulario— y nunca un campo de captura.
 *
 * **La línea de última edición se omite cuando no hubo edición.** Un registro recién apuntado tiene
 * `updated_at === created_at`, y repetir el mismo nombre dos veces no informa de nada: solo mete
 * ruido en el sitio donde menos se quiere. La comparación se hace sobre el instante —no sobre el
 * nombre— porque corregir tu propio registro **sí** es una edición que merece contarse, aunque el
 * nombre sea el mismo.
 *
 * No hay histórico de cambios: `RU-21` lo excluye expresamente («No se mantiene histórico completo de
 * cambios por simplicidad»).
 */
export const RecordAuthorship: React.FC<{ record: AuthoredRecord }> = ({ record }) => {
  const editado = huboEdicionPosterior(record);

  return (
    <div className="pt-3 border-t border-[#f0ede8] text-[11px] text-[#76786b] space-y-0.5">
      <p className="flex items-start gap-1.5">
        {/* `person`, que ya se usa en el producto, y no un glifo nuevo: la fuente de iconos entera
            pesa 3,78 MB (`P-115`) y no hace falta ampliar el juego para una línea de apoyo. */}
        <span className="material-symbols-outlined text-sm shrink-0" aria-hidden="true">
          person
        </span>
        <span>
          Registrado por <span className="font-semibold">{record.created_by_name}</span> el{' '}
          {fechaDelInstante(record.created_at)}
        </span>
      </p>
      {editado && (
        <p className="pl-[1.375rem]">
          Última edición de <span className="font-semibold">{record.updated_by_name}</span> el{' '}
          {fechaDelInstante(record.updated_at)}
        </p>
      )}
    </div>
  );
};

/**
 * ¿Ha tocado alguien el registro después de crearlo?
 *
 * Los dos instantes salen del mismo reloj en el alta, así que en un registro sin corregir son
 * idénticos. Se comparan como instantes y no como cadenas para no depender de que el servidor los
 * serialice con el mismo número de decimales; si alguno no se pudiera parsear se cae a la comparación
 * literal, que es lo conservador: ante la duda, no se enseña una línea de más.
 */
function huboEdicionPosterior({ created_at, updated_at }: AuthoredRecord): boolean {
  const alta = Date.parse(created_at);
  const edicion = Date.parse(updated_at);

  if (Number.isNaN(alta) || Number.isNaN(edicion)) return created_at !== updated_at;
  return edicion > alta;
}
