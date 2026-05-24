using Npgsql;

var connString =
    Environment.GetEnvironmentVariable("CRM_DB_CONNECTION")
    ?? "Host=localhost;Port=5432;Database=FrontCRM;Username=postgres;Password=1234";

var scriptNames = args.Length > 0
    ? args
    : new[] { "add_packing_picking_item_code_postgresql.sql", "add_picking_task_packing_centric_postgresql.sql" };

var scriptsDir = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), "scripts"),
    Directory.GetCurrentDirectory(),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
}.FirstOrDefault(Directory.Exists) ?? Directory.GetCurrentDirectory();

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

foreach (var name in scriptNames)
{
    var sqlPath = Path.Combine(scriptsDir, name);
    if (!File.Exists(sqlPath))
    {
        Console.Error.WriteLine($"SQL file not found: {sqlPath}");
        return 1;
    }

    var sql = await File.ReadAllTextAsync(sqlPath);
    Console.WriteLine($"Applying: {sqlPath}");
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("Done.");
return 0;
