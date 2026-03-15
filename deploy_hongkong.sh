#!/bin/bash
# FrontCRM 香港服务器一键部署脚本
# 服务器IP: 129.226.161.3
# 执行方式: bash deploy_hongkong.sh

set -e

echo "🚀 FrontCRM 项目部署脚本开始执行"
echo "=================================="

# 配置变量
SERVER_IP="129.226.161.3"
USER="ubuntu"
PROJECT_DIR="/home/ubuntu/frontcrm"
BACKEND_PORT="5000"
FRONTEND_PORT="80"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查服务器连接
check_connection() {
    log_info "检查服务器连接..."
    if ping -c 1 -W 2 $SERVER_IP > /dev/null 2>&1; then
        log_info "服务器可达"
    else
        log_error "无法连接到服务器 $SERVER_IP"
        exit 1
    fi
}

# 安装 Docker
install_docker() {
    log_info "安装 Docker..."
    ssh $USER@$SERVER_IP << 'EOF'
        # 安装必要工具
        sudo apt-get update
        sudo apt-get install -y ca-certificates curl gnupg lsb-release
        
        # 添加 Docker GPG 密钥
        sudo mkdir -p /etc/apt/keyrings
        curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
        
        # 添加 Docker 仓库
        echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
        
        # 安装 Docker
        sudo apt-get update
        sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
        
        # 启动 Docker 服务
        sudo systemctl start docker
        sudo systemctl enable docker
        
        # 将当前用户加入 docker 组
        sudo usermod -aG docker $USER
EOF
    
    log_info "Docker 安装完成"
}

# 上传项目文件
upload_project() {
    log_info "上传项目文件到服务器..."
    
    # 创建本地临时部署目录（在Windows上）
    LOCAL_TEMP_DIR="$TEMP/frontcrm_deploy_$(date +%s)"
    mkdir -p "$LOCAL_TEMP_DIR"
    
    # 复制关键文件
    cp -r ./* "$LOCAL_TEMP_DIR/" 2>/dev/null || true
    
    # 创建优化后的 docker-compose 文件
    cat > "$LOCAL_TEMP_DIR/docker-compose.prod.yml" << 'EOF'
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
      JWT__Secret: YourSuperSecretKeyForJWT2026
      JWT__Issuer: FrontCRM
      JWT__Audience: FrontCRM-Users
    ports:
      - "5000:5000"
    depends_on:
      - postgres
    restart: unless-stopped
    networks:
      - frontcrm-network
    volumes:
      - ./logs:/app/logs

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
EOF
    
    # 创建前端构建脚本
    cat > "$LOCAL_TEMP_DIR/CRM.Web/build_frontend.sh" << 'EOF'
#!/bin/bash
# 前端构建脚本
echo "构建前端..."
npm install
npm run build
EOF
    
    chmod +x "$LOCAL_TEMP_DIR/CRM.Web/build_frontend.sh"
    
    # 上传到服务器
    scp -r "$LOCAL_TEMP_DIR" $USER@$SERVER_IP:$PROJECT_DIR
    
    # 清理本地临时文件
    rm -rf "$LOCAL_TEMP_DIR"
    
    log_info "项目文件上传完成"
}

# 配置服务器环境
setup_server() {
    log_info "配置服务器环境..."
    
    ssh $USER@$SERVER_IP << EOF
        # 创建项目目录
        sudo mkdir -p $PROJECT_DIR
        sudo chown -R $USER:$USER $PROJECT_DIR
        
        # 配置防火墙
        sudo ufw allow 22/tcp
        sudo ufw allow 80/tcp
        sudo ufw allow 5000/tcp
        sudo ufw --force enable
        
        # 创建日志目录
        mkdir -p $PROJECT_DIR/logs
EOF
    
    log_info "服务器环境配置完成"
}

# 构建和启动项目
deploy_project() {
    log_info "开始部署项目..."
    
    ssh $USER@$SERVER_IP << EOF
        cd $PROJECT_DIR
        
        log_info "构建前端..."
        cd CRM.Web
        bash build_frontend.sh
        cd ..
        
        log_info "启动 Docker 服务..."
        docker-compose -f docker-compose.prod.yml down
        docker-compose -f docker-compose.prod.yml build --no-cache
        docker-compose -f docker-compose.prod.yml up -d
        
        log_info "检查服务状态..."
        sleep 10
        docker-compose -f docker-compose.prod.yml ps
        
        log_info "查看服务日志..."
        docker-compose -f docker-compose.prod.yml logs --tail=20
EOF
    
    log_info "部署完成"
}

# 验证部署
verify_deployment() {
    log_info "验证部署..."
    
    echo -e "\n${GREEN}✅ 部署验证${NC}"
    echo "=================================="
    echo "前端访问: http://$SERVER_IP"
    echo "后端API: http://$SERVER_IP:5000/api/health"
    echo "数据库: $SERVER_IP:5432"
    echo ""
    echo "检查命令:"
    echo "  ssh $USER@$SERVER_IP"
    echo "  cd $PROJECT_DIR && docker-compose -f docker-compose.prod.yml ps"
    echo "  docker-compose -f docker-compose.prod.yml logs -f"
}

# 主函数
main() {
    log_info "开始部署 FrontCRM 到香港服务器 (IP: $SERVER_IP)"
    echo ""
    
    # 检查连接
    check_connection
    
    # 设置服务器环境
    setup_server
    
    # 安装 Docker
    install_docker
    
    # 上传项目
    upload_project
    
    # 部署项目
    deploy_project
    
    # 验证部署
    verify_deployment
    
    echo -e "\n${GREEN}🎉 部署完成！${NC}"
    echo "=================================="
    echo "请访问: http://$SERVER_IP"
    echo ""
    echo "如果需要设置SSH密码，请执行:"
    echo "  ssh $USER@$SERVER_IP"
    echo "  sudo passwd ubuntu"
}

# 执行主函数
main "$@"