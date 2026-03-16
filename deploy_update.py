#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""FrontCRM 前端部署脚本 - 使用 SCP/SSH"""

import subprocess
import sys
import os

# 配置
SERVER = "129.226.161.3"
USERNAME = "ubuntu"
PASSWORD = "xl@Front#1729"
LOCAL_DIST_PATH = "d:/MyProject/FrontCRM_CSharp/CRM.Web/dist"
REMOTE_PATH = "/home/ubuntu/frontcrm_deploy/CRM.Web/dist"

def run_command(cmd, input_text=None):
    """运行命令并返回结果"""
    try:
        result = subprocess.run(
            cmd,
            shell=True,
            capture_output=True,
            text=True,
            input=input_text,
            timeout=300
        )
        return result.returncode, result.stdout, result.stderr
    except subprocess.TimeoutExpired:
        return -1, "", "命令超时"
    except Exception as e:
        return -1, "", str(e)

def deploy():
    print("=" * 60)
    print("FrontCRM 前端自动部署")
    print("=" * 60)
    print(f"\n服务器: {SERVER}")
    print(f"用户: {USERNAME}")
    print(f"本地路径: {LOCAL_DIST_PATH}")
    print(f"远程路径: {REMOTE_PATH}")
    print()
    
    # 检查本地dist目录
    if not os.path.exists(LOCAL_DIST_PATH):
        print(f"✗ 错误: 本地dist目录不存在: {LOCAL_DIST_PATH}")
        sys.exit(1)
    
    # 步骤1: 上传文件
    print("[1/2] 正在上传前端文件到服务器...")
    print("      (这可能需要1-2分钟)")
    
    # 使用 scp 命令
    scp_cmd = f'scp -r -o StrictHostKeyChecking=no -o ConnectTimeout=30 "{LOCAL_DIST_PATH}\\*" {USERNAME}@{SERVER}:{REMOTE_PATH}'
    
    # 使用 echo 管道传递密码
    returncode, stdout, stderr = run_command(scp_cmd, input_text=PASSWORD + "\n")
    
    if returncode != 0:
        print(f"✗ SCP 上传失败")
        print(f"错误: {stderr}")
        print(f"\n请手动执行:")
        print(f"  {scp_cmd}")
        print(f"密码: {PASSWORD}")
        sys.exit(1)
    
    print("✓ 文件上传成功")
    
    # 步骤2: 重启容器
    print("\n[2/2] 正在重启前端容器...")
    
    ssh_cmd = f'ssh -o StrictHostKeyChecking=no -o ConnectTimeout=30 {USERNAME}@{SERVER} "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend"'
    returncode, stdout, stderr = run_command(ssh_cmd, input_text=PASSWORD + "\n")
    
    if returncode != 0:
        print(f"✗ SSH 命令失败")
        print(f"错误: {stderr}")
        print(f"\n请手动执行:")
        print(f"  {ssh_cmd}")
        print(f"密码: {PASSWORD}")
        sys.exit(1)
    
    print("✓ 前端容器已重启")
    if stdout:
        print(f"\n输出:\n{stdout}")
    
    print("\n" + "=" * 60)
    print("部署完成!")
    print("=" * 60)
    print(f"\n访问地址: http://{SERVER}")
    print("\n提示: 如果页面仍显示旧样式，请按 Ctrl+F5 强制刷新浏览器缓存")
    print()

if __name__ == "__main__":
    deploy()
