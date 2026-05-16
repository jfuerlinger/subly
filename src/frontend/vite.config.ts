import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
// When running under Aspire, the API URL is injected via environment variables.
// Falls back to localhost:8080 for standalone development.
const apiTarget =
  process.env['services__api__https__0'] ??
  process.env['services__api__http__0'] ??
  'http://localhost:8080';

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/tests/setup.ts'],
    coverage: {
      reporter: ['text', 'html'],
      include: ['src/app/**/*.{ts,tsx}'],
    },
  },
})
