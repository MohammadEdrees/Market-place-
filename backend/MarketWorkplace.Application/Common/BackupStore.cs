namespace MarketWorkplace.Application.Common;

/// <summary>A snapshot file kept in the server-side backup folder.</summary>
/// <param name="Name">File name (safe, ends with <c>.json</c>).</param>
/// <param name="CreatedAtUtc">Last write time.</param>
/// <param name="SizeBytes">File size on disk.</param>
public record StoredBackup(string Name, DateTime CreatedAtUtc, long SizeBytes);

/// <summary>
/// Server-side storage for backup snapshots. Backups contain password hashes, so they live
/// <b>outside</b> <c>wwwroot</c> (never served statically) and every endpoint that touches them
/// is restricted to dashboard admins.
/// </summary>
public interface IBackupStore
{
    /// <summary>Stored snapshots, newest first.</summary>
    IReadOnlyList<StoredBackup> List();

    /// <summary>Writes (or overwrites) a snapshot; the folder is created on demand.</summary>
    void Save(string name, byte[] content);

    /// <summary>Reads a snapshot, or <c>null</c> when it does not exist.</summary>
    byte[]? Read(string name);

    /// <summary>Deletes a snapshot; returns <c>false</c> when it did not exist.</summary>
    bool Delete(string name);

    /// <summary>Keeps the <paramref name="keepNewest"/> most recent snapshots and returns how many were removed.</summary>
    int Prune(int keepNewest);
}
