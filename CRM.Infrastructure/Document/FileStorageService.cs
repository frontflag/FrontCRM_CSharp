using System.Collections.Concurrent;
using CRM.Core.Document;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CRM.Infrastructure.Document
{
    public class FileStorageService : IFileStorageService
    {
        private readonly DocumentModuleOptions _options;
        private readonly FileNameGenerator _nameGenerator;
        private static readonly ConcurrentDictionary<string, byte[]> MagicSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 },
            [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A },
            [".zip"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            [".docx"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
            [".xlsx"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 },
        };

        public FileStorageService(IOptions<DocumentModuleOptions> options, FileNameGenerator nameGenerator)
        {
            _options = options.Value;
            _nameGenerator = nameGenerator;
        }

        public async Task<StoredFileResult> SaveAsync(DocumentSaveContext ctx)
        {
            var ext = Path.GetExtension(ctx.OriginalFileName);
            if (string.IsNullOrEmpty(ext)) ext = ".bin";
            ext = ext.ToLowerInvariant();

            if (!_options.AllowedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"不支持的文件格式: {ext}");

            var maxBytes = _options.MaxFileSizeMb * 1024L * 1024L;
            if (ctx.FileStream.CanSeek && ctx.FileStream.Length > maxBytes)
                throw new ArgumentException($"文件大小超过限制（{_options.MaxFileSizeMb}MB）");

            // 读取前 16 字节做简单魔数校验（防 exe 伪装）
            var header = new byte[16];
            var read = await ctx.FileStream.ReadAsync(header, 0, header.Length);
            if (read >= 4 && MagicSignatures.TryGetValue(ext, out var sig) && read >= sig.Length)
            {
                if (!header.Take(sig.Length).SequenceEqual(sig))
                    throw new ArgumentException("文件内容与扩展名不符，可能为伪装文件");
            }
            ctx.FileStream.Position = 0;

            var storedName = await _nameGenerator.GenerateAsync(ctx.BizType, ctx.BizId, ctx.OriginalFileName);
            var relDir = Path.Combine("UploadFile", ctx.BizType, ctx.BizId).Replace('\\', '/');
            var fullDir = Path.Combine(_options.RootPath, relDir);
            Directory.CreateDirectory(fullDir);
            var fullPath = Path.Combine(fullDir, storedName);
            var relativePath = $"{relDir}/{storedName}".Replace('\\', '/');

            await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await ctx.FileStream.CopyToAsync(fs);
            }

            var fileSize = new FileInfo(fullPath).Length;
            var isImage = ext is ".jpg" or ".jpeg" or ".png";
            string? thumbRel = null;

            if (isImage && _options.Thumbnail.Enable)
            {
                try
                {
                    thumbRel = await GenerateThumbnailAsync(fullPath, storedName, relDir);
                }
                catch
                {
                    // 缩略图失败不阻塞主流程
                }
            }

            return new StoredFileResult
            {
                StoredFileName = storedName,
                RelativePath = relativePath,
                FileSize = fileSize,
                IsImage = isImage,
                ThumbnailRelativePath = thumbRel,
                FileExtension = ext,
                MimeType = ctx.ContentType
            };
        }

        private async Task<string?> GenerateThumbnailAsync(string fullPath, string storedName, string relDir)
        {
            var thumbName = Path.GetFileNameWithoutExtension(storedName) + "_thumb.jpg";
            var thumbFull = Path.Combine(_options.RootPath, relDir, thumbName).Replace('/', Path.DirectorySeparatorChar);
            var thumbRel = $"{relDir}/{thumbName}".Replace('\\', '/');

            await using var source = File.OpenRead(fullPath);
            using var image = await Image.LoadAsync(source);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(_options.Thumbnail.MaxWidth, _options.Thumbnail.MaxHeight),
                Mode = ResizeMode.Max
            }));
            await image.SaveAsJpegAsync(thumbFull, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = _options.Thumbnail.Quality });
            return thumbRel;
        }

        public Task<Stream> OpenReadAsync(string relativePath)
        {
            var full = Path.Combine(_options.RootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full))
                throw new FileNotFoundException("文件不存在", relativePath);
            return Task.FromResult<Stream>(new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public Task<bool> ExistsAsync(string relativePath)
        {
            var full = Path.Combine(_options.RootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult(File.Exists(full));
        }

        public Task DeleteAsync(string relativePath)
        {
            var full = Path.Combine(_options.RootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full))
                File.Delete(full);
            return Task.CompletedTask;
        }
    }
}
