# FrontCRM 快速启动指南

## 📋 当前项目状态

### ✅ 后端状态（ASP.NET Core 9.0）
- **编译状态**: ✅ 可以成功构建
- **核心功能**: 已实现
  - ✅ 用户认证（JWT）
  - ✅ 用户注册/登录
  - ✅ 统一 API 响应格式
  - ✅ 全局异常处理
  - ✅ CORS 配置
  - ⏸️ Swagger 文档（已移除以简化启动）

### ✅ 前端状态（Vue 3 + TypeScript）
- **项目结构**: ✅ 已创建
- **核心配置**: ✅ 已完成
  - ✅ Vite 配置
  - ✅ Vue Router 路由
  - ✅ Pinia 状态管理
  - ✅ Axios HTTP 客户端
  - ✅ Element Plus UI
  - ⏸️ 依赖安装（需要运行 npm install）

## 🚀 快速启动步骤

### 1️⃣ 启动 PostgreSQL 数据库

#### 方式 A：使用 Docker（推荐）
```bash
docker run -d \
  --name frontcrm-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres123 \
  -e POSTGRES_DB=FrontCRM \
  -p 5432:5432 \
  postgres:15-alpine
```

#### 方式 B：本地 PostgreSQL
确保 PostgreSQL 服务已运行，并创建数据库：
```sql
CREATE DATABASE FrontCRM;
```

### 2️⃣ 启动后端 API

```bash
# 进入 API 目录
cd d:/MyProject/FrontCRM/CRM.API

# 启动 API
dotnet run --urls "http://localhost:5000"

# API 将运行在 http://localhost:5000
```

**测试 API 端点：**
```bash
# 健康检查
curl http://localhost:5000/api/v1/health

# 用户注册
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","email":"admin@example.com","password":"123456"}'

# 用户登录
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"123456"}'
```

### 3️⃣ 启动前端应用

```bash
# 进入前端目录
cd d:/MyProject/FrontCRM/CRM.Web

# 安装依赖（首次运行需要）
npm install

# 启动开发服务器
npm run dev

# 前端将运行在 http://localhost:5173
```

### 4️⃣ 访问应用

打开浏览器访问：
- **前端应用**: http://localhost:5173
- **后端 API**: http://localhost:5000

## 📦 使用 Docker Compose 一键启动

```bash
cd d:/MyProject/FrontCRM
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止所有服务
docker-compose down
```

## ⚠️ 常见问题

### Q1: 数据库连接失败
**错误**: `Npgsql.NpgsqlException: The authentication type 10 is not supported`

**解决方案**:
- 确认 PostgreSQL 版本兼容性
- 检查连接字符串中的用户名和密码
- 确认数据库已创建

### Q2: 前端无法连接后端
**错误**: `Network Error` 或 `CORS policy`

**解决方案**:
- 确认后端 API 已启动
- 检查 `.env` 文件中的 `VITE_API_BASE_URL` 配置
- 后端 CORS 已配置允许所有来源

### Q3: npm install 失败
**错误**: 依赖安装超时或失败

**解决方案**:
```bash
# 清理缓存
npm cache clean --force

# 使用国内镜像
npm config set registry https://registry.npmmirror.com

# 重新安装
npm install
```

### Q4: 前端构建失败
**错误**: TypeScript 编译错误

**解决方案**:
```bash
# 检查 Node.js 版本（需要 18+）
node --version

# 清理 node_modules 重新安装
rm -rf node_modules package-lock.json
npm install
```

## 🎯 功能测试清单

### 用户认证功能
- [ ] 访问首页 http://localhost:5173
- [ ] 注册新用户
- [ ] 登录系统
- [ ] 查看 Dashboard（需要登录）
- [ ] 退出登录

### API 功能
- [ ] 健康检查: `GET /api/v1/health`
- [ ] 用户注册: `POST /api/v1/auth/register`
- [ ] 用户登录: `POST /api/v1/auth/login`
- [ ] 获取用户信息: `GET /api/v1/auth/me`（需要 Token）

## 📝 下一步开发建议

1. **添加 Swagger 文档**（可选）
2. **实现客户管理模块**
3. **添加数据可视化**
4. **实现权限控制**
5. **添加文件上传功能**
6. **优化 UI/UX**

## 🔧 技术支持

如遇到问题，请检查：
1. 控制台错误日志
2. 浏览器开发者工具
3. 网络连接状态
4. 服务端口占用情况

---

**项目状态**: ✅ 后端可运行，⏸️ 前端需安装依赖
**最后更新**: 2026-03-03
