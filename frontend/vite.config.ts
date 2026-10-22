/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// En desarrollo, /api se redirige a la API .NET: el navegador ve un solo
// origen y no hace falta habilitar CORS. En Docker lo hace nginx.
const apiUrl = process.env.VITE_API_PROXY ?? 'http://localhost:5039';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiUrl, changeOrigin: true },
      '/health': { target: apiUrl, changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
});
