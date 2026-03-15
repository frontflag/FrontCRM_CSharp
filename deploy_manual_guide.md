# FrontCRM 香港服务器手动部署指南

## 📋 服务器信息
- **IP地址**: 129.226.161.3
- **实例ID**: ins-mxrewslo
- **用户名**: ubuntu
- **状态**: 未设置密码

## 🚀 部署步骤

### 第一步：设置服务器密码
由于服务器未设置密码，您需要在腾讯云控制台设置：

1. **登录腾讯云控制台**
2. **进入轻量应用服务器**
3. **找到实例 `ins-mxrewslo`**
4. **重置密码**：选择"更多" → "重置密码"
5. **设置新密码**（请妥善保管）

### 第二步：连接服务器
```bash
# 使用新设置的密码连接
ssh ubuntu@129.226.161.3
# 输入您设置的密码
```

### 第三步：安装必要软件
在服务器上执行以下命令：

```bash
# 1. 更新系统
sudo apt-get update
sudo apt-get upgrade -y

# 2. 安装 Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# 3. 安装 Docker Compose
sudo apt-get install docker-compose -y

# 4. 启动 Docker 服务
sudo systemctl start docker
sudo systemctl enable docker

# 5. 将当前用户加入 docker 组
sudo usermod -aG docker ubuntu

# 6. 登出并重新登录
exit

# 重新连接
ssh ubuntu@129.226.161.3
```

### 第四步：准备部署目录
```bash
# 创建项目目录
sudo mkdir -p /home/ubuntu/frontcrm
sudo chown -R ubuntu:ubuntu /home/ubuntu/frontcrm

# 进入目录
cd /home/ubuntu/frontcrm
```

### 第五步：上传项目文件

**从您的本地 Windows 机器上传**：

在 Windows PowerShell 中执行：
```powershell
# 压缩项目
Compress-Archive -Path "d:\MyProject\FrontCRM_CSharp\*" -DestinationPath "frontcrm_deploy.zip"

# 上传到服务器（需要先设置密码）
scp frontcrm_deploy.zip ubuntu@129.226.161.3:/home/ubuntu/frontcrm/

# 或者使用 FTP/SFTP 客户端
```

**在服务器上解压**：
```bash
cd /home/ubuntu/frontcrm
unzip frontcrm_deploy.zip
```

### 第六步：修改后端配置解决 NuGet 问题

编辑 `Dockerfile.backend`，在 `RUN dotnet restore` 前添加：
```dockerfile
# 在 build 阶段添加国内镜像源
RUN sed -i 's|https://api.nuget.org/v3/index.json|https://mirrors.cloud.tencent.com/nuget/|g' /root/.nuget/NuGet/NuGet.Config || true
RUN dotnet nuget add source https://mirrors.cloud.tencent.com/nuget/ --name tencent
```

### 第七步：创建生产环境配置文件

创建 `docker-compose.prod.yml`：
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: frontcrm-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
      POSTGRES_DB: FrontCRM
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped
    networks:
      - frontcrm-network

  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    container_name: frontcrm-backend
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=FrontCRM;Username=postgres;Password=postgres123
      ASPNETCORE_URLS: http://+:5000
    ports:
      - "5000:5000"
    depends_on:
      - postgres
    restart: unless-stopped
    networks:
      - frontcrm-network

  frontend:
    build:
      context: ./CRM.Web
      dockerfile: Dockerfile
    container_name: frontcrm-frontend
    ports:
      - "80:80"
    depends_on:
      - backend
    restart: unless-stopped
    networks:
      - frontcrm-network

volumes:
  postgres_data:

networks:
  frontcrm-network:
    driver: bridge
```

### 第八步：构建前端
```bash
cd /home/ubuntu/frontcrm/CRM.Web
npm install
npm run build
```

### 第九步：启动服务
```bash
cd /home/ubuntu/frontcrm
docker-compose -f docker-compose.prod.yml up -d
```

### 第十步：检查部署状态
```bash
# 查看容器状态
docker-compose -f docker-compose.prod.yml ps

# 查看日志
docker-compose -f docker-compose.prod.yml logs -f

# 检查服务健康
curl http://localhost:5000/api/health || echo "后端可能还在启动中"
```

### 第十一步：配置防火墙
```bash
# 允许必要端口
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # 前端
sudo ufw allow 5000/tcp  # 后端
sudo ufw --force enable
```

## 🌐 访问地址

- **前端**: http://129.226.161.3
- **后端API**: http://129.226.161.3:5000
- **数据库**: 129.226.161.3:5432 (用户名: postgres, 密码: postgres123)

## 🔧 常用管理命令

```bash
# 查看服务状态
docker-compose -f docker-compose.prod.yml ps

# 查看日志
docker-compose -f docker-compose.prod.yml logs -f

# 停止服务
docker-compose -f docker-compose.prod.yml down

# 重启服务
docker-compose -f docker-compose.prod.yml restart

# 更新服务（修改后）
docker-compose -f docker-compose.prod.yml up -d --build
```

## 🐛 故障排除

### 问题1：后端构建失败 (NuGet 包下载)
**解决方案**：
1. 确保已修改 Dockerfile 使用国内镜像源
2. 或在服务器上使用代理：
   ```bash
   export https_proxy=http://your-proxy:port
   export http_proxy=http://your-proxy:port
   ```

### 问题2：前端无法访问
**检查**：
1. Nginx 是否运行
2. 端口 80 是否开放
3. 防火墙规则是否正确

### 问题3：数据库连接失败
**检查**：
1. PostgreSQL 容器是否运行
2. 连接字符串配置是否正确
3. 数据库端口是否允许外部访问

## 📞 技术支持

如果遇到问题，请检查：
1. 服务器 SSH 连接是否正常
2. Docker 和 Docker Compose 是否安装成功
3. 项目文件是否完整上传
4. 所有服务端口是否正常开放

## 🎯 下一步

1. **配置域名**（可选）：将域名解析到 129.226.161.3
2. **设置 SSL 证书**：使用 Let's Encrypt 配置 HTTPS
3. **配置备份**：定期备份数据库
4. **监控设置**：配置系统监控和日志收集

---

**重要提示**：请先通过腾讯云控制台为实例设置密码，这是所有后续操作的前提。