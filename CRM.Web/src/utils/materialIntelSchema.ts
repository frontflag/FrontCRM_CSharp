/** material.intel.lookup Phase 3 契约 v2（与 ai_prompt_template.json_schema_hint 对齐） */

export const MATERIAL_INTEL_SCHEMA_VERSION = 2

export const MATERIAL_INTEL_SCHEMA_V2_HINT = {
  brand_info: {
    brand: 'string|null',
    manufacturer: 'string|null',
    origin: 'string|null',
    product_line: 'string|null',
    product_category: 'string|null',
    series: 'string|null'
  },
  spec_params: {
    category: 'string|null',
    part_number_breakdown: [{ segment: 'string', meaning: 'string' }],
    technical_features: ['string'],
    electrical_params: {},
    datasheet_url: 'string|null',
    image_url: 'string|null'
  },
  application_areas: ['string'],
  alternatives: [{ part_number: 'string', brand: 'string|null', note: 'string|null' }],
  pricing: {
    market_price: {
      reference_price: 'string|null',
      currency: 'string|null',
      note: 'string|null'
    },
    market_conditions: {
      availability: 'string|null',
      trend: 'string|null',
      note: 'string|null'
    },
    price_tiers: [{ quantity: 'string', unit_price: 'string', currency: 'string|null' }],
    distributors: [
      {
        distributor: 'string',
        price_range: 'string|null',
        currency: 'string|null',
        stock_status: 'string|null',
        moq: 'string|null',
        last_updated: 'string|null'
      }
    ]
  },
  industry_news: [{ title: 'string', url: 'string|null', summary: 'string|null' }],
  disclaimer: 'string'
} as const

export type MaterialIntelValidationSeverity = 'error' | 'warn' | 'info'

export type MaterialIntelValidationIssue = {
  path: string
  severity: MaterialIntelValidationSeverity
  code: string
  message: string
}

export type MaterialIntelValidationResult = {
  valid: boolean
  issues: MaterialIntelValidationIssue[]
  missingPaths: string[]
  extraPaths: string[]
  typeMismatchPaths: string[]
}

const ROOT_KEYS = Object.keys(MATERIAL_INTEL_SCHEMA_V2_HINT)

function isPlainObject(v: unknown): v is Record<string, unknown> {
  return v !== null && typeof v === 'object' && !Array.isArray(v)
}

function isSnakeCaseKey(key: string): boolean {
  return /^[a-z][a-z0-9_]*$/.test(key)
}

function typeLabel(v: unknown): string {
  if (v === null) return 'null'
  if (Array.isArray(v)) return 'array'
  return typeof v
}

function pushIssue(
  issues: MaterialIntelValidationIssue[],
  path: string,
  severity: MaterialIntelValidationSeverity,
  code: string,
  message: string
) {
  issues.push({ path, severity, code, message })
}

function validateStringOrNull(value: unknown, path: string, issues: MaterialIntelValidationIssue[], required = false) {
  if (value === null || value === undefined) {
    if (required) pushIssue(issues, path, 'error', 'required', '应为非空 string，当前为 null')
    return
  }
  if (typeof value !== 'string') {
    pushIssue(issues, path, 'error', 'type', `应为 string|null，实际为 ${typeLabel(value)}`)
    return
  }
  if (required && !value.trim()) {
    pushIssue(issues, path, 'warn', 'empty', '应为非空 string')
  }
}

function validateStringArray(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (!Array.isArray(value)) {
    pushIssue(issues, path, 'error', 'type', `应为 string[]，实际为 ${typeLabel(value)}`)
    return
  }
  value.forEach((item, i) => {
    if (typeof item !== 'string') {
      pushIssue(issues, path + `[${i}]`, 'error', 'type', `数组项应为 string，实际为 ${typeLabel(item)}`)
    }
  })
}

function validateObjectArray(
  value: unknown,
  path: string,
  issues: MaterialIntelValidationIssue[],
  rowShape: Record<string, 'string' | 'string|null'>
) {
  if (!Array.isArray(value)) {
    pushIssue(issues, path, 'error', 'type', `应为 object[]，实际为 ${typeLabel(value)}`)
    return
  }
  if (value.length === 0) return
  value.forEach((item, i) => {
    const rowPath = `${path}[${i}]`
    if (!isPlainObject(item)) {
      pushIssue(issues, rowPath, 'error', 'type', `应为 object，实际为 ${typeLabel(item)}`)
      return
    }
    for (const [key, rule] of Object.entries(rowShape)) {
      const cell = item[key]
      if (rule === 'string') validateStringOrNull(cell, `${rowPath}.${key}`, issues, true)
      else validateStringOrNull(cell, `${rowPath}.${key}`, issues, false)
    }
    for (const key of Object.keys(item)) {
      if (!(key in rowShape)) {
        pushIssue(issues, `${rowPath}.${key}`, 'info', 'extra', '契约 v2 未声明的扩展字段')
      }
      if (!isSnakeCaseKey(key)) {
        pushIssue(issues, `${rowPath}.${key}`, 'warn', 'naming', '键名应使用 snake_case')
      }
    }
  })
}

function validateElectricalParams(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (!isPlainObject(value)) {
    pushIssue(issues, path, 'error', 'type', `应为 object，实际为 ${typeLabel(value)}`)
    return
  }
  for (const [key, cell] of Object.entries(value)) {
    if (!isSnakeCaseKey(key)) {
      pushIssue(issues, `${path}.${key}`, 'warn', 'naming', '键名应使用 snake_case')
    }
    const t = typeof cell
    if (cell !== null && t !== 'string' && t !== 'number' && t !== 'boolean') {
      pushIssue(issues, `${path}.${key}`, 'warn', 'type', 'electrical_params 值应为 string|number|boolean|null')
    }
  }
}

function validateBrandInfo(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (!isPlainObject(value)) {
    pushIssue(issues, path, 'error', 'type', `应为 object，实际为 ${typeLabel(value)}`)
    return
  }
  const allowed = ['brand', 'manufacturer', 'origin', 'product_line', 'product_category', 'series']
  for (const key of allowed) {
    if (key in value) validateStringOrNull(value[key], `${path}.${key}`, issues, false)
  }
  for (const key of Object.keys(value)) {
    if (!allowed.includes(key)) {
      pushIssue(issues, `${path}.${key}`, 'info', 'extra', 'brand_info 扩展字段')
    }
  }
}

function validateSpecParams(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (!isPlainObject(value)) {
    pushIssue(issues, path, 'error', 'type', `应为 object，实际为 ${typeLabel(value)}`)
    return
  }
  if ('category' in value) validateStringOrNull(value.category, `${path}.category`, issues)
  if ('datasheet_url' in value) validateStringOrNull(value.datasheet_url, `${path}.datasheet_url`, issues)
  else pushIssue(issues, `${path}.datasheet_url`, 'warn', 'missing', '建议包含 datasheet_url')
  if ('image_url' in value) validateStringOrNull(value.image_url, `${path}.image_url`, issues)
  else pushIssue(issues, `${path}.image_url`, 'warn', 'missing', '建议包含 image_url')
  if ('part_number_breakdown' in value) {
    validateObjectArray(value.part_number_breakdown, `${path}.part_number_breakdown`, issues, {
      segment: 'string',
      meaning: 'string'
    })
  }
  if ('technical_features' in value) validateStringArray(value.technical_features, `${path}.technical_features`, issues)
  if ('electrical_params' in value) validateElectricalParams(value.electrical_params, `${path}.electrical_params`, issues)
}

function validatePricing(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (Array.isArray(value)) {
    pushIssue(issues, path, 'warn', 'deprecated', 'pricing 曾为 array，v2 应为 object')
    return
  }
  if (!isPlainObject(value)) {
    pushIssue(issues, path, 'error', 'type', `应为 object，实际为 ${typeLabel(value)}`)
    return
  }
  if ('market_price' in value && isPlainObject(value.market_price)) {
    for (const key of ['reference_price', 'currency', 'note']) {
      if (key in value.market_price) {
        validateStringOrNull(value.market_price[key], `${path}.market_price.${key}`, issues)
      }
    }
  }
  if ('market_conditions' in value && isPlainObject(value.market_conditions)) {
    for (const key of ['availability', 'trend', 'note']) {
      if (key in value.market_conditions) {
        validateStringOrNull(value.market_conditions[key], `${path}.market_conditions.${key}`, issues)
      }
    }
  }
  if ('price_tiers' in value) {
    validateObjectArray(value.price_tiers, `${path}.price_tiers`, issues, {
      quantity: 'string',
      unit_price: 'string',
      currency: 'string|null'
    })
  }
  if ('distributors' in value) {
    validateObjectArray(value.distributors, `${path}.distributors`, issues, {
      distributor: 'string',
      price_range: 'string|null',
      currency: 'string|null',
      stock_status: 'string|null',
      moq: 'string|null',
      last_updated: 'string|null'
    })
  }
}

function validateAlternatives(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (Array.isArray(value) && value.length > 0 && typeof value[0] === 'string') {
    pushIssue(issues, path, 'warn', 'deprecated', 'alternatives v2 应为 {part_number, brand, note}[]，当前为 string[]')
    validateStringArray(value, path, issues)
    return
  }
  validateObjectArray(value, path, issues, {
    part_number: 'string',
    brand: 'string|null',
    note: 'string|null'
  })
}

function validateIndustryNews(value: unknown, path: string, issues: MaterialIntelValidationIssue[]) {
  if (Array.isArray(value) && value.length > 0 && typeof value[0] === 'string') {
    pushIssue(issues, path, 'warn', 'deprecated', 'industry_news v2 应为 {title, url, summary}[]，当前为 string[]')
    validateStringArray(value, path, issues)
    return
  }
  validateObjectArray(value, path, issues, {
    title: 'string',
    url: 'string|null',
    summary: 'string|null'
  })
}

/** 对照契约 v2 校验 AI 返回的 JSON 对象 */
export function validateMaterialIntelJson(data: unknown): MaterialIntelValidationResult {
  const issues: MaterialIntelValidationIssue[] = []

  if (!isPlainObject(data)) {
    pushIssue(issues, '$', 'error', 'root', '根节点应为 JSON object')
    return summarize(issues)
  }

  for (const key of ROOT_KEYS) {
    if (!(key in data)) {
      pushIssue(issues, key, 'warn', 'missing', '契约 v2 建议包含此顶层键')
    } else if (!isSnakeCaseKey(key)) {
      pushIssue(issues, key, 'warn', 'naming', '键名应使用 snake_case')
    }
  }

  for (const key of Object.keys(data)) {
    if (!ROOT_KEYS.includes(key)) {
      pushIssue(issues, key, 'info', 'extra', '契约 v2 未声明的顶层扩展字段')
    }
  }

  if ('disclaimer' in data) validateStringOrNull(data.disclaimer, 'disclaimer', issues, false)
  if ('brand_info' in data) validateBrandInfo(data.brand_info, 'brand_info', issues)
  if ('spec_params' in data) validateSpecParams(data.spec_params, 'spec_params', issues)
  if ('application_areas' in data) validateStringArray(data.application_areas, 'application_areas', issues)
  if ('alternatives' in data) validateAlternatives(data.alternatives, 'alternatives', issues)
  if ('pricing' in data) validatePricing(data.pricing, 'pricing', issues)
  if ('industry_news' in data) validateIndustryNews(data.industry_news, 'industry_news', issues)

  return summarize(issues)
}

function summarize(issues: MaterialIntelValidationIssue[]): MaterialIntelValidationResult {
  const missingPaths = issues.filter((i) => i.code === 'missing').map((i) => i.path)
  const extraPaths = issues.filter((i) => i.code === 'extra').map((i) => i.path)
  const typeMismatchPaths = issues.filter((i) => i.code === 'type').map((i) => i.path)
  const valid = !issues.some((i) => i.severity === 'error')
  return { valid, issues, missingPaths, extraPaths, typeMismatchPaths }
}

export function materialIntelSchemaHintJson(): string {
  return JSON.stringify(MATERIAL_INTEL_SCHEMA_V2_HINT)
}
