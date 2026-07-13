/** customer.intel.lookup 契约（13 章：Phase 1 八章 + Phase 2 五章） */

import { FIELD_LABEL_ZH } from '@/utils/jsonLabels'

export const CUSTOMER_INTEL_SCHEMA_VERSION = '1.1'

export const CUSTOMER_INTEL_SECTION_IDS = [
  'registry',
  'ownership',
  'business',
  'scale',
  'certifications',
  'timeline',
  'contacts',
  'compliance_risks',
  'market_risks',
  'procurement_signals',
  'opportunities',
  'key_people',
  'ai_assessment'
] as const

export type CustomerIntelSectionId = (typeof CUSTOMER_INTEL_SECTION_IDS)[number]

export const CUSTOMER_INTEL_SECTION_LABELS: Record<CustomerIntelSectionId, string> = {
  registry: '基础档案',
  ownership: '股权结构',
  business: '经营业务',
  scale: '企业规模',
  certifications: '资质与认证',
  timeline: '发展历程',
  contacts: '联系方式',
  compliance_risks: '合规与司法风险',
  market_risks: '经营与市场风险',
  procurement_signals: '采购与供应链信号',
  opportunities: '商机线索',
  key_people: '关键人与组织',
  ai_assessment: 'AI 综合评估'
}

/** 章节置信度中文 */
export const CUSTOMER_INTEL_CONFIDENCE_LABELS: Record<string, string> = {
  high: '高',
  'medium-high': '中高',
  medium: '中',
  low: '低'
}

/** 章节 content 字段中文标签（snake_case → 中文） */
export const CUSTOMER_INTEL_FIELD_LABELS: Record<string, string> = {
  // 基础档案 registry
  official_name: '企业全称',
  company_name: '企业名称',
  company_type: '企业类型',
  operating_status: '经营状态',
  credit_code: '统一社会信用代码',
  unified_social_credit_code: '统一社会信用代码',
  legal_representative: '法定代表人',
  registered_capital: '注册资本',
  paid_in_capital: '实缴资本',
  incorporation_date: '成立日期',
  establishment_date: '成立日期',
  approved_date: '核准日期',
  business_term: '营业期限',
  business_scope: '经营范围',
  registered_address: '注册地址',
  address: '地址',
  region: '所在地区',
  province: '省份',
  city: '城市',
  district: '区县',
  industry: '所属行业',
  registration_authority: '登记机关',
  taxpayer_qualification: '纳税人资质',
  org_code: '组织机构代码',
  organization_code: '组织机构代码',
  enterprise_scale: '企业规模',
  registration_status: '登记状态',
  english_name: '英文名称',
  established_date: '成立日期',
  // 经营业务 business
  main_products: '主营产品',
  main_business: '主营业务',
  core_business: '核心业务',
  business_model: '商业模式',
  industry_tags: '行业标签',
  application_fields: '应用领域',
  customer_profile: '客户画像',
  competitive_advantage: '竞争优势',
  key_features: '关键特点',
  technology_focus: '技术重点',
  category: '业务类别',
  // 企业规模 scale
  employees: '员工人数',
  rd_investment: '研发投入',
  employee_total: '员工总数',
  employee_count: '员工人数',
  revenue: '营业收入',
  annual_revenue: '年营收',
  factory_count: '生产基地数量',
  factories: '生产基地',
  branches: '分支机构',
  valuation: '估值',
  robots_sold: '售出机器人',
  total_funding: '累计融资',
  funding: '融资金额',
  financing_stage: '融资阶段',
  annual_growth: '年增长率',
  annual_growth_2024: '2024年增长率',
  ecosystem_partners: '生态合作伙伴',
  projects_implemented: '已实施项目',
  intellectual_property: '知识产权',
  overseas_revenue_ratio: '海外收入占比',
  social_insurance_count: '参保人数',
  qualifications: '资质认证',
  value: '数值',
  unit: '单位',
  // 股权结构 ownership
  shareholders: '股东',
  shareholder_name: '股东名称',
  share_ratio: '持股比例',
  shareholding_ratio: '持股比例',
  parent_company: '母公司',
  ultimate_controller: '实际控制人',
  listed_info: '上市信息',
  is_listed: '是否上市',
  stock_code: '股票代码',
  exchange: '交易所',
  ownership_notes: '股权备注',
  ownership_type: '股东类型',
  // 资质与认证 certifications
  is_high_tech_enterprise: '高新技术企业',
  issuer: '发证机构',
  valid_until: '有效期至',
  honors: '荣誉奖项',
  certification_type: '认证类型',
  // 经营与市场风险 market_risks
  customer_concentration: '客户集中度',
  competition_summary: '竞争格局',
  policy_risks: '政策风险',
  // 采购与供应链信号 procurement_signals
  procurement_signals: '采购信号',
  expansion_signals: '扩产信号',
  bom_needs: 'BOM 需求',
  localization_signals: '本地化供应链信号',
  urgency: '紧迫程度',
  signal_type: '信号类型',
  // 关键人与组织 key_people
  people: '关键人员',
  org_summary: '组织概况',
  rd_team_summary: '研发团队概况',
  department: '部门',
  background: '背景',
  public_contact: '公开联系方式',
  linkedin_url: '领英链接',
  // 合规与司法 compliance_risks
  risk_level: '风险等级',
  checks: '风险核查项',
  attention_items: '关注事项',
  type: '类型',
  count: '数量',
  status: '状态',
  description: '说明',
  severity: '严重程度',
  risk_tips: '风险提示',
  tax_rating: '纳税信用等级',
  // 商机 opportunities
  items: '商机列表',
  title: '标题',
  priority: '优先级',
  suggested_actions: '建议行动',
  opportunity_type: '商机类型',
  industry_trends: '行业趋势',
  market_position: '市场地位',
  growth_drivers: '增长驱动',
  recent_developments: '近期动态',
  competitive_advantages: '竞争优势',
  cooperation_opportunities: '合作机会',
  // 联系方式 contacts
  locations: '办公地点',
  public_emails: '公开邮箱',
  public_phones: '公开电话',
  website: '官网',
  official_email: '官方邮箱',
  official_website: '官方网站',
  recruitment_page: '招聘页面',
  overseas_presence: '海外布局',
  location: '所在地',
  contact_name: '联系人',
  phone: '电话',
  email: '邮箱',
  role: '职务',
  // 发展历程 timeline
  events: '发展历程',
  date: '日期',
  event: '事件',
  year: '年份',
  // AI 综合评估 ai_assessment
  dimensions: '评估维度',
  overall_summary: '综合摘要',
  overall_rating: '综合评级',
  recommendation: '推荐建议',
  financial_health: '财务健康',
  growth_potential: '增长潜力',
  technology_strength: '技术实力',
  key_monitoring_points: '重点跟踪事项',
  visit_strategy: '拜访策略',
  recommended_next_steps: '建议下一步',
  score: '评分',
  basis_section_ids: '依据章节',
  name: '名称',
  note: '备注',
  notes: '备注',
  summary: '摘要',
  url: '链接',
  source: '来源',
  sources: '信息来源',
  // 常见 AI 英文字段（截图红框）
  shareholder_type: '股东类型',
  products: '主营产品',
  industry_chain_position: '产业链地位',
  subsidiary_count: '子公司人数',
  domestic_offices: '国内办事处',
  international_presence: '国际布局',
  international_layout: '国际布局',
  revenue_breakdown: '收入构成',
  business_line: '业务条线',
  amount: '金额',
  proportion: '占比',
  percentage: '占比',
  milestones: '里程碑',
  investor_relations_email: '投资者关系邮箱',
  company_email: '企业邮箱',
  risk_summary: '风险摘要',
  last_verified: '最后核实日期',
  recommended_approach: '建议接触方式',
  key_decision_factors: '关键决策因素',
  timeframe: '时间窗口',
  potential: '潜力',
  key_strengths: '关键优势',
  key_concerns: '关键关切',
  dimension: '评估维度',
  assessment: '评估结论'
}

/** 字段值常见英文枚举 → 中文 */
export const CUSTOMER_INTEL_VALUE_LABELS: Record<string, string> = {
  high: '高',
  'medium-high': '中高',
  medium: '中',
  low: '低',
  unknown: '未知',
  clear: '无异常'
}

const FIELD_LABEL_FALLBACK_WORDS: Record<string, string> = {
  id: '编号',
  code: '编码',
  no: '编号',
  num: '数量',
  total: '累计',
  list: '列表',
  info: '信息',
  data: '数据',
  level: '等级',
  model: '模式',
  term: '期限',
  scope: '范围',
  term_end: '期限截止',
  term_start: '期限起始',
  enterprise: '企业',
  scale: '规模',
  organization: '组织机构',
  registration: '登记',
  status: '状态',
  features: '特点',
  core: '核心',
  business: '业务',
  technology: '技术',
  focus: '重点',
  key: '关键',
  valuation: '估值',
  robots: '机器人',
  sold: '售出',
  funding: '融资',
  financing: '融资',
  stage: '阶段',
  annual: '年度',
  growth: '增长',
  ecosystem: '生态',
  partners: '合作伙伴',
  projects: '项目',
  implemented: '已实施',
  intellectual: '知识',
  property: '产权',
  overseas: '海外',
  revenue: '收入',
  ratio: '占比',
  social: '社保',
  insurance: '参保',
  qualifications: '资质',
  tips: '提示',
  tax: '纳税',
  rating: '等级',
  trends: '趋势',
  market: '市场',
  position: '地位',
  drivers: '驱动',
  recent: '近期',
  developments: '动态',
  advantages: '优势',
  cooperation: '合作',
  opportunities: '机会',
  official: '官方',
  recruitment: '招聘',
  page: '页面',
  presence: '布局',
  location: '所在地',
  email: '邮箱',
  website: '网站',
  overall: '综合',
  health: '健康',
  potential: '潜力',
  strength: '实力',
  monitoring: '跟踪',
  points: '事项',
  recommendation: '建议',
  financial: '财务',
  competitive: '竞争',
  industry: '行业',
  risk: '风险',
  contact: '联系',
  phone: '电话',
  shareholder: '股东',
  share: '持股',
  holding: '持有',
  english: '英文',
  established: '成立',
  date: '日期',
  category: '类别',
  employees: '员工',
  employee: '员工',
  rd: '研发',
  investment: '投入',
  name: '名称',
  address: '地址',
  capital: '资本',
  registered: '注册',
  legal: '法定',
  representative: '代表人',
  operating: '经营',
  paid: '实缴',
  incorporation: '设立',
  approval: '核准',
  taxpayer: '纳税人',
  qualification: '资质',
  paid_in: '实缴',
  in: '在',
  of: '的',
  and: '与',
  the: '',
  a: '',
  an: '',
  for: '用于',
  with: '含',
  from: '来自',
  to: '至',
  by: '由',
  on: '于',
  at: '在',
  as: '作为',
  or: '或',
  vs: '对比',
  per: '每',
  min: '最小',
  max: '最大',
  avg: '平均',
  est: '估计',
  approx: '约',
  ceo: '首席执行官',
  cfo: '首席财务官',
  cto: '首席技术官',
  coo: '首席运营官',
  ip: '知识产权',
  url: '链接',
  api: '接口',
  listed: '上市',
  stock: '股票',
  exchange: '交易所',
  certification: '认证',
  honor: '荣誉',
  concentration: '集中度',
  competition: '竞争',
  policy: '政策',
  procurement: '采购',
  expansion: '扩产',
  localization: '本地化',
  signal: '信号',
  urgency: '紧迫',
  people: '人员',
  org: '组织',
  team: '团队',
  department: '部门',
  background: '背景',
  linkedin: '领英',
  count: '人数',
  type: '类型',
  products: '产品',
  chain: '产业链',
  subsidiary: '子公司',
  domestic: '国内',
  offices: '办事处',
  international: '国际',
  breakdown: '构成',
  line: '条线',
  amount: '金额',
  proportion: '占比',
  milestone: '里程碑',
  milestones: '里程碑',
  verified: '核实',
  last: '最后',
  summary: '摘要',
  approach: '接触方式',
  recommended: '建议',
  factors: '因素',
  decision: '决策',
  timeframe: '时间窗口',
  dimension: '维度',
  assessment: '评估',
  strengths: '优势',
  concerns: '关切',
  investor: '投资者',
  relations: '关系',
  company: '企业',
  medium: '中',
  low: '低',
  high: '高',
  unknown: '未知',
  clear: '无异常'
}

/** 是否含拉丁字母（视为英文 Key） */
function containsLatinLetters(text: string): boolean {
  return /[a-zA-Z]/.test(text)
}

/** 是否纯中文 Key（无需翻译） */
function isChineseFieldKey(key: string): boolean {
  return /[\u4e00-\u9fff]/.test(key) && !containsLatinLetters(key)
}

/** camelCase / PascalCase / 空格 / 连字符 → token 列表 */
function splitFieldKeyTokens(key: string): string[] {
  let k = key.trim()
  if (!k) return []
  k = k
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
  const normalized = normalizeCustomerIntelFieldKey(k)
  if (normalized) return normalized.split('_').filter(Boolean)
  return k.toLowerCase().split(/[\s\-]+/).filter(Boolean)
}

function lookupFieldLabelExact(key: string): string | undefined {
  const lower = key.toLowerCase()
  return (
    CUSTOMER_INTEL_FIELD_LABELS[key] ??
    CUSTOMER_INTEL_FIELD_LABELS[lower] ??
    FIELD_LABEL_ZH[key] ??
    FIELD_LABEL_ZH[lower]
  )
}

function lookupSingleFieldToken(token: string): string | undefined {
  const lower = token.toLowerCase()
  const direct =
    CUSTOMER_INTEL_FIELD_LABELS[lower] ??
    FIELD_LABEL_FALLBACK_WORDS[lower] ??
    FIELD_LABEL_ZH[lower]
  if (direct) return direct || undefined

  if (lower.endsWith('ies') && lower.length > 4) {
    const yForm = `${lower.slice(0, -3)}y`
    const hit =
      FIELD_LABEL_FALLBACK_WORDS[yForm] ?? FIELD_LABEL_ZH[yForm] ?? CUSTOMER_INTEL_FIELD_LABELS[yForm]
    if (hit) return hit
  }
  if (lower.endsWith('es') && lower.length > 4) {
    const base = lower.slice(0, -2)
    const hit =
      FIELD_LABEL_FALLBACK_WORDS[base] ?? FIELD_LABEL_ZH[base] ?? CUSTOMER_INTEL_FIELD_LABELS[base]
    if (hit) return hit
  }
  if (lower.endsWith('s') && lower.length > 3) {
    const base = lower.slice(0, -1)
    const hit =
      FIELD_LABEL_FALLBACK_WORDS[base] ?? FIELD_LABEL_ZH[base] ?? CUSTOMER_INTEL_FIELD_LABELS[base]
    if (hit) return hit
  }
  return undefined
}

function composeFieldLabelParts(parts: string[]): string {
  const meaningful = parts.filter((p) => p && p.trim())
  if (!meaningful.length) return ''
  if (meaningful.every((p) => /[\u4e00-\u9fff]/.test(p))) return meaningful.join('')
  return meaningful.join(' ')
}

/** 无法译出时保留可读英文（下划线 / camelCase → 空格） */
function humanizeEnglishFieldKey(key: string): string {
  return key
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/[_\-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

/** 按 token 贪心匹配短语 + 单词；未命中词典的 token 保留英文 */
function translateEnglishFieldKeyTokens(tokens: string[]): string {
  const parts: string[] = []
  let i = 0
  while (i < tokens.length) {
    let matched = false
    for (let len = Math.min(4, tokens.length - i); len >= 2; len--) {
      const phraseKey = tokens.slice(i, i + len).join('_')
      const phraseLabel = lookupFieldLabelExact(phraseKey)
      if (phraseLabel) {
        parts.push(phraseLabel)
        i += len
        matched = true
        break
      }
    }
    if (!matched) {
      const single = lookupSingleFieldToken(tokens[i])
      parts.push(single ?? tokens[i])
      i += 1
    }
  }
  return composeFieldLabelParts(parts)
}

/** 将 AI 返回的字段键规范为 snake_case（支持 camelCase、空格、中英混合） */
function normalizeCustomerIntelFieldKey(key: string): string {
  let k = key.trim()
  k = k
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
  k = k.toLowerCase()
  k = k.replace(/编码/g, '_code')
  k = k.replace(/^合计/, 'total_')
  k = k.replace(/[\s\-]+/g, '_')
  k = k.replace(/[^a-z0-9_\u4e00-\u9fff]/g, '_')
  k = k.replace(/[\u4e00-\u9fff]+/g, '_')
  k = k.replace(/_+/g, '_').replace(/^_|_$/g, '')
  return k
}

export function resolveCustomerIntelConfidence(raw: string): string {
  const key = raw.trim().toLowerCase()
  return CUSTOMER_INTEL_CONFIDENCE_LABELS[key] ?? raw
}

/**
 * 将报告 content 字段 Key 解析为展示标签。
 * 策略：精确词典 → snake_case / camelCase 拆分 → 短语贪心匹配 → 单词回退（含复数）；
 * 纯中文 Key 原样保留；无法译出的英文词保留英文（下划线转为空格）。
 */
export function resolveCustomerIntelFieldLabel(key: string): string {
  const raw = key.trim()
  if (!raw) return '字段'
  if (isChineseFieldKey(raw)) return raw

  const candidates = [
    raw,
    raw.toLowerCase(),
    normalizeCustomerIntelFieldKey(raw),
    raw.toLowerCase().replace(/[\s\-]+/g, '_')
  ]

  for (const c of candidates) {
    const exact = c ? lookupFieldLabelExact(c) : undefined
    if (exact) return exact
  }

  const tokens = splitFieldKeyTokens(raw)
  if (!tokens.length) return humanizeEnglishFieldKey(raw)

  const auto = translateEnglishFieldKeyTokens(tokens)
  if (auto) return auto

  return humanizeEnglishFieldKey(raw)
}

export type CustomerIntelKvRow = {
  key: string
  label: string
  value: string
  isUrl?: boolean
}

export type CustomerIntelListBlock = {
  title: string
  rows: CustomerIntelKvRow[]
}

function isPlainContentObject(v: unknown): v is Record<string, unknown> {
  return !!v && typeof v === 'object' && !Array.isArray(v)
}

function isEmptyContentValue(v: unknown): boolean {
  if (v == null) return true
  if (typeof v === 'string') return v.trim() === ''
  if (Array.isArray(v)) return v.length === 0
  if (isPlainContentObject(v)) return Object.keys(v).length === 0
  return false
}

function isScalarContentValue(v: unknown): v is string | number | boolean {
  const t = typeof v
  return t === 'string' || t === 'number' || t === 'boolean'
}

function looksLikeStandaloneHttpUrl(v: string): boolean {
  const s = v.trim()
  return /^https?:\/\/\S+$/i.test(s)
}

function isUrlContentValue(_key: string, v: unknown): v is string {
  if (typeof v !== 'string' || !v.trim()) return false
  return looksLikeStandaloneHttpUrl(v)
}

function formatScalarContentValue(v: unknown): string {
  if (v == null) return '—'
  if (typeof v === 'boolean') return v ? '是' : '否'
  if (typeof v === 'number') return String(v)
  const s = String(v).trim()
  if (!s) return '—'
  const mapped = CUSTOMER_INTEL_VALUE_LABELS[s.toLowerCase()]
  return mapped ?? s
}

function isValueUnitObject(v: Record<string, unknown>): boolean {
  const keys = Object.keys(v)
  return keys.length > 0 && keys.every((k) => k === 'value' || k === 'unit')
}

function formatValueUnitObject(v: Record<string, unknown>): string {
  const val = v.value
  const unit = v.unit != null ? String(v.unit).trim() : ''
  if (val == null || val === '') return '—'
  const base = formatScalarContentValue(val)
  return unit ? `${base}${unit}` : base
}

function joinPathLabel(prefix: string | undefined, label: string): string {
  return prefix ? `${prefix} · ${label}` : label
}

function collectContentRows(
  value: unknown,
  labelPrefix: string | undefined,
  keyPath: string,
  rows: CustomerIntelKvRow[],
  listBlocks: CustomerIntelListBlock[],
  depth: number
): void {
  if (depth > 6 || isEmptyContentValue(value)) return

  if (isScalarContentValue(value)) {
    rows.push({
      key: keyPath || labelPrefix || 'value',
      label: labelPrefix || '内容',
      value: formatScalarContentValue(value)
    })
    return
  }

  if (typeof value === 'string' && isUrlContentValue(keyPath.split('.').pop() ?? '', value)) {
    rows.push({
      key: keyPath,
      label: labelPrefix || resolveCustomerIntelFieldLabel(keyPath),
      value: value.trim(),
      isUrl: true
    })
    return
  }

  if (Array.isArray(value)) {
    if (value.every((x) => typeof x === 'string')) {
      rows.push({
        key: keyPath,
        label: labelPrefix || resolveCustomerIntelFieldLabel(keyPath),
        value: value.map((x) => String(x).trim()).filter(Boolean).join('、') || '—'
      })
      return
    }

    if (value.every((x) => isPlainContentObject(x))) {
      const blockTitle = labelPrefix || resolveCustomerIntelFieldLabel(keyPath)
      value.forEach((item, idx) => {
        const blockRows: CustomerIntelKvRow[] = []
        for (const [k, v] of Object.entries(item)) {
          collectContentRows(
            v,
            resolveCustomerIntelFieldLabel(k),
            `${keyPath}[${idx}].${k}`,
            blockRows,
            listBlocks,
            depth + 1
          )
        }
        if (blockRows.length) {
          listBlocks.push({ title: `${blockTitle} ${idx + 1}`, rows: blockRows })
        }
      })
      return
    }

    rows.push({
      key: keyPath,
      label: labelPrefix || resolveCustomerIntelFieldLabel(keyPath),
      value: value.map((x) => formatScalarContentValue(x)).join('、')
    })
    return
  }

  if (!isPlainContentObject(value)) return

  if (isValueUnitObject(value)) {
    rows.push({
      key: keyPath,
      label: labelPrefix || resolveCustomerIntelFieldLabel(keyPath),
      value: formatValueUnitObject(value)
    })
    return
  }

  const scalarEntries: Array<[string, unknown]> = []
  const complexEntries: Array<[string, unknown]> = []

  for (const [k, v] of Object.entries(value)) {
    if (isEmptyContentValue(v)) continue
    if (isScalarContentValue(v) || isUrlContentValue(k, v)) {
      scalarEntries.push([k, v])
    } else if (isPlainContentObject(v) && isValueUnitObject(v)) {
      scalarEntries.push([k, v])
    } else {
      complexEntries.push([k, v])
    }
  }

  for (const [k, v] of scalarEntries) {
    const label = joinPathLabel(labelPrefix, resolveCustomerIntelFieldLabel(k))
    if (isPlainContentObject(v) && isValueUnitObject(v)) {
      rows.push({ key: `${keyPath}.${k}`, label, value: formatValueUnitObject(v) })
      continue
    }
    if (isUrlContentValue(k, v)) {
      rows.push({ key: `${keyPath}.${k}`, label, value: String(v).trim(), isUrl: true })
      continue
    }
    rows.push({ key: `${keyPath}.${k}`, label, value: formatScalarContentValue(v) })
  }

  for (const [k, v] of complexEntries) {
    collectContentRows(
      v,
      joinPathLabel(labelPrefix, resolveCustomerIntelFieldLabel(k)),
      `${keyPath}.${k}`,
      rows,
      listBlocks,
      depth + 1
    )
  }
}

/** 将章节 content 转为中文 Key-Value 行与列表块 */
export function buildCustomerIntelContentView(content: Record<string, unknown> | null | undefined): {
  rows: CustomerIntelKvRow[]
  listBlocks: CustomerIntelListBlock[]
} {
  const rows: CustomerIntelKvRow[] = []
  const listBlocks: CustomerIntelListBlock[] = []
  if (!content || !isPlainContentObject(content)) {
    return { rows, listBlocks }
  }
  collectContentRows(content, undefined, 'content', rows, listBlocks, 0)
  return { rows, listBlocks }
}

export function orderCustomerIntelSectionId(id: string): number {
  const idx = CUSTOMER_INTEL_SECTION_IDS.indexOf(id as CustomerIntelSectionId)
  return idx >= 0 ? idx : 999
}

export function extractCustomerIntelSections(
  report: Record<string, unknown> | null | undefined
): Array<Record<string, unknown>> {
  if (!report || typeof report !== 'object') return []
  const sections = report.sections
  if (!Array.isArray(sections)) return []

  const list = sections.filter((s) => s && typeof s === 'object') as Array<Record<string, unknown>>

  const relations = report.relations
  if (relations && typeof relations === 'object' && !Array.isArray(relations)) {
    const sectionOrder = (relations as Record<string, unknown>).section_order
    if (Array.isArray(sectionOrder) && sectionOrder.length) {
      const orderMap = new Map<string, number>()
      sectionOrder.forEach((id, i) => orderMap.set(String(id), i))
      return [...list].sort((a, b) => {
        const ai = orderMap.get(String(a.id ?? '')) ?? orderCustomerIntelSectionId(String(a.id ?? ''))
        const bi = orderMap.get(String(b.id ?? '')) ?? orderCustomerIntelSectionId(String(b.id ?? ''))
        return ai - bi
      })
    }
  }

  return [...list].sort(
    (a, b) => orderCustomerIntelSectionId(String(a.id ?? '')) - orderCustomerIntelSectionId(String(b.id ?? ''))
  )
}

export type CustomerIntelValidationIssue = {
  path: string
  severity: 'error' | 'warn' | 'info'
  code: string
  message: string
}

export type CustomerIntelValidationResult = {
  valid: boolean
  issues: CustomerIntelValidationIssue[]
}

function isPlainObject(v: unknown): v is Record<string, unknown> {
  return !!v && typeof v === 'object' && !Array.isArray(v)
}

/** 对照 13 章契约校验 AI 返回的 JSON 对象 */
export function validateCustomerIntelJson(data: unknown): CustomerIntelValidationResult {
  const issues: CustomerIntelValidationIssue[] = []

  if (!isPlainObject(data)) {
    issues.push({ path: '$', severity: 'error', code: 'root', message: '根节点应为 JSON object' })
    return { valid: false, issues }
  }

  if (!Array.isArray(data.sections)) {
    issues.push({ path: 'sections', severity: 'error', code: 'missing', message: 'sections 应为数组' })
    return { valid: false, issues }
  }

  const foundIds = new Set<string>()
  for (let i = 0; i < data.sections.length; i++) {
    const section = data.sections[i]
    const path = `sections[${i}]`
    if (!isPlainObject(section)) {
      issues.push({ path, severity: 'error', code: 'type', message: '章节应为 object' })
      continue
    }
    const id = String(section.id ?? '').trim()
    if (!id) {
      issues.push({ path: `${path}.id`, severity: 'warn', code: 'missing', message: '章节缺少 id' })
    } else {
      foundIds.add(id)
      if (!CUSTOMER_INTEL_SECTION_IDS.includes(id as CustomerIntelSectionId)) {
        issues.push({ path: `${path}.id`, severity: 'info', code: 'extra', message: `非标准章节: ${id}` })
      }
    }
  }

  for (const id of CUSTOMER_INTEL_SECTION_IDS) {
    if (!foundIds.has(id)) {
      issues.push({ path: `sections.${id}`, severity: 'warn', code: 'missing', message: `缺少标准章节: ${id}` })
    }
  }

  const valid = !issues.some((i) => i.severity === 'error')
  return { valid, issues }
}
