#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""FrontCRM 前端部署脚本"""

import paramiko
import os
import sys

# 配置
SERVER = "129.226.161.3"
USERNAME = "ubuntu"
PASSWORD = "xl@Front#1729"
LOCAL_DIST_PATH = "d:/MyProject/FrontCRM_CSharp/CRM.Web/dist"
REMOTE_PATH = "/home/ubuntu/frontcrm_deploy/CRM.Web/dist"

def deploy():
    print("=" * 50)
    print("FrontCRM 前端自动部署")
    print("=" * 50)
    print(f"\n服务器: {SERVER}")
    print(f"用户: {USERNAME}")
    print(f"本地路径: {LOCAL_DIST_PATH}")
    print(f"远程路径: {REMOTE_PATH}")
    print()
    
    try:
        # 创建 SSH 客户端
        print("[1/3] 连接服务器...")
        ssh = paramiko.SSHClient()
        ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
        ssh.connect(SERVER, username=USERNAME, password=PASSWORD, timeout=30)
        print("✓ SSH 连接成功")
        
        # 创建 SFTP 客户端
        print("\n[2/3] 上传前端文件...")
        sftp = ssh.open_sftp()
        
        # 递归上传目录
        local_path = os.path.normpath(LOCAL_DIST_PATH)
        remote_path = REMOTE_PATH
        
        # 确保远程目录存在
        try:
            sftp.stat(remote_path)
        except FileNotFoundError:
            print(f"创建远程目录: {remote_path}")
            ssh.exec_command(f"mkdir -p {remote_path}")
        
        # 上传文件
        file_count = 0
        for root, dirs, files in os.walk(local_path):
            for file in files:
                local_file = os.path.join(root, file)
                relative_path = os.path.relpath(local_file, local_path)
                remote_file = f"{remote_path}/{relative_path}".replace("\\", "/")
                
                # 确保远程子目录存在
                remote_dir = os.path.dirname(remote_file)
                ssh.exec_command(f"mkdir -p {remote_dir}")
                
                sftp.put(local_file, remote_file)
                file_count += 1
                if file_count % 10 == 0:
                    print(f"  已上传 {file_count} 个文件...")
        
        print(f"✓ 上传完成，共 {file_count} 个文件")
        
        sftp.close()
        
        # 重启前端容器
        print("\n[3/3] 重启前端容器...")
        stdin, stdout, stderr = ssh.exec_command(
            "cd /home/ubuntu/frontcrm_deploy && docker-compose restart frontend"
        )
        output = stdout.read().decode()
        error = stderr.read().decode()
        
        if error:
            print(f"警告: {error}")
        print(f"✓ 容器重启完成")
        print(f"\n输出:\n{output}")
        
        ssh.close()
        
        print("\n" + "=" * 50)
        print("部署完成!")
        print("=" * 50)
        print(f"\n访问地址: http://{SERVER}")
        print()
        
    except Exception as e:
        print(f"\n✗ 部署失败: {e}")
        sys.exit(1)

if __name__ == "__main__":
    deploy()
