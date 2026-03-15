---
name: 任务级进度追踪功能实现
overview: 为FrontCRM系统开发完整的任务级进度追踪模块，包括看板视图、任务管理、进度统计等功能。
design:
  architecture:
    framework: vue
  styleKeywords:
    - Modern
    - Clean
    - Card-based
    - Drag-and-drop
    - Progressive
  fontSystem:
    fontFamily: PingFang SC
    heading:
      size: 20px
      weight: 600
    subheading:
      size: 16px
      weight: 500
    body:
      size: 14px
      weight: 400
  colorSystem:
    primary:
      - "#667eea"
      - "#409EFF"
    background:
      - "#f5f7fa"
      - "#ffffff"
    text:
      - "#303133"
      - "#606266"
      - "#909399"
    functional:
      - "#F56C6C"
      - "#E6A23C"
      - "#67C23A"
      - "#909399"
todos:
  - id: install-deps
    content: 安装vue-draggable-next依赖包
    status: completed
  - id: create-types
    content: 创建任务相关TypeScript类型定义
    status: completed
    dependencies:
      - install-deps
  - id: create-task-store
    content: 创建TaskStore状态管理模块
    status: completed
    dependencies:
      - create-types
  - id: create-task-api
    content: 创建任务API接口层（预留后端对接）
    status: completed
    dependencies:
      - create-types
  - id: create-task-board
    content: 开发看板视图组件TaskBoard.vue
    status: completed
    dependencies:
      - create-task-store
  - id: create-task-list
    content: 开发列表视图组件TaskList.vue
    status: completed
    dependencies:
      - create-task-store
  - id: create-task-card
    content: 开发任务卡片组件TaskCard.vue
    status: completed
    dependencies:
      - create-task-board
  - id: create-task-detail
    content: 开发任务详情弹窗组件TaskDetail.vue
    status: completed
    dependencies:
      - create-task-card
  - id: create-tasks-view
    content: 开发任务管理主页面TasksView.vue
    status: completed
    dependencies:
      - create-task-board
      - create-task-list
  - id: update-routes
    content: 添加任务管理路由配置
    status: completed
    dependencies:
      - create-tasks-view
  - id: update-sidebar
    content: 在侧边栏添加任务管理菜单项
    status: completed
    dependencies:
      - update-routes
  - id: update-dashboard
    content: 在Dashboard首页添加任务统计卡片
    status: completed
    dependencies:
      - update-sidebar
---

## 产品概述

为FrontCRM系统开发任务级进度追踪模块，帮助用户管理开发项目。该模块将集成到现有的CRM系统中，提供直观的任务管理和进度可视化功能。

## 核心功能

1. **任务看板视图**：拖拽式看板，支持任务状态流转（待办/进行中/已完成/已归档）
2. **任务列表管理**：表格形式展示所有任务，支持增删改查
3. **子任务支持**：大任务可分解为多个子任务，子任务完成度自动计算父任务进度
4. **进度可视化**：任务进度条、项目整体统计图表
5. **任务属性管理**：优先级（高/中/低）、截止日期、负责人、标签分类
6. **筛选与搜索**：按状态、优先级、负责人、标签筛选，关键词搜索
7. **任务详情弹窗**：查看和编辑任务完整信息，管理子任务

## 技术栈

- **前端框架**：Vue 3.5.13 + TypeScript（与现有项目一致）
- **UI组件库**：Element Plus 2.9.1（与现有项目一致）
- **状态管理**：Pinia 2.3.0（与现有项目一致）
- **路由**：Vue Router 4.5.0（与现有项目一致）
- **拖拽库**：vue-draggable-next（Vue 3兼容的拖拽方案）
- **图表库**：Element Plus内置统计组件 + ECharts（按需引入）

## 实现方案

### 架构设计

采用与现有项目一致的组件化架构：

- **Store层**：TaskStore管理任务状态，支持本地持久化（localStorage）
- **API层**：task.ts定义任务相关接口（预留后端对接）
- **视图层**：TasksView.vue作为入口，内嵌看板和列表两种视图
- **组件层**：TaskBoard.vue（看板）、TaskList.vue（列表）、TaskCard.vue（卡片）、TaskDetail.vue（详情弹窗）

### 数据结构

```typescript
interface Task {
  id: string
  title: string
  description: string
  status: 'todo' | 'in-progress' | 'done' | 'archived'
  priority: 'high' | 'medium' | 'low'
  assignee: string
  dueDate: string
  tags: string[]
  progress: number // 0-100，根据子任务自动计算
  subtasks: SubTask[]
  createdAt: string
  updatedAt: string
}

interface SubTask {
  id: string
  title: string
  completed: boolean
}
```

### 关键实现细节

1. **看板拖拽**：使用vue-draggable-next实现跨列拖拽，拖拽后自动更新任务状态
2. **进度计算**：父任务进度 = 已完成子任务数 / 总子任务数 * 100%
3. **本地存储**：使用localStorage持久化任务数据，刷新页面不丢失
4. **响应式设计**：适配不同屏幕尺寸，看板在移动端可横向滚动

## 设计风格

采用与现有CRM系统一致的现代化简洁风格，使用Element Plus组件库保持视觉统一。

### 页面结构

1. **左侧导航栏**：新增"任务管理"菜单项，与现有"客户管理"并列
2. **顶部工具栏**：视图切换按钮（看板/列表）、新建任务按钮、筛选器
3. **主内容区**：

- 看板视图：四列布局（待办/进行中/已完成/已归档），每列可垂直滚动
- 列表视图：Element Plus表格，支持排序和分页

4. **任务卡片**：显示标题、优先级标签、进度条、截止日期、负责人头像
5. **详情弹窗**：表单编辑区 + 子任务列表 + 操作按钮

### 交互设计

- 拖拽任务卡片到不同列自动更新状态
- 点击卡片打开详情弹窗
- 进度条实时反映子任务完成情况
- 优先级用颜色区分（高-红色、中-橙色、低-绿色）

## Agent Extensions

### SubAgent

- **code-explorer**
- Purpose: 探索项目现有代码结构和模式，确保新功能与现有架构保持一致
- Expected outcome: 提供项目架构分析报告，确认状态管理、API调用、组件编写等最佳实践