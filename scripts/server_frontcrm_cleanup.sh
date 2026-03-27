#!/usr/bin/env bash
# 在服务器上执行：停止并删除当前目录 compose 管理的容器、网络；删除已废弃的 postgres 数据卷。
# 用法：cd /home/ubuntu/frontcrm && bash /path/to/server_frontcrm_cleanup.sh
# 注意：会删除名为 frontcrm_postgres_data 等 compose 前缀的卷；请先备份 .env 与数据库。

set -euo pipefail
COMPOSE_DIR="${1:-$HOME/frontcrm}"
cd "$COMPOSE_DIR"

echo "==> compose down (含孤儿容器；若有 postgres 卷则 -v 会删卷)"
docker compose down --remove-orphans -v 2>/dev/null || true

echo "==> 删除仍占用名字的 frontcrm 容器（若有）"
for n in frontcrm-backend frontcrm-frontend frontcrm-postgres; do
  docker rm -f "$n" 2>/dev/null || true
done

echo "==> 尝试删除旧 compose 遗留的 postgres 卷（名称因项目名可能不同）"
docker volume ls -q | grep -E 'postgres_data|frontcrm' | while read -r vol; do
  echo "    docker volume rm -f $vol"
  docker volume rm -f "$vol" 2>/dev/null || true
done

echo "==> 完成。请确认同目录存在 .env（含 CONNECTION_STRING），再："
echo "    docker compose build --no-cache"
echo "    docker compose up -d"
