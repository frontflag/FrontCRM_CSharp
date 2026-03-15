# FrontCRM System

一个基于 Vue 3 + .NET 8 的现代客户关系管理系统。

## 项目结构

```
FrontCRM/
├── CRM.API/                    # 后端 Web API 项目
│   ├── Controllers/            # API 控制器
│   ├── Services/               # 业务服务层
│   ├── Models/                 # 数据模型和DTOs
│   ├── Middlewares/            # 中间件
│   ├── Filters/                # 过滤器
│   ├── Extensions/             # 扩展方法
│   ├── Data/                   # 数据库相关
│   └── Program.cs              # 程序入口
├── CRM.Core/                   # 核心业务逻辑层
│   ├── Models/                 # 领域模型
│   ├── Interfaces/             # 接口定义
│   └── Constants/              # 常量定义
├── CRM.Infrastructure/         # 基础设施层
│   ├── Data/                   # EF Core 数据库上下文
│   ├── Services/               # 外部服务集成
│   ├── Repositories/           # 仓储实现
│   └── Extensions/             # 服务扩展
├── CRM.Web/                    # 前端 Vue 3 项目
│   ├── src/
│   │   ├── api/                # API 客户端
│   │   ├── assets/             # 静态资源
│   │   ├── components/         # Vue 组件
│   │   ├── views/              # 页面视图
│   │   ├── router/             # 路由配置
│   │   ├── stores/             # Pinia 状态管理
│   │   ├── utils/              # 工具函数
│   │   └── types/              # TypeScript 类型定义
│   ├── package.json
│   └── vite.config.ts
├── docker-compose.yml          # Docker Compose 配置
├── Dockerfile.backend          # 后端 Dockerfile
└── README.md                   # 项目说明文档
```

## 技术栈

### 后端
- **框架**: ASP.NET Core 8.0
- **数据库**: PostgreSQL 15
- **ORM**: Entity Framework Core 9.0
- **认证**: JWT Bearer Token
- **API文档**: Swagger/OpenAPI
- **日志**: Serilog
- **密码加密**: BCrypt

### 前端
- **框架**: Vue 3 + Composition API
- **构建工具**: Vite 6.0
- **语言**: TypeScript 5.7
- **UI框架**: Element Plus
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **HTTP客户端**: Axios
- **CSS预处理器**: Sass

## 快速开始

### 前置要求
- .NET 9.0 SDK
- Node.js 18+
- PostgreSQL 15+
- npm 或 yarn

### 数据库配置

1. 创建 PostgreSQL 数据库:
```sql
CREATE DATABASE FrontCRM;
```

2. 更新连接字符串:

在 `CRM.API/appsettings.json` 中配置数据库连接:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=your_password"
  }
}
```

### 后端启动

```bash
# 1. 还原依赖
cd CRM.API
dotnet restore

# 2. 运行数据库迁移（首次运行）
dotnet ef database update

# 3. 启动 API
dotnet run

# API 将运行在 http://localhost:5000
# Swagger 文档: http://localhost:5000/swagger
```

### 前端启动

```bash
# 1. 进入前端目录
cd CRM.Web

# 2. 安装依赖
npm install

# 3. 启动开发服务器
npm run dev

# 前端将运行在 http://localhost:5173
```

## Docker 部署

使用 Docker Compose 一键启动所有服务:

```bash
# 启动所有服务
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止所有服务
docker-compose down
```

服务将运行在:
- 后端 API: http://localhost:5000
- 前端应用: http://localhost:80
- PostgreSQL: localhost:5432

## API 端点

### 认证相关
- `POST /api/v1/auth/register` - 用户注册
- `POST /api/v1/auth/login` - 用户登录
- `GET /api/v1/auth/me` - 获取当前用户信息

### 健康检查
- `GET /api/v1/health` - 健康检查

## 环境变量

### 后端环境变量 (.env)
```
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=your_password
```

### 前端环境变量 (.env)
```
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

## 开发指南

### 添加新的 API 端点

1. 在 `CRM.Core/Models/` 中定义领域模型
2. 在 `CRM.API/Models/DTOs/` 中创建 DTOs
3. 在 `CRM.API/Controllers/` 中创建控制器
4. 在 `CRM.API/Services/` 中实现业务逻辑
5. 在 `CRM.Web/src/api/` 中添加 API 客户端方法

### 添加数据库表

1. 在 `CRM.Core/Models/` 中创建实体类
2. 在 `CRM.Infrastructure/Data/ApplicationDbContext.cs` 中添加 DbSet
3. 配置实体映射关系
4. 运行数据库迁移

## 项目特点

- ✅ 前后端分离架构
- ✅ RESTful API 设计
- ✅ JWT 用户认证
- ✅ 统一响应格式
- ✅ 全局异常处理
- ✅ CORS 跨域支持
- ✅ Swagger API 文档
- ✅ Pinia 状态管理
- ✅ Vue Router 路由守卫
- ✅ Element Plus UI 组件
- ✅ Docker 容器化部署
- ✅ TypeScript 类型安全

## 常见问题

### 数据库连接失败
- 检查 PostgreSQL 服务是否运行
- 验证连接字符串配置是否正确
- 确保数据库已创建

### 前端无法连接后端
- 确认后端 API 正常运行
- 检查 CORS 配置
- 验证环境变量配置

### 迁移失败
- 确保已安装 Entity Framework Core 工具:
```bash
dotnet tool install --global dotnet-ef
```

## 许可证

MIT License

## 联系方式

如有问题，请提交 Issue。
