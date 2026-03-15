# 在腾讯云服务器启动 FrontCRM 容器

## 快速启动 (3 步)

### 步骤 1: SSH 连接到服务器

```bash
ssh ubuntu@129.226.161.3
```

### 步骤 2: 进入部署目录

```bash
cd /home/ubuntu/frontcrm_deploy
```

### 步骤 3: 启动 Docker 容器

```bash
docker-compose up -d
```

---

## 验证部署

启动后检查容器状态:

```bash
docker-compose ps
```

你应该看到 3 个容器在运行:
- **frontend** - Nginx 前端服务器
- **backend** - ASP.NET Core API 服务
- **postgres** - PostgreSQL 数据库

---

## 访问应用

部署完成后，可以访问:

- **前端应用**: http://129.226.161.3
- **后端 API**: http://129.226.161.3:5000
- **API 端点**: http://129.226.161.3:5000/api/v1

---

## 常用命令

### 查看容器日志

```bash
# 所有容器
docker-compose logs -f

# 仅查看后端日志
docker-compose logs -f backend

# 仅查看前端日志
docker-compose logs -f frontend

# 仅查看数据库日志
docker-compose logs -f postgres
```

### 停止服务

```bash
docker-compose down
```

### 重启服务

```bash
docker-compose restart
```

### 进入容器

```bash
# 进入后端容器
docker exec -it frontcrm_backend bash

# 进入前端容器
docker exec -it frontcrm_frontend sh

# 进入数据库容器
docker exec -it frontcrm_postgres bash
```

---

## 故障排查

### 1. 容器无法启动

查看详细错误信息:

```bash
docker-compose logs
```

### 2. 前端无法连接到后端

检查后端是否运行:

```bash
docker-compose ps backend
curl http://localhost:5000/api/v1/health
```

### 3. 数据库连接失败

确保 PostgreSQL 容器已启动:

```bash
docker-compose ps postgres
```

重新初始化数据库:

```bash
docker-compose down
docker-compose up -d
```

### 4. 端口被占用

更改 `docker-compose.yml` 中的端口:

```bash
nano docker-compose.yml
# 修改端口后保存
docker-compose restart
```

---

## 监控和维护

### 查看容器资源使用

```bash
docker stats
```

### 备份数据库

```bash
docker exec frontcrm_postgres pg_dump -U postgres crm_db > backup.sql
```

### 恢复数据库

```bash
docker exec -i frontcrm_postgres psql -U postgres crm_db < backup.sql
```

---

## 更新应用

当有新版本时:

1. 在本地重新编译
2. 重新上传 `frontcrm_deploy` 文件夹
3. 在服务器重启容器:

```bash
docker-compose down
docker-compose up -d
```

---

**启动就绪！** 🚀

执行以上三个步骤即可在腾讯云香港服务器上运行 FrontCRM 应用。
