namespace CRM.Core.Models.System;

/// <summary>登录方式，与表 log_login.LoginMethod（smallint）一致。</summary>
public enum LoginMethod : short
{
    Password = 1,
    Impersonate = 2,
    Wechat = 3
}
