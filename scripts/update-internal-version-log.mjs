#!/usr/bin/env node
/**
 * post-commit：prepend 内部版本日志，更新 FRONTEND_DEBUG_VERSION，并 amend 进同一 commit。
 * 跳过：SKIP_INTERNAL_VERSION_LOG=1 或 subject 以 chore: 内部版本日志 开头。
 */
import { execSync } from 'node:child_process'
import { readFileSync, writeFileSync, existsSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const __dirname = dirname(fileURLToPath(import.meta.url))
const repoRoot = join(__dirname, '..')

if (process.env.SKIP_INTERNAL_VERSION_LOG === '1') {
  process.exit(0)
}

const DEBUG_DIR = join(repoRoot, 'CRM.Web/src/views/Debug')
const CONSTANTS_PATH = join(DEBUG_DIR, 'debugConstants.ts')
const LOG_PATH = join(DEBUG_DIR, 'internal-version-log.txt')

const skipPrefixes = ['chore: 内部版本日志', 'chore: internal version log']

const pad = (n) => String(n).padStart(2, '0')

let subject = ''
let commitDate = new Date()

try {
  const line = execSync('git log -1 --format=%ci|%s', { cwd: repoRoot, encoding: 'utf-8' }).trim()
  const pipe = line.indexOf('|')
  if (pipe === -1) process.exit(0)
  const ci = line.slice(0, pipe).trim()
  subject = line.slice(pipe + 1).trim()
  commitDate = new Date(ci)
} catch {
  process.exit(0)
}

if (!subject || skipPrefixes.some((p) => subject.toLowerCase().startsWith(p.toLowerCase()))) {
  process.exit(0)
}

const MM = pad(commitDate.getMonth() + 1)
const dd = pad(commitDate.getDate())
const HH = pad(commitDate.getHours())
const mm = pad(commitDate.getMinutes())
const versionSuffix = `${MM}${dd}-${HH}${mm}`
const versionLine = `1.1.${versionSuffix} ${subject}`

const timestamp = `${commitDate.getFullYear()}-${MM}-${dd} ${HH}:${mm}:${pad(commitDate.getSeconds())}`
const logLine = `${timestamp} | ${subject}`

const existingLog = existsSync(LOG_PATH) ? readFileSync(LOG_PATH, 'utf-8') : ''
const firstLine = existingLog.split('\n')[0]?.trim()

if (firstLine === logLine.trim()) {
  const currentConstants = existsSync(CONSTANTS_PATH) ? readFileSync(CONSTANTS_PATH, 'utf-8') : ''
  if (currentConstants.includes(`'${versionLine.replace(/'/g, "\\'")}'`) || currentConstants.includes(versionLine)) {
    process.exit(0)
  }
}

const newLog = existingLog.trimEnd()
  ? `${logLine}\n${existingLog.trimEnd()}\n`
  : `${logLine}\n`

writeFileSync(LOG_PATH, newLog, 'utf-8')

const escapedSubject = subject.replace(/\\/g, '\\\\').replace(/'/g, "\\'")
const constantsContent = `/** Debug 页展示用前端版本号（post-commit 自动更新，格式 1.1.MMdd-HHmm + 提交说明） */
export const FRONTEND_DEBUG_VERSION = '1.1.${versionSuffix} ${escapedSubject}'
`
writeFileSync(CONSTANTS_PATH, constantsContent, 'utf-8')

const logRel = 'CRM.Web/src/views/Debug/internal-version-log.txt'
const constantsRel = 'CRM.Web/src/views/Debug/debugConstants.ts'

try {
  execSync(`git add "${logRel}" "${constantsRel}"`, { cwd: repoRoot, stdio: 'inherit' })
  execSync('git commit --amend --no-edit --no-verify', {
    cwd: repoRoot,
    stdio: 'inherit',
    env: { ...process.env, SKIP_INTERNAL_VERSION_LOG: '1' }
  })
} catch (e) {
  console.warn('[internal-version-log] amend skipped:', e?.message || e)
}
