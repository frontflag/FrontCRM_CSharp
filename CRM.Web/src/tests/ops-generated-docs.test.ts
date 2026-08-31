import { describe, expect, it } from 'vitest'
import {
  collectOpsGeneratedDocs,
  listLinkedArrivalNoticeDocs,
  listLinkedPurchaseRequisitionDocs,
  listLinkedStockOutRequestDocs
} from '@/utils/opsGeneratedDocs'

describe('collectOpsGeneratedDocs', () => {
  it('skips deleted, empty, duplicate and skipped statuses', () => {
    const docs = collectOpsGeneratedDocs(
      [
        { id: 'a', code: 'N-2', status: 10 },
        { id: 'b', code: 'N-1', status: -1 },
        { id: 'a', code: 'N-2-dup', status: 10 },
        { id: '', code: 'N-x', status: 10 },
        { id: 'c', code: '', status: 10 },
        { id: 'd', code: 'N-3', status: 10, isDeleted: true }
      ],
      (status) => status < 0
    )
    expect(docs.map((x) => x.code)).toEqual(['N-2'])
  })
})

describe('listLinkedArrivalNoticeDocs', () => {
  it('maps noticeCode and skips negative status', () => {
    const docs = listLinkedArrivalNoticeDocs({
      arrivalNotices: [
        { id: 'n2', noticeCode: 'AN002', status: 10 },
        { id: 'n1', noticeCode: 'AN001', status: -1 }
      ]
    })
    expect(docs).toEqual([{ id: 'n2', code: 'AN002' }])
  })
})

describe('listLinkedPurchaseRequisitionDocs', () => {
  it('skips cancelled PR status 3', () => {
    const docs = listLinkedPurchaseRequisitionDocs({
      purchaseRequisitions: [
        { id: 'p1', billCode: 'PR001', status: 1 },
        { id: 'p2', billCode: 'PR002', status: 3 }
      ]
    })
    expect(docs).toEqual([{ id: 'p1', code: 'PR001' }])
  })
})

describe('listLinkedStockOutRequestDocs', () => {
  it('skips cancelled stock-out request status -1', () => {
    const docs = listLinkedStockOutRequestDocs({
      stockOutRequests: [
        { id: 's1', requestCode: 'SOR001', status: 10 },
        { id: 's2', requestCode: 'SOR002', status: -1 }
      ]
    })
    expect(docs).toEqual([{ id: 's1', code: 'SOR001' }])
  })
})
