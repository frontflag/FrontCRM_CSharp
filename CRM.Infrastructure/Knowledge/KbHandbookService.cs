using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Knowledge;
using CRM.Infrastructure.Ai;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Knowledge;

public sealed class KbHandbookService : IKbHandbookService
{
    private const string Disclaimer = "以下内容来自培训教材：";
    private const string LegacyDisclaimer = "以下内容来自新人培训教材，不是公司制度。";
    private const string Compliance = "涉及合规、假货或诈骗的筛查，以公司合规要求为准。";
    private const string Uncovered = "教材未覆盖该问题。";

    private readonly ApplicationDbContext _db;
    private readonly IKbEmbeddingClient _embeddingClient;
    private readonly IAiSecretResolver _secrets;
    private readonly IAiOrchestrator _orchestrator;
    private readonly IRbacService _rbac;

    public KbHandbookService(
        ApplicationDbContext db,
        IKbEmbeddingClient embeddingClient,
        IAiSecretResolver secrets,
        IAiOrchestrator orchestrator,
        IRbacService rbac)
    {
        _db = db;
        _embeddingClient = embeddingClient;
        _secrets = secrets;
        _orchestrator = orchestrator;
        _rbac = rbac;
    }

    public async Task EnsurePermissionAsync(string? userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var perm = (permissionCode ?? "").Trim();
        if (perm.Length == 0)
            return;
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("未登录。");
        var summary = await _rbac.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return;
        if (summary.PermissionCodes.Any(c => string.Equals(c, perm, StringComparison.OrdinalIgnoreCase)))
            return;
        throw new InvalidOperationException("当前账号无权使用培训问答。");
    }

    private async Task EnsureReadAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("未登录。");
        var summary = await _rbac.GetUserPermissionSummaryAsync(userId.Trim());
        if (summary.IsSysAdmin)
            return;
        if (summary.PermissionCodes.Any(c =>
                string.Equals(c, KbHandbookCodes.AskPermission, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c, KbHandbookCodes.AdminPermission, StringComparison.OrdinalIgnoreCase)))
            return;
        throw new InvalidOperationException("当前账号无权浏览培训教材。");
    }

    public async Task<KbImportResultDto> EnqueueDocxAsync(
        string storagePath,
        string fileName,
        string documentCode,
        string title,
        CancellationToken cancellationToken = default)
    {
        var sha = await Sha256FileAsync(storagePath, cancellationToken);
        var conn = await OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var documentId = await ScalarAsync(conn, tx, """
            SELECT id FROM kb_document WHERE code = @code AND is_deleted = false
            """, cancellationToken, P("@code", documentCode));
        if (string.IsNullOrEmpty(documentId))
        {
            documentId = Guid.NewGuid().ToString();
            await ExecAsync(conn, tx, """
                INSERT INTO kb_document (id, code, title)
                VALUES (@id, @code, @title)
                """, cancellationToken,
                P("@id", documentId), P("@code", documentCode), P("@title", title));
        }

        var dup = await ScalarAsync(conn, tx, """
            SELECT id FROM kb_document_version
            WHERE document_id = @doc AND source_sha256 = @sha AND status = 2 AND is_deleted = false
            """, cancellationToken, P("@doc", documentId), P("@sha", sha));
        if (!string.IsNullOrEmpty(dup))
            throw new InvalidOperationException("同一份教材已经导入过。");

        var maxNo = await ScalarAsync(conn, tx, """
            SELECT COALESCE(MAX(version_no), 0)::text FROM kb_document_version WHERE document_id = @doc
            """, cancellationToken, P("@doc", documentId));
        var versionNo = int.Parse(maxNo ?? "0", CultureInfo.InvariantCulture) + 1;
        var versionId = Guid.NewGuid().ToString();
        await ExecAsync(conn, tx, """
            INSERT INTO kb_document_version (
                id, document_id, version_no, source_file_name, source_sha256, storage_path, status)
            VALUES (@id, @doc, @no, @name, @sha, @path, 0)
            """, cancellationToken,
            P("@id", versionId), P("@doc", documentId), P("@no", versionNo),
            P("@name", fileName), P("@sha", sha), P("@path", storagePath));
        await tx.CommitAsync(cancellationToken);
        return new KbImportResultDto
        {
            DocumentId = documentId,
            VersionId = versionId,
            VersionNo = versionNo,
            Status = KbHandbookCodes.StatusPending
        };
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var conn = await OpenAsync(cancellationToken);
        var versionId = await ScalarAsync(conn, null, """
            SELECT id FROM kb_document_version
            WHERE is_deleted = false AND status IN (0, 1, 3)
            ORDER BY create_time
            LIMIT 1
            """, cancellationToken);
        if (string.IsNullOrEmpty(versionId))
            return;

        try
        {
            await ProcessVersionAsync(conn, versionId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await ExecAsync(conn, null, """
                UPDATE kb_document_version
                SET status = 3, error_message = @err, "modify_time" = now()
                WHERE id = @id
                """, cancellationToken, P("@id", versionId), P("@err", ex.Message));
        }
    }

    public async Task ActivateAsync(string versionId, CancellationToken cancellationToken = default)
    {
        var conn = await OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        var docId = await ScalarAsync(conn, tx, """
            SELECT document_id FROM kb_document_version
            WHERE id = @id AND status = 2 AND is_deleted = false
            """, cancellationToken, P("@id", versionId));
        if (string.IsNullOrEmpty(docId))
            throw new InvalidOperationException("只有已就绪的版本可以启用。");
        await ExecAsync(conn, tx, """
            UPDATE kb_document_version SET is_active = false, "modify_time" = now()
            WHERE document_id = @doc AND is_active = true AND id <> @id
            """, cancellationToken, P("@doc", docId), P("@id", versionId));
        await ExecAsync(conn, tx, """
            UPDATE kb_document_version SET is_active = true, "modify_time" = now() WHERE id = @id
            """, cancellationToken, P("@id", versionId));
        await ExecAsync(conn, tx, """
            UPDATE kb_document SET active_version_id = @id, "modify_time" = now() WHERE id = @doc
            """, cancellationToken, P("@id", versionId), P("@doc", docId));
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KbDocumentVersionDto>> ListVersionsAsync(
        string documentCode,
        CancellationToken cancellationToken = default)
    {
        var conn = await OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT d.id, d.code, d.title, v.id, v.version_no, v.status, v.is_active, v.chunk_count, v.error_message, v.source_file_name
            FROM kb_document d
            JOIN kb_document_version v ON v.document_id = d.id AND v.is_deleted = false
            WHERE d.code = @code AND d.is_deleted = false
            ORDER BY v.version_no DESC
            """;
        cmd.Parameters.Add(P("@code", documentCode));
        var rows = new List<KbDocumentVersionDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new KbDocumentVersionDto
            {
                DocumentId = reader.GetString(0),
                DocumentCode = reader.GetString(1),
                Title = reader.GetString(2),
                VersionId = reader.GetString(3),
                VersionNo = reader.GetInt32(4),
                Status = reader.GetInt16(5),
                IsActive = reader.GetBoolean(6),
                ChunkCount = reader.GetInt32(7),
                ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
                SourceFileName = reader.GetString(9)
            });
        }

        return rows;
    }

    public async Task<IReadOnlyList<KbChunkListItemDto>> ListChunksAsync(
        string versionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var conn = await OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, chunk_index, heading, chapter_no, section_no, content_chars, left(content, 240)
            FROM kb_chunk
            WHERE document_version_id = @id
            ORDER BY chunk_index
            OFFSET @off LIMIT @lim
            """;
        cmd.Parameters.Add(P("@id", versionId));
        cmd.Parameters.Add(P("@off", (page - 1) * pageSize));
        cmd.Parameters.Add(P("@lim", pageSize));
        var rows = new List<KbChunkListItemDto>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new KbChunkListItemDto
            {
                Id = reader.GetString(0),
                ChunkIndex = reader.GetInt32(1),
                Heading = reader.GetString(2),
                ChapterNo = reader.IsDBNull(3) ? null : reader.GetString(3),
                SectionNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                ContentChars = reader.GetInt32(5),
                Excerpt = reader.IsDBNull(6) ? "" : reader.GetString(6)
            });
        }

        return rows;
    }

    public async Task<KbHandbookReaderDto> GetReaderAsync(
        string userId,
        string? versionId,
        CancellationToken cancellationToken = default)
    {
        await EnsureReadAsync(userId);
        var conn = await OpenAsync(cancellationToken);
        string id;
        int versionNo;
        string title;
        if (string.IsNullOrWhiteSpace(versionId))
        {
            await using var activeCmd = conn.CreateCommand();
            activeCmd.CommandText = """
                SELECT v.id, v.version_no, d.title
                FROM kb_document_version v
                JOIN kb_document d ON d.id = v.document_id
                WHERE v.is_active = true AND v.status = 2 AND v.is_deleted = false AND d.is_deleted = false
                ORDER BY v."modify_time" DESC NULLS LAST
                LIMIT 1
                """;
            await using var activeReader = await activeCmd.ExecuteReaderAsync(cancellationToken);
            if (!await activeReader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("还没有启用的教材版本。");
            id = activeReader.GetString(0);
            versionNo = activeReader.GetInt32(1);
            title = activeReader.GetString(2);
        }
        else
        {
            await using var verCmd = conn.CreateCommand();
            verCmd.CommandText = """
                SELECT v.id, v.version_no, d.title
                FROM kb_document_version v
                JOIN kb_document d ON d.id = v.document_id
                WHERE v.id = @id AND v.status = 2 AND v.is_deleted = false AND d.is_deleted = false
                """;
            verCmd.Parameters.Add(P("@id", versionId.Trim()));
            await using var verReader = await verCmd.ExecuteReaderAsync(cancellationToken);
            if (!await verReader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("教材版本不存在或尚未就绪。");
            id = verReader.GetString(0);
            versionNo = verReader.GetInt32(1);
            title = verReader.GetString(2);
        }

        var pieces = await LoadPiecesAsync(conn, id, cancellationToken);
        return new KbHandbookReaderDto
        {
            VersionId = id,
            VersionNo = versionNo,
            Title = title,
            Chapters = BuildChapters(pieces)
        };
    }

    public async Task<KbAskResultDto> AskAsync(
        string userId,
        string question,
        CancellationToken cancellationToken = default,
        string? dialogueContext = null)
    {
        await EnsurePermissionAsync(userId, KbHandbookCodes.AskPermission, cancellationToken);
        var normalized = HandbookChunker.Normalize(question);
        if (normalized.Length is < 1 or > 500)
            throw new InvalidOperationException("问题长度需要在 1 到 500 字之间。");
        var useCache = string.IsNullOrWhiteSpace(dialogueContext);
        var questionForModel = normalized;
        if (!useCache)
        {
            var prior = dialogueContext!.Trim();
            if (prior.Length > 4000)
                prior = prior[^4000..];
            questionForModel = normalized + "\n\n【本轮已有对话，仅供理解指代】\n" + prior;
        }

        var conn = await OpenAsync(cancellationToken);
        await using var activeCmd = conn.CreateCommand();
        activeCmd.CommandText = """
            SELECT v.id, v.version_no, d.title
            FROM kb_document_version v
            JOIN kb_document d ON d.id = v.document_id
            WHERE v.is_active = true AND v.status = 2 AND v.is_deleted = false AND d.is_deleted = false
            ORDER BY v."modify_time" DESC NULLS LAST
            LIMIT 1
            """;
        await using var activeReader = await activeCmd.ExecuteReaderAsync(cancellationToken);
        if (!await activeReader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("还没有启用的教材版本。");
        var versionId = activeReader.GetString(0);
        var versionNo = activeReader.GetInt32(1);
        var title = activeReader.GetString(2);
        await activeReader.DisposeAsync();

        var questionHash = AiJsonHelper.ComputeSha256Hex(normalized);
        if (useCache)
        {
            var cached = await ReadCacheAsync(conn, versionId, questionHash, cancellationToken);
            if (cached != null)
            {
                if (cached.Covered)
                    cached.Answer = WithDisclaimer(cached.Answer);
                cached.DocumentTitle = title;
                cached.VersionId = versionId;
                cached.VersionNo = versionNo;
                cached.FromCache = true;
                return cached;
            }
        }

        var profile = await LoadProfileAsync(conn, cancellationToken);
        var queryVector = await EmbedOneAsync(profile, normalized, cancellationToken);
        var hits = await SearchAsync(conn, versionId, queryVector, cancellationToken);
        var maxTop = await ConfigDoubleAsync(conn, "kb_handbook_max_top_distance", KbHandbookCodes.DefaultMaxTopDistance, cancellationToken);
        var maxChunk = await ConfigDoubleAsync(conn, "kb_handbook_max_chunk_distance", KbHandbookCodes.DefaultMaxChunkDistance, cancellationToken);
        if (hits.Count == 0 || hits[0].Distance > maxTop)
        {
            var missed = new KbAskResultDto
            {
                Covered = false,
                Answer = Uncovered,
                DocumentTitle = title,
                VersionId = versionId,
                VersionNo = versionNo
            };
            if (useCache)
                await WriteCacheAsync(conn, versionId, questionHash, normalized, missed, hits.FirstOrDefault()?.Distance, cancellationToken);
            return missed;
        }

        var selected = hits.Where(h => h.Distance <= maxChunk).Take(KbHandbookCodes.TopK).ToList();
        if (selected.Count == 0)
            selected.Add(hits[0]);
        var compliance = selected.Any(h => h.ChapterNo is "15" or "16");
        var context = string.Join("\n\n", selected.Select((h, i) => $"[{i + 1}] {h.Heading}\n{h.Content}"));
        var invoked = await _orchestrator.InvokeAsync(new AiInvokeRequestDto
        {
            ScenarioCode = KbHandbookCodes.Scenario,
            Input = new Dictionary<string, string?>
            {
                ["question"] = questionForModel,
                ["corpus_version_id"] = versionId,
                ["chunk_ids"] = string.Join(',', selected.Select(h => h.Id)),
                ["context"] = context,
                ["compliance_hint"] = compliance ? Compliance : ""
            }
        }, userId, cancellationToken);

        var covered = false;
        var answer = "";
        var parsed = AiJsonHelper.TryParseJsonObject(invoked.Content);
        if (parsed is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("covered", out var coveredNode))
                covered = coveredNode.ValueKind == JsonValueKind.True;
            if (element.TryGetProperty("answer", out var answerNode) && answerNode.ValueKind == JsonValueKind.String)
                answer = answerNode.GetString() ?? "";
        }

        if (!covered || string.IsNullOrWhiteSpace(answer))
        {
            var missed = new KbAskResultDto
            {
                Covered = false,
                Answer = Uncovered,
                DocumentTitle = title,
                VersionId = versionId,
                VersionNo = versionNo
            };
            if (useCache)
                await WriteCacheAsync(conn, versionId, questionHash, normalized, missed, hits[0].Distance, cancellationToken);
            return missed;
        }

        var text = WithDisclaimer(answer);
        if (compliance && !text.Contains(Compliance, StringComparison.Ordinal))
            text += "\n" + Compliance;
        var result = new KbAskResultDto
        {
            Covered = true,
            Answer = text,
            DocumentTitle = title,
            VersionId = versionId,
            VersionNo = versionNo,
            Citations = DedupeCitations(selected)
        };
        if (useCache)
            await WriteCacheAsync(conn, versionId, questionHash, normalized, result, hits[0].Distance, cancellationToken);
        return result;
    }

    private static string WithDisclaimer(string answer)
    {
        var body = answer.Trim();
        foreach (var prefix in new[] { LegacyDisclaimer, Disclaimer })
        {
            if (!body.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            body = body[prefix.Length..].TrimStart('\r', '\n', ' ');
            break;
        }
        return string.IsNullOrEmpty(body) ? Disclaimer : Disclaimer + "\n" + body;
    }

    private async Task ProcessVersionAsync(DbConnection conn, string versionId, CancellationToken cancellationToken)
    {
        await ExecAsync(conn, null, """
            UPDATE kb_document_version SET status = 1, error_message = null, "modify_time" = now() WHERE id = @id
            """, cancellationToken, P("@id", versionId));

        var chunkCount = int.Parse(await ScalarAsync(conn, null, """
            SELECT COUNT(*)::text FROM kb_chunk WHERE document_version_id = @id
            """, cancellationToken, P("@id", versionId)) ?? "0", CultureInfo.InvariantCulture);
        if (chunkCount == 0)
        {
            var path = await ScalarAsync(conn, null, """
                SELECT storage_path FROM kb_document_version WHERE id = @id
                """, cancellationToken, P("@id", versionId));
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new InvalidOperationException("教材文件不存在。");
            await using var stream = File.OpenRead(path);
            var paragraphs = HandbookDocxReader.ReadParagraphs(stream);
            var chunks = HandbookChunker.Chunk(paragraphs);
            if (chunks.Count == 0)
                throw new InvalidOperationException("教材没有切出任何块。");
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            foreach (var chunk in chunks)
            {
                await ExecAsync(conn, tx, """
                    INSERT INTO kb_chunk (
                        id, document_version_id, chunk_index, chapter_no, chapter_title,
                        section_no, section_title, heading, content, content_chars, content_sha256)
                    VALUES (
                        @id, @ver, @idx, @cno, @ctitle, @sno, @stitle, @heading, @content, @chars, @sha)
                    """, cancellationToken,
                    P("@id", Guid.NewGuid().ToString()),
                    P("@ver", versionId),
                    P("@idx", chunk.ChunkIndex),
                    P("@cno", chunk.ChapterNo),
                    P("@ctitle", chunk.ChapterTitle),
                    P("@sno", chunk.SectionNo),
                    P("@stitle", chunk.SectionTitle),
                    P("@heading", chunk.Heading),
                    P("@content", chunk.Content),
                    P("@chars", chunk.ContentChars),
                    P("@sha", chunk.ContentSha256));
            }

            await ExecAsync(conn, tx, """
                UPDATE kb_document_version SET chunk_count = @n, "modify_time" = now() WHERE id = @id
                """, cancellationToken, P("@n", chunks.Count), P("@id", versionId));
            await tx.CommitAsync(cancellationToken);
        }

        var profile = await LoadProfileAsync(conn, cancellationToken);
        while (true)
        {
            var batch = await LoadUnembeddedAsync(conn, versionId, cancellationToken);
            if (batch.Count == 0)
                break;
            var vectors = string.Equals(profile.ProviderCode, AiProviderCodes.Mock, StringComparison.OrdinalIgnoreCase)
                ? batch.Select(b => MockKbEmbedding.Vector(b.Content)).ToList()
                : (await _embeddingClient.EmbedAsync(profile.BaseUrl, profile.ApiKey, profile.Model, batch.Select(b => b.Content).ToList(), cancellationToken)).ToList();
            for (var i = 0; i < batch.Count; i++)
            {
                await ExecAsync(conn, null, """
                    UPDATE kb_chunk SET embedding = CAST(@vec AS vector) WHERE id = @id
                    """, cancellationToken, P("@vec", ToVectorLiteral(vectors[i])), P("@id", batch[i].Id));
            }
        }

        await ExecAsync(conn, null, """
            UPDATE kb_document_version
            SET status = 2, error_message = null, "modify_time" = now(),
                embedding_provider_code = @provider, embedding_model = @model, embedding_dimension = @dim
            WHERE id = @id
            """, cancellationToken,
            P("@provider", profile.ProviderCode), P("@model", profile.Model),
            P("@dim", KbHandbookCodes.EmbeddingDimension), P("@id", versionId));

        var docId = await ScalarAsync(conn, null, """
            SELECT document_id FROM kb_document_version WHERE id = @id
            """, cancellationToken, P("@id", versionId));
        var active = await ScalarAsync(conn, null, """
            SELECT id FROM kb_document_version
            WHERE document_id = @doc AND is_active = true AND is_deleted = false AND id <> @id
            """, cancellationToken, P("@doc", docId), P("@id", versionId));
        if (string.IsNullOrEmpty(active))
            await ActivateAsync(versionId, cancellationToken);
    }

    private async Task<EmbeddingProfile> LoadProfileAsync(DbConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT p.provider_code, p.model, p.is_enabled, COALESCE(a.base_url, ''), COALESCE(a.api_key_env, '')
            FROM kb_embedding_profile p
            LEFT JOIN ai_provider a ON a.code = p.provider_code AND a.is_deleted = false
            WHERE p.is_deleted = false
            LIMIT 1
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("还没有配置向量模型。");
        var enabled = reader.GetBoolean(2);
        var provider = reader.GetString(0);
        var model = reader.GetString(1);
        var baseUrl = reader.GetString(3);
        var env = reader.GetString(4);
        if (!enabled)
            throw new InvalidOperationException("向量模型未启用。");
        if (!string.Equals(provider, AiProviderCodes.Mock, StringComparison.OrdinalIgnoreCase))
        {
            var key = _secrets.ResolveApiKey(env);
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException($"向量密钥未配置。请在启动 API 的进程中设置环境变量 {env}。");
            return new EmbeddingProfile(provider, model, baseUrl, key);
        }

        return new EmbeddingProfile(provider, model, baseUrl, "");
    }

    private async Task<float[]> EmbedOneAsync(EmbeddingProfile profile, string text, CancellationToken cancellationToken)
    {
        if (string.Equals(profile.ProviderCode, AiProviderCodes.Mock, StringComparison.OrdinalIgnoreCase))
            return MockKbEmbedding.Vector(text);
        var vectors = await _embeddingClient.EmbedAsync(profile.BaseUrl, profile.ApiKey, profile.Model, new[] { text }, cancellationToken);
        return vectors[0];
    }

    private async Task<List<Hit>> SearchAsync(DbConnection conn, string versionId, float[] vector, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, heading, content, chapter_no, chapter_title, section_no, section_title,
                   (embedding <=> CAST(@q AS vector))
            FROM kb_chunk
            WHERE document_version_id = @ver AND embedding IS NOT NULL
            ORDER BY embedding <=> CAST(@q AS vector)
            LIMIT 5
            """;
        cmd.Parameters.Add(P("@ver", versionId));
        cmd.Parameters.Add(P("@q", ToVectorLiteral(vector)));
        var hits = new List<Hit>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hits.Add(new Hit(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetDouble(7)));
        }

        return hits;
    }

    private static async Task<List<ChunkRow>> LoadUnembeddedAsync(DbConnection conn, string versionId, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, content FROM kb_chunk
            WHERE document_version_id = @id AND embedding IS NULL
            ORDER BY chunk_index
            LIMIT 16
            """;
        cmd.Parameters.Add(P("@id", versionId));
        var rows = new List<ChunkRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new ChunkRow(reader.GetString(0), reader.GetString(1)));
        return rows;
    }

    private static async Task<KbAskResultDto?> ReadCacheAsync(
        DbConnection conn, string versionId, string questionHash, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT covered, answer, chunk_ids::text, top_distance
            FROM kb_ask_cache
            WHERE document_version_id = @ver AND question_sha256 = @sha AND expire_time > now()
            """;
        cmd.Parameters.Add(P("@ver", versionId));
        cmd.Parameters.Add(P("@sha", questionHash));
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;
        var covered = reader.GetBoolean(0);
        var answer = reader.GetString(1);
        var ids = reader.IsDBNull(2) ? "[]" : reader.GetString(2);
        await reader.DisposeAsync();
        return new KbAskResultDto
        {
            Covered = covered,
            Answer = answer,
            Citations = covered
                ? await LoadCachedCitationsAsync(conn, ids, cancellationToken)
                : new List<KbCitationDto>()
        };
    }

    private static async Task WriteCacheAsync(
        DbConnection conn,
        string versionId,
        string questionHash,
        string question,
        KbAskResultDto result,
        double? topDistance,
        CancellationToken cancellationToken)
    {
        var ids = JsonSerializer.Serialize(result.Citations.Select(c => c.ChunkId).Where(id => id.Length > 0).ToList());
        await ExecAsync(conn, null, """
            INSERT INTO kb_ask_cache (
                id, document_version_id, question_sha256, question_norm, covered, answer, chunk_ids, top_distance, expire_time)
            VALUES (
                @id, @ver, @sha, @q, @covered, @answer, CAST(@ids AS jsonb), @dist, now() + interval '7 days')
            ON CONFLICT (document_version_id, question_sha256) DO UPDATE
            SET covered = EXCLUDED.covered, answer = EXCLUDED.answer, chunk_ids = EXCLUDED.chunk_ids,
                top_distance = EXCLUDED.top_distance, expire_time = EXCLUDED.expire_time
            """, cancellationToken,
            P("@id", Guid.NewGuid().ToString()),
            P("@ver", versionId),
            P("@sha", questionHash),
            P("@q", question),
            P("@covered", result.Covered),
            P("@answer", result.Answer),
            P("@ids", ids),
            P("@dist", topDistance));
    }

    private static async Task<double> ConfigDoubleAsync(DbConnection conn, string key, double fallback, CancellationToken cancellationToken)
    {
        var raw = await ScalarAsync(conn, null, """
            SELECT config_value FROM ai_global_config WHERE config_key = @key
            """, cancellationToken, P("@key", key));
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }

    private async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var conn = _db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);
        return conn;
    }

    private static async Task ExecAsync(
        DbConnection conn,
        DbTransaction? tx,
        string sql,
        CancellationToken cancellationToken,
        params DbParameter[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string?> ScalarAsync(
        DbConnection conn,
        DbTransaction? tx,
        string sql,
        CancellationToken cancellationToken,
        params DbParameter[] parameters)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);
        var value = await cmd.ExecuteScalarAsync(cancellationToken);
        return value == null || value is DBNull ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static DbParameter P(string name, object? value)
    {
        var parameter = new Npgsql.NpgsqlParameter(name, value ?? DBNull.Value);
        return parameter;
    }

    private static string ToVectorLiteral(float[] values)
    {
        var sb = new StringBuilder(values.Length * 8);
        sb.Append('[');
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(values[i].ToString("G9", CultureInfo.InvariantCulture));
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static async Task<string> Sha256FileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static KbCitationDto ToCitation(Hit hit) => new()
    {
        ChunkId = hit.Id,
        Heading = hit.Heading,
        ChapterNo = hit.ChapterNo,
        SectionNo = hit.SectionNo,
        Anchor = HandbookAnchor.Section(hit.ChapterNo, hit.Heading, hit.SectionNo, hit.SectionTitle),
        Excerpt = hit.Content.Length <= 240 ? hit.Content : hit.Content[..240],
        Distance = hit.Distance
    };

    private static List<KbCitationDto> DedupeCitations(IEnumerable<Hit> hits) =>
        hits.GroupBy(h => HandbookAnchor.Section(h.ChapterNo, h.Heading, h.SectionNo, h.SectionTitle))
            .Select(group => ToCitation(group.OrderBy(h => h.Distance).First()))
            .ToList();

    private static async Task<List<KbCitationDto>> LoadCachedCitationsAsync(
        DbConnection conn, string json, CancellationToken cancellationToken)
    {
        List<string>? ids;
        try
        {
            ids = JsonSerializer.Deserialize<List<string>>(json);
        }
        catch (JsonException)
        {
            return new List<KbCitationDto>();
        }

        var parsed = (ids ?? new List<string>())
            .Where(id => Guid.TryParse(id, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
        if (parsed.Count == 0)
            return new List<KbCitationDto>();

        await using var cmd = conn.CreateCommand();
        var names = new List<string>();
        for (var i = 0; i < parsed.Count; i++)
        {
            var name = "@c" + i;
            names.Add(name);
            cmd.Parameters.Add(P(name, parsed[i]));
        }

        cmd.CommandText = $"""
            SELECT id, heading, left(content, 240), chapter_no, chapter_title, section_no, section_title
            FROM kb_chunk
            WHERE id IN ({string.Join(", ", names)})
            """;
        var found = new Dictionary<string, KbCitationDto>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hit = new Hit(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                0);
            found[hit.Id] = ToCitation(hit);
        }

        return parsed.Where(found.ContainsKey).Select(id => found[id]).ToList();
    }

    private static async Task<List<Piece>> LoadPiecesAsync(DbConnection conn, string versionId, CancellationToken cancellationToken)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT chunk_index, chapter_no, chapter_title, section_no, section_title, heading, content
            FROM kb_chunk
            WHERE document_version_id = @id
            ORDER BY chunk_index
            """;
        cmd.Parameters.Add(P("@id", versionId));
        var rows = new List<Piece>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new Piece(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6)));
        }

        return rows;
    }

    private static List<KbHandbookChapterDto> BuildChapters(IReadOnlyList<Piece> pieces)
    {
        var chapters = new List<KbHandbookChapterDto>();
        KbHandbookChapterDto? chapter = null;
        KbHandbookSectionDto? section = null;
        var sectionParts = new List<string>();
        string? sectionAnchor = null;

        void FlushSection()
        {
            if (section == null || chapter == null)
                return;
            section.Content = HandbookSectionText.Merge(sectionParts);
            chapter.Sections.Add(section);
            section = null;
            sectionParts.Clear();
        }

        foreach (var piece in pieces)
        {
            var chapterAnchor = HandbookAnchor.Chapter(piece.ChapterNo, piece.Heading);
            if (chapter == null || chapter.Anchor != chapterAnchor)
            {
                FlushSection();
                chapter = new KbHandbookChapterDto
                {
                    Anchor = chapterAnchor,
                    Title = ChapterLabel(piece)
                };
                chapters.Add(chapter);
            }

            var anchor = HandbookAnchor.Section(piece.ChapterNo, piece.Heading, piece.SectionNo, piece.SectionTitle);
            if (section == null || anchor != sectionAnchor)
            {
                FlushSection();
                sectionAnchor = anchor;
                section = new KbHandbookSectionDto
                {
                    Anchor = anchor,
                    Title = SectionLabel(piece)
                };
            }

            sectionParts.Add(piece.Content);
        }

        FlushSection();
        return chapters;
    }

    private static string ChapterLabel(Piece piece)
    {
        if (HandbookAnchor.IsAppendix(piece.Heading))
            return TrimLabel(string.IsNullOrEmpty(piece.ChapterNo) ? "附录" : "附录 " + piece.ChapterNo + " " + (piece.ChapterTitle ?? ""));
        if (!string.IsNullOrEmpty(piece.ChapterNo))
            return TrimLabel("第" + piece.ChapterNo + "章 " + (piece.ChapterTitle ?? ""));
        return TrimLabel(string.IsNullOrEmpty(piece.ChapterTitle) ? "前言" : piece.ChapterTitle);
    }

    private static string SectionLabel(Piece piece)
    {
        if (!string.IsNullOrEmpty(piece.SectionNo))
            return TrimLabel((piece.SectionNo + " " + (piece.SectionTitle ?? "")).Trim());
        if (!string.IsNullOrEmpty(piece.SectionTitle))
            return TrimLabel(piece.SectionTitle);
        return "正文";
    }

    private static string TrimLabel(string text)
    {
        var value = text.Trim();
        return value.Length <= 42 ? value : value[..42] + "…";
    }

    private sealed record EmbeddingProfile(string ProviderCode, string Model, string BaseUrl, string ApiKey);
    private sealed record ChunkRow(string Id, string Content);
    private sealed record Hit(
        string Id,
        string Heading,
        string Content,
        string? ChapterNo,
        string? ChapterTitle,
        string? SectionNo,
        string? SectionTitle,
        double Distance);
    private sealed record Piece(
        int ChunkIndex,
        string? ChapterNo,
        string? ChapterTitle,
        string? SectionNo,
        string? SectionTitle,
        string Heading,
        string Content);
}
