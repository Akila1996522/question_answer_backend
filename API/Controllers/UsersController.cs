using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using question_answer.Domain.Enums;
using question_answer.Infrastructure.Data;

namespace question_answer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] RoleType? role, [FromQuery] UserStatus? status)
    {
        var query = _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        if (role.HasValue)
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == role.Value));

        var users = await query.Select(u => new
        {
            u.Id,
            u.FirstName,
            u.LastName,
            u.Email,
            u.Status,
            Roles = u.UserRoles.Select(ur => ur.Role!.Name.ToString()).ToList(),
            u.CreatedAt
        }).ToListAsync();

        return Ok(users);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        user.Status = dto.Status;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User status updated successfully.", NewStatus = user.Status });
    }
}

public class UpdateUserStatusDto
{
    public UserStatus Status { get; set; }
}
