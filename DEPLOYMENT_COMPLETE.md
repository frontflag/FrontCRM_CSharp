# FrontCRM 部署完成！🎉

## 部署状态: ✅ 完成

所有文件已成功上传到腾讯云香港服务器 (129.226.161.3)

---

## 部署摘要

| 项目 | 详情 |
|-----|------|
| **服务器位置** | 香港 (ap-hongkong) |
| **服务器地址** | 129.226.161.3 |
| **用户** | ubuntu |
| **部署包** | frontcrm_deploy/ (11.96 MB) |
| **编译方式** | 本地编译 + 远程部署 |
| **容器技术** | Docker Compose |
| **编译时间** | ~15 秒 |
| **上传大小** | ~12.5 MB |
| **上传速度** | 1-3 MB/s |

---

## 已上传的文件结构

```
/home/ubuntu/frontcrm_deploy/
├── CRM.API/publish/         # 后端编译文件
│   ├── CRM.API.dll          # 主程序
│   ├── CRM.Core.dll         # 核心库
│   ├── CRM.Infrastructure.dll # 基础设施库
│   ├── *.dll                # 所有依赖
│   ├── appsettings.json     # 配置文件
│   └── ...
├── CRM.Web/dist/            # 前端编译文件
│   ├── index.html           # 入口页面
│   ├── assets/              # 静态资源
│   │   ├── *.js             # JavaScript 文件
│   │   ├── *.css            # 样式文件
│   │   └── ...
│   ├── Dockerfile           # 前端容器定义
│   └── nginx.conf           # Nginx 配置
├── Dockerfile.backend        # 后端容器定义
└── docker-compose.yml        # 容器编排文件
```

---

## 下一步: 启动容器

### 方式1: 使用 SSH (推荐)

```bash
# 1. 连接到服务器
ssh ubuntu@129.226.161.3

# 2. 进入部署目录
cd /home/ubuntu/frontcrm_deploy

# 3. 启动容器
docker-compose up -d

# 4. 验证状态
docker-compose ps
```

### 方式2: 使用本地 PowerShell

```powershell
# 直接执行启动命令
ssh ubuntu@129.226.161.3 "cd /home/ubuntu/frontcrm_deploy && docker-compose up -d"

# 查看状态
ssh ubuntu@129.226.161.3 "cd /home/ubuntu/frontcrm_deploy && docker-compose ps"
```

---

## 容器服务

启动后将运行以下 3 个容器:

### 1. Frontend (Nginx)
- **端口**: 80
- **功能**: Vue 3 前端应用
- **访问**: http://129.226.161.3

### 2. Backend (ASP.NET Core)
- **端口**: 5000
- **功能**: RESTful API 服务
- **访问**: http://129.226.161.3:5000

### 3. Database (PostgreSQL)
- **端口**: 5432 (仅内部)
- **数据库**: crm_db
- **用户**: postgres

---

## 应用访问

### 首次访问

```
Frontend: http://129.226.161.3
Backend:  http://129.226.161.3:5000
```

### 登录凭证

- 需要在数据库中创建用户
- 或使用 API 的注册端点创建用户

### API 端点示例

```
POST   /api/v1/auth/login          # 登录
POST   /api/v1/auth/register       # 注册
GET    /api/v1/customers           # 获取客户列表
POST   /api/v1/customers           # 创建客户
PUT    /api/v1/customers/{id}      # 更新客户
DELETE /api/v1/customers/{id}      # 删除客户
```

---

## 监控和日志

### 查看容器日志

```bash
# SSH 连接后
cd /home/ubuntu/frontcrm_deploy

# 所有容器
docker-compose logs -f

# 特定容器
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres
```

### 容器状态检查

```bash
docker-compose ps
docker stats
```

---

## 数据库初始化

### 连接到数据库

```bash
# 进入 PostgreSQL 容器
docker exec -it frontcrm_postgres psql -U postgres

# 连接到数据库
\c crm_db

# 列出表
\dt
```

### 创建测试用户

```sql
-- 在 PostgreSQL 中执行
INSERT INTO users (username, email, password_hash) 
VALUES ('admin', 'admin@example.com', 'hashed_password');
```

---

## 常用操作

### 停止服务

```bash
docker-compose down
```

### 重启服务

```bash
docker-compose restart
```

### 查看特定容器日志

```bash
docker-compose logs -f backend --tail 100
```

### 进入容器终端

```bash
docker exec -it frontcrm_backend bash
```

### 查看容器 IP

```bash
docker inspect frontcrm_backend | grep IPAddress
```

---

## 故障排查

### 容器启动失败

1. 检查 Docker 是否安装

```bash
docker --version
docker-compose --version
```

2. 查看日志

```bash
docker-compose logs
```

3. 检查端口是否被占用

```bash
netstat -tuln | grep -E '80|5000|5432'
```

### 前端无法连接到后端

1. 检查后端容器是否运行

```bash
docker-compose ps backend
```

2. 测试 API 连接

```bash
curl http://localhost:5000/api/v1
```

3. 检查 nginx.conf 中的后端地址

```bash
cat CRM.Web/nginx.conf | grep proxy_pass
```

### 数据库连接错误

1. 检查 PostgreSQL 容器

```bash
docker-compose ps postgres
```

2. 查看数据库日志

```bash
docker-compose logs postgres
```

3. 检查连接字符串

```bash
grep "Connection" CRM.API/publish/appsettings.json
```

---

## 备份和恢复

### 备份数据库

```bash
docker exec frontcrm_postgres pg_dump -U postgres crm_db > backup.sql
```

### 恢复数据库

```bash
docker exec -i frontcrm_postgres psql -U postgres crm_db < backup.sql
```

### 备份整个应用

```bash
tar -czf frontcrm_backup.tar.gz /home/ubuntu/frontcrm_deploy/
```

---

## 更新应用

当有新的代码更新时:

1. **本地重新编译**
   ```powershell
   cd d:\MyProject\FrontCRM_CSharp
   .\build_frontend_temp.ps1    # 编译前端
   .\build_simple.ps1           # 生成部署包
   ```

2. **上传新包**
   ```powershell
   .\deploy_to_server.ps1
   ```

3. **服务器重启容器**
   ```bash
   cd /home/ubuntu/frontcrm_deploy
   docker-compose down
   docker-compose up -d
   ```

---

## 性能调优

### 增加后端容器资源

编辑 `docker-compose.yml`:

```yaml
backend:
  services:
    limits:
      cpus: '2'
      memory: 2G
    reservations:
      cpus: '1'
      memory: 1G
```

### 数据库性能

```sql
-- 创建索引
CREATE INDEX idx_customer_email ON customers(email);
CREATE INDEX idx_customer_created ON customers(created_at);
```

---

## 安全建议

### 1. 更改数据库密码

编辑 `docker-compose.yml` 中的 `POSTGRES_PASSWORD`

### 2. 配置防火墙

```bash
# 仅允许必要端口
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP
sudo ufw allow 443/tcp  # HTTPS (如果启用)
sudo ufw enable
```

### 3. 启用 HTTPS

使用 Let's Encrypt 配置 SSL 证书

### 4. 定期备份

```bash
# 添加到 crontab
0 2 * * * docker exec frontcrm_postgres pg_dump -U postgres crm_db > /backup/db_$(date +\%Y\%m\%d).sql
```

---

## 技术栈

| 组件 | 版本 | 技术 |
|-----|------|------|
| **前端** | 1.0.0 | Vue 3.5.13, TypeScript, Vite, Element Plus |
| **后端** | 1.0.0 | ASP.NET Core 9.0, Entity Framework Core 9.0.11 |
| **数据库** | 15 | PostgreSQL 15 |
| **容器** | - | Docker & Docker Compose |
| **Web服务器** | 最新 | Nginx |
| **认证** | - | JWT Bearer Tokens |

---

## 支持

如有问题，请:

1. 查看日志: `docker-compose logs`
2. 检查配置: `cat docker-compose.yml`
3. 测试连接: `curl http://localhost:5000`
4. 查看详细说明: `START_CONTAINERS.md`

---

**部署状态**: ✅ 完成并就绪

**下一步**: SSH 到服务器并执行 `docker-compose up -d`

🚀 Happy Deploying!
