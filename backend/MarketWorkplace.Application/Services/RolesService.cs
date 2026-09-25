using System.Security.Claims;
using MarketWorkplace.Application.Common;
using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Interfaces;
using MarketWorkplace.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MarketWorkplace.Application.Common.ServiceResults;

namespace MarketWorkplace.Application.Services;

/// <summary>Roles CRUD (backs <c>RolesController</c>): the table every account role resolves against.</summary>
public class RolesService(IRoleRepository roles)
{
    /// <summary>All roles with their account counts, in seed (id) order.</summary>
    public ActionResult<IEnumerable<RoleDto>> GetAll()
    {
        var items = roles.QueryReadOnly()
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.Users.Count))
            .ToList();

        return Ok(items);
    }

    /// <summary>One role with its account count, or <c>404</c>.</summary>
    public ActionResult<RoleDto> GetById(int id)
    {
        var role = roles.QueryReadOnly()
            .Where(r => r.Id == id)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.Users.Count))
            .FirstOrDefault();

        return role is null ? NotFound() : Ok(role);
    }

    /// <summary>Creates a role (admin callers only); names are unique regardless of case.</summary>
    public ActionResult<RoleDto> Create(ClaimsPrincipal caller, RoleInput input)
    {
        if (!Access.IsAdmin(caller))
        {
            return Forbid();
        }

        var name = input.Name.Trim();
        if (roles.QueryReadOnly().Any(r => r.Name.ToLower() == name.ToLower()))
        {
            return Problem(
                title: "Role already exists",
                detail: "Another role already uses this name.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var role = new Role
        {
            Id = roles.QueryReadOnly().Any() ? roles.QueryReadOnly().Max(r => r.Id) + 1 : 1,
            Name = name,
            Description = NullIfBlank(input.Description),
        };

        roles.Add(role);
        roles.SaveChanges();

        return CreatedAtAction("GetById", new { id = role.Id }, new RoleDto(role.Id, role.Name, role.Description, 0));
    }

    /// <summary>Renames a role or changes its description (admin callers only).</summary>
    /// <remarks>Members pick the new name up on their next sign-in — it is what the token claim carries.</remarks>
    public ActionResult<RoleDto> Update(ClaimsPrincipal caller, int id, RoleInput input)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var role = roles.Query().FirstOrDefault(r => r.Id == id);
        if (role is null) return NotFound();

        var name = NullIfBlank(input.Name);
        if (name is null)
            return Problem(title: "Invalid role", detail: "Name is required.", statusCode: StatusCodes.Status400BadRequest);
        if (roles.QueryReadOnly().Any(r => r.Id != id && r.Name.ToLower() == name.ToLower()))
            return Problem(title: "Role already exists", detail: "Another role already uses this name.", statusCode: StatusCodes.Status409Conflict);

        role.Name = name;
        role.Description = NullIfBlank(input.Description);
        roles.SaveChanges();

        var userCount = roles.QueryReadOnly().Where(r => r.Id == id).Select(r => r.Users.Count).FirstOrDefault();
        return Ok(new RoleDto(role.Id, role.Name, role.Description, userCount));
    }

    /// <summary>Deletes a role that no accounts use (admin callers only).</summary>
    /// <remarks>Returns <c>409 Conflict</c> while accounts are assigned — reassign them first.</remarks>
    public IActionResult Delete(ClaimsPrincipal caller, int id)
    {
        if (!Access.IsAdmin(caller)) return Forbid();

        var role = roles.Query().FirstOrDefault(r => r.Id == id);
        if (role is null) return NotFound();

        var userCount = roles.QueryReadOnly().Where(r => r.Id == id).Select(r => r.Users.Count).FirstOrDefault();
        if (userCount > 0)
        {
            return Problem(
                title: "Role in use",
                detail: $"Reassign the {userCount} account(s) using this role before deleting it.",
                statusCode: StatusCodes.Status409Conflict);
        }

        roles.Remove(role);
        roles.SaveChanges();
        return NoContent();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
