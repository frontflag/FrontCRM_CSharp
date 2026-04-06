namespace CRM.Core.Interfaces
{
    /// <summary>销售主单扩展表 <c>last_item_line_seq</c> 的并发安全序号预留（Infrastructure 实现）。</summary>
    public interface ISellOrderExtendLineSeqService
    {
        /// <summary>预留连续 <paramref name="count"/> 个 Item 序号，返回本块第一个序号（含）。</summary>
        Task<int> ReserveNextSequenceBlockAsync(string sellOrderId, int count, CancellationToken cancellationToken = default);
    }

    /// <summary>采购主单扩展表 <c>last_item_line_seq</c> 的并发安全序号预留（Infrastructure 实现）。</summary>
    public interface IPurchaseOrderExtendLineSeqService
    {
        /// <summary>预留连续 <paramref name="count"/> 个 Item 序号，返回本块第一个序号（含）。</summary>
        Task<int> ReserveNextSequenceBlockAsync(string purchaseOrderId, int count, CancellationToken cancellationToken = default);
    }
}
