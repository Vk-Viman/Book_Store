using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Readify.Services
{
    public class StorageOptions
    {
        public string RootPath { get; set; } = "wwwroot";
        public string ImagesPath { get; set; } = "images";
        public long MaxImageSizeBytes { get; set; } = 2 * 1024 * 1024; // 2MB
        public string PublicBaseUrl { get; set; } = string.Empty; // e.g. https://site.com
    }

    public interface IUploadService
    {
        Task<string> SaveImageAsync(IFormFile file, CancellationToken ct = default);
    }

    public class LocalUploadService : IUploadService
    {
        private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly IWebHostEnvironment _env;
        private readonly StorageOptions _opts;
        private readonly IHttpContextAccessor _http;

        public LocalUploadService(IWebHostEnvironment env, IOptions<StorageOptions> opts, IHttpContextAccessor http)
        {
            _env = env;
            _opts = opts.Value;
            _http = http;
        }

        public async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0) throw new InvalidOperationException("File is required");
            if (file.Length > _opts.MaxImageSizeBytes) throw new InvalidOperationException("File too large");

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !Allowed.Contains(ext)) throw new InvalidOperationException("Unsupported file type");

            var root = Path.Combine(Directory.GetCurrentDirectory(), _opts.RootPath);
            var imagesDir = Path.Combine(root, _opts.ImagesPath);
            Directory.CreateDirectory(imagesDir);

            var name = $"{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(imagesDir, name);
            using (var stream = new FileStream(full, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var relative = $"/{_opts.ImagesPath}/{name}".Replace("\\", "/");

            var baseUrl = _opts.PublicBaseUrl?.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrEmpty(baseUrl))
            {
                var req = _http.HttpContext?.Request;
                if (req != null)
                {
                    baseUrl = $"{req.Scheme}://{req.Host.ToUriComponent()}";
                }
            }

            if (!string.IsNullOrEmpty(baseUrl))
            {
                return baseUrl + relative;
            }
            return relative;
        }
    }
}
