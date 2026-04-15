import type { RouteRecordRaw } from 'vue-router'
import VendorEdit from '@/views/Vendor/VendorEdit.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Auth/LoginView.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Auth/RegisterView.vue')
  },
  // 带左侧菜单的布局（需要认证）
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: 'dashboard',
        name: 'Dashboard',
        component: () => import('@/views/Dashboard/DashboardView.vue'),
        meta: { requiresAuth: true, title: '控制台' }
      },
      // 客户管理
      {
        path: 'custome',
        name: 'CustomerHome',
        component: () => import('@/views/Customer/CustomerHome.vue'),
        meta: { requiresAuth: true, title: '客户首页', permission: 'customer.read' }
      },
      {
        path: 'customerlist',
        name: 'CustomerList',
        component: () => import('@/views/Customer/CustomerList.vue'),
        meta: { requiresAuth: true, title: '客户', permission: 'customer.read' }
      },
      {
        path: 'customers',
        redirect: '/customerlist'
      },
      {
        path: 'customers/create',
        name: 'CustomerCreate',
        component: () => import('@/views/Customer/CustomerEdit.vue'),
        meta: { requiresAuth: true, title: '新增客户', permission: 'customer.write' }
      },
      // 静态路由必须在 :id 动态路由之前
      {
        path: 'customers/recycle-bin',
        name: 'CustomerRecycleBin',
        component: () => import('@/views/Customer/CustomerRecycleBin.vue'),
        meta: { requiresAuth: true, title: '客户回收站', permission: 'customer.read' }
      },
      {
        path: 'customers/blacklist',
        name: 'CustomerBlacklist',
        component: () => import('@/views/Customer/CustomerBlacklist.vue'),
        meta: { requiresAuth: true, title: '黑名单管理', permission: 'customer.read' }
      },
      {
        path: 'customers/frozen',
        name: 'CustomerFreezeManagement',
        component: () => import('@/views/Customer/CustomerFreezeManagement.vue'),
        meta: { requiresAuth: true, title: '冻结管理', permission: 'customer.read' }
      },
      {
        path: 'customers/:id',
        name: 'CustomerDetail',
        component: () => import('@/views/Customer/CustomerDetail.vue'),
        meta: { requiresAuth: true, title: '客户详情', permission: 'customer.read' }
      },
      {
        path: 'customers/:id/edit',
        name: 'CustomerEdit',
        component: () => import('@/views/Customer/CustomerEdit.vue'),
        meta: { requiresAuth: true, title: '编辑客户', permission: 'customer.write' }
      },
      {
        path: 'customers/:id/contacts/create',
        name: 'CustomerContactCreate',
        component: () => import('@/views/Customer/CustomerContactEdit.vue'),
        meta: { requiresAuth: true, title: '新增联系人', permission: 'customer.write' }
      },
      {
        path: 'customers/:id/contacts/:contactId/edit',
        name: 'CustomerContactEdit',
        component: () => import('@/views/Customer/CustomerContactEdit.vue'),
        meta: { requiresAuth: true, title: '编辑联系人', permission: 'customer.write' }
      },
      // RFQ 管理（首页 / 列表 / 详情仍使用 /rfqs/* 子路径）
      {
        path: 'rfq',
        name: 'RFQHome',
        component: () => import('@/views/RFQ/RFQHome.vue'),
        meta: { requiresAuth: true, title: '需求首页', permission: 'rfq.read' }
      },
      {
        path: 'rfqlist',
        name: 'RFQList',
        component: () => import('@/views/RFQ/RFQList.vue'),
        meta: { requiresAuth: true, title: '需求列表', permission: 'rfq.read' }
      },
      {
        path: 'pn',
        name: 'PnList',
        component: () => import('@/views/Material/PnList.vue'),
        meta: { requiresAuth: true, title: '物料列表' }
      },
      {
        path: 'rfq-items',
        name: 'RFQItemList',
        component: () => import('@/views/RFQ/RFQItemList.vue'),
        meta: { requiresAuth: true, title: '需求明细', permission: 'rfq.read' }
      },
      {
        path: 'rfqs/create',
        name: 'RFQCreate',
        component: () => import('@/views/RFQ/RFQCreate.vue'),
        meta: { requiresAuth: true, title: '新建需求', permission: 'rfq.create' }
      },
      {
        path: 'rfqs/:id',
        name: 'RFQDetail',
        component: () => import('@/views/RFQ/RFQDetail.vue'),
        meta: { requiresAuth: true, title: 'RFQ 详情' }
      },
      {
        path: 'rfqs/:id/edit',
        name: 'RFQEdit',
        component: () => import('@/views/RFQ/RFQCreate.vue'),
        meta: { requiresAuth: true, title: '编辑需求', permission: 'rfq.write' }
      },
      {
        path: 'rfqs',
        redirect: '/rfqlist'
      },
      // BOM 快速报价
      {
        path: 'boms',
        name: 'BOMList',
        component: () => import('@/views/BOM/BOMList.vue'),
        meta: { requiresAuth: true, title: 'BOM 快速报价' }
      },
      {
        path: 'boms/create',
        name: 'BOMCreate',
        component: () => import('@/views/BOM/BOMCreate.vue'),
        meta: { requiresAuth: true, title: '新建 BOM' }
      },
      {
        path: 'boms/:id',
        name: 'BOMDetail',
        component: () => import('@/views/BOM/BOMDetail.vue'),
        meta: { requiresAuth: true, title: 'BOM 详情' }
      },
      // 供应商管理
      {
        path: 'vendor',
        name: 'VendorHome',
        component: () => import('@/views/Vendor/VendorHome.vue'),
        meta: { requiresAuth: true, title: '供应商首页', permission: 'vendor.read' }
      },
      {
        path: 'vendorlist',
        name: 'VendorList',
        component: () => import('@/views/Vendor/VendorList.vue'),
        meta: { requiresAuth: true, title: '供应商', permission: 'vendor.read' }
      },
      {
        path: 'vendors',
        redirect: '/vendorlist'
      },
      {
        path: 'vendors/create',
        name: 'VendorCreate',
        component: VendorEdit,
        meta: { requiresAuth: true, title: '新增供应商', permission: 'vendor.write' }
      },
      {
        path: 'vendors/recycle-bin',
        name: 'VendorRecycleBin',
        component: () => import('@/views/Vendor/VendorRecycleBin.vue'),
        meta: { requiresAuth: true, title: '供应商回收站', permission: 'vendor.read' }
      },
      {
        path: 'vendors/blacklist',
        name: 'VendorBlacklist',
        component: () => import('@/views/Vendor/VendorBlacklist.vue'),
        meta: { requiresAuth: true, title: '供应商黑名单', permission: 'vendor.read' }
      },
      {
        path: 'vendors/frozen',
        name: 'VendorFreezeManagement',
        component: () => import('@/views/Vendor/VendorFreezeManagement.vue'),
        meta: { requiresAuth: true, title: '冻结管理', permission: 'vendor.read' }
      },
      {
        path: 'vendors/:id/warranty/:lang',
        name: 'VendorWarrantyReport',
        component: () => import('@/views/Vendor/VendorWarrantyReportPage.vue'),
        meta: { requiresAuth: true, title: '供应商质保书', permission: 'vendor.read' }
      },
      {
        path: 'vendors/:id',
        name: 'VendorDetail',
        component: () => import('@/views/Vendor/VendorDetail.vue'),
        meta: { requiresAuth: true, title: '供应商详情', permission: 'vendor.read' }
      },
      {
        path: 'vendors/:id/edit',
        name: 'VendorEdit',
        component: VendorEdit,
        meta: { requiresAuth: true, title: '编辑供应商', permission: 'vendor.write' }
      },
      {
        path: 'vendors/:id/contacts/create',
        name: 'VendorContactCreate',
        component: () => import('@/views/Vendor/VendorContactEdit.vue'),
        meta: { requiresAuth: true, title: '新增联系人', permission: 'vendor.write' }
      },
      {
        path: 'vendors/:id/contacts/:contactId/edit',
        name: 'VendorContactEdit',
        component: () => import('@/views/Vendor/VendorContactEdit.vue'),
        meta: { requiresAuth: true, title: '编辑联系人', permission: 'vendor.write' }
      },
      // 库存管理
      {
        path: 'inventory/list',
        name: 'InventoryList',
        component: () => import('@/views/Inventory/InventoryList.vue'),
        meta: { requiresAuth: true, title: '库存列表' }
      },
      {
        path: 'inventory/stock-items',
        name: 'InventoryStockItemList',
        component: () => import('@/views/Inventory/InventoryStockItemList.vue'),
        meta: { requiresAuth: true, title: '库存明细' }
      },
      {
        path: 'inventory/stocks/:stockId',
        name: 'InventoryStockDetail',
        component: () => import('@/views/Inventory/InventoryStockDetailPage.vue'),
        meta: { requiresAuth: true, title: '汇总库存明细' }
      },
      {
        path: 'inventory/traces/:materialId',
        name: 'InventoryTrace',
        component: () => import('@/views/Inventory/InventoryTracePage.vue'),
        meta: { requiresAuth: true, title: '入库追溯' }
      },
      {
        path: 'inventory/stock-in',
        name: 'StockInList',
        component: () => import('@/views/Inventory/StockInList.vue'),
        meta: { requiresAuth: true, title: '入库单列表' }
      },
      {
        path: 'inventory/stock-in/create',
        name: 'StockInCreate',
        component: () => import('@/views/Inventory/StockInEdit.vue'),
        meta: { requiresAuth: true, title: '新建入库单' }
      },
      {
        path: 'inventory/stock-in/:id',
        name: 'StockInDetail',
        component: () => import('@/views/Inventory/StockInEdit.vue'),
        meta: { requiresAuth: true, title: '入库单详情' }
      },
      {
        path: 'inventory/stock-out',
        name: 'StockOutList',
        component: () => import('@/views/Inventory/StockOutList.vue'),
        meta: { requiresAuth: true, title: '出库单列表' }
      },
      {
        path: 'inventory/stock-out/items',
        name: 'StockOutItemList',
        component: () => import('@/views/Inventory/StockOutItemList.vue'),
        meta: { requiresAuth: true, title: '出库明细' }
      },
      {
        path: 'inventory/stock-out/create',
        name: 'StockOutCreate',
        component: () => import('@/views/Inventory/StockOutEdit.vue'),
        meta: { requiresAuth: true, title: '执行出库' }
      },
      {
        path: 'inventory/stock-out/:id/invoice-report',
        name: 'StockOutInvoiceReport',
        component: () => import('@/views/Inventory/StockOutInvoiceReportPage.vue'),
        meta: { requiresAuth: true, title: '出库 Invoice' }
      },
      {
        path: 'inventory/stock-out/:id/packing-report/:packingInspection',
        name: 'StockOutPackingReport',
        component: () => import('@/views/Inventory/StockOutPackingReportPage.vue'),
        meta: { requiresAuth: true, title: '出库 Packing' }
      },
      {
        path: 'inventory/stock-out/:id',
        name: 'StockOutDetail',
        component: () => import('@/views/Inventory/StockOutDetail.vue'),
        meta: { requiresAuth: true, title: '出库单详情' }
      },
      {
        path: 'inventory/stock-out-notifies',
        name: 'InventoryStockOutNotifyList',
        component: () => import('@/views/RFQ/StockOutNotifyList.vue'),
        meta: {
          requiresAuth: true,
          title: '出库通知',
          // 销售用 SO 权限；采购/履约用 PO 权限（采购主部门不再持有 sales-order.read）
          permissions: ['sales-order.read', 'purchase-order.read']
        }
      },
      {
        path: 'inventory/transfer',
        name: 'InventoryTransfer',
        component: () => import('@/views/Inventory/InventoryTransfer.vue'),
        meta: { requiresAuth: true, title: '库存调拨' }
      },
      {
        path: 'inventory/check',
        name: 'InventoryCheck',
        component: () => import('@/views/Inventory/InventoryCheck.vue'),
        meta: { requiresAuth: true, title: '库存盘点' }
      },
      // 报价管理（quotes/create、quotes/:id/edit 必须在 quotes/:id 之前，否则 :id 会误匹配）
      {
        path: 'quotes',
        name: 'QuoteList',
        component: () => import('@/views/RFQ/QuoteList.vue'),
        meta: { requiresAuth: true, title: '报价列表' }
      },
      {
        path: 'quotes/create',
        name: 'QuoteCreate',
        component: () => import('@/views/RFQ/QuoteCreate.vue'),
        meta: { requiresAuth: true, title: '新建报价' }
      },
      {
        path: 'quotes/:id/edit',
        name: 'QuoteEdit',
        component: () => import('@/views/RFQ/QuoteCreate.vue'),
        meta: { requiresAuth: true, title: '编辑报价' }
      },
      {
        path: 'quotes/:id',
        name: 'QuoteDetail',
        component: () => import('@/views/RFQ/QuoteDetail.vue'),
        meta: { requiresAuth: true, title: '报价单详情' }
      },
      // 采购订单 / 采购申请（meta.permission 与 RbacService 销售主部门剥离一致）
      {
        path: 'purchase-orders',
        name: 'PurchaseOrderList',
        component: () => import('@/views/RFQ/PurchaseOrderList.vue'),
        meta: { requiresAuth: true, title: '采购订单', permission: 'purchase-order.read' }
      },
      {
        path: 'purchase-requisitions',
        name: 'PurchaseRequisitionList',
        component: () => import('@/views/RFQ/PurchaseRequisitionListPage.vue'),
        meta: {
          requiresAuth: true,
          title: '采购申请',
          /** sales-order.read：主部门未标销售时 Rbac 仍可能已合并 PR，此项作路由兜底 */
          permissions: ['purchase-requisition.read', 'purchase-order.read', 'sales-order.read']
        }
      },
      {
        path: 'purchase-requisitions/new',
        name: 'PurchaseRequisitionCreate',
        component: () => import('@/views/RFQ/PurchaseRequisitionCreate.vue'),
        meta: {
          requiresAuth: true,
          title: '新建采购申请',
          permissions: ['purchase-requisition.write', 'purchase-order.write', 'sales-order.write']
        }
      },
      {
        path: 'purchase-requisitions/:id',
        name: 'PurchaseRequisitionDetail',
        component: () => import('@/views/RFQ/PurchaseRequisitionDetailPage.vue'),
        meta: {
          requiresAuth: true,
          title: '采购申请详情',
          permissions: ['purchase-requisition.read', 'purchase-order.read', 'sales-order.read']
        }
      },
      {
        path: 'purchase-orders/new',
        name: 'PurchaseOrderCreate',
        component: () => import('@/views/RFQ/PurchaseOrderCreate.vue'),
        meta: {
          requiresAuth: true,
          title: '新建采购订单',
          /** 权限由全局守卫 purchaseOrderCreateAccess + canAccessPurchaseOrderCreatePage 判定（销售员不可进） */
          purchaseOrderCreateAccess: true
        }
      },
      {
        path: 'purchase-orders/:id/report',
        name: 'PurchaseOrderReport',
        component: () => import('@/views/RFQ/PurchaseOrderReportPage.vue'),
        meta: { requiresAuth: true, title: '采购订单报表', permission: 'purchase-order.read' }
      },
      {
        path: 'purchase-orders/:id/edit',
        name: 'PurchaseOrderEdit',
        component: () => import('@/views/RFQ/PurchaseOrderCreate.vue'),
        meta: { requiresAuth: true, title: '编辑采购订单', permission: 'purchase-order.write' }
      },
      {
        path: 'purchase-orders/:id',
        name: 'PurchaseOrderDetail',
        component: () => import('@/views/RFQ/PurchaseOrderDetail.vue'),
        meta: { requiresAuth: true, title: '采购订单详情', permission: 'purchase-order.read' }
      },
      // 销售订单（直链防护；采购主部门无 sales-order.read）
      {
        path: 'sales-orders',
        name: 'SalesOrderList',
        component: () => import('@/views/RFQ/SalesOrderList.vue'),
        meta: { requiresAuth: true, title: '销售订单', permission: 'sales-order.read' }
      },
      {
        path: 'sales-order-items',
        name: 'SalesOrderItemList',
        component: () => import('@/views/RFQ/SalesOrderItemList.vue'),
        meta: { requiresAuth: true, title: '销售订单明细', permission: 'sales-order.read' }
      },
      {
        path: 'stock-out-notifies',
        name: 'StockOutNotifyList',
        component: () => import('@/views/RFQ/StockOutNotifyList.vue'),
        meta: {
          requiresAuth: true,
          title: '出库通知',
          permissions: ['sales-order.read', 'purchase-order.read']
        }
      },
      // 采购订单明细
      {
        path: 'purchase-order-items',
        name: 'PurchaseOrderItemList',
        component: () => import('@/views/RFQ/PurchaseOrderItemList.vue'),
        meta: { requiresAuth: true, title: '采购订单明细', permission: 'purchase-order.read' }
      },
      {
        path: 'logistics/arrival-notices',
        name: 'ArrivalNoticeList',
        component: () => import('@/views/Logistics/ArrivalNoticeList.vue'),
        meta: { requiresAuth: true, title: '到货通知', permission: 'purchase-order.read' }
      },
      {
        path: 'logistics/qc',
        name: 'QcList',
        component: () => import('@/views/Logistics/QcList.vue'),
        meta: { requiresAuth: true, title: '质检', permission: 'purchase-order.read' }
      },
      {
        path: 'logistics/qc/new',
        name: 'QcCreate',
        component: () => import('@/views/Logistics/QcCreate.vue'),
        meta: { requiresAuth: true, title: '新建质检', permission: 'purchase-order.read' }
      },
      {
        path: 'sales-orders/new',
        name: 'SalesOrderCreate',
        component: () => import('@/views/RFQ/SalesOrderCreate.vue'),
        meta: { requiresAuth: true, title: '新建销售订单', permission: 'sales-order.write' }
      },
      {
        path: 'sales-orders/:id/report',
        name: 'SalesOrderReport',
        component: () => import('@/views/RFQ/SalesOrderReportPage.vue'),
        meta: { requiresAuth: true, title: '销售订单报表', permission: 'sales-order.read' }
      },
      {
        path: 'sales-orders/:id',
        name: 'SalesOrderDetail',
        component: () => import('@/views/RFQ/SalesOrderDetail.vue'),
        meta: { requiresAuth: true, title: '销售订单详情', permission: 'sales-order.read' }
      },
      // 系统设置
      {
        path: 'dashboard/settings',
        name: 'Settings',
        component: () => import('@/views/Dashboard/SettingsView.vue'),
        meta: { requiresAuth: true, title: '系统设置' }
      },
      // 个人设置
      {
        path: 'profile',
        name: 'Profile',
        component: () => import('@/views/Profile/ProfileView.vue'),
        meta: { requiresAuth: true, title: '个人设置' }
      },
      {
        path: 'profile/wechat-bind',
        name: 'WechatBinding',
        component: () => import('@/views/Profile/WechatBinding.vue'),
        meta: { requiresAuth: true, title: '微信绑定' }
      },
      {
        path: 'drafts',
        name: 'DraftList',
        component: () => import('@/views/Draft/DraftList.vue'),
        meta: { requiresAuth: true, title: '草稿箱', permission: 'draft.read' }
      },
      // 待办 - 待审批
      {
        path: 'pending-approvals',
        name: 'PendingApprovals',
        component: () => import('@/views/Approvals/PendingApprovals.vue'),
        meta: { requiresAuth: true, title: '待审批' }
      },
      // 系统管理
      {
        path: 'system/users',
        name: 'UserList',
        component: () => import('@/views/System/UserList.vue'),
        meta: { requiresAuth: true, title: '员工管理', permission: 'rbac.manage' }
      },
      {
        path: 'system/users/create',
        name: 'UserCreate',
        component: () => import('@/views/System/UserEdit.vue'),
        meta: { requiresAuth: true, title: '新增员工', permission: 'rbac.manage' }
      },
      {
        path: 'system/users/:id/edit',
        name: 'UserEdit',
        component: () => import('@/views/System/UserEdit.vue'),
        meta: { requiresAuth: true, title: '编辑员工', permission: 'rbac.manage' }
      },
      {
        path: 'system/roles',
        name: 'RoleList',
        component: () => import('@/views/System/RoleList.vue'),
        meta: { requiresAuth: true, title: '角色管理', permission: 'rbac.manage' }
      },
      {
        path: 'system/roles/create',
        name: 'RoleCreate',
        component: () => import('@/views/System/RoleEdit.vue'),
        meta: { requiresAuth: true, title: '新增角色', permission: 'rbac.manage' }
      },
      {
        path: 'system/roles/:id/edit',
        name: 'RoleEdit',
        component: () => import('@/views/System/RoleEdit.vue'),
        meta: { requiresAuth: true, title: '编辑角色', permission: 'rbac.manage' }
      },
      {
        path: 'system/permissions',
        name: 'PermissionList',
        component: () => import('@/views/System/PermissionList.vue'),
        meta: { requiresAuth: true, title: '权限管理', permission: 'rbac.manage' }
      },
      {
        path: 'system/permissions/create',
        name: 'PermissionCreate',
        component: () => import('@/views/System/PermissionEdit.vue'),
        meta: { requiresAuth: true, title: '新增权限', permission: 'rbac.manage' }
      },
      {
        path: 'system/permissions/:id/edit',
        name: 'PermissionEdit',
        component: () => import('@/views/System/PermissionEdit.vue'),
        meta: { requiresAuth: true, title: '编辑权限', permission: 'rbac.manage' }
      },
      {
        path: 'system/departments',
        name: 'DepartmentList',
        component: () => import('@/views/System/DepartmentList.vue'),
        meta: { requiresAuth: true, title: '部门管理', permission: 'rbac.manage' }
      },
      {
        path: 'system/departments/create',
        name: 'DepartmentCreate',
        component: () => import('@/views/System/DepartmentEdit.vue'),
        meta: { requiresAuth: true, title: '新增部门', permission: 'rbac.manage' }
      },
      {
        path: 'system/departments/:id/edit',
        name: 'DepartmentEdit',
        component: () => import('@/views/System/DepartmentEdit.vue'),
        meta: { requiresAuth: true, title: '编辑部门', permission: 'rbac.manage' }
      },
      {
        path: 'system/departments/:id',
        name: 'DepartmentDetail',
        component: () => import('@/views/System/DepartmentDetail.vue'),
        meta: { requiresAuth: true, title: '部门详情', permission: 'rbac.manage' }
      },
      {
        path: 'system/company-info',
        name: 'CompanyInfo',
        component: () => import('@/views/System/CompanyInfoView.vue'),
        meta: { requiresAuth: true, title: '公司信息', permission: 'rbac.manage' }
      },
      {
        path: 'system/dict-items',
        name: 'DictItemList',
        component: () => import('@/views/System/DictItemList.vue'),
        meta: { requiresAuth: true, title: '数据字典', permission: 'rbac.manage' }
      },
      {
        path: 'system/finance-params',
        component: () => import('@/views/System/FinanceParamsLayout.vue'),
        meta: { requiresAuth: true, title: '财务参数', permission: 'rbac.manage' },
        children: [
          { path: '', redirect: '/system/finance-params/exchange-rates' },
          {
            path: 'exchange-rates',
            name: 'FinanceExchangeRates',
            component: () => import('@/views/System/FinanceExchangeRateSettings.vue'),
            meta: { requiresAuth: true, title: '财务参数', permission: 'rbac.manage' }
          }
        ]
      },
      // 财务模块（meta.permission 与 RbacService 按主部门剥离一致，防止直链 URL）
      {
        path: 'finance/payments',
        name: 'FinancePaymentList',
        component: () => import('@/views/Finance/FinancePaymentList.vue'),
        meta: { requiresAuth: true, title: '付款管理', permission: 'finance-payment.read' }
      },
      {
        path: 'finance/payments/:id',
        name: 'FinancePaymentDetail',
        component: () => import('@/views/Finance/FinancePaymentDetail.vue'),
        meta: { requiresAuth: true, title: '付款单详情', permission: 'finance-payment.read' }
      },
      {
        path: 'finance/receipts',
        name: 'FinanceReceiptList',
        component: () => import('@/views/Finance/FinanceReceiptList.vue'),
        meta: { requiresAuth: true, title: '收款管理', permission: 'finance-receipt.read' }
      },
      {
        path: 'finance/receipts/:id',
        name: 'FinanceReceiptDetail',
        component: () => import('@/views/Finance/FinanceReceiptDetail.vue'),
        meta: { requiresAuth: true, title: '收款单详情', permission: 'finance-receipt.read' }
      },
      {
        path: 'finance/purchase-invoices',
        name: 'FinancePurchaseInvoiceList',
        component: () => import('@/views/Finance/FinancePurchaseInvoiceList.vue'),
        meta: { requiresAuth: true, title: '进项发票', permission: 'finance-purchase-invoice.read' }
      },
      {
        path: 'finance/purchase-invoices/:id',
        name: 'FinancePurchaseInvoiceDetail',
        component: () => import('@/views/Finance/FinancePurchaseInvoiceDetail.vue'),
        meta: { requiresAuth: true, title: '进项发票详情', permission: 'finance-purchase-invoice.read' }
      },
      {
        path: 'finance/sell-invoices',
        name: 'FinanceSellInvoiceList',
        component: () => import('@/views/Finance/FinanceSellInvoiceList.vue'),
        meta: { requiresAuth: true, title: '销项发票', permission: 'finance-sell-invoice.read' }
      },
      {
        path: 'finance/sell-invoices/:id',
        name: 'FinanceSellInvoiceDetail',
        component: () => import('@/views/Finance/FinanceSellInvoiceDetail.vue'),
        meta: { requiresAuth: true, title: '销项发票详情', permission: 'finance-sell-invoice.read' }
      },
      {
        path: 'documents/demo',
        name: 'DocumentDemo',
        component: () => import('@/views/Document/DocumentDemo.vue'),
        meta: { requiresAuth: true, title: '文档模块演示' }
      },
      {
        path: 'debug/data',
        name: 'DebugData',
        component: () => import('@/views/Debug/DebugData.vue'),
        meta: { requiresAuth: true, title: 'Debug 模拟数据' }
      },
      // 兼容历史访问链接：/debugdata -> /debug/data
      {
        path: 'debugdata',
        redirect: '/debug/data'
      },
      // 免登录：便于部署后核对脱敏数据库连接串与前端构建版本（PRD）
      {
        path: 'debug',
        name: 'DebugList',
        component: () => import('@/views/Debug/DebugList.vue'),
        meta: { requiresAuth: false, title: 'Debug' }
      }
    ]
  }
]

export default routes
