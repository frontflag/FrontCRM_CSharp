# FrontCRM 本地编译+服务器部署方案

## 🎯 目标：本地编译所有代码，服务器只运行不编译

### 第一步：解决 NuGet 包下载问题

#### 方案A：使用国内镜像源（推荐）
1. **已创建** `CRM.API/nuget.config` 文件
2. **运行修复脚本**：
   ```bash
   # 双击运行
   修复NuGet问题.bat
   ```

3. **手动配置**：
   ```powershell
   # 添加腾讯云镜像源
   dotnet nuget add source https://mirrors.cloud.tencent.com/nuget/ -n tencent
   
   # 添加阿里云镜像源  
   dotnet nuget add source https://nuget.alicdn.com/nuget/index.json -n aliyun
   
   # 恢复包
   cd "d:\MyProject\FrontCRM_CSharp\CRM.API"
   dotnet restore --source https://mirrors.cloud.tencent.com/nuget/
   ```

#### 方案B：使用代理
```powershell
# 设置代理环境变量
$env:https_proxy="http://127.0.0.1:7890"
$env:http_proxy="http://127.0.0.1:7890"

# 恢复包
cd "d:\MyProject\FrontCRM_CSharp\CRM.API"
dotnet restore
```

#### 方案C：离线下载包
1. 访问 https://www.nuget.org/packages/
2. 下载以下包：
   - Microsoft.AspNetCore.Authentication.JwtBearer 9.0.11
   - Microsoft.EntityFrameworkCore 9.0.11
   - Npgsql.EntityFrameworkCore.PostgreSQL 9.0.3
   - Serilog.AspNetCore 10.0.0
   - System.IdentityModel.Tokens.Jwt 8.8.0
   - BCrypt.Net-Next 4.1.0
   - Microsoft.EntityFrameworkCore.InMemory 9.0.11
   - Serilog.Sinks.Console 6.1.1
   - Serilog.Sinks.File 7.0.0

3. 将 `.nupkg` 文件放到：
   ```
   C:\Users\[用户名]\.nuget\packages\
   ```

### 第二步：本地编译所有代码

#### 1. 编译前端（Vue 3）
```powershell
cd "d:\MyProject\FrontCRM_CSharp\CRM.Web"
npm install
npm run build
```
**生成**：`dist/` 目录（包含已编译的前端文件）

#### 2. 编译后端（ASP.NET Core）
```powershell
cd "d:\MyProject\FrontCRM_CSharp\CRM.API"
dotnet restore  # 如果使用国内镜像源
dotnet build -c Release
dotnet publish -c Release -o ./publish
```
**生成**：`publish/` 目录（包含可执行文件和依赖）

#### 3. 检查编译结果
```
CRM.Web/dist/          # 前端编译结果
CRM.API/publish/       # 后端编译结果
```

### 第三步：优化 Dockerfile 用于部署编译结果

#### 修改 `Dockerfile.backend`
```dockerfile
# 使用已编译的后端文件
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# 复制已编译的文件
COPY CRM.API/publish/ /app/

ENTRYPOINT ["dotnet", "CRM.API.dll"]
```

#### 修改 `CRM.Web/Dockerfile`
```dockerfile
# 使用已编译的前端文件
FROM nginx:alpine
COPY dist/ /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### 第四步：创建部署包结构

```
frontcrm_deploy/
├── docker-compose.yml          # 容器编排
├── Dockerfile.backend          # 后端Dockerfile
├── CRM.Web/
│   ├── dist/                   # 已编译前端
│   ├── Dockerfile              # 前端Dockerfile
│   └── nginx.conf             # Nginx配置
└── CRM.API/
    └── publish/                # 已编译后端
```

### 第五步：上传到服务器

```powershell
# 1. 创建部署目录
$deployDir = "$TEMP\frontcrm_deploy"
mkdir -p $deployDir

# 2. 复制必要文件
Copy-Item "docker-compose.yml" $deployDir\
Copy-Item "Dockerfile.backend" $deployDir\
Copy-Item -Recurse "CRM.Web\dist" "$deployDir\CRM.Web\"
Copy-Item "CRM.Web\Dockerfile" "$deployDir\CRM.Web\"
Copy-Item -Recurse "CRM.API\publish" "$deployDir\CRM.API\"

# 3. 上传到服务器
scp -r $deployDir ubuntu@129.226.161.3:/home/ubuntu/
```

### 第六步：在服务器上部署

```bash
# 1. 进入部署目录
cd /home/ubuntu/frontcrm_deploy

# 2. 启动服务
docker-compose up -d

# 3. 检查状态
docker-compose ps
docker-compose logs -f
```

## 🎯 优势

1. **快速部署**：服务器不需要编译，直接运行
2. **低负载**：服务器资源用于运行服务，而非编译
3. **环境一致**：本地编译确保环境兼容性
4. **可重复**：相同的编译结果可部署到多台服务器

## 🔧 故障排除

### 问题：NuGet包下载失败
**解决**：
1. 使用 `修复NuGet问题.bat`
2. 检查网络连接
3. 尝试使用代理

### 问题：前端编译失败
**解决**：
```powershell
# 清理缓存
rm -rf node_modules
rm -rf dist

# 重新安装
npm cache clean --force
npm install
npm run build
```

### 问题：后端编译失败
**解决**：
```powershell
# 清理
dotnet clean
rm -rf bin/ obj/ publish/

# 重新编译
dotnet restore --source https://mirrors.cloud.tencent.com/nuget/
dotnet build
dotnet publish -c Release -o ./publish
```

## 📋 下一步

1. **先运行** `修复NuGet问题.bat`
2. **如果成功**：编译前端和后端
3. **如果失败**：告诉我具体错误信息

**请先尝试解决NuGet问题，然后告诉我结果。**