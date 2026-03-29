/** 与列表「状态」列、后端 customerinfo.Status 一致 */
export const CUSTOMER_WORKFLOW_STATUS_OPTIONS: { value: number; label: string }[] = [
  { value: 1, label: '新建' },
  { value: 2, label: '待审核' },
  { value: 10, label: '已审核' },
  { value: 12, label: '待财务审核' },
  { value: 20, label: '财务建档' },
  { value: -1, label: '审核失败' }
]
