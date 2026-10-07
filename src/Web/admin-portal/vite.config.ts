import { fileURLToPath, URL } from 'node:url';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// Aspire injects the gateway address; fall back to the gateway's default local port.
const gatewayUrl =
  process.env.services__gateway__https__0 ??
  process.env.services__gateway__http__0 ??
  'http://localhost:5197';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: Number(process.env.PORT) || 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: gatewayUrl,
        changeOrigin: true,
        // Dev-only: the local ASP.NET Core dev certificate is self-signed.
        secure: false,
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
  },
});
