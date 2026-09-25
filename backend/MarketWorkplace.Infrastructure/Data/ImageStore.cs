using MarketWorkplace.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>
/// Validates uploaded images, stores them under <c>wwwroot/images/{folder}</c> (served by the
/// static-files middleware) and hands out their public URLs. The base URL comes from
/// <c>BackendUrl</c> in appsettings.json (e.g. <c>http://localhost:5240</c>), so every path the
/// API returns is absolute, e.g. <c>http://localhost:5240/images/products/ab12….png</c>.
/// </summary>
public class ImageStore(IWebHostEnvironment env, IConfiguration config) : IImageStore
{
    /// <summary>Public base URL from appsettings (no trailing slash); empty when not configured.</summary>
    private readonly string _baseUrl = (config["BackendUrl"] ?? string.Empty).TrimEnd('/');

    /// <summary>wwwroot path; created on demand by <see cref="SaveAsync"/>.</summary>
    private string WebRoot =>
        env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

    /// <summary>Prefixes a relative path (<c>/images/…</c>) with the configured <c>BackendUrl</c>.</summary>
    /// <remarks>Absolute URLs and an empty configuration pass through unchanged.</remarks>
    public string ToUrl(string path)
    {
        if (string.IsNullOrEmpty(path) ||
            path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }
        return _baseUrl + path;
    }

    /// <summary>Strips the configured <c>BackendUrl</c>, turning an absolute URL back into <c>/images/…</c>.</summary>
    public string ToRelative(string webPath)
    {
        if (!string.IsNullOrEmpty(_baseUrl) &&
            webPath.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            webPath = webPath[_baseUrl.Length..];
        }
        return webPath;
    }

    /// <summary>Persists an upload and returns its public URL, e.g. <c>http://localhost:5240/images/products/ab12….png</c>.</summary>
    public async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var directory = Path.Combine(WebRoot, "images", folder);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = File.Create(Path.Combine(directory, fileName));
        await file.CopyToAsync(stream);

        return ToUrl($"/images/{folder}/{fileName}");
    }

    /// <summary>Deletes the file behind a stored path (absolute URL or relative); missing files are ignored.</summary>
    public void Delete(string webPath)
    {
        var relative = ToRelative(webPath);
        if (!relative.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var segments = relative["/images/".Length..].Replace('/', Path.DirectorySeparatorChar);
        if (segments.Contains(".."))
        {
            return;
        }

        var fullPath = Path.Combine(WebRoot, "images", segments);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
