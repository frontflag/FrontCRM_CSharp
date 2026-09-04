import { describe, expect, it } from 'vitest'
import { buildStockItemFlowStations } from '@/utils/stockItemFlowPanel'

const t = (key: string) => key

const row = {
  stockItemId: 'si-1',
  stockItemCode: 'STK-1-01',
  stockAggregateId: 'agg-1',
  outboundStatus: 1,
  stockInDate: '2026-08-01T00:00:00Z',
  vendorName: 'V1',
  vendorCode: 'V001',
  customerId: 'c-1',
  customerName: 'Cust',
  salespersonName: 'Sales',
  purchasePrice: 1.5,
  purchaseCurrency: 2,
  salesPrice: 3,
  salesCurrency: 2,
  qtyInbound: 10,
  qtyStockOut: 2,
  sellOrderItemCode: 'SO-1-01'
}

describe('buildStockItemFlowStations', () => {
  it('always renders seven stations in business order', () => {
    const stations = buildStockItemFlowStations(row, null, t)
    expect(stations.map((s) => s.key)).toEqual([
      'purchaseOrderItem',
      'qc',
      'stockIn',
      'stockItem',
      'stockOutNotify',
      'packing',
      'stockOut'
    ])
    expect(stations.filter((s) => s.key !== 'stockItem').every((s) => s.stationStatus === 'empty')).toBe(true)
    expect(stations.find((s) => s.key === 'stockItem')?.stationStatus).toBe('active')
  })

  it('keeps empty downstream stations when this layer has no packing/stock-out', () => {
    const stations = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 1, qty: 10, qty2: 0 },
      stockOutNotifies: [],
      packings: [],
      stockOuts: []
    }, t)
    expect(stations.find((s) => s.key === 'stockOutNotify')?.cards).toHaveLength(0)
    expect(stations.find((s) => s.key === 'packing')?.cards).toHaveLength(0)
    expect(stations.find((s) => s.key === 'stockOut')?.cards).toHaveLength(0)
  })

  it('uses this-layer packing qty rather than a sibling line', () => {
    const stations = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      packings: [{ id: 'pk-1', docCode: 'Pak1', status: 10, qty: 4, createTime: '2026-08-02T00:00:00Z' }]
    }, t)
    const packing = stations.find((s) => s.key === 'packing')
    expect(packing?.cards).toHaveLength(1)
    expect(packing?.cards[0].qtyText).toBe('4 pcs')
    expect(packing?.cards[0].qtyText).not.toBe('10 pcs')
  })

  it('puts inbound type beside doc no and flags customs icon only when declaration is linked', () => {
    const customs = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockIn: {
        id: 'sti-1',
        docCode: 'STI0026U',
        status: 1,
        stockInType: 20,
        qty: 310,
        createTime: '2026-08-15T18:03:00Z'
      },
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 1, qty: 10, qty2: 0 }
    }, t)
    const customsCard = customs.find((s) => s.key === 'stockIn')?.cards[0]
    expect(customsCard?.bizTypeText).toBe('stockInList.stockInTypeLabels.customs')
    expect(customsCard?.stockInType).toBe(20)
    expect(customsCard?.showCustomsIcon).toBe(false)
    const customsItem = customs.find((s) => s.key === 'stockItem')?.cards[0]
    expect(customsItem?.showPerson).toBe(false)
    expect(customsItem?.bizTypeText).toBe('stockInList.stockInTypeLabels.customs')
    expect(customsItem?.stockInType).toBe(20)
    expect(customsItem?.showCustomsIcon).toBe(false)

    const customsLinked = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockIn: {
        id: 'sti-1',
        docCode: 'STI0026U',
        status: 1,
        stockInType: 20,
        qty: 310,
        customsDeclarationId: 'dec-1',
        customsDeclarationCode: 'CD001'
      },
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 1, qty: 10, qty2: 0 }
    }, t)
    expect(customsLinked.find((s) => s.key === 'stockIn')?.cards[0].showCustomsIcon).toBe(true)
    expect(customsLinked.find((s) => s.key === 'stockItem')?.cards[0].showCustomsIcon).toBe(true)
    expect(customsLinked.find((s) => s.key === 'stockItem')?.cards[0].customsDeclarationId).toBe('dec-1')

    const customsItemTyped = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: {
        id: 'si-1',
        docCode: 'STK-1-01',
        status: 1,
        qty: 10,
        qty2: 0,
        stockInType: 20,
        customsDeclarationId: 'dec-2',
        customsDeclarationCode: 'CD002'
      }
    }, t).find((s) => s.key === 'stockItem')?.cards[0]
    expect(customsItemTyped?.bizTypeText).toBe('stockInList.stockInTypeLabels.customs')
    expect(customsItemTyped?.showCustomsIcon).toBe(true)
    expect(customsItemTyped?.showPerson).toBe(false)

    const purchase = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockIn: {
        id: 'sti-2',
        docCode: 'STI0026P',
        status: 1,
        stockInType: 10,
        qty: 10
      },
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 1, qty: 10, qty2: 0 }
    }, t)
    const purchaseCard = purchase.find((s) => s.key === 'stockIn')?.cards[0]
    expect(purchaseCard?.bizTypeText).toBe('stockInList.stockInTypeLabels.purchase')
    expect(purchaseCard?.showCustomsIcon).toBe(false)
  })

  it('puts stock-out type beside created date and flags customs icon only when declaration is linked', () => {
    const customs = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      stockOutNotifies: [
        { id: 'n-1', docCode: 'STOR1', status: 10, stockOutType: 20, qty: 4, createTime: '2026-08-02T00:00:00Z' }
      ]
    }, t)
    const customsNotify = customs.find((s) => s.key === 'stockOutNotify')?.cards[0]
    expect(customsNotify?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.customs')
    expect(customsNotify?.stockOutType).toBe(20)
    expect(customsNotify?.showCustomsIcon).toBe(false)
    expect(customsNotify?.showPerson).toBe(false)
    expect(customsNotify?.bizTypeLabelKey).toBe('inventoryStockItemList.flowPanel.fields.stockOutType')
    const stockItemFromNotify = customs.find((s) => s.key === 'stockItem')?.cards[0]
    expect(stockItemFromNotify?.stockOutType).toBe(20)
    expect(stockItemFromNotify?.stockOutCustomsDeclarationId).toBeNull()

    const customsNotifyLinked = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      stockOutNotifies: [
        {
          id: 'n-1',
          docCode: 'STOR1',
          status: 10,
          stockOutType: 20,
          qty: 4,
          createTime: '2026-08-02T00:00:00Z',
          customsDeclarationId: 'dec-out-1',
          customsDeclarationCode: 'CDOUT1'
        }
      ]
    }, t)
    expect(customsNotifyLinked.find((s) => s.key === 'stockOutNotify')?.cards[0].showCustomsIcon).toBe(true)
    expect(customsNotifyLinked.find((s) => s.key === 'stockItem')?.cards[0].stockOutCustomsDeclarationId).toBe(
      'dec-out-1'
    )

    const sales = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      stockOutNotifies: [
        { id: 'n-2', docCode: 'STOR2', status: 10, stockOutType: 10, qty: 4, createTime: '2026-08-02T00:00:00Z' }
      ]
    }, t)
    const salesNotify = sales.find((s) => s.key === 'stockOutNotify')?.cards[0]
    expect(salesNotify?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.sales')
    expect(salesNotify?.showCustomsIcon).toBe(false)

    const customsOut = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 3, qty: 10, qty2: 4 },
      stockOuts: [
        { id: 'so-1', docCode: 'STO1', status: 2, stockOutType: 20, qty: 4, createTime: '2026-08-03T00:00:00Z' }
      ]
    }, t)
    const customsOutCard = customsOut.find((s) => s.key === 'stockOut')?.cards[0]
    expect(customsOutCard?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.customs')
    expect(customsOutCard?.showCustomsIcon).toBe(false)
    expect(customsOutCard?.showPerson).toBe(false)

    const customsOutLinked = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 3, qty: 10, qty2: 4 },
      stockOuts: [
        {
          id: 'so-1',
          docCode: 'STO1',
          status: 2,
          stockOutType: 20,
          qty: 4,
          createTime: '2026-08-03T00:00:00Z',
          customsDeclarationId: 'dec-so-1'
        }
      ]
    }, t)
    expect(customsOutLinked.find((s) => s.key === 'stockOut')?.cards[0].showCustomsIcon).toBe(true)

    const salesOut = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 3, qty: 10, qty2: 4 },
      stockOuts: [
        { id: 'so-2', docCode: 'STO2', status: 2, stockOutType: 10, qty: 4, createTime: '2026-08-03T00:00:00Z' }
      ]
    }, t)
    const salesOutCard = salesOut.find((s) => s.key === 'stockOut')?.cards[0]
    expect(salesOutCard?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.sales')
    expect(salesOutCard?.showCustomsIcon).toBe(false)

    const transferOut = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 3, qty: 10, qty2: 4 },
      stockOuts: [
        { id: 'so-tr', docCode: 'STO00054', status: 2, stockOutType: 3, qty: 4, createTime: '2026-08-03T00:00:00Z' }
      ]
    }, t)
    const transferOutCard = transferOut.find((s) => s.key === 'stockOut')?.cards[0]
    expect(transferOutCard?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.transfer')
    expect(transferOutCard?.showCustomer).toBe(false)

    const customsPacking = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      packings: [
        { id: 'pk-1', docCode: 'PAK1', status: 10, stockOutType: 20, qty: 4, createTime: '2026-08-02T00:00:00Z' }
      ]
    }, t)
    const customsPackingCard = customsPacking.find((s) => s.key === 'packing')?.cards[0]
    expect(customsPackingCard?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.customs')
    expect(customsPackingCard?.showCustomsIcon).toBe(false)
    expect(customsPackingCard?.showPerson).toBe(false)

    const customsPackingLinked = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      packings: [
        {
          id: 'pk-1',
          docCode: 'PAK1',
          status: 10,
          stockOutType: 20,
          qty: 4,
          createTime: '2026-08-02T00:00:00Z',
          customsDeclarationId: 'dec-pk-1'
        }
      ]
    }, t)
    expect(customsPackingLinked.find((s) => s.key === 'packing')?.cards[0].showCustomsIcon).toBe(true)

    const salesPacking = buildStockItemFlowStations(row, {
      stockItemId: 'si-1',
      stockItem: { id: 'si-1', docCode: 'STK-1-01', status: 2, qty: 10, qty2: 4 },
      packings: [
        { id: 'pk-2', docCode: 'PAK2', status: 10, stockOutType: 10, qty: 4, createTime: '2026-08-02T00:00:00Z' }
      ]
    }, t)
    const salesPackingCard = salesPacking.find((s) => s.key === 'packing')?.cards[0]
    expect(salesPackingCard?.bizTypeText).toBe('stockOutList.stockOutTypeLabels.sales')
    expect(salesPackingCard?.showCustomsIcon).toBe(false)
  })

  it('masks purchase vendor/price and sale customer/salesperson as dash', () => {
    const stations = buildStockItemFlowStations(
      row,
      {
        stockItemId: 'si-1',
        purchaseOrderItem: {
          id: 'poi-1',
          docCode: 'PO-1-01',
          status: 60,
          vendorName: 'SecretVendor',
          vendorCode: 'SV',
          unitPrice: 9,
          currency: 2,
          qty: 10,
          purchaseOrderId: 'po-1'
        },
        stockItem: {
          id: 'si-1',
          docCode: 'STK-1-01',
          status: 1,
          vendorName: 'SecretVendor',
          customerName: 'SecretCust',
          personName: 'SecretSales',
          unitPrice: 9,
          currency: 2,
          salesUnitPrice: 12,
          salesCurrency: 2,
          qty: 10,
          qty2: 0
        },
        stockOutNotifies: [
          { id: 'n-1', docCode: 'STOR1', status: 10, customerName: 'SecretCust', personName: 'SecretSales', qty: 4 }
        ]
      },
      t,
      { maskPurchase: true, maskSale: true }
    )
    const po = stations.find((s) => s.key === 'purchaseOrderItem')?.cards[0]
    expect(po?.vendorName).toBe('—')
    expect(po?.unitPriceText).toBe('—')
    expect(po?.docRoute).toBeUndefined()
    const item = stations.find((s) => s.key === 'stockItem')?.cards[0]
    expect(item?.vendorName).toBe('—')
    expect(item?.customerName).toBe('—')
    expect(item?.personName).toBe('—')
    expect(item?.salesPriceText).toBe('—')
    const notify = stations.find((s) => s.key === 'stockOutNotify')?.cards[0]
    expect(notify?.customerName).toBe('—')
    expect(notify?.docRoute).toBeUndefined()
  })
})
