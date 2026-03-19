import type { RouteRecordRaw } from 'vue-router'

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
        path: 'customers',
        name: 'CustomerList',
        component: () => import('@/views/Customer/CustomerList.vue'),
        meta: { requiresAuth: true, title: '客户管理', permission: 'customer.read' }
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
      // RFQ 管理
      {
        path: 'rfqs',
        name: 'RFQList',
        component: () => import('@/views/RFQ/RFQList.vue'),
        meta: { requiresAuth: true, title: 'RFQ 管理', permission: 'rfq.read' }
      },
      {
        path: 'rfqs/create',
        name: 'RFQCreate',
        component: () => import('@/views/RFQ/RFQEdit.vue'),
        meta: { requiresAuth: true, title: '新增 RFQ' }
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
        component: () => import('@/views/RFQ/RFQEdit.vue'),
        meta: { requiresAuth: true, title: '编辑 RFQ' }
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
        path: 'vendors',
        name: 'VendorList',
        component: () => import('@/views/Vendor/VendorList.vue'),
        meta: { requiresAuth: true, title: '供应商管理', permission: 'vendor.read' }
      },
      {
        path: 'vendors/create',
        name: 'VendorCreate',
        component: () => import('@/views/Vendor/VendorEdit.vue'),
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
        path: 'vendors/:id',
        name: 'VendorDetail',
        component: () => import('@/views/Vendor/VendorDetail.vue'),
        meta: { requiresAuth: true, title: '供应商详情', permission: 'vendor.read' }
      },
      {
        path: 'vendors/:id/edit',
        name: 'VendorEdit',
        component: () => import('@/views/Vendor/VendorEdit.vue'),
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
        path: 'inventory/stock-out/create',
        name: 'StockOutCreate',
        component: () => import('@/views/Inventory/StockOutEdit.vue'),
        meta: { requiresAuth: true, title: '执行出库' }
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
      // 报价管理
      {
        path: 'quotes',
        name: 'QuoteList',
        component: () => import('@/views/RFQ/QuoteList.vue'),
        meta: { requiresAuth: true, title: '报价管理' }
      },
      // 采购订单
      {
        path: 'purchase-orders',
        name: 'PurchaseOrderList',
        component: () => import('@/views/RFQ/PurchaseOrderList.vue'),
        meta: { requiresAuth: true, title: '采购订单' }
      },
      {
        path: 'purchase-orders/:id',
        name: 'PurchaseOrderDetail',
        component: () => import('@/views/RFQ/PurchaseOrderDetail.vue'),
        meta: { requiresAuth: true, title: '采购订单详情' }
      },
      // 销售订单
      {
        path: 'sales-orders',
        name: 'SalesOrderList',
        component: () => import('@/views/RFQ/SalesOrderList.vue'),
        meta: { requiresAuth: true, title: '销售订单' }
      },
      {
        path: 'sales-orders/:id',
        name: 'SalesOrderDetail',
        component: () => import('@/views/RFQ/SalesOrderDetail.vue'),
        meta: { requiresAuth: true, title: '销售订单详情' }
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
        path: 'drafts',
        name: 'DraftList',
        component: () => import('@/views/Draft/DraftList.vue'),
        meta: { requiresAuth: true, title: '草稿箱', permission: 'draft.read' }
      },
      // 财务模块
      {
        path: 'finance/payments',
        name: 'FinancePaymentList',
        component: () => import('@/views/Finance/FinancePaymentList.vue'),
        meta: { requiresAuth: true, title: '付款管理' }
      },
      {
        path: 'finance/receipts',
        name: 'FinanceReceiptList',
        component: () => import('@/views/Finance/FinanceReceiptList.vue'),
        meta: { requiresAuth: true, title: '收款管理' }
      },
      {
        path: 'finance/purchase-invoices',
        name: 'FinancePurchaseInvoiceList',
        component: () => import('@/views/Finance/FinancePurchaseInvoiceList.vue'),
        meta: { requiresAuth: true, title: '进项发票' }
      },
      {
        path: 'finance/sell-invoices',
        name: 'FinanceSellInvoiceList',
        component: () => import('@/views/Finance/FinanceSellInvoiceList.vue'),
        meta: { requiresAuth: true, title: '销项发票' }
      },
      {
        path: 'documents/demo',
        name: 'DocumentDemo',
        component: () => import('@/views/Document/DocumentDemo.vue'),
        meta: { requiresAuth: true, title: '文档模块演示' }
      }
    ]
  }
]

export default routes
