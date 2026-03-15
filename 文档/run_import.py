import subprocess
import sys

# 运行导入脚本
result = subprocess.run([sys.executable, "import_ebs.py"], 
                       capture_output=True, 
                       text=True, 
                       cwd="d:\\MyProject\\FrontCRM(C#)\\文档")

print("标准输出:")
print(result.stdout)
print("\n标准错误:")
print(result.stderr)
print(f"\n返回码: {result.returncode}")