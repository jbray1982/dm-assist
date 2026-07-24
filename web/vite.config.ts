import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // The API host (see src/DmAssist.Api/Properties/launchSettings.json). Proxying here means
      // `npm run dev` needs no CORS configuration and no environment-variable plumbing.
      '/api': {
        target: 'http://localhost:5178',
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    globals: true,
  },
})
