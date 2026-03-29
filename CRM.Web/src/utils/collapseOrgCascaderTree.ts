/**
 * 组织树 → el-cascader：若某层仅有单一非人员节点，且其直接子级中存在人员，
 * 则跳过该层，用其子级作为当前层（可多层连续折叠，递归处理子树）。
 * 用于采购员 / 业务员部门-人员级联，减少无效点击。
 */

export interface OrgCascaderNode {
  value: string
  label: string
  isUser: boolean
  children?: OrgCascaderNode[]
}

function hasDirectUserChild(children: OrgCascaderNode[]): boolean {
  return children.some((c) => c.isUser)
}

export function collapseSingleOrgLayersWithUsersBelow(nodes: OrgCascaderNode[]): OrgCascaderNode[] {
  let current = nodes
  while (
    current.length === 1 &&
    !current[0].isUser &&
    !!current[0].children?.length &&
    hasDirectUserChild(current[0].children)
  ) {
    current = current[0].children
  }
  return current.map((node) => ({
    ...node,
    children: node.children?.length
      ? collapseSingleOrgLayersWithUsersBelow(node.children)
      : undefined
  }))
}
