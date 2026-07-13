/**
 * 将 help/pages 帮助文档中的开发向表述改为业务用户可读文案。
 *
 * 规范依据：《扩展面板.帮助规范》§2.6 用户向正文要求
 *   document/PRD/规范/UI规范/扩展面板.帮助规范.md
 *
 * 用法：node scripts/clean-help-user-facing.mjs
 * 建议：编写或 AI/脚本生成 help/pages 后默认执行本脚本，再人工通读。
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const pagesDir = path.join(root, 'help', 'pages')

/** 权限码 → 业务用语 */
const PERM_REPLACEMENTS = [
  [/biz\.ai\.customer_intel\.lookup/g, 'AI 客户情报调查'],
  [/biz\.ai\.vendor_intel\.lookup/g, 'AI 供应商情报调查'],
  [/sales\.amount\.read/g, '销售金额读'],
  [/sales-order\.write/g, '销售订单维护'],
  [/purchase-requisition\.write/g, '采购申请维护'],
  [/purchase-order\.read/g, '采购订单读'],
  [/purchase-order\.write/g, '采购订单维护'],
  [/purchase\.amount\.read/g, '采购金额读'],
  [/purchase\.user\.read/g, '采购员信息读'],
  [/finance-receipt\.read/g, '收款读'],
  [/finance-receipt\.write/g, '收款维护'],
  [/customer\.info\.read/g, '客户信息读'],
  [/vendor\.info\.read/g, '供应商信息读'],
  [/`SYS_ADMIN`/g, '**系统管理员**'],
  [/SYS_ADMIN/g, '系统管理员'],
  [/`isSysAdmin`/g, '系统管理员'],
]

/** 技术字段/参数 → 业务用语（顺序敏感：先长后短） */
const TECH_REPLACEMENTS = [
  [/assertQuotesSameCustomer/g, '同一客户校验'],
  [/canApplyPayment=true/g, '显示可申请付款'],
  [/hasOpenReceivable/g, '存在未清应收'],
  [/globalBatchNo/g, '全局批次号'],
  [/applyStockOut=1/g, '出库申请'],
  [/requisitionIds/g, '多条采购申请'],
  [/requisitionId/g, '采购申请'],
  [/quoteIds/g, '所选报价单'],
  [/rfqItemId/g, '需求明细'],
  [/rfqId/g, '需求主单'],
  [/StockOutType=20/g, '报关类出库'],
  [/StockInType=20/g, '报关类到货'],
  [/StockInType=10/g, '采购类到货'],
  [/invoiceStatus=100/g, '可作废状态'],
  [/invoiceStatus=1/g, '待申请/待开票'],
  [/itemStatus=已确认\(30\)/g, '已确认'],
  [/PurchasePrice/g, '采购价'],
  [/pendlist/g, '待报关记录'],
  [/vendorId/g, '物料供应商'],
  [/CustomsBroker/g, '报关公司流水号'],
]

/** 整行删除 */
const REMOVE_LINE_PATTERNS = [
  /^\*\*技术说明：\*\*.+$/,
  /^详见 \[.+?\]\(.+?document\/.+?\)\.?\s*$/,
  /^（请按《扩展面板\.帮助规范》.+$/,
  /^（请补充：本页面支持的业务目标.+$/,
]

function stripDevParentheticals(text) {
  let out = text
  // （与原 …）整段
  out = out.replace(/（与原[^）]*）/g, '')
  // 残留「与原…」片段
  out = out.replace(/与原[^；。）\n]*/g, '')
  // 状态码括号：待报价（0）→ 待报价
  out = out.replace(/（\d+）/g, (m, n) => {
    // 保留如 1.03 已在别处处理；纯数字状态码去掉
    return ''
  })
  out = out.replace(/状态\s*0→5/g, '状态由「待报价」变为「查无报价」')
  out = out.replace(/明细为待报价（0）/g, '明细为待报价')
  out = out.replace(/；以列表\/接口字段一致/g, '')
  out = out.replace(/；与列表\/接口字段一致/g, '')
  out = out.replace(/（以系统实现为准）/g, '')
  out = out.replace(/（幂等）/g, '（已生成则不会重复创建）')
  return out
}

function replaceRoutes(text) {
  return text
    .replace(/（`\/sales-orders\/:id`）/g, '')
    .replace(/（`\/purchase-orders\/:id`）/g, '')
    .replace(/在销售订单详情页（`\/sales-orders\/:id`）/g, '在销售订单详情页')
    .replace(/在采购订单详情页（`\/purchase-orders\/:id`）/g, '在采购订单详情页')
    .replace(/跳转至 `\/dashboard\/settings`/g, '跳转至控制台设置')
    .replace(/（`\/inventory\/batch-reconciliation`）/g, '')
    .replace(/已统一跳转本页（`\/inventory\/batch-reconciliation`）/g, '已统一跳转本页')
    .replace(/（`\/[\w/-]+`）/g, '')
    .replace(/`\/[\w/:.-]+`/g, '')
}

function replaceHeadings(text) {
  return text
    .replace(/### 质检结论（`status`）/g, '### 质检结论')
    .replace(/### 入库状态（`stockInStatus`）/g, '### 入库状态')
    .replace(/### 报关流程状态（`internalStatus`）/g, '### 报关流程状态')
    .replace(/### 海关状态（`customsClearanceStatus`）/g, '### 海关状态')
}

function unwrapBusinessBackticks(text) {
  return text.replace(/`([^`]+)`/g, (full, inner) => {
    if (/^深圳市/.test(inner)) return full
    if (/^\d+(\.\d+)?$/.test(inner)) return full
    if (/[\u4e00-\u9fff]/.test(inner)) return inner
    if (/^id$/i.test(inner)) return '有效标识'
    return full
  })
}

function cleanDocumentRefs(text) {
  return text
    .replace(/详见 \[.+?\]\([^)]*document\/[^)]+\)\.?\s*/g, '')
    .replace(/技术说明见 \[.+?\]\([^)]*document\/[^)]+\)\.?\s*/g, '')
    .replace(/详见 `document\/实现方案\/[^`]+`/g, '')
    .replace(/`document\/实现方案\/[^`]+`/g, '')
    .replace(/与后端对接后以接口与列表状态为准/g, '以列表状态为准')
    .replace(/当前实现可能以 toast 提示/g, '操作成功后将提示')
    .replace(/须经前端校验\*\*属于同一客户\*\*（同一客户校验）/g, '须**属于同一客户**')
    .replace(/所选报价须经前端校验\*\*属于同一客户\*\*/g, '所选报价须**属于同一客户**')
    .replace(/1\.\s*\*\*服务端\*\*：/g, '1. ')
    .replace(/将当前表单提交至后端/g, '保存当前表单')
    .replace(/（与列表\/接口字段一致）/g, '')
    .replace(/（`status ≠ -1`）/g, '（非「未通过」）')
    .replace(/`status ≠ -1`/g, '非「未通过」')
    .replace(/（每行最多一条，幂等）/g, '（每行最多一条，已生成则不会重复创建）')
    .replace(/\*\*幂等：\*\*/g, '**勿重复操作：**')
    .replace(/关联待报关 待报关记录/g, '关联待报关记录')
    .replace(/须同一报价供应商等条件，。/g, '须同一报价供应商等条件。')
    .replace(/同一 RFQ 主单/g, '同一需求主单')
    .replace(/并附带 `需求主单`、`需求明细`/g, '并关联当前需求主单与明细')
    .replace(/行上具备有效 `需求主单`\/`有效标识`/g, '行上具备有效需求主单与明细标识')
    .replace(/行上具备有效 `需求主单`\/有效标识/g, '行上具备有效需求主单与明细标识')
    .replace(/（同一 `需求主单`）/g, '')
    .replace(/并携带 `所选报价单` 等参数/g, '并带入所选报价单')
    .replace(/并携带 `采购申请`/g, '并关联采购申请')
    .replace(/（`多条采购申请`）/g, '')
    .replace(/跳转销售订单详情并带 `出库申请` 等参数以发起出库/g, '跳转销售订单详情并发起出库申请')
    .replace(/行上 \*\*`显示可申请付款`\*\*/g, '页面显示可申请付款')
    .replace(/；`显示可申请付款` 时/g, '；显示可申请付款时')
    .replace(/无 stockInId/g, '尚未生成入库单')
    .replace(/（同一客户校验）/g, '')
    .replace(/（非「未通过」）（非「未通过」）/g, '（非「未通过」）')
    .replace(/\n。\n/g, '\n')
    .replace(/。。/g, '。')
    .replace(/生成 PO/g, '生成采购订单')
    .replace(/仅状态 0\/1 可加入/g, '仅「新建」或「部分完成」状态可加入')
    .replace(/采购报价 dock/g, '采购报价面板')
    .replace(/\bRFQ\b/g, '需求')
    .replace(/无独立路由时通过目录进入本说明/g, '通过左侧菜单进入各子报表')
    .replace(/受 销售金额读 控制/g, '受销售金额读权限控制')
    .replace(/受 供应商信息读/g, '受供应商信息读权限控制')
    .replace(/账号具备 采购订单读/g, '账号具备采购订单读权限')
    .replace(/账号具备 `收款读`/g, '账号具备收款读权限')
    .replace(/账号具备 收款读/g, '账号具备收款读权限')
    .replace(/`收款维护`/g, '收款维护权限')
    .replace(/行上具备有效 需求主单\/有效标识/g, '行上具备有效需求主单与明细信息')
    .replace(/另需具备 采购申请维护/g, '另需具备采购申请维护权限')
    .replace(/另需具备 销售订单维护/g, '另需具备销售订单维护权限')
    .replace(/；另需具备 销售订单维护/g, '；另需具备销售订单维护权限')
    .replace(/账号具备 销售订单维护 权限/g, '账号具备销售订单维护权限')
    .replace(/具备 采购订单读/g, '具备采购订单读权限')
    .replace(/（`采购订单读`）/g, '（采购订单读权限）')
}

function cleanLines(text) {
  const lines = text.split(/\r?\n/)
  const out = []
  for (let line of lines) {
    if (REMOVE_LINE_PATTERNS.some((re) => re.test(line.trim()))) continue
    out.push(line)
  }
  return out.join('\n')
}

function cleanContent(text) {
  let out = text
  for (const [re, repl] of PERM_REPLACEMENTS) out = out.replace(re, repl)
  for (const [re, repl] of TECH_REPLACEMENTS) out = out.replace(re, repl)
  out = stripDevParentheticals(out)
  out = replaceRoutes(out)
  out = replaceHeadings(out)
  out = cleanDocumentRefs(out)
  out = unwrapBusinessBackticks(out)
  out = cleanLines(out)
  // 清理多余空格与连续空行
  out = out.replace(/[ \t]+$/gm, '')
  out = out.replace(/\n{3,}/g, '\n\n')
  return out
}

const files = fs.readdirSync(pagesDir).filter((f) => f.endsWith('.md'))
let changed = 0

for (const file of files) {
  const fp = path.join(pagesDir, file)
  const before = fs.readFileSync(fp, 'utf8')
  const after = cleanContent(before)
  if (after !== before) {
    fs.writeFileSync(fp, after, 'utf8')
    changed++
    console.log(`[clean-help-user] ${file}`)
  }
}

console.log(`[clean-help-user] done: ${changed} file(s) updated`)
