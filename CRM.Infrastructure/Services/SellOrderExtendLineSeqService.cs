using System.Data;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Services;

public sealed class SellOrderExtendLineSeqService : ISellOrderExtendLineSeqService
{
    private readonly ApplicationDbContext _db;

    public SellOrderExtendLineSeqService(ApplicationDbContext db) => _db = db;

    public async Task<int> ReserveNextSequenceBlockAsync(string sellOrderId, int count, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellOrderId))
            throw new ArgumentException("销售订单 ID 不能为空", nameof(sellOrderId));
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

            // 单次 upsert：避免 INSERT DO NOTHING + UPDATE 在部分库/权限下 UPDATE 未命中导致 RETURNING 为空
            cmd.CommandText = """
INSERT INTO sellorderextend ("SellOrderId", last_item_line_seq, "CreateTime", "ModifyTime")
VALUES (@sid, @cnt, @ct, @mod)
ON CONFLICT ("SellOrderId") DO UPDATE SET
  last_item_line_seq = sellorderextend.last_item_line_seq + EXCLUDED.last_item_line_seq,
  "ModifyTime" = EXCLUDED."ModifyTime"
RETURNING last_item_line_seq - @cnt + 1;
""";
            cmd.Parameters.AddWithValue("sid", sellOrderId);
            cmd.Parameters.AddWithValue("ct", now);
            cmd.Parameters.AddWithValue("cnt", count);
            cmd.Parameters.AddWithValue("mod", now);

            // INSERT…RETURNING 用 ExecuteReader 取首行；部分环境下 ExecuteScalar 对 RETURNING 不稳定
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("无法预留销售明细序号。");
            return reader.GetInt32(0);
        }
        finally
        {
            if (!wasOpen)
                await conn.CloseAsync();
        }
    }
}
