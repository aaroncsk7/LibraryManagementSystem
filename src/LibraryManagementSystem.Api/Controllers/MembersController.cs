using LibraryManagementSystem.Api.Data;
using LibraryManagementSystem.Api.DTOs;
using LibraryManagementSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly LibraryContext _context;

    public MembersController(LibraryContext context)
    {
        _context = context;
    }

    // GET: api/members
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberReadDto>>> GetMembers()
    {
        var members = await _context.Members
            .OrderBy(m => m.FullName)
            .Select(m => new MemberReadDto
            {
                Id = m.Id,
                FullName = m.FullName,
                Email = m.Email,
                PhoneNumber = m.PhoneNumber,
                JoinDate = m.JoinDate
            })
            .ToListAsync();

        return Ok(members);
    }

    // GET: api/members/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MemberReadDto>> GetMember(int id)
    {
        var member = await _context.Members.FindAsync(id);

        if (member is null)
        {
            return NotFound(new { message = $"Member with id {id} was not found." });
        }

        return Ok(ToReadDto(member));
    }

    // POST: api/members
    [HttpPost]
    public async Task<ActionResult<MemberReadDto>> CreateMember(MemberWriteDto dto)
    {
        if (await _context.Members.AnyAsync(m => m.Email == dto.Email))
        {
            return Conflict(new { message = $"A member with email {dto.Email} already exists." });
        }

        var member = new Member
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            JoinDate = DateTime.UtcNow
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMember), new { id = member.Id }, ToReadDto(member));
    }

    // PUT: api/members/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMember(int id, MemberWriteDto dto)
    {
        var member = await _context.Members.FindAsync(id);
        if (member is null)
        {
            return NotFound(new { message = $"Member with id {id} was not found." });
        }

        if (await _context.Members.AnyAsync(m => m.Email == dto.Email && m.Id != id))
        {
            return Conflict(new { message = $"Another member already uses email {dto.Email}." });
        }

        member.FullName = dto.FullName;
        member.Email = dto.Email;
        member.PhoneNumber = dto.PhoneNumber;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/members/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMember(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member is null)
        {
            return NotFound(new { message = $"Member with id {id} was not found." });
        }

        _context.Members.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static MemberReadDto ToReadDto(Member m) => new()
    {
        Id = m.Id,
        FullName = m.FullName,
        Email = m.Email,
        PhoneNumber = m.PhoneNumber,
        JoinDate = m.JoinDate
    };
}
