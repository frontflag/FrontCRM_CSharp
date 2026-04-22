using System.Data;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Services;

public sealed class StockExtendLineSeqService : IStockExtendLineSeqService
{
    private readonly ApplicationDbContext _db;

    public StockExtendLineSeqService(ApplicationDbContext db) => _db = db;

    public async Task<int> ReserveNextSequenceBlockAsync(string stockId, int count, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockId))
            throw new ArgumentException("库存分桶 ID 不能为空", nameof(stockId));
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
INSERT INTO stock_extend ("StockId", last_item_line_seq, "CreateTime", "ModifyTime")
VALUES (@sid, @cnt, @ct, @mod)
ON CONFLICT ("StockId") DO UPDATE SET
  last_item_line_seq = stock_extend.last_item_line_seq + EXCLUDED.last_item_line_seq,
  "ModifyTime" = EXCLUDED."ModifyTime"
RETURNING last_item_line_seq - @cnt + 1;
""";
            cmd.Parameters.AddWithValue("sid", stockId);
            cmd.Parameters.AddWithValue("ct", now);
            cmd.Parameters.AddWithValue("cnt", count);
            cmd.Parameters.AddWithValue("mod", now);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("无法预留在库明细序号。");
            return reader.GetInt32(0);
        }
        finally
        {
            if (!wasOpen)
                await conn.CloseAsync();
        }
    }
}
