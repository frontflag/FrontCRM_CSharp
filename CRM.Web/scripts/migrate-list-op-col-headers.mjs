/**
 * One-shot: replace legacy op-col-header (+ op-col-header-text) with list-op-col-header--icon-only.
 * Run from repo: node CRM.Web/scripts/migrate-list-op-col-headers.mjs
 */
import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const viewsRoot = path.resolve(__dirname, '../src/views')

const newHeaderToggle =
  '<template #col-actions-header>\n' +
  '          <div class="list-op-col-header--icon-only">\n' +
  '            <button\n' +
  '              type="button"\n' +
  '              class="op-col-toggle-btn list-op-col-toggle"\n' +
  `              :aria-label="opColExpanded ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"\n` +
  '              @click.stop="toggleOpCol"\n' +
  '            >\n' +
  "              {{ opColExpanded ? '>' : '<' }}\n" +
  '            </button>\n' +
  '          </div>\n' +
  '        </template>'

/** Match span with any t('...') inside col-actions-header */
const reColActionsHeader = new RegExp(
  '<template\\s+#col-actions-header>\\s*' +
    '<div\\s+class="op-col-header">\\s*' +
    '<span\\s+class="op-col-header-text">\\{\\{\\s*t\\([^)]+\\)\\s*\\}\\}</span>\\s*' +
    '<button\\s+type="button"\\s+class="op-col-toggle-btn"\\s+@click\\.stop="toggleOpCol"\\s*>' +
    '\\s*\\{\\{\\s*opColExpanded\\s*\\?\\s*[\'"]>[\'"]\\s*:\\s*[\'"]<[\'"]\\s*\\}\\}\\s*' +
    '</button>\\s*</div>\\s*</template>',
  'g'
)

function walk(dir, out = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name)
    if (ent.isDirectory()) walk(p, out)
    else if (ent.name.endsWith('.vue')) out.push(p)
  }
  return out
}

let touched = 0
for (const file of walk(viewsRoot)) {
  let s = fs.readFileSync(file, 'utf8')
  if (!s.includes('#col-actions-header') || !s.includes('op-col-header-text')) continue
  if (s.includes('list-op-col-header--icon-only') && file.includes('RFQList')) {
    /* RFQList uses rfq-list-op-col — migrate separately */
  }
  const next = s.replace(reColActionsHeader, newHeaderToggle)
  if (next !== s) {
    fs.writeFileSync(file, next, 'utf8')
    touched++
    console.log('updated:', path.relative(path.resolve(__dirname, '..'), file))
    s = next
  }
}

console.log('files touched:', touched)
