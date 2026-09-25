using Microsoft.AspNetCore.Http;

namespace MarketWorkplace.Application.Interfaces;

/// <summary>Storage for image files under wwwroot (implemented in Infrastructure).</summary>
public interface IImageStore
{
    /// <summary>Persists an upload and returns its public URL (prefixed with <c>BackendUrl</c>).</summary>
    Task<string> SaveAsync(IFormFile file, string folder);

    /// <summary>Prefixes a relative path (<c>/images/…</c>) with the configured <c>BackendUrl</c>.</summary>
    string ToUrl(string path);

    /// <summary>Strips the configured <c>BackendUrl</c>, turning an absolute URL back into <c>/images/…</c>.</summary>
    string ToRelative(string webPath);

    /// <summary>Deletes the file behind a stored path (absolute URL or relative); missing files are ignored.</summary>
    void Delete(string webPath);
}
