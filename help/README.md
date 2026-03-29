# 帮助手册（Markdown）

- 按业务模块分子目录，例如客户模块文档放在 `customer/`，供应商模块放在 `vendor/`。
- 每个页面对应一个文件，**文件名与 Vue Router 的 `route.name` 一致**，扩展名为 `.md`。  
  例：客户列表路由 `name: 'CustomerList'` → `customer/CustomerList.md`。
- 开发与生产构建前会执行 `scripts/sync-help.mjs`，将本目录复制到 `CRM.Web/public/help/`，随前端静态资源一并部署。

## 模块目录约定

与前端 `CRM.Web/src/utils/helpDocPath.ts` 中路径前缀一致，例如：

| 路径前缀 | 子目录名 |
|---------|---------|
| `/customers`、`/custome`、`/customerlist` | `customer` |
| `/vendors`、`/vendor`、`/vendorlist` | `vendor` |
| `/rfqs`、`/rfq-items` | `rfq` |
| `/boms` | `bom` |
| `/inventory`、`/stock-out-notifies` | `inventory` |
| `/quotes` | `quote` |
| `/purchase-orders`、`/purchase-requisitions`、`/purchase-order-items` | `purchase` |
| `/sales-orders`、`/sales-order-items` | `sales` |
| `/logistics` | `logistics` |
| `/finance` | `finance` |
| `/system` | `system` |
| `/dashboard` | `dashboard` |
| `/profile` | `profile` |
| `/drafts` | `draft` |
| `/pending-approvals` | `approval` |
| `/documents` | `document` |
| `/debug` | `debug` |
| （其他） | `common` |

未匹配到对应 `.md` 文件（404）时，右栏「帮助」显示「暂无帮助」。

## 主菜单相关页面（已备帮助）

侧栏主菜单可直接进入的路由，已在对应目录下提供与 `route.name` 同名的 `.md`（如 `Dashboard.md`、`PendingApprovals.md`、`CustomerHome.md`、`VendorHome.md`、`RFQList.md`、`BOMList.md`、`QuoteList.md`、销售/采购/库存/财务/系统各列表页等）。子页面（详情、新建、编辑）可按同样规则逐步补充。
