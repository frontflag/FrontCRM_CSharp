import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** 满足任一权限即可进入（与 meta.permission 二选一） */
    permissions?: string[]
    /** 仅系统管理员或平台管理员 */
    adminOrManagerOnly?: boolean
    /** 入库/出库运维检查：系统/平台管理员，或财务部门总监 */
    inventoryOpsCheckAccess?: boolean
  }
}
