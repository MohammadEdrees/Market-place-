namespace MarketWorkplace.Domain.Entities;

/// <summary>A named role accounts are assigned to (managed by <c>/api/roles</c>).</summary>
/// <remarks>
/// The <see cref="Name"/> is what the token's <c>role</c> claim carries and what the
/// authorization rules (<c>Access</c>) compare against — renaming a role therefore changes
/// what its members may do on their next sign-in.
/// </remarks>
public class Role
{
    public int Id { get; set; }

    /// <summary>Unique display name, e.g. <c>Admin</c> or <c>Provider</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>What the role is for, shown in the Roles table.</summary>
    public string? Description { get; set; }

    /// <summary>Accounts assigned to this role.</summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
