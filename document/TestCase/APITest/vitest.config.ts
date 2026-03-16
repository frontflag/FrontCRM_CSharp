import { defineConfig } from 'vitest/config'
import { fileURLToPath, URL } from 'node:url'
import path from 'node:path'

// 指向 CRM.Web 项目根目录
const crmWebRoot = path.resolve(__dirname, '../../../CRM.Web')

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: [path.join(crmWebRoot, 'src/tests/setup.ts')],
    include: [
      '**/*.{test,spec}.{ts,tsx}'
    ],
    alias: {
      '@': path.join(crmWebRoot, 'src')
    }
  }
})
