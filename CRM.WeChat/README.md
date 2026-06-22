# FrontCRM 微信端 (uni-app)

基于 uni-app 构建，同时支持 **公众号H5** 和 **微信小程序**。

## 技术栈

- **框架**: uni-app 3.x (Vue 3 + Vite)
- **语言**: TypeScript
- **状态管理**: Pinia
- **共享后端**: ASP.NET Core API (`/api/v1/`)

## 快速开始

```bash
# 安装依赖
npm install

# H5 开发
npm run dev:h5

# 微信小程序开发
npm run dev:mp-weixin
```

## 构建发布

### H5 公众号
```bash
npm run build:h5
# 产物在 dist/build/h5/
# 部署到 Nginx 或静态文件服务器
```

### 微信小程序
```bash
npm run build:mp-weixin
# 产物在 dist/build/mp-weixin/
# 用微信开发者工具打开该目录，上传审核
```

## 项目结构

```
CRM.WeChat/
├── src/
│   ├── api/              # API 接口层（共享后端 /api/v1/）
│   │   ├── client.ts     # 请求客户端（基于 uni.request）
│   │   ├── auth.ts       # 认证 API
│   │   ├── customer.ts   # 客户 API
│   │   ├── order.ts      # 订单 API
│   │   ├── inventory.ts  # 库存 API
│   │   └── index.ts      # 统一导出
│   ├── stores/           # Pinia 状态管理
│   │   └── auth.ts       # 认证状态
│   ├── utils/            # 工具函数
│   ├── pages/            # 页面
│   │   ├── login/        # 登录
│   │   ├── index/        # 首页
│   │   ├── customer/     # 客户管理
│   │   ├── order/        # 订单管理
│   │   ├── inventory/    # 库存查询
│   │   └── mine/         # 个人中心
│   ├── static/           # 静态资源
│   ├── App.vue           # 应用入口
│   ├── main.ts           # 主入口
│   ├── pages.json        # 页面配置
│   └── manifest.json     # 应用配置
├── vite.config.ts        # Vite 配置
├── tsconfig.json         # TypeScript 配置
└── project.config.json   # 小程序项目配置
```

## 功能范围（核心功能）

| 模块 | 功能 | 状态 |
|------|------|------|
| 认证 | 用户名密码登录、微信授权登录、退出 | ✅ |
| 首页 | 快捷入口、最近拣货单 | ✅ |
| 客户 | 列表搜索、详情查看 | ✅ |
| 订单 | 销售/采购订单列表、详情 | ✅ |
| 库存 | 库存概览、搜索 | ✅ |
| 个人 | 用户信息、退出登录 | ✅ |

## 后端 API 配置

### H5 开发环境
通过 Vite 代理转发到本地后端：
```
/api -> http://localhost:5000
```

### 小程序 / 生产环境
需要配置后端服务器地址：
1. 修改 `src/api/client.ts` 中的 `PROD_API_BASE`
2. 小程序需在微信公众平台配置服务器域名白名单

## 发布流程

### 1. H5 公众号发布
```bash
# 构建
npm run build:h5

# 上传到服务器
scp -r dist/build/h5/* root@43.129.212.65:/var/www/wechat/

# Nginx 配置示例
# location /wechat/ {
#   alias /var/www/wechat/;
#   try_files $uri $uri/ /wechat/index.html;
# }
```

### 2. 微信小程序发布
```bash
# 构建
npm run build:mp-weixin

# 1. 打开微信开发者工具
# 2. 导入项目 → 选择 dist/build/mp-weixin/
# 3. 填写 AppID
# 4. 上传代码 → 提交审核 → 发布
```

## 与 Web 端的关系

```
FrontCRM_CSharp/
├── CRM.Web/          # Web端 (Vue 3 + Element Plus)
├── CRM.WeChat/       # 微信端 (uni-app) ← 本项目
├── CRM.API/          # 后端 API（共享）
├── CRM.Core/         # 核心业务（共享）
└── CRM.Infrastructure/ # 基础设施（共享）
```

两端共享同一套后端 API (`/api/v1/`)，保证数据一致性。
