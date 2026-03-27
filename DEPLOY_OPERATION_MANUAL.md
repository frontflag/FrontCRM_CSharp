# FrontCRM 发布操作手册（非 Docker）

适用场景：服务器不使用 Docker，采用“前端静态托管 + 后端 dotnet 进程 + Nginx 反代”的方式发布。

本手册依据你本次在 `Ubuntu 22.04` 上的实际发布过程整理。

## 0. 发布前准备

确认本机已完成版本构建并生成部署包（通常为 `frontcrm_deploy/`）。

检查仓库根目录是否存在：

```text
frontcrm_deploy/CRM.Web/dist/
frontcrm_deploy/CRM.API/publish/
frontcrm_deploy/（其他预设文件）
```

## 1. Ubuntu 一次性环境准备

### 1.1 安装 .NET 9 ASP.NET Core Runtime

在服务器上执行（Ubuntu 22.04）：

```bash
sudo apt-get update
sudo apt-get install -y wget ca-certificates

wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-9.0

dotnet --info
```

### 1.2 安装 Nginx

```bash
sudo apt-get update
sudo apt-get install -y nginx
sudo systemctl enable nginx --now
```

验证：

```bash
sudo ss -ltnp | grep ':80' || true
```

## 2. 上传发布产物到服务器

目标目录建议使用：

```text
/opt/frontcrm/frontend
/opt/frontcrm/backend
```

准备目录：

```bash
sudo mkdir -p /opt/frontcrm/frontend /opt/frontcrm/backend
sudo chown -R ubuntu:ubuntu /opt/frontcrm
```

上传前端 `dist`：

```powershell
scp -r "d:\MyProject\FrontCRM_CSharp\frontcrm_deploy\CRM.Web\dist\*" ubuntu@<SERVER_IP>:/opt/frontcrm/frontend/
```

上传后端 publish：

```powershell
scp -r "d:\MyProject\FrontCRM_CSharp\frontcrm_deploy\CRM.API\publish\*" ubuntu@<SERVER_IP>:/opt/frontcrm/backend/
```

## 3. 配置 Nginx（前端 SPA + API 反代）

在服务器上创建/覆盖站点配置：

```bash
sudo tee /etc/nginx/sites-available/frontcrm > /dev/null <<'EOF'
server {
    listen 80;
    server_name crm.semicegroup.com;

    root /opt/frontcrm/frontend;
    index index.html;

    location = /index.html {
        add_header Cache-Control "no-cache, no-store, must-revalidate";
    }

    location /assets/ {
        add_header Cache-Control "public, max-age=31536000, immutable";
    }

    # SPA 回退：/debug 等前端路由需要回 index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API 代理到本机 dotnet 后端
    location /api {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;

        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        proxy_cache_bypass $http_upgrade;
    }

    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
}
EOF
```

启用站点并重载：

```bash
sudo ln -sf /etc/nginx/sites-available/frontcrm /etc/nginx/sites-enabled/frontcrm
sudo rm -f /etc/nginx/sites-enabled/default 2>/dev/null || true
sudo nginx -t && sudo systemctl reload nginx
```

## 4. 启动后端 dotnet（监听 5000）

后端目录通常在：

```text
/opt/frontcrm/backend
```

启动前关键点：

- 若 `appsettings.Production.json` 的连接串为空，且你将 `ASPNETCORE_ENVIRONMENT` 设置成 `Production`，后端会启动失败
- 本次你采用了 `ASPNETCORE_ENVIRONMENT=Local` 来使用 `/opt/frontcrm/backend/appsettings.json` 的连接串

启动命令（示例，连接串如需修改请参考第 7 节）：

```bash
cd /opt/frontcrm/backend
pkill -f "CRM.API.dll" 2>/dev/null || true

export ASPNETCORE_ENVIRONMENT=Local
export ASPNETCORE_URLS=http://0.0.0.0:5000

nohup dotnet CRM.API.dll > api.log 2>&1 &
sleep 2

tail -200 api.log
sudo ss -ltnp | grep ':5000' || true
```

验证后端健康：

```bash
curl -i http://127.0.0.1:5000/api/v1/health
curl -i http://127.0.0.1:5000/api/v1/debug
```

## 5. 验证前端与联通性

验证前端（走 Nginx）：

```bash
curl -I http://127.0.0.1/ | head -n 5
curl -I http://127.0.0.1/debug | head -n 5
```

验证 API 是否被 Nginx 正确转发：

```bash
curl -i http://127.0.0.1/api/v1/health | head -n 20
curl -i http://127.0.0.1/api/v1/debug | head -n 20
```

浏览器访问：

- 前端 Debug（免登录）：`http://crm.semicegroup.com/debug`
- Debug 模拟写入（需要登录）：`http://crm.semicegroup.com/debug/data`

## 6. Debug 页常见提示与权限说明

### 6.1 “连接串没写对”怎么判断

如果后端日志里出现类似：

- “无法连接到数据库”
- “Password/ConnectionStrings 为空”

则说明应用在某个环境配置下加载到了空连接串。

### 6.2 “permission denied for table debug”

该错误通常表示：连接到正确数据库了，但后端使用的数据库账号没有 `debug` 表权限。

解决思路：

- 在数据库中给 `debug` 表补齐权限（至少 `SELECT`，如果还涉及写入/模拟写库，则还需 `INSERT/UPDATE/DELETE` 等）

具体 SQL 需根据你实际数据库账号与部署环境决定。

## 7. 修改连接串（当需要换库/改密码）

你可以二选一：

方案 A：直接改配置文件

```bash
sudo nano /opt/frontcrm/backend/appsettings.json
```

方案 B：在启动时通过环境变量覆盖

```bash
export 'ConnectionStrings__DefaultConnection=<你的连接串>'
```

重启后端后再用：

```bash
curl -i http://127.0.0.1:5000/api/v1/health
```

## 8. 回滚/重启建议

仅更新版本时，一般执行：

1. 上传新的 `frontend/dist` 与 `backend/publish`
2. 重启后端进程（先 pkill 再 nohup）
3. 只要 Nginx 配置不变，无需重启 Nginx（但可 `nginx -t` 校验）

## 9. 快速排错清单

1. 前端访问 404/显示 Welcome：说明 Nginx 未启用你的站点配置或站点配置冲突
2. 前端访问 502：说明 `/api` 代理到的后端（本机 5000）不可达（后端未启动或监听失败）
3. 后端启动后立刻退出：检查 `api.log`，通常是连接串/数据库权限/网络白名单问题
4. Debug 页出现 “permission denied for table debug”：检查数据库账号对 `debug` 表的权限

