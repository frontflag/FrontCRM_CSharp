import { describe, expect, it } from 'vitest'
import type { CrmTableColumnDef } from '@/composables/usePersistedTableColumns'
import {
  clampColumnResizeWidth,
  isHeaderColumnResizable,
  isHeaderResizeControlTarget,
  isPointerOverTableHeader,
  pickNearestBoundaryIndex,
  parseColumnWidthPx,
  resolveHeaderResizeMinWidth
} from '@/utils/crmTableHeaderResizeGuide'

describe('pickNearestBoundaryIndex', () => {
  it('选离鼠标最近的列右缘', () => {
    expect(pickNearestBoundaryIndex([100, 250, 400], 120)).toBe(0)
    expect(pickNearestBoundaryIndex([100, 250, 400], 200)).toBe(1)
    expect(pickNearestBoundaryIndex([100, 250, 400], 390)).toBe(2)
  })

  it('空列表返回 -1', () => {
    expect(pickNearestBoundaryIndex([], 10)).toBe(-1)
  })
})

describe('isHeaderColumnResizable', () => {
  it('普通列与扩展列可拖，勾选/操作列不可', () => {
    expect(isHeaderColumnResizable({ key: 'code', minWidth: 160 })).toBe(true)
    expect(
      isHeaderColumnResizable({ key: 'customer', className: 'customer-extend-col', minWidth: 160 })
    ).toBe(true)
    expect(isHeaderColumnResizable({ key: 'sel', type: 'selection' })).toBe(false)
    expect(
      isHeaderColumnResizable({
        key: 'actions',
        pinned: 'end',
        fixed: 'right',
        className: 'op-col',
        resizable: false
      } as CrmTableColumnDef)
    ).toBe(false)
  })
})

describe('clamp / parse', () => {
  it('列宽夹取与解析', () => {
    expect(clampColumnResizeWidth(10, 160)).toBe(160)
    expect(clampColumnResizeWidth(5000, 160)).toBe(4000)
    expect(parseColumnWidthPx('180', 30)).toBe(180)
    expect(parseColumnWidthPx(undefined, 30)).toBe(30)
  })
})

describe('resolveHeaderResizeMinWidth', () => {
  it('扩展列 minWidth 等于当前宽时仍允许往左收', () => {
    expect(
      resolveHeaderResizeMinWidth(
        { key: 'customer', className: 'customer-extend-col', minWidth: 320, width: 320 },
        320
      )
    ).toBe(30)
  })

  it('普通列保留声明的 minWidth', () => {
    expect(resolveHeaderResizeMinWidth({ key: 'code', minWidth: 160 }, 240)).toBe(160)
  })
})

describe('header target helpers', () => {
  it('识别子列拖条与表头区域', () => {
    const resizer = document.createElement('span')
    resizer.className = 'customer-extend-sub-col-resizer'
    document.body.appendChild(resizer)
    expect(isHeaderResizeControlTarget(resizer)).toBe(true)

    const th = document.createElement('th')
    th.className = 'el-table__cell'
    const wrap = document.createElement('div')
    wrap.className = 'el-table__header-wrapper'
    wrap.appendChild(th)
    expect(isPointerOverTableHeader(th)).toBe(true)
    expect(isPointerOverTableHeader(document.body)).toBe(false)
    resizer.remove()
  })
})
