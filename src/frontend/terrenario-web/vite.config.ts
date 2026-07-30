import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [tailwindcss(), react()],
  build: {
    // Separa las dependencias que casi nunca cambian (React y el router) del código propio de la app.
    // No reduce la primera carga —el total gzip es el mismo— pero el chunk de vendor se cachea entre
    // despliegues, así que cada versión nueva solo obliga a re-descargar el código de la app (~50 kB
    // gzip) en vez del bundle entero. De paso, ningún chunk supera ya el umbral de aviso de Vite: el
    // aviso desaparece porque el bundle es genuinamente más pequeño, no porque se suba el listón.
    // Rolldown (Vite 8) usa `codeSplitting.groups`; `[\\/]` captura ambos separadores de ruta, que es
    // lo recomendado para no fallar en Windows.
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            { name: 'react-vendor', test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
            { name: 'router', test: /node_modules[\\/]react-router/ },
          ],
        },
      },
    },
  },
})
