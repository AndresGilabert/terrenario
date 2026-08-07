import React, { createContext, useCallback, useContext, useMemo, useState } from 'react';

interface DataScopeContextValue {
  /**
   * Cambia cada vez que el **contexto activo** cambia por decisión del usuario. Se usa como clave de
   * remontaje del área operativa: no hay que leerlo, solo depender de él.
   */
  scopeVersion: number;
  /** Declara que el contexto activo ha cambiado y todo lo cargado bajo el anterior ya no vale. */
  invalidateScope: () => void;
}

const DataScopeContext = createContext<DataScopeContextValue | null>(null);

/**
 * MVP-701 — Punto **único** de invalidación de los datos cargados cuando cambia el contexto activo
 * (Workspace o temporada de trabajo).
 *
 * `P-081` no fue el fallo de una vista, fue el de un patrón: `ApiProvider` memoiza el cliente HTTP con
 * `useMemo(..., [])`, de él cuelgan los `*.service` y de estos los `reload` de cada vista, y **ninguna
 * de esas cadenas menciona el Workspace**. Nueve de las diez vistas operativas seguían mostrando los
 * datos del Workspace anterior tras cambiar de uno a otro.
 *
 * La corrección **no** es añadir `workspaceId` a nueve efectos: eso deja la trampa puesta para la
 * décima vista que se añada, que volvería a nacer rota (decisión del PO). Lo que se hace es remontar el
 * área operativa entera con una clave que cambia con el contexto; una vista nueva hereda el
 * comportamiento sin saber que este problema existió.
 *
 * Remontar —y no solo recargar— es además lo que cierra el agravante del punto: al desmontarse se van
 * con el árbol las filas del Workspace anterior, los formularios abiertos y las confirmaciones
 * pendientes, así que no queda ninguna acción apuntando a un registro que ya no es del usuario (CA-2).
 *
 * Es un contador y no el propio identificador del Workspace a propósito: el identificador también
 * cambia en la **carga inicial** (de «no hay» a «este»), y eso remontaría el árbol recién montado,
 * duplicando la primera petición de todas las vistas. Aquí solo se avisa de los cambios deliberados.
 */
export function DataScopeProvider({ children }: { children: React.ReactNode }) {
  const [scopeVersion, setScopeVersion] = useState(0);

  const invalidateScope = useCallback(() => setScopeVersion((version) => version + 1), []);

  const value = useMemo<DataScopeContextValue>(
    () => ({ scopeVersion, invalidateScope }),
    [scopeVersion, invalidateScope]
  );

  return <DataScopeContext.Provider value={value}>{children}</DataScopeContext.Provider>;
}

export function useDataScope(): DataScopeContextValue {
  const context = useContext(DataScopeContext);
  if (!context) throw new Error('useDataScope must be used within a DataScopeProvider');
  return context;
}
