using MarketWorkplace.Application.Common;
using Microsoft.AspNetCore.Hosting;

namespace MarketWorkplace.Infrastructure.Data;

/// <summary>
/// File storage for backup snapshots under <c>App_Data/backups</c> next to the API.
/// Deliberately <b>not</b> under <c>wwwroot</c>: a snapshot contains password hashes and the
/// static-files middleware would otherwise publish it to anyone who guesses the name.
/// </summary>
public class BackupStore(IWebHostEnvironment env) : IBackupStore
{
    /// <summary>Folder holding the snapshots; created on the first write or read.</summary>
    private string Root => Path.Combine(env.ContentRootPath, "App_Data", "backups");

    /// <summary>Stored snapshots, newest first.</summary>
    public IReadOnlyList<StoredBackup> List()
    {
        var root = EnsureRoot();
        return Directory.GetFiles(root, "*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ThenByDescending(f => f.Name)
            .Select(f => new StoredBackup(f.Name, f.LastWriteTimeUtc, f.Length))
            .ToList();
    }

    /// <summary>Writes (or overwrites) a snapshot.</summary>
    public void Save(string name, byte[] content)
    {
        var safe = SafeName(name);
        if (safe is null)
        {
            throw new ArgumentException("Invalid backup file name.", nameof(name));
        }

        File.WriteAllBytes(Path.Combine(EnsureRoot(), safe), content);
    }

    /// <summary>Reads a snapshot, or <c>null</c> when it does not exist.</summary>
    public byte[]? Read(string name)
    {
        var safe = SafeName(name);
        if (safe is null)
        {
            return null;
        }

        var path = Path.Combine(Root, safe);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    /// <summary>Deletes a snapshot; returns <c>false</c> when it did not exist.</summary>
    public bool Delete(string name)
    {
        var safe = SafeName(name);
        if (safe is null)
        {
            return false;
        }

        var path = Path.Combine(Root, safe);
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    /// <summary>Keeps the newest snapshots and removes the rest.</summary>
    public int Prune(int keepNewest)
    {
        var all = List();
        if (all.Count <= keepNewest)
        {
            return 0;
        }

        var removed = 0;
        foreach (var stale in all.Skip(keepNewest))
        {
            if (Delete(stale.Name))
            {
                removed++;
            }
        }

        return removed;
    }

    private string EnsureRoot()
    {
        var root = Root;
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// Rejects anything that is not a plain <c>*.json</c> file name, so a request can never walk
    /// out of the backup folder.
    /// </summary>
    private static string? SafeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("..", StringComparison.Ordinal) ||
            !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal))
        {
            return null;
        }

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            if (name.Contains(invalid, StringComparison.Ordinal))
            {
                return null;
            }
        }

        return name;
    }
}
