import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

/**
 * MVP-501 — Arnés de tests unitarios del frontend (`MVP-999`, `P-012`/`P-023`).
 *
 * Config aparte de `vite.config.ts` a propósito: el build de producción lleva Tailwind y las reglas
 * de troceado del bundle, que en un test no aportan nada y solo lo hacen más lento y más frágil.
 * Vitest prioriza este fichero sobre `vite.config.ts` cuando existe.
 */
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    // El CSS no participa en ninguna aserción: procesarlo solo añadiría tiempo de arranque.
    css: false,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      // Lo que la estrategia de testing llama «lógica de dominio» en el cliente: decisiones que no
      // se ven en el tipado. Las vistas grandes se cubren por su lógica extraída, no por píxeles.
      include: ['src/lib/**', 'src/services/**', 'src/contexts/**', 'src/components/**'],
      exclude: ['src/test/**', 'src/**/*.test.{ts,tsx}', 'src/types/**'],
    },
  },
});
