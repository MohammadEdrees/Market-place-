using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>
/// Backup and restore for the dashboard's Settings page: snapshot the whole dataset to JSON,
/// keep the snapshots server-side, and apply one back when needed.
/// </summary>
/// <remarks>
/// <b>Every endpoint is admin-only</b> — snapshots contain password hashes. Files live in
/// <c>App_Data/backups</c>, never under <c>wwwroot</c>, so the static-files middleware cannot
/// serve them. Uploaded image files are not included, only the rows that reference them.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/backup")]
[Tags("Backup")]
[Produces("application/json")]
public class BackupController(BackupService backupService) : ControllerBase
{
    /// <summary>Lists the stored snapshots with their row counts, newest first.</summary>
    /// <returns>Stored backups (admin callers only).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BackupFileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IEnumerable<BackupFileDto>> List() => backupService.List(User);

    /// <summary>
    /// Snapshots the live data, stores a copy under <c>App_Data/backups</c> and returns the JSON
    /// file for download (admin callers only).
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Export()
    {
        var result = backupService.Export(User);
        if (result.Result is not OkObjectResult ok || ok.Value is not ValueTuple<string, byte[]> payload)
        {
            return result.Result ?? StatusCode(StatusCodes.Status500InternalServerError);
        }

        return File(payload.Item2, "application/json", payload.Item1);
    }

    /// <summary>Downloads a stored snapshot by file name (admin callers only).</summary>
    /// <param name="name">File name inside the backup folder, e.g. <c>marketplace-20260925-180000-123.json</c>.</param>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Download(string name)
    {
        var result = backupService.Download(User, name);
        if (result.Result is not OkObjectResult ok || ok.Value is not byte[] content)
        {
            return result.Result ?? StatusCode(StatusCodes.Status500InternalServerError);
        }

        return File(content, "application/json", name);
    }

    /// <summary>Deletes a stored snapshot (admin callers only).</summary>
    /// <param name="name">File name inside the backup folder.</param>
    [HttpDelete("{name}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteBackup(string name) => backupService.DeleteBackup(User, name);

    /// <summary>
    /// Applies a backup file (admin callers only).
    /// </summary>
    /// <param name="file">Multipart <c>file</c> field: a snapshot from <c>GET /api/backup/export</c>,
    /// up to 25 MB. No <c>[FromForm]</c> attribute — ApiExplorer infers
    /// <c>BindingSource.FormFile</c> from <see cref="IFormFile"/> itself.</param>
    /// <param name="mode">Multipart <c>mode</c> field: <c>merge</c> (default, upsert by id and
    /// never deletes) or <c>replace</c> (wipes every table first, inside one transaction).</param>
    /// <returns>A per-table report of inserted/updated rows.</returns>
    [HttpPost("restore")]
    [RequestSizeLimit(26 * 1024 * 1024)]
    [ProducesResponseType(typeof(RestoreReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RestoreReportDto>> Restore(IFormFile? file, [FromForm] string? mode)
    {
        using var buffer = new MemoryStream();
        if (file is not null)
        {
            await file.CopyToAsync(buffer);
        }

        return backupService.Restore(User, buffer.ToArray(), mode);
    }
}
