<template>
  <div class="app-layout">
    <!-- 全局顶栏：深色 · 左 Logo+双行标题 · 右 通知+用户（全宽） -->
    <header class="global-top-bar">
      <div class="global-top-inner">
        <router-link to="/dashboard" class="global-logo" @click="closeDropdown">
          <span class="global-logo-mark" aria-hidden="true">
            <svg viewBox="0 0 40 40" fill="none" xmlns="http://www.w3.org/2000/svg">
              <polygon points="20,2 36,11 36,29 20,38 4,29 4,11" fill="none" stroke="#00D4FF" stroke-width="1.5"/>
              <polygon points="20,8 30,14 30,26 20,32 10,26 10,14" fill="rgba(0,212,255,0.12)" stroke="#0066FF" stroke-width="1"/>
              <circle cx="20" cy="20" r="5" fill="#00D4FF" opacity="0.95"/>
            </svg>
          </span>
          <span class="global-logo-stack">
            <span class="global-logo-title">FrontCRM</span>
            <span class="global-logo-sub">AI智销系统</span>
          </span>
        </router-link>
        <div class="global-top-right">
          <button
            type="button"
            class="global-notify-btn"
            title="消息通知"
            @click="handleUnimplemented('消息通知')"
          >
            <span class="global-notify-icon-wrap" aria-hidden="true">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
                <path d="M13.73 21a2 2 0 01-3.46 0"/>
              </svg>
            </span>
            <span v-if="headerNotifyCount > 0" class="global-notify-badge">{{ headerNotifyCount > 99 ? '99+' : headerNotifyCount }}</span>
          </button>
          <div class="user-dropdown global-user-dropdown" v-click-outside="closeDropdown">
            <button type="button" class="global-user-trigger" @click="toggleDropdown">
              <span class="global-user-avatar">{{ userInitial }}</span>
              <span class="global-user-name">{{ userName }}</span>
              <svg class="dropdown-chevron global-user-chevron" :class="{ open: dropdownOpen }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 9l6 6 6-6"/>
              </svg>
            </button>
            <transition name="dropdown">
              <div class="dropdown-menu" v-if="dropdownOpen">
                <div class="dropdown-header">
                  <div class="dropdown-avatar">{{ userInitial }}</div>
                  <div class="dropdown-user-info">
                    <span class="dropdown-username">{{ userName }}</span>
                    <span class="dropdown-email">{{ userEmail }}</span>
                  </div>
                </div>
                <div class="dropdown-divider"></div>
                <router-link to="/profile" class="dropdown-item" @click="closeDropdown">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
                    <circle cx="12" cy="7" r="4"/>
                  </svg>
                  个人设置
                </router-link>
                <div class="dropdown-divider"></div>
                <button class="dropdown-item danger" @click="handleLogout">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                    <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/>
                    <polyline points="16 17 21 12 16 7"/>
                    <line x1="21" y1="12" x2="9" y2="12"/>
                  </svg>
                  退出系统
                </button>
              </div>
            </transition>
          </div>
        </div>
      </div>
    </header>

    <div class="app-layout-body">
    <!-- 左侧菜单面板：完整 / 边条 -->
    <aside
      class="sidebar"
      :class="{ collapsed: isCollapsed }"
      :style="sidebarMode === 'full' ? { width: sidebarWidthPx + 'px' } : undefined"
    >
      <!-- 菜单区域 -->
      <nav class="sidebar-nav">
        <!-- 主菜单标签 -->
        <div class="menu-section-label" v-if="!isCollapsed">主菜单</div>

        <!-- 控制台：展开时不包 el-tooltip，避免触发器层把菜单项挤成纵向 -->
        <SidebarMenuTooltipWrap :collapsed="isCollapsed" tooltip="控制台">
          <router-link to="/dashboard" class="menu-item" active-class="active" exact>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <rect x="3" y="3" width="7" height="7" rx="1"/>
                <rect x="14" y="3" width="7" height="7" rx="1"/>
                <rect x="3" y="14" width="7" height="7" rx="1"/>
                <rect x="14" y="14" width="7" height="7" rx="1"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">控制台</span>
            <span class="active-dot" v-if="!isCollapsed"></span>
          </router-link>
        </SidebarMenuTooltipWrap>

        <!-- 待办 -->
        <div class="menu-section-label" v-if="!isCollapsed">待办</div>
        <SidebarMenuTooltipWrap
          v-if="hasAnyApprovalPermission"
          :collapsed="isCollapsed"
          tooltip="待审批"
        >
          <router-link
            to="/pending-approvals"
            class="menu-item"
            active-class="active"
            exact
          >
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M12 8v4l3 3" />
                <circle cx="12" cy="12" r="9" />
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">待审批</span>
            <span class="active-dot" v-if="!isCollapsed"></span>
          </router-link>
        </SidebarMenuTooltipWrap>

        <SidebarMenuTooltipWrap
          v-if="hasPermission('draft.read')"
          :collapsed="isCollapsed"
          tooltip="草稿箱"
        >
          <router-link
            to="/drafts"
            class="menu-item"
            active-class="active"
          >
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M4 4h16v16H4z"/>
                <path d="M8 8h8M8 12h8M8 16h5"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">草稿箱</span>
            <span class="active-dot" v-if="!isCollapsed"></span>
          </router-link>
        </SidebarMenuTooltipWrap>

        <!-- 基础资料 -->
        <div class="menu-section-label" v-if="!isCollapsed">基础资料</div>

        <SidebarMenuGroupFlyout
          v-if="showCustomerMenuSection && hasPermission('customer.read')"
          :collapsed="isCollapsed"
          :expanded="openGroups.customers"
          @toggle="toggleGroup('customers')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
                <circle cx="12" cy="7" r="4"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">客户管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.customers }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/custome" class="submenu-item" active-class="active" exact>客户</router-link>
            <router-link to="/customers/frozen" class="submenu-item" active-class="active">冻结管理</router-link>
            <router-link to="/customers/blacklist" class="submenu-item" active-class="active">黑名单管理</router-link>
            <router-link to="/customers/recycle-bin" class="submenu-item" active-class="active">回收站</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <SidebarMenuGroupFlyout
          v-if="showVendorMenuSection && hasPermission('vendor.read')"
          :collapsed="isCollapsed"
          :expanded="openGroups.vendors"
          @toggle="toggleGroup('vendors')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
                <circle cx="9" cy="7" r="4"/>
                <path d="M23 21v-2a4 4 0 00-3-3.87"/>
                <path d="M16 3.13a4 4 0 010 7.75"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">供应商管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.vendors }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="6 9 12 15 18 9"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/vendor" class="submenu-item" active-class="active" exact>供应商</router-link>
            <router-link to="/vendors/frozen" class="submenu-item" active-class="active">冻结管理</router-link>
            <router-link to="/vendors/blacklist" class="submenu-item" active-class="active">黑名单管理</router-link>
            <router-link to="/vendors/recycle-bin" class="submenu-item" active-class="active">回收站</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 询价 -->
        <div class="menu-section-label" v-if="!isCollapsed">询价</div>

        <SidebarMenuGroupFlyout
          v-if="hasPermission('rfq.read')"
          :collapsed="isCollapsed"
          :expanded="openGroups.rfqs"
          @toggle="toggleGroup('rfqs')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
                <polyline points="14 2 14 8 20 8"/>
                <line x1="16" y1="13" x2="8" y2="13"/>
                <line x1="16" y1="17" x2="8" y2="17"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">需求管理</span>
          </template>
          <template #chevron>
            <svg
              v-if="!isCollapsed"
              class="chevron"
              :class="{ rotated: openGroups.rfqs }"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/rfq" class="submenu-item" active-class="active" exact>需求列表</router-link>
            <router-link to="/rfq-items" class="submenu-item" active-class="active">需求明细</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <SidebarMenuTooltipWrap :collapsed="isCollapsed" tooltip="BOM 快速报价">
          <router-link to="/boms" class="menu-item" active-class="active">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <rect x="3" y="3" width="18" height="18" rx="2"/>
                <line x1="3" y1="9" x2="21" y2="9"/>
                <line x1="9" y1="21" x2="9" y2="9"/>
                <path d="M13 13h5M13 17h3"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">BOM 快速报价</span>
            <span class="active-dot" v-if="!isCollapsed"></span>
          </router-link>
        </SidebarMenuTooltipWrap>

        <SidebarMenuGroupFlyout
          v-if="hasPermission('rfq.read')"
          :collapsed="isCollapsed"
          :expanded="openGroups.quotes"
          @toggle="toggleGroup('quotes')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M12 2v20M2 12h20"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">报价管理</span>
          </template>
          <template #chevron>
            <svg
              v-if="!isCollapsed"
              class="chevron"
              :class="{ rotated: openGroups.quotes }"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/quotes" class="submenu-item" active-class="active" exact>报价列表</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 订单 -->
        <div class="menu-section-label" v-if="!isCollapsed">订单</div>

        <!-- 销售管理：必须具备销售订单读取权限才显示入口 -->
        <SidebarMenuGroupFlyout
          v-if="hasPermission('sales-order.read')"
          :collapsed="isCollapsed"
          :expanded="openGroups.sales"
          @toggle="toggleGroup('sales')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">销售管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.sales }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/sales-orders" class="submenu-item" active-class="active">销售订单</router-link>
            <router-link
              v-if="hasPermission('sales-order.read')"
              to="/sales-order-items"
              class="submenu-item"
              active-class="active"
            >销售订单明细</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 采购管理：销售部门不显示 -->
        <SidebarMenuGroupFlyout
          v-if="(isSysAdmin || identityType !== 1) && (hasPermission('purchase-requisition.read') || hasPermission('purchase-order.read'))"
          :collapsed="isCollapsed"
          :expanded="openGroups.purchase"
          @toggle="toggleGroup('purchase')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M6 2L3 6v14a2 2 0 002 2h14a2 2 0 002-2V6l-3-4z"/>
                <line x1="3" y1="6" x2="21" y2="6"/>
                <path d="M16 10a4 4 0 01-8 0"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">采购管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.purchase }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link
              v-if="hasPermission('purchase-requisition.read')"
              to="/purchase-requisitions"
              class="submenu-item"
              active-class="active"
            >采购申请</router-link>
            <router-link
              v-if="hasPermission('purchase-order.read')"
              to="/purchase-orders"
              class="submenu-item"
              active-class="active"
            >采购订单</router-link>
            <router-link
              v-if="hasPermission('purchase-order.read')"
              to="/purchase-order-items"
              class="submenu-item"
              active-class="active"
            >采购订单明细</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 库存 -->
        <div class="menu-section-label" v-if="!isCollapsed">库存</div>

        <!-- 入库管理 -->
        <SidebarMenuGroupFlyout
          :collapsed="isCollapsed"
          :expanded="openGroups.stockInManagement"
          @toggle="toggleGroup('stockInManagement')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M3 7l9-4 9 4-9 4-9-4"/>
                <path d="M3 12l9 4 9-4"/>
                <path d="M3 17l9 4 9-4"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">入库管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.stockInManagement }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link
              v-if="hasPermission('purchase-order.read')"
              to="/logistics/arrival-notices"
              class="submenu-item"
              active-class="active"
            >到货通知</router-link>
            <router-link
              v-if="hasPermission('purchase-order.read')"
              to="/logistics/qc"
              class="submenu-item"
              active-class="active"
            >质检</router-link>
            <router-link to="/inventory/stock-in" class="submenu-item" active-class="active">入库</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 库存管理 -->
        <SidebarMenuGroupFlyout
          :collapsed="isCollapsed"
          :expanded="openGroups.inventory"
          @toggle="toggleGroup('inventory')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/>
                <polyline points="3.27 6.96 12 12.01 20.73 6.96"/>
                <line x1="12" y1="22.08" x2="12" y2="12"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">库存管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.inventory }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/inventory/list" class="submenu-item" active-class="active">库存管理</router-link>
            <router-link to="/inventory/check" class="submenu-item" active-class="active">库存盘点</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 出库管理 -->
        <SidebarMenuGroupFlyout
          :collapsed="isCollapsed"
          :expanded="openGroups.stockOutManagement"
          @toggle="toggleGroup('stockOutManagement')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M7 7h10v10H7z"/>
                <path d="M3 12h8"/>
                <path d="M13 12h8"/>
                <path d="M17 8l4 4-4 4"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">出库管理</span>
          </template>
          <template #chevron>
            <svg v-if="!isCollapsed" class="chevron" :class="{ rotated: openGroups.stockOutManagement }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/inventory/stock-out-notifies" class="submenu-item" active-class="active">出库通知</router-link>
            <router-link to="/inventory/stock-out" class="submenu-item" active-class="active">出库</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 财务：按部门隔离维度拆分“付款管理/收款管理”
             红框（财务管理折叠按钮）已移除；这里以“付款管理/收款管理”作为二级菜单组（带图标与收起展开）。 -->
        <div class="menu-section-label" v-if="!isCollapsed && (isSysAdmin || identityType !== 6)">财务</div>
        <div v-if="(isSysAdmin || identityType !== 6)" class="sidebar-nav-inline-group">
          <!-- 付款管理组：销售部门（identityType=1）不显示 -->
          <SidebarMenuGroupFlyout
            v-if="
              (isSysAdmin || identityType !== 1) &&
              (hasPermission('finance-payment.read') || hasPermission('finance-purchase-invoice.read'))
            "
            :collapsed="isCollapsed"
            :expanded="openGroups.financePayments"
            @toggle="toggleGroup('financePayments')"
          >
            <template #icon>
              <span class="menu-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <rect x="3" y="5" width="18" height="14" rx="2" ry="2"/>
                  <path d="M7 9h10"/>
                  <path d="M7 13h6"/>
                </svg>
              </span>
            </template>
            <template #label>
              <span class="menu-label" v-if="!isCollapsed">付款管理</span>
            </template>
            <template #chevron>
              <svg
                v-if="!isCollapsed"
                class="chevron"
                :class="{ rotated: openGroups.financePayments }"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path d="M6 9l6 6 6-6"/>
              </svg>
            </template>
            <template #submenu>
              <router-link
                v-if="hasPermission('finance-payment.read')"
                to="/finance/payments"
                class="submenu-item"
                active-class="active"
              >付款记录</router-link>
              <router-link
                v-if="hasPermission('finance-purchase-invoice.read')"
                to="/finance/purchase-invoices"
                class="submenu-item"
                active-class="active"
              >进项发票</router-link>
            </template>
          </SidebarMenuGroupFlyout>

          <!-- 收款管理组：采购部门（identityType=2）不显示 -->
          <SidebarMenuGroupFlyout
            v-if="
              (isSysAdmin || identityType !== 2) &&
              (hasPermission('finance-receipt.read') || hasPermission('finance-sell-invoice.read'))
            "
            :collapsed="isCollapsed"
            :expanded="openGroups.financeReceipts"
            @toggle="toggleGroup('financeReceipts')"
          >
            <template #icon>
              <span class="menu-icon">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                  <rect x="3" y="11" width="18" height="10" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 019.5 0v4"/>
                  <path d="M12 15v2"/>
                  <path d="M12 19h0"/>
                </svg>
              </span>
            </template>
            <template #label>
              <span class="menu-label" v-if="!isCollapsed">收款管理</span>
            </template>
            <template #chevron>
              <svg
                v-if="!isCollapsed"
                class="chevron"
                :class="{ rotated: openGroups.financeReceipts }"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
              >
                <path d="M6 9l6 6 6-6"/>
              </svg>
            </template>
            <template #submenu>
              <router-link
                v-if="hasPermission('finance-receipt.read')"
                to="/finance/receipts"
                class="submenu-item"
                active-class="active"
              >收款记录</router-link>
              <router-link
                v-if="hasPermission('finance-sell-invoice.read')"
                to="/finance/sell-invoices"
                class="submenu-item"
                active-class="active"
              >销项发票</router-link>
            </template>
          </SidebarMenuGroupFlyout>
        </div>

        <!-- 数据分析 -->
        <div class="menu-section-label" v-if="!isCollapsed">数据分析</div>

        <SidebarMenuTooltipWrap :collapsed="isCollapsed" tooltip="报表分析">
          <button type="button" class="menu-item" @click="handleUnimplemented('报表分析')">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <line x1="18" y1="20" x2="18" y2="10"/>
                <line x1="12" y1="20" x2="12" y2="4"/>
                <line x1="6" y1="20" x2="6" y2="14"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">报表分析</span>
          </button>
        </SidebarMenuTooltipWrap>

        <!-- 系统管理 -->
        <div class="menu-section-label" v-if="!isCollapsed">系统管理</div>
        <SidebarMenuGroupFlyout
          v-if="hasPermission('rbac.manage')"
          :collapsed="isCollapsed"
          :expanded="openGroups.systemManagement"
          @toggle="toggleGroup('systemManagement')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M12 15.5V3"/>
                <path d="M19 8.5L12 3 5 8.5"/>
                <path d="M5 15.5L12 21l7-5.5"/>
                <path d="M12 15.5h7"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">系统管理</span>
          </template>
          <template #chevron>
            <svg
              v-if="!isCollapsed"
              class="chevron"
              :class="{ rotated: openGroups.systemManagement }"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/system/users" class="submenu-item" active-class="active" exact>员工管理</router-link>
            <router-link to="/system/departments" class="submenu-item" active-class="active" exact>部门管理</router-link>
            <router-link to="/system/roles" class="submenu-item" active-class="active" exact>角色管理</router-link>
            <router-link to="/system/permissions" class="submenu-item" active-class="active" exact>权限管理</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <SidebarMenuGroupFlyout
          v-if="hasPermission('rbac.manage')"
          :collapsed="isCollapsed"
          :expanded="openGroups.paramManagement"
          @toggle="toggleGroup('paramManagement')"
        >
          <template #icon>
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <line x1="4" y1="21" x2="4" y2="14"/>
                <line x1="4" y1="10" x2="4" y2="3"/>
                <line x1="12" y1="21" x2="12" y2="12"/>
                <line x1="12" y1="8" x2="12" y2="3"/>
                <line x1="20" y1="21" x2="20" y2="16"/>
                <line x1="20" y1="12" x2="20" y2="3"/>
                <line x1="1" y1="14" x2="7" y2="14"/>
                <line x1="9" y1="8" x2="15" y2="8"/>
                <line x1="17" y1="16" x2="23" y2="16"/>
              </svg>
            </span>
          </template>
          <template #label>
            <span class="menu-label" v-if="!isCollapsed">参数管理</span>
          </template>
          <template #chevron>
            <svg
              v-if="!isCollapsed"
              class="chevron"
              :class="{ rotated: openGroups.paramManagement }"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
            >
              <path d="M6 9l6 6 6-6"/>
            </svg>
          </template>
          <template #submenu>
            <router-link to="/system/company-info" class="submenu-item" active-class="active" exact>公司信息</router-link>
          </template>
        </SidebarMenuGroupFlyout>

        <!-- 系统 -->
        <div class="menu-section-label" v-if="!isCollapsed">系统</div>

        <SidebarMenuTooltipWrap :collapsed="isCollapsed" tooltip="系统设置">
          <router-link to="/dashboard/settings" class="menu-item" active-class="active">
            <span class="menu-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <circle cx="12" cy="12" r="3"/>
                <path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/>
              </svg>
            </span>
            <span class="menu-label" v-if="!isCollapsed">系统设置</span>
          </router-link>
        </SidebarMenuTooltipWrap>
      </nav>

      <div class="sidebar-collapse-bar">
        <button
          type="button"
          class="collapse-btn"
          @click="toggleCollapse"
          :title="sidebarMode === 'full' ? '缩窄为边条' : '展开主菜单'"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path v-if="!isCollapsed" d="M15 18l-6-6 6-6"/>
            <path v-else d="M9 18l6-6-6-6"/>
          </svg>
        </button>
      </div>

    </aside>

    <!-- 主菜单与工作内容之间的可调宽度（仅完整主菜单时） -->
    <div
      v-if="sidebarMode === 'full'"
      class="col-splitter"
      title="拖拽调整主菜单宽度"
      @mousedown="onResizeStart('sidebar', $event)"
    />

    <div class="workspace-cols">
      <!-- 左侧面板（检索 / 多 Tab） -->
      <aside
        v-show="leftPanelVisible"
        class="aux-panel aux-left"
        :class="{ 'is-fullscreen': leftFullscreen }"
        :style="{ width: leftPanelWidth + 'px' }"
      >
        <div class="aux-panel-toolbar">
          <div class="aux-tabs" role="tablist">
            <button
              v-for="t in leftTabs"
              :key="t.id"
              type="button"
              class="aux-tab"
              :class="{ active: leftActiveTabId === t.id }"
              @click="leftActiveTabId = t.id"
            >
              {{ t.label }}
            </button>
          </div>
          <div class="aux-panel-actions">
            <button type="button" class="aux-icon-btn" title="全屏" @click="toggleLeftFullscreen()">⛶</button>
            <button type="button" class="aux-icon-btn" title="隐藏左侧面板" @click="toggleLeftPanel(false)">✕</button>
          </div>
        </div>
        <div class="aux-panel-body">
          <CustomerSearchPanel v-if="showCustomerSearchPanel" />
          <CustomerFavoritePanel v-else-if="showCustomerFavoritePanel" />
          <CustomerRecentHistoryPanel v-else-if="showCustomerRecentHistoryPanel" />
          <VendorSearchPanel v-else-if="showVendorSearchPanel" />
          <VendorFavoritePanel v-else-if="showVendorFavoritePanel" />
          <VendorRecentHistoryPanel v-else-if="showVendorRecentHistoryPanel" />
          <RFQSearchPanel v-else-if="showRfqSearchPanel" />
          <RFQItemSearchPanel v-else-if="showRfqItemSearchPanel" />
          <RFQFavoritePanel v-else-if="showRfqFavoritePanel || showRfqItemFavoritePanel" />
          <RFQRecentHistoryPanel v-else-if="showRfqRecentHistoryPanel || showRfqItemRecentHistoryPanel" />
          <SalesOrderSearchPanel v-else-if="showSalesOrderSearchPanel" />
          <SalesOrderFavoritePanel v-else-if="showSalesOrderFavoritePanel" />
          <SalesOrderRecentHistoryPanel v-else-if="showSalesOrderRecentHistoryPanel" />
          <QcSearchPanel v-else-if="showQcSearchPanel" />
          <StockInSearchPanel v-else-if="showStockInSearchPanel" />
          <StockOutSearchPanel v-else-if="showStockOutSearchPanel" />
          <PurchaseOrderFavoritePanel v-else-if="showPurchaseOrderFavoritePanel" />
          <PurchaseOrderRecentHistoryPanel v-else-if="showPurchaseOrderRecentHistoryPanel" />
          <template v-else>
            <p class="aux-placeholder">左侧面板 · {{ leftPanelTitle }}</p>
            <p class="aux-hint">子页面可 inject(WorkspaceLayoutKey)；或 window 派发 workspace:toggle-left / workspace:toggle-right</p>
          </template>
        </div>
      </aside>

      <div
        v-show="leftPanelVisible"
        class="col-splitter"
        title="拖拽调整左侧栏宽度"
        @mousedown="onResizeStart('left', $event)"
      />

      <!-- 主内容区域（页签与业务区在全局顶栏之下） -->
      <main class="main-content" :class="{ 'is-fullscreen': centerFullscreen }">
      <!-- 侧栏开关 + 页签栏 同一行 -->
      <header class="main-chrome-bar">
        <div class="workspace-top-tools">
          <button
            type="button"
            class="ws-tool-btn ws-tool-btn--icon"
            :class="{ active: leftPanelVisible }"
            :title="leftPanelVisible ? '隐藏左侧面板' : '展开左侧面板'"
            :aria-label="leftPanelVisible ? '隐藏左侧面板' : '展开左侧面板'"
            @click="toggleLeftPanel()"
          >
            <span class="ws-tool-icon" aria-hidden="true">
              <!-- 展开时「<」收起；隐藏时「>」展开 -->
              <svg v-if="leftPanelVisible" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M15 18l-6-6 6-6"/>
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M9 18l6-6-6-6"/>
              </svg>
            </span>
          </button>
        </div>
        <div class="main-chrome-center">
          <div class="tab-bar" v-if="tabs.length > 0" ref="tabBarRef">
        <div
          v-for="tab in visibleTabs"
          :key="tab.path"
          class="tab-item"
          :class="{ active: tab.path === activeTab }"
          @click="activateTab(tab)"
          @contextmenu.prevent="openContextMenu($event, tab)"
          :draggable="true"
          @dragstart="onDragStart($event, tab)"
          @dragover.prevent="onDragOver($event, tab)"
          @drop="onDrop($event, tab)"
          @dragend="onDragEnd"
        >
          <span class="tab-label">{{ tab.title }}</span>
          <button class="tab-close" @click.stop="closeTab(tab)" v-if="tabs.length > 1">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <line x1="18" y1="6" x2="6" y2="18"/>
              <line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
          <div class="tab-active-bar" v-if="tab.path === activeTab"></div>
        </div>
        <el-dropdown
          v-if="overflowTabs.length > 0"
          trigger="click"
          class="tab-overflow-dropdown"
          @command="onOverflowTabSelect"
        >
          <button class="tab-overflow-trigger" type="button" title="更多页签">...</button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item
                v-for="tab in overflowTabs"
                :key="tab.path"
                :command="tab.path"
                :class="{ 'is-active-item': tab.path === activeTab }"
              >
                {{ tab.title }}
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
          </div>
        </div>
        <div class="workspace-top-tools workspace-top-tools--end">
          <button
            type="button"
            class="ws-tool-btn ws-tool-btn--icon"
            :class="{ active: centerFullscreen }"
            title="工作内容区全屏"
            aria-label="工作内容区全屏"
            @click="toggleCenterFullscreen()"
          >
            <span class="ws-tool-icon ws-tool-icon--fs" aria-hidden="true">⛶</span>
          </button>
          <button
            type="button"
            class="ws-tool-btn ws-tool-btn--icon"
            :class="{ active: rightPanelVisible }"
            :title="rightPanelVisible ? '隐藏右侧面板' : '展开右侧面板'"
            :aria-label="rightPanelVisible ? '隐藏右侧面板' : '展开右侧面板'"
            @click="toggleRightPanel()"
          >
            <span class="ws-tool-icon" aria-hidden="true">
              <!-- 隐藏时「<」展开；展开时「>」收起（与左栏相反） -->
              <svg v-if="!rightPanelVisible" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M15 18l-6-6 6-6"/>
              </svg>
              <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M9 18l6-6-6-6"/>
              </svg>
            </span>
          </button>
        </div>
      </header>

      <!-- 右键菜单 -->
      <div
        class="context-menu"
        v-if="contextMenu.visible"
        :style="{ top: contextMenu.y + 'px', left: contextMenu.x + 'px' }"
        v-click-outside="closeContextMenu"
      >
        <div class="context-menu-title">{{ contextMenu.tab?.title }}</div>
        <div class="context-menu-divider"></div>
        <button class="context-menu-item" @click="contextMenuClose('current')">关闭当前</button>
        <button class="context-menu-item" @click="contextMenuClose('others')" :disabled="tabs.length <= 1">关闭其他</button>
        <button class="context-menu-item" @click="contextMenuClose('right')" :disabled="isLastTab">关闭右侧</button>
        <div class="context-menu-divider"></div>
        <button class="context-menu-item" @click="contextMenuClose('all')">全部关闭</button>
      </div>

      <!-- 页面内容 -->
      <div class="content-wrapper">
        <!-- path 作为 key：新增/编辑切换时重挂载；避免 fullPath（含 query）导致异常重挂载 -->
        <router-view :key="route.path" />
      </div>
    </main>

      <div
        v-show="rightPanelVisible"
        class="col-splitter"
        title="拖拽调整右侧栏宽度"
        @mousedown="onResizeStart('right', $event)"
      />

      <!-- 右侧面板（帮助） -->
      <aside
        v-show="rightPanelVisible"
        class="aux-panel aux-right"
        :class="{ 'is-fullscreen': rightFullscreen }"
        :style="{ width: rightPanelWidth + 'px' }"
      >
        <div class="aux-panel-toolbar">
          <div class="aux-tabs" role="tablist">
            <button
              v-for="t in rightTabs"
              :key="t.id"
              type="button"
              class="aux-tab"
              :class="{ active: rightActiveTabId === t.id }"
              @click="rightActiveTabId = t.id"
            >
              {{ t.label }}
            </button>
          </div>
          <div class="aux-panel-actions">
            <button type="button" class="aux-icon-btn" title="全屏" @click="toggleRightFullscreen()">⛶</button>
            <button type="button" class="aux-icon-btn" title="隐藏右侧面板" @click="toggleRightPanel(false)">✕</button>
          </div>
        </div>
        <div class="aux-panel-body">
          <HelpManualPanel />
        </div>
      </aside>
    </div>
    <!-- /.workspace-cols -->
    </div>
    <!-- /.app-layout-body -->
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick, type Directive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores'
import { ElMessageBox, ElNotification } from 'element-plus'
import { useWorkspaceLayout } from '@/composables/useWorkspaceLayout'
import SidebarMenuGroupFlyout from '@/layouts/components/SidebarMenuGroupFlyout.vue'
import SidebarMenuTooltipWrap from '@/layouts/components/SidebarMenuTooltipWrap.vue'
import CustomerSearchPanel from '@/components/Customer/CustomerSearchPanel.vue'
import CustomerFavoritePanel from '@/components/Customer/CustomerFavoritePanel.vue'
import CustomerRecentHistoryPanel from '@/components/Customer/CustomerRecentHistoryPanel.vue'
import VendorSearchPanel from '@/components/Vendor/VendorSearchPanel.vue'
import VendorFavoritePanel from '@/components/Vendor/VendorFavoritePanel.vue'
import VendorRecentHistoryPanel from '@/components/Vendor/VendorRecentHistoryPanel.vue'
import RFQSearchPanel from '@/components/RFQ/RFQSearchPanel.vue'
import RFQItemSearchPanel from '@/components/RFQ/RFQItemSearchPanel.vue'
import RFQFavoritePanel from '@/components/RFQ/RFQFavoritePanel.vue'
import RFQRecentHistoryPanel from '@/components/RFQ/RFQRecentHistoryPanel.vue'
import SalesOrderSearchPanel from '@/components/SalesOrder/SalesOrderSearchPanel.vue'
import QcSearchPanel from '@/components/Logistics/QcSearchPanel.vue'
import StockInSearchPanel from '@/components/Inventory/StockInSearchPanel.vue'
import StockOutSearchPanel from '@/components/Inventory/StockOutSearchPanel.vue'
import SalesOrderFavoritePanel from '@/components/SalesOrder/SalesOrderFavoritePanel.vue'
import SalesOrderRecentHistoryPanel from '@/components/SalesOrder/SalesOrderRecentHistoryPanel.vue'
import PurchaseOrderFavoritePanel from '@/components/purchaseOrder/PurchaseOrderFavoritePanel.vue'
import PurchaseOrderRecentHistoryPanel from '@/components/purchaseOrder/PurchaseOrderRecentHistoryPanel.vue'
import HelpManualPanel from '@/components/workspace/HelpManualPanel.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const {
  sidebarMode,
  sidebarWidthPx,
  isSidebarCollapsed,
  cycleSidebarMode,
  leftPanelVisible,
  rightPanelVisible,
  leftPanelWidth,
  rightPanelWidth,
  leftFullscreen,
  centerFullscreen,
  rightFullscreen,
  leftTabs,
  rightTabs,
  leftActiveTabId,
  rightActiveTabId,
  onResizeStart,
  toggleLeftPanel,
  toggleRightPanel,
  toggleLeftFullscreen,
  toggleRightFullscreen,
  toggleCenterFullscreen
} = useWorkspaceLayout()

/** 客户首页 / 列表 / 详情 / 新建编辑：左栏「检索 / 收藏 / 历史」 */
const isCustomerLeftAuxRoute = computed(() => {
  const n = route.name
  return (
    n === 'CustomerHome' ||
    n === 'CustomerList' ||
    n === 'CustomerDetail' ||
    n === 'CustomerEdit' ||
    n === 'CustomerCreate' ||
    n === 'CustomerRecycleBin' ||
    n === 'CustomerBlacklist' ||
    n === 'CustomerFreezeManagement' ||
    n === 'CustomerContactCreate' ||
    n === 'CustomerContactEdit'
  )
})

const showCustomerSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isCustomerLeftAuxRoute.value
)

const showCustomerFavoritePanel = computed(
  () => leftActiveTabId.value === 'l2' && isCustomerLeftAuxRoute.value
)

const showCustomerRecentHistoryPanel = computed(
  () => leftActiveTabId.value === 'l3' && isCustomerLeftAuxRoute.value
)

/** 供应商列表 / 详情 / 新建编辑：左栏「收藏 / 历史」（与客户模块对齐） */
const isVendorLeftAuxRoute = computed(() => {
  const n = route.name
  return (
    n === 'VendorHome' ||
    n === 'VendorList' ||
    n === 'VendorDetail' ||
    n === 'VendorEdit' ||
    n === 'VendorCreate' ||
    n === 'VendorRecycleBin' ||
    n === 'VendorBlacklist' ||
    n === 'VendorFreezeManagement' ||
    n === 'VendorContactCreate' ||
    n === 'VendorContactEdit'
  )
})

const showVendorSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isVendorLeftAuxRoute.value
)

const showVendorFavoritePanel = computed(
  () => leftActiveTabId.value === 'l2' && isVendorLeftAuxRoute.value
)

const showVendorRecentHistoryPanel = computed(
  () => leftActiveTabId.value === 'l3' && isVendorLeftAuxRoute.value
)

/** 需求列表 / 详情 / 新建编辑：左栏「检索 / 收藏 / 历史」（与客户模块一致） */
const isRfqLeftAuxRoute = computed(() => {
  const n = route.name
  return n === 'RFQList' || n === 'RFQDetail' || n === 'RFQCreate' || n === 'RFQEdit'
})

const showRfqSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isRfqLeftAuxRoute.value
)

const showRfqFavoritePanel = computed(
  () => leftActiveTabId.value === 'l2' && isRfqLeftAuxRoute.value
)

const showRfqRecentHistoryPanel = computed(
  () => leftActiveTabId.value === 'l3' && isRfqLeftAuxRoute.value
)

/** 需求明细列表 /rfq-items：左栏「检索 / 收藏(主表) / 历史(主表)」 */
const isRfqItemListLeftAuxRoute = computed(() => route.name === 'RFQItemList')

const showRfqItemSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isRfqItemListLeftAuxRoute.value
)

const showRfqItemFavoritePanel = computed(
  () => leftActiveTabId.value === 'l2' && isRfqItemListLeftAuxRoute.value
)

const showRfqItemRecentHistoryPanel = computed(
  () => leftActiveTabId.value === 'l3' && isRfqItemListLeftAuxRoute.value
)

/** 销售订单列表/详情/新建/明细列表：左栏「检索 / 收藏 / 历史」 */
const isSalesOrderLeftAuxRoute = computed(() => {
  const n = route.name
  return (
    n === 'SalesOrderList' ||
    n === 'SalesOrderDetail' ||
    n === 'SalesOrderCreate' ||
    n === 'SalesOrderItemList'
  )
})

const showSalesOrderSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isSalesOrderLeftAuxRoute.value
)

const showSalesOrderFavoritePanel = computed(
  () => leftActiveTabId.value === 'l2' && isSalesOrderLeftAuxRoute.value
)

const showSalesOrderRecentHistoryPanel = computed(
  () => leftActiveTabId.value === 'l3' && isSalesOrderLeftAuxRoute.value
)

/** 质检列表/新建：左栏「检索」 */
const isQcLeftAuxRoute = computed(() => route.name === 'QcList' || route.name === 'QcCreate')

const showQcSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isQcLeftAuxRoute.value
)

/** 入库单列表/新建/详情：左栏「检索」 */
const isStockInLeftAuxRoute = computed(() => {
  const n = route.name
  return n === 'StockInList' || n === 'StockInCreate' || n === 'StockInDetail'
})

const showStockInSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isStockInLeftAuxRoute.value
)

/** 出库单列表/新建：左栏「检索」 */
const isStockOutLeftAuxRoute = computed(() => {
  const n = route.name
  return n === 'StockOutList' || n === 'StockOutCreate'
})

const showStockOutSearchPanel = computed(
  () => leftActiveTabId.value === 'l1' && isStockOutLeftAuxRoute.value
)

/** 采购订单列表/详情/新建/编辑/明细/报表：左栏「收藏 / 历史」 */
const isPurchaseOrderLeftAuxRoute = computed(() => {
  const n = route.name
  return (
    n === 'PurchaseOrderList' ||
    n === 'PurchaseOrderDetail' ||
    n === 'PurchaseOrderCreate' ||
    n === 'PurchaseOrderEdit' ||
    n === 'PurchaseOrderItemList' ||
    n === 'PurchaseOrderReport'
  )
})

const showPurchaseOrderFavoritePanel = computed(
  () => leftActiveTabId.value === 'l2' && isPurchaseOrderLeftAuxRoute.value
)

const showPurchaseOrderRecentHistoryPanel = computed(
  () => leftActiveTabId.value === 'l3' && isPurchaseOrderLeftAuxRoute.value
)

/** 模板沿用 isCollapsed：仅「边条」模式隐藏菜单文字 */
const isCollapsed = isSidebarCollapsed

const leftPanelTitle = computed(() => leftTabs.value.find(t => t.id === leftActiveTabId.value)?.label ?? '')
const openGroups = ref({
  purchase: false,
  sales: false,
  inventory: false,
  stockInManagement: false,
  stockOutManagement: false,
  customers: false,
  vendors: false,
  rfqs: false,
  quotes: false,
  finance: false,
  financePayments: false,
  financeReceipts: false,
  systemManagement: false,
  paramManagement: false
})

const expandAllGroups = () => {
  // SYS_ADMIN 需要“看见所有菜单项”，因此默认把所有分组都展开
  openGroups.value = {
    purchase: true,
    sales: true,
    inventory: true,
    stockInManagement: true,
    stockOutManagement: true,
    customers: true,
    vendors: true,
    rfqs: true,
    quotes: true,
    finance: true,
    financePayments: true,
    financeReceipts: true,
    systemManagement: true,
    paramManagement: true
  }
}

const toggleCollapse = () => {
  cycleSidebarMode()
  if (sidebarMode.value === 'narrow') {
    openGroups.value = { purchase: false, sales: false, inventory: false, stockInManagement: false, stockOutManagement: false, customers: false, vendors: false, rfqs: false, quotes: false, finance: false, financePayments: false, financeReceipts: false, systemManagement: false, paramManagement: false }
  } else if (sidebarMode.value === 'full' && isSysAdmin.value) {
    expandAllGroups()
  }
}

const toggleGroup = (group: keyof typeof openGroups.value) => {
  openGroups.value[group] = !openGroups.value[group]
}

const userName = computed(() => authStore.user?.userName || '管理员')
const userEmail = computed(() => authStore.user?.email || '')
const userInitial = computed(() => (authStore.user?.userName || '管')[0].toUpperCase())

/** 顶栏通知角标数量（后续可对接未读接口） */
const headerNotifyCount = ref(1)

const pageTitleMap: Record<string, string> = {
  '/dashboard': '控制台',
  '/pending-approvals': '待审批',
  '/custome': '客户首页',
  '/customerlist': '客户',
  '/customers/create': '新增客户',
  '/customers/recycle-bin': '客户回收站',
  '/customers/blacklist': '黑名单管理',
  '/customers/frozen': '冻结管理',
  '/vendor': '供应商首页',
  '/vendorlist': '供应商',
  '/vendors/create': '新增供应商',
  '/vendors/recycle-bin': '供应商回收站',
  '/vendors/blacklist': '供应商黑名单',
  '/vendors/frozen': '冻结管理',
  '/system/users': '员工管理',
  '/system/users/create': '新增员工',
  '/system/roles': '角色管理',
  '/system/roles/create': '新增角色',
  '/system/permissions': '权限管理',
  '/system/permissions/create': '新增权限',
  '/system/departments': '部门管理',
  '/system/departments/create': '新增部门',
  '/system/company-info': '公司信息',
  '/inventory/list': '库存列表',
  '/inventory/stock-in': '入库管理',
  '/inventory/stock-out': '出库管理',
  '/inventory/stock-out-notifies': '出库通知',
  '/inventory/transfer': '库存调拨',
  '/inventory/check': '库存盘点',
  '/reports': '报表分析',
  '/dashboard/settings': '系统设置',
  '/rfq': '需求首页',
  '/rfqlist': '需求列表',
  '/pn': '物料列表',
  '/rfq-items': '需求明细',
  '/quotes': '报价列表',
  '/quotes/create': '新建报价',
  '/purchase-orders': '采购订单',
  '/purchase-order-items': '采购订单明细',
  '/logistics/arrival-notices': '到货通知',
  '/logistics/qc': '质检',
  '/sales-orders': '销售订单',
  '/sales-order-items': '销售订单明细',
  '/stock-out-notifies': '出库通知',
  '/profile': '个人设置',
  '/drafts': '草稿箱',
  '/debug': 'Debug',
  '/debug/data': 'Debug 模拟数据',
}

const currentPageTitle = computed(() => {
  const path = route.path
  if (pageTitleMap[path]) return pageTitleMap[path]
  if (/^\/purchase-orders\/[^/]+\/report$/.test(path)) return '采购订单报表'
  if (/^\/quotes\/[^/]+\/edit$/.test(path)) return '编辑报价'
  if (path.includes('/customers/') && path.includes('/edit')) return '编辑客户'
  if (path.includes('/customers/')) return '客户详情'
  if (path.includes('/vendors/') && path.includes('/edit')) return '编辑供应商'
  if (path === '/vendors/create') return '新增供应商'
  return route.meta?.title as string || 'FrontCRM'
})

// ===== 账号下拉菜单 =====
const dropdownOpen = ref(false)
const toggleDropdown = () => { dropdownOpen.value = !dropdownOpen.value }
const closeDropdown = () => { dropdownOpen.value = false }

const handleLogout = async () => {
  closeDropdown()
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '退出登录', {
      confirmButtonText: '确定退出',
      cancelButtonText: '取消',
      type: 'warning'
    })
    authStore.logout()
    router.push('/login')
  } catch {
    // 取消操作
  }
}

const handleUnimplemented = (name: string) => {
  ElNotification.info({ title: '功能开发中', message: `「${name}」功能正在开发中，敬请期待` })
}

const hasPermission = (code: string) => authStore.hasPermission(code)
const identityType = computed(() => authStore.user?.identityType ?? 0)
const isSysAdmin = computed(() => authStore.user?.isSysAdmin === true)

/** 客户板块：采购(2)、采购助理(3)、物流(6) 不显示 */
const showCustomerMenuSection = computed(() => {
  if (isSysAdmin.value) return true
  const t = identityType.value
  return t !== 2 && t !== 3 && t !== 6
})

/** 供应商板块：销售(1)、物流(6) 不显示 */
const showVendorMenuSection = computed(() => {
  if (isSysAdmin.value) return true
  const t = identityType.value
  return t !== 1 && t !== 6
})

// 管理员登录时默认展开所有分组（不强制主菜单宽度，以便可缩为边条/隐藏）
watch(
  isSysAdmin,
  (v) => {
    if (v) expandAllGroups()
  },
  { immediate: true }
)

// 待办：只在员工拥有任一“审批”相关写权限时显示入口
const hasAnyApprovalPermission = computed(() => {
  const codes = ['vendor.write', 'rfq.write', 'sales-order.write', 'finance-receipt.write', 'finance-payment.write']
  return codes.some(code => hasPermission(code))
})

// 根据当前路由自动展开“需求管理”分组
watch(
  () => route.path,
  (p) => {
    if (
      p === '/rfq' ||
      p === '/rfqlist' ||
      p.startsWith('/rfqs/') ||
      p === '/rfq-items' ||
      p === '/pn'
    ) {
      openGroups.value.rfqs = true
    }
    if (p === '/quotes' || p.startsWith('/quotes/')) {
      openGroups.value.quotes = true
    }
    if (p === '/sales-orders' || p.startsWith('/sales-orders/') || p === '/sales-order-items') {
      openGroups.value.sales = true
    }
    if (
      p === '/purchase-orders' ||
      p.startsWith('/purchase-orders/') ||
      p === '/purchase-order-items'
    ) {
      openGroups.value.purchase = true
    }
    if (p.startsWith('/logistics/') || p === '/inventory/stock-in' || p.startsWith('/inventory/stock-in/')) {
      openGroups.value.stockInManagement = true
    }
    if (p === '/inventory/stock-out' || p.startsWith('/inventory/stock-out/') || p === '/inventory/stock-out-notifies') {
      openGroups.value.stockOutManagement = true
    }
    if (p.startsWith('/system/')) {
      openGroups.value.systemManagement = true
    }
    if (p === '/system/company-info') {
      openGroups.value.paramManagement = true
    }
  },
  { immediate: true }
)

// ===== Tab Bar 多标签页 =====
interface TabItem {
  path: string
  title: string
}

const STORAGE_KEY = 'crm_tabs'
const ACTIVE_KEY = 'crm_active_tab'

const loadTabs = (): TabItem[] => {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return saved ? JSON.parse(saved) : []
  } catch { return [] }
}

const tabs = ref<TabItem[]>(loadTabs())
const activeTab = ref<string>(localStorage.getItem(ACTIVE_KEY) || '')
const tabBarRef = ref<HTMLElement | null>(null)
const visibleTabCount = ref<number>(tabs.value.length)
let tabBarResizeObserver: ResizeObserver | null = null

const saveTabs = () => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(tabs.value))
  localStorage.setItem(ACTIVE_KEY, activeTab.value)
}

const estimateTabWidth = (tab: TabItem) => {
  const titleLen = tab.title?.length ?? 0
  return Math.min(200, Math.max(92, 50 + titleLen * 14))
}

const recalcTabOverflow = () => {
  const host = tabBarRef.value
  if (!host) {
    visibleTabCount.value = tabs.value.length
    return
  }
  const totalWidth = host.clientWidth
  if (totalWidth <= 0) {
    visibleTabCount.value = tabs.value.length
    return
  }

  const moreBtnWidth = 44
  let used = 0
  let count = 0

  for (const tab of tabs.value) {
    const tabWidth = estimateTabWidth(tab)
    const reserve = count < tabs.value.length - 1 ? moreBtnWidth : 0
    if (used + tabWidth + reserve > totalWidth) break
    used += tabWidth + 2 // include gap
    count += 1
  }

  // 至少显示一个，避免全量都被折叠
  visibleTabCount.value = Math.max(1, count)
}

const visibleTabs = computed(() => tabs.value.slice(0, visibleTabCount.value))
const overflowTabs = computed(() => tabs.value.slice(visibleTabCount.value))

const onOverflowTabSelect = (path: string) => {
  const hit = tabs.value.find(t => t.path === path)
  if (hit) activateTab(hit)
}

// 监听路由变化，自动添加/激活标签
watch(() => route.path, async (newPath) => {
  const title = currentPageTitle.value
  const exists = tabs.value.find(t => t.path === newPath)
  if (!exists) {
    tabs.value.push({ path: newPath, title })
  }
  activeTab.value = newPath
  saveTabs()
  await nextTick()
  recalcTabOverflow()
}, { immediate: true })

const activateTab = (tab: TabItem) => {
  activeTab.value = tab.path
  router.push(tab.path)
  saveTabs()
}

const closeTab = (tab: TabItem) => {
  const idx = tabs.value.findIndex(t => t.path === tab.path)
  tabs.value.splice(idx, 1)
  if (activeTab.value === tab.path) {
    const next = tabs.value[idx] || tabs.value[idx - 1]
    if (next) {
      activeTab.value = next.path
      router.push(next.path)
    } else {
      activeTab.value = ''
      router.push('/dashboard')
    }
  }
  saveTabs()
  nextTick(() => recalcTabOverflow())
}

// 拖拽排序
const dragSource = ref<TabItem | null>(null)

const onDragStart = (_e: DragEvent, tab: TabItem) => {
  dragSource.value = tab
}

const onDragOver = (_e: DragEvent, _tab: TabItem) => {}

const onDrop = (_e: DragEvent, target: TabItem) => {
  if (!dragSource.value || dragSource.value.path === target.path) return
  const fromIdx = tabs.value.findIndex(t => t.path === dragSource.value!.path)
  const toIdx = tabs.value.findIndex(t => t.path === target.path)
  const newTabs = [...tabs.value]
  newTabs.splice(fromIdx, 1)
  newTabs.splice(toIdx, 0, dragSource.value)
  tabs.value = newTabs
  saveTabs()
}

const onDragEnd = () => {
  dragSource.value = null
}

// 右键菜单
const contextMenu = ref({
  visible: false,
  x: 0,
  y: 0,
  tab: null as TabItem | null
})

const isLastTab = computed(() => {
  if (!contextMenu.value.tab) return true
  const idx = tabs.value.findIndex(t => t.path === contextMenu.value.tab!.path)
  return idx === tabs.value.length - 1
})

const openContextMenu = (e: MouseEvent, tab: TabItem) => {
  contextMenu.value = { visible: true, x: e.clientX, y: e.clientY, tab }
}

const closeContextMenu = () => {
  contextMenu.value.visible = false
}

const contextMenuClose = (type: 'current' | 'others' | 'right' | 'all') => {
  const tab = contextMenu.value.tab
  if (!tab) return
  const idx = tabs.value.findIndex(t => t.path === tab.path)
  if (type === 'current') {
    closeTab(tab)
  } else if (type === 'others') {
    tabs.value = [tab]
    activeTab.value = tab.path
    router.push(tab.path)
  } else if (type === 'right') {
    tabs.value = tabs.value.slice(0, idx + 1)
    if (!tabs.value.find(t => t.path === activeTab.value)) {
      activeTab.value = tab.path
      router.push(tab.path)
    }
  } else if (type === 'all') {
    tabs.value = []
    activeTab.value = ''
    router.push('/dashboard')
  }
  saveTabs()
  closeContextMenu()
  nextTick(() => recalcTabOverflow())
}

// v-click-outside 指令
const vClickOutside: Directive = {
  mounted(el, binding) {
    el._clickOutsideHandler = (event: MouseEvent) => {
      if (!el.contains(event.target as Node)) {
        binding.value()
      }
    }
    document.addEventListener('mousedown', el._clickOutsideHandler)
  },
  unmounted(el) {
    document.removeEventListener('mousedown', el._clickOutsideHandler)
  }
}

onMounted(() => {
  // 如果没有标签，初始化当前路由
  if (tabs.value.length === 0) {
    const title = currentPageTitle.value
    tabs.value.push({ path: route.path, title })
    activeTab.value = route.path
    saveTabs()
  }
  nextTick(() => recalcTabOverflow())
  if (tabBarRef.value && typeof ResizeObserver !== 'undefined') {
    tabBarResizeObserver = new ResizeObserver(() => recalcTabOverflow())
    tabBarResizeObserver.observe(tabBarRef.value)
  }
})

onBeforeUnmount(() => {
  tabBarResizeObserver?.disconnect()
  tabBarResizeObserver = null
})
</script>

<style lang="scss" scoped>
@use '@/assets/styles/variables' as vars;

// ============================================================
// AppLayout — 深海量子风布局
// 设计规范：三层面板 #192A3F / #0A1628 / #162233
// 侧边栏宽度：展开 240px / 收起 64px
// ============================================================

.app-layout {
  display: flex;
  flex-direction: column;
  width: 100vw;
  height: 100vh;
  background: vars.$layer-1;
  overflow: clip; // 使用 clip 而非 hidden，避免裁剪 position:fixed 的元素（如 ElMessage）
}

.app-layout-body {
  flex: 1;
  display: flex;
  flex-direction: row;
  min-height: 0;
  min-width: 0;
  overflow: hidden;
}

// ── 全局顶栏（更深底色 · 浮于内容之上 · 底边浅色分割）──────────
.global-top-bar {
  flex-shrink: 0;
  width: 100%;
  height: 56px;
  position: relative;
  z-index: 120;
  isolation: isolate;
  background: #070f1b;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  box-shadow:
    0 1px 0 rgba(255, 255, 255, 0.08),
    0 6px 20px rgba(0, 0, 0, 0.55),
    0 12px 40px -8px rgba(0, 0, 0, 0.4);
  overflow: visible;
}

.global-top-inner {
  height: 100%;
  max-width: 100%;
  padding: 0 20px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.global-logo {
  display: inline-flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  flex-shrink: 0;
  min-width: 0;
  &:hover .global-logo-title {
    color: #fff;
  }
}

.global-logo-mark {
  display: flex;
  width: 36px;
  height: 36px;
  flex-shrink: 0;
  svg {
    width: 100%;
    height: 100%;
  }
}

.global-logo-stack {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  gap: 2px;
  line-height: 1.15;
}

.global-logo-title {
  font-family: 'Orbitron', 'Noto Sans SC', sans-serif;
  font-weight: 700;
  font-size: 1rem;
  letter-spacing: 0.04em;
  color: #e8f4ff;
  white-space: nowrap;
}

.global-logo-sub {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 11px;
  font-weight: 500;
  color: rgba(0, 212, 255, 0.85);
  white-space: nowrap;
}

.global-top-right {
  display: flex;
  align-items: center;
  gap: 14px;
  flex-shrink: 0;
}

.global-notify-btn {
  position: relative;
  width: 40px;
  height: 40px;
  padding: 0;
  border: 1px solid rgba(0, 212, 255, 0.12);
  border-radius: 8px;
  background: rgba(0, 40, 80, 0.45);
  color: rgba(180, 210, 230, 0.85);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, border-color 0.2s, color 0.2s;
  &:hover {
    background: rgba(0, 212, 255, 0.1);
    border-color: rgba(0, 212, 255, 0.28);
    color: #00d4ff;
  }
}

.global-notify-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  svg {
    width: 20px;
    height: 20px;
  }
}

.global-notify-badge {
  position: absolute;
  top: 4px;
  right: 4px;
  min-width: 16px;
  height: 16px;
  padding: 0 4px;
  border-radius: 8px;
  background: #c95745;
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  line-height: 16px;
  text-align: center;
  font-family: 'Space Mono', monospace;
}

.global-user-dropdown {
  position: relative;
}

.global-user-trigger {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 4px 4px 4px 4px;
  border: none;
  border-radius: 8px;
  background: transparent;
  cursor: pointer;
  transition: background 0.15s;
  &:hover {
    background: rgba(0, 212, 255, 0.08);
  }
}

.global-user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066ff, #00d4ff);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 13px;
  font-weight: 700;
  color: #fff;
  flex-shrink: 0;
}

.global-user-name {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 14px;
  font-weight: 500;
  color: #e8f4ff;
  max-width: 140px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.global-user-chevron {
  width: 14px;
  height: 14px;
  color: rgba(80, 187, 227, 0.65);
  flex-shrink: 0;
  transition: transform 0.2s;
  &.open {
    transform: rotate(180deg);
  }
}

// ── 侧边栏 ──────────────────────────────────────────────────
// 高度随 app-layout-body（顶栏已占 56px），勿用 100vh 否则底部折叠条会被 body 的 overflow 裁掉
.sidebar {
  width: 240px;
  align-self: stretch;
  min-height: 0;
  // 图2：与旧版一致的 #001529 系侧栏底
  background: #001529;
  border-right: 1px solid rgba(0, 212, 255, 0.1);
  display: flex;
  flex-direction: column;
  transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  flex-shrink: 0;
  position: relative;
  z-index: 10;

  &.collapsed {
    width: 64px;

    .sidebar-nav {
      display: flex;
      flex-direction: column;
      // 相对面板左缘至右缘（红线）整宽居中，子项宽度收缩为图标列
      align-items: center;
      gap: 10px;
      padding: 10px 0 10px;
    }
  }
}

// 侧栏底部：折叠主菜单（左对齐 + 固定左右 padding，避免完整/边条宽度变化时按钮水平漂移，便于同一点连点）
.sidebar-collapse-bar {
  flex-shrink: 0;
  padding: 8px;
  border-top: 1px solid rgba(0, 212, 255, 0.08);
  display: flex;
  justify-content: flex-start;
  align-items: center;
}

.collapse-btn {
  width: 32px;
  height: 32px;
  appearance: none;
  -webkit-appearance: none;
  background: rgba(0, 212, 255, 0.06);
  border: 1px solid rgba(0, 212, 255, 0.25);
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: rgba(0, 212, 255, 0.75);
  transition: all 0.2s;

  svg {
    width: 14px;
    height: 14px;
  }

  &:hover {
    background: rgba(0, 212, 255, 0.15);
    color: #00D4FF;
    border-color: #00D4FF;
  }
}

// 导航区域（min-height:0 保证在 flex 列里可收缩，底部折叠条始终留在视口内）
.sidebar-nav {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
  // 图2：整体更紧凑，行距接近字高
  padding: 10px 8px;

  /* 隐藏滚动条，保留滚轮/触控滚动 */
  scrollbar-width: none;
  -ms-overflow-style: none;

  &::-webkit-scrollbar {
    width: 0;
    height: 0;
    display: none;
  }
}

// 分组标签（图2：小字、偏淡青、上下留白收紧）
.menu-section-label {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 11px;
  font-weight: 500;
  color: rgba(100, 185, 230, 0.48);
  letter-spacing: 0.4px;
  text-transform: none;
  padding: 8px 10px 3px;
  white-space: nowrap;
  overflow: hidden;
}

// 菜单项（含 SidebarMenuGroupFlyout / el-popover 内 button，scoped 需 :deep 否则出现白底默认按钮样式）
.sidebar :deep(.menu-item) {
  display: flex;
  flex-direction: row;
  flex-wrap: nowrap;
  align-items: center;
  gap: 8px;
  // 主项：图标在左、文字在右；纵向略紧
  padding: 4px 10px;
  border-radius: 4px;
  cursor: pointer;
  // 默认略压白，避免刺眼大白字
  color: rgba(175, 200, 222, 0.9);
  text-decoration: none;
  transition: background 0.2s, color 0.2s;
  position: relative;
  width: 100%;
  max-width: 100%;
  border: none;
  background: transparent;
  appearance: none;
  -webkit-appearance: none;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  font-weight: 400;
  line-height: 1.35;
  white-space: nowrap;
  overflow: hidden;
  margin-bottom: 0;
  box-sizing: border-box;

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    color: rgba(210, 232, 248, 0.95);

    .menu-icon svg {
      stroke: #00d4ff;
    }
  }

  &.active,
  &.router-link-active {
    background: rgba(0, 212, 255, 0.14);
    color: #7fdfff;
    border-left: 2px solid #00d4ff;
    padding-left: 8px;
    box-shadow: inset 0 0 20px rgba(0, 212, 255, 0.07);

    .menu-icon svg {
      stroke: #00d4ff;
    }
  }

  &.has-children {
    justify-content: flex-start;
  }

  .active-dot {
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #00d4ff;
    margin-left: auto;
    flex-shrink: 0;
    box-shadow: 0 0 8px rgba(0, 212, 255, 0.85);
  }

  .menu-icon {
    width: 20px;
    height: 20px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;

    svg {
      width: 18px;
      height: 18px;
      stroke: rgba(0, 212, 255, 0.72);
      transition: stroke 0.2s;
    }
  }
}

// 边条模式：图标在面板宽度内水平居中；完整模式里 has-children 左对齐，此处后声明以覆盖
.sidebar.collapsed :deep(.menu-item.has-children) {
  justify-content: center;
}

.sidebar.collapsed :deep(.menu-item) {
  justify-content: center;
  margin-bottom: 0;
  padding: 8px 0;
  width: auto;
  max-width: 100%;
  box-sizing: border-box;

  &.active,
  &.router-link-active {
    border-left: none;
    padding: 8px 0;
  }
}

// 边条：触发器不再拉满整行，便于在 64px 面板内几何居中
.sidebar.collapsed :deep(.el-tooltip__trigger) {
  width: auto;
  max-width: 100%;
  display: flex;
  justify-content: center;
}

.sidebar.collapsed :deep(.el-popover__reference-wrapper) {
  width: auto;
  max-width: 100%;
  display: flex;
  justify-content: center;
}

.sidebar.collapsed :deep(.menu-group) {
  width: auto;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
}

// 财务区两个分组包在同一容器内，边条下与全局 gap 一致（已减半）
.sidebar.collapsed .sidebar-nav-inline-group {
  display: flex;
  flex-direction: column;
  gap: 10px;
  align-items: center;
  width: auto;
  align-self: center;
}

.sidebar :deep(.menu-label) {
  flex: 1;
  min-width: 0;
  text-align: left;
  font-size: 13px;
  font-weight: 400;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sidebar :deep(.chevron) {
  width: 14px;
  height: 14px;
  flex-shrink: 0;
  margin-left: auto;
  transition: transform 0.25s;
  stroke: rgba(0, 212, 255, 0.5);

  &.rotated {
    transform: rotate(180deg);
  }
}

// 子菜单（图2：缩进保留、上下留白收紧）
.sidebar :deep(.submenu) {
  padding: 2px 0 5px 28px;
  overflow: hidden;
}

// 侧栏内 el-tooltip（仅边条模式，由 SidebarMenuTooltipWrap 渲染）：去默认块样式
.sidebar :deep(.el-tooltip__trigger) {
  border: none;
  background: transparent;
  padding: 0;
  font: inherit;
  color: inherit;
}

// el-popover 引用层不引入浅色底
.sidebar :deep(.el-popover__reference-wrapper) {
  display: block;
  width: 100%;
  background: transparent !important;
  border: none;
  padding: 0;
}

.submenu-item {
  display: block;
  width: 100%;
  padding: 4px 8px 4px 15px;
  border-radius: 3px;
  color: rgba(175, 200, 220, 0.82);
  text-decoration: none;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  font-weight: 400;
  line-height: 1.35;
  transition: color 0.2s, background 0.2s, border-color 0.2s;
  margin-bottom: 0;
  position: relative;
  /* Reset button defaults */
  background: transparent;
  border: none;
  outline: none;
  cursor: pointer;
  text-align: left;
  box-sizing: border-box;

  // 旧版：空心小圆点（非实心填充）
  &::before {
    content: '';
    position: absolute;
    left: 3px;
    top: 50%;
    transform: translateY(-50%);
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: transparent;
    border: 1px solid rgba(0, 212, 255, 0.38);
    box-sizing: border-box;
    transition: border-color 0.2s, box-shadow 0.2s;
  }

  &:hover {
    color: #e8f4ff;
    background: rgba(0, 212, 255, 0.05);

    &::before {
      border-color: #00d4ff;
    }
  }

  &.active,
  &.router-link-active {
    color: #7fdfff;
    background: rgba(0, 212, 255, 0.06);

    &::before {
      border-color: #00d4ff;
      box-shadow: 0 0 8px rgba(0, 212, 255, 0.55);
    }
  }
}

.submenu-group {
  margin-top: 2px;
}

.submenu-group-label {
  padding: 4px 10px 2px;
  font-size: 11px;
  font-weight: 600;
  color: rgba(200, 216, 232, 0.6);
  letter-spacing: 0.5px;
}

.finance-inline {
  padding: 6px 0 2px;
}

// 底部账号信息
.sidebar-footer {
  border-top: 1px solid rgba(0, 212, 255, 0.08);
  padding: 12px 8px;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px;
  border-radius: 8px;
  background: rgba(0, 212, 255, 0.05);
  border: 1px solid rgba(0, 212, 255, 0.1);

  &.collapsed {
    justify-content: center;
    padding: 8px 4px;
  }
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 13px;
  font-weight: 700;
  color: #fff;
  flex-shrink: 0;
}

.user-details {
  flex: 1;
  min-width: 0;
  overflow: hidden;
}

.user-name {
  display: block;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  color: #E8F4FF;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-email {
  display: block;
  font-size: 10.5px;
  color: rgba(80, 187, 227, 0.5);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.logout-btn {
  width: 28px;
  height: 28px;
  border: none;
  background: transparent;
  color: rgba(80, 187, 227, 0.5);
  cursor: pointer;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all 0.2s;

  svg {
    width: 15px;
    height: 15px;
  }

  &:hover {
    background: rgba(201, 87, 69, 0.15);
    color: #C95745;
  }
}

// ── 工作区多栏（左辅 / 主内容 / 右辅）────────────────────────
.workspace-cols {
  flex: 1;
  display: flex;
  flex-direction: row;
  min-width: 0;
  min-height: 0;
  overflow: hidden;
}

.col-splitter {
  width: 6px;
  flex-shrink: 0;
  cursor: col-resize;
  background: transparent;
  align-self: stretch;
  position: relative;
  z-index: 6;
  transition: background 0.15s;
  &:hover {
    background: rgba(0, 212, 255, 0.18);
  }
}

.aux-panel {
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  min-height: 0;
  overflow: hidden;
  background: vars.$layer-2;
  border-right: 1px solid rgba(0, 212, 255, 0.12);
  &.aux-right {
    border-right: none;
    border-left: 1px solid rgba(0, 212, 255, 0.12);
  }
  &.is-fullscreen {
    position: fixed !important;
    inset: 0;
    z-index: 9998;
    width: 100% !important;
    max-width: none !important;
    height: 100vh !important;
    border: none;
  }
}

.aux-panel-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.1);
  flex-shrink: 0;
  min-height: 44px;
}

.aux-tabs {
  display: flex;
  flex: 1;
  gap: 4px;
  overflow-x: auto;
  min-width: 0;
  &::-webkit-scrollbar {
    height: 4px;
  }
}

.aux-tab {
  flex-shrink: 0;
  padding: 6px 10px;
  border-radius: 6px;
  border: 1px solid transparent;
  background: rgba(255, 255, 255, 0.04);
  color: rgba(180, 210, 230, 0.75);
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s;
  &.active {
    border-color: rgba(0, 212, 255, 0.35);
    color: #00d4ff;
    background: rgba(0, 212, 255, 0.1);
  }
  &:hover {
    color: #e8f4ff;
  }
}

.aux-panel-actions {
  display: flex;
  gap: 4px;
  flex-shrink: 0;
}

.aux-icon-btn {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid rgba(0, 212, 255, 0.15);
  background: rgba(0, 212, 255, 0.05);
  color: rgba(180, 210, 230, 0.85);
  cursor: pointer;
  font-size: 13px;
  line-height: 1;
  &:hover {
    background: rgba(0, 212, 255, 0.12);
    color: #00d4ff;
  }
}

.aux-panel-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  padding: 12px 14px;
  font-size: 13px;
  color: rgba(200, 220, 240, 0.85);
}

.aux-placeholder {
  margin: 0 0 8px;
  font-weight: 600;
  color: #e8f4ff;
}

.aux-hint {
  margin: 0;
  font-size: 11px;
  color: rgba(120, 160, 190, 0.75);
  line-height: 1.5;
  word-break: break-all;
}

.workspace-top-tools {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  padding-right: 10px;
  border-right: 1px solid rgba(0, 212, 255, 0.1);
}

.ws-tool-btn {
  padding: 6px 10px;
  border-radius: 6px;
  border: 1px solid rgba(0, 212, 255, 0.15);
  background: rgba(0, 212, 255, 0.06);
  color: rgba(180, 210, 230, 0.85);
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s;
  &:hover {
    border-color: rgba(0, 212, 255, 0.35);
    color: #00d4ff;
  }
  &.active {
    border-color: rgba(0, 212, 255, 0.4);
    color: #00d4ff;
    background: rgba(0, 212, 255, 0.12);
  }
}

// 左栏 / 全屏 / 右栏：与左侧面板 aux-icon-btn 同尺寸（28×28），图标区 13px
.ws-tool-btn--icon {
  width: 28px;
  height: 28px;
  min-width: 28px;
  padding: 0;
  border-radius: 6px;
  box-sizing: border-box;
  display: inline-flex;
  align-items: center;
  justify-content: center;

  .ws-tool-icon svg {
    display: block;
    width: 13px;
    height: 13px;
  }

  .ws-tool-icon--fs {
    font-size: 13px;
    line-height: 1;
  }
}

// ── 主内容区域 ───────────────────────────────────────────────
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-width: 0;
  &.is-fullscreen {
    position: fixed !important;
    inset: 0;
    z-index: 9997;
    width: 100vw !important;
    max-width: none !important;
    height: 100vh !important;
    background: vars.$layer-1;
  }
}

// 主内容区顶条：侧栏开关 + 页签栏同一行（位于全局顶栏之下）
.main-chrome-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 44px;
  height: 44px;
  padding: 0 12px 0 16px;
  background: vars.$layer-2;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
  flex-shrink: 0;
}

// 中间：页签区占满剩余宽度（无页签时也撑开，使「右栏」固定在最右侧）
.main-chrome-center {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  align-self: stretch;
  min-height: 0;
}

.workspace-top-tools--end {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-shrink: 0;
  padding-left: 10px;
  border-left: 1px solid rgba(0, 212, 255, 0.1);
}

// 内容区域
.content-wrapper {
  flex: 1;
  min-width: 0;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
  background: vars.$layer-1;

  &::-webkit-scrollbar {
    width: 8px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.2);
    border-radius: 4px;
  }
}

// 过渡动画
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s, transform 0.2s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateX(-8px);
}

// ===== 账号下拉菜单 =====
.user-dropdown {
  position: relative;
}

.dropdown-chevron {
  width: 14px;
  height: 14px;
  color: rgba(80, 187, 227, 0.6);
  transition: transform 0.2s;
  margin-left: 2px;

  &.open {
    transform: rotate(180deg);
  }
}

.dropdown-menu {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  width: 220px;
  background: #162233;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 10px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4), 0 0 0 1px rgba(0, 212, 255, 0.05);
  z-index: 1000;
  overflow: hidden;
  padding: 6px;
}

.dropdown-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 8px;
}

.dropdown-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: linear-gradient(135deg, #0066FF, #00D4FF);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: 'Orbitron', monospace;
  font-size: 13px;
  font-weight: 700;
  color: #fff;
  flex-shrink: 0;
}

.dropdown-user-info {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.dropdown-username {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  color: #E8F4FF;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.dropdown-email {
  font-size: 11px;
  color: rgba(80, 187, 227, 0.5);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 2px;
}

.dropdown-divider {
  height: 1px;
  background: rgba(0, 212, 255, 0.08);
  margin: 4px 0;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 10px;
  border-radius: 6px;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  color: rgba(180, 210, 230, 0.85);
  background: transparent;
  border: none;
  cursor: pointer;
  text-decoration: none;
  transition: all 0.15s;

  svg {
    width: 15px;
    height: 15px;
    flex-shrink: 0;
  }

  &:hover {
    background: rgba(0, 212, 255, 0.08);
    color: #E8F4FF;
  }

  &.danger {
    color: rgba(201, 87, 69, 0.8);
    &:hover {
      background: rgba(201, 87, 69, 0.1);
      color: #C95745;
    }
  }
}

// 下拉动画
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s, transform 0.15s;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

// ===== Tab Bar（与 .main-chrome-bar 同一行，占满剩余宽度）=====
.tab-bar {
  display: flex;
  align-items: center;
  flex: 1;
  min-width: 0;
  height: 100%;
  min-height: 0;
  background: transparent;
  padding: 0 4px 0 0;
  gap: 2px;
  overflow-x: auto;
  flex-shrink: 1;

  &::-webkit-scrollbar {
    height: 4px;
  }
  &::-webkit-scrollbar-track {
    background: transparent;
  }
  &::-webkit-scrollbar-thumb {
    background: rgba(0, 212, 255, 0.2);
    border-radius: 2px;
  }
}

.tab-overflow-dropdown {
  margin-left: auto;
  flex-shrink: 0;
}

.tab-overflow-trigger {
  height: 28px;
  min-width: 32px;
  padding: 0 10px;
  border-radius: 6px;
  border: 1px solid rgba(0, 212, 255, 0.18);
  background: rgba(255, 255, 255, 0.03);
  color: rgba(180, 210, 230, 0.85);
  cursor: pointer;
  font-size: 14px;
  line-height: 1;
}
.tab-overflow-trigger:hover {
  background: rgba(0, 212, 255, 0.08);
  color: #00d4ff;
}

.tab-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 10px;
  height: 30px;
  border-radius: 6px 6px 0 0;
  cursor: pointer;
  user-select: none;
  flex-shrink: 0;
  border: 1px solid transparent;
  border-bottom: none;
  background: rgba(255, 255, 255, 0.03);
  transition: all 0.15s;

  &:hover {
    background: rgba(0, 212, 255, 0.06);
    border-color: rgba(0, 212, 255, 0.1);
  }

  &.active {
    background: linear-gradient(180deg, rgba(0, 212, 255, 0.12) 0%, rgba(0, 212, 255, 0.05) 100%);
    border-color: rgba(0, 212, 255, 0.25);
    transform: translateY(-1px);
    box-shadow: 0 0 14px rgba(0, 212, 255, 0.15);

    .tab-label {
      color: #00D4FF;
      font-weight: 600;
      text-shadow: 0 0 8px rgba(0, 212, 255, 0.4);
    }
  }
}

.tab-label {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12px;
  color: rgba(180, 210, 230, 0.6);
  white-space: nowrap;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tab-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  border-radius: 3px;
  background: transparent;
  border: none;
  cursor: pointer;
  color: rgba(80, 187, 227, 0.4);
  padding: 0;
  flex-shrink: 0;
  transition: all 0.15s;

  svg {
    width: 10px;
    height: 10px;
  }

  &:hover {
    background: rgba(201, 87, 69, 0.2);
    color: #C95745;
  }
}

.tab-active-bar {
  position: absolute;
  bottom: -1px;
  left: 0;
  right: 0;
  height: 2px;
  background: linear-gradient(90deg, transparent, #00D4FF, transparent);
  box-shadow: 0 0 8px rgba(0, 212, 255, 0.6);
  border-radius: 1px;
}

// ===== 右键菜单 =====
.context-menu {
  position: fixed;
  z-index: 9999;
  background: #162233;
  border: 1px solid rgba(0, 212, 255, 0.2);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
  padding: 4px;
  min-width: 140px;
}

.context-menu-title {
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 11px;
  color: rgba(80, 187, 227, 0.5);
  padding: 6px 10px 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 160px;
}

.context-menu-divider {
  height: 1px;
  background: rgba(0, 212, 255, 0.08);
  margin: 3px 0;
}

.context-menu-item {
  display: block;
  width: 100%;
  padding: 7px 10px;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 12.5px;
  color: rgba(180, 210, 230, 0.85);
  background: transparent;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  text-align: left;
  transition: all 0.15s;

  &:hover:not(:disabled) {
    background: rgba(0, 212, 255, 0.08);
    color: #E8F4FF;
  }

  &:disabled {
    opacity: 0.35;
    cursor: not-allowed;
  }
}
</style>

<!-- 边条模式子菜单浮层 Teleport 到 body，需非 scoped -->
<style lang="scss">
.sidebar-menu-flyout-popper.el-popover,
.sidebar-menu-flyout-popper.el-popper {
  padding: 6px 0 !important;
  background: #001529 !important;
  border: 1px solid rgba(0, 212, 255, 0.18) !important;
  box-shadow: 0 8px 28px rgba(0, 0, 0, 0.5) !important;
  border-radius: 8px !important;
}

.sidebar-menu-flyout-popper .sidebar-menu-flyout-body {
  padding: 0 6px;
  min-width: 0;
}

.sidebar-menu-flyout-popper .sidebar-menu-flyout-body .submenu-item {
  display: block;
  width: 100%;
  padding: 8px 12px 8px 16px;
  border-radius: 4px;
  color: rgba(200, 220, 240, 0.78);
  text-decoration: none;
  font-family: 'Noto Sans SC', sans-serif;
  font-size: 13px;
  transition: background 0.15s, color 0.15s;
  margin-bottom: 2px;
  background: transparent;
  border: none;
  cursor: pointer;
  text-align: left;
  box-sizing: border-box;
}

.sidebar-menu-flyout-popper .sidebar-menu-flyout-body .submenu-item:hover {
  background: rgba(0, 212, 255, 0.06);
  color: #e8f4ff;
}

.sidebar-menu-flyout-popper .sidebar-menu-flyout-body .submenu-item.router-link-active,
.sidebar-menu-flyout-popper .sidebar-menu-flyout-body .submenu-item.active {
  color: #7fdfff;
  background: rgba(0, 212, 255, 0.08);
}
</style>
