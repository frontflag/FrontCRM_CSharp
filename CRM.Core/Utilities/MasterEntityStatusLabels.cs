namespace CRM.Core.Utilities;

/// <summary>客户/供应商主状态展示文案（字段变更日志用）。</summary>
public static class MasterEntityStatusLabels
{
    public static string Format(short status) => status switch
    {
        0 => "新建",
        1 => "新建",
        2 => "待审核",
        10 => "已审核",
        12 => "待财务审核",
        20 => "财务建档",
        -1 => "审核失败",
        _ => status.ToString()
    };
}
