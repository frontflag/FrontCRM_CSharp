using System.Data;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Services;

public sealed class PackingItemLineSeqService : IPackingItemLineSeqService
{
    private readonly ApplicationDbContext _db;

    public PackingItemLineSeqService(ApplicationDbContext db) => _db = db;

    public async Task<int> ReserveNextSequenceBlockAsync(string packingId, int count, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packingId))
            throw new ArgumentException("装箱单 ID 不能为空", nameof(packingId));
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var now = DateTime.UtcNow;
        var conn = _db.Database.GetDbConnection();
        var wasOpen = conn.State == ConnectionState.Open;
        if (!wasOpen)
            await conn.OpenAsync(cancellationToken);

        try
        {
            await using var cmd = (NpgsqlCommand)conn.CreateCommand();
            cmd.CommandText = """
INSERT INTO packing_extend ("PackingId", last_item_line_seq, "CreateTime", "ModifyTime", is_deleted)
VALUES (@pid, @cnt, @ct, @mod, false)
ON CONFLICT ("PackingId") DO UPDATE SET
  last_item_line_seq = packing_extend.last_item_line_seq + EXCLUDED.last_item_line_seq,
  "ModifyTime" = EXCLUDED."ModifyTime"
WHERE packing_extend.is_deleted = false
RETURNING last_item_line_seq - @cnt + 1;
""";
            cmd.Parameters.AddWithValue("pid", packingId.Trim());
            cmd.Parameters.AddWithValue("cnt", count);
            cmd.Parameters.AddWithValue("ct", now);
            cmd.Parameters.AddWithValue("mod", now);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("无法预留装箱明细序号（装箱单不存在或未落库）。");
            return reader.GetInt32(0);
        }
        finally
        {
            if (!wasOpen)
                await conn.CloseAsync();
        }
    }
}
