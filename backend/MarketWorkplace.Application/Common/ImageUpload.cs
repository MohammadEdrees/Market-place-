using Microsoft.AspNetCore.Http;

namespace MarketWorkplace.Application.Common;

/// <summary>Upload rules shared by the application services and the storage implementation.</summary>
public static class ImageUpload
{
    /// <summary>Maximum size of a single image upload (5 MB).</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>Maximum number of images per listing.</summary>
    public const int MaxPerListing = 10;

    /// <summary>Extensions accepted for uploads (lower-case, with dot).</summary>
    public static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"];

    /// <summary>Validates an upload and returns an error message, or <c>null</c> when acceptable.</summary>
    public static string? Validate(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "The uploaded file is empty.";
        }
        if (file.Length > MaxBytes)
        {
            return "Images must be 5 MB or smaller.";
        }
        if (!(file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return "The file must be an image.";
        }
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
        {
            return "Only png, jpg, jpeg, webp or gif images are allowed.";
        }
        return null;
    }
}
