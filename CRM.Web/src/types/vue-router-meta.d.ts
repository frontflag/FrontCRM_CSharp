import 'vue-router'

declare module 'vue-router' {
  interface RouteMeta {
    /** 满足任一权限即可进入（与 meta.permission 二选一） */
    permissions?: string[]
  }
}
