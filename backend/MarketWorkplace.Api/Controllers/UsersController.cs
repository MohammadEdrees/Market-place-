using MarketWorkplace.Application.Dtos;
using MarketWorkplace.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketWorkplace.Api.Controllers;

/// <summary>User profiles: contact details, the provider directory and self-service edits.</summary>
/// <remarks>Clients use this to view a seller/provider's details and get in touch.</remarks>
[ApiController]
[Authorize]
[Route("api/users")]
[Tags("Users")]
[Produces("application/json")]
public class UsersController(UsersService usersService) : ControllerBase
{
    /// <summary>Lists one page of user profiles with optional filters and sorting (never includes password hashes).</summary>
    /// <param name="search">Case-insensitive match against name, email or location.</param>
    /// <param name="role">Exact role match, e.g. <c>Provider</c>.</param>
    /// <param name="type"><c>Dashboard</c> or <c>Mobile</c>.</param>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Rows per page, clamped to 1–100 (default 20).</param>
    /// <param name="sortBy">Sort column: <c>id</c>, <c>name</c>, <c>email</c>, <c>role</c> or <c>type</c>; unknown values sort by id.</param>
    /// <param name="sortDir"><c>asc</c> (default) or <c>desc</c>.</param>
    /// <returns>One page of profiles — dashboard admins see everyone, other callers get the provider directory.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<UserProfileDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? sortBy = null,
        [FromQuery] string sortDir = "asc") =>
        usersService.GetAll(User, search, role, type, page, pageSize, sortBy, sortDir);

    /// <summary>Gets one user's profile, including contact info (phone / location / bio).</summary>
    /// <param name="id">The user identifier (e.g. the seller behind a product).</param>
    /// <returns>The profile, or <c>404 Not Found</c> when the identifier does not exist.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<UserProfileDto> GetById(int id) => usersService.GetById(id);

    /// <summary>Creates a dashboard or mobile user account (<c>SuperAdmin</c>, <c>Admin</c> or <c>Manager</c> only).</summary>
    /// <remarks>The platform is derived from the role: <c>Provider</c>/<c>Client</c> become <c>Mobile</c> accounts,
    /// every other role becomes <c>Dashboard</c>.</remarks>
    /// <param name="input">Name, email, password and role for the new account.</param>
    /// <returns>The created profile (never the password hash).</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<UserProfileDto> Create([FromBody] UserCreateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return usersService.Create(User, input);
    }

    /// <summary>Edits any user — profile fields, email, role and an optional password reset (dashboard admins only).</summary>
    /// <remarks>The platform is re-derived from the role (<c>Provider</c>/<c>Client</c> → <c>Mobile</c>).
    /// Leave <c>password</c> null or empty to keep the hash.</remarks>
    /// <param name="id">The user to edit.</param>
    /// <param name="input">New field values; name/email/role are required.</param>
    /// <returns>The updated profile.</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<UserProfileDto> Update(int id, [FromBody] UserEditInput input) =>
        usersService.Update(User, id, input);

    /// <summary>Uploads (or replaces) the user's profile picture — the account owner or a dashboard admin.</summary>
    /// <remarks>The old image file is deleted. Stored under <c>wwwroot/images/users</c> and returned as an
    /// absolute URL prefixed with <c>BackendUrl</c> from appsettings.</remarks>
    /// <param name="id">The user identifier.</param>
    /// <param name="file">Multipart <c>file</c> field; png/jpg/webp/gif up to 5 MB.
    /// No <c>[FromForm]</c> attribute — ApiExplorer infers <c>BindingSource.FormFile</c> from
    /// <see cref="IFormFile"/> itself (the attribute would break Swashbuckle's form description).</param>
    /// <returns>The updated profile with the new image URL.</returns>
    [HttpPost("{id:int}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> UploadImage(int id, IFormFile? file) =>
        await usersService.UploadImage(User, id, file);

    /// <summary>Removes the user's profile picture and deletes its file — the account owner or a dashboard admin.</summary>
    /// <param name="id">The user identifier.</param>
    /// <returns><c>204 No Content</c>, or <c>404</c> when no picture is set.</returns>
    [HttpDelete("{id:int}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteImage(int id) => usersService.DeleteImage(User, id);

    /// <summary>Updates the caller's own display name and contact fields.</summary>
    /// <remarks>Null leaves a field unchanged; send an empty string to clear it.
    /// Email, role and type are not editable here.</remarks>
    /// <param name="input">The fields to change.</param>
    /// <returns>The updated profile.</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public ActionResult<UserProfileDto> UpdateMe([FromBody] UserUpdateInput input)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return usersService.UpdateMe(User, input);
    }
}
