using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>Roles CRUD: the named roles accounts are assigned to.</summary>
/// <remarks>Listing is open to any signed-in user; mutations are limited to dashboard admins.
/// A role with assigned accounts cannot be deleted (the API answers <c>409 Conflict</c>).</remarks>
[ApiController]
[Authorize]
[Route("api/roles")]
[Tags("Roles")]
[Produces("application/json")]
public class RolesController(RolesService rolesService) : ControllerBase
{
    /// <summary>Lists all roles with the number of accounts assigned to each.</summary>
    /// <returns>Every role, ordered by id (seed order).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<RoleDto>> GetAll() => rolesService.GetAll();

    /// <summary>Gets one role with its account count.</summary>
    /// <param name="id">The role identifier.</param>
    /// <returns>The role, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<RoleDto> GetById(int id) => rolesService.GetById(id);

    /// <summary>Creates a role (dashboard admins only).</summary>
    /// <param name="input">Unique name plus optional description.</param>
    /// <returns>The created role with a zero account count.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<RoleDto> Create([FromBody] RoleInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return rolesService.Create(User, input);
    }

    /// <summary>Renames a role or changes its description (dashboard admins only).</summary>
    /// <remarks>Members pick the new name up on their next sign-in.</remarks>
    /// <param name="id">The role to edit.</param>
    /// <param name="input">New name and description; the name stays required.</param>
    /// <returns>The updated role.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<RoleDto> Update(int id, [FromBody] RoleInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return rolesService.Update(User, id, input);
    }

    /// <summary>Deletes a role that no accounts use (dashboard admins only).</summary>
    /// <param name="id">The role to delete.</param>
    /// <returns><c>204 No Content</c>; <c>404</c> when unknown, <c>409</c> while accounts still hold it.</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public IActionResult Delete(int id) => rolesService.Delete(User, id);
}
