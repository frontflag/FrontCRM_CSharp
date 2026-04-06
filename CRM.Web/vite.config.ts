import { defineConfig } from 'vitest/config'
import { loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import { ElementPlusResolver } from 'unplugin-vue-components/resolvers'
import { readFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'

const pkg = JSON.parse(
  readFileSync(fileURLToPath(new URL('./package.json', import.meta.url)), 'utf-8')
) as { version: string }

const projectRoot = fileURLToPath(new URL('.', import.meta.url))

/** 与 CRM.API 实际监听地址一致；https 配置见 Properties/launchSettings.json（常为 5121） */
function resolveApiProxyTarget(mode: string): string {
  const env = loadEnv(mode, projectRoot, '')
  const explicit = env.VITE_API_PROXY_TARGET?.trim()
  if (explicit) return explicit.replace(/\/$/, '')
  const base = env.VITE_API_BASE_URL?.trim()
  if (base) {
    try {
      return new URL(base).origin
    } catch {
      /* ignore */
    }
  }
  return 'http://localhost:5000'
}

export default defineConfig(({ mode }) => ({
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version)
  },
  plugins: [
    vue(),
    AutoImport({
      resolvers: [ElementPlusResolver()],
    }),
    Components({
      resolvers: [ElementPlusResolver()],
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
    allowedHosts: true,
    fs: { allow: ['..'] },
    proxy: {
      '/api': {
        target: resolveApiProxyTarget(mode),
        changeOrigin: true
      }
    }
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/tests/setup.ts'],
    include: [
      'src/tests/**/*.{test,spec}.{ts,tsx}',
      '../document/TestCase/APITest/**/*.test.ts',
      '../document/TestCase/UnitTest/**/*.test.ts'
    ],
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  }
}))
