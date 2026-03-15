# FrontCRM 部署就绪

## 编译完成 ✓

本地编译已成功完成！

### 编译信息
- **前端**: Vue 3 + TypeScript + Vite (dist 已生成)
- **后端**: ASP.NET Core 9.0 (publish 已生成)
- **部署包**: `frontcrm_deploy/` 文件夹
- **包大小**: ~12 MB
- **编译时间**: ~10 秒

### 部署包内容

```
frontcrm_deploy/
├── CRM.API/
│   ├── CRM.API.dll (后端主程序)
│   ├── *.dll (依赖程序集)
│   ├── *.json (配置文件)
│   └── ...
├── CRM.Web/
│   ├── dist/ (前端编译后的静态文件)
│   ├── Dockerfile (前端容器定义)
│   └── nginx.conf (Nginx 配置)
├── docker-compose.yml (容器编排)
└── Dockerfile.backend (后端容器定义)
```

## 上传到腾讯云香港服务器

### 方式1: 使用 SCP 上传 (推荐)

```powershell
# Windows PowerShell 中执行 (需要安装 OpenSSH)
.\deploy_to_server.ps1

# 或手动上传
scp -r frontcrm_deploy ubuntu@129.226.161.3:/home/ubuntu/
```

### 方式2: 使用 WinSCP 或 FileZilla
1. 连接到服务器: `129.226.161.3`
2. 用户: `ubuntu`
3. 上传文件夹: `frontcrm_deploy` 到 `/home/ubuntu/`

## 在服务器上部署

### 步骤1: SSH 连接到服务器

```bash
ssh ubuntu@129.226.161.3
```

### 步骤2: 进入部署目录

```bash
cd /home/ubuntu/frontcrm_deploy
```

### 步骤3: 启动 Docker 容器

```bash
# 启动容器
docker-compose up -d

# 查看容器状态
docker-compose ps

# 查看日志
docker-compose logs -f
```

### 步骤4: 验证部署

```bash
# 检查容器是否运行
docker ps

# 检查网络连接
curl http://localhost
curl http://localhost:5000/api/v1/health
```

## 访问应用

部署完成后，可以通过以下地址访问:

- **前端应用**: http://129.226.161.3
- **后端API**: http://129.226.161.3:5000
- **API 文档**: http://129.226.161.3:5000/swagger (如果启用了 Swagger)

## 数据库配置

### PostgreSQL 初始化

容器启动后，PostgreSQL 数据库会自动初始化。

**数据库信息:**
- 主机: postgres (Docker 内部网络)
- 端口: 5432
- 用户: postgres
- 密码: 在 `docker-compose.yml` 中配置
- 数据库: crm_db

### 连接字符串

后端已配置的连接字符串 (在 `appsettings.json` 中):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=postgres;Port=5432;Database=crm_db;User Id=postgres;Password=your_password;"
}
```

## 常用命令

```bash
# 查看所有容器日志
docker-compose logs -f

# 查看特定容器日志
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres

# 停止服务
docker-compose down

# 重启服务
docker-compose restart

# 进入容器终端
docker exec -it frontcrm_backend /bin/bash
docker exec -it frontcrm_frontend /bin/sh

# 查看容器 IP
docker inspect frontcrm_backend
```

## 故障排查

### 容器无法启动

```bash
# 查看详细日志
docker-compose logs

# 检查文件权限
ls -la /home/ubuntu/frontcrm_deploy/
```

### 前端无法连接到后端

检查 `CRM.Web/nginx.conf` 中的后端 API 地址配置:

```nginx
proxy_pass http://backend:5000;
```

### 数据库连接失败

确保 PostgreSQL 容器已启动:

```bash
docker-compose ps postgres
```

## 更新部署

当有新的代码更新时:

1. 在本地运行:
   ```powershell
   .\build_frontend_temp.ps1  # 编译前端
   .\build_simple.ps1         # 创建部署包
   ```

2. 重新上传 `frontcrm_deploy` 文件夹到服务器

3. 在服务器重启容器:
   ```bash
   docker-compose down
   docker-compose up -d
   ```

## 备份和恢复

### 备份数据库

```bash
# 在服务器执行
docker exec frontcrm_postgres pg_dump -U postgres crm_db > backup.sql
```

### 恢复数据库

```bash
docker exec -i frontcrm_postgres psql -U postgres crm_db < backup.sql
```

## 安全性建议

1. **更改默认密码**: 在 `docker-compose.yml` 中更改 PostgreSQL 密码
2. **配置 HTTPS**: 为 Nginx 配置 SSL 证书
3. **设置防火墙**: 仅暴露必要的端口 (80, 443, 5000)
4. **定期备份**: 自动备份数据库

---

**部署状态**: ✓ 就绪

**下一步**: 运行 `.\deploy_to_server.ps1` 上传到服务器
