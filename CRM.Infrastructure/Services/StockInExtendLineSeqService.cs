using System.Data;
using CRM.Core.Interfaces;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CRM.Infrastructure.Services;

public sealed class StockInExtendLineSeqService : IStockInExtendLineSeqService
{
    private readonly ApplicationDbContext _db;

    public StockInExtendLineSeqService(ApplicationDbContext db) => _db = db;

    public async Task<int> ReserveNextSequenceBlockAsync(string stockInId, int count, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stockInId))
            throw new ArgumentException("入库单 ID 不能为空", nameof(stockInId));
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
INSERT INTO stock_in_extend ("StockInId", last_item_line_seq, "CreateTime", "ModifyTime")
VALUES (@sid, @cnt, @ct, @mod)
ON CONFLICT ("StockInId") DO UPDATE SET
  last_item_line_seq = stock_in_extend.last_item_line_seq + EXCLUDED.last_item_line_seq,
  "ModifyTime" = EXCLUDED."ModifyTime"
RETURNING last_item_line_seq - @cnt + 1;
""";
            cmd.Parameters.AddWithValue("sid", stockInId);
            cmd.Parameters.AddWithValue("ct", now);
            cmd.Parameters.AddWithValue("cnt", count);
            cmd.Parameters.AddWithValue("mod", now);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("无法预留入库明细序号。");
            return reader.GetInt32(0);
        }
        finally
        {
            if (!wasOpen)
                await conn.CloseAsync();
        }
    }
}
