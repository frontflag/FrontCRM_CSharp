namespace CRM.Core.Interfaces;

/// <summary>由 <see cref="CRM.Infrastructure.Repositories.Repository{T}"/> 识别：DeleteAsync 改为标记删除而非物理删除。</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
