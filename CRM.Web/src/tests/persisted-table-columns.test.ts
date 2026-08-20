import { beforeEach, describe, expect, it } from 'vitest'
import { nextTick, ref } from 'vue'
import {
  isColumnWidthPersistable,
  mergeLayout,
  resolveColumnKeyFromDrag,
  sanitizeColumnWidths,
  usePersistedTableColumns,
  type CrmTableColumnDef
} from '@/composables/usePersistedTableColumns'

const baseDefs: CrmTableColumnDef[] = [
  { key: 'sel', type: 'selection', hideable: false },
  { key: 'code', label: '编号', prop: 'code', minWidth: 160 },
  { key: 'name', label: '名称', prop: 'name', minWidth: 200 },
  { key: 'customer', label: '客户', prop: 'customer', minWidth: 160, className: 'customer-extend-col' },
  {
    key: 'actions',
    label: '操作',
    pinned: 'end',
    fixed: 'right',
    className: 'op-col',
    hideable: false,
    resizable: false
  }
]

describe('isColumnWidthPersistable', () => {
  it('普通数据列可记宽', () => {
    expect(isColumnWidthPersistable(baseDefs[1]!)).toBe(true)
    expect(isColumnWidthPersistable(baseDefs[2]!)).toBe(true)
  })

  it('勾选列、操作列、扩展列不记宽', () => {
    expect(isColumnWidthPersistable(baseDefs[0]!)).toBe(false)
    expect(isColumnWidthPersistable(baseDefs[3]!)).toBe(false)
    expect(isColumnWidthPersistable(baseDefs[4]!)).toBe(false)
  })
})

describe('sanitizeColumnWidths / mergeLayout', () => {
  it('只保留仍存在且可记宽的 key，非法值丢弃', () => {
    const widths = sanitizeColumnWidths(baseDefs, {
      code: 240,
      name: '180',
      customer: 400,
      actions: 90,
      gone: 120,
      bad: 'x',
      zero: 0
    })
    expect(widths).toEqual({ code: 240, name: 180 })
  })

  it('版本更新插入新列时，旧列宽仍有效，新列无记录', () => {
    const saved = {
      middleOrder: ['name', 'code'],
      hiddenKeys: [] as string[],
      columnWidths: { name: 280, code: 200 }
    }
    const withNewCol: CrmTableColumnDef[] = [
      baseDefs[0]!,
      baseDefs[1]!,
      { key: 'brand', label: '品牌', prop: 'brand', minWidth: 120 },
      baseDefs[2]!,
      baseDefs[3]!,
      baseDefs[4]!
    ]
    const merged = mergeLayout(withNewCol, saved)
    expect(merged.columnWidths).toEqual({ name: 280, code: 200 })
    expect(merged.columnWidths.brand).toBeUndefined()
    expect(merged.middleOrder).toContain('brand')
    expect(merged.middleOrder).toContain('code')
    expect(merged.middleOrder).toContain('name')
  })

  it('列 key 改名后旧宽度对不上，按新列处理', () => {
    const renamed: CrmTableColumnDef[] = [
      baseDefs[0]!,
      { key: 'docCode', label: '编号', prop: 'docCode', minWidth: 160 },
      baseDefs[2]!,
      baseDefs[4]!
    ]
    const merged = mergeLayout(renamed, { columnWidths: { code: 240, name: 300 } })
    expect(merged.columnWidths).toEqual({ name: 300 })
  })

  it('无已存布局时列宽为空（用代码默认宽）', () => {
    const merged = mergeLayout(baseDefs, null)
    expect(merged.columnWidths).toEqual({})
  })
})

describe('resolveColumnKeyFromDrag', () => {
  it('优先 columnKey，其次 property 对上 key 或 prop', () => {
    expect(resolveColumnKeyFromDrag(baseDefs, { columnKey: 'name', property: 'code' })).toBe('name')
    expect(resolveColumnKeyFromDrag(baseDefs, { property: 'code' })).toBe('code')
    expect(
      resolveColumnKeyFromDrag(
        [{ key: 'rfqNo', prop: 'rfqCode', label: '需求单号' }],
        { property: 'rfqCode' }
      )
    ).toBe('rfqNo')
  })
})

describe('usePersistedTableColumns columnWidths 往返', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('拖过的列宽写入存储；插入新列后旧宽仍在，新列用默认', async () => {
    const tableKey = ref('unit-col-width')
    const cols = ref<CrmTableColumnDef[]>([
      { key: 'code', label: '编号', prop: 'code', minWidth: 160 },
      { key: 'name', label: '名称', prop: 'name', minWidth: 200 }
    ])
    const persist = usePersistedTableColumns(tableKey, cols)
    await nextTick()
    persist.applyHeaderDragWidth({ columnKey: 'code' }, 240)
    await nextTick()

    const stored = JSON.parse(localStorage.getItem('crm-table-columns:v1:unit-col-width') ?? 'null')
    expect(stored?.columnWidths?.code).toBe(240)

    cols.value = [
      { key: 'code', label: '编号', prop: 'code', minWidth: 160 },
      { key: 'brand', label: '品牌', minWidth: 120 },
      { key: 'name', label: '名称', prop: 'name', minWidth: 200 }
    ]
    await nextTick()
    expect(persist.columnWidths.value.code).toBe(240)
    expect(persist.columnWidths.value.brand).toBeUndefined()
    const visible = persist.orderedVisibleColumns.value
    expect(visible.find((c) => c.key === 'code')?.width).toBe(240)
    expect(visible.find((c) => c.key === 'brand')?.width).toBeUndefined()
  })
})
