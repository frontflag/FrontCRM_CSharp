import { describe, expect, it } from 'vitest'
import { buildCustomsDeclarationFlowStations } from '@/utils/customsDeclarationFlowPanel'
import type { CustomsDeclarationFlowAggregatesDto } from '@/api/customs'

const t = (key: string) => key

const base: CustomsDeclarationFlowAggregatesDto = {
  declarationId: 'dec-1',
  sellOrderItems: [
    {
      id: 'soi-1',
      docCode: 'SO-1-01',
      status: 0,
      createTime: '2026-08-01T00:00:00Z',
      customerId: 'cust-1',
      customerName: 'Acme',
      customerCode: 'C001',
      salesOrderId: 'so-1',
      qty: 2,
      unitPrice: 1.5,
      currency: 1
    }
  ],
  salesStockOutNotifies: [
    { id: 'sor-10', docCode: 'STOR10', status: 20, createTime: '2026-08-02T00:00:00Z', qty: 2 }
  ],
  pendlists: [{ id: 'pend-1', docCode: 'STOR10', status: 20, createTime: '2026-08-03T00:00:00Z', qty: 2 }],
  customsStockOutNotifies: [
    {
      id: 'sor-20',
      docCode: 'CTOR20',
      status: 20,
      createTime: '2026-08-04T00:00:00Z',
      qty: 2,
      stockOutType: 20,
      customsDeclarationId: 'dec-1'
    }
  ],
  packing: {
    id: 'pk-1',
    docCode: 'PK001',
    status: 100,
    createTime: '2026-08-05T00:00:00Z',
    qty: 2,
    stockOutType: 20,
    customsDeclarationId: 'dec-1'
  },
  declaration: {
    id: 'dec-1',
    docCode: 'CD001',
    status: 2,
    createTime: '2026-08-06T00:00:00Z',
    brokerName: '报关行A',
    qty: 2
  },
  stockOuts: [
    {
      id: 'so-out-1',
      docCode: 'SOUT001',
      status: 2,
      createTime: '2026-08-07T00:00:00Z',
      qty: 2,
      stockOutType: 20,
      customsDeclarationId: 'dec-1'
    }
  ],
  arrivals: [],
  qcs: [],
  stockIns: []
}

describe('buildCustomsDeclarationFlowStations', () => {
  it('uses 10 stations without picking and marks declaration as current', () => {
    const stations = buildCustomsDeclarationFlowStations(base, t)
    expect(stations.map((s) => s.key)).toEqual([
      'sellOrderItem',
      'stockOutNotify',
      'pendlist',
      'customsStockOutNotify',
      'packing',
      'customsDeclaration',
      'stockOut',
      'arrivalNotify',
      'qc',
      'customsStockIn'
    ])
    expect(stations.some((s) => s.key === 'picking')).toBe(false)
  })

  it('puts broker only on the declaration card and qty labels per station', () => {
    const stations = buildCustomsDeclarationFlowStations(base, t)
    const decl = stations.find((s) => s.key === 'customsDeclaration')?.cards[0]
    expect(decl?.brokerName).toBe('报关行A')
    expect(decl?.qtyText).toBeNull()
    const sell = stations.find((s) => s.key === 'sellOrderItem')?.cards[0]
    expect(sell?.qtyLabelKey).toBe('customsPages.declarations.flowPanel.fields.salesOutQty')
    expect(sell?.unitPriceText).toBeTruthy()
    expect(sell?.customerId).toBe('cust-1')
    const packing = stations.find((s) => s.key === 'packing')?.cards[0]
    expect(packing?.stockOutType).toBe(20)
    expect(packing?.customsDeclarationId).toBe('dec-1')
  })

  it('clears customerId when masked', () => {
    const stations = buildCustomsDeclarationFlowStations(base, t, { maskSensitive: true })
    const sell = stations.find((s) => s.key === 'sellOrderItem')?.cards[0]
    expect(sell?.customerId).toBeNull()
    expect(sell?.customerName).toBe('—')
  })

  it('marks the customs stock-out station from the stock-out business status', () => {
    const statusOf = (stockOuts: CustomsDeclarationFlowAggregatesDto['stockOuts']) =>
      buildCustomsDeclarationFlowStations({ ...base, stockOuts }, t).find((s) => s.key === 'stockOut')
        ?.stationStatus

    expect(statusOf(base.stockOuts)).toBe('active')
    expect(statusOf([{ ...base.stockOuts[0], status: 0 }])).toBe('active')
    expect(statusOf([{ ...base.stockOuts[0], status: 1 }])).toBe('active')
    expect(statusOf([{ ...base.stockOuts[0], status: 4 }])).toBe('done')
    expect(statusOf([{ ...base.stockOuts[0], status: 3 }])).toBe('cancelled')
    expect(
      statusOf([
        { ...base.stockOuts[0], status: 4 },
        { ...base.stockOuts[0], id: 'so-out-2', status: 3 }
      ])
    ).toBe('done')
    expect(
      statusOf([
        { ...base.stockOuts[0], status: 2 },
        { ...base.stockOuts[0], id: 'so-out-2', status: 4 }
      ])
    ).toBe('active')
    expect(statusOf([])).toBe('empty')
  })

  it('derives station badges from business outcome and ignores deleted docs', () => {
    const stations = buildCustomsDeclarationFlowStations(
      {
        ...base,
        sellOrderItems: [
          { ...base.sellOrderItems[0], status: 0, receiptProgressStatus: 2, invoiceProgressStatus: 2 }
        ],
        salesStockOutNotifies: [{ ...base.salesStockOutNotifies[0], status: -1 }],
        pendlists: [{ ...base.pendlists[0], status: 10 }],
        customsStockOutNotifies: [{ ...base.customsStockOutNotifies[0], status: 100 }],
        declaration: { ...base.declaration, status: -1 },
        qcs: [
          { id: 'qc-pass', docCode: 'QC1', status: 100 },
          { id: 'qc-fail', docCode: 'QC2', status: -1 }
        ],
        stockIns: [{ id: 'si-1', docCode: 'STI1', status: 3 }]
      },
      t
    )
    expect(stations.find((s) => s.key === 'sellOrderItem')?.stationStatus).toBe('done')
    expect(stations.find((s) => s.key === 'sellOrderItem')?.cards[0]?.statusText).toBe('正常')
    expect(stations.find((s) => s.key === 'stockOutNotify')?.stationStatus).toBe('cancelled')
    expect(stations.find((s) => s.key === 'pendlist')?.stationStatus).toBe('done')
    expect(stations.find((s) => s.key === 'customsStockOutNotify')?.stationStatus).toBe('done')
    expect(stations.find((s) => s.key === 'customsDeclaration')?.stationStatus).toBe('cancelled')
    expect(stations.find((s) => s.key === 'qc')?.stationStatus).toBe('failed')
    expect(stations.find((s) => s.key === 'customsStockIn')?.stationStatus).toBe('cancelled')

    const deletedOnly = buildCustomsDeclarationFlowStations(
      {
        ...base,
        stockOuts: [{ ...base.stockOuts[0], status: 4, isDeleted: true }]
      },
      t
    )
    expect(deletedOnly.find((s) => s.key === 'stockOut')?.stationStatus).toBe('cancelled')

    const partialSell = buildCustomsDeclarationFlowStations(
      {
        ...base,
        sellOrderItems: [
          { ...base.sellOrderItems[0], status: 0, receiptProgressStatus: 2, invoiceProgressStatus: 1 }
        ]
      },
      t
    )
    expect(partialSell.find((s) => s.key === 'sellOrderItem')?.stationStatus).toBe('active')
  })

  it('translates stock-out, arrival, qc, and stock-in status codes', () => {
    const stations = buildCustomsDeclarationFlowStations(
      {
        ...base,
        arrivals: [{ id: 'an-1', docCode: 'STIR1', status: 100, createTime: '2026-08-08T00:00:00Z', qty: 2 }],
        qcs: [{ id: 'qc-1', docCode: 'QC1', status: 100, createTime: '2026-08-09T00:00:00Z' }],
        stockIns: [{ id: 'si-1', docCode: 'STI1', status: 2, createTime: '2026-08-10T00:00:00Z', qty: 2 }]
      },
      t
    )
    expect(stations.find((s) => s.key === 'stockOut')?.cards[0]?.statusText).toBe('stockOutList.status.done')
    expect(stations.find((s) => s.key === 'arrivalNotify')?.cards[0]?.statusText).toBe(
      'arrivalNoticeList.status.stocked'
    )
    expect(stations.find((s) => s.key === 'qc')?.cards[0]?.statusText).toBe('qcList.qcStatus.passed')
    expect(stations.find((s) => s.key === 'customsStockIn')?.cards[0]?.statusText).toBe('stockInList.status.done')
  })

  it('renders empty downstream stations as empty', () => {
    const stations = buildCustomsDeclarationFlowStations(base, t)
    expect(stations.find((s) => s.key === 'arrivalNotify')?.stationStatus).toBe('empty')
    expect(stations.find((s) => s.key === 'customsStockIn')?.stationStatus).toBe('empty')
  })
})
