import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// I dev proxyer vi /api til API'en, så frontend og backend er samme origin (ingen CORS-bøvl).
// I prod gør nginx det samme (se nginx.conf).
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
})
