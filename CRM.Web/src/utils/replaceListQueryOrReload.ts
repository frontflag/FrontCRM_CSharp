import type {
  LocationQuery,
  LocationQueryRaw,
  RouteLocationNormalizedLoaded,
  RouteLocationRaw,
  Router
} from 'vue-router'

function queryValue(value: LocationQuery[string] | LocationQueryRaw[string]): string {
  if (value == null) return ''
  if (Array.isArray(value)) {
    return value
      .filter((item) => item != null && String(item) !== '')
      .map((item) => String(item))
      .join(',')
  }
  return String(value)
}

/** 地址栏查询参数与即将写入的参数是否一致（空值与缺省视为相同）。 */
export function isSameLocationQuery(current: LocationQuery, next: LocationQueryRaw): boolean {
  const keys = new Set<string>([...Object.keys(current), ...Object.keys(next)])
  for (const key of keys) {
    if (queryValue(current[key]) !== queryValue(next[key])) return false
  }
  return true
}

/**
 * 筛选写入地址栏。参数有变化时由路由监听拉列表；
 * 参数不变时路由不会更新，改为直接再请求一次，便于刷新状态等字段。
 */
export function replaceListQueryOrReload(
  router: Router,
  route: RouteLocationNormalizedLoaded,
  to: Exclude<RouteLocationRaw, string>,
  reload: () => void | Promise<void>
): void {
  const nextName = 'name' in to ? to.name : undefined
  const nextQuery: LocationQueryRaw = 'query' in to && to.query ? to.query : {}
  const sameName = nextName == null || route.name === nextName
  if (sameName && isSameLocationQuery(route.query, nextQuery)) {
    void reload()
    return
  }
  void router.replace(to)
}
