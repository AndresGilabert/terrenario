import { useCallback } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { logUsageEvent, type UsageEventPayload } from '../services/telemetry.service';
import type { UsageEventName } from './usage-telemetry';

/**
 * MVP-602 — Emisor de señales de uso para las vistas autenticadas.
 *
 * Existe para que ninguna pantalla tenga que acordarse de dos cosas: resolver el token y **no** dejar
 * que un fallo de telemetría se propague. La función que devuelve es síncrona y no devuelve nada, así
 * que llamarla nunca puede meterse en el camino de lo que la pantalla estaba haciendo.
 */
export function useUsageTelemetry(): (event: UsageEventName, payload?: UsageEventPayload) => void {
  const { getAccessToken } = useAuth();

  return useCallback(
    (event, payload) => {
      void getAccessToken()
        .then((accessToken) => logUsageEvent(event, accessToken, payload))
        .catch(() => {
          // Sin token no hay señal, y eso es todo lo que pasa.
        });
    },
    [getAccessToken]
  );
}
