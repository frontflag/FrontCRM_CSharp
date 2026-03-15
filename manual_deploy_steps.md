# FrontCRM 香港服务器手动部署步骤

## ✅ 已完成步骤
1. ✅ SSH连接测试成功
2. ✅ Docker已安装成功

## 🚀 下一步：手动部署

### 第一步：在服务器上创建项目目录
在PowerShell中执行：
```powershell
# 连接到服务器（输入密码）
ssh ubuntu@129.226.161.3
```

在服务器上执行：
```bash
# 创建项目目录
sudo mkdir -p /home/ubuntu/frontcrm
sudo chown -R ubuntu:ubuntu /home/ubuntu/frontcrm

# 进入目录
cd /home/ubuntu/frontcrm
```

### 第二步：上传项目文件（从本地Windows）

**选项A：使用SCP上传（推荐）**
在PowerShell中执行（新窗口）：
```powershell
# 上传整个项目目录
scp -r "d:\MyProject\FrontCRM_CSharp" ubuntu@129.226.161.3:/home/ubuntu/

# 或者只上传关键文件
scp "d:\MyProject\FrontCRM_CSharp\docker-compose.yml" ubuntu@129.226.161.3:/home/ubuntu/frontcrm/
scp "d:\MyProject\FrontCRM_CSharp\Dockerfile.backend" ubuntu@129.226.161.3:/home/ubuntu/frontcrm/
scp -r "d:\MyProject\FrontCRM_CSharp\CRM.API" ubuntu@129.226.161.3:/home/ubuntu/frontcrm/
scp -r "d:\MyProject\FrontCRM_CSharp\CRM.Core" ubuntu@129.226.161.3:/home/ubuntu/frontcrm/
scp -r "d:\MyProject\FrontCRM_CSharp\CRM.Infrastructure" ubuntu@129.226.161.3:/home/ubuntu/frontcrm/
scp -r "d:\MyProject\FrontCRM_CSharp\CRM.Web" ubuntu@129.226.161.3:/home/ubuntu/frontcrm/
```

**选项B：压缩后上传**
1. 在Windows上压缩项目为ZIP文件
2. 上传ZIP文件到服务器
3. 在服务器上解压

### 第三步：在服务器上配置和部署

在服务器上执行：
```bash
# 1. 进入项目目录
cd /home/ubuntu/frontcrm

# 2. 解决后端NuGet问题（创建nuget.config）
cat > nuget.config << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="tencent" value="https://mirrors.cloud.tencent.com/nuget/" />
    <add key="aliyun" value="https://nuget.alicdn.com/nuget/index.json" />
  </packageSources>
</configuration>
EOF

# 3. 复制到CRM.API目录
cp nuget.config CRM.API/

# 4. 构建前端（需要在CRM.Web目录中）
cd CRM.Web
npm install
npm run build
cd ..

# 5. 使用docker-compose部署
docker-compose up -d

# 6. 检查服务状态
docker-compose ps

# 7. 查看日志
docker-compose logs -f
```

### 第四步：配置防火墙
```bash
# 开放必要端口
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # 前端HTTP
sudo ufw allow 5000/tcp  # 后端API
sudo ufw --force enable

# 检查端口开放情况
sudo ufw status
```

### 第五步：验证部署
1. **前端访问**：打开浏览器访问 http://129.226.161.3
2. **后端API测试**：
   ```bash
   curl http://localhost:5000/api/health
   ```
3. **查看容器状态**：
   ```bash
   docker-compose ps
   docker-compose logs --tail=50
   ```

## 🔧 故障排除

### 问题：后端构建失败（NuGet包下载）
**解决方案**：
1. 确保 `nuget.config` 文件已创建
2. 或修改 Dockerfile.backend：
   ```dockerfile
   # 在 RUN dotnet restore 前添加
   RUN dotnet nuget add source https://mirrors.cloud.tencent.com/nuget/ --name tencent
   ```

### 问题：前端无法访问
**检查**：
```bash
# 检查Nginx容器是否运行
docker ps | grep frontcrm-frontend

# 检查端口映射
docker port frontcrm-frontend
```

### 问题：数据库连接失败
**检查**：
```bash
# PostgreSQL容器状态
docker ps | grep frontcrm-postgres

# 数据库日志
docker logs frontcrm-postgres
```

## 📊 部署成功标志

1. ✅ 三个容器都显示为 `Up` 状态
2. ✅ 前端页面正常加载（无错误）
3. ✅ 后端API响应正常
4. ✅ 防火墙规则正确配置

---

## 🎯 现在请执行：

1. **打开新的PowerShell窗口**
2. **上传项目文件**：
   ```powershell
   scp -r "d:\MyProject\FrontCRM_CSharp" ubuntu@129.226.161.3:/home/ubuntu/
   ```
3. **在服务器上执行部署命令**

需要我为您提供更详细的具体命令吗？