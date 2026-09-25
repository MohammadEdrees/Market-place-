using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

/// <summary>Request body for creating or updating a role.</summary>
public class RoleInput
{
    /// <summary>Unique display name, e.g. <c>Editor</c>.</summary>
    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    /// <summary>What the role is for, shown in the Roles table.</summary>
    [StringLength(300)]
    public string? Description { get; set; }
}

/// <summary>A role as returned by the roles endpoints.</summary>
/// <param name="Id">Role identifier.</param>
/// <param name="Name">Unique display name; also carried by the token's <c>role</c> claim.</param>
/// <param name="Description">What the role is for.</param>
/// <param name="UserCount">How many accounts are currently assigned to the role.</param>
public record RoleDto(int Id, string Name, string? Description, int UserCount);
