[帮助文档目录](../帮助文档目录.md)

# 员工管理

## 页面功能

- 员工账号、角色、部门。
- **SuperAdmin 账号对 Admin / Manager 不可见**；Manager 仅可维护普通员工，不可创建 Manager。
- Admin 可新建 Manager（SYS_BIZ_MANAGER）；不可见 / 不可维护 SuperAdmin。
- SuperAdmin 账号不可在本页重置密码；持有该身份者可通过隐蔽运维页自助改密，或由运维执行数据库 SQL。

## 操作说明

<div class="help-op-block">

**账号维护**

**说明：** 支持新建、编辑员工；删除需二次确认（通用列表操作）。按当前登录管理角色限制可赋角色与目标账号范围。

</div>

<div class="help-op-block">

**模拟登录**

**说明：** 以所选行用户身份进入系统（用于排障或代操作）。

**前置条件：** 当前登录用户为 SuperAdmin（系统管理员）；目标账号已启用且非本人。

</div>
