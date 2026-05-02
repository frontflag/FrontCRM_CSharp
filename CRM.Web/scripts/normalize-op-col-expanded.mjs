import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

function walk(dir, o = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name)
    if (ent.isDirectory()) walk(p, o)
    else if (ent.name.endsWith('.vue')) o.push(p)
  }
  return o
}

const skip = new Set([path.join('src/views/System/UserList.vue')])
const root = path.resolve(__dirname, '..')
let n = 0

for (const fp of walk(path.join(root, 'src/views'))) {
  const rel = path.relative(root, fp).replace(/\\/g, '/')
  if (skip.has(rel)) continue
  let s = fs.readFileSync(fp, 'utf8')
  if (!s.includes('OP_COL_EXPANDED_WIDTH')) continue
  if (s.includes('OP_COL_EXPANDED_WIDTH = computed')) continue
  const orig = s
  s = s.replace(/const OP_COL_EXPANDED_WIDTH = \d+(\s*\/\/[^\n]*)?/g, 'const OP_COL_EXPANDED_WIDTH = 173')
  s = s.replace(/const OP_COL_EXPANDED_MIN_WIDTH = \d+/g, 'const OP_COL_EXPANDED_MIN_WIDTH = 160')
  s = s.replace(/const OP_COL_COLLAPSED_WIDTH = 120/g, 'const OP_COL_COLLAPSED_WIDTH = 43')
  if (s !== orig) {
    fs.writeFileSync(fp, s, 'utf8')
    n++
    console.log(rel)
  }
}
console.log('patched', n)
