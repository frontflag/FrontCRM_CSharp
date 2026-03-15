# FrontCRM Ubuntu 服务器部署指南

本文档详细介绍如何在 Ubuntu 服务器上部署 FrontCRM 项目。

## 目录

- [前置要求](#前置要求)
- [第一步：连接服务器](#第一步连接服务器)
- [第二步：安装 Docker](#第二步安装-docker)
- [第三步：上传项目](#第三步上传项目)
- [第四步：配置生产环境](#第四步配置生产环境)
- [第五步：构建和启动](#第五步构建和启动)
- [第六步：配置防火墙](#第六步配置防火墙)
- [第七步：验证部署](#第七步验证部署)
- [第八步：配置 SSL](#第八步配置-ssl-可选)
- [第九步：自动备份](#第九步自动备份)
- [常用管理命令](#常用管理命令)
- [故障排查](#故障排查)

---

## 前置要求

- Ubuntu 服务器（推荐 22.04 LTS）
- 最低配置：2核 CPU、4GB 内存、20GB 硬盘
- SSH 访问权限
- 具有 sudo 权限的用户账户

---

## 第一步：连接服务器

### 使用 SSH 连接

```bash
# 基本连接
ssh username@your-server-ip

# 示例
ssh root@192.168.1.100

# 使用 SSH 密钥
ssh -i /path/to/key.pem user@server-ip

# 指定端口
ssh -p 2222 user@server-ip
```

### 生成 SSH 密钥（如果需要）

```bash
# 在本地生成密钥对
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"

# 复制公钥到服务器
ssh-copy-id user@server-ip

# 或手动复制
cat ~/.ssh/id_rsa.pub | ssh user@server-ip "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

---

## 第二步：安装 Docker

### 安装 Docker Engine

```bash
# 1. 更新系统
sudo apt-get update && sudo apt-get upgrade -y

# 2. 安装必要的包
sudo apt-get install -y curl git ca-certificates gnupg lsb-release

# 3. 添加 Docker 官方 GPG 密钥
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# 4. 设置 Docker 仓库
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# 5. 安装 Docker
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# 6. 启动 Docker
sudo systemctl start docker
sudo systemctl enable docker

# 7. 验证安装
docker --version
docker compose version
```

### 配置用户权限（可选）

避免每次使用 sudo：

```bash
# 将当前用户添加到 docker 组
sudo usermod -aG docker $USER

# 重新登录或执行
newgrp docker

# 验证
docker ps
```

---

## 第三步：上传项目

### 方式 A：使用 Git（推荐）

如果项目已推送到 Git 仓库：

```bash
# 1. 创建项目目录
sudo mkdir -p /opt/FrontCRM
sudo chown $USER:$USER /opt/FrontCRM

# 2. 克隆项目
cd /opt/FrontCRM
git clone <你的仓库地址> .

# 3. 查看文件
ls -la
```

### 方式 B：使用 SCP 上传（本地执行）

在本地 Windows PowerShell 或 CMD 中执行：

```bash
# 上传整个项目
scp -r d:\MyProject\FrontCRM\* username@your-server-ip:/opt/FrontCRM

# 示例
scp -r d:\MyProject\FrontCRM\* root@192.168.1.100:/opt/FrontCRM
```

### 方式 C：使用 SFTP 工具

使用 FileZilla、WinSCP 等工具：
- 服务器地址：your-server-ip
- 端口：22
- 用户名：your-username
- 密码：your-password
- 远程目录：/opt/FrontCRM

上传所有项目文件。

---

## 第四步：配置生产环境

### 修改 docker-compose.yml

```bash
cd /opt/FrontCRM
nano docker-compose.yml
```

#### 生产环境配置示例

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: frontcrm-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: your_secure_password_here  # ⚠️ 修改为强密码
      POSTGRES_DB: FrontCRM
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./db-init:/docker-entrypoint-initdb.d  # 初始化脚本目录
    restart: always
    networks:
      - frontcrm-network

  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    container_name: frontcrm-backend
    environment:
      ASPNETCORE_ENVIRONMENT: Production  # ⚠️ 生产环境
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=FrontCRM;Username=postgres;Password=your_secure_password_here
    ports:
      - "5000:5000"
    depends_on:
      - postgres
    restart: always
    networks:
      - frontcrm-network
    volumes:
      - ./backend-logs:/app/Logs  # ⚠️ 日志持久化

  frontend:
    build:
      context: CRM.Web
      dockerfile: Dockerfile
    container_name: frontcrm-frontend
    ports:
      - "80:80"
    depends_on:
      - backend
    restart: always
    networks:
      - frontcrm-network

volumes:
  postgres_data:

networks:
  frontcrm-network:
    driver: bridge
```

### 创建必要的目录

```bash
# 创建日志目录
mkdir -p backend-logs
chmod 755 backend-logs

# 创建数据库初始化脚本目录（可选）
mkdir -p db-init
```

### 修改 appsettings.json（可选）

如果需要覆盖生产环境配置：

```bash
# 创建生产环境配置文件
cp CRM.API/appsettings.json CRM.API/appsettings.Production.json
nano CRM.API/appsettings.Production.json
```

---

## 第五步：构建和启动

### 构建镜像

```bash
# 1. 进入项目目录
cd /opt/FrontCRM

# 2. 构建所有镜像（首次运行，会花几分钟）
docker compose build

# 输出示例：
# [+] Building 245.3s (45/45) FINISHED
# => => exporting to image                    45.2s
# => => naming to docker.io/library/xxx     0.1s
```

### 启动服务

```bash
# 启动所有服务（后台运行）
docker compose up -d

# 查看启动日志
docker compose logs -f

# 按 Ctrl+C 退出日志查看
```

### 查看服务状态

```bash
# 查看容器状态
docker compose ps

# 预期输出：
# NAME                    STATUS          PORTS
# frontcrm-backend        Up 2 minutes    0.0.0.0:5000->5000/tcp
# frontcrm-frontend       Up 2 minutes    0.0.0.0:80->80/tcp
# frontcrm-postgres       Up 2 minutes    0.0.0.0:5432->5432/tcp
```

---

## 第六步：配置防火墙

### 使用 UFW 配置防火墙

```bash
# 1. 允许 SSH（重要，防止被锁定）
sudo ufw allow 22/tcp

# 2. 允许 HTTP 和 HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# 3. 允许后端 API（可选，如果需要直接访问）
sudo ufw allow 5000/tcp

# 4. 启用防火墙
sudo ufw enable

# 5. 查看防火墙状态
sudo ufw status

# 预期输出：
# Status: active
#
# To                         Action      From
# --                         ------      ----
# 22/tcp                     ALLOW       Anywhere
# 80/tcp                     ALLOW       Anywhere
# 443/tcp                    ALLOW       Anywhere
# 5000/tcp                   ALLOW       Anywhere
```

### 使用 iptables（替代方案）

如果不想使用 UFW：

```bash
# 允许已建立的连接
sudo iptables -A INPUT -m state --state ESTABLISHED,RELATED -j ACCEPT

# 允许 SSH
sudo iptables -A INPUT -p tcp --dport 22 -j ACCEPT

# 允许 HTTP/HTTPS
sudo iptables -A INPUT -p tcp --dport 80 -j ACCEPT
sudo iptables -A INPUT -p tcp --dport 443 -j ACCEPT

# 允许后端 API
sudo iptables -A INPUT -p tcp --dport 5000 -j ACCEPT

# 保存规则
sudo apt-get install -y iptables-persistent
sudo netfilter-persistent save
```

---

## 第七步：验证部署

### 检查容器状态

```bash
# 1. 查看所有容器
docker compose ps

# 2. 确认所有容器状态为 "Up"
docker compose ps --filter "status=running"
```

### 测试后端 API

```bash
# 1. 测试健康检查接口
curl http://localhost:5000/api/v1/health

# 预期输出：
# {"status":"healthy","timestamp":"2025-03-05T10:00:00Z"}

# 2. 查看后端日志
docker compose logs backend

# 3. 查看详细日志（最近 100 行）
docker compose logs --tail 100 backend
```

### 测试前端

```bash
# 1. 测试前端响应
curl -I http://localhost

# 预期输出：
# HTTP/1.1 200 OK
# Content-Type: text/html
```

### 浏览器访问

在浏览器中打开：
- **前端**：`http://your-server-ip`
- **后端 API**：`http://your-server-ip:5000/api/v1/health`

---

## 第八步：配置 SSL（可选但推荐）

### 安装 Certbot

```bash
# 1. 安装 Certbot
sudo apt-get install -y certbot python3-certbot-nginx

# 2. 准备域名
# 确保你的域名已解析到服务器 IP
```

### 获取 SSL 证书

```bash
# 方式 A：使用 Nginx（推荐）
sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com

# 方式 B：独立模式
sudo certbot certonly --standalone -d yourdomain.com

# 方式 C：DNS 验证
sudo certbot certonly --manual --preferred-challenges dns -d yourdomain.com
```

### 配置 Nginx

#### 安装 Nginx

```bash
# 1. 安装 Nginx
sudo apt-get install -y nginx

# 2. 启动 Nginx
sudo systemctl start nginx
sudo systemctl enable nginx
```

#### 创建 Nginx 配置

```bash
# 1. 创建配置文件
sudo nano /etc/nginx/sites-available/frontcrm
```

#### Nginx 配置内容

```nginx
# HTTP 重定向到 HTTPS
server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

# HTTPS 配置
server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;

    # SSL 证书路径
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    # SSL 配置
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # 安全头
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # 前端（Vue 应用）
    location / {
        proxy_pass http://localhost:80;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 后端 API
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # WebSocket 支持（如果需要）
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    # 日志
    access_log /var/log/nginx/frontcrm-access.log;
    error_log /var/log/nginx/frontcrm-error.log;
}
```

#### 启用配置

```bash
# 1. 创建符号链接
sudo ln -s /etc/nginx/sites-available/frontcrm /etc/nginx/sites-enabled/

# 2. 删除默认配置
sudo rm /etc/nginx/sites-enabled/default

# 3. 测试配置
sudo nginx -t

# 预期输出：
# nginx: configuration file /etc/nginx/nginx.conf test is successful

# 4. 重启 Nginx
sudo systemctl restart nginx

# 5. 设置自动续期
sudo certbot renew --dry-run
```

#### 更新 docker-compose.yml（修改前端端口）

```yaml
frontend:
  build:
    context: CRM.Web
    dockerfile: Dockerfile
  container_name: frontcrm-frontend
  ports:
    - "8080:80"  # 改为 8080，避免与 Nginx 冲突
  depends_on:
    - backend
  restart: always
  networks:
    - frontcrm-network
```

更新 Nginx 配置中的前端代理：
```nginx
proxy_pass http://localhost:8080;  # 修改为 8080
```

重启服务：
```bash
docker compose down
docker compose up -d
sudo systemctl restart nginx
```

---

## 第九步：自动备份

### 创建备份脚本

```bash
# 1. 创建备份目录
sudo mkdir -p /opt/backups

# 2. 创建备份脚本
sudo nano /usr/local/bin/backup-crm.sh
```

### 备份脚本内容

```bash
#!/bin/bash

# FrontCRM 自动备份脚本
# 功能：备份数据库和配置文件

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/backups"
RETENTION_DAYS=7
PROJECT_DIR="/opt/FrontCRM"

# 创建备份目录
mkdir -p $BACKUP_DIR

echo "========================================="
echo "Starting backup at $(date)"
echo "========================================="

# 1. 备份数据库
echo "Backing up PostgreSQL database..."
docker exec frontcrm-postgres pg_dump -U postgres FrontCRM > $BACKUP_DIR/crm_db_$DATE.sql

if [ $? -eq 0 ]; then
    echo "✓ Database backup completed"
else
    echo "✗ Database backup failed"
    exit 1
fi

# 2. 备份配置文件
echo "Backing up configuration files..."
tar -czf $BACKUP_DIR/crm_config_$DATE.tar.gz \
    $PROJECT_DIR/docker-compose.yml \
    $PROJECT_DIR/CRM.API/appsettings*.json \
    $PROJECT_DIR/CRM.Web/.env* 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✓ Configuration backup completed"
else
    echo "✗ Configuration backup failed"
fi

# 3. 压缩数据库备份
echo "Compressing database backup..."
gzip $BACKUP_DIR/crm_db_$DATE.sql

if [ $? -eq 0 ]; then
    echo "✓ Compression completed"
else
    echo "✗ Compression failed"
fi

# 4. 清理旧备份
echo "Cleaning old backups (older than $RETENTION_DAYS days)..."
find $BACKUP_DIR -name "crm_db_*.sql.gz" -mtime +$RETENTION_DAYS -delete
find $BACKUP_DIR -name "crm_config_*.tar.gz" -mtime +$RETENTION_DAYS -delete

# 5. 显示备份文件
echo ""
echo "Current backup files:"
ls -lh $BACKUP_DIR

echo ""
echo "========================================="
echo "Backup completed at $(date)"
echo "========================================="
```

### 设置权限和定时任务

```bash
# 1. 添加执行权限
sudo chmod +x /usr/local/bin/backup-crm.sh

# 2. 测试备份脚本
sudo /usr/local/bin/backup-crm.sh

# 3. 编辑 crontab
crontab -e
```

### 添加定时任务

在 crontab 文件末尾添加：

```bash
# FrontCRM 每日凌晨 2 点自动备份
0 2 * * * /usr/local/bin/backup-crm.sh >> /var/log/backup-crm.log 2>&1

# FrontCRM 每周日凌晨 3 点清理日志
0 3 * * 0 find /opt/FrontCRM/backend-logs -name "*.txt" -mtime +30 -delete
```

### 恢复备份

```bash
# 1. 恢复数据库
gunzip /opt/backups/crm_db_20250305_020000.sql.gz
cat /opt/backups/crm_db_20250305_020000.sql | docker exec -i frontcrm-postgres psql -U postgres -d FrontCRM

# 2. 恢复配置文件
tar -xzf /opt/backups/crm_config_20250305_020000.tar.gz -C /opt/FrontCRM
```

---

## 常用管理命令

### Docker Compose 命令

```bash
# 进入项目目录
cd /opt/FrontCRM

# 启动服务
docker compose up -d

# 停止服务
docker compose down

# 重启服务
docker compose restart

# 重启特定服务
docker compose restart backend
docker compose restart frontend
docker compose restart postgres

# 查看服务状态
docker compose ps

# 查看日志
docker compose logs -f              # 所有服务
docker compose logs -f backend      # 后端
docker compose logs -f frontend     # 前端
docker compose logs -f postgres     # 数据库

# 查看最近 N 行日志
docker compose logs --tail 100 backend

# 实时跟踪日志
docker compose logs -f --tail 50 backend
```

### 容器管理

```bash
# 查看运行中的容器
docker ps

# 查看所有容器（包括停止的）
docker ps -a

# 进入容器
docker compose exec backend bash
docker compose exec postgres psql -U postgres FrontCRM

# 查看容器资源使用
docker stats

# 查看容器详细信息
docker inspect frontcrm-backend

# 停止容器
docker stop frontcrm-backend

# 启动容器
docker start frontcrm-backend

# 删除容器
docker rm frontcrm-backend
```

### 镜像管理

```bash
# 查看镜像
docker images

# 删除镜像
docker rmi <镜像ID>

# 清理未使用的镜像
docker image prune -f

# 清理所有未使用的资源
docker system prune -a
```

### 数据库管理

```bash
# 进入数据库
docker compose exec postgres psql -U postgres FrontCRM

# 查看数据库列表
docker compose exec postgres psql -U postgres -l

# 备份数据库
docker exec frontcrm-postgres pg_dump -U postgres FrontCRM > backup.sql

# 恢复数据库
cat backup.sql | docker exec -i frontcrm-postgres psql -U postgres FrontCRM
```

### 日志管理

```bash
# 查看容器日志
docker logs frontcrm-backend

# 查看文件日志
tail -f /opt/FrontCRM/backend-logs/log-*.txt

# 查看磁盘使用
du -sh /opt/FrontCRM/backend-logs

# 清理旧日志（保留最近 30 天）
find /opt/FrontCRM/backend-logs -name "*.txt" -mtime +30 -delete
```

### 系统监控

```bash
# 查看 CPU 和内存使用
htop

# 查看磁盘使用
df -h

# 查看内存使用
free -h

# 查看网络连接
netstat -tlnp
```

---

## 故障排查

### 容器无法启动

```bash
# 1. 查看容器状态
docker compose ps

# 2. 查看详细日志
docker compose logs backend

# 3. 检查端口占用
sudo netstat -tlnp | grep 5000

# 4. 检查磁盘空间
df -h

# 5. 查看容器事件
docker events --since 1h
```

### 数据库连接失败

```bash
# 1. 检查 PostgreSQL 容器状态
docker compose ps postgres

# 2. 查看 PostgreSQL 日志
docker compose logs postgres

# 3. 测试数据库连接
docker compose exec postgres pg_isready -U postgres

# 4. 检查连接字符串
docker compose exec backend env | grep ConnectionStrings

# 5. 验证数据库存在
docker compose exec postgres psql -U postgres -l | grep FrontCRM
```

### 前端无法访问后端

```bash
# 1. 检查后端容器状态
docker compose ps backend

# 2. 测试后端 API
curl http://localhost:5000/api/v1/health

# 3. 查看后端日志
docker compose logs backend

# 4. 检查网络
docker network inspect frontcrm_frontcrm-network

# 5. 从前端容器测试
docker compose exec frontend wget -qO- http://backend:5000/api/v1/health
```

### 磁盘空间不足

```bash
# 1. 查看磁盘使用
df -h

# 2. 清理 Docker 未使用的资源
docker system prune -a

# 3. 清理日志
find /opt/FrontCRM/backend-logs -name "*.txt" -mtime +30 -delete

# 4. 清理备份文件
find /opt/backups -name "crm_*.sql.gz" -mtime +7 -delete

# 5. 清理 PostgreSQL WAL 文件
docker compose exec postgres psql -U postgres -c "VACUUM FULL;"
```

### 内存不足

```bash
# 1. 查看内存使用
free -h

# 2. 查看进程内存使用
ps aux --sort=-%mem | head -20

# 3. 查看容器内存限制
docker inspect frontcrm-backend | grep -A 10 Memory

# 4. 增加交换空间
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile

# 5. 永久启用交换空间
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### SSL 证书问题

```bash
# 1. 检查证书有效期
sudo certbot certificates

# 2. 手动续期
sudo certbot renew

# 3. 测试续期
sudo certbot renew --dry-run

# 4. 查看续期日志
sudo tail -f /var/log/letsencrypt/letsencrypt.log

# 5. 重启 Nginx
sudo systemctl reload nginx
```

### 网络问题

```bash
# 1. 检查防火墙状态
sudo ufw status

# 2. 查看开放端口
sudo netstat -tlnp

# 3. 测试端口连通性
telnet localhost 5000

# 4. 查看 Docker 网络
docker network ls
docker network inspect frontcrm_frontcrm-network

# 5. 重建网络
docker compose down
docker network prune
docker compose up -d
```

### 权限问题

```bash
# 1. 检查目录权限
ls -la /opt/FrontCRM

# 2. 修复权限
sudo chown -R $USER:$USER /opt/FrontCRM
chmod -R 755 /opt/FrontCRM

# 3. 检查 Docker 权限
groups

# 4. 添加用户到 docker 组
sudo usermod -aG docker $USER
newgrp docker
```

### 查看详细错误

```bash
# 1. 查看容器详细信息
docker inspect <容器名>

# 2. 查看系统日志
sudo journalctl -u docker -f

# 3. 查看 Docker 守护进程日志
sudo tail -f /var/log/docker.log

# 4. 查看 Nginx 错误日志
sudo tail -f /var/log/nginx/frontcrm-error.log

# 5. 查看系统日志
sudo tail -f /var/log/syslog
```

---

## 更新项目

### 使用 Git 更新

```bash
# 1. 拉取最新代码
cd /opt/FrontCRM
git pull origin main

# 2. 查看变更
git log --oneline -5

# 3. 重新构建
docker compose build

# 4. 重启服务
docker compose up -d

# 5. 查看日志
docker compose logs -f
```

### 手动更新文件

```bash
# 1. 停止服务
docker compose down

# 2. 上传新文件
# 使用 SCP 或 SFTP 上传

# 3. 重新构建
docker compose build

# 4. 启动服务
docker compose up -d

# 5. 清理旧镜像
docker image prune -f
```

### 数据库迁移

```bash
# 1. 进入后端容器
docker compose exec backend bash

# 2. 运行迁移
dotnet ef database update

# 3. 检查迁移状态
dotnet ef migrations list
```

---

## 性能优化

### Docker 日志配置

```bash
# 创建 Docker 守护进程配置
sudo nano /etc/docker/daemon.json
```

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

```bash
# 重启 Docker
sudo systemctl restart docker
```

### 资源限制

在 `docker-compose.yml` 中添加资源限制：

```yaml
backend:
  deploy:
    resources:
      limits:
        cpus: '1.0'
        memory: 1G
      reservations:
        cpus: '0.5'
        memory: 512M
```

---

## 安全加固

### 1. 更新系统

```bash
sudo apt-get update && sudo apt-get upgrade -y
```

### 2. 配置 SSH 安全

```bash
# 编辑 SSH 配置
sudo nano /etc/ssh/sshd_config

# 修改以下配置：
# PermitRootLogin no
# PasswordAuthentication no
# Port 2222

# 重启 SSH
sudo systemctl restart ssh
```

### 3. 安装 fail2ban

```bash
sudo apt-get install -y fail2ban
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

---

## 快速检查清单

部署完成后，请检查以下项目：

- [ ] Docker 已安装并运行
- [ ] Docker Compose 可用
- [ ] 项目已上传到 `/opt/FrontCRM`
- [ ] 数据库密码已修改
- [ ] `docker compose build` 成功
- [ ] `docker compose up -d` 成功
- [ ] `docker compose ps` 显示所有容器运行中
- [ ] 前端可访问 `http://your-server-ip`
- [ ] 后端 API 可访问 `http://your-server-ip:5000/api/v1/health`
- [ ] 防火墙已配置
- [ ] SSL 证书已配置（如果需要）
- [ ] 备份脚本已设置
- [ ] 定时任务已配置
- [ ] 日志正常输出
- [ ] 资源使用正常

---

## 联系支持

如遇到问题，请提供以下信息：
1. 服务器操作系统版本：`lsb_release -a`
2. Docker 版本：`docker --version`
3. 容器状态：`docker compose ps`
4. 错误日志：`docker compose logs backend`

---

**部署完成！** 🎉

如有任何问题，请参考故障排查章节或联系技术支持。
