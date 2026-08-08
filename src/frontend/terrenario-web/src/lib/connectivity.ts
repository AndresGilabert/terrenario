import { useSyncExternalStore } from 'react';

/**
 * MVP-709 (`P-091`) — Estado de conectividad del cliente.
 *
 * <b>Por qué no basta `navigator.onLine`.</b> Solo sabe si hay *interfaz de red*, no si se llega al
 * servidor. En el campo el caso normal es el contrario del que detecta: el móvil sigue enganchado a
 * una antena con una barra, `navigator.onLine` dice `true` y las peticiones mueren igual. Su `false`
 * es fiable —si dice que no hay red, no la hay—; su `true` no significa nada.
 *
 * Por eso hay **dos fuentes** y la que manda es la de los hechos:
 *
 * 1. Los eventos `online`/`offline` del navegador, que llegan solos y sin coste.
 * 2. Lo que le pasa de verdad a cada petición: el cliente HTTP avisa cuando una muere sin respuesta
 *    y cuando otra vuelve con una. Esto es lo que detecta la cobertura de una barra.
 *
 * <b>No es una cola ni un modo sin conexión.</b> `ADR-0002` mantiene el producto online-first: aquí
 * solo se sabe si hay conexión para poder decirlo. Nada se guarda ni se reintenta por su cuenta.
 */
export type EstadoDeConexion = 'en-linea' | 'sin-conexion';

let estadoActual: EstadoDeConexion =
  typeof navigator !== 'undefined' && navigator.onLine === false ? 'sin-conexion' : 'en-linea';

const suscriptores = new Set<() => void>();

function fijar(nuevo: EstadoDeConexion) {
  if (nuevo === estadoActual) return;
  estadoActual = nuevo;
  for (const avisar of suscriptores) avisar();
}

/** El cliente HTTP no obtuvo respuesta: la petición murió antes de llegar o de volver. */
export function marcarSinConexion() {
  fijar('sin-conexion');
}

/**
 * Una petición ha vuelto con respuesta del servidor. Vale igual si esa respuesta es un error: un 500
 * demuestra que hay conexión, que es lo único que este módulo mide.
 */
export function marcarConConexion() {
  fijar('en-linea');
}

export function estadoDeConexion(): EstadoDeConexion {
  return estadoActual;
}

if (typeof window !== 'undefined') {
  // El `offline` del navegador es fiable y llega antes que el primer fallo de petición: sirve para
  // avisar sin esperar a que algo falle.
  window.addEventListener('offline', marcarSinConexion);
  // El `online`, en cambio, solo dice que hay interfaz. Se acepta igual —es lo que cierra el aviso sin
  // exigir recargar (CA-2)— y si la cobertura sigue sin dar, el siguiente fallo lo vuelve a levantar.
  window.addEventListener('online', marcarConConexion);
}

export function suscribir(avisar: () => void) {
  suscriptores.add(avisar);
  return () => {
    suscriptores.delete(avisar);
  };
}

/** Estado de conexión, reactivo. */
export function useEstadoDeConexion(): EstadoDeConexion {
  return useSyncExternalStore(suscribir, estadoDeConexion, () => 'en-linea' as const);
}

/** Solo para tests: devuelve el módulo a su estado inicial. */
export function reiniciarConectividadParaTests() {
  estadoActual = 'en-linea';
  suscriptores.clear();
}
