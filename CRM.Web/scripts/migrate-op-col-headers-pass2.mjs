/**
 * Replace remaining <div class="op-col-header"> + op-col-header-text blocks (flexible whitespace).
 */
import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

const files = [
  'src/views/Approvals/PendingApprovals.vue',
  'src/views/Inventory/InventoryList.vue',
  'src/views/Inventory/InventoryCheck.vue',
  'src/views/Inventory/InventoryStockDetailPage.vue',
  'src/views/Inventory/StockInEdit.vue',
  'src/views/Inventory/StockOutEdit.vue',
  'src/views/Finance/FinancePaymentDetail.vue',
  'src/views/Finance/FinanceReceiptDetail.vue',
  'src/views/Vendor/VendorDetail.vue',
  'src/views/RFQ/QuoteList.vue',
  'src/views/RFQ/SalesOrderItemList.vue',
  'src/views/RFQ/SalesOrderDetail.vue',
  'src/views/RFQ/RFQDetail.vue',
  'src/views/RFQ/RFQCreate.vue',
  'src/views/RFQ/QuoteEdit.vue',
  'src/views/RFQ/PurchaseRequisitionDetailPage.vue',
  'src/views/BOM/BOMDetail.vue',
  'src/views/Customer/CustomerDetail.vue',
  'src/views/System/DepartmentDetail.vue'
]

const re = /<div class="op-col-header">\s*<span class="op-col-header-text">(?:\{\{[\s\S]*?\}\}|[^<]*)<\/span>\s*<button type="button" class="op-col-toggle-btn" @click\.stop="([^"]+)">\s*\{\{\s*([\s\S]*?)\s*\?\s*['"]>['"]\s*:\s*['"]<['"]\s*\}\}\s*<\/button>\s*<\/div>/g

function patch(content) {
  return content.replace(re, (_m, toggleFn, cond) => {
    const c = cond.replace(/\s+/g, ' ').trim()
    return `<div class="list-op-col-header--icon-only">
            <button
              type="button"
              class="op-col-toggle-btn list-op-col-toggle"
              :aria-label="${c} ? t('common.listOpCol.collapse') : t('common.listOpCol.expand')"
              @click.stop="${toggleFn}"
            >
              {{ ${c} ? '>' : '<' }}
            </button>
          </div>`
  })
}

const root = path.resolve(__dirname, '..')
let n = 0
for (const rel of files) {
  const fp = path.join(root, rel)
  if (!fs.existsSync(fp)) {
    console.warn('skip missing', rel)
    continue
  }
  const s = fs.readFileSync(fp, 'utf8')
  if (!s.includes('op-col-header-text')) continue
  const next = patch(s)
  if (next !== s) {
    fs.writeFileSync(fp, next, 'utf8')
    n++
    console.log('patched', rel)
  }
}
console.log('pass2 files:', n)
